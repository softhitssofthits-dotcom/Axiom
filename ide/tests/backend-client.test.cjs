const { test } = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const { BackendClient } = require('../src/main/backend-client');
const { resolveHostPath } = require('../src/main/host-ensure');
const root = path.resolve(__dirname, '../..');
const executable = resolveHostPath({}, root).exe;

test('Мост получает ready, Unicode-вывод и завершает только свой процесс', async () => {
  const client = new BackendClient({ executable });
  try {
    const ready = await client.ready();
    assert.equal(ready.executionEngine, 'BytecodeVm');
    assert.equal((await client.request('host.ping')).alive, true);
    const file = path.join(root, '.artifacts/ide-shell/client-process-' + process.pid + '.axm');
    const sourceText = 'PRINT "Привет, мост"\n';
    fs.writeFileSync(file, sourceText);
    const end = new Promise(resolve => client.once('execution.finished', resolve));
    await client.request('execution.start', { documentId: 'd1', documentRevision: 1, sourceText,
      sourceFilePath: file, resourceBaseDirectory: path.dirname(file), launchKind: 'run' }, 'client1');
    assert.match((await end).payload.outputText, /Привет, мост/);
  } finally { await client.close(); }
  assert.notEqual(client.process.exitCode, null);
});

test('Отсутствующий процесс возвращает ошибку без замены на CLI', async () => {
  const client = new BackendClient({ executable: path.join(root, '.artifacts/does-not-exist-host.exe') });
  await assert.rejects(client.ready(), /ENOENT|не найден/);
  await client.close();
});

test('Неизвестная команда отклонена; последующий ping остаётся исправен', async () => {
  const client = new BackendClient({ executable });
  try {
    await client.ready();
    await assert.rejects(client.request('invalid.command'), error => error.code === 'UNKNOWN_METHOD');
    assert.equal((await client.request('host.ping')).alive, true);
  } finally { await client.close(); }
});

test('Несовпавший хеш исходников запрещает запуск устаревшей сборки', async () => {
  const client = new BackendClient({ executable, expectedFingerprint: '0'.repeat(64) });
  try { await assert.rejects(client.ready(), error => error.code === 'HOST_STALE'); }
  finally { await client.close(); }
});
