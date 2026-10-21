const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createProject, projectCreationParent } = require('../src/main/project-create');

function workspace(t) {
  const base = path.resolve(__dirname, '../../.artifacts/project-create');
  fs.mkdirSync(base, { recursive: true });
  const root = fs.mkdtempSync(path.join(base, 'case-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  return root;
}
test('Создание проекта записывает только пустой main.axm в новую папку', t => {
  const root = workspace(t), result = createProject(root, 'Моя программа');
  assert.equal(result.path, path.join(fs.realpathSync(root), 'Моя программа'));
  assert.equal(result.main, path.join(result.path, 'main.axm'));
  assert.deepEqual(fs.readdirSync(result.path), ['main.axm']);
  assert.equal(fs.readFileSync(result.main, 'utf8'), '');
});
test('Недопустимые имена и родительские пути не создают файлов', t => {
  const root = workspace(t);
  for (const name of ['', '.', '..', '../escape', 'a/b', 'a\\b', 'a:b', 'a*', 'a?', 'a"', 'a<', 'a>', 'a|', 'a\0', 'a\n', 'end.', 'end ', 'CON', 'nul.txt', 'COM1.axm', 'LPT9', 'x'.repeat(256), null, {}])
    assert.throws(() => createProject(root, name));
  for (const parent of [null, {}, '', '.', path.join(root, 'missing')]) assert.throws(() => createProject(parent, 'new'));
  assert.deepEqual(fs.readdirSync(root), []);
});
test('Существующая папка и файл не изменяются', t => {
  const root = workspace(t), folder = path.join(root, 'existing'); fs.mkdirSync(folder);
  fs.writeFileSync(path.join(folder, 'main.axm'), 'KEEP'); fs.writeFileSync(path.join(root, 'file'), 'KEEP FILE');
  assert.throws(() => createProject(root, 'existing'));
  assert.throws(() => createProject(root, 'file'));
  assert.equal(fs.readFileSync(path.join(folder, 'main.axm'), 'utf8'), 'KEEP');
  assert.equal(fs.readFileSync(path.join(root, 'file'), 'utf8'), 'KEEP FILE');
});
test('Отказ записи удаляет лишь собственную пустую папку и сохраняет чужой файл', t => {
  const root = workspace(t), original = fs.writeFileSync;
  t.after(() => { fs.writeFileSync = original; });
  fs.writeFileSync = () => { throw new Error('Отказ записи'); };
  assert.throws(() => createProject(root, 'empty'), /Отказ записи/);
  assert.equal(fs.existsSync(path.join(root, 'empty')), false);
  fs.writeFileSync = (file) => { original(file, 'FOREIGN'); throw new Error('Конфликт'); };
  assert.throws(() => createProject(root, 'occupied'), /Конфликт/);
  assert.equal(fs.readFileSync(path.join(root, 'occupied', 'main.axm'), 'utf8'), 'FOREIGN');
});
test('Начальная папка учитывает проект, затем последнюю папку и безопасно пропускает повреждённые значения', t => {
  const root = workspace(t), project = path.join(root, 'project'), recent = path.join(root, 'recent');
  fs.mkdirSync(project); fs.mkdirSync(recent);
  assert.equal(projectCreationParent({ lastProjectDir: project, lastOpenDir: recent }, root), root);
  assert.equal(projectCreationParent({ lastProjectDir: {}, lastOpenDir: recent }, root), recent);
  assert.equal(projectCreationParent({ lastProjectDir: '\0', lastOpenDir: [] }, root), root);
});
test('Ссылка на родительскую папку разрешается до создания проекта', t => {
  const root = workspace(t), actual = path.join(root, 'actual'), link = path.join(root, 'link');
  fs.mkdirSync(actual); fs.symlinkSync(actual, link, process.platform === 'win32' ? 'junction' : 'dir');
  const result = createProject(link, 'new');
  assert.equal(result.path, path.join(fs.realpathSync(actual), 'new'));
  assert.equal(fs.readFileSync(result.main, 'utf8'), '');
});
test('Существующая ссылка не позволяет записать main.axm вне выбранной папки', t => {
  const root = workspace(t), actual = path.join(root, 'actual'), link = path.join(root, 'existing');
  fs.mkdirSync(actual); fs.symlinkSync(actual, link, process.platform === 'win32' ? 'junction' : 'dir');
  assert.throws(() => createProject(root, 'existing'));
  assert.deepEqual(fs.readdirSync(actual), []);
});
