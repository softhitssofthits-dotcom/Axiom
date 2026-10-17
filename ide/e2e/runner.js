// E2E runner for the Axiom IDE.
// Usage: node runner.js [filter-substring]
//        node runner.js --from <id-substring>   (запуск от первого совпадения до конца)
//        node runner.js --out <file>            (свой файл результата вместо evidence/results.json)
//        node runner.js --group <name>          (логическая группа из groups.json)
//        node runner.js --groups                (список групп и число сценариев)
const fs = require('fs');
const path = require('path');
const { Checks, Session, EVIDENCE, killStrayAxiom, TMP, resolveGuardAxiom } = require('./helpers');

const rawArgs = process.argv.slice(2);
let FILTER = '';
let FROM = '';
let GROUP = '';
let OUT = path.join(EVIDENCE, 'results.json');
for (let i = 0; i < rawArgs.length; i++) {
  const a = rawArgs[i];
  if (a === '--from') { FROM = rawArgs[++i] || ''; }
  else if (a.startsWith('--from=')) { FROM = a.slice('--from='.length); }
  else if (a === '--group') { GROUP = rawArgs[++i] || ''; }
  else if (a.startsWith('--group=')) { GROUP = a.slice('--group='.length); }
  else if (a === '--out') { OUT = path.resolve(rawArgs[++i] || OUT); }
  else if (a.startsWith('--out=')) { OUT = path.resolve(a.slice('--out='.length)); }
  else if (a === '--list' || a === '--groups') { FILTER = a; }
  else if (!FILTER) { FILTER = a; }
}
const SPEC_DIR = path.join(__dirname, 'specs');
const GROUPS_PATH = path.join(__dirname, 'groups.json');

let GROUPS = {};
try { GROUPS = JSON.parse(fs.readFileSync(GROUPS_PATH, 'utf-8')); } catch (e) {
  if (GROUP) { console.error(`Не удалось прочитать ${GROUPS_PATH}: ${e.message}`); process.exit(2); }
}

const specFiles = fs.readdirSync(SPEC_DIR).filter((f) => f.endsWith('.spec.js')).sort();
const tests = [];
for (const f of specFiles) {
  const mod = require(path.join(SPEC_DIR, f));
  for (const t of mod) tests.push({ ...t, spec: f });
}

if (GROUP && !Object.hasOwn(GROUPS, GROUP)) {
  console.error(`Неизвестная группа: ${GROUP}`);
  console.error('Доступные группы: ' + Object.keys(GROUPS).join(', '));
  process.exit(2);
}

// Каталог готовых сценариев: без запуска Electron и проверки бинарных сборок.
if (FILTER === '--list') {
  const ids = new Set();
  for (const test of tests) {
    if (ids.has(test.id)) throw new Error('Повтор идентификатора сценария: ' + test.id);
    ids.add(test.id);
  }
  console.log(JSON.stringify({ suite: 'Axiom Electron IDE', count: tests.length,
    scenarios: tests.map(({ id, spec, name }) => ({ id, spec, name })) }, null, 2));
  process.exit(0);
}

// Список логических групп без запуска приложения.
if (FILTER === '--groups') {
  const known = new Set(specFiles);
  const listed = Object.entries(GROUPS).map(([name, files]) => {
    const missing = files.filter((f) => !known.has(f));
    const count = tests.filter((t) => files.includes(t.spec)).length;
    return { group: name, scenarios: count, specs: files.length, missing };
  });
  console.log(JSON.stringify({ suite: 'Axiom Electron IDE', groups: listed }, null, 2));
  process.exit(0);
}

(async () => {
  const results = [];
  const groupFiles = GROUP ? new Set(GROUPS[GROUP]) : null;
  const matches = (t) => (!groupFiles || groupFiles.has(t.spec)) && (!FILTER || `${t.spec}/${t.id}`.toLowerCase().includes(FILTER.toLowerCase()));
  let startIndex = 0;
  if (FROM) {
    startIndex = tests.findIndex((t) => `${t.spec}/${t.id}`.toLowerCase().includes(FROM.toLowerCase()));
    if (startIndex < 0) { console.error(`--from не найден: ${FROM}`); process.exit(2); }
    console.log(`--from: продолжаю с №${startIndex + 1} ${tests[startIndex].spec} :: ${tests[startIndex].id}`);
  }
  // Refuse to run against a missing/self-reported-broken CLI. Specs that deliberately
  // point AXIOM_EXE at a broken binary are exempt via their file name.
  const selected = tests.filter((t, i) => i >= startIndex && matches(t));
  if (selected.length && selected.some((t) => t.spec !== 'errors.spec.js')) {
    console.log(`axiom guard: ${resolveGuardAxiom()}`);
  }
  console.log(`Axiom IDE E2E: ${tests.length} tests${GROUP ? ` [${GROUP}]` : ''}, will run ${selected.length}\n`);
  let idx = 0;
  for (const t of tests) {
    idx++;
    if (idx - 1 < startIndex) continue;
    if (!matches(t)) continue;
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
    // Сохраняю после каждого сценария, чтобы длинный прогон не терял результат при прерывании.
    try { fs.mkdirSync(path.dirname(OUT), { recursive: true }); fs.writeFileSync(OUT, JSON.stringify(results, null, 2), 'utf-8'); } catch {}
    if (C.failures.length) {
      console.log(`FAIL (${C.failures.length})`);
      for (const f of C.failures) console.log(`    ✗ ${f.msg}${f.detail ? `\n      ${String(f.detail).slice(0, 300)}` : ''}`);
    } else {
      console.log(`ok (${C.passed} checks, ${res.ms}ms)`);
    }
  }

  fs.writeFileSync(OUT, JSON.stringify(results, null, 2), 'utf-8');
  const failed = results.filter((r) => r.failures.length);
  console.log(`\n==== ${results.length} executed, ${failed.length} failed ====${failed.length ? '' : ' 🎉'}`);
  for (const f of failed) console.log(`  FAIL ${f.spec} :: ${f.id}`);
  process.exit(failed.length ? 1 : 0);
})().catch((e) => {
  console.error('RUNNER FATAL:', e);
  process.exit(2);
});
