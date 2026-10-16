const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, I18N: { t: key => key } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/trace-controller.js'), 'utf8'), context);
  const app = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8');
  vm.runInContext(app.replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const pending = [], runs = [];
  const ide = Object.create(context.window.AxiomIDE.prototype);
  const trace = Object.create(context.window.TraceController.prototype);
  Object.assign(ide, { running: true, activeFile: 'b.axm', _runFile: 'a.axm',
    openFiles: new Map([['a.axm', { path: 'a.axm' }], ['b.axm', { path: 'b.axm' }]]),
    execution: { sessionId: 'old', stopId: 'paused', sourceSnapshot: {}, stop: () => new Promise(resolve => pending.push(resolve)) },
    setStatus() {}, switchToFile(file) { this.activeFile = file; },
    async run(kind, stopFirst) { runs.push([this.activeFile, kind, stopFirst]); } });
  Object.assign(trace, { ide, update() {}, clearMarker() {} });
  ide.trace = trace;
  return { ide, trace, pending, runs, finish() { ide.running = false; ide.execution.sessionId = null; trace.finished(); } };
}

test('Restart после смены вкладки запускает исходную программу', async () => {
  const { trace, finish, pending, runs } = setup();
  const restart = trace.command('restart');
  finish(); pending[0](); await restart;
  assert.deepEqual(runs, [['a.axm', 'debug', false]]);
  assert.equal(trace.restarting, false);
});

test('Поздний ответ Stop не оставляет Restart ждать завершения уже другой программы', async () => {
  const { ide, trace, finish, pending, runs } = setup();
  const restart = trace.command('restart');
  finish();
  ide.running = true; ide.execution.sessionId = 'new'; ide.execution.sourceSnapshot = {};
  pending[0](); await new Promise(resolve => setImmediate(resolve));
  assert.equal(trace.restarting, false);
  await restart;
  assert.deepEqual(runs, []);
  assert.equal(ide.execution.sessionId, 'new');
});

test('Stop отменяет ожидающий Restart даже после завершения прежней программы', async () => {
  const { ide, trace, finish, pending, runs } = setup();
  const restart = trace.command('restart');
  finish();
  assert.equal(!!trace.allowed('stop'), true);
  await trace.command('stop');
  pending[0](); await restart;
  assert.equal(trace.restarting, false);
  assert.deepEqual(runs, []);
  assert.equal(ide.running, false);
});

test('Закрытие и повторное открытие файла не подменяет документ ожидающего Restart', async () => {
  const { ide, trace, finish, pending, runs } = setup();
  const restart = trace.command('restart');
  finish();
  ide.openFiles.set('a.axm', { path: 'a.axm' });
  pending[0](); await restart;
  assert.deepEqual(runs, []);
});

test('Завершение отменённого Restart не снимает ожидание более новой попытки', async () => {
  const { ide, trace, finish, pending, runs } = setup();
  const first = trace.command('restart');
  finish(); trace.cancelRestart();
  ide.running = true; ide.execution.sourceSnapshot = {}; ide.execution.sessionId = 'new';
  const second = trace.command('restart');
  pending[0](); await first;
  assert.equal(trace.restarting, true);
  finish(); pending[1](); await second;
  assert.equal(trace.restarting, false);
  assert.deepEqual(runs, [['a.axm', 'debug', false]]);
});
