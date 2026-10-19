const { test } = require('node:test');
const assert = require('node:assert/strict');
const { DocumentStore } = require('../src/renderer/documents');
function model(text = '') {
  let value = text, listeners = [], disposed = false;
  return { getValue: () => value, onDidChangeContent: cb => { listeners.push(cb); return { dispose: () => { listeners = listeners.filter(x => x !== cb); } }; },
    edit: text => { value = text; listeners.forEach(cb => cb()); }, dispose: () => { disposed = true; }, isDisposed: () => disposed };
}
test('Путь Windows не создаёт второй документ при другом регистре', () => {
  const store = new DocumentStore(); store.set('C:\\Программы\\Файл.axm', { model: model('PRINT 1') });
  assert.equal(store.has('c:/программы/файл.axm'), true); assert.equal(store.size, 1);
});
test('SaveAs сохраняет идентификатор, модель, состояние и точки', () => {
  const store = new DocumentStore(); const editor = model('PRINT 1');
  store.set('untitled1', { model: editor, viewState: { caret: 5 } });
  const before = store.get('untitled1'); before.breakpoints.add(3); editor.edit('PRINT 2');
  store.rename('untitled1', 'C:\\новый.axm'); const after = store.get('C:\\новый.axm');
  assert.equal(after.documentId, before.documentId); assert.equal(after.model, editor);
  assert.deepEqual(after.viewState, { caret: 5 }); assert.deepEqual([...after.breakpoints], [3]);
  assert.equal(after.revision, 1); assert.equal(store.isDirty('C:\\новый.axm'), true);
  store.markSaved('C:\\новый.axm'); assert.equal(store.isDirty('C:\\новый.axm'), false);
});
test('Возврат к сохранённому тексту снимает dirty; ошибочное переименование сохраняет обе модели', () => {
  const store = new DocumentStore(); const a = model('PRINT 1'); const b = model('PRINT 2');
  store.set('C:\\a.axm', { model: a }); store.set('C:\\b.axm', { model: b });
  a.edit('PRINT 3'); assert.equal(store.isDirty('C:\\a.axm'), true);
  a.edit('PRINT 1'); assert.equal(store.isDirty('C:\\a.axm'), false);
  assert.throws(() => store.rename('C:\\a.axm', 'C:\\b.axm'));
  assert.equal(store.get('C:\\a.axm').model, a); assert.equal(store.get('C:\\b.axm').model, b);
});
test('Удаление освобождает подписку, отмена до удаления сохраняет документ', () => {
  const store = new DocumentStore(); const a = model('PRINT 1'); store.set('a', { model: a });
  const document = store.get('a'); assert.equal(store.size, 1);
  store.delete('a'); a.edit('PRINT 2'); assert.equal(document.revision, 0); assert.equal(store.size, 0);
});
test('Исчезнувший неизменённый файл требует сохранения даже после возврата исходного текста', () => {
  const store = new DocumentStore(); const editor = model('PRINT 1'); store.set('a.axm', { model: editor });
  store.get('a.axm').missing = true;
  assert.equal(store.isDirty('a.axm'), true);
  editor.edit('PRINT 2'); editor.edit('PRINT 1'); assert.equal(store.isDirty('a.axm'), true);
  store.markSaved('a.axm'); assert.equal(store.isDirty('a.axm'), false);
});

test('Закрытие точки входа снимает выбор до уведомления; закрытие зависимости его сохраняет', () => {
  let observed;
  const store = new DocumentStore(() => { observed = store.navigationEntryId; });
  store.set('main.axm', { model: model('IMPORT helper') });
  store.set('helper.axm', { model: model('PRINT 1') });
  const entryId = store.get('main.axm').documentId;
  store.navigationEntryId = entryId;
  store.delete('helper.axm'); assert.equal(observed, entryId);
  store.delete('main.axm'); assert.equal(observed, null);
  store.set('main.axm', { model: model('PRINT 2') });
  assert.equal(store.navigationEntryId, null);
});

test('Выбранная точка входа переживает переименование и смену идентичности при переполнении редакции', () => {
  const store = new DocumentStore(), editor = model('PRINT 1');
  store.set('main.axm', { model: editor });
  store.navigationEntryId = store.get('main.axm').documentId;
  store.rename('main.axm', 'renamed.axm');
  const entry = store.get('renamed.axm');
  assert.equal(store.navigationEntryId, entry.documentId);
  entry.revision = 2147483647; editor.edit('PRINT 2');
  assert.equal(store.navigationEntryId, entry.documentId);
});
