#!/usr/bin/env node
// Freshness guard, launcher edition: run-ide.bat calls this so a stale Release axiom.exe
// never serves outdated diagnostics. Prints what it does; exits 0 when a usable axiom.exe
// exists afterwards, 1 otherwise.
const path = require('path');
const { resolveAxiomPath, ensureFreshAxiom } = require('../src/main/axiom-ensure');

const srcRoot = path.join(__dirname, '..', '..');
const pre = resolveAxiomPath(process.env);
const r = ensureFreshAxiom(srcRoot, { env: process.env });

if (r.rebuilt) console.log(`Building Axiom CLI (Release; previous binary ${r.status})... ${r.buildOk ? 'done' : 'FAILED'}`);
if (!pre.overridden && pre.exe) console.log(`axiom: ${pre.exe}${r.rebuilt ? ' (replaced by a fresh build)' : ''}`);
else if (r.exe) console.log(`axiom: ${r.exe}`);

if (!r.exe) {
  console.error(pre.overridden
    ? 'AXIOM_EXE is set but does not exist: ' + process.env.AXIOM_EXE
    : 'No axiom.exe after the build. Build it manually: dotnet build src/Axiom.Cli -c Release');
  if (r.buildError) console.error(r.buildError);
  process.exit(1);
}
process.exit(0);
