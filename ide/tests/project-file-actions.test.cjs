const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const { ProjectFileActions } = require('../src/main/project-file-actions');
function workspace(t) {
  const base = path.resolve(__dirname, '../../.artifacts/project-file-actions'); fs.mkdirSync(base, { recursive: true });
  const root = fs.mkdtempSync(path.join(base, 'case-')); t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const source = path.join(root, 'исходная папка'), destination = path.join(root, 'папка назначения');
  fs.mkdirSync(source); fs.mkdirSync(destination);
  return { root, source, destination };
}
test('Копирование файла с русским путём сохраняет источник и позволяет повтор', async t => {
  const w = workspace(t), file = path.join(w.source, 'Программа.axm'), content = Buffer.from('PRINT "Привет"\r\n'); fs.writeFileSync(file, content);
  const actions = new ProjectFileActions(); actions.capture(w.source, file, false);
  const result = await actions.paste(w.destination, w.destination);
  assert.equal(result.ok, true); assert.deepEqual(fs.readFileSync(result.target), content); assert.deepEqual(fs.readFileSync(file), content);
  assert.equal(actions.clipboard.source, file);
});
test('Конфликт назначения не перезаписывает файл и не очищает вырезание', async t => {
  const w = workspace(t), file = path.join(w.source, 'same.axm'), target = path.join(w.destination, 'same.axm');
  fs.writeFileSync(file, 'original'); fs.writeFileSync(target, 'keep');
  const actions = new ProjectFileActions(); actions.capture(w.source, file, true);
  const result = await actions.paste(w.destination, w.destination);
  assert.equal(result.ok, false); assert.equal(fs.readFileSync(file, 'utf8'), 'original'); assert.equal(fs.readFileSync(target, 'utf8'), 'keep');
  assert.equal(actions.clipboard.cut, true);
});
test('Перемещение файла переносит точные байты и очищает буфер после успеха', async t => {
  const w = workspace(t), file = path.join(w.source, 'move.axm'); fs.writeFileSync(file, 'move me');
  const actions = new ProjectFileActions(); actions.capture(w.source, file, true);
  const result = await actions.paste(w.destination, w.destination);
  assert.equal(result.ok, true); assert.equal(fs.existsSync(file), false); assert.equal(fs.readFileSync(result.target, 'utf8'), 'move me');
  assert.equal(actions.clipboard, null);
});
test('Папка копируется с вложенными файлами и затем перемещается без перезаписи', async t => {
  const w = workspace(t), folder = path.join(w.source, 'Примеры'); fs.mkdirSync(path.join(folder, 'внутри'), { recursive: true });
  fs.writeFileSync(path.join(folder, 'внутри', 'test.axm'), 'PRINT 42');
  const actions = new ProjectFileActions(); actions.capture(w.source, folder, false);
  const copy = await actions.paste(w.destination, w.destination); assert.equal(copy.ok, true);
  assert.equal(fs.readFileSync(path.join(copy.target, 'внутри', 'test.axm'), 'utf8'), 'PRINT 42');
  const other = path.join(w.root, 'third'); fs.mkdirSync(other);
  actions.capture(w.source, folder, true); const move = await actions.paste(other, other);
  assert.equal(move.ok, true); assert.equal(fs.existsSync(folder), false); assert.equal(fs.existsSync(path.join(move.target, 'внутри', 'test.axm')), true);
});
test('Нельзя выйти из проекта или скопировать папку в собственный потомок', async t => {
  const w = workspace(t), outside = path.join(w.root, 'outside.axm'); fs.writeFileSync(outside, 'private');
  const actions = new ProjectFileActions(); assert.throws(() => actions.capture(w.source, outside, false), /проекта/);
  const folder = path.join(w.source, 'folder'), nested = path.join(folder, 'nested'); fs.mkdirSync(nested, { recursive: true });
  actions.capture(w.source, folder, false); const result = await actions.paste(w.source, nested);
  assert.equal(result.ok, false); assert.equal(fs.existsSync(path.join(nested, 'folder')), false);
});
test('Исчезнувший источник и отказ переноса возвращают честный результат', async t => {
  const w = workspace(t), file = path.join(w.source, 'item.axm'); fs.writeFileSync(file, 'keep');
  const actions = new ProjectFileActions({ transfer: async () => ({ ok: false, error: 'Файл заблокирован' }) });
  actions.capture(w.source, file, true); const failed = await actions.paste(w.destination, w.destination);
  assert.equal(failed.ok, false); assert.match(failed.error, /заблокирован/); assert.equal(actions.clipboard.cut, true);
  fs.unlinkSync(file); const missing = await actions.paste(w.destination, w.destination);
  assert.equal(missing.ok, false); assert.equal(actions.clipboard, null);
});
test('Частичный результат сообщает созданное назначение и сохраняет источник', async t => {
  const w = workspace(t), file = path.join(w.source, 'item.axm'); fs.writeFileSync(file, 'original');
  const actions = new ProjectFileActions({ transfer: async (_source, target) => { fs.writeFileSync(target, 'partial'); return { ok: false, error: 'Отказ чтения' }; } });
  actions.capture(w.source, file, true); const result = await actions.paste(w.destination, w.destination);
  assert.equal(result.ok, false); assert.equal(result.partialTarget, path.join(w.destination, 'item.axm'));
  assert.equal(fs.readFileSync(file, 'utf8'), 'original'); assert.equal(actions.clipboard.cut, true);
});
test('Свойства возвращают имя, полный путь, тип, размер и дату существующего файла', async t => {
  const w = workspace(t), file = path.join(w.source, 'свойства.axm'), bytes = Buffer.from('Привет'); fs.writeFileSync(file, bytes);
  const actions = new ProjectFileActions(), result = actions.properties(w.source, file);
  assert.equal(result.name, 'свойства.axm'); assert.equal(result.path, file); assert.equal(result.kind, 'file'); assert.equal(result.size, bytes.length);
  assert.equal(Number.isNaN(Date.parse(result.modified)), false);
});
test('Отказ корзины сохраняет файл; успешная операция не очищает чужой буфер', async t => {
  const w = workspace(t), file = path.join(w.source, 'item.axm'), other = path.join(w.source, 'other.axm'); fs.writeFileSync(file, 'keep'); fs.writeFileSync(other, 'other');
  const actions = new ProjectFileActions({ trash: async () => { throw new Error('Отмена корзины'); } });
  actions.capture(w.source, other, false);
  const result = await actions.trash(w.source, file); assert.equal(result.ok, false); assert.equal(fs.existsSync(file), true);
  actions.trashItem = async target => fs.renameSync(target, path.join(w.root, 'recycled.axm'));
  assert.equal((await actions.trash(w.source, file)).ok, true); assert.equal(fs.existsSync(file), false); assert.equal(actions.clipboard.source, other);
});
async function lockFile(file) {
  const script = "$stream=[System.IO.File]::Open($env:AXIOM_LOCK_FILE,[System.IO.FileMode]::Open,[System.IO.FileAccess]::ReadWrite,[System.IO.FileShare]::None);[Console]::Out.WriteLine('ready');[Console]::Out.Flush();[void][Console]::ReadLine();$stream.Dispose()";
  const child = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-EncodedCommand', Buffer.from(script, 'utf16le').toString('base64')], {
    windowsHide: true, env: { ...process.env, AXIOM_LOCK_FILE: file }, stdio: ['pipe', 'pipe', 'pipe'],
  });
  let output = '', errors = '';
  await new Promise((resolve, reject) => {
    const timer = setTimeout(() => { child.kill(); reject(new Error('Не получено подтверждение блокировки: ' + errors)); }, 10000);
    child.stdout.on('data', data => { output += data; if (output.includes('ready')) { clearTimeout(timer); resolve(); } });
    child.stderr.on('data', data => errors += data);
    child.once('error', error => { clearTimeout(timer); reject(error); });
    child.once('exit', () => { clearTimeout(timer); if (!output.includes('ready')) reject(new Error(errors)); });
  });
  return async () => {
    if (child.exitCode !== null) return;
    await new Promise(resolve => {
      const timer = setTimeout(() => { child.kill(); resolve(); }, 3000);
      child.once('exit', () => { clearTimeout(timer); resolve(); }); child.stdin.end('release\n');
    });
  };
}
test('Настоящая Windows-блокировка не теряет источник и буфер вырезания', async t => {
  const w = workspace(t), file = path.join(w.source, 'locked.axm'); fs.writeFileSync(file, 'keep locked');
  const actions = new ProjectFileActions(), unlock = await lockFile(file);
  try {
    for (const cut of [false, true]) {
      actions.capture(w.source, file, cut); const result = await actions.paste(w.destination, w.destination);
      assert.equal(result.ok, false); assert.equal(fs.existsSync(file), true); assert.equal(actions.clipboard.cut, cut);
    }
  } finally { await unlock(); }
  assert.equal(fs.readFileSync(file, 'utf8'), 'keep locked');
});
test('Назначение появилось во время операции: .NET не перезаписывает его', async t => {
  const w = workspace(t), file = path.join(w.source, 'race.axm'); fs.writeFileSync(file, 'original');
  for (const cut of [false, true]) {
    const actions = new ProjectFileActions(), actual = actions.transfer;
    actions.transfer = async (...args) => { fs.writeFileSync(args[1], 'concurrent'); return actual(...args); };
    actions.capture(w.source, file, cut); const result = await actions.paste(w.destination, w.destination);
    const target = path.join(w.destination, 'race.axm');
    assert.equal(result.ok, false); assert.equal(fs.readFileSync(target, 'utf8'), 'concurrent');
    assert.equal(fs.readFileSync(file, 'utf8'), 'original'); fs.unlinkSync(target);
  }
});
test('Ожидающая файловая операция не допускает второй и снимает занятость после отказа', { timeout: 2000 }, async t => {
  const w = workspace(t), file = path.join(w.source, 'pending.axm'); fs.writeFileSync(file, 'original');
  let release, calls = 0;
  const actions = new ProjectFileActions({ transfer: () => { calls++; return new Promise(resolve => release = resolve); } });
  actions.capture(w.source, file, true); const first = actions.paste(w.destination, w.destination);
  let timer;
  const second = await Promise.race([actions.paste(w.destination, w.destination), new Promise((_, reject) => timer = setTimeout(() => reject(new Error('Вторая команда не отклонена')), 200))]).finally(() => clearTimeout(timer));
  assert.equal(second.ok, false); assert.equal(calls, 1);
  assert.throws(() => actions.capture(w.source, file, false), /операци/);
  release({ ok: false, error: 'Отказ' }); await first; assert.equal(actions.pending, false);
  actions.capture(w.source, file, false); assert.equal(actions.clipboard.cut, false);
});
