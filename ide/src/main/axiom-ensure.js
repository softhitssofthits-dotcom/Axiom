// Axiom IDE — axiom freshness guard.
// A Release axiom.exe can survive a git pull. The guard therefore compares the git revision
// compiled into axiom.exe (AssemblyMetadata AxiomSourceRevision, exposed via `axiom metadata`)
// with the checkout's HEAD instead of guessing from file mtimes. Consumers: main.js (startup
// guard), ipc.js (resolveAxiom), scripts/ensure-axiom.js (run-ide.bat) and e2e specs.
// Plain Node only (fs/path/child_process) — no Electron APIs.
const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');

// Resolution order: explicit AXIOM_EXE override, then Release, then Debug.
// NOTE: this repo's Directory.Build.props redirects all build outputs to
// .artifacts/bin/<Project>/<Config>/ — the CLI binary lives there, not under src/.
function resolveAxiomPath(env = process.env) {
  if (env.AXIOM_EXE && fs.existsSync(env.AXIOM_EXE)) return { exe: env.AXIOM_EXE, overridden: true };
  const root = path.join(__dirname, '..', '..', '..');
  const candidates = [
    { config: 'Release', exe: path.join(root, '.artifacts', 'bin', 'Axiom.Cli', 'Release', 'net10.0', 'axiom.exe') },
    { config: 'Debug', exe: path.join(root, '.artifacts', 'bin', 'Axiom.Cli', 'Debug', 'net10.0', 'axiom.exe') },
  ];
  for (const c of candidates) {
    if (fs.existsSync(c.exe)) return { exe: c.exe, config: c.config, overridden: false };
  }
  return { exe: null, config: 'Release', overridden: false };
}

// `axiom --version` prints "axiom X.Y.Z (revision …)" — to STDERR (the stream contract:
// stderr carries all compiler messages, stdout is program output only). Returns null when
// the binary cannot be executed or prints nothing recognizable (a broken binary is stale
// by definition).
function installedVersion(exePath) {
  try {
    const r = spawnSync(exePath, ['--version'], { timeout: 15000, encoding: 'utf8', windowsHide: true });
    const out = `${r.stdout || ''}\n${r.stderr || ''}`;
    const m = /^axiom (\d+\.\d+\.\d+)/m.exec(out);
    return m ? m[1] : null;
  } catch {
    return null;
  }
}

function isRevision(value) {
  return typeof value === 'string' && /^[0-9a-f]{7,128}$/i.test(value);
}

// Current checkout identity. null means this is not a Git working tree (for example a packaged
// IDE), so revision freshness cannot be determined and no rebuild is requested for that reason.
function expectedRevision(srcRoot) {
  try {
    const r = spawnSync('git', ['-C', srcRoot, 'rev-parse', 'HEAD'], { timeout: 15000, encoding: 'utf8', windowsHide: true });
    const revision = (r.stdout || '').trim();
    return r.status === 0 && isRevision(revision) ? revision.toLowerCase() : null;
  } catch {
    return null;
  }
}

// `axiom metadata` carries the sourceRevision field compiled into the assembly. An older
// binary without the field is stale.
function installedRevision(exePath) {
  try {
    const r = spawnSync(exePath, ['metadata'], { timeout: 15000, encoding: 'utf8', windowsHide: true });
    if (r.error || r.status !== 0) return null;
    const revision = JSON.parse((r.stdout || '').trim()).sourceRevision;
    return isRevision(revision) ? revision.toLowerCase() : null;
  } catch {
    return null;
  }
}

// 'missing' — no exe; 'broken' — exe exists but reports no usable version; 'stale' — built
// from another revision than the checkout HEAD; 'fresh'. The revision comparison is the
// authoritative signal: Axiom's CLI version constant changes rarely, while every commit
// changes HEAD. opts.* are injection points for tests.
function staleness(exePath, srcRoot, opts = {}) {
  if (!exePath || !fs.existsSync(exePath)) return 'missing';
  const actual = opts.installedVersion ? opts.installedVersion(exePath) : installedVersion(exePath);
  if (!actual) return 'broken';
  try {
    const expected = JSON.parse(fs.readFileSync(path.join(srcRoot, 'versions.json'), 'utf8')).compiler;
    if (actual !== expected) return 'stale';
  } catch { /* В готовой поставке исходный файл версий может отсутствовать. */ }
  const expectedSource = opts.expectedRevision !== undefined ? opts.expectedRevision : expectedRevision(srcRoot);
  if (!expectedSource) return 'fresh'; // no repo layout — nothing to compare against
  const installedSource = opts.installedRevision ? opts.installedRevision(exePath) : installedRevision(exePath);
  return installedSource === expectedSource ? 'fresh' : 'stale';
}

// Rebuild the Release CLI when it is missing, broken, or built from another revision.
// Returns { exe, rebuilt, buildOk, status }; a present-but-stale binary stays available
// after a failed build so the IDE degrades to old behavior instead of losing axiom.
function ensureFreshAxiom(srcRoot, opts = {}) {
  const resolved = resolveAxiomPath(opts.env);
  if (resolved.overridden) {
    // An explicit AXIOM_EXE points at a binary we did not build here (CI artifact, injected
    // broken binary in e2e) — trust the override, never rebuild.
    return { exe: resolved.exe, rebuilt: false, buildOk: true, status: 'overridden' };
  }
  const exePath = resolved.exe || path.join(srcRoot, '.artifacts', 'bin', 'Axiom.Cli', 'Release', 'net10.0', 'axiom.exe');
  const status = staleness(resolved.exe, srcRoot, opts);
  if (status === 'fresh') return { exe: resolved.exe, rebuilt: false, buildOk: true, status };

  const project = path.join(srcRoot, 'src', 'Axiom.Cli', 'Axiom.Cli.csproj');
  // NuGetAudit=false: the advisory-DB fetch must not fail (or stall) the IDE's self-rebuild on
  // an offline / slow-network machine — packages resolve from the local cache either way.
  const r = spawnSync('dotnet', ['build', project, '-c', 'Release', '-p:NuGetAudit=false'], {
    stdio: 'pipe',
    timeout: opts.buildTimeoutMs || 240000,
    shell: false,
  });
  const buildOk = r.status === 0;
  const exe = fs.existsSync(exePath) ? exePath : resolved.exe;
  let finalStatus = status;
  if (buildOk && exe) {
    finalStatus = staleness(exe, srcRoot, opts);
  }
  return { exe, rebuilt: true, buildOk, status: finalStatus, buildError: buildOk ? null : (r.stderr || r.stdout || '').toString().slice(-2000) };
}

module.exports = { resolveAxiomPath, installedVersion, expectedRevision, installedRevision, staleness, ensureFreshAxiom };
