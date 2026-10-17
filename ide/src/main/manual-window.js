const fs = require('node:fs');
const path = require('node:path');
const { fileURLToPath } = require('node:url');

class ManualWindow {
  constructor({ root = path.resolve(__dirname, '../../..'), BrowserWindow, openExternal }) {
    this.directory = path.join(root, 'docs', 'manual');
    this.BrowserWindow = BrowserWindow; this.openExternal = openExternal; this.windows = new WeakMap();
  }
  local(url) {
    try {
      const parsed = new URL(url); if (parsed.protocol !== 'file:') return false;
      const relative = path.relative(fs.realpathSync(this.directory), fs.realpathSync(fileURLToPath(parsed)));
      return relative !== '..' && !relative.startsWith('..' + path.sep) && !path.isAbsolute(relative);
    } catch { return false; }
  }
  external(url) {
    try {
      if (['http:', 'https:'].includes(new URL(url).protocol)) Promise.resolve(this.openExternal(url)).catch(() => {});
    } catch { /* Другие схемы не передаются операционной системе. */ }
  }
  async open(owner) {
    if (!owner || owner.isDestroyed()) throw new Error('Окно IDE закрыто.');
    const existing = this.windows.get(owner);
    if (existing && !existing.isDestroyed()) { existing.show(); existing.focus(); return existing; }
    const file = path.join(this.directory, 'index.html');
    if (!fs.existsSync(file)) throw new Error('Локальный справочник не найден.');
    const window = new this.BrowserWindow({ parent: owner, title: 'Axiom', width: 1100, height: 800,
      minWidth: 640, minHeight: 480, show: false, autoHideMenuBar: true, backgroundColor: owner.getBackgroundColor(),
      webPreferences: { nodeIntegration: false, contextIsolation: true, sandbox: true } });
    this.windows.set(owner, window);
    const closeWithOwner = () => { if (!window.isDestroyed()) window.destroy(); };
    owner.once('closed', closeWithOwner);
    window.on('closed', () => {
      owner.removeListener('closed', closeWithOwner);
      if (this.windows.get(owner) === window) this.windows.delete(owner);
    });
    window.webContents.on('will-navigate', (event, url) => {
      if (this.local(url)) return;
      event.preventDefault(); this.external(url);
    });
    window.webContents.setWindowOpenHandler(({ url }) => {
      if (this.local(url)) window.loadURL(url).catch(() => {}); else this.external(url);
      return { action: 'deny' };
    });
    try { await window.loadFile(file); window.show(); return window; }
    catch (error) { window.destroy(); throw error; }
  }
}
module.exports = { ManualWindow };
