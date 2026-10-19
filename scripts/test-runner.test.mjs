import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

function run(group, scriptExit = 0) {
  const base = path.join(root, '.artifacts/test-runner-tests');
  fs.mkdirSync(base, { recursive: true });
  const directory = fs.mkdtempSync(path.join(base, 'case-'));
  for (const folder of ['scripts', 'ide/tests', 'ide/e2e']) {
    fs.mkdirSync(path.join(directory, folder), { recursive: true });
  }
  fs.copyFileSync(path.join(root, 'scripts/test.ps1'), path.join(directory, 'scripts/test.ps1'));
  for (const name of ['first.test.mjs', 'newly-added.test.mjs', 'not-a-test.mjs']) {
    fs.writeFileSync(path.join(directory, 'scripts', name), '');
  }
  fs.writeFileSync(path.join(directory, 'ide/tests/groups.json'), JSON.stringify({ 'ide-unit-all': ['unit.cjs', 'other.cjs'] }));
  fs.writeFileSync(path.join(directory, 'ide/e2e/groups.json'), JSON.stringify({ 'e2e-all': [] }));
  fs.writeFileSync(path.join(directory, 'driver.ps1'), `
param($Group, $ScriptExit)
$global:LASTEXITCODE = 0
function global:dotnet { $global:LASTEXITCODE = 0 }
function global:node {
    ConvertTo-Json -Compress -InputObject @($args) | Add-Content (Join-Path $PSScriptRoot 'calls.jsonl')
    $global:LASTEXITCODE = 0
    if ($args -contains '--test' -and ($args -match '\\.test\\.mjs$')) {
        $global:LASTEXITCODE = [int]$ScriptExit
    }
}
& (Join-Path $PSScriptRoot 'scripts/test.ps1') $Group
if (-not $?) { exit 1 }
exit 0
`);
  const result = spawnSync('powershell', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', path.join(directory, 'driver.ps1'), group, String(scriptExit)], { encoding: 'utf8' });
  assert.ifError(result.error);
  const callsFile = path.join(directory, 'calls.jsonl');
  const calls = fs.existsSync(callsFile)
    ? fs.readFileSync(callsFile, 'utf8').trim().split(/\r?\n/).filter(Boolean).map(line => JSON.parse(line.replace(/^\uFEFF/, '')))
    : [];
  return { ...result, calls };
}

test('scripts-all автоматически находит новые тесты инструментов', () => {
  const result = run('scripts-all');
  assert.equal(result.status, 0, result.stderr || result.stdout);
  assert.equal(result.calls.length, 1);
  assert.equal(result.calls[0][0], '--test');
  assert.deepEqual(result.calls[0].slice(1).map(file => path.basename(file)).sort(), ['first.test.mjs', 'newly-added.test.mjs']);
});

test('all включает тесты инструментов наряду с Node и Electron', () => {
  const result = run('all');
  assert.equal(result.status, 0, result.stderr || result.stdout);
  assert.equal(result.calls.length, 3);
  assert.ok(result.calls.some(args => args.some(arg => arg.endsWith('newly-added.test.mjs'))));
  assert.ok(result.calls.some(args => args.some(arg => arg.endsWith('unit.cjs'))));
  assert.ok(result.calls.some(args => args.includes('ide/e2e/runner.js')));
});

test('all возвращает отказ тестов инструментов и продолжает Electron', () => {
  const result = run('all', 1);
  assert.equal(result.status, 1, result.stderr || result.stdout);
  assert.ok(result.calls.some(args => args.includes('ide/e2e/runner.js')));
});
