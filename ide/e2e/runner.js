// E2E runner for the Axiom IDE.
// Usage: node runner.js [filter-substring]
const fs = require('fs');
const path = require('path');
const { Checks, Session, EVIDENCE, killStrayAxiom, TMP, resolveGuardAxiom } = require('./helpers');

const FILTER = process.argv[2] || '';
const SPEC_DIR = path.join(__dirname, 'specs');

const specFiles = fs.readdirSync(SPEC_DIR).filter((f) => f.endsWith('.spec.js')).sort();
const tests = [];
for (const f of specFiles) {
  const mod = require(path.join(SPEC_DIR, f));
  for (const t of mod) tests.push({ ...t, spec: f });
}

(async () => {
  const results = [];
  // Refuse to run against a missing/self-reported-broken CLI. Specs that deliberately
  // point AXIOM_EXE at a broken binary are exempt via their file name.
  const selected = tests.filter((t) => !FILTER || `${t.spec}/${t.id}`.toLowerCase().includes(FILTER.toLowerCase()));
  if (selected.length && selected.some((t) => t.spec !== 'errors.spec.js')) {
    console.log(`axiom guard: ${resolveGuardAxiom()}`);
  }
  console.log(`Axiom IDE E2E: ${tests.length} tests\n`);
  let idx = 0;
  for (const t of tests) {
    idx++;
    if (FILTER && !`${t.spec}/${t.id}`.toLowerCase().includes(FILTER.toLowerCase())) continue;
    const S = new Session(t.id);
    const C = new Checks(t.id);
    const started = Date.now();
    let crashed = false;
    process.stdout.write(`[${String(idx).padStart(3, '0')}/${tests.length}] ${t.spec} :: ${t.id} ... `);
    try {
      await t.fn({ S, C });
    } catch (e) {
      C.failures.push({ msg: `TEST THREW: ${e.message}`, detail: String((e && e.stack) || e).split('\n').slice(0, 6).join('\n') });
      crashed = true;
    }
    // failure evidence
    if (C.failures.length) {
      try { if (S.page) await S.screenshot(`FAIL_${t.id}`); } catch {}
    }
    // teardown
    try { await S.close(); } catch {}
    killStrayAxiom(TMP);
    const res = {
      id: t.id,
      spec: t.spec,
      name: t.name,
      ms: Date.now() - started,
      passed: C.passed,
      failures: C.failures,
      notes: C.notes,
      crashed,
      consoleTail: S.consoleLines.slice(-25),
      pageErrors: S.pageErrors,
      mainStderrTail: S.mainStderr.slice(-8).join(''),
      rendererDialogs: S.rendererDialogs,
    };
    results.push(res);
    if (C.failures.length) {
      console.log(`FAIL (${C.failures.length})`);
      for (const f of C.failures) console.log(`    ✗ ${f.msg}${f.detail ? `\n      ${String(f.detail).slice(0, 300)}` : ''}`);
    } else {
      console.log(`ok (${C.passed} checks, ${res.ms}ms)`);
    }
  }

  fs.writeFileSync(path.join(EVIDENCE, 'results.json'), JSON.stringify(results, null, 2), 'utf-8');
  const failed = results.filter((r) => r.failures.length);
  console.log(`\n==== ${results.length} executed, ${failed.length} failed ====${failed.length ? '' : ' 🎉'}`);
  for (const f of failed) console.log(`  FAIL ${f.spec} :: ${f.id}`);
  process.exit(failed.length ? 1 : 0);
})().catch((e) => {
  console.error('RUNNER FATAL:', e);
  process.exit(2);
});
