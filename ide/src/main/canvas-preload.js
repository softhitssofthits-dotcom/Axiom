const { contextBridge, ipcRenderer } = require('electron');
contextBridge.exposeInMainWorld('canvasHost', {
  onFrame: callback => ipcRenderer.on('canvas-frame', (_event, frame) => callback(frame)),
  painted: frameId => ipcRenderer.send('canvas-painted', frameId),
  key: data => ipcRenderer.send('canvas-key', data),
});
