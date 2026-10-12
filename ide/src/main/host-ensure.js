// Axiom IDE — DesktopHost freshness guard.
// The Electron shell spawns Axiom.DesktopHost.exe as its shared .NET backend
// (backend-ipc.js). Two failure modes survive a git pull: the host was never built (spawn
// ENOENT on the first backend request — the IDE opens with "Не удалось открыть IDE") or it
// was built from older sources (BackendClient rejects host.ready with HOST_STALE because
// the embedded source fingerprint no longer matches the checkout). Same incident family and
// same mechanism as the axiom.exe guard (axiom-ensure.js): the Debug build stamps its
// sources at build time (backend-fingerprint.js writes host-build.json, see the
// PrepareHostBuildStamp target in Axiom.DesktopHost.csproj) and this guard rebuilds while
// that stamp no longer matches. Plain Node only (fs/path/child_process) — no Electron APIs.
const { spawnSync } = require('child_process');
const path = require('path');
const fs = require('fs');
const { sourceFingerprint } = require('./backend-fingerprint');
const { portableRuntime } = require('./portable-runtime');

const HOST_PROJECT = 'Axiom.DesktopHost';
const HOST_CONFIG = 'Debug'; // backend-ipc.js spawns the Debug build; keep in sync
const HOST_TFM = 'net10.0-windows';

// Resolution order: explicit AXIOM_DESKTOP_HOST_EXE override (a binary we did not build
// here — CI artifact, test injection), then the Debug build under .artifacts/bin. The
// repo's Directory.Build.props redirects all build outputs there, not under src/bin.
function resolveHostPath(env = process.env, srcRoot = path.join(__dirname, '..', '..', '..')) {
  if (env.AXIOM_DESKTOP_HOST_EXE && fs.existsSync(env.AXIOM_DESKTOP_HOST_EXE))
    return { exe: env.AXIOM_DESKTOP_HOST_EXE, overridden: true };
  const portable = portableRuntime(srcRoot);
  if (portable) return { exe: portable.host, overridden: true };
  const exe = path.join(srcRoot, '.artifacts', 'bin', HOST_PROJECT, HOST_CONFIG, HOST_TFM, HOST_PROJECT + '.exe');
  return { exe, overridden: false };
}

// The build stamp lives in the intermediate tree next to the redirected output. A missing
// stamp proves nothing — such a host is stale by definition and gets rebuilt.
function stampPathFor(srcRoot) {
  return path.join(srcRoot, '.artifacts', 'obj', HOST_PROJECT, HOST_CONFIG, HOST_TFM, 'host-build.json');
}

// 'missing' — no exe; 'stale' — built from other sources (or no stamp to prove otherwise);
// 'fresh'. opts.* are injection points for tests.
function staleness(exePath, srcRoot, opts = {}) {
  if (!exePath || !fs.existsSync(exePath)) return 'missing';
  let stamp = null;
  try { stamp = JSON.parse(fs.readFileSync(opts.stampPath || stampPathFor(srcRoot), 'utf8')); } catch { return 'stale'; }
  const expected = opts.sourceFingerprint ? opts.sourceFingerprint(srcRoot) : sourceFingerprint(srcRoot);
  return stamp.sourceFingerprint === expected && stamp.protocolVersion === 1 ? 'fresh' : 'stale';
}

// Rebuild the Debug host when it is missing or built from other sources. An explicit
// AXIOM_DESKTOP_HOST_EXE is trusted and never rebuilt. Returns
// { exe, rebuilt, buildOk, status, buildError }; a present-but-stale binary stays available
// after a failed build so the shell can report HOST_STALE instead of losing the backend.
function ensureFreshHost(srcRoot, opts = {}) {
  const resolved = resolveHostPath(opts.env, srcRoot);
  if (resolved.overridden) return { exe: resolved.exe, rebuilt: false, buildOk: true, status: 'overridden' };
  const status = staleness(resolved.exe, srcRoot, opts);
  if (status === 'fresh') return { exe: resolved.exe, rebuilt: false, buildOk: true, status };
  const project = path.join(srcRoot, 'src', HOST_PROJECT, HOST_PROJECT + '.csproj');
  // NuGetAudit=false: the advisory-DB fetch must not fail (or stall) the IDE's self-rebuild
  // on an offline / slow-network machine — packages resolve from the local cache either way.
  const run = opts.build
    ? opts.build(project, HOST_CONFIG)
    : spawnSync('dotnet', ['build', project, '-c', HOST_CONFIG, '-p:NuGetAudit=false'], {
        stdio: 'pipe', timeout: opts.buildTimeoutMs || 240000, shell: false,
      });
  const buildOk = run.status === 0;
  return {
    exe: resolved.exe,
    rebuilt: true,
    buildOk,
    previousStatus: status,
    status: staleness(resolved.exe, srcRoot, opts),
    buildError: buildOk ? null : (run.stderr || run.stdout || '').toString().slice(-2000),
  };
}

module.exports = { resolveHostPath, stampPathFor, staleness, ensureFreshHost, HOST_CONFIG, HOST_TFM };
