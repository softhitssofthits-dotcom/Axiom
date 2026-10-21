const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { DocumentStore } = require('../src/renderer/documents');

function setup() {
  const api = { unwatchFile: async () => {}, watchFile: async () => {}, addRecentFile: async () => [] };
  const context = { I18N:{t:(_key,value)=>value}, window: { axiom: api }, document: { getElementById: () => ({}), querySelectorAll: () => [] } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  const model = { getValue: () => 'PRINT 1', onDidChangeContent: () => ({ dispose() {} }) };
  const store = new DocumentStore(); store.set('a.axm', { model }); store.set('b.axm', { model: { ...model } });
  const snapshot = { documentId: store.get('a.axm').documentId, file: 'a.axm', sourceText: 'PRINT 1' };
  Object.assign(ide, { openFiles: store, activeFile: 'a.axm', _runFile: 'a.axm', running: true,
    execution: { sourceSnapshot: snapshot, sessionId: 'session' }, dirty: new Set(), settings: {},
    markDirty() {}, renderMiscellaneousFiles() {}, updateSaveButton() {}, rebuildOutline() {} });
  return { ide, store, snapshot, model };
}

for (const renamed of [false, true]) {
  test('Save As выполняемой записи обновляет путь без изменения снимка: ' + renamed, async () => {
    const { ide, store, snapshot, model } = setup();
    if (renamed) store.rename('a.axm', 'copy.axm');
    await ide.renameActive('copy.axm', 'a.axm');
    assert.equal(ide._runFile, 'copy.axm');
    assert.equal(store.get(ide._runFile).model, model);
    assert.equal(ide.execution.sourceSnapshot, snapshot);
    assert.equal(snapshot.file, 'a.axm');
    assert.equal(ide.execution.sessionId, 'session');
  });
}

test('Save As другого документа не переносит владельца сессии', async () => {
  const { ide, store } = setup();
  ide.activeFile = 'b.axm';
  await ide.renameActive('other.axm', 'b.axm');
  assert.equal(ide._runFile, 'a.axm');
  assert.equal(ide.activeFile, 'other.axm');
  assert.ok(store.get('a.axm'));
});

test('Несуществующий документ не меняет путь исполнения', async () => {
  const { ide } = setup();
  await ide.renameActive('copy.axm', 'missing.axm');
  assert.equal(ide._runFile, 'a.axm');
});

test('Отказ записи проекта не разрывает связь сохранённой вкладки с новым путём',async()=>{
 const {ide,store}=setup();ide.projectSession={renamed:async()=>{throw new Error('conflict');}};let failure=false;ide.showIdeProblem=()=>{failure=true;};
 // Реальный DocumentSaver уже перенёс запись перед renameActive.
 store.rename('a.axm','copy.axm');
 await ide.renameActive('copy.axm','a.axm');
 assert.equal(ide.activeFile,'copy.axm');assert.ok(store.has('copy.axm'));assert.equal(store.has('a.axm'),false);assert.equal(failure,true);
});
