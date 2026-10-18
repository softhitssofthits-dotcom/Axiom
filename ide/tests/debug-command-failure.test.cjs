const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, document: { getElementById: () => ({}) }, performance: { now: () => 0 },
    I18N: { t: (key, line) => line ? `${key}:${line}` : key, setText: () => {} } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
  let resolve, reject;
  const pending = new Promise((done, fail) => { resolve = done; reject = fail; });
  const calls = { status: [], marker: null };
  const ide = { running: true, term: { write() {} }, focusEditor() {}, showIdeProblem() {}, editor: { revealLineInCenter() {}, setPosition() {}, focus() {} },
    setStatus(text) { calls.status.push(text); }, trace: {
      clearMarker() { calls.marker = null; }, mark(payload) { calls.marker = payload.line; }, update() {}, finished() {},
    } };
  const api = { onBackendEvent() {}, onBackendFault() {}, debugCommand: () => pending };
  const controller = new context.window.ExecutionController(ide, api);
  Object.assign(controller, { sessionId: 'session', stopId: 'stop-1', line: 7, callDepth: 1, canStepBack: true });
  calls.marker = 7;
  const pause = () => controller.event({ sessionId: 'session', event: 'debug.paused',
    payload: { stopId: 'stop-2', line: 9, column: 1, callDepth: 2, canStepBack: true, outputText: '' } });
  return { controller, ide, api, calls, resolve, reject, pause };
}

for (const rejected of [false, true]) {
  test(`Отказ шага восстанавливает остановку и указатель: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { controller, ide, calls, api, resolve, reject } = setup();
    const operation = controller.debug('stepOver');
    assert.equal(calls.marker, null);
    if (rejected) reject(new Error('Команда не доставлена'));
    else resolve({ ok: false, error: { message: 'Команда не доставлена' } });
    await assert.doesNotReject(operation);
    assert.equal(controller.stopId, 'stop-1');
    assert.equal(controller.debugBusy, false);
    assert.equal(calls.marker, 7);
    assert.equal(calls.status.at(-1), 'Команда не доставлена');
    assert.equal(ide.running, true);
    let called = false;
    api.debugCommand = async (session, command, stopId) => {
      assert.equal(stopId, 'stop-1'); called = true; return { ok: true };
    };
    await controller.debug('stepInto');
    assert.equal(called, true);
  });

  test(`Поздний отказ не меняет новую остановку той же сессии: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { controller, calls, resolve, reject, pause } = setup();
    const operation = controller.debug('stepOver');
    pause();
    assert.equal(controller.debugBusy, false);
    calls.status.length = 0;
    if (rejected) reject(new Error('Старый отказ'));
    else resolve({ ok: false, error: { message: 'Старый отказ' } });
    await assert.doesNotReject(operation);
    assert.equal(controller.stopId, 'stop-2');
    assert.equal(controller.line, 9);
    assert.equal(controller.callDepth, 2);
    assert.equal(calls.marker, 9);
    assert.deepEqual(calls.status, []);
  });
}

test('Позднее исключение не снимает занятость следующего шага', async () => {
  const { controller, calls, api, reject, pause } = setup();
  const first = controller.debug('stepOver');
  pause();
  let resolveSecond;
  api.debugCommand = () => new Promise(resolve => { resolveSecond = resolve; });
  const second = controller.debug('stepOver');
  calls.status.length = 0;
  reject(new Error('Старое исключение'));
  await assert.doesNotReject(first);
  assert.equal(controller.debugBusy, true);
  assert.equal(controller.stopId, null);
  assert.deepEqual(calls.status, []);
  resolveSecond({ ok: false, error: { message: 'Текущий отказ' } });
  await second;
  assert.equal(controller.stopId, 'stop-2');
  assert.equal(controller.debugBusy, false);
  assert.equal(calls.marker, 9);
  assert.equal(calls.status.at(-1), 'Текущий отказ');
});

test('Позднее исключение после завершения не возвращает остановку', async () => {
  const { controller, ide, calls, reject } = setup();
  const operation = controller.debug('continue');
  controller.event({ sessionId: 'session', event: 'execution.finished', payload: { status: 'Succeeded', outputText: '' } });
  calls.status.length = 0;
  reject(new Error('Поздняя доставка'));
  await assert.doesNotReject(operation);
  assert.equal(controller.stopId, null);
  assert.equal(controller.debugBusy, false);
  assert.equal(ide.running, false);
  assert.deepEqual(calls.status, []);
});

test('Позднее исключение сохраняет новую сессию и её занятость', async () => {
  const { controller, calls, reject } = setup();
  const operation = controller.debug('stepOver');
  Object.assign(controller, { sessionId: 'new-session', stopId: 'new-stop', line: 11, debugBusy: true });
  calls.status.length = 0; calls.marker = 11;
  reject(new Error('Прежняя сессия'));
  await assert.doesNotReject(operation);
  assert.equal(controller.sessionId, 'new-session');
  assert.equal(controller.stopId, 'new-stop');
  assert.equal(controller.debugBusy, true);
  assert.equal(calls.marker, 11);
  assert.deepEqual(calls.status, []);
});

test('Авария аннулирует команду даже при повторном идентификаторе сессии', async () => {
  const { controller, ide, calls, resolve } = setup();
  const operation = controller.debug('stepOver');
  controller.fault('Авария службы');
  Object.assign(controller, { sessionId: 'session', stopId: 'new-stop', line: 11, debugBusy: false });
  ide.running = true; calls.status.length = 0; calls.marker = 11;
  resolve({ ok: false, error: { message: 'Прежняя команда' } });
  await operation;
  assert.equal(controller.stopId, 'new-stop');
  assert.equal(calls.marker, 11);
  assert.deepEqual(calls.status, []);
});
