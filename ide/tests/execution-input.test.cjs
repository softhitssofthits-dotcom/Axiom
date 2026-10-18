const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, I18N: { t: key => key, setText: () => {} }, performance: { now: () => 0 } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
  const calls = { focus: 0, submitted: [], output: '' };
  const ide = { running: true, term: { focus() { calls.focus++; }, write(text) { calls.output = text; } },
    setStatus() {}, trace: { update() {}, clearMarker() {} } };
  const api = { onBackendEvent() {}, onBackendFault() {}, acknowledgeOutput() {},
    async submitInput(...args) { calls.submitted.push(args); return { ok: true }; } };
  const controller = new context.window.ExecutionController(ide, api);
  controller.sessionId = 'session';
  const event = (name, payload, sessionId = 'session') => controller.event({ event: name, payload, sessionId });
  const request = (id, target = 'screen') => event('input.request', { inputRequestId: id, target, prompt: 'Ответ: ', outputText: '' });
  return { controller, calls, ide, api, event, request };
}

test('Графический INPUT не переводит фокус в терминал IDE', () => {
  const { request, calls } = setup();
  request('1');
  assert.equal(calls.focus, 0);
});

test('Обычный консольный INPUT сохраняет фокус терминала', () => {
  const { request, calls } = setup();
  request('1', 'console');
  assert.equal(calls.focus, 1);
});

test('Ранний запрос SCREEN в очереди запуска не получает поздний фокус терминала', async () => {
  const { controller, ide, api, calls, request } = setup();
  Object.assign(ide, { clearDiagnostics() {}, showPanel() {}, switchPanel() {}, ensureConsole: () => true,
    openFiles: new Map([['file', { documentId: 'document', revision: 1, model: { getValue: () => 'SCREEN 1' } }]]) });
  ide.term.reset = () => {};
  api.startExecution = async () => { request('1'); return { ok: true, payload: { sessionId: 'session' } }; };
  await controller.start('file');
  assert.equal(controller.inputRequestId, '1');
  assert.equal(calls.focus, 0);
});

test('Нативное завершение закрывает запрос и не допускает отправку прежнего ответа', async () => {
  const { request, event, controller, calls, ide } = setup();
  request('1');
  event('input.completed', { inputRequestId: '1' });
  event('output.append', { text: 'Программа продолжает работу', outputVersion: '1' });
  assert.equal(controller.inputRequestId, null);
  assert.equal(controller.inputPrompt, '');
  assert.doesNotMatch(calls.output, /Ответ:/);
  await controller.input('ошибочный повтор\r');
  assert.equal(calls.submitted.length, 0);
  assert.equal(ide.running, true);
});

test('Запоздалое завершение другого запроса или сессии сохраняет новый INPUT', () => {
  const { request, event, controller } = setup();
  request('2'); controller.inputText = 'Тест';
  event('input.completed', { inputRequestId: '1' });
  event('input.completed', { inputRequestId: '2' }, 'old-session');
  assert.equal(controller.inputRequestId, '2');
  assert.equal(controller.inputText, 'Тест');
});

test('Завершение предыдущего консольного ответа не закрывает повторный INPUT', async () => {
  const { request, event, controller, calls } = setup();
  request('1', 'console');
  await controller.input('42\r');
  request('2', 'console');
  event('input.completed', { inputRequestId: '1' });
  assert.equal(controller.inputRequestId, '2');
  assert.equal(calls.submitted.length, 1);
});
