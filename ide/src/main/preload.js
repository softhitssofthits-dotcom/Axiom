const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('axiom', {
  backendReady: () => ipcRenderer.invoke('backend-ready'),
  documentCommand: (method, snapshot) => ipcRenderer.invoke('backend-document', method, snapshot),
  startExecution: document => ipcRenderer.invoke('backend-start', document),
  stopExecution: sessionId => ipcRenderer.invoke('backend-stop', sessionId),
  submitInput: (sessionId, requestId, text) => ipcRenderer.invoke('backend-input', sessionId, requestId, text),
  programKey: (sessionId, data) => ipcRenderer.invoke('backend-key', sessionId, data),
  debugCommand: (sessionId, command, stopId) => ipcRenderer.invoke('backend-debug', sessionId, command, stopId),
  acknowledgeFrame: (sessionId, frameId) => ipcRenderer.invoke('backend-frame-ack', sessionId, frameId),
  acknowledgeOutput: (sessionId, outputVersion) => ipcRenderer.invoke('backend-output-ack', sessionId, outputVersion),
  onBackendEvent: cb => { const listener = (_e, data) => cb(data); ipcRenderer.on('backend-event', listener); return () => ipcRenderer.removeListener('backend-event', listener); },
  onBackendFrame: cb => { const listener = (_e, data) => cb(data); ipcRenderer.on('backend-frame', listener); return () => ipcRenderer.removeListener('backend-frame', listener); },
  onBackendFault: cb => { const listener = (_e, data) => cb(data); ipcRenderer.on('backend-fault', listener); return () => ipcRenderer.removeListener('backend-fault', listener); },
  // files
  readFileContent: (p) => ipcRenderer.invoke('read-file-content', p),
  writeFile: (p, content, expectedText) => ipcRenderer.invoke('write-file', p, content, expectedText),
  createProjectFile: (root, target, text) => ipcRenderer.invoke('create-project-file', root, target, text),
  openFileDialog: () => ipcRenderer.invoke('open-file-dialog'),
  openProjectDialog: () => ipcRenderer.invoke('open-project-dialog'),
  listProjectFiles: (p) => ipcRenderer.invoke('list-project-files', p),
  saveFileDialog: (defaultPath) => ipcRenderer.invoke('save-file-dialog', defaultPath),
  confirmDocumentClose: name => ipcRenderer.invoke('confirm-document-close', name),
  onWindowCloseRequested: cb => { ipcRenderer.on('window-close-requested', () => cb()); ipcRenderer.send('window-close-handler-ready'); },
  approveWindowClose: () => ipcRenderer.send('window-close-approved'),

  // run / check / format / stop
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
  getLanguageMetadata: async () => {
    const response = await ipcRenderer.invoke('backend-language-metadata');
    return response.ok ? { ok: true, metadata: response.payload } : response;
  },

  // settings
  getSettings: () => ipcRenderer.invoke('get-settings'),
  getRecovery: () => ipcRenderer.invoke('get-recovery'),
  saveRecoverySync: tabs => ipcRenderer.sendSync('save-recovery-sync', tabs),
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
