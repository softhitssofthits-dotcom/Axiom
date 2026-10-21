const nativeFs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { randomUUID } = require('node:crypto');

const isRecord = value => value !== null && typeof value === 'object' && !Array.isArray(value);
const transientRenameErrors = new Set(['EPERM', 'EACCES', 'EBUSY', 'ETXTBSY']);
const sleepSync = milliseconds => Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, milliseconds);
const parse = text => JSON.parse(text.replace(/^\uFEFF/, ''));
function normalize(value) {
  const settings = { theme: 'dark', language: 'ru', ...value };
  settings.recentFiles = Array.isArray(settings.recentFiles) ? settings.recentFiles.filter(file => typeof file === 'string' && file) : [];
  settings.openTabs = Array.isArray(settings.openTabs) ? settings.openTabs.filter(tab => isRecord(tab) && typeof tab.path === 'string') : [];
  for (const key of ['lastOpenDir', 'lastProjectDir', 'lastProjectFile', 'lastProjectFileDir', 'lastProjectFolderDir']) if (typeof settings[key] !== 'string' || !settings[key]) delete settings[key];
  return settings;
}

// Прежний RecentFilesState переносится только при отсутствии профиля Electron.
// Исходник никогда не записывается; повреждённый текущий файл сохраняется до замены.
class SettingsStore {
  constructor(directory, options = {}) {
    this.directory = directory;
    this.file = path.join(directory, 'settings.json');
    this.legacyPath = options.legacyPath ?? path.join(process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local'), 'Axiom', 'Axiom.Ide', 'recent-files.json');
    this.fs = options.fs || nativeFs;
    this.settings = null;
    this.needsBackup = false;
    this.backup = null;
  }
  load() {
    if (this.settings) return this.settings;
    if (this.fs.existsSync(this.file)) {
      try {
        const value = parse(this.fs.readFileSync(this.file, 'utf8'));
        if (!isRecord(value)) throw new Error('Профиль должен быть объектом.');
        return this.settings = normalize(value);
      } catch {
        this.needsBackup = true;
        this.preserveInvalid();
        return this.settings = normalize({});
      }
    }
    this.settings = normalize({});
    try {
      const old = parse(this.fs.readFileSync(this.legacyPath, 'utf8'));
      if (!isRecord(old)) return this.settings;
      const migrated = {};
      if (Array.isArray(old.RecentFiles)) migrated.recentFiles = old.RecentFiles;
      if (typeof old.LastDirectoryPath === 'string') migrated.lastOpenDir = old.LastDirectoryPath;
      const theme = new Map([['light', 'light'], ['dark', 'dark']]).get(String(old.Theme).toLowerCase());
      const language = new Map([['russian', 'ru'], ['english', 'en']]).get(String(old.Language).toLowerCase());
      if (theme) migrated.theme = theme;
      if (language) migrated.language = language;
      for (const [before, after] of [['IsFilesPaneVisible', 'projectExplorer'], ['IsStatusBarVisible', 'statusBarVisible'], ['SmartIdentifierEnabled', 'smartIdentifierEnabled']]) {
        if (typeof old[before] === 'boolean') migrated[after] = old[before];
      }
      this.settings = normalize(migrated);
      this.settings.legacyImportVersion = 1;
      if (!this.save(this.settings)) return this.settings;
      const windowFile = path.join(this.directory, 'window-state.json');
      if (!this.fs.existsSync(windowFile) && /^(Normal|Maximized)$/i.test(old.WindowState)) {
        this.fs.writeFileSync(windowFile, JSON.stringify({ maximized: /^Maximized$/i.test(old.WindowState) }), { encoding: 'utf8', flag: 'wx' });
      }
    } catch { /* Отсутствующий или повреждённый прежний файл остаётся нетронутым. */ }
    return this.settings;
  }
  preserveInvalid() {
    if (!this.needsBackup || this.backup) return true;
    const target = path.join(this.directory, `settings.invalid-${randomUUID()}.json`);
    try {
      this.fs.copyFileSync(this.file, target, nativeFs.constants.COPYFILE_EXCL);
      this.backup = target;
      return true;
    } catch { return false; }
  }
  save(value) {
    if (!isRecord(value) || !this.preserveInvalid()) return false;
    for (let attempt = 0; attempt < 3; attempt++) {
      const temporary = path.join(this.directory, `settings-${randomUUID()}.tmp`);
      try {
        this.fs.mkdirSync(this.directory, { recursive: true });
        this.fs.writeFileSync(temporary, JSON.stringify(value, null, 2), { encoding: 'utf8', flag: 'wx' });
        this.fs.renameSync(temporary, this.file);
        this.needsBackup = false;
        this.settings = value;
        return true;
      } catch (error) {
        try { this.fs.unlinkSync(temporary); } catch {}
        if (!transientRenameErrors.has(error?.code) || attempt === 2) return false;
        sleepSync(10 * (attempt + 1));
      }
    }
    return false;
  }
}
module.exports = { SettingsStore };
