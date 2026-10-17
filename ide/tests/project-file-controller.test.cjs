const { test } = require('node:test');
const assert = require('node:assert/strict');
require('../src/renderer/project-file-controller');
const ProjectFileController = globalThis.ProjectFileController;

function setup(overrides = {}) {
  const calls = { rename: [], close: [], trash: 0, paste: 0, prepare: [] };
  const ide = {
    projectDir: 'C:\\Проект',
    openFiles: new Map(),
    running: false,
    _runFile: null,
    setStatus() {},
    async refreshProjectFiles() {},
    async renameActive(next, old) { calls.rename.push([old, next]); },
    async prepareClose(path) { calls.prepare.push(path); return true; },
    async closeFile(path, approved) { calls.close.push([path, approved]); },
    ...overrides,
  };
  const api = {
    async captureProjectFile() { return { ok: true }; },
    async pasteProjectFile() { calls.paste++; return { ok: false, error: 'нет' }; },
    async trashProjectFile() { calls.trash++; return { ok: true, source: 'C:\\Проект\\a.axm' }; },
    async projectFileProperties() { return { ok: false }; },
  };
  return { ide, api, calls, controller: new ProjectFileController(ide, api) };
}

test('Перемещение запускаемой программы блокируется до файловой операции', async () => {
  const { ide, calls, controller } = setup({ running: true });
  const source = 'C:\\Проект\\a.axm';
  ide.openFiles.set(source, {}); ide._runFile = source;
  controller.clipboard = { source, cut: true, directory: false };
  const messages = []; globalThis.alert = message => messages.push(message);
  globalThis.I18N = { t: key => key };
  await controller.paste();
  assert.equal(calls.paste, 0);
  assert.deepEqual(messages, ['files.runningBlocked']);
});

test('Успешное вырезание переносит открытую запись на новый путь', async () => {
  const { ide, api, calls, controller } = setup();
  const source = 'C:\\Проект\\вложенная\\a.axm', target = 'C:\\Проект\\a.axm';
  ide.openFiles.set(source, {});
  controller.clipboard = { source, cut: true, directory: false };
  api.pasteProjectFile = async () => { calls.paste++; return { ok: true, source, target, cut: true, directory: false }; };
  globalThis.I18N = { t: key => key };
  await controller.paste();
  assert.deepEqual(calls.rename, [[source, target]]);
  assert.equal(controller.clipboard, null);
});

test('Перемещение папки переносит всех открытых потомков с сохранением относительных путей', async () => {
  const { ide, api, calls, controller } = setup();
  const folder = 'C:\\Проект\\вложенная', target = 'C:\\Проект\\вложенная-копия';
  const first = folder + '\\Первый.axm', second = folder + '\\Глубоко\\Второй.axm';
  ide.openFiles.set(first, {}); ide.openFiles.set(second, {});
  controller.clipboard = { source: folder, cut: true, directory: true };
  api.pasteProjectFile = async () => { calls.paste++; return { ok: true, source: folder, target, cut: true, directory: true }; };
  globalThis.I18N = { t: key => key };
  await controller.paste();
  assert.deepEqual(calls.rename, [
    [first, target + '\\Первый.axm'],
    [second, target + '\\Глубоко\\Второй.axm'],
  ]);
});

test('Отмена корзины не закрывает документ и не вызывает shell', async () => {
  const { ide, calls, controller } = setup();
  const source = 'C:\\Проект\\a.axm'; ide.openFiles.set(source, {});
  globalThis.I18N = { t: (key, name) => key + ':' + name };
  globalThis.confirm = () => false; globalThis.alert = () => {};
  await controller.trash(source);
  assert.equal(calls.trash, 0);
  assert.deepEqual(calls.prepare, []);
  assert.deepEqual(calls.close, []);
});

test('Отказ сохранения перед корзиной оставляет файл и вкладку', async () => {
  const { ide, calls, controller } = setup({ async prepareClose() { return false; } });
  const source = 'C:\\Проект\\a.axm'; ide.openFiles.set(source, {});
  globalThis.I18N = { t: key => key };
  globalThis.confirm = () => true; globalThis.alert = () => {};
  await controller.trash(source);
  assert.equal(calls.trash, 0);
  assert.deepEqual(calls.close, []);
});

test('После успешной корзины закрываются только затронутые открытые документы', async () => {
  const { ide, api, calls, controller } = setup();
  const source = 'C:\\Проект\\a.axm', other = 'C:\\Проект\\b.axm';
  ide.openFiles.set(source, {}); ide.openFiles.set(other, {});
  api.trashProjectFile = async () => { calls.trash++; return { ok: true, source }; };
  globalThis.I18N = { t: (key, name) => key + ':' + name };
  globalThis.confirm = () => true; globalThis.alert = () => {};
  await controller.trash(source);
  assert.equal(calls.trash, 1);
  assert.deepEqual(calls.close, [[source, true]]);
});
