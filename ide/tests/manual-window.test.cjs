const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { EventEmitter } = require('node:events');
const { pathToFileURL } = require('node:url');
const { ManualWindow } = require('../src/main/manual-window');
function setup(t) {
  const base = path.resolve(__dirname, '../../.artifacts/manual-window'); fs.mkdirSync(base, { recursive: true });
  const root = fs.mkdtempSync(path.join(base, 'case-')); t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const directory = path.join(root, 'docs', 'manual'); fs.mkdirSync(directory, { recursive: true });
  fs.writeFileSync(path.join(directory, 'index.html'), '<h1>Axiom</h1>'); fs.writeFileSync(path.join(directory, 'print.html'), '<h1>PRINT</h1>');
  const windows = [], external = [];
  class Window extends EventEmitter {
    constructor(options) { super(); this.options = options; this.webContents = new EventEmitter(); this.webContents.setWindowOpenHandler = callback => this.popup = callback; windows.push(this); }
    isDestroyed() { return !!this.destroyed; } show() { this.shown = true; } focus() { this.focused = true; }
    async loadFile(file) { this.file = file; } async loadURL(url) { this.url = url; }
    destroy() { this.destroyed = true; this.emit('closed'); }
  }
  const service = new ManualWindow({ root, BrowserWindow: Window, openExternal: async url => external.push(url) });
  const owner = Object.assign(new EventEmitter(), { isDestroyed: () => false, getBackgroundColor: () => '#123456' });
  return { root, directory, service, owner, windows, external };
}
test('Локальный справочник открывается в отдельном окне без Node и preload', async t => {
  const { service, owner, directory, windows } = setup(t); await service.open(owner);
  assert.equal(windows.length, 1); const window = windows[0];
  assert.equal(window.file, path.join(directory, 'index.html')); assert.equal(window.options.parent, owner);
  assert.deepEqual(window.options.webPreferences, { nodeIntegration: false, contextIsolation: true, sandbox: true });
  assert.equal(window.shown, true);
  await service.open(owner); assert.equal(windows.length, 1); assert.equal(window.focused, true);
  window.destroy(); await service.open(owner); assert.equal(windows.length, 2);
});
test('Навигация разрешает статьи справочника и отвергает чужие файлы', async t => {
  const { service, owner, directory, root, windows } = setup(t); await service.open(owner);
  const window = windows[0], outside = path.join(root, 'secret.html'); fs.writeFileSync(outside, 'private');
  for (const [url, blocked] of [[pathToFileURL(path.join(directory, 'print.html')).href + '#syntax', false],
    [pathToFileURL(outside).href, true], ['javascript:alert(1)', true], ['file:///C:/Windows/win.ini', true]]) {
    let prevented = false; window.webContents.emit('will-navigate', { preventDefault() { prevented = true; } }, url);
    assert.equal(prevented, blocked, url);
  }
});
test('Внешние ссылки открываются только через системный браузер по HTTP/HTTPS', async t => {
  const { service, owner, directory, windows, external } = setup(t); await service.open(owner);
  const window = windows[0], article = pathToFileURL(path.join(directory, 'print.html')).href;
  assert.equal(window.popup({ url: article }).action, 'deny'); assert.equal(window.url, article);
  assert.equal(window.popup({ url: 'https://example.com/help' }).action, 'deny');
  window.popup({ url: 'javascript:alert(1)' }); window.popup({ url: 'data:text/html,hello' });
  assert.deepEqual(external, ['https://example.com/help']);
});
test('Отсутствующий справочник даёт ошибку без пустого окна', async t => {
  const { service, owner, directory, windows } = setup(t); fs.unlinkSync(path.join(directory, 'index.html'));
  await assert.rejects(service.open(owner), /справочник/); assert.equal(windows.length, 0);
});
test('Закрытие владельца уничтожает его окно справочника', async t => {
  const { service, owner, windows } = setup(t); await service.open(owner);
  owner.emit('closed'); assert.equal(windows[0].isDestroyed(), true);
});
