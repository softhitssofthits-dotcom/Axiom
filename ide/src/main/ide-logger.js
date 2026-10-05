const fs = require('fs');
const path = require('path');

const DEFAULT_MAX_FILES = 5;
const DEFAULT_MAX_BYTES = 2 * 1024 * 1024;

function stamp(date = new Date()) {
  return date.toISOString().replace(/[:.]/g, '-');
}

function safeValue(value, depth = 0) {
  if (depth > 3) return '[depth-limit]';
  if (value === null || value === undefined) return value;
  if (typeof value === 'string') return value.length > 4000 ? value.slice(0, 4000) + '…' : value;
  if (typeof value === 'number' || typeof value === 'boolean') return value;
  if (value instanceof Error) return { name: value.name, message: value.message, stack: value.stack };
  if (Array.isArray(value)) return value.slice(0, 100).map((x) => safeValue(x, depth + 1));
  if (typeof value === 'object') {
    const out = {};
    for (const [key, item] of Object.entries(value).slice(0, 100)) out[key] = safeValue(item, depth + 1);
    return out;
  }
  return String(value);
}

class IdeLogger {
  constructor(directory, options = {}) {
    this.directory = directory;
    this.fileName = options.fileName || 'ide.log';
    this.maxFiles = Math.max(1, Number(options.maxFiles) || DEFAULT_MAX_FILES);
    this.maxBytes = Math.max(1024, Number(options.maxBytes) || DEFAULT_MAX_BYTES);
    this.currentPath = path.join(directory, this.fileName);
    this.started = false;
    this.rotationCounter = 0;
  }

  start() {
    if (this.started) return;
    fs.mkdirSync(this.directory, { recursive: true });
    if (fs.existsSync(this.currentPath) && fs.statSync(this.currentPath).size > 0) this.rotateCurrent();
    this.cleanup();
    this.started = true;
    this.write('info', 'logger.started', { maxFiles: this.maxFiles, maxBytes: this.maxBytes });
  }

  write(level, event, data) {
    if (!this.started) this.start();
    const record = {
      time: new Date().toISOString(),
      level: String(level || 'info'),
      event: String(event || 'unknown'),
      data: safeValue(data || {}),
    };
    const line = JSON.stringify(record) + '\n';
    try {
      fs.appendFileSync(this.currentPath, line, 'utf8');
      if (fs.statSync(this.currentPath).size > this.maxBytes) this.rotateCurrent();
    } catch {
      // Logging must never take down the IDE.
    }
  }

  rotateCurrent() {
    try {
      if (!fs.existsSync(this.currentPath)) return;
      const rotated = path.join(this.directory, `ide-${stamp()}-${process.pid}-${this.rotationCounter++}.log`);
      fs.renameSync(this.currentPath, rotated);
      fs.writeFileSync(this.currentPath, '', 'utf8');
      this.cleanup();
    } catch {
      // A locked log should not affect the editor; the next write will retry.
    }
  }

  cleanup() {
    try {
      const prefix = this.fileName.replace(/\.log$/i, '') + '-';
      const files = fs.readdirSync(this.directory)
        .filter((name) => name.startsWith(prefix) && name.endsWith('.log'))
        .map((name) => ({ name, path: path.join(this.directory, name), mtime: fs.statSync(path.join(this.directory, name)).mtimeMs }))
        .sort((a, b) => b.mtime - a.mtime || b.name.localeCompare(a.name));
      for (const item of files.slice(Math.max(0, this.maxFiles - 1))) {
        try { fs.unlinkSync(item.path); } catch {}
      }
    } catch {}
  }

  info(event, data) { this.write('info', event, data); }
  warn(event, data) { this.write('warn', event, data); }
  error(event, data) { this.write('error', event, data); }
}

module.exports = { IdeLogger, safeValue, stamp };
