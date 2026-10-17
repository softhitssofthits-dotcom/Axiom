const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
function setup() {
  const context = { window: {} }; vm.createContext(context);
  vm.runInContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8').replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype), pending = [], problems = [];
  let text = 'if 1 then\n', offset = 10;
  const model = { getLanguageId: () => 'axiom', getValue: () => text, getOffsetAt: () => offset,
    getLineContent: () => '', isDisposed: () => false };
  Object.assign(ide, { activeFile: 'a.axm', openFiles: new Map([['a.axm', { model }]]),
    settings: { editorPreferences: { autoIndent: 'advanced' } },
    editor: { getModel: () => model, getContribution: () => null, getPosition: () => ({ lineNumber: 2, column: 1 }) },
    documentServices: { request: () => new Promise((resolve, reject) => pending.push({ resolve, reject })) },
    editedLines: { consume() {} }, showIdeProblem: (...args) => problems.push(args) });
  return { ide, model, pending, problems, edit: () => text = 'PRINT 1\n', move: () => offset++ };
}
for (const change of ['switch', 'run', 'smart', 'style', 'indent', 'caret', 'edit', 'close', 'newer']) {
  test('Прежний отказ структурного редактирования отклонён: ' + change, async () => {
    const s = setup(), { ide, pending, problems } = s;
    const operation = ide.editLineTransition(s.model, 1, { lineNumber: 2, column: 1 });
    if (change === 'switch') ide.activeFile = 'b.axm';
    if (change === 'run') { ide.running = true; ide._runFile = 'a.axm'; }
    if (change === 'smart') ide.settings.smartIdentifierEnabled = true;
    if (change === 'style') ide.settings.formattingStyle = 'Modern';
    if (change === 'indent') ide.settings.editorPreferences.autoIndent = 'none';
    if (change === 'caret') s.move();
    if (change === 'edit') s.edit();
    if (change === 'close') ide.openFiles.delete('a.axm');
    if (change === 'newer') {
      const next = ide.editLineTransition(s.model, 1, { lineNumber: 2, column: 1 });
      pending[1].resolve({ edits: [] }); await next;
    }
    pending[0].reject(new Error('Прежний отказ')); await operation;
    assert.deepEqual(problems, []);
  });
}
test('Текущий отказ виден, включая отсутствие объекта ошибки; повтор доступен', async () => {
  const { ide, model, pending, problems } = setup();
  let operation = ide.editLineTransition(model, 1, { lineNumber: 2, column: 1 });
  pending[0].reject(null); await operation;
  assert.equal(problems.length, 1); assert.equal(problems[0][1], 'null');
  operation = ide.editLineTransition(model, 1, { lineNumber: 2, column: 1 });
  pending[1].resolve({ edits: [] }); await operation;
  assert.equal(problems.length, 1);
});
for (const setting of ['style', 'indent']) {
  test('Успешный ответ прежней настройки не применяется: ' + setting, async () => {
    const { ide, model, pending } = setup();
    let consumed = 0; ide.editedLines.consume = () => consumed++;
    const operation = ide.editLineTransition(model, 1, { lineNumber: 2, column: 1 });
    if (setting === 'style') ide.settings.formattingStyle = 'Modern';
    else ide.settings.editorPreferences.autoIndent = 'none';
    pending[0].resolve({ edits: [] }); await operation;
    assert.equal(consumed, 0);
  });
}
test('Сбой применения после частичной правки остаётся видимым', async () => {
  const { ide, model, pending, problems, edit } = setup();
  model.getValueLength = () => 10; model.getPositionAt = () => ({ lineNumber: 1, column: 1 });
  ide.monaco = { Range: class {} }; ide.editor.pushUndoStop = () => {};
  ide.editor.executeEdits = () => { edit(); throw new Error('Сбой применения'); };
  const operation = ide.editLineTransition(model, 1, { lineNumber: 2, column: 1 });
  pending[0].resolve({ edits: [{ start: 0, length: 1, newText: 'I' }] }); await operation;
  assert.equal(problems.length, 1); assert.equal(problems[0][1], 'Сбой применения');
  assert.equal(ide._autoFormatting, false);
});
