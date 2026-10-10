const { BrowserWindow, ipcMain } = require('electron');
const path = require('node:path');
const { pathToFileURL } = require('node:url');

// Окно получает только кадры своей сессии и подтверждает показанный кадр.
class CanvasWindow {
  constructor({ acknowledge, stop }) {
    this.acknowledge = acknowledge; this.stop = stop; this.window = null;
    this.sessionId = null; this.pending = null; this.ready = false;
    this.url = pathToFileURL(path.join(__dirname, '../renderer/canvas.html')).href;
    ipcMain.on('canvas-painted', (event, frameId) => {
      if (!this.window || event.sender !== this.window.webContents ||
          event.senderFrame !== event.sender.mainFrame || event.senderFrame.url !== this.url ||
          frameId !== this.pending?.header.frameId) return;
      const id = this.sessionId; this.pending = null;
      this.acknowledge(id, frameId).catch(() => {});
    });
  }
  reset() {
    this.sessionId = null; this.pending = null; this.ready = false;
    const window = this.window; this.window = null;
    if (window && !window.isDestroyed()) window.destroy();
  }
  frame(frame) {
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
        const id = this.sessionId; this.window = null; this.pending = null; this.ready = false;
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
