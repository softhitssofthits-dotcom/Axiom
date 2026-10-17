const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { IdeLogger } = require('../src/main/ide-logger');
const { EventLog } = require('../src/main/event-log');
function workspace(t) {
  const base = path.resolve(__dirname, '../../.artifacts/event-log'); fs.mkdirSync(base, { recursive: true });
  const directory = fs.mkdtempSync(path.join(base, 'case-'));
  t.after(() => fs.rmSync(directory, { recursive: true, force: true }));
  return directory;
}
const record = (level, event, data = {}) => JSON.stringify({ time: '2026-10-16T18:00:00.000Z', level, event, data }) + '\n';
test('Настоящие записи IDE читаются и фильтруются по уровню и русскому тексту', t => {
  const directory = workspace(t), logger = new IdeLogger(directory);
  logger.start(); logger.info('document.open', { path: 'Программа.axm' }); logger.error('document.failed', { reason: 'Ошибка записи' });
  const log = new EventLog(directory);
  assert.equal(log.read().entries.length, 3);
  const selected = log.read({ level: 'error', search: 'ошибка' });
  assert.equal(selected.entries.length, 1); assert.equal(selected.entries[0].event, 'document.failed');
  assert.equal(selected.entries[0].file, 'ide.log');
  assert.deepEqual(log.read({ level: 'warn' }).entries, []);
});
test('Старые и текущие журналы доступны по имени, чужие файлы и пути запрещены', t => {
  const directory = workspace(t), archive = 'ide-2026-10-16T18-00-00-000Z-123-0.log';
  fs.writeFileSync(path.join(directory, archive), record('info', 'old'));
  fs.writeFileSync(path.join(directory, 'ide.log'), record('error', 'current'));
  fs.writeFileSync(path.join(directory, 'private.log'), record('info', 'private'));
  fs.writeFileSync(path.join(directory, 'settings.json'), '{}');
  const log = new EventLog(directory);
  assert.deepEqual(new Set(log.read().files), new Set([archive, 'ide.log']));
  assert.equal(log.read({ file: archive }).entries[0].event, 'old');
  for (const name of ['../settings.json', 'private.log', path.join(directory, 'ide.log'), 'missing.log']) {
    assert.throws(() => log.resolveFile(name), /журнал/);
  }
  assert.equal(log.resolveFile('ide.log'), path.join(directory, 'ide.log'));
});
test('Повреждённая строка не скрывает соседние события', t => {
  const directory = workspace(t);
  fs.writeFileSync(path.join(directory, 'ide.log'), record('info', 'before') + '{broken}\nnull\n' + record('error', 'after'));
  const result = new EventLog(directory).read();
  assert.deepEqual(result.entries.map(entry => entry.event), ['before', 'after']);
  assert.equal(result.invalidLines, 2);
});
test('Просмотр ограничивает объём и возвращает последние события', t => {
  const directory = workspace(t);
  fs.writeFileSync(path.join(directory, 'ide.log'), Array.from({ length: 40 }, (_, n) => record('info', 'event-' + n)).join(''));
  const result = new EventLog(directory, { maxBytes: 400 }).read({ limit: 2 });
  assert.equal(result.truncated, true); assert.deepEqual(result.entries.map(entry => entry.event), ['event-38', 'event-39']);
});
test('Очистка затрагивает только журналы IDE, после неё запись продолжается', t => {
  const directory = workspace(t), logger = new IdeLogger(directory); logger.start(); logger.info('first');
  const archive = path.join(directory, 'ide-2026-10-16T18-00-00-000Z-123-0.log');
  fs.writeFileSync(archive, record('info', 'old')); fs.writeFileSync(path.join(directory, 'private.log'), 'сохранить');
  const log = new EventLog(directory), result = log.clear();
  assert.equal(result.ok, true); assert.deepEqual(log.read().entries, []);
  assert.equal(fs.readFileSync(path.join(directory, 'private.log'), 'utf8'), 'сохранить');
  logger.info('after-clear'); assert.equal(log.read().entries[0].event, 'after-clear');
});
test('Отказ очистки не выдаётся за успех и сохраняет причину', t => {
  const directory = workspace(t); fs.writeFileSync(path.join(directory, 'ide.log'), record('info', 'keep'));
  const log = new EventLog(directory, { fs: { ...fs, truncateSync() { throw new Error('Файл заблокирован'); } } });
  const result = log.clear(); assert.equal(result.ok, false); assert.match(result.errors[0].message, /заблокирован/);
  assert.equal(log.read().entries[0].event, 'keep');
});
test('Отсутствующий каталог журнала даёт пустой доступный просмотр', t => {
  const directory = path.join(workspace(t), 'not-created');
  assert.deepEqual(new EventLog(directory).read().entries, []);
  assert.equal(new EventLog(directory).clear().ok, true);
});
test('Очистка не оставляет старые журналы за пределом объёма просмотра', t => {
  const directory = workspace(t);
  for (let n = 0; n < 15; n++) fs.writeFileSync(path.join(directory, `ide-2026-10-16T18-00-00-000Z-123-${n}.log`), record('info', 'event-' + n));
  const log = new EventLog(directory);
  assert.equal(log.read().truncated, true);
  assert.equal(log.clear().ok, true);
  assert.deepEqual(fs.readdirSync(directory), []);
});
test('Ошибка чтения возвращается с именем журнала', t => {
  const directory = workspace(t); fs.writeFileSync(path.join(directory, 'ide.log'), record('info', 'event'));
  const log = new EventLog(directory, { fs: { ...fs, openSync() { throw new Error('Нет доступа'); } } });
  const result = log.read(); assert.deepEqual(result.entries, []);
  assert.deepEqual(result.errors, [{ file: 'ide.log', message: 'Нет доступа' }]);
});
