const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(method) {
  const context = { window: {}, I18N: { t: key => key } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
  let resolve;
  const pending = new Promise(done => { resolve = done; });
  const calls = { problems: [], status: [] };
  const ide = { running: true, term: { write() {}, focus() {} }, setStatus(text) { calls.status.push(text); },
    showIdeProblem(key, message) { calls.problems.push(message); }, trace: { finished() {}, update() {}, clearMarker() {} } };
  const api = { onBackendEvent() {}, onBackendFault() {}, [method]: () => pending };
  const controller = new context.window.ExecutionController(ide, api);
  Object.assign(controller, { sessionId: 'old', inputRequestId: 'input-1', inputText: '42', stopId: 'stop-1' });
  const fail = () => resolve({ ok: false, error: { code: 'INVALID_STATE', message: 'Старая команда уже недействительна' } });
  return { controller, ide, calls, api, fail };
}

for (const [name, method, action] of [
  ['INPUT', 'submitInput', controller => controller.input('\r')],
  ['Stop', 'stopExecution', controller => controller.stop()],
  ['шаг отладки', 'debugCommand', controller => controller.debug('stepOver')],
]) {
  test(`Поздний отказ ${name} не изменяет новый запуск`, async () => {
    const { controller, ide, calls, fail } = setup(method);
    const operation = action(controller);
    calls.status.length = 0;
    Object.assign(controller, { sessionId: 'new', inputRequestId: 'input-2', inputText: 'Анна', stopId: 'stop-2', debugBusy: true });
    fail(); await operation;
    assert.equal(controller.sessionId, 'new');
    assert.equal(controller.inputRequestId, 'input-2');
    assert.equal(controller.inputText, 'Анна');
    assert.equal(controller.stopId, 'stop-2');
    assert.equal(controller.debugBusy, true);
    assert.equal(ide.running, true);
    assert.deepEqual(calls.problems, []);
    assert.deepEqual(calls.status, []);
  });
}

test('Поздний отказ ответа INPUT не закрывает следующий запрос той же сессии', async () => {
  const { controller, ide, calls, fail } = setup('submitInput');
  const operation = controller.input('\r');
  controller.inputRequestId = 'input-2'; controller.inputText = 'Анна';
  fail(); await operation;
  assert.equal(controller.sessionId, 'old');
  assert.equal(controller.inputRequestId, 'input-2');
  assert.equal(ide.running, true);
  assert.deepEqual(calls.problems, []);
});

test('Отказ действующего INPUT остаётся видимой ошибкой', async () => {
  const { controller, ide, calls, fail } = setup('submitInput');
  const operation = controller.input('\r');
  fail(); await operation;
  assert.equal(ide.running, true);
  assert.equal(controller.inputRequestId, 'input-1');
  assert.equal(controller.inputText, '42');
  assert.equal(calls.status.at(-1), 'Старая команда уже недействительна');
  assert.equal(calls.problems.length, 0);
});

test('Поздний отказ INPUT не изменяет программу после завершения следующего запроса', async () => {
  const { controller, ide, calls, fail } = setup('submitInput');
  const operation = controller.input('\r');
  controller.event({ sessionId: 'old', event: 'input.request', payload: { inputRequestId: 'input-2', prompt: 'Имя', outputText: '' } });
  controller.event({ sessionId: 'old', event: 'input.completed', payload: { inputRequestId: 'input-2' } });
  fail(); await operation;
  assert.equal(controller.sessionId, 'old');
  assert.equal(ide.running, true);
  assert.deepEqual(calls.problems, []);
});

test('Поздний ответ шага не сбрасывает ожидание более новой команды той же сессии', async () => {
  const { controller, calls, api, fail } = setup('debugCommand');
  const first = controller.debug('stepOver');
  let resolveSecond;
  api.debugCommand = () => new Promise(resolve => { resolveSecond = resolve; });
  controller.stopId = 'stop-2';
  const second = controller.debug('stepInto');
  calls.status.length = 0;
  fail(); await first;
  assert.equal(controller.debugBusy, true);
  assert.equal(controller.stopId, null);
  assert.deepEqual(calls.status, []);
  resolveSecond({ ok: true }); await second;
  assert.equal(controller.debugBusy, false);
});
