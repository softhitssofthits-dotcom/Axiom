const { BrowserWindow, ipcMain } = require('electron');
const path = require('node:path');
const { pathToFileURL } = require('node:url');

// Окно получает только кадры своей сессии и подтверждает показанный кадр.
class CanvasWindow {
  constructor({ acknowledge, stop, input, mouse }) {
    this.acknowledge = acknowledge; this.stop = stop; this.window = null;
    this.sessionId = null; this.pending = null; this.ready = false;
    this.closedSessionId = null;
    this.url = pathToFileURL(path.join(__dirname, '../renderer/canvas.html')).href;
    ipcMain.on('canvas-mouse', (event, data) => {
      if (!this.window || event.sender !== this.window.webContents ||
          event.senderFrame !== event.sender.mainFrame || event.senderFrame.url !== this.url || !data ||
          !['move','down','up','wheel','clear'].includes(data.action) ||
          (data.action !== 'clear' && (!Number.isSafeInteger(data.x) || !Number.isSafeInteger(data.y))) ||
          (['down','up'].includes(data.action) && ![1,2,3].includes(data.button)) ||
          (data.action === 'wheel' && !Number.isInteger(data.delta))) return;
      mouse?.(this.sessionId, data).catch(() => {});
    });
    ipcMain.on('canvas-key', (event, data) => {
      if (!this.window || event.sender !== this.window.webContents ||
          event.senderFrame !== event.sender.mainFrame || event.senderFrame.url !== this.url ||
          !data || !['down', 'up', 'clear'].includes(data.action) ||
          (data.action !== 'clear' && (typeof data.key !== 'string' || data.key.length === 0 || data.key.length > 32)) ||
          (data.text !== undefined && (typeof data.text !== 'string' || data.text.length > 32))) return;
      input(this.sessionId, data).catch(() => {});
    });
    ipcMain.on('canvas-painted', (event, frameId) => {
      if (!this.window || event.sender !== this.window.webContents ||
          event.senderFrame !== event.sender.mainFrame || event.senderFrame.url !== this.url ||
          frameId !== this.pending?.header.frameId) return;
      const id = this.sessionId; this.pending = null;
      this.acknowledge(id, frameId).catch(() => {});
    });
  }
  reset() {
    this.closedSessionId = null;
    this.sessionId = null; this.pending = null; this.ready = false;
    const window = this.window; this.window = null;
    if (window && !window.isDestroyed()) window.destroy();
  }
  frame(frame) {
    // Кадры уже закрытой сессии могут прибыть до завершения отмены VM.
    if (frame.header.sessionId === this.closedSessionId) {
      this.acknowledge(frame.header.sessionId, frame.header.frameId).catch(() => {});
      return;
    }
    if (this.sessionId && this.sessionId !== frame.header.sessionId) this.reset();
    this.sessionId = frame.header.sessionId; this.pending = frame;
    if (!this.window) {
      const window = new BrowserWindow({ width: 1280, height: 800, minWidth: 320, minHeight: 240,
        title: 'Axiom — CANVAS', backgroundColor: '#101720', show: false,
        webPreferences: { preload: path.join(__dirname, 'canvas-preload.js'),
          contextIsolation: true, nodeIntegration: false, sandbox: true } });
      this.window = window; window.setMenu(null);
      window.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
      window.webContents.on('will-navigate', event => event.preventDefault());
      window.webContents.once('did-finish-load', () => {
        if (this.window !== window) return;
        this.ready = true; this.deliver(); window.show();
      });
      window.on('closed', () => {
        if (this.window !== window) return;
        const id = this.sessionId; this.closedSessionId = id;
        if (this.pending) this.acknowledge(id, this.pending.header.frameId).catch(() => {});
        this.window = null; this.pending = null; this.ready = false;
        if (id) this.stop(id).catch(() => {});
      });
      window.loadURL(this.url);
    } else this.deliver();
  }
  deliver() {
    if (this.ready && this.pending && this.window && !this.window.isDestroyed())
      this.window.webContents.send('canvas-frame', this.pending);
  }
}
module.exports = { CanvasWindow };
