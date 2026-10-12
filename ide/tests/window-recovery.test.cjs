const { test } = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const { attachWindowRecovery } = require('../src/main/window-recovery');

function fixture() {
  const window = new EventEmitter();
  window.webContents = new EventEmitter();
  window.webContents.executeJavaScript = async () => true;
  const calls = [];
  window.isDestroyed = () => false;
  window.destroy = () => { calls.push('destroy'); window.emit('closed'); };
  let answer;
  const api = attachWindowRecovery({ window,
    app: { getPath: () => '.artifacts/nonexistent-recovery-test', quit: () => calls.push('quit') },
    dialog: { showMessageBox: (_w, options) => { calls.push(options); return new Promise(resolve => { answer = resolve; }); } },
    log: { warn() {}, info() {}, error() {} },
    stop: async () => { calls.push('stop'); },
    replace: () => calls.push('replace'), saveState: () => calls.push('save'),
  });
  return { window, calls, api, answer: response => answer({ response }) };
}
const settle = () => new Promise(resolve => setImmediate(resolve));

test('Ожидание не останавливает программу и не уничтожает документы', async () => {
  const f = fixture(); f.window.emit('unresponsive'); f.answer(1); await settle();
  assert.equal(f.calls.length,1);
  f.window.emit('closed');
});
test('Восстановление сначала останавливает VM, затем заменяет окно', async () => {
  const f = fixture(); f.window.emit('unresponsive'); f.window.emit('unresponsive');
  f.answer(0); await settle();
  assert.equal(f.calls.filter(c => typeof c === 'object').length,1);
  assert.deepEqual(f.calls.slice(1),['stop','save','replace','destroy']);
});
test('Возобновление ответа отменяет устаревшее решение диалога', async () => {
  const f = fixture(); f.window.emit('unresponsive'); f.window.emit('responsive'); f.answer(0); await settle();
  assert.equal(f.calls.length,1); f.window.emit('closed');
});
test('Выход освобождает VM и уничтожает окно без восстановления', async () => {
  const f = fixture(); f.window.emit('unresponsive'); f.answer(2); await settle();
  assert.deepEqual(f.calls.slice(1),['stop','save','destroy','quit']);
});
test('Закрытое окно не восстанавливается по позднему ответу', async () => {
  const f = fixture(); f.window.emit('unresponsive'); f.window.emit('closed'); f.answer(0); await settle();
  assert.equal(f.calls.length,1);
});
test('Авария во время диалога ожидания немедленно прекращает VM', async () => {
  const f = fixture(); f.window.emit('unresponsive');
  f.window.webContents.emit('render-process-gone',{}, { reason:'crashed' }); await settle();
  assert.ok(f.calls.includes('stop'));
  f.answer(0); await settle();
  assert.equal(f.calls.filter(c => typeof c === 'object').length,1);
});
test('Живой renderer с открытым диалогом сохранения не считается зависшим', async context => {
  context.mock.timers.enable({ apis: ['setTimeout'] });
  const f = fixture(); f.api.requestClose(); context.mock.timers.tick(5000); await settle();
  assert.equal(f.calls.length,0); f.window.emit('closed');
});
test('Отсутствие ответа на close даёт доступный диалог даже без unresponsive', async context => {
  context.mock.timers.enable({ apis: ['setTimeout'] });
  const f = fixture(); f.window.webContents.executeJavaScript = () => new Promise(() => {});
  f.api.requestClose(); context.mock.timers.tick(5000); context.mock.timers.tick(1000); await settle();
  assert.equal(f.calls.length,1); f.answer(2); await settle();
  assert.deepEqual(f.calls.slice(1),['stop','save','destroy','quit']);
});
