const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const os = require('node:os');
const { resolveHostPath, stampPathFor, staleness, ensureFreshHost } = require('../src/main/host-ensure');
const root = path.resolve(__dirname, '../..');

// Та же раскладка, что и у настоящей сборки: exe в .artifacts/bin, штамп в .artifacts/obj.
function fixture() {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'axiom-host-ensure-'));
  const exe = path.join(dir, '.artifacts', 'bin', 'Axiom.DesktopHost', 'Debug', 'net10.0-windows', 'Axiom.DesktopHost.exe');
  fs.mkdirSync(path.dirname(exe), { recursive: true });
  return { dir, exe };
}

test('Путь хоста: override AXIOM_DESKTOP_HOST_EXE важнее Debug-сборки', () => {
  const { dir, exe } = fixture();
  fs.writeFileSync(exe, 'x');
  const r = resolveHostPath({ AXIOM_DESKTOP_HOST_EXE: exe }, dir);
  assert.equal(r.exe, exe);
  assert.equal(r.overridden, true);
});

test('Путь хоста: несуществующий override игнорируется, берётся .artifacts/bin', () => {
  const r = resolveHostPath({ AXIOM_DESKTOP_HOST_EXE: path.join(root, '.artifacts/does-not-exist') }, root);
  assert.equal(r.overridden, false);
  assert.equal(r.exe, path.join(root, '.artifacts', 'bin', 'Axiom.DesktopHost', 'Debug', 'net10.0-windows', 'Axiom.DesktopHost.exe'));
});

test('Свежесть: отсутствующий exe — missing; без штампа — stale', () => {
  const { dir, exe } = fixture();
  assert.equal(staleness(exe, dir), 'missing');
  fs.writeFileSync(exe, 'x');
  assert.equal(staleness(exe, dir), 'stale');
});

test('Свежесть: совпадающий отпечаток — fresh, чужой — stale', () => {
  const { dir, exe } = fixture();
  fs.writeFileSync(exe, 'x');
  fs.mkdirSync(path.dirname(stampPathFor(dir)), { recursive: true });
  fs.writeFileSync(stampPathFor(dir), JSON.stringify({ sourceFingerprint: 'abc', protocolVersion: 1 }));
  const opts = { sourceFingerprint: () => 'abc' };
  assert.equal(staleness(exe, dir, opts), 'fresh');
  assert.equal(staleness(exe, dir, { sourceFingerprint: () => 'other' }), 'stale');
});

test('Свежесть: штамп чужого протокола — stale', () => {
  const { dir, exe } = fixture();
  fs.writeFileSync(exe, 'x');
  fs.mkdirSync(path.dirname(stampPathFor(dir)), { recursive: true });
  fs.writeFileSync(stampPathFor(dir), JSON.stringify({ sourceFingerprint: 'abc', protocolVersion: 2 }));
  assert.equal(staleness(exe, dir, { sourceFingerprint: () => 'abc' }), 'stale');
});

test('Пересборка: свежий хост не трогается, отсутствующий собирается', () => {
  const { dir, exe } = fixture();
  let builds = 0;
  const build = () => {
    builds++;
    fs.writeFileSync(exe, 'x');
    fs.mkdirSync(path.dirname(stampPathFor(dir)), { recursive: true });
    fs.writeFileSync(stampPathFor(dir), JSON.stringify({ sourceFingerprint: 'abc', protocolVersion: 1 }));
    return { status: 0 };
  };
  const opts = { env: {}, sourceFingerprint: () => 'abc', build };
  const miss = ensureFreshHost(dir, opts);
  assert.equal(miss.rebuilt, true);
  assert.equal(miss.buildOk, true);
  assert.equal(miss.previousStatus, 'missing');
  assert.equal(miss.status, 'fresh');
  assert.equal(builds, 1);
  const again = ensureFreshHost(dir, opts);
  assert.equal(again.rebuilt, false);
  assert.equal(again.status, 'fresh');
  assert.equal(builds, 1);
});

test('Пересборка: устаревший хост пересобирается, падение сборки сохраняет ошибку', () => {
  const { dir, exe } = fixture();
  fs.writeFileSync(exe, 'x');
  fs.mkdirSync(path.dirname(stampPathFor(dir)), { recursive: true });
  fs.writeFileSync(stampPathFor(dir), JSON.stringify({ sourceFingerprint: 'old', protocolVersion: 1 }));
  const opts = { env: {}, sourceFingerprint: () => 'new', build: () => ({ status: 1, stderr: 'build failed' }) };
  const r = ensureFreshHost(dir, opts);
  assert.equal(r.rebuilt, true);
  assert.equal(r.buildOk, false);
  assert.equal(r.previousStatus, 'stale');
  assert.equal(r.status, 'stale');
  assert.match(r.buildError, /build failed/);
  assert.equal(fs.existsSync(exe), true);
});

test('Пересборка: override не пересобирается никогда', () => {
  const { dir, exe } = fixture();
  fs.writeFileSync(exe, 'x');
  let builds = 0;
  const r = ensureFreshHost(dir, { env: { AXIOM_DESKTOP_HOST_EXE: exe }, build: () => { builds++; return { status: 0 }; } });
  assert.equal(r.status, 'overridden');
  assert.equal(r.rebuilt, false);
  assert.equal(builds, 0);
});

test('Штамп сборки лежит в obj-дереве рядом с перенаправленным выводом', () => {
  assert.equal(stampPathFor(root), path.join(root, '.artifacts', 'obj', 'Axiom.DesktopHost', 'Debug', 'net10.0-windows', 'host-build.json'));
  assert.ok(fs.existsSync(stampPathFor(root)), 'штамп host-build.json должен существовать после сборки хоста');
});
