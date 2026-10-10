const { test } = require('node:test');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const net = require('node:net');
const { randomUUID } = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const exe = path.join(root, '.artifacts/bin/Axiom.DesktopHost/Debug/net10.0-windows/Axiom.DesktopHost.exe');

class Host {
  constructor(framePipe) {
    assert.ok(fs.existsSync(exe), 'Служебный процесс должен быть предварительно собран');
    this.messages = []; this.nextId = 1; this.stderr = '';
    this.process = spawn(exe, framePipe ? ['--frame-pipe', framePipe] : [], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
    this.process.stdin.on('error', () => {});
    this.process.stderr.on('data', chunk => { this.stderr += chunk; });
    let pending = '';
    this.process.stdout.setEncoding('utf8');
    this.process.stdout.on('data', chunk => {
      pending += chunk;
      while (pending.includes('\n')) {
        const end = pending.indexOf('\n');
        this.messages.push(JSON.parse(pending.slice(0, end)));
        pending = pending.slice(end + 1);
      }
    });
  }
  async wait(predicate) {
    const until = Date.now() + 12000;
    while (Date.now() < until) {
      const index = this.messages.findIndex(predicate);
      if (index >= 0) return this.messages.splice(index, 1)[0];
      if (this.process.exitCode !== null) throw new Error('Host stopped: ' + this.stderr);
      await new Promise(resolve => setTimeout(resolve, 10));
    }
    throw new Error('Не получено событие; ' + this.stderr);
  }
  request(method, payload = {}, sessionId) {
    const id = String(this.nextId++);
    this.process.stdin.write(JSON.stringify({ protocolVersion: 1, kind: 'request', id, method, payload, sessionId }) + '\n');
    return this.wait(message => message.kind === 'response' && message.id === id);
  }
  event(name, sessionId) { return this.wait(m => m.event === name && (!sessionId || m.sessionId === sessionId)); }
  async start(sourceText, sessionId = 's1', extra = {}) {
    const folder = path.join(root, '.artifacts/ide-shell/host-tests', String(process.pid));
    fs.mkdirSync(folder, { recursive: true });
    const sourceFilePath = path.join(folder, sessionId + '.axm');
    fs.writeFileSync(sourceFilePath, sourceText);
    return this.request('execution.start', { documentId: 'd1', documentRevision: 1,
      sourceText, sourceFilePath, resourceBaseDirectory: folder, launchKind: 'run', ...extra }, sessionId);
  }
  async close() {
    if (this.process.exitCode !== null) return;
    try { await this.request('host.shutdown'); } finally {
      this.process.stdin.end();
      await new Promise(resolve => {
        if (this.process.exitCode !== null) return resolve();
        const timer = setTimeout(() => { this.process.kill(); resolve(); }, 3000);
        this.process.once('exit', () => { clearTimeout(timer); resolve(); });
      });
    }
  }
}

async function withHost(fn) {
  const host = new Host();
  try { await host.event('host.ready'); await fn(host); } finally { await host.close(); }
}

test('DesktopHost исполняет точный пример одной VM', () => withHost(async h => {
  const source = fs.readFileSync(path.join(root, 'samples/smoke/bytecode-only/compiler_execution.axm'), 'utf8');
  assert.equal((await h.start(source)).ok, true);
  const finished = await h.event('execution.finished', 's1');
  assert.equal(finished.payload.actualEngine, 'BytecodeVm');
  assert.equal(finished.payload.status, 'Succeeded');
  assert.deepEqual(finished.payload.outputText.replace(/\r\n/g, '\n').trimEnd().split('\n'),
    ['Сумма: 3', '', 'Шаг: 1', 'Шаг: 2', 'MB3007', 'Завершено']);
}));

test('INPUT принимает значение один раз; отмена следующего запроса освобождает сессию', () => withHost(async h => {
  const source = 'DIM value AS INTEGER\nINPUT value\nPRINT value\n';
  await h.start(source);
  const input = await h.event('input.request', 's1');
  const payload = { inputRequestId: input.payload.inputRequestId, text: '2718' };
  assert.equal((await h.request('input.submit', payload, 's1')).ok, true);
  assert.equal((await h.request('input.submit', payload, 's1')).ok, false);
  assert.match((await h.event('execution.finished', 's1')).payload.outputText, /2718/);
  await h.start(source, 's2');
  await h.event('input.request', 's2');
  assert.equal((await h.request('execution.stop', {}, 's2')).ok, true);
  assert.equal((await h.event('execution.finished', 's2')).payload.status, 'Cancelled');
}));

test('Занятый Host не запускает вторую VM и принимает Stop', () => withHost(async h => {
  await h.start('DO\nLOOP\n');
  await h.event('execution.state', 's1');
  const second = await h.start('PRINT 9\n', 's2');
  assert.equal(second.error.code, 'BUSY');
  await h.request('execution.stop', {}, 's1');
  assert.equal((await h.event('execution.finished', 's1')).payload.status, 'Cancelled');
}));

test('Ошибка компиляции не исполняет программу', () => withHost(async h => {
  await h.start('PRINT missing\n');
  const end = await h.event('execution.finished', 's1');
  assert.equal(end.payload.status, 'CompilationFailed');
  assert.equal(end.payload.actualEngine, null);
  assert.equal(end.payload.outputText, '');
}));

test('Отладчик сохраняет точные шаги и отклоняет старый stopId', () => withHost(async h => {
  // Номера относятся к этому короткому исходнику, а не к комментариям большого примера.
  const source = 'DIM result AS INTEGER = 1\nresult = DoubleValue(result)\nPRINT result\n' +
    'FUNCTION DoubleValue(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION\n';
  await h.start(source, 's1', { launchKind: 'debug', stopBeforeFirstInstruction: true });
  let stop = await h.event('debug.paused', 's1');
  assert.equal(stop.payload.line, 1);
  const oldStopId = stop.payload.stopId;
  for (const [method, line] of [['debug.stepOver', 2], ['debug.stepInto', 5], ['debug.stepOut', 2], ['debug.stepBack', 5]]) {
    assert.equal((await h.request(method, { stopId: stop.payload.stopId }, 's1')).ok, true);
    stop = await h.event('debug.paused', 's1');
    assert.equal(stop.payload.line, line);
  }
  assert.equal((await h.request('debug.continue', { stopId: oldStopId }, 's1')).error.code, 'INVALID_STOP');
  await h.request('debug.continue', { stopId: stop.payload.stopId }, 's1');
  assert.equal((await h.event('execution.finished', 's1')).payload.status, 'Succeeded');
}));

test('SCREEN открывает настоящее нативное окно и закрывает его при Stop', () => withHost(async h => {
  await h.start('SCREEN 1\nDO\nLOOP\n');
  const opened = await h.wait(m => m.event === 'graphics.surface' && m.payload.kind === 'screen' && m.payload.windowHandle !== '0');
  assert.ok(BigInt(opened.payload.windowHandle) > 0n);
  await h.request('execution.stop', {}, 's1');
  assert.equal((await h.event('execution.finished', 's1')).payload.status, 'Cancelled');
  const closed = await h.wait(m => m.event === 'graphics.surface' && m.payload.kind === 'screen' && m.payload.state === 'closed');
  assert.equal(closed.payload.windowHandle, '0');
}));

test('CANVAS передаёт BGRA по отдельному каналу и ждёт подтверждения кадра', async () => {
  const pipe = 'axiom-ide-' + randomUUID();
  const h = new Host(pipe);
  let socket;
  try {
    await h.event('host.ready');
    socket = net.connect('\\\\.\\pipe\\' + pipe);
    await new Promise((resolve, reject) => { socket.once('connect', resolve); socket.once('error', reject); });
    let pending = Buffer.alloc(0);
    const frames = [];
    socket.on('data', chunk => {
      pending = Buffer.concat([pending, chunk]);
      while (pending.length >= 4) {
        const headerLength = pending.readUInt32LE(0);
        if (pending.length < 4 + headerLength) break;
        const header = JSON.parse(pending.subarray(4, 4 + headerLength));
        if (pending.length < 4 + headerLength + header.byteLength) break;
        frames.push({ header, pixels: pending.subarray(4 + headerLength, 4 + headerLength + header.byteLength) });
        pending = pending.subarray(4 + headerLength + header.byteLength);
      }
    });
    await h.start('CANVAS 4, 3\nBEGINFRAME\nCLS RGB(255, 0, 0)\nPRESENT\nDO\nLOOP\n');
    const until = Date.now() + 12000;
    while (!frames.length && Date.now() < until) await new Promise(r => setTimeout(r, 10));
    assert.ok(frames.length);
    const first = frames.shift();
    assert.equal(first.header.format, 'BGRA8');
    assert.equal(first.header.sessionId, 's1');
    assert.equal(first.pixels.length, first.header.width * first.header.height * 4);
    await new Promise(r => setTimeout(r, 100));
    assert.equal(frames.length, 0, 'Без ACK второй кадр не передаётся');
    await h.request('graphics.ack', { frameId: first.header.frameId }, 's1');
    while (!frames.length && Date.now() < until) await new Promise(r => setTimeout(r, 10));
    assert.ok(frames.length);
    const red = frames.shift();
    assert.deepEqual([...red.pixels.subarray(0, 4)], [0, 0, 255, 255]);
    await h.request('graphics.ack', { frameId: red.header.frameId }, 's1');
    await h.request('execution.stop', {}, 's1');
    assert.equal((await h.event('execution.finished', 's1')).payload.status, 'Cancelled');
  } finally { socket?.destroy(); await h.close(); }
});

test('Транспорт понимает разделённый UTF-8 и несколько запросов в одном чтении', () => withHost(async h => {
  const request = Buffer.from(JSON.stringify({ protocolVersion: 1, kind: 'request', id: 'fragmented', method: 'host.ping', payload: { text: 'Привет 🌍' } }) + '\n');
  const split = request.indexOf(Buffer.from('🌍')) + 1;
  h.process.stdin.write(request.subarray(0, split));
  h.process.stdin.write(request.subarray(split));
  assert.equal((await h.wait(m => m.id === 'fragmented')).ok, true);
  h.process.stdin.write(['batch1', 'batch2'].map(id => JSON.stringify({ protocolVersion: 1, kind: 'request', id, method: 'host.ping', payload: {} })).join('\n') + '\n');
  assert.equal((await h.wait(m => m.id === 'batch1')).ok, true);
  assert.equal((await h.wait(m => m.id === 'batch2')).ok, true);
  h.process.stdin.write(JSON.stringify({ protocolVersion: 2, kind: 'request', id: 'bad-version', method: 'host.ping' }) + '\n');
  assert.equal((await h.wait(m => m.id === 'bad-version')).error.code, 'PROTOCOL_VERSION_MISMATCH');
  await h.start('PRINT "Привет 🌍"\n');
  assert.equal((await h.event('execution.finished', 's1')).payload.outputText.trim(), 'Привет 🌍');
}));

test('Закрытие stdin отменяет ожидание INPUT и завершает собственный Host', async () => {
  const h = new Host();
  try {
    await h.event('host.ready'); await h.start('DIM value AS INTEGER\nINPUT value\n');
    await h.event('input.request', 's1'); h.process.stdin.end();
    await new Promise((resolve, reject) => {
      const timer = setTimeout(() => reject(new Error('После потери владельца Host не завершился.')), 5000);
      h.process.once('exit', () => { clearTimeout(timer); resolve(); });
    });
    assert.equal(h.process.exitCode, 0);
  } finally { await h.close(); }
});

test('Документ: форматирование и структурная правка возвращают идентичность снимка', async () => {
 const host=new Host();try {
  await host.wait(m=>m.event==='host.ready');
  const snapshot={documentId:'русский-документ',documentRevision:7,sourceText:'IF a = 0 THEN\n',formattingStyle:'Classic'};
  const r=await host.request('document.editTransition',{...snapshot,previousLineNumber:1,currentLineNumber:2,caretOffset:snapshot.sourceText.length});
  assert.equal(r.ok,true);assert.equal(r.payload.documentId,snapshot.documentId);assert.equal(r.payload.documentRevision,7);
  const e=r.payload.edits[0];assert.equal(snapshot.sourceText.slice(0,e.start)+e.newText+snapshot.sourceText.slice(e.start+e.length),'IF a = 0 THEN\n    \nEND IF');
  const formatted=await host.request('document.format',{...snapshot,sourceText:'print 42\n'});
  assert.equal(formatted.ok,true);assert.equal(formatted.payload.sourceText.trim(),'PRINT 42');
  const bad=await host.request('document.format',{...snapshot,documentRevision:-1});assert.equal(bad.ok,false);
 }finally{await host.close();}
});

test('Метаданные Host используют полный общий реестр языка', async()=>{
 const host=new Host();try{await host.wait(m=>m.event==='host.ready');const r=await host.request('language.metadata');
 assert.equal(r.ok,true);assert.ok(r.payload.keywords.includes('CANVAS'));assert.ok(r.payload.builtins.some(f=>f.name==='LEN'));
 }finally{await host.close();}
});

test('Диагностика снимка сохраняет UTF-16 диапазон после Unicode и CRLF',async()=>{
 const host=new Host();try{await host.wait(m=>m.event==='host.ready');const sourceText="' 😀\r\nPRINT missing\r\n";
 const r=await host.request('document.analyse',{documentId:'range',documentRevision:3,sourceText});assert.equal(r.ok,true);
 const d=r.payload.diagnostics.find(d=>d.code==='MB2001');assert.ok(d);assert.equal(d.line,2);assert.equal(d.column,7);assert.equal(d.severity,'error');
 assert.equal(sourceText.slice(d.spanStart,d.spanStart+d.spanLength),'missing');
 const modern=await host.request('document.format',{documentId:'range',documentRevision:4,sourceText:'print 42\n',formattingStyle:'Modern'});assert.equal(modern.payload.sourceText.trim(),'Print 42');
 const missing=await host.request('document.analyse',{documentId:'range',sourceText});assert.equal(missing.ok,false);
 assert.equal((await host.request('host.ping')).ok,true);
 }finally{await host.close();}
});
