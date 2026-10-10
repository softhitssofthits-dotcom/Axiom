// Shared E2E helpers for the Axiom IDE suite (Playwright Electron automation).
const { _electron } = require('playwright');
const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');
const os = require('os');
const { isolatedEnvironment, workspacePath } = require('./isolation.cjs');

const ROOT = path.resolve(__dirname, '..', '..'); // C:\Axiom
const IDE_DIR = path.join(ROOT, 'ide');
const MAIN_JS = path.join(IDE_DIR, 'src', 'main', 'main.js');
const AXIOM = path.join(ROOT, '.artifacts', 'bin', 'Axiom.Cli', 'Release', 'net10.0', 'axiom.exe');
const EVIDENCE = path.join(__dirname, 'evidence');
const SHOTS = path.join(EVIDENCE, 'shots');
const TMP = path.join(ROOT, '.artifacts', 'ide-shell', 'e2e', String(process.pid));

for (const d of [EVIDENCE, SHOTS, TMP]) fs.mkdirSync(d, { recursive: true });

function testWorkspace(tag) {
  const dir = workspacePath(TMP, tag);
  fs.rmSync(dir, { recursive: true, force: true });
  fs.mkdirSync(dir, { recursive: true });
  return dir;
}

function writeAxm(dir, name, content) {
  const p = path.join(dir, name);
  fs.writeFileSync(p, content, 'utf-8');
  return p;
}

// ---------- assertions ----------

class Checks {
  constructor(testId) {
    this.testId = testId;
    this.failures = [];
    this.notes = [];
    this.passed = 0;
  }
  ok(cond, msg, detail) {
    if (cond) { this.passed++; return true; }
    this.failures.push({ msg, detail: detail === undefined ? '' : String(detail) });
    return false;
  }
  eq(actual, expected, msg) {
    return this.ok(actual === expected, msg, actual === undefined ? 'undefined' : `expected [${expected}], got [${actual}]`);
  }
  contains(haystack, needle, msg) {
    return this.ok(typeof haystack === 'string' && haystack.includes(needle), msg, `expected to contain [${needle}], got [${haystack}]`);
  }
  note(msg) { this.notes.push(msg); }
}

// ---------- session (one IDE instance) ----------

class Session {
  constructor(tag) {
    this.tag = tag;
    this.consoleLines = [];
    this.pageErrors = [];
    this.requestFailed = [];
    this.mainStderr = [];
    this.rendererDialogs = [];
    this.confirmAction = 'accept'; // 'accept' | 'dismiss'
    this.userData = null;
    this.electronApp = null;
    this.page = null;
    this.processExit = null;
  }

  async launch(opts = {}) {
    this.userData = opts.userData || path.join(os.tmpdir(), `axiom-ide-e2e-${this.tag}-${process.pid}`);
    fs.mkdirSync(this.userData, { recursive: true });
    // NOTE: with --user-data-dir=X, app.getPath('userData') returns exactly X
    // (no app-name subfolder is appended), so the app reads X\settings.json directly.
    const appDataDir = this.userData;
    fs.mkdirSync(appDataDir, { recursive: true });
    // The console (ConPTY) is the only run mode. Custom settings are still seeded via
    // opts.settings; WITHOUT them an existing settings.json must survive — restart-
    // persistence specs rely on the previous session's writes.
    const settingsPath = path.join(appDataDir, 'settings.json');
    if (opts.settings !== undefined) {
      let seededSettings = {};
      try { seededSettings = typeof opts.settings === 'string' ? JSON.parse(opts.settings) : { ...opts.settings }; } catch { seededSettings = {}; }
      fs.writeFileSync(settingsPath, JSON.stringify(seededSettings), 'utf-8');
    } else if (!fs.existsSync(settingsPath)) {
      fs.writeFileSync(settingsPath, '{}', 'utf-8');
    }
    if (opts.windowState) fs.writeFileSync(path.join(appDataDir, 'window-state.json'), typeof opts.windowState === 'string' ? opts.windowState : JSON.stringify(opts.windowState), 'utf-8');

    const env = isolatedEnvironment(process.env, this.userData, opts.axiomExe !== undefined ? opts.axiomExe : AXIOM);
    fs.mkdirSync(env.APPDATA, { recursive: true });
    fs.mkdirSync(env.LOCALAPPDATA, { recursive: true });
    this.electronApp = await _electron.launch({ args: [MAIN_JS, `--user-data-dir=${this.userData}`], cwd: IDE_DIR, env });
    try { this.electronApp.process().stderr.on('data', (d) => this.mainStderr.push(d.toString())); } catch {}
    this.processExit = new Promise((resolve) => {
      try { this.electronApp.process().once('exit', (code) => resolve(code)); } catch { resolve(null); }
    });
    this.page = await this.electronApp.firstWindow();
    this.page.on('console', (m) => this.consoleLines.push(`[${m.type()}] ${m.text()}`));
    this.page.on('pageerror', (e) => this.pageErrors.push(String((e && e.stack) || e)));
    this.page.on('requestfailed', (r) => this.requestFailed.push(`${r.url()} :: ${(r.failure() && r.failure().errorText) || 'failed'}`));
    this.page.on('dialog', async (d) => {
      this.rendererDialogs.push({ type: d.type(), message: d.message() });
      try {
        if (d.type() === 'confirm' && this.confirmAction === 'dismiss') await d.dismiss();
        else await d.accept();
      } catch (error) {
        if (!/No dialog is showing|Target.*closed/i.test(error.message)) throw error;
      }
    });
    if (opts.allowNotReady) {
      try { await this.page.waitForSelector('#app.ready', { timeout: opts.readyTimeoutMs || 8000 }); } catch { this.notReady = true; }
    } else {
      await this.page.waitForSelector('#app.ready', { timeout: 25000 });
    }
    return this;
  }

  async close() {
    try { if (this.electronApp) await this.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 1 }); }); } catch {}
    try { if (this.electronApp) await this.electronApp.close(); } catch {}
    this.electronApp = null;
    this.page = null;
  }

  async screenshot(name) {
    const file = path.join(SHOTS, `${name}.png`);
    try { await this.page.screenshot({ path: file }); } catch (e) { this.screenshotError = String(e); }
    return file;
  }

  // ---------- main-process dialog stubs ----------

  async stubOpenDialog(paths) {
    await this.electronApp.evaluate(({ dialog }, p) => {
      dialog.showOpenDialog = async () => (p && p.length ? { canceled: false, filePaths: p } : { canceled: true, filePaths: [] });
    }, paths || null);
  }

  async stubSaveDialog(filePath) {
    await this.electronApp.evaluate(({ dialog }, p) => {
      dialog.showSaveDialog = async () => ({ canceled: p == null, filePath: p });
    }, filePath === undefined ? null : filePath);
  }

  // ---------- waiters ----------

  async waitStatus(substr, timeout = 15000) {
    await this.page.waitForFunction(
      (s) => document.getElementById('status-text') && document.getElementById('status-text').textContent.includes(s),
      substr, { timeout, polling: 100 }
    );
  }

  async waitIdle(timeout = 30000) {
    await this.page.waitForFunction(
      () => window.__ide && window.__ide.running === false,
      null, { timeout, polling: 100 }
    );
  }

  async sleep(ms) { await new Promise((r) => setTimeout(r, ms)); }

  // ---------- editor helpers ----------

  // Deterministic full-content replacement. Triggers the same
  // onDidChangeModelContent events as typing (dirty flag + scheduled check),
  // but is immune to Monaco auto-closing pairs / auto-indent.
  async editorType(text) {
    await this.page.evaluate((t) => {
      const ed = window.__ide.editor;
      const model = ed.getModel();
      ed.executeEdits('e2e', [{ range: model.getFullModelRange(), text: t, forceMoveMarkers: true }]);
    }, text);
  }

  async keyboardType(text) {
    await this.page.click('#editor');
    await this.page.keyboard.type(text, { delay: 4 });
  }

  async editorValue() {
    return this.page.evaluate(() => window.__ide.editor.getValue());
  }

  async markers() {
    return this.page.evaluate(() => window.monaco.editor.getModelMarkers({ owner: 'axiom' }).map((m) => ({
      line: m.startLineNumber, col: m.startColumn, message: m.message, severity: m.severity,
    })));
  }

  // ---------- UI state ----------

  async status() { return this.page.evaluate(() => document.getElementById('status-text').textContent); }

  async activeTab() {
    return this.page.evaluate(() => {
      const t = document.querySelector('.editor-tab.active');
      return t ? { path: t.dataset.path, name: t.querySelector('.tab-name').textContent, dirty: t.querySelector('.dirty-indicator').style.display !== 'none' } : null;
    });
  }

  async tabNames() {
    return this.page.evaluate(() => [...document.querySelectorAll('.editor-tab .tab-name')].map((e) => e.textContent));
  }

  async diagnosticsText() {
    return this.page.evaluate(() => document.getElementById('diagnostics-content').innerText.replace(/\r\n/g, '\n').replace(/\r/g, '\n'));
  }

  async diagnosticItems() {
    return this.page.evaluate(() => [...document.querySelectorAll('#diagnostics-content .diagnostic-item')].map((el) => ({
      text: el.innerText.replace(/\r\n/g, '\n').replace(/\r/g, '\n'), icon: el.querySelector('.diagnostic-icon').textContent,
    })));
  }

  async activePanel() {
    return this.page.evaluate(() => {
      const t = document.querySelector('.panel-tab.active');
      return t ? t.dataset.tab : null;
    });
  }

  // ---------- integrated console ----------

  // Full xterm buffer as plain text (one line per row, trailing spaces trimmed).
  async consoleText() {
    return this.page.evaluate(() => {
      const ide = window.__ide;
      if (!ide || !ide.term) return null;
      const buf = ide.term.buffer.active;
      const lines = [];
      for (let i = 0; i < buf.length; i++) {
        lines.push(buf.getLine(i) ? buf.getLine(i).translateToString(true) : '');
      }
      return lines.join('\n').replace(/[ \t]+$/gm, '').replace(/\n{3,}/g, '\n\n');
    });
  }

  async waitConsole(substr, timeout = 20000) {
    await this.page.waitForFunction(
      (s) => {
        const ide = window.__ide;
        if (!ide || !ide.term) return false;
        const buf = ide.term.buffer.active;
        let all = '';
        for (let i = 0; i < buf.length; i++) all += (buf.getLine(i) ? buf.getLine(i).translateToString(true) : '') + '\n';
        return all.includes(s);
      },
      substr, { timeout, polling: 120 }
    );
  }

  // Types into whatever is focused — after a console run that is the xterm view.
  async typeConsole(text, opts = {}) {
    await this.page.keyboard.type(text, { delay: (opts && opts.delay) || 15 });
    await this.page.keyboard.press('Enter');
  }

  async clickMenuItem(menu, action) {
    // Menu interactions occasionally flake under CPU load (the dropdown mousedown lands
    // before the renderer processes it). One Escape-reset + retry makes them robust;
    // the label is a toggle, so a stale open menu must be closed before re-clicking.
    const attempt = async () => {
      await this.page.click(`.menu-item[data-menu="${menu}"] .menu-label`);
      await this.page.waitForSelector(`.menu-item[data-menu="${menu}"] .menu-dropdown-item[data-action="${action}"]`, { state: 'visible', timeout: 3000 });
      await this.page.click(`.menu-item[data-menu="${menu}"] .menu-dropdown-item[data-action="${action}"]`);
    };
    try {
      await attempt();
    } catch (e) {
      await this.page.keyboard.press('Escape').catch(() => {});
      await this.sleep(150);
      await attempt();
    }
  }

  async clickLangAction(action) {
    // The submenu reveals on CSS :hover, which is flaky under Playwright actionability
    // (the item can be visibly rendered yet fail its hit-test). The click handler is a
    // plain listener on .menu-dropdown-item, so a DOM-dispatched click exercises the
    // same app path deterministically — the same discipline as clickTab.
    await this.page.click('.menu-item[data-menu="view"] .menu-label');
    await this.page.waitForFunction(
      (a) => !!document.querySelector(`#lang-submenu .menu-dropdown-item[data-action="${a}"]`),
      action, { timeout: 8000, polling: 100 }
    );
    await this.page.evaluate((a) => {
      const item = document.querySelector(`#lang-submenu .menu-dropdown-item[data-action="${a}"]`);
      item.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }, action);
  }

  // Click a tab by its data-path via DOM lookup (immune to CSS escaping of Windows paths).
  async clickTab(p) {
    await this.page.waitForFunction(
      (path) => [...document.querySelectorAll('.editor-tab')].some((t) => t.dataset.path === path),
      p, { timeout: 8000, polling: 100 }
    );
    await this.page.evaluate((path) => {
      const tab = [...document.querySelectorAll('.editor-tab')].find((t) => t.dataset.path === path);
      tab.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }, p);
  }

  async clickTabClose(p) {
    await this.page.waitForFunction(
      (path) => [...document.querySelectorAll('.editor-tab')].some((t) => t.dataset.path === path),
      p, { timeout: 8000, polling: 100 }
    );
    await this.page.evaluate((path) => {
      const tab = [...document.querySelectorAll('.editor-tab')].find((t) => t.dataset.path === path);
      tab.querySelector('.tab-close').dispatchEvent(new MouseEvent('click', { bubbles: true }));
    }, p);
  }
}

// ---------- axiom process hygiene ----------

// The suite must not silently test a broken binary: when a spec does not override
// axiomExe, the Release binary must exist and identify itself; the version travels in
// the run log so evidence stays honest.
function resolveGuardAxiom() {
  if (!fs.existsSync(AXIOM)) {
    throw new Error(`axiom binary not found at ${AXIOM} — build it first: dotnet build src/Axiom.Cli -c Release`);
  }
  let version = '';
  // axiom prints --version to stderr (stream contract) — capture both streams.
  try { version = execSync(`"${AXIOM}" --version 2>&1`, { encoding: 'utf-8', timeout: 20000, stdio: ['ignore', 'pipe', 'pipe'] }).trim(); } catch {}
  if (!/axiom \d+\.\d+\.\d+/.test(version)) {
    throw new Error(`axiom at ${AXIOM} did not report a version (got: "${version}") — refusing to run against a broken binary`);
  }
  return version;
}

function getAxiomProcesses() {
  try {
    const out = execSync(
      'powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \\"Name=\'axiom.exe\'\\" | Select-Object ProcessId,CommandLine | ConvertTo-Json -Compress"',
      { encoding: 'utf-8', timeout: 20000 }
    ).trim();
    if (!out) return [];
    const parsed = JSON.parse(out.startsWith('[') ? out : `[${out}]`);
    return parsed.map((p) => ({ pid: p.ProcessId, cmd: p.CommandLine || '' }));
  } catch {
    return [];
  }
}

function killStrayAxiom(tmpDir) {
  const procs = getAxiomProcesses();
  for (const p of procs) {
    if (p.cmd.toLowerCase().includes(tmpDir.toLowerCase())) {
      try { execSync(`taskkill /PID ${p.pid} /F`, { timeout: 10000 }); } catch {}
    }
  }
  return procs.length;
}

module.exports = {
  ROOT, IDE_DIR, MAIN_JS, AXIOM, EVIDENCE, SHOTS, TMP,
  Checks, Session, testWorkspace, writeAxm, getAxiomProcesses, killStrayAxiom, resolveGuardAxiom,
};
