const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
function setup() {
  const context = { window: {}, clearTimeout() {} };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8').replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  let text = 'print 1', resolve, reject;
  const calls = { problems: [], edits: 0 };
  const model = { getLanguageId: () => 'axiom', getValue: () => text, isDisposed: () => false,
    pushStackElement() {}, pushEditOperations() { calls.edits++; text = 'PRINT 1'; } };
  const entry = { model };
  Object.assign(ide, { activeFile: 'a.axm', openFiles: new Map([['a.axm', entry]]), settings: {}, _formatBusy: 0, _serviceSeq: 0,
    editor: { getModel: () => model, getSelection: () => null },
    documentServices: { request: () => new Promise((yes, no) => { resolve = yes; reject = no; }) },
    log() {}, showIdeProblem: (...args) => calls.problems.push(args),
    scheduleLiveCheck() {}, scheduleOutlineRebuild() {}, lineDiffEdits: () => [{}] });
  return { ide, model, calls, fail: () => reject(new Error('Отказ форматирования')), finish: () => resolve({ sourceText: 'PRINT 1' }) };
}
for (const change of ['switch', 'run', 'request', 'setting']) {
  test('Прежний отказ форматирования не меняет интерфейс: ' + change, async () => {
    const { ide, calls, fail } = setup();
    const pending = ide.formatWholeDocument('test');
    if (change === 'switch') ide.activeFile = 'b.axm';
    if (change === 'run') { ide.running = true; ide._runFile = 'a.axm'; }
    if (change === 'request') ide._serviceSeq++;
    if (change === 'setting') ide.settings.smartIdentifierEnabled = true;
    fail(); await pending;
    assert.deepEqual(calls.problems, []);
    assert.equal(ide._formatBusy, 0);
  });
}
test('Текущий отказ виден, занятость снята и повтор доступен', async () => {
  const { ide, calls, fail, finish } = setup();
  let pending = ide.formatWholeDocument('test'); fail();
  assert.equal(await pending, false); assert.equal(calls.problems.length, 1);
  assert.equal(ide._formatBusy, 0);
  pending = ide.formatWholeDocument('retry'); finish();
  assert.equal(await pending, true); assert.equal(calls.edits, 1);
});
test('Успешный ответ не меняет ставшую исполняемой модель', async () => {
  const { ide, calls, finish } = setup();
  const pending = ide.formatWholeDocument('test'); ide.running = true; ide._runFile = 'a.axm';
  finish(); await pending; assert.equal(calls.edits, 0); assert.equal(ide._formatBusy, 0);
});
test('Успешное форматирование фоновой модели остаётся допустимым', async () => {
  const { ide, calls, finish } = setup();
  const pending = ide.formatWholeDocument('test'); ide.activeFile = 'b.axm';
  finish(); assert.equal(await pending, true); assert.equal(calls.edits, 1);
});
test('Сбой применения действующей правки не скрывается после частичного изменения модели', async () => {
  const { ide, model, calls, finish } = setup();
  model.pushEditOperations = () => { model.getValue = () => 'PRINT 1'; throw new Error('Не удалось применить правку'); };
  const pending = ide.formatWholeDocument('apply-error'); finish();
  assert.equal(await pending, false);
  assert.equal(calls.problems.length, 1);
  assert.equal(ide._formatBusy, 0);
  assert.equal(ide._autoFormatting, false);
});
