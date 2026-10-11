#!/usr/bin/env node
// Freshness guard, launcher edition: run-ide.bat calls this so a missing or stale
// Axiom.DesktopHost.exe never reaches the shell (spawn ENOENT on the first backend request,
// or HOST_STALE on host.ready). Prints what it does; exits 0 when a usable host exists
// afterwards, 1 otherwise. Unlike axiom.exe the host is not allowed to run stale: every
// backend request would be rejected, so an unrefreshed binary is a hard failure here.
const path = require('path');
const fs = require('fs');
const { resolveHostPath, ensureFreshHost } = require('../src/main/host-ensure');

const srcRoot = path.join(__dirname, '..', '..');
const pre = resolveHostPath(process.env, srcRoot);
const r = ensureFreshHost(srcRoot, { env: process.env });

if (r.rebuilt) console.log(`Building Axiom.DesktopHost (was ${r.previousStatus}; ${r.buildOk ? 'done' : 'FAILED'})...`);
if (pre.overridden) console.log(`desktop host: ${r.exe} (AXIOM_DESKTOP_HOST_EXE)`);
else console.log(`desktop host: ${r.exe}${r.rebuilt ? ' (rebuilt)' : ''}`);

const unusable = !fs.existsSync(r.exe) || (r.rebuilt && (!r.buildOk || r.status !== 'fresh'));
if (unusable) {
  console.error(
    r.status === 'missing'
      ? 'No Axiom.DesktopHost.exe after the build. Build it manually: dotnet build src/Axiom.DesktopHost/Axiom.DesktopHost.csproj'
      : 'Axiom.DesktopHost.exe is out of date and the automatic Debug rebuild failed. ' +
        'Close running IDE instances and rebuild: dotnet build src/Axiom.DesktopHost/Axiom.DesktopHost.csproj'
  );
  if (r.buildError) console.error(r.buildError);
  process.exit(1);
}
process.exit(0);
