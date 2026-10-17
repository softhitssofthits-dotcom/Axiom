const nativeFs = require('node:fs');
const path = require('node:path');
const logName = name => typeof name === 'string' && /^ide(?:-[0-9TZ-]+)?\.log$/.test(name);

// Просмотр и очистка ограничены собственными файлами IdeLogger в каталоге профиля.
class EventLog {
  constructor(directory, options = {}) {
    this.directory = path.resolve(directory);
    this.fs = options.fs || nativeFs;
    this.maxBytes = Math.min(2 * 1024 * 1024, Math.max(256, options.maxBytes || 2 * 1024 * 1024));
  }
  files() {
    try {
      return this.fs.readdirSync(this.directory).filter(logName).flatMap(name => {
        try {
          const stat = this.fs.lstatSync(path.join(this.directory, name));
          return stat.isFile() && !stat.isSymbolicLink() ? [{ name, mtime: stat.mtimeMs }] : [];
        } catch { return []; }
      }).sort((a, b) => a.mtime - b.mtime || a.name.localeCompare(b.name));
    } catch (error) {
      if (error.code === 'ENOENT') return [];
      throw error;
    }
  }
  resolveFile(name) {
    if (!logName(name) || !this.files().some(file => file.name === name)) throw new Error('Указан недоступный журнал.');
    return path.join(this.directory, name);
  }
  read(options = {}) {
    const files = this.files();
    const selected = options.file ? files.filter(file => file.name === path.basename(this.resolveFile(options.file))) : files.slice(-10);
    const limit = Number.isInteger(options.limit) ? Math.max(1, Math.min(2000, options.limit)) : 500;
    const level = ['info', 'warn', 'error'].includes(options.level) ? options.level : null;
    const search = typeof options.search === 'string' ? options.search.slice(0, 1000).toLowerCase() : '';
    const entries = [], errors = [];
    let invalidLines = 0, truncated = !options.file && files.length > selected.length;
    for (const file of selected) {
      let descriptor;
      try {
        descriptor = this.fs.openSync(path.join(this.directory, file.name), 'r');
        const size = this.fs.fstatSync(descriptor).size;
        const start = Math.max(0, size - this.maxBytes), buffer = Buffer.alloc(Math.min(size, this.maxBytes));
        const count = this.fs.readSync(descriptor, buffer, 0, buffer.length, start);
        let text = buffer.subarray(0, count).toString('utf8');
        if (start) { truncated = true; text = text.slice(text.indexOf('\n') + 1); }
        for (const line of text.split(/\r?\n/).filter(Boolean)) {
          try {
            const record = JSON.parse(line);
            if (!record || typeof record !== 'object' || typeof record.time !== 'string' || typeof record.event !== 'string' || typeof record.level !== 'string') {
              invalidLines++; continue;
            }
            if ((!level || record.level === level) && (!search || line.toLowerCase().includes(search))) entries.push({
              time: record.time, level: record.level, event: record.event, data: record.data, file: file.name, text: line,
            });
          } catch { invalidLines++; }
        }
      } catch (error) { errors.push({ file: file.name, message: error.message }); }
      finally { if (descriptor !== undefined) this.fs.closeSync(descriptor); }
    }
    if (entries.length > limit) truncated = true;
    return { files: files.map(file => file.name), entries: entries.slice(-limit), invalidLines, truncated, errors };
  }
  clear() {
    const errors = [];
    for (const file of this.files()) {
      try {
        const target = path.join(this.directory, file.name);
        if (file.name === 'ide.log') this.fs.truncateSync(target, 0);
        else this.fs.unlinkSync(target);
      } catch (error) { errors.push({ file: file.name, message: error.message }); }
    }
    return { ok: errors.length === 0, errors };
  }
}
module.exports = { EventLog };
