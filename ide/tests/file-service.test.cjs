const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { createProjectFile, sameSourceText } = require('../src/main/file-service');
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
