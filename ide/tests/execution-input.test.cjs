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
  api.startExecution = async () => { calls.focus = 0; request('1'); return { ok: true, payload: { sessionId: 'session' } }; };
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

test('INPUT сохраняет одну и две начальные пустые строки из полного снимка', () => {
  for (const count of [1, 2]) {
    const { event, calls } = setup();
    event('input.request', { inputRequestId: 'blank', target: 'console', prompt: 'Ответ: ',
      outputText: '\r\n'.repeat(count - 1), outputLineCount: count });
    assert.equal(calls.output, '\x1bc' + '\r\n'.repeat(count) + 'Ответ: ');
  }
});

test('Очистка единственной пустой строки сохраняется после полного снимка ответа', () => {
  const { event, calls, controller } = setup();
  event('input.request', { inputRequestId: 'blank', target: 'console', prompt: 'Ответ: ', outputText: '', outputLineCount: 1 });
  controller.clearConsole();
  event('input.request', { inputRequestId: 'next', target: 'console', prompt: 'Следующий: ', outputText: '\r\nОтвет: да', outputLineCount: 2 });
  assert.equal(calls.output, '\x1bcОтвет: да\r\nСледующий: ');
});

test('Замена опубликованного вывода обновляет число строк перед активным INPUT', () => {
  for (const count of [0, 1]) {
    const { event, calls, controller } = setup();
    event('input.request', { inputRequestId: 'next', prompt: 'Ответ: ', outputText: 'История', outputLineCount: 1 });
    controller.inputText = '16';
    event('output.replace', { text: '', outputVersion: '1', outputLineCount: count });
    assert.equal(controller.outputLineCount, count);
    assert.equal(calls.output, '\x1bc' + '\r\n'.repeat(count) + 'Ответ: 16');
  }
});

test('Запоздалое добавление не меняет опережающий снимок INPUT, новый поток обновляет число строк', () => {
  const { event, controller } = setup();
  event('input.request', { inputRequestId: 'next', prompt: 'Ответ: ', outputText: 'Первая\nВторая', outputLineCount: 2 });
  event('output.append', { text: 'Первая', outputVersion: '1', outputLineCount: 1 });
  assert.equal(controller.outputText, 'Первая\nВторая');
  assert.equal(controller.outputLineCount, 2);
  event('output.append', { text: '\nВторая\nТретья', outputVersion: '2', outputLineCount: 3 });
  assert.equal(controller.outputLineCount, 3);
});

test('Запрос SCREEN не дублирует приглашение в консоли IDE', () => {
  const { request, calls } = setup();
  request('1', 'screen');
  assert.doesNotMatch(calls.output, /Ответ:/);
});

test('Консольный запрос по-прежнему показывает приглашение', () => {
  const { request, calls } = setup();
  request('1', 'console');
  assert.match(calls.output, /Ответ:/);
});

test('Терминал не отправляет ответ, пока ввод принадлежит окну SCREEN', async () => {
  const { request, controller, calls } = setup();
  request('1', 'screen');
  await controller.input('чужой ответ\r');
  assert.equal(calls.submitted.length, 0);
  assert.equal(controller.inputRequestId, '1');
  assert.equal(controller.inputText, '');
});

test('После нативного завершения запроса SCREEN терминал снова работает', async () => {
  const { request, event, controller, calls } = setup();
  request('1', 'screen');
  await controller.input('игнор\r');
  event('input.completed', { inputRequestId: '1' });
  request('2', 'console');
  await controller.input('42\r');
  assert.equal(calls.submitted.length, 1);
  assert.equal(calls.submitted[0][2], '42');
});