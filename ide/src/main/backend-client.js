const { spawn } = require('node:child_process');
const { EventEmitter } = require('node:events');
const { randomUUID } = require('node:crypto');
const net = require('node:net');

const LIMIT = 16 * 1024 * 1024;
class BackendClient extends EventEmitter {
  constructor({ executable, env = process.env, readyTimeout = 10000, expectedFingerprint }) {
    super();
    this.requests = new Map(); this.nextId = 0; this.closed = false; this.stderr = '';
    this.pending = Buffer.alloc(0); this.framePending = Buffer.alloc(0); this.sequences = new Map();
    this.pipeName = 'axiom-ide-' + randomUUID();
    this.expectedFingerprint = expectedFingerprint;
    this.readyPromise = new Promise((resolve, reject) => { this.resolveReady = resolve; this.rejectReady = reject; });
    // Ошибка может возникнуть до первого вызова ready; сохраняем отклонённый Promise.
    this.readyPromise.catch(() => {});
    this.readyTimer = setTimeout(() => this.fail(Object.assign(new Error('Служебный процесс не ответил за 10 секунд.'), { code: 'HOST_TIMEOUT' })), readyTimeout);
    this.process = spawn(executable, ['--frame-pipe', this.pipeName], { shell: false, windowsHide: true,
      env, stdio: ['pipe', 'pipe', 'pipe'] });
    this.process.stdin.on('error', error => { if (!this.closed) this.fail(error); });
    this.process.on('error', error => this.fail(error));
    this.process.stderr.on('data', bytes => { this.stderr = (this.stderr + bytes.toString('utf8')).slice(-8192); });
    this.process.stdout.on('data', bytes => this.read(bytes));
    this.process.on('exit', (code, signal) => {
      clearTimeout(this.readyTimer);
      if (!this.closed) this.fail(Object.assign(new Error(`Служебный процесс завершился (${code ?? signal}). ${this.stderr}`), { code: 'HOST_DISCONNECTED' }));
      this.frameSocket?.destroy(); this.emit('closed', { code, signal });
    });
  }
  ready() { return this.readyPromise; }
  read(bytes) {
    try {
      this.pending = Buffer.concat([this.pending, bytes]);
      let end;
      while ((end = this.pending.indexOf(10)) >= 0) {
        if (end > LIMIT) throw new Error('Превышен предел сообщения хоста.');
        const message = JSON.parse(this.pending.subarray(0, end).toString('utf8'));
        this.pending = this.pending.subarray(end + 1);
        if (message.protocolVersion !== 1) throw new Error('Несовместимая версия протокола.');
        if (message.kind === 'response') {
          const request = this.requests.get(message.id);
          if (!request) continue;
          clearTimeout(request.timer); this.requests.delete(message.id);
          if (message.ok) request.resolve(message.payload);
          else request.reject(Object.assign(new Error(message.error?.message || 'Ошибка служебного процесса.'), { code: message.error?.code }));
        } else if (message.kind === 'event') {
          if (message.event === 'host.ready') {
            if (message.payload.executionEngine !== 'BytecodeVm') throw new Error('Неожиданный исполнитель.');
            if (this.expectedFingerprint && message.payload.sourceFingerprint !== this.expectedFingerprint)
              throw Object.assign(new Error('Служебный процесс устарел. Выполните dotnet build src/Axiom.DesktopHost.'), { code: 'HOST_STALE' });
            clearTimeout(this.readyTimer); this.connectFrames(); this.resolveReady(message.payload);
          }
          if (message.sessionId) {
            if (!/^\d+$/.test(message.sequence)) throw new Error('Некорректный счётчик события.');
            const sequence = BigInt(message.sequence);
            if (sequence <= (this.sequences.get(message.sessionId) || 0n)) continue;
            this.sequences.set(message.sessionId, sequence);
          }
          this.emit(message.event, message); this.emit('event', message);
        } else throw new Error('Некорректный тип сообщения хоста.');
      }
      if (this.pending.length > LIMIT) throw new Error('Превышен предел сообщения хоста.');
    } catch (error) { this.fail(error); }
  }
  connectFrames() {
    if (this.frameSocket) return;
    this.frameSocket = net.connect('\\\\.\\pipe\\' + this.pipeName);
    this.frameSocket.on('error', error => { if (!this.closed) this.fail(error); });
    this.frameSocket.on('data', bytes => {
      try {
        this.framePending = Buffer.concat([this.framePending, bytes]);
        while (this.framePending.length >= 4) {
          const size = this.framePending.readUInt32LE(0);
          if (size > 65536) throw new Error('Некорректный заголовок кадра.');
          if (this.framePending.length < 4 + size) break;
          const header = JSON.parse(this.framePending.subarray(4, 4 + size).toString('utf8'));
          const length = header.byteLength;
          if (!Number.isSafeInteger(length) || length <= 0 || length > 256 * 1024 * 1024 ||
              !Number.isInteger(header.width) || header.width <= 0 || !Number.isInteger(header.height) || header.height <= 0 ||
              length !== header.width * header.height * 4 || header.stride !== header.width * 4 || header.format !== 'BGRA8')
            throw new Error('Некорректный размер или формат кадра.');
          if (this.framePending.length < 4 + size + length) break;
          const pixels = Buffer.from(this.framePending.subarray(4 + size, 4 + size + length));
          this.framePending = this.framePending.subarray(4 + size + length);
          this.emit('frame', { header, pixels });
        }
      } catch (error) { this.fail(error); }
    });
  }
  async request(method, payload = {}, sessionId, timeout = 15000) {
    await this.ready();
    if (this.closed || this.failure) throw this.failure || new Error('Связь закрыта.');
    const id = String(++this.nextId);
    const json = JSON.stringify({ protocolVersion: 1, kind: 'request', id, method, sessionId, payload });
    if (Buffer.byteLength(json) > LIMIT) throw Object.assign(new Error('Сообщение превышает 16 MiB.'), { code: 'MESSAGE_TOO_LARGE' });
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => { this.requests.delete(id); reject(Object.assign(new Error('Истекло время ответа: ' + method), { code: 'HOST_TIMEOUT' })); }, timeout);
      this.requests.set(id, { resolve, reject, timer });
      this.process.stdin.write(json + '\n');
    });
  }
  fail(error) {
    if (this.failure || this.closed) return;
    this.failure = error; clearTimeout(this.readyTimer); this.rejectReady(error);
    for (const request of this.requests.values()) { clearTimeout(request.timer); request.reject(error); }
    this.requests.clear(); this.emit('fault', { code: error.code || 'HOST_DISCONNECTED', message: error.message });
    this.process?.kill();
  }
  async close() {
    if (this.closed) return;
    if (!this.failure && this.process.exitCode === null) {
      try { await this.request('host.shutdown', {}, undefined, 3000); } catch {}
    }
    this.closed = true; clearTimeout(this.readyTimer); this.frameSocket?.destroy(); this.process.stdin?.end();
    if (this.process.exitCode !== null || !this.process.pid) return;
    await new Promise(resolve => {
      const timer = setTimeout(() => { this.process.kill(); resolve(); }, 5000);
      this.process.once('exit', () => { clearTimeout(timer); resolve(); });
    });
  }
}
module.exports = { BackendClient };
