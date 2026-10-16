const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, document: { getElementById: () => ({}) }, performance: { now: () => 0 }, I18N: { t: key => key } };
  vm.createContext(context);
  for (const file of ['execution-controller.js', 'trace-controller.js', 'app.js']) {
    vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer', file), 'utf8')
      .replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  }
  const pending = [], calls = { runs: [], status: [], problems: [] };
  const api = { onBackendEvent() {}, onBackendFault() {}, stopExecution: () => new Promise((resolve, reject) => pending.push({ resolve, reject })) };
  const ide = Object.create(context.window.AxiomIDE.prototype);
  Object.assign(ide, { running: true, _runFile: 'a.axm', activeFile: 'a.axm',
    openFiles: new Map([['a.axm', {}]]), term: { write() {} }, focusEditor() {},
    setStatus(text) { calls.status.push(text); }, showIdeProblem(key, message) { calls.problems.push(message); },
    switchToFile(file) { this.activeFile = file; }, async run() { calls.runs.push(this.activeFile); } });
  const execution = ide.execution = new context.window.ExecutionController(ide, api);
  Object.assign(execution, { sessionId: 'old', stopId: 'pause', line: 2, sourceSnapshot: {}, inputRequestId: 'input', inputText: 'ответ' });
  const trace = ide.trace = Object.create(context.window.TraceController.prototype);
  Object.assign(trace, { ide, update() {}, clearMarker() {} });
  const finish = () => execution.event({ sessionId: 'old', event: 'execution.finished', payload: { status: 'Cancelled', outputText: '' } });
  return { ide, execution, trace, calls, pending, finish };
}

for (const rejected of [false, true]) {
  const settle = (p, message) => rejected ? p.reject(new Error(message)) : p.resolve({ ok: false, error: { message } });
  test(`Stop сохраняет работающую сессию и разрешает повтор: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { ide, execution, pending, calls } = setup();
    const operation = execution.stop(); settle(pending[0], 'Остановка не доставлена');
    let accepted;
    await assert.doesNotReject(async () => { accepted = await operation; });
    assert.equal(accepted, false);
    assert.equal(ide.running, true);
    assert.equal(execution.sessionId, 'old');
    assert.equal(execution.stopId, 'pause');
    assert.equal(execution.inputRequestId, 'input');
    assert.equal(execution.inputText, 'ответ');
    assert.equal(calls.status.at(-1), 'Остановка не доставлена');
    const retry = execution.stop(); pending[1].resolve({ ok: true });
    assert.equal(await retry, true);
  });

  test(`Restart не запускает вторую программу после отказа Stop: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { ide, execution, trace, pending, calls } = setup();
    const operation = trace.command('restart'); settle(pending[0], 'Отказ остановки');
    await assert.doesNotReject(operation);
    assert.equal(trace.restarting, false);
    assert.equal(ide.running, true);
    assert.equal(execution.stopId, 'pause');
    assert.deepEqual(calls.runs, []);
    const retry = trace.command('restart'); pending[1].resolve({ ok: true }); finishLater();
    function finishLater() { setImmediate(() => { ide.running = false; execution.sessionId = null; trace.finished(); }); }
    await retry;
    assert.equal(calls.runs.length, 1);
  });

  test(`Завершение программы подтверждает Stop несмотря на поздний отказ: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { trace, pending, calls, finish } = setup();
    const operation = trace.command('restart'); finish(); calls.status.length = 0;
    settle(pending[0], 'Поздний отказ'); await assert.doesNotReject(operation);
    assert.equal(trace.restarting, false);
    assert.deepEqual(calls.runs, ['a.axm']);
    assert.deepEqual(calls.status, []);
  });

  test(`Поздний Stop сохраняет новый INPUT: ${rejected ? 'исключение' : 'ответ'}`, async () => {
    const { execution, pending, calls, finish, ide } = setup();
    const operation = execution.stop(); finish();
    Object.assign(execution, { sessionId: 'new', sourceSnapshot: {}, inputRequestId: 'new-input', inputText: 'новый' });
    ide.running = true; calls.status.length = 0;
    settle(pending[0], 'Прежний отказ'); await assert.doesNotReject(operation);
    assert.equal(ide.running, true);
    assert.equal(execution.sessionId, 'new');
    assert.equal(execution.inputRequestId, 'new-input');
    assert.equal(execution.inputText, 'новый');
    assert.deepEqual(calls.status, []);
  });
}

test('Поздний отказ Stop не заменяет ошибку более новой попытки', async () => {
  const { execution, pending, calls, ide } = setup();
  const first = execution.stop(), second = execution.stop();
  pending[1].resolve({ ok: false, error: { message: 'Новая попытка' } }); await second;
  calls.status.length = 0;
  pending[0].resolve({ ok: false, error: { message: 'Старая попытка' } }); await first;
  assert.equal(ide.running, true);
  assert.equal(execution.debugError, 'Новая попытка');
  assert.deepEqual(calls.status, []);
});

test('Команда IDE Stop не объявляет завершение до ответа службы', async () => {
  const { ide, pending, calls } = setup();
  const operation = ide.stop();
  assert.equal(calls.status.includes('status.stopped'), false);
  pending[0].resolve({ ok: true }); await operation;
});

test('Авария службы не становится поводом для автоматического Restart', async () => {
  const { execution, trace, pending, calls } = setup();
  const operation = trace.command('restart'); execution.fault('Авария службы');
  pending[0].resolve({ ok: false, error: { message: 'Прежняя команда' } });
  await operation;
  assert.equal(trace.restarting, false);
  assert.deepEqual(calls.runs, []);
});
