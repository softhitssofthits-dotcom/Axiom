const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { RecoveryStore } = require('../src/main/recovery-store');
const tab = text => ({ path: 'untitled1.axm', content: text, savedText: '', isDirty: true, isActive: true });
function fixture() {
  const base = path.resolve(__dirname, '../../.artifacts/recovery-tests'); fs.mkdirSync(base, { recursive: true });
  const directory = fs.mkdtempSync(path.join(base, 'journal-'));
  return { directory, store: new RecoveryStore(directory) };
}
test('Журнал сохраняет последнюю правку, проверяет целостность и откатывается к целому слоту', () => {
  const { directory, store } = fixture();
  try {
    store.save([tab('Первая правка')]); store.save([tab('Последняя правка')]);
    assert.equal(new RecoveryStore(directory).load().tabs[0].content, 'Последняя правка');
    fs.writeFileSync(path.join(directory, 'documents-recovery-0.json'), '{обрыв');
    assert.equal(new RecoveryStore(directory).load().tabs[0].content, 'Первая правка');
    store.save([tab('Исправленный снимок')]);
    assert.equal(store.load().tabs[0].content, 'Исправленный снимок');
    const slot = path.join(directory, 'documents-recovery-0.json');
    const data = JSON.parse(fs.readFileSync(slot, 'utf8')); data.tabs[0].content = 'Подмена';
    fs.writeFileSync(slot, JSON.stringify(data));
    assert.equal(store.load().tabs[0].content, 'Первая правка');
  } finally { fs.rmSync(directory, { recursive: true, force: true }); }
});
test('Отказ записи сохраняет предыдущий снимок; удаление всех вкладок не воскрешает их', () => {
  const { directory, store } = fixture();
  try {
    store.save([tab('Сохранённый буфер')]);
    fs.mkdirSync(path.join(directory, 'documents-recovery-0.json.tmp'));
    assert.throws(() => store.save([tab('Не записанный буфер')]));
    assert.equal(store.load().tabs[0].content, 'Сохранённый буфер');
    fs.rmdirSync(path.join(directory, 'documents-recovery-0.json.tmp'));
    store.save([]); assert.deepEqual(store.load().tabs, []);
    const latest = store.load();
    fs.writeFileSync(path.join(directory, 'documents-recovery-' + latest.sequence % 2 + '.json'), '{обрыв');
    assert.deepEqual(store.load().tabs, []);
    assert.throws(() => store.save([tab('a'), tab('b')]));
    assert.deepEqual(store.load().tabs, []);
  } finally { fs.rmSync(directory, { recursive: true, force: true }); }
});
