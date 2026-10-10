const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createProjectFile } = require('../src/main/file-service');
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
