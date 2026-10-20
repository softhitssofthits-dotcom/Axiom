const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const context = { window: {}, document:{getElementById:()=>({})}, I18N: { t: key => key, setText: () => {} }, performance: { now: () => 0 } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(path.join(__dirname, '../src/renderer/execution-controller.js'), 'utf8'), context);
  const calls = { problems: [], focus: 0 };
  const ide = { running: false, clearDiagnostics() {}, showPanel() {}, switchPanel() {}, setStatus() {},
    ensureConsole: () => true, showIdeProblem(key, text) { calls.problems.push(text); },
    term: { reset() {}, write() {}, focus() { calls.focus++; } },
    trace: { clearMarker() {}, update() {}, finished() {} },
    openFiles: new Map([['file', { documentId: 'document', revision: 1, model: { getValue: () => 'PRINT 42' } }]]) };
  const requests = [];
  let backendFault;
  const api = { onBackendEvent() {}, onBackendFault(fn) { backendFault = fn; }, startExecution(snapshot) {
    return new Promise((resolve, reject) => requests.push({ resolve, reject,snapshot }));
  } };
  return { controller: new context.window.ExecutionController(ide, api), ide, calls, requests,
    fault: () => backendFault({ message: 'DesktopHost завершился' }) };
}

test('Запуск передаёт неизменный текст открытой зависимости',async()=>{
  const {controller,ide,requests}=setup();
  const documents=[{documentId:'helper-id',documentRevision:2,sourceFilePath:'helper.axm',sourceText:'PRINT 2'}];
  ide.openFiles.captureProject=()=>({documents,isCurrent:()=>true});
  const pending=controller.start('file');
  assert.equal(requests[0].snapshot.projectDocuments?.[0].sourceText,'PRINT 2');
  requests[0].resolve({ok:true,payload:{sessionId:'run'}});await pending;
  assert.equal(controller.sourceSnapshot.documents[0].sourceText,'PRINT 2');
});

test('Ошибка зависимости сохраняет текст и идентификатор снимка запуска',()=>{
  const {controller,ide}=setup();let diagnostics;
  ide.focusEditor=()=>{};ide.applyDiagnostics=items=>{diagnostics=items;};controller.renderOutput=()=>{};
  controller.sessionId='run';controller.sourceSnapshot={documentId:'document',file:'file',sourceText:'PRINT Answer()',documents:[{
    documentId:'helper-id',sourceFilePath:'helper.axm',sourceText:'PRINT 1/0',documentRevision:2}]};
  controller.event({sessionId:'run',event:'execution.finished',payload:{status:'RuntimeFailed',outputText:'',diagnostics:[{
    code:'MB3001',sourceFilePath:'helper.axm',line:1,column:8,message:'error'}]}});
  assert.equal(diagnostics[0].executionSnapshot?.documentId,'helper-id');
  assert.equal(diagnostics[0].executionSnapshot?.sourceText,'PRINT 1/0');
});

test('Закрытая при старте зависимость получает фактический текст из службы исполнения',()=>{
  const {controller,ide}=setup();let diagnostics;
  ide.focusEditor=()=>{};ide.applyDiagnostics=items=>{diagnostics=items;};controller.renderOutput=()=>{};
  controller.sessionId='run';controller.sourceSnapshot={documentId:'document',file:'file',sourceText:'PRINT Answer()',documents:[]};
  controller.event({sessionId:'run',event:'execution.finished',payload:{status:'RuntimeFailed',outputText:'',
    sourceDocuments:[{sourceFilePath:'helper.axm',sourceText:'PRINT 1/0'}],diagnostics:[{
      code:'MB3001',sourceFilePath:'helper.axm',line:1,column:8,message:'error'}]}});
  assert.equal(diagnostics[0].executionSnapshot?.sourceText,'PRINT 1/0');
  assert.equal(diagnostics[0].executionSnapshot?.documentId,null);
});

test('Отказ DesktopHost во время подготовки завершает ожидание запуска', async () => {
  const { controller, ide, requests, fault } = setup();
  const pending = controller.start('file');
  controller.pendingStop = true;
  controller.event({ sessionId: 'old', event: 'execution.state', payload: { state: 'Compiling' } });
  fault();
  assert.equal(controller.starting, false);
  assert.equal(controller.pendingStop, false);
  assert.equal(controller.pendingEvents.length, 0);
  assert.equal(ide.running, false);
  requests[0].resolve({ ok: true, payload: { sessionId: 'old' } });
  await pending;
  assert.equal(controller.sessionId, null);
});

for (const ok of [true, false]) {
  test(`Поздний ${ok ? 'успех' : 'отказ'} подготовки после аварии не меняет следующий запуск`, async () => {
    const { controller, ide, requests, fault, calls } = setup();
    const first = controller.start('file');
    fault();
    const second = controller.start('file');
    requests[1].resolve({ ok: true, payload: { sessionId: 'new' } });
    await second;
    calls.problems.length = 0;
    const focus = calls.focus;
    requests[0].resolve(ok ? { ok: true, payload: { sessionId: 'old' } } : { ok: false, error: { message: 'Старый отказ' } });
    await first;
    assert.equal(controller.sessionId, 'new');
    assert.equal(ide.running, true);
    assert.equal(calls.focus, focus);
    assert.deepEqual(calls.problems, []);
  });
}

test('Отклонение команды запуска не оставляет IDE в подготовке и разрешает повтор', async () => {
  const { controller, ide, requests, calls } = setup();
  const pending = controller.start('file');
  requests[0].reject(new Error('Связь прервана'));
  await assert.doesNotReject(pending);
  assert.equal(ide.running, false);
  assert.equal(controller.starting, false);
  assert.deepEqual(calls.problems, ['Связь прервана']);
  const next = controller.start('file');
  requests[1].resolve({ ok: true, payload: { sessionId: 'new' } });
  await next;
  assert.equal(controller.sessionId, 'new');
  assert.equal(ide.running, true);
});

test('Старый ответ не завершает подготовку нового запуска и не забирает его ранние события', async () => {
  const { controller, requests, fault } = setup();
  const first = controller.start('file');
  fault();
  const second = controller.start('file');
  controller.event({ sessionId: 'new', event: 'input.request', payload: { inputRequestId: 'new-input', prompt: '? ', outputText: '' } });
  requests[0].resolve({ ok: true, payload: { sessionId: 'old' } });
  await first;
  assert.equal(controller.starting, true);
  assert.equal(controller.pendingEvents.length, 1);
  requests[1].resolve({ ok: true, payload: { sessionId: 'new' } });
  await second;
  assert.equal(controller.inputRequestId, 'new-input');
  assert.equal(controller.pendingEvents.length, 0);
});

test('Позднее исключение старого запуска не сбрасывает новую сессию', async () => {
  const { controller, ide, requests, fault, calls } = setup();
  const first = controller.start('file');
  fault();
  const second = controller.start('file');
  requests[1].resolve({ ok: true, payload: { sessionId: 'new' } });
  await second;
  calls.problems.length = 0;
  requests[0].reject(new Error('Старая связь прервана'));
  await assert.doesNotReject(first);
  assert.equal(controller.sessionId, 'new');
  assert.equal(ide.running, true);
  assert.deepEqual(calls.problems, []);
});

test('Раннее графическое окно без INPUT не получает поздний фокус терминала', async () => {
  const { controller, requests, calls } = setup();
  const pending = controller.start('file');
  assert.equal(calls.focus, 1, 'Консоль получает фокус до ответа службы');
  calls.focus = 0;
  controller.event({ sessionId: 'run', event: 'graphics.surface', payload: { kind: 'screen', state: 'open', windowVisible: true, windowHandle: '123' } });
  requests[0].resolve({ ok: true, payload: { sessionId: 'run' } });
  await pending;
  assert.equal(calls.focus, 0);
});
