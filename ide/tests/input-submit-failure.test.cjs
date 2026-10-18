const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, document: { getElementById: () => ({}) }, performance: { now: () => 0 }, I18N: { t: key => key, setText: () => {} } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
  const calls = { status: [], problems: [], submitted: [], output: '', focus: 0 };
  const pending = [];
  const api = { onBackendEvent() {}, onBackendFault() {}, acknowledgeOutput() {},
    submitInput: (...args) => { calls.submitted.push(args); return new Promise((resolve, reject) => pending.push({ resolve, reject })); } };
  const ide = { running: true, focusEditor() {}, term: { write(text) { calls.output = text; }, focus() { calls.focus++; } },
    setStatus(text) { calls.status.push(text); }, showIdeProblem(key, message) { calls.problems.push(message); }, trace: { update() {}, finished() {} } };
  const controller = new context.window.ExecutionController(ide, api);
  controller.sessionId = 'session';
  const event = (name, payload) => controller.event({ sessionId: 'session', event: name, payload });
  const request = id => event('input.request', { inputRequestId: id, prompt: 'Имя: ', target: 'console', outputText: 'До' });
  request('first'); controller.inputText = 'Анн'; calls.focus = 0;
  return { controller, ide, api, calls, pending, event, request };
}

for (const rejected of [false, true]) {
  const mode = rejected ? 'исключение' : 'ответ';
  const fail = p => rejected ? p.reject(new Error('Ввод не доставлен')) : p.resolve({ ok: false, error: { message: 'Ввод не доставлен' } });
  test(`Текущий INPUT восстанавливается для редактирования и повтора: ${mode}`, async () => {
    const { controller, ide, api, calls, pending, event } = setup();
    const operation = controller.input('\r');
    await controller.input('лишний повтор\r');
    assert.equal(calls.submitted.length, 1);
    fail(pending[0]); await assert.doesNotReject(operation);
    assert.equal(ide.running, true);
    assert.equal(controller.sessionId, 'session');
    assert.equal(controller.inputRequestId, 'first');
    assert.equal(controller.inputText, 'Анн');
    assert.equal(controller.inputPrompt, 'Имя: ');
    assert.equal(controller.inputTarget, 'console');
    assert.match(calls.output, /До\r\nИмя: Анн$/);
    assert.equal(calls.focus, 1);
    assert.equal(calls.status.at(-1), 'Ввод не доставлен');
    assert.deepEqual(calls.problems, []);
    api.submitInput = async (session, id, text) => {
      assert.equal(text, 'Анна'); event('input.completed', { inputRequestId: id }); return { ok: true };
    };
    await controller.input('а\r');
    assert.equal(controller.inputRequestId, null);
    assert.equal(controller.inputText, '');
  });

  test(`Подтверждённый INPUT не возвращается после позднего отказа: ${mode}`, async () => {
    const { controller, ide, calls, pending, event } = setup();
    const operation = controller.input('\r');
    event('input.completed', { inputRequestId: 'first' }); calls.status.length = 0;
    fail(pending[0]); await assert.doesNotReject(operation);
    assert.equal(ide.running, true);
    assert.equal(controller.inputRequestId, null);
    assert.equal(controller.inputText, '');
    assert.deepEqual(calls.status, []);
    assert.deepEqual(calls.problems, []);
  });

  for (const completed of [false, true]) {
    test(`Поздний INPUT не меняет следующий ${completed ? 'завершённый' : 'активный'} запрос: ${mode}`, async () => {
      const { controller, ide, calls, pending, event, request } = setup();
      const operation = controller.input('\r'); request('second'); controller.inputText = 'Новый';
      if (completed) event('input.completed', { inputRequestId: 'second' });
      calls.status.length = 0;
      fail(pending[0]); await assert.doesNotReject(operation);
      assert.equal(ide.running, true);
      assert.equal(controller.inputRequestId, completed ? null : 'second');
      assert.equal(controller.inputText, completed ? '' : 'Новый');
      assert.deepEqual(calls.status, []);
    });
  }

  test(`Поздний INPUT не меняет новую сессию: ${mode}`, async () => {
    const { controller, ide, calls, pending } = setup();
    const operation = controller.input('\r');
    Object.assign(controller, { sessionId: 'new-session', inputRequestId: 'new-input', inputText: 'Новый' });
    calls.status.length = 0;
    fail(pending[0]); await assert.doesNotReject(operation);
    assert.equal(ide.running, true);
    assert.equal(controller.inputRequestId, 'new-input');
    assert.equal(controller.inputText, 'Новый');
    assert.deepEqual(calls.status, []);
  });

  test(`Завершение программы не отменяется поздним INPUT: ${mode}`, async () => {
    const { controller, ide, calls, pending, event } = setup();
    const operation = controller.input('\r');
    event('execution.finished', { status: 'Succeeded', outputText: '' }); calls.status.length = 0;
    fail(pending[0]); await assert.doesNotReject(operation);
    assert.equal(ide.running, false);
    assert.equal(controller.sessionId, null);
    assert.equal(controller.inputRequestId, null);
    assert.deepEqual(calls.status, []);
  });
}

test('Авария аннулирует отправку даже при повторном идентификаторе сессии', async () => {
  const { controller, ide, calls, pending } = setup();
  const operation = controller.input('\r'); controller.fault('Авария службы');
  controller.sessionId = 'session'; ide.running = true; calls.status.length = 0;
  pending[0].resolve({ ok: false, error: { message: 'Прежний ввод' } }); await operation;
  assert.equal(ide.running, true);
  assert.equal(controller.inputRequestId, null);
  assert.deepEqual(calls.status, []);
});
