const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('axiom', {
  // files
  readFileContent: (p) => ipcRenderer.invoke('read-file-content', p),
  writeFile: (p, content) => ipcRenderer.invoke('write-file', p, content),
  openFileDialog: () => ipcRenderer.invoke('open-file-dialog'),
  openProjectDialog: () => ipcRenderer.invoke('open-project-dialog'),
  listProjectFiles: (p) => ipcRenderer.invoke('list-project-files', p),
  saveFileDialog: (defaultPath) => ipcRenderer.invoke('save-file-dialog', defaultPath),

  // run / check / format / stop
  checkFile: (p) => ipcRenderer.invoke('check-file', p),
  formatFile: (p) => ipcRenderer.invoke('format-file', p),
  formatBuffer: (text) => ipcRenderer.invoke('format-buffer', text),
  checkBuffer: (text, filePath) => ipcRenderer.invoke('check-buffer', text, filePath),
  stopRun: () => ipcRenderer.invoke('stop-run'),

  // Integrated console: the program runs in a real ConPTY (terminal echo, Enter
  // newline); the renderer hosts an xterm.js view wired to these channels.
  runConsole: (p) => ipcRenderer.invoke('run-console', p),
  ptyWrite: (data) => ipcRenderer.invoke('pty-write', data),
  ptyResize: (cols, rows) => ipcRenderer.invoke('pty-resize', cols, rows),
  onPtyData: (cb) => ipcRenderer.on('pty-data', (_e, d) => cb(d)),
  onPtyExit: (cb) => ipcRenderer.on('pty-exit', (_e, payload) => cb(payload)),
  // Clipboard text via the main process — navigator.clipboard.readText() hangs
  // on an image-only clipboard (no text part), which once let a paste stall
  // the console input queue entirely
  readClipboardText: () => ipcRenderer.invoke('read-clipboard-text'),
  writeClipboardText: (text) => ipcRenderer.invoke('write-clipboard-text', text),
  openLogFolder: () => ipcRenderer.invoke('open-log-folder'),

  // meta
  getVersion: () => ipcRenderer.invoke('get-version'),
  getLanguageMetadata: () => ipcRenderer.invoke('get-language-metadata'),

  // settings
  getSettings: () => ipcRenderer.invoke('get-settings'),
  saveSettings: (patch) => ipcRenderer.invoke('save-settings', patch),
  // Synchronous final save from beforeunload (cannot await async invoke there)
  saveSettingsSync: (patch) => ipcRenderer.sendSync('save-settings-sync', patch),
  addRecentFile: (p) => ipcRenderer.invoke('add-recent-file', p),

  // file watcher (external modification detection)
  watchFile: (p) => ipcRenderer.invoke('watch-file', p),
  unwatchFile: (p) => ipcRenderer.invoke('unwatch-file', p),
  onFileChanged: (cb) => ipcRenderer.on('ax-file-changed', (_e, payload) => cb(payload)),
  // IDE diagnostics log: fire-and-forget so logging can never block editor input.
  log: (event, data, level) => ipcRenderer.send('ide-log', { event, data, level }),
});
