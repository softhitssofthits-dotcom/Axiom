const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createProjectFile, sameSourceText } = require('../src/main/file-service');
const { writeDocumentFile: saveWithAttempt } = require('../src/main/file-service');
const { BackendClient } = require('../src/main/backend-client');
const { resolveHostPath } = require('../src/main/host-ensure');
async function writeDocumentFile(...args) {
  const client = new BackendClient({ executable: resolveHostPath({}, path.resolve(__dirname, '../..')).exe });
  try { await client.ready(); return await saveWithAttempt(...args, payload => client.request('file.save', payload)); }
  finally { await client.close(); }
}
function scratchWorkspace(prefix) {
  const base = path.resolve(__dirname, '../../.artifacts');
  fs.mkdirSync(base, { recursive: true });
  return fs.mkdtempSync(path.join(base, prefix));
}
test('Сохранение обнаруживает внешний текст до уведомления наблюдателя и отмена сохраняет его', async () => {
  const root = scratchWorkspace('save-conflict-');
  try {
    const file = path.join(root, 'файл.axm'); fs.writeFileSync(file, 'внешний');
    let prompts = 0;
    assert.equal(await writeDocumentFile(file, 'правка', 'исходный', async () => { prompts++; return false; }), null);
    assert.equal(prompts, 1); assert.equal(fs.readFileSync(file, 'utf8'), 'внешний');
    assert.equal(await writeDocumentFile(file, 'правка', 'исходный', async () => true), true);
    assert.equal(fs.readFileSync(file, 'utf8'), 'правка');
  } finally { fs.rmSync(root, { recursive: true, force: true }); }
});
test('Новая внешняя запись во время подтверждения требует нового решения', async () => {
  const root = scratchWorkspace('save-dialog-');
  try {
    const file = path.join(root, 'файл.axm'); fs.writeFileSync(file, 'первая версия'); let prompts = 0;
    assert.equal(await writeDocumentFile(file, 'правка', 'исходный', async () => {
      if (++prompts === 1) { fs.writeFileSync(file, 'вторая версия'); return true; }
      return false;
    }), null);
    assert.equal(prompts, 2); assert.equal(fs.readFileSync(file, 'utf8'), 'вторая версия');
    fs.unlinkSync(file);
    assert.equal(await writeDocumentFile(file, 'восстановленный', 'исходный', async missing => { assert.equal(missing, true); return false; }), null);
    assert.equal(fs.existsSync(file), false);
    assert.equal(await writeDocumentFile(file, 'восстановленный', null, async () => { throw new Error('Повторное подтверждение не требуется'); }), true);
    assert.equal(fs.readFileSync(file, 'utf8'), 'восстановленный');
  } finally { fs.rmSync(root, { recursive: true, force: true }); }
});
test('Снимок редактора совпадает с файлом при смешанных окончаниях строк, но обнаруживает изменение программы', () => {
  assert.equal(sameSourceText('\uFEFFDIM value = 1\r\nPRINT value\n', 'DIM value = 1\r\nPRINT value\r\n'), true);
  assert.equal(sameSourceText('PRINT 1\r\n', 'PRINT 2\n'), false);
  assert.equal(sameSourceText('PRINT "a b"\n', 'PRINT "ab"\n'), false);
  assert.equal(sameSourceText('PRINT 1\n', 'PRINT 1\n\n'), false);
});
test('Создание файла сохраняет русский текст и отказывается перезаписывать существующий файл', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'axiom-create-'));
  try {
    const file = path.join(root, 'Программа.axm');
    createProjectFile(root, file, 'PRINT "Привет"\n');
    assert.equal(fs.readFileSync(file, 'utf8'), 'PRINT "Привет"\n');
    assert.throws(() => createProjectFile(root, file, 'замена'), /существует/);
    assert.equal(fs.readFileSync(file, 'utf8'), 'PRINT "Привет"\n');
    assert.throws(() => createProjectFile(root, path.join(root, '..', 'за_границей.axm'), ''), /проекта/);
  } finally { fs.rmSync(root, { recursive: true, force: true }); }
});
test('Отсутствующий каталог и неверный тип не создают файл', () => {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'axiom-create-'));
  try {
    assert.throws(() => createProjectFile(root, path.join(root, 'нет', 'a.axm'), ''));
    assert.throws(() => createProjectFile(root, path.join(root, 'a.axm'), null));
    assert.equal(fs.readdirSync(root).length, 0);
  } finally { fs.rmSync(root, { recursive: true, force: true }); }
});
