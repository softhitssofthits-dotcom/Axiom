const { test } = require('node:test');
const assert = require('node:assert/strict');
const { DocumentStore } = require('../src/renderer/documents');
const { DocumentSaver } = require('../src/renderer/document-save');
function fixture() {
  const store = new DocumentStore(); let text = 'старый';
  const model = { getValue: () => text, onDidChangeContent: () => ({ dispose() {} }), edit: v => text = v };
  store.set('untitled1', { model }); model.edit('новый');
  return { store, model };
}
test('Отмена выбора пути и ошибка записи сохраняют документ', async () => {
  const { store } = fixture(); let writes = 0;
  const saver = new DocumentSaver(store, { saveFileDialog: async () => null, writeFile: async () => { writes++; return false; } });
  assert.equal(await saver.save('untitled1'), null); assert.equal(writes, 0);
  saver.api.saveFileDialog = async () => 'C:\\a.axm';
  await assert.rejects(saver.save('untitled1')); assert.equal(store.has('untitled1'), true);
  assert.equal(store.isDirty('untitled1'), true);
});
test('Изменение во время записи остаётся несохранённым после Сохранить как', async () => {
  const { store, model } = fixture(); let written;
  const saver = new DocumentSaver(store, { saveFileDialog: async () => 'C:\\a.axm', writeFile: async (_p, text) => { written = text; model.edit('ещё новее'); return true; } });
  const result = await saver.save('untitled1');
  assert.equal(written, 'новый'); assert.equal(result.path, 'C:\\a.axm');
  assert.equal(store.isDirty(result.path), true); assert.equal(model.getValue(), 'ещё новее');
});
test('Сохранить как не перезаписывает файл другой открытой вкладки', async () => {
  const { store, model } = fixture(); store.set('C:\\a.axm', { model: { ...model } }); let writes = 0;
  const saver = new DocumentSaver(store, { saveFileDialog: async () => 'c:/A.axm', writeFile: async () => { writes++; return true; } });
  await assert.rejects(saver.save('untitled1')); assert.equal(writes, 0); assert.equal(store.size, 2);
});
test('Отмена конфликта не объявляет текст сохранённым и передаёт исходную версию файла', async () => {
  const { store, model } = fixture(); store.rename('untitled1', 'C:\\a.axm'); let expected;
  const saver = new DocumentSaver(store, { writeFile: async (_p, _text, baseline) => { expected = baseline; return null; } });
  assert.equal(await saver.save('C:\\a.axm'), null);
  assert.equal(expected, 'старый'); assert.equal(store.isDirty('C:\\a.axm'), true); assert.equal(model.getValue(), 'новый');
});
