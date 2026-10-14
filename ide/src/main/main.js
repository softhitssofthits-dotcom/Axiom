const { app, BrowserWindow, Menu, screen, ipcMain } = require('electron');
const path = require('path');
const fs = require('fs');
const { setupIpc, killActivePty } = require('./ipc');
const { ensureFreshAxiom } = require('./axiom-ensure');
const { IdeLogger } = require('./ide-logger');
const { setupBackendIpc, closeBackend, recoverBackend } = require('./backend-ipc');
const { attachWindowRecovery } = require('./window-recovery');

// Explicit profile injection keeps UI checks away from the user's real settings.
if (process.env.AXIOM_IDE_USER_DATA_DIR) {
  app.setPath('userData', path.resolve(process.env.AXIOM_IDE_USER_DATA_DIR));
}

let logger = null;

function getLogger() {
  if (!logger) logger = new IdeLogger(path.join(app.getPath('userData'), 'logs'));
  logger.start();
  return logger;
}

process.on('uncaughtExceptionMonitor', (error) => { try { getLogger().error('main.uncaught-exception', error); } catch {} });
process.on('unhandledRejection', (reason) => { try { getLogger().error('main.unhandled-rejection', reason); } catch {} });

// Rebuild the Release CLI at startup if it is missing or built from another revision. On
// a failed rebuild with no usable binary at all the IDE cannot do its job — surface that
// loudly instead of opening a silent editor whose every check reports axiom-not-found.
// The guard must never kill the window itself: any unexpected throw inside it degrades
// to running with whatever binary exists.
function ensureAxiomAtStartup() {
  const log = getLogger();
  try {
    const srcRoot = path.join(__dirname, '..', '..', '..');
    const r = ensureFreshAxiom(srcRoot);
    if (r.rebuilt) log.info('axiom.ensure', { status: r.status, buildOk: r.buildOk });
    if (!r.exe || (r.rebuilt && (!r.buildOk || r.status !== 'fresh'))) {
      const { dialog } = require('electron');
      const why = r.exe
        ? `The installed axiom.exe (${r.status}) is out of date and an automatic Release rebuild failed.`
        : 'axiom.exe was not found and an automatic Release rebuild failed.';
      dialog.showErrorBox(
        'Axiom IDE — CLI out of date',
        `${why}\n\nBuild it manually: dotnet build src/Axiom.Cli -c Release${r.buildError ? '\n\n' + r.buildError : ''}`
      );
    }
  } catch (e) {
    log.error('axiom.ensure.failed', e);
  }
}

// Fix Chromium disk/GPU cache errors ("Unable to move the cache", "Gpu Cache Creation failed").
app.disableHardwareAcceleration();
app.commandLine.appendSwitch('disable-gpu');
app.commandLine.appendSwitch('disk-cache-size', '1');

// A single instance avoids two Electron processes fighting over the cache lock.
const gotLock = app.requestSingleInstanceLock();

let mainWindow = null;

function statePath() {
  return path.join(app.getPath('userData'), 'window-state.json');
}

function loadState() {
  const def = { width: 1200, height: 800, x: undefined, y: undefined, maximized: false };
  try {
    const p = statePath();
    if (fs.existsSync(p)) return { ...def, ...JSON.parse(fs.readFileSync(p, 'utf-8')) };
  } catch {}
  return def;
}

// Window background matches the saved theme so startup doesn't flash the wrong color.
function loadBackgroundColor() {
  try {
    const p = path.join(app.getPath('userData'), 'settings.json');
    if (fs.existsSync(p)) {
      const s = JSON.parse(fs.readFileSync(p, 'utf-8'));
      if (s.theme === 'light') return '#ffffff';
      if (s.theme === 'borland') return '#0000aa';
      return '#1e1e1e';
    }
  } catch {}
  return '#1e1e1e';
}

function saveState() {
  if (!mainWindow) return;
  try {
    const isMax = mainWindow.isMaximized();
    const b = isMax ? mainWindow.getNormalBounds() : mainWindow.getBounds();
    const p = statePath();
    fs.mkdirSync(path.dirname(p), { recursive: true });
    fs.writeFileSync(p, JSON.stringify({ width: b.width, height: b.height, x: b.x, y: b.y, maximized: isMax }), 'utf-8');
  } catch {}
}

function createWindow() {
  const log = getLogger();
  const state = loadState();

  let { x, y } = state;
  if (x !== undefined && y !== undefined) {
    const visible = screen.getAllDisplays().some((d) =>
      x >= d.bounds.x - 100 && x <= d.bounds.x + d.bounds.width - 100 &&
      y >= d.bounds.y - 100 && y <= d.bounds.y + d.bounds.height - 100
    );
    if (!visible) { x = undefined; y = undefined; }
  }

  mainWindow = new BrowserWindow({
    width: state.width,
    height: state.height,
    x, y,
    minWidth: 800,
    minHeight: 600,
    // Native title bar: the Axiom mark lives in the icon, the caption is just
    // "Axiom IDE" so the brand is not spelled twice.
    title: 'Axiom IDE',
    icon: path.join(__dirname, '..', 'renderer', 'assets', 'logo-light.png'),
    backgroundColor: loadBackgroundColor(),
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: path.join(__dirname, 'preload.js'),
    },
    show: false,
  });
  const createdWindow = mainWindow;

  if (state.maximized) mainWindow.maximize();

  mainWindow.loadFile(path.join(__dirname, '..', 'renderer', 'index.html'));
  mainWindow.on('focus', () => log.info('window.focus'));
  mainWindow.on('blur', () => log.info('window.blur'));
  mainWindow.on('closed', () => log.info('window.closed'));
  mainWindow.webContents.on('did-finish-load', () => log.info('renderer.loaded'));
  const recovery = attachWindowRecovery({
    window: createdWindow, app, dialog: require('electron').dialog, log,
    stop: () => { killActivePty(); return recoverBackend(); },
    replace: createWindow, saveState,
  });
  mainWindow.webContents.on('console-message', (details) => {
    const { level, message, lineNumber, sourceId } = details;
    const severity = level === 'error' ? 'error' : level === 'warning' ? 'warn' : 'info';
    const eventName = level === 'error'
      ? 'renderer.console.error'
      : level === 'warning' ? 'renderer.console.warn' : 'renderer.console';
    log.write(severity, eventName, { message, line: lineNumber, sourceId });
  });
  mainWindow.once('ready-to-show', () => mainWindow?.show());
  let closeApproved = false;
  let closeHandlerReady = false;
  const handlerReady = event => {
    if (event.sender === mainWindow?.webContents && event.senderFrame === event.sender.mainFrame) closeHandlerReady = true;
  };
  ipcMain.on('window-close-handler-ready', handlerReady);
  const approveClose = event => {
    if (event.sender !== mainWindow?.webContents || event.senderFrame !== event.sender.mainFrame) return;
    closeApproved = true;
    recovery.cancelClose();
    mainWindow.close();
  };
  ipcMain.on('window-close-approved', approveClose);
  mainWindow.once('closed', () => {
    ipcMain.removeListener('window-close-approved', approveClose);
    ipcMain.removeListener('window-close-handler-ready', handlerReady);
    if (process.platform !== 'darwin' && mainWindow === createdWindow) app.quit();
  });
  mainWindow.on('close', event => {
    if (closeHandlerReady && !closeApproved && !mainWindow.webContents.isDestroyed() && !mainWindow.webContents.isCrashed()) {
      event.preventDefault(); recovery.requestClose(); mainWindow.webContents.send('window-close-requested'); return;
    }
    log.info('window.close'); saveState();
  });
  return mainWindow;
}

if (!gotLock) {
  // app.quit() before whenReady() never terminates the process on
  // Electron/Windows (the loser stayed as a zombie and the first instance
  // never got the second-instance handshake). app.exit() terminates for real.
  app.exit(0);
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore();
      mainWindow.focus();
    }
  });

  app.whenReady().then(() => {
    getLogger().info('app.ready', { version: app.getVersion(), electron: process.versions.electron, node: process.versions.node, userData: app.getPath('userData'), logDirectory: path.join(app.getPath('userData'), 'logs') });
    // Freshness guard: a stale Release axiom.exe must never serve diagnostics. Rebuild
    // synchronously before the window opens (incremental no-op build is a few seconds).
    ensureAxiomAtStartup();

    setupIpc(getLogger());
    setupBackendIpc();
    Menu.setApplicationMenu(null);
    createWindow();

    app.on('activate', () => {
      if (BrowserWindow.getAllWindows().length === 0) createWindow();
    });
  });

  app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') app.quit();
  });

  // Quitting (window close / Exit menu) must not orphan a running Axiom program:
  // without this the program keeps running invisibly after the IDE is gone.
  let backendClosed = false;
  app.on('before-quit', event => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      event.preventDefault(); mainWindow.close(); return;
    }
    if (backendClosed) return;
    killActivePty();
    event.preventDefault();
    closeBackend().finally(() => { backendClosed = true; app.quit(); });
  });
}
