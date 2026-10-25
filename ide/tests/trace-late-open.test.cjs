const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// Переход отладчика к закрытой зависимости читает файл асинхронно. Пока чтение идёт,
// пользователь может остановить программу, перейти к другой остановке или открыть
// другой документ. Запоздалое открытие не должно подменять активную модель.
function setup() {
  const reads = new Map(), rejectedReads = new Map(), readCounts = new Map(), activated = [], alerts = [];
  const context = { window: { axiom: {
    readFileContent: p => new Promise((resolve, reject) => {
      readCounts.set(p, (readCounts.get(p) || 0) + 1);
      reads.set(p, resolve); rejectedReads.set(p, reject);
    }),
    fileReadonly: async () => false, watchFile: async () => {}, addRecentFile: async () => [],
  } }, alert: message => alerts.push(message), I18N: { t: key => key, current: 'ru', setText: () => {} } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/trace-controller.js'), 'utf8'), context);
  const model = (text) => ({
    getValue: () => text, isDisposed: () => false, decorations: [],
    deltaDecorations(old, next) { this.decorations = next; return next.map((_, index) => index + 1); },
  });
  const main = { path: 'main.axm', documentId: 'main-id', model: model('PRINT 1') };
  let current = main.model;
  const ide = Object.create(context.window.AxiomIDE.prototype);
  Object.assign(ide, { _runFile: 'main.axm', activeFile: 'main.axm', openFiles: new Map([['main.axm', main]]),
    settings: { smartIdentifierEnabled: false },
    editor: { getModel: () => current, revealLineInCenter() {}, setPosition() {}, focus() {}, updateOptions() {},
      saveViewState() { return null; }, restoreViewState() {} },
    monaco: { Range: class { constructor(a, b, c, d) { Object.assign(this, { a, b, c, d }); } },
      Uri: { file: p => p }, editor: { getModel: () => null, createModel: text => model(text) } },
    execution: { stopId: 'one', snapshotForFile: file => ({
      'main.axm': { documentId: 'main-id', sourceText: 'PRINT 1' },
      'helper.axm': { documentId: 'helper-id', sourceText: 'PRINT 1' } })[file] },
    setDebugCurrentElement() {},
    createTab() {},
    switchToFile(p) { const entry = this.openFiles.get(p); if (!entry) return;
      current = entry.model; this.activeFile = p; activated.push(p); },
    scheduleLiveCheck() {}, renderRecentFiles() {}, log() {}, formatWholeDocument: async () => {} });
  const trace = Object.create(context.window.TraceController.prototype);
  trace.ide = ide;
  return { context, ide, trace, reads, rejectedReads, readCounts, alerts, activated, main,
    pause: (stopId, sourceFilePath) => ({ stopId, sourceFilePath, line: 1, column: 1 }) };
}

test('Stop отменяет запоздалое открытие закрытой зависимости', async () => {
  const { ide, trace, reads, activated, pause } = setup();
  const opening = trace.mark(pause('one', 'helper.axm'));
  ide.execution.stopId = null;
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.deepEqual(activated, []);
  assert.equal(ide.openFiles.has('helper.axm'), false);
  assert.equal(ide.activeFile, 'main.axm');
});

test('Новая остановка отменяет переход прежней остановки', async () => {
  const { ide, trace, reads, activated, main, pause } = setup();
  const opening = trace.mark(pause('one', 'helper.axm'));
  ide.execution.stopId = 'two';
  trace.mark(pause('two', 'main.axm'));
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.deepEqual(activated, ['main.axm']);
  assert.equal(ide.openFiles.has('helper.axm'), false);
  assert.equal(ide.activeFile, 'main.axm');
  assert.equal(main.model.decorations.length, 1);
});

test('Намеренно открытый пользователем документ остаётся активным без повторного чтения', async () => {
  const { ide, trace, reads, readCounts, pause } = setup();
  const opening = trace.mark(pause('one', 'helper.axm'));
  const userOpening = ide.openFile('c.axm');
  reads.get('c.axm')('PRINT 3');
  await userOpening;
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.equal(readCounts.get('helper.axm'), 1);
  assert.equal(ide.activeFile, 'c.axm');
  assert.equal(ide.openFiles.has('c.axm'), true);
  assert.equal(ide.openFiles.has('helper.axm'), false);
});

test('Ошибка чтения зависимости не запускает повторное чтение и сообщения', async () => {
  const { ide, trace, rejectedReads, readCounts, alerts, pause } = setup();
  const opening = trace.mark(pause('one', 'helper.axm'));
  rejectedReads.get('helper.axm')(new Error('Недоступный файл'));
  await opening;
  assert.equal(readCounts.get('helper.axm'), 1);
  assert.equal(alerts.length, 1);
  assert.equal(ide.openFiles.has('helper.axm'), false);
});

test('Восстановление без stopId открывает зависимость и ставит маркер', async () => {
  const { ide, trace, reads } = setup();
  const opening = trace.mark({ sourceFilePath: 'helper.axm', line: 1, column: 1 });
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.equal(ide.activeFile, 'helper.axm');
  assert.equal(ide.openFiles.get('helper.axm').model.decorations.length, 1);
});

test('Новая остановка отменяет восстановление без stopId', async () => {
  const { ide, trace, reads, readCounts, activated } = setup();
  const opening = trace.mark({ sourceFilePath: 'helper.axm', line: 1, column: 1 });
  ide.execution.stopId = 'two';
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.equal(readCounts.get('helper.axm'), 1);
  assert.deepEqual(activated, []);
  assert.equal(ide.openFiles.has('helper.axm'), false);
});

test('Обычное чтение открывает зависимость и ставит маркер остановки', async () => {
  const { ide, trace, reads, activated, pause } = setup();
  const opening = trace.mark(pause('one', 'helper.axm'));
  reads.get('helper.axm')('PRINT 1');
  await opening;
  assert.equal(activated.at(-1), 'helper.axm');
  assert.equal(ide.activeFile, 'helper.axm');
  assert.equal(ide.openFiles.get('helper.axm').model.decorations.length, 1);
});

for (const previousStop of [null, 'previous']) {
  test(`Событие debug.paused публикует новую остановку до перехода из ${previousStop}`, async () => {
    const { context, ide, trace, reads, pause } = setup();
    vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
    const execution = Object.create(context.window.ExecutionController.prototype);
    Object.assign(execution, { ide, sessionId: 'session', stopId: previousStop,
      snapshotForFile: ide.execution.snapshotForFile, renderOutput() {} });
    ide.execution = execution; ide.trace = trace; ide.setStatus = () => {};
    trace.update = () => {};
    let opening;
    const mark = trace.mark.bind(trace);
    trace.mark = payload => (opening = mark(payload));
    execution.event({ event: 'debug.paused', sessionId: 'session', payload: pause('next', 'helper.axm') });
    assert.equal(reads.has('helper.axm'), true, 'Настоящий обработчик события должен начать чтение закрытого файла');
    reads.get('helper.axm')('PRINT 1');
    await opening;
    assert.equal(execution.stopId, 'next');
    assert.equal(ide.activeFile, 'helper.axm');
    assert.equal(trace.marker.model, ide.openFiles.get('helper.axm').model);
  });
}

test('Фоновое открытие проекта и щелчок по файлу не создают вторую модель и не теряют правки',async()=>{
  const {context,ide}=setup();const pending=[];
  context.window.axiom.readFileContent=()=>new Promise(resolve=>pending.push(resolve));
  const background=ide.openFile('helper.axm',()=>true,{activate:false});
  const foreground=ide.openFile('helper.axm');
  pending[0]('PRINT 1');await background;
  assert.equal(ide.activeFile,'main.axm','Фоновая вкладка не перехватывает активный редактор');
  // В настоящем DocumentStore путь добавляется при set; этот стенд использует Map.
  ide.openFiles.get('helper.axm').path='helper.axm';
  const model=ide.openFiles.get('helper.axm').model;
  model.getValue=()=> 'PRINT 99';
  pending[1]('PRINT 1');await foreground;
  assert.equal(ide.openFiles.get('helper.axm').model,model);
  assert.equal(model.getValue(),'PRINT 99');assert.equal(ide.activeFile,'helper.axm');
});
