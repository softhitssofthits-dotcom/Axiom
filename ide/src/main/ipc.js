const { ipcMain, dialog, app, BrowserWindow, clipboard, shell } = require('electron');
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');
// node-pty is an N-API module with prebuilds: no electron-rebuild step needed.
// Guarded require — without it the console run reports the failure in the
// terminal instead of crashing the main process.
const pty = (() => { try { return require('node-pty'); } catch { return null; } })();

// ---------- axiom resolution ----------

// axiom resolution lives in axiom-ensure.js (single source of truth, shared with main.js
// startup guard and the run-ide.bat launcher helper).
const { resolveAxiomPath } = require('./axiom-ensure');

// File dialogs remember the last visited folder (settings.json `lastOpenDir`).
const { resolveDefaultDir, dirOfPickedFile } = require('./last-dir');
const { listProjectFiles } = require('./project-files');

// IDE-synthetic diagnostics are not compiler diagnostics, so they carry a synthetic code.
const IDE_CODE = 'AXIOM-IDE';

function resolveAxiom() {
  return resolveAxiomPath().exe;
}

function axiomNotFound(filePath) {
  return {
    stdout: '',
    stderr: '',
    diagnostics: [{
      file: filePath, line: 0, col: 0, severity: 'error', code: IDE_CODE,
      message: 'axiom.exe не найден. Соберите CLI (run-ide.bat или dotnet build src/Axiom.Cli -c Release).',
    }],
    exitCode: 2,
    stopped: false,
  };
}

// axiom is a framework-dependent .NET 10 binary. The host may have a different
// DOTNET_ROOT (e.g. a bundled .NET 8), so point the child at the system .NET 10.
function buildEnv() {
  const env = { ...process.env };
  if (require('./portable-runtime').portableRuntime()) return env;
  const preferred = process.env.AXIOM_DOTNET_ROOT || 'C:\\Program Files\\dotnet';
  if (fs.existsSync(path.join(preferred, 'dotnet.exe'))) {
    env.DOTNET_ROOT = preferred;
    env.PATH = preferred + ';' + (env.PATH || '');
  }
  return env;
}

// ---------- diagnostics parsing ----------

// stderr format: "<file>(<line>,<col>): ошибка MBxxxx: <message>" (the Axiom CLI prints
// diagnostics in Russian; "error" is accepted too so an English-localized build still parses).
const DIAG_RE = /\((\d+),(\d+)\):\s*(?:error|ошибка)\s+(MB[\dA-Za-z-]*):\s*(.*)$/;
const CARET_RE = /^\s*\^+\s*$/;
const ANSI_RE = /\x1B(?:\[[0-?]*[ -/]*[@-~]|\][^\x07]*(?:\x07|\x1B\\))/g;
const IDE_RUNTIME_ERROR_RE = /^AXIOM_IDE_RUNTIME_ERROR:\s*(.+)$/m;

function ideRuntimeDiagnostic(filePath, detail) {
  return {
    file: filePath,
    line: 1,
    col: 1,
    length: 1,
    severity: 'error',
    code: IDE_CODE,
    message: 'Ошибка выполнения: ' + detail,
  };
}

function parseDiagnostics(stderr) {
  const out = [];
  for (const raw of stderr.split(/\r?\n/)) {
    const line = raw.trimEnd();
    if (!line) continue;
    if (CARET_RE.test(line)) {
      if (out.length) out[out.length - 1].length = line.trim().length;
      continue;
    }
    const m = line.match(DIAG_RE);
    if (!m) continue;
    out.push({
      file: line.slice(0, m.index).trim(),
      line: parseInt(m[1], 10),
      col: parseInt(m[2], 10),
      severity: 'error',
      code: m[3],
      message: m[4],
    });
  }
  return out;
}

// ---------- process management ----------

// ---------- integrated console ----------

// The program runs inside a real Windows pseudoconsole (ConPTY): the terminal echoes
// typed characters and the Enter newline exactly like cmd/PowerShell — INPUT()
// interaction is indistinguishable from `axiom run` in a native console. Only the latest
// console session is kept: a new Run replaces it, Stop/quit kill it.
let activePty = null;

function killActivePty() {
  if (activePty) {
    const p = activePty;
    activePty = null;
    try { p.kill(); } catch {}
  }
}

function startConsoleRun(filePath) {
  return new Promise((resolve) => {
    logIpc('run-console.start', { filePath });
    if (!pty) { resolve({ ok: false, reason: 'node-pty-missing' }); return; }
    const axiom = resolveAxiom();
    if (!axiom) { resolve({ ok: false, reason: 'axiom-not-found' }); return; }
    killActivePty(); // one console session at a time — the new Run replaces the old one

    let term;
    let diagnosticsDirectory;
    let diagnosticsPath;
    const cleanupDiagnostics = () => {
      if (!diagnosticsDirectory) return;
      try { fs.unlinkSync(diagnosticsPath); } catch {}
      try { fs.rmdirSync(diagnosticsDirectory); } catch {}
    };
    try {
      diagnosticsDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'axiom-diagnostics-'));
      diagnosticsPath = path.join(diagnosticsDirectory, 'stderr.txt');
      term = pty.spawn(axiom, ['run', filePath], {
        cwd: path.dirname(filePath),
        name: 'xterm-256color',
        cols: 100,
        rows: 26,
        env: { ...buildEnv(), AXIOM_IDE_DIAGNOSTICS_PATH: diagnosticsPath },
      });
    } catch (e) {
      cleanupDiagnostics();
      resolve({ ok: false, reason: 'spawn-failed', detail: String((e && e.message) || e) });
      return;
    }
    activePty = term;
    term.onData((d) => {
      // Only program output reaches ConPTY; diagnostics use the private file channel.
      const w = BrowserWindow.getAllWindows()[0];
      if (w && !w.isDestroyed()) w.webContents.send('pty-data', d);
    });
    term.onExit(({ exitCode }) => {
      logIpc('run-console.exit', { filePath, exitCode });
      if (activePty === term) activePty = null;
      const w = BrowserWindow.getAllWindows()[0];
      let stderr = '';
      try { stderr = fs.readFileSync(diagnosticsPath, 'utf8'); }
      catch (error) { logIpc('run-console.diagnostics-read.failed', { message: error.message }); }
      cleanupDiagnostics();
      const diagnostics = parseDiagnostics(stderr.replace(ANSI_RE, ''));
      const runtimeError = stderr.match(IDE_RUNTIME_ERROR_RE);
      if (runtimeError) diagnostics.push(ideRuntimeDiagnostic(filePath, runtimeError[1].trim()));
      if (stderr.trim() && !diagnostics.length) {
        diagnostics.push({ file: filePath, line: 1, col: 1, length: 1, severity: 'error',
          code: IDE_CODE, message: stderr.trim() });
      }
      if (diagnostics.length) logIpc('run-console.diagnostics', { diagnostics });
      if (w && !w.isDestroyed()) w.webContents.send('pty-exit', { exitCode, diagnostics });
    });
    resolve({ ok: true });
  });
}

// ---------- file watcher (external modification detection) ----------

// Polls mtime/size for every open file so the IDE can offer a reload when the
// file is changed or deleted outside of the IDE (no fs.watch quirks, deterministic).
const watchedFiles = new Map(); // path -> { exists, mtimeMs, size, lastNotifiedMtime, deletedNotified }
const activeFileWrites = new Map();
let watchTimer = null;

function statOf(p) {
  try {
    const s = fs.statSync(p);
    return { exists: true, mtimeMs: s.mtimeMs, size: s.size };
  } catch {
    return { exists: false };
  }
}

function broadcastFileChange(payload) {
  const win = BrowserWindow.getAllWindows()[0];
  if (win && !win.isDestroyed()) win.webContents.send('ax-file-changed', payload);
}

function ensureWatchLoop() {
  if (watchTimer) return;
  watchTimer = setInterval(() => {
    for (const [p, w] of watchedFiles) {
      if (activeFileWrites.has(p)) continue;
      const info = statOf(p);
      if (!info.exists) {
        if (w.exists && !w.deletedNotified) {
          w.deletedNotified = true;
          broadcastFileChange({ path: p, exists: false });
        }
        w.exists = false;
        continue;
      }
      if (!w.exists) {
        // the file reappeared after an external deletion
        w.exists = true;
        w.mtimeMs = info.mtimeMs;
        w.size = info.size;
        w.lastNotifiedMtime = info.mtimeMs;
        w.deletedNotified = false;
        broadcastFileChange({ path: p, exists: true, mtimeMs: info.mtimeMs });
        continue;
      }
      if (info.mtimeMs !== w.mtimeMs || info.size !== w.size) {
        w.mtimeMs = info.mtimeMs;
        w.size = info.size;
        if (w.lastNotifiedMtime !== info.mtimeMs) {
          w.lastNotifiedMtime = info.mtimeMs;
          broadcastFileChange({ path: p, exists: true, mtimeMs: info.mtimeMs });
        }
      }
    }
    if (watchedFiles.size === 0 && watchTimer) {
      clearInterval(watchTimer);
      watchTimer = null;
    }
  }, 1500);
}

function watcherNoteSelfWrite(filePath) {
  const w = watchedFiles.get(filePath);
  if (!w) return;
  const info = statOf(filePath);
  if (info.exists) {
    w.exists = true;
    w.mtimeMs = info.mtimeMs;
    w.size = info.size;
    w.lastNotifiedMtime = info.mtimeMs;
    w.deletedNotified = false;
  }
}

// ---------- settings ----------

// Профиль ведёт SettingsStore: он переносит прежний профиль RecentFilesState,
// сохраняет повреждённый файл до замены и пишет настройки атомарно.
const { SettingsStore } = require('./settings-store');
let settingsStore = null;

function loadSettings() {
  if (!settingsStore) settingsStore = new SettingsStore(app.getPath('userData'));
  return settingsStore.load();
}

function saveSettings() {
  const store = settingsStore || (settingsStore = new SettingsStore(app.getPath('userData')));
  return store.save(store.settings || store.load());
}

// ---------- IPC ----------

let ideLogger = null;

function logIpc(event, data) {
  try { ideLogger?.info(event, data); } catch {}
}

function setupIpc(logger, options = {}) {
  const manualWindow = options.manualWindow || null;
  const fromMainFrame = (event) => event.senderFrame === event.sender.mainFrame && event.senderFrame.url.endsWith('/index.html');
  const invalidSource = () => ({ ok: false, error: 'Недопустимый источник команды.' });
  const { ProjectFileActions } = require('./project-file-actions');
  // Один внутренний буфер файловой панели на окно IDE: системный буфер Windows
  // не является частью договора этих действий.
  const projectFileActions = new ProjectFileActions();
  const projectPath = value => typeof value === 'string' && value.length > 0 && value.length <= 32767 && path.isAbsolute(value);
  const projectCreation = require('./project-create');
  ipcMain.handle('project-creation-defaults', event => {
    if (!fromMainFrame(event)) return { parent: null };
    return { parent: projectCreation.projectCreationParent(loadSettings(), app.getPath('documents')) };
  });
  ipcMain.handle('choose-project-parent', async (event, currentParent) => {
    if (!fromMainFrame(event)) return null;
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) return null;
    const defaultPath = projectCreation.existingDirectory(currentParent) ||
      projectCreation.projectCreationParent(loadSettings(), app.getPath('documents'));
    const result = await dialog.showOpenDialog(owner, { defaultPath, properties: ['openDirectory'] });
    return result.canceled ? null : result.filePaths[0] || null;
  });
  ipcMain.handle('create-project', (event, parent, name) => {
    if (!fromMainFrame(event)) return { ok: false, message: 'Недопустимый источник команды.' };
    try { return { ok: true, ...projectCreation.createProject(parent, name) }; }
    catch (error) { return { ok: false, message: error.message }; }
  });
  ipcMain.handle('create-project-file', (event, root, target, text) => {
    if (!fromMainFrame(event)) return { ok: false, message: 'Недопустимый источник команды.' };
    try { return { ok: true, path: require('./file-service').createProjectFile(root, target, text) }; }
    catch (error) { return { ok: false, message: error.message }; }
  });
  ipcMain.handle('capture-project-file', (event, root, item, cut) => {
    if (!fromMainFrame(event)) return invalidSource();
    if (!projectPath(root) || !projectPath(item) || typeof cut !== 'boolean') return { ok: false, error: 'Неверные аргументы файловой операции.' };
    try { return projectFileActions.capture(root, item, cut); }
    catch (error) { return { ok: false, error: error.message }; }
  });
  ipcMain.handle('paste-project-file', async (event, root, directory) => {
    if (!fromMainFrame(event)) return invalidSource();
    if (!projectPath(root) || !projectPath(directory)) return { ok: false, error: 'Неверные аргументы файловой операции.' };
    return projectFileActions.paste(root, directory);
  });
  ipcMain.handle('trash-project-file', async (event, root, item) => {
    if (!fromMainFrame(event)) return invalidSource();
    if (!projectPath(root) || !projectPath(item)) return { ok: false, error: 'Неверные аргументы файловой операции.' };
    return projectFileActions.trash(root, item);
  });
  ipcMain.handle('project-file-properties', (event, root, item) => {
    if (!fromMainFrame(event)) return invalidSource();
    if (!projectPath(root) || !projectPath(item)) return { ok: false, error: 'Неверные аргументы файловой операции.' };
    try { return { ok: true, ...projectFileActions.properties(root, item) }; }
    catch (error) { return { ok: false, error: error.message }; }
  });
  ipcMain.handle('confirm-document-close', async (event, name) => {
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || typeof name !== 'string' || name.length > 1024) return 'cancel';
    const en = loadSettings().language === 'en';
    const result = await dialog.showMessageBox(owner, {
      type: 'question', title: 'Axiom IDE', message: en ? 'Save changes to “' + name + '”?' : 'Сохранить изменения в документе «' + name + '»?',
      buttons: en ? ['Save', 'Don’t Save', 'Cancel'] : ['Сохранить', 'Не сохранять', 'Отмена'], defaultId: 0, cancelId: 2, noLink: true,
    });
    return ['save', 'discard', 'cancel'][result.response] || 'cancel';
  });
  ideLogger = logger || null;
  ipcMain.on('ide-log', (_event, payload) => {
    if (!payload || typeof payload !== 'object') return;
    const level = payload.level === 'error' || payload.level === 'warn' ? payload.level : 'info';
    try { ideLogger?.write(level, `renderer.${String(payload.event || 'event')}`, payload.data); } catch {}
  });
  ipcMain.handle('stop-run', async () => {
    // The ConPTY session is the only run path — Stop kills it; with nothing
    // running this is a no-op.
    killActivePty();
  });

  // Integrated console: start a ConPTY run, then feed it keystrokes and size.
  ipcMain.handle('run-console', async (_e, filePath) => startConsoleRun(filePath));
  ipcMain.handle('pty-write', async (_e, data) => {
    logIpc('pty.write', { length: String(data ?? '').length });
    if (!activePty) return false;
    try { activePty.write(String(data)); return true; } catch { return false; }
  });
  ipcMain.handle('pty-resize', async (_e, cols, rows) => {
    logIpc('pty.resize', { cols, rows });
    if (!activePty) return;
    try { activePty.resize(Math.max(2, cols | 0), Math.max(2, rows | 0)); } catch {}
  });

  // Console paste: the renderer reads the clipboard through the main process —
  // navigator.clipboard.readText() hangs forever in Chromium when the clipboard holds
  // an image with no text part; Electron's own clipboard.readText() just returns ''.
  ipcMain.handle('read-clipboard-text', async () => {
    try { return clipboard.readText(); } catch { return ''; }
  });
  ipcMain.handle('write-clipboard-text', async (_e, text) => {
    if (typeof text !== 'string') return false;
    try {
      clipboard.writeText(text);
      logIpc('clipboard.write', { length: text.length });
      return true;
    } catch (e) {
      logIpc('clipboard.write.failed', { message: e?.message || String(e) });
      return false;
    }
  });

  // Локальный справочник Axiom открывается отдельным окном без Node.js и API IDE.
  ipcMain.handle('open-manual', async (event) => {
    if (event.senderFrame !== event.sender.mainFrame || !event.senderFrame.url.endsWith('/index.html'))
      return { ok: false, message: 'Недопустимый источник команды.' };
    const owner = BrowserWindow.fromWebContents(event.sender);
    try {
      await manualWindow.open(owner);
      return { ok: true };
    } catch (e) {
      logIpc('manual.open.failed', { message: e?.message || String(e) });
      return { ok: false, message: e?.message || String(e) };
    }
  });

  ipcMain.handle('open-log-folder', async () => {
    const directory = path.join(app.getPath('userData'), 'logs');
    try {
      fs.mkdirSync(directory, { recursive: true });
      const error = await shell.openPath(directory);
      logIpc(error ? 'logs.open.failed' : 'logs.opened', { directory, error: error || null });
      return { ok: !error, directory, error: error || null };
    } catch (e) {
      logIpc('logs.open.exception', { directory, message: e?.message || String(e) });
      return { ok: false, directory, error: e?.message || String(e) };
    }
  });

  // Журнал IDE: чтение, очистка и открытие ограничены собственными файлами IdeLogger
  // в каталоге профиля и не влияют на вывод программы.
  const { EventLog } = require('./event-log');
  let eventLog = null;
  const getEventLog = () => eventLog || (eventLog = new EventLog(path.join(app.getPath('userData'), 'logs')));
  ipcMain.handle('read-event-log', async (event, options) => {
    if (!fromMainFrame(event)) return { ok: false, error: 'Недопустимый источник команды.' };
    try { return { ok: true, ...getEventLog().read(options && typeof options === 'object' ? options : {}) }; }
    catch (e) { return { ok: false, error: e?.message || String(e) }; }
  });
  ipcMain.handle('clear-event-log', async (event) => {
    if (!fromMainFrame(event)) return { ok: false, error: 'Недопустимый источник команды.' };
    try { return getEventLog().clear(); }
    catch (e) { return { ok: false, errors: [{ file: '', message: e?.message || String(e) }] }; }
  });
  ipcMain.handle('open-event-log-file', async (event, name) => {
    if (!fromMainFrame(event)) return { ok: false, error: 'Недопустимый источник команды.' };
    try {
      const file = getEventLog().resolveFile(name);
      const error = await shell.openPath(file);
      logIpc(error ? 'logs.file.failed' : 'logs.file.opened', { file, error: error || null });
      return { ok: !error, file, error: error || null };
    } catch (e) { return { ok: false, error: e?.message || String(e) }; }
  });

  ipcMain.handle('get-version', async () => {
    const axiom = resolveAxiom();
    let ide = null;
    try {
      const pkg = require(path.join(__dirname, '..', '..', 'package.json'));
      // app.getVersion() returns the Electron runtime version in dev mode
      // (`electron .`), so the IDE's own version comes from package.json;
      // the fallback is only for packaged builds, where it is the app version.
      ide = pkg.version;
    } catch { ide = null; }
    const base = { electron: process.versions.electron, node: process.versions.node, ide: ide || app.getVersion(), language: null, compiler: null };
    if (!axiom) return base;
    return new Promise((resolve) => {
      const child = spawn(axiom, ['metadata'], { windowsHide: true, env: buildEnv() });
      const timer = setTimeout(() => child.kill(), 10000);
      let out = '';
      let err = '';
      child.stdout.on('data', (d) => (out += d.toString()));
      child.stderr.on('data', (d) => (err += d.toString()));
      child.on('error', () => { clearTimeout(timer); resolve(base); });
      child.on('close', () => {
        clearTimeout(timer);
        try {
          const metadata = JSON.parse(out);
          const version = value => typeof value === 'string' && /^\d+\.\d+\.\d+$/.test(value) ? value : null;
          resolve({ ...base, language: version(metadata.languageVersion), compiler: version(metadata.compilerVersion) });
        } catch { resolve(base); }
      });
    });
  });

  ipcMain.handle('read-file-content', async (_e, filePath) => fs.readFileSync(filePath, 'utf-8'));
  ipcMain.handle('file-readonly', async (_e, filePath) => {
    try { await fs.promises.access(filePath, fs.constants.W_OK); return false; }
    catch (error) {
      if (['EPERM', 'EACCES'].includes(error.code)) return true;
      throw error;
    }
  });
  ipcMain.handle('write-file', async (event, filePath, content, expectedText) => {
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) return false;
    activeFileWrites.set(filePath, (activeFileWrites.get(filePath) || 0) + 1);
    try {
      const saved = await require('./file-service').writeDocumentFile(filePath, content, expectedText, async missing => {
        const result = await dialog.showMessageBox(owner, {
          type: 'warning', title: 'Сохранение файла',
          message: missing ? 'Файл удалён с диска. Создать его заново?' : 'Файл изменён на диске. Заменить внешние изменения текстом редактора?',
          detail: filePath, buttons: ['Заменить', 'Отмена'], defaultId: 1, cancelId: 1,
        });
        return result.response === 0;
      }, payload => require('./backend-ipc').writeLockedFile(payload));
      if (saved === null) return null;
      watcherNoteSelfWrite(filePath); // own writes must not trigger the external-change prompt
      return true;
    } catch {
      return false;
    } finally {
      const remaining = activeFileWrites.get(filePath) - 1;
      if (remaining) activeFileWrites.set(filePath, remaining); else activeFileWrites.delete(filePath);
    }
  });

  ipcMain.handle('watch-file', async (_e, filePath) => {
    if (!filePath || filePath.startsWith('untitled') || watchedFiles.has(filePath)) return;
    const info = statOf(filePath);
    watchedFiles.set(filePath, {
      exists: info.exists,
      mtimeMs: info.mtimeMs || 0,
      size: info.size || 0,
      lastNotifiedMtime: info.mtimeMs || 0,
      deletedNotified: false,
    });
    ensureWatchLoop();
  });

  ipcMain.handle('unwatch-file', async (_e, filePath) => {
    watchedFiles.delete(filePath);
  });

  ipcMain.handle('open-file-dialog', async event => {
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) return null;
    // Start in the last visited folder; a stale dir is dropped (system default instead)
    const settings = loadSettings();
    const defaultPath = resolveDefaultDir(settings.lastOpenDir, (p) => fs.existsSync(p));
    const r = await dialog.showOpenDialog(owner, {
      defaultPath,
      properties: ['openFile'],
      filters: [{ name: 'Axiom Sources', extensions: ['axm'] }, { name: 'All Files', extensions: ['*'] }],
    });
    const picked = r.filePaths[0] || null;
    const dir = dirOfPickedFile(picked);
    if (dir && dir !== settings.lastOpenDir) {
      settings.lastOpenDir = dir;
      saveSettings();
    }
    return picked;
  });

  ipcMain.handle('open-project-dialog', async event => {
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) return null;
    const settings = loadSettings();
    const defaultPath = resolveDefaultDir(settings.lastProjectDir, (p) => fs.existsSync(p));
    const r = await dialog.showOpenDialog(owner, { defaultPath, properties: ['openDirectory'] });
    const picked = r.filePaths[0] || null;
    if (picked && picked !== settings.lastProjectDir) {
      settings.lastProjectDir = picked;
      saveSettings();
    }
    return picked;
  });

  ipcMain.handle('list-project-files', async (_e, projectDir) => {
    return listProjectFiles(projectDir);
  });

  ipcMain.handle('save-file-dialog', async (event, defaultPath) => {
    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) return null;
    const r = await dialog.showSaveDialog(owner, {
      defaultPath,
      filters: [{ name: 'Axiom Sources', extensions: ['axm'] }, { name: 'All Files', extensions: ['*'] }],
    });
    const picked = r.filePath || null;
    // Saving somewhere also counts as "having been there" — remember it for Open
    const dir = dirOfPickedFile(picked);
    if (dir) {
      const settings = loadSettings();
      if (dir !== settings.lastOpenDir) {
        settings.lastOpenDir = dir;
        saveSettings();
      }
    }
    return picked;
  });

  const recovery = new (require('./recovery-store').RecoveryStore)(app.getPath('userData'));
  ipcMain.handle('get-recovery', () => recovery.load()?.tabs ?? null);
  ipcMain.on('save-recovery-sync', (event, tabs) => {
    try { event.returnValue = { ok: true, sequence: recovery.save(tabs) }; }
    catch (error) { event.returnValue = { ok: false, message: error.message }; }
  });
  ipcMain.handle('get-settings', async () => loadSettings());
  ipcMain.handle('save-settings', async (_e, patch) => {
    const settings = loadSettings();
    const previous = { ...settings };
    Object.assign(settings, patch);
    const ok = saveSettings();
    if (!ok && settingsStore) settingsStore.settings = previous;
    return { ok };
  });
  // Synchronous variant for window teardown: beforeunload cannot await
  // an async invoke, so the final openTabs snapshot travels over sendSync.
  ipcMain.on('save-settings-sync', (e, patch) => {
    Object.assign(loadSettings(), patch);
    saveSettings();
    e.returnValue = true;
  });
  ipcMain.handle('add-recent-file', async (_e, filePath) => {
    const s = loadSettings();
    s.recentFiles = [filePath, ...s.recentFiles.filter((f) => f !== filePath)].slice(0, 12);
    saveSettings();
    return s.recentFiles;
  });

  // Профиль читается до создания окна: перенос прежнего профиля пишет window-state.json,
  // который учитывается при выборе размеров и максимизации главного окна.
  loadSettings();
}

module.exports = { setupIpc, killActivePty, parseDiagnostics };
