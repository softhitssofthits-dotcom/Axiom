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

function styleFixture() {
  const { DocumentStore } = require('../src/renderer/documents');
  const { DocumentSaver } = require('../src/renderer/document-save');
  const { ide, calls } = setup();
  const store = new DocumentStore(), disk = new Map(), writes = [];
  const api = { writeFile: async (path, text, expected) => {
    assert.equal(disk.get(path), expected); writes.push({ path, text }); disk.set(path, text); return true;
  } };
  const saver = new DocumentSaver(store, api);
  Object.assign(ide, { openFiles: store, editor: { getModel: () => null },
    saveSettings: patch => Object.assign(ide.settings, patch), updateFormatMenuChecks() {},
    save: async path => !!await saver.save(path) && !store.isDirty(path),
    lineDiffEdits: (model, text) => model.getValue() === text ? [] : [{ text }],
    documentServices: { request: async (path, _method, options) => ({ sourceText:
      options.formattingStyle === 'Modern' ? store.get(path).model.getValue().replace(/print/ig, 'Print') : store.get(path).model.getValue().replace(/print/ig, 'PRINT') }) }
  });
  function add(name, text = 'PRINT 1\n', options = {}) {
    const path = name.startsWith('untitled') ? name : 'C:\\project\\' + name;
    let value = text; const listeners = [];
    const model = { getValue: () => value, getLanguageId: () => 'axiom', isDisposed: () => false,
      onDidChangeContent: callback => { listeners.push(callback); return { dispose() {} }; },
      pushStackElement() {}, edit: text => { value = text; listeners.forEach(callback => callback()); },
      pushEditOperations: (_selection, edits) => model.edit(edits[0].text) };
    store.set(path, { model, ...options }); disk.set(path, text); return { path, model, entry: store.get(path) };
  }
  return { ide, store, disk, writes, api, add, calls };
}

test('Смена стиля сохраняет чистые фоновые вкладки, прежние правки и черновики не записывает', async () => {
  const { ide, store, add, disk, writes } = styleFixture();
  const clean = add('clean.axm'), dirty = add('dirty.axm'), draft = add('untitled1.axm'), readonly = add('readonly.axm', 'PRINT 4\n', { readonly: true });
  dirty.model.edit('PRINT 2\n');
  await ide.setFormattingStyle('Modern');
  assert.equal(clean.model.getValue(), 'Print 1\n'); assert.equal(store.isDirty(clean.path), false);
  assert.equal(disk.get(clean.path), 'Print 1\n'); assert.equal(writes.length, 1);
  assert.equal(dirty.model.getValue(), 'Print 2\n'); assert.equal(store.isDirty(dirty.path), true);
  assert.equal(disk.get(dirty.path), 'PRINT 1\n'); assert.equal(draft.model.getValue(), 'Print 1\n');
  assert.equal(readonly.model.getValue(), 'PRINT 4\n');
});

test('Ручное форматирование не сохраняет документ автоматически', async () => {
  const { ide, add, writes, store } = styleFixture(); const file = add('manual.axm', 'print 1\n');
  await ide.formatWholeDocument('manual', file.path);
  assert.equal(writes.length, 0); assert.equal(store.isDirty(file.path), true);
});

test('Правки во время записи форматирования остаются несохранёнными', async () => {
  const { ide, add, api, disk, store } = styleFixture(); const file = add('edit.axm');
  api.writeFile = async (path, text) => { disk.set(path, text); file.model.edit('Print 99\n'); return true; };
  await ide.setFormattingStyle('Modern');
  assert.equal(disk.get(file.path), 'Print 1\n'); assert.equal(file.model.getValue(), 'Print 99\n');
  assert.equal(store.isDirty(file.path), true); assert.equal(file.entry.savedText, 'Print 1\n');
});

test('Отказ записи после смены стиля не снимает признак изменения', async () => {
  const { ide, add, api, disk, store } = styleFixture(); const file = add('failure.axm');
  api.writeFile = async () => false;
  await ide.setFormattingStyle('Modern');
  assert.equal(file.model.getValue(), 'Print 1\n'); assert.equal(store.isDirty(file.path), true);
  assert.equal(disk.get(file.path), 'PRINT 1\n');
});

for (const change of ['edit', 'close', 'rename']) test('Ответ форматирования не сохраняет устаревший документ: ' + change, async () => {
  const { ide, add, store, writes } = styleFixture(); const file = add('stale.axm'); let finish;
  ide.documentServices.request = () => new Promise(resolve => { finish = resolve; });
  const pending = ide.formatWholeDocument('style', file.path, 'document.format', true);
  if (change === 'edit') file.model.edit('PRINT 55\n');
  if (change === 'close') { store.delete(file.path); add('stale.axm', 'PRINT 66\n'); }
  if (change === 'rename') store.rename(file.path, 'C:\\project\\renamed.axm');
  finish({ sourceText: 'Print 1\n' }); await pending;
  assert.equal(writes.length, 0);
  assert.notEqual(store.get(file.path)?.model.getValue(), 'Print 1\n');
});

test('Повторный выбор стиля дожидается записи и сохраняет последний стиль', async () => {
  const { ide, add, api, disk, store } = styleFixture(); const file = add('rapid.axm');
  const original = api.writeFile; let release;
  api.writeFile = async (...args) => { await new Promise(resolve => { release = resolve; }); return original(...args); };
  const first = ide.setFormattingStyle('Modern');
  while (!release) await Promise.resolve();
  const last = ide.setFormattingStyle('Classic');
  const finish = release; api.writeFile = original; finish(); await Promise.all([first, last]);
  assert.equal(file.model.getValue(), 'PRINT 1\n'); assert.equal(disk.get(file.path), 'PRINT 1\n');
  assert.equal(store.isDirty(file.path), false);
});
