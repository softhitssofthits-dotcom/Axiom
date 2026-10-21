const fs = require('node:fs');
const path = require('node:path');
const { createHash } = require('node:crypto');
const checksum = value => createHash('sha256').update(JSON.stringify(value)).digest('hex');
function validTabs(tabs) {
  if (!Array.isArray(tabs)) return false;
  const paths = new Set();
  return tabs.every(t => {
    if (!t || typeof t.path !== 'string' || !t.path || typeof t.content !== 'string' ||
        typeof t.savedText !== 'string' || typeof t.isDirty !== 'boolean' || typeof t.isActive !== 'boolean') return false;
    const key = t.path.replace(/\//g, '\\').toLowerCase();
    if (paths.has(key)) return false;
    paths.add(key); return true;
  });
}
class RecoveryStore {
  constructor(directory) { this.directory = directory; }
  slotSignature() {
    return [0, 1].map(slot => {
      try {
        const stat = fs.statSync(path.join(this.directory, 'documents-recovery-' + slot + '.json'), { bigint: true });
        return [stat.dev, stat.ino, stat.size, stat.mtimeNs, stat.ctimeNs].join(':');
      } catch { return 'missing'; }
    }).join('|');
  }
  load() {
    let latest = null;
    for (const slot of [0, 1]) {
      try {
        const data = JSON.parse(fs.readFileSync(path.join(this.directory, 'documents-recovery-' + slot + '.json'), 'utf8'));
        if (data.version !== 1 || !Number.isSafeInteger(data.sequence) || data.sequence <= 0 ||
            !validTabs(data.tabs) || data.checksum !== checksum({ version: data.version, sequence: data.sequence, tabs: data.tabs })) continue;
        if (!latest || data.sequence > latest.sequence) latest = data;
      } catch { /* Другой слот сохраняет последний целый снимок. */ }
    }
    return latest;
  }
  save(tabs) {
    if (!validTabs(tabs)) throw new Error('Неверный снимок восстановления документов.');
    const signature = this.slotSignature();
    let previous = this.cachedSignature === signature ? this.cached : null;
    if (!previous) {
      const loaded = this.load();
      if (loaded) previous = { sequence: loaded.sequence, tabs: loaded.tabs, tabsHash: checksum(loaded.tabs) };
    }
    const tabsJson = JSON.stringify(tabs);
    const tabsHash = createHash('sha256').update(tabsJson).digest('hex');
    if (previous && previous.tabsHash === tabsHash) return previous.sequence;
    const sequence = (previous?.sequence || 0) + 1;
    if (!Number.isSafeInteger(sequence)) throw new Error('Исчерпан счётчик восстановления.');
    fs.mkdirSync(this.directory, { recursive: true });
    const key = p => p.replace(/\//g, '\\').toLowerCase();
    const current = new Map(tabs.map(t => [key(t.path), t]));
    const forgets = previous?.tabs.some(t => !current.has(key(t.path)) || (t.isDirty && !current.get(key(t.path)).isDirty));
    const last = forgets ? sequence + 1 : sequence;
    if (!Number.isSafeInteger(last)) throw new Error('Исчерпан счётчик восстановления.');
    // Завершённый отказ от текста или закрытие не должны возвращаться из резервного слота.
    for (let number = sequence; number <= last; number++) {
      const target = path.join(this.directory, 'documents-recovery-' + number % 2 + '.json');
      const temporary = target + '.tmp';
      const fd = fs.openSync(temporary, 'w');
      try {
        const data = '{"version":1,"sequence":' + number + ',"tabs":' + tabsJson + '}';
        const hash = createHash('sha256').update(data).digest('hex');
        fs.writeFileSync(fd, data.slice(0, -1) + ',"checksum":"' + hash + '"}', 'utf8');
        fs.fsyncSync(fd);
      } finally { fs.closeSync(fd); }
      fs.renameSync(temporary, target);
    }
    // В кэше нет изменяемых объектов вызывающей стороны. Загрузка при запуске
    // по-прежнему полностью проверяет оба слота и их контрольные суммы.
    this.cached = { sequence: last, tabsHash, tabs: tabs.map(t => ({ path: t.path, isDirty: t.isDirty })) };
    this.cachedSignature = this.slotSignature();
    return last;
  }
}
module.exports = { RecoveryStore };
