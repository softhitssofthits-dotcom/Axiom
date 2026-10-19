const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { DocumentStore } = require('../src/renderer/documents');
const { DocumentServices } = require('../src/renderer/document-services');

function setup() {
  let text = 'PRINT missing', changed;
  const model = { getValue: () => text, isDisposed: () => false,
    onDidChangeContent: fn => (changed = fn, { dispose() {} }),
    change(value) { text = value; changed(); } };
  const store = new DocumentStore(); store.set('a.axm', { model });
  const pending = [];
  const services = new DocumentServices(store, { documentCommand: (method, snapshot) =>
    new Promise((resolve, reject) => pending.push({ resolve, reject, snapshot })) });
  return { store, model, services, pending };
}

for (const change of ['edit', 'close', 'reopen', 'rename', 'newer']) {
  test('Прежнее исключение доставки документа отклонено: ' + change, async () => {
    const { store, model, services, pending } = setup();
    const operation = services.request('a.axm', 'document.analyse');
    if (change === 'edit') model.change('PRINT 1');
    if (change === 'close' || change === 'reopen') store.delete('a.axm');
    if (change === 'reopen') store.set('a.axm', { model });
    if (change === 'rename') store.rename('a.axm', 'b.axm');
    if (change === 'newer') {
      const next = services.request('a.axm', 'document.analyse');
      pending[1].resolve({ ok: true, payload: { ...pending[1].snapshot, diagnostics: [] } });
      assert.ok(await next);
    }
    pending[0].reject(new Error('Прежний отказ доставки'));
    assert.equal(await operation, null);
  });
}

test('Действующее исключение не скрыто и повтор запроса доступен', async () => {
  const { services, pending } = setup();
  const operation = services.request('a.axm', 'document.analyse');
  pending[0].reject(new Error('Служба недоступна'));
  await assert.rejects(operation, /Служба недоступна/);
  const retry = services.request('a.axm', 'document.analyse');
  pending[1].resolve({ ok: true, payload: { ...pending[1].snapshot, diagnostics: [] } });
  assert.ok(await retry);
});

function app() {
  const context = { window: {}, performance: { now: () => 0 }, I18N: { t: key => key, setText: () => {} },
    document: { getElementById: () => ({}) } };
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  let reject;
  const calls = [];
  Object.assign(ide, { activeFile: 'a.axm', openFiles: new Map([['a.axm', {}], ['b.axm', {}]]),
    documentServices: { request: () => new Promise((_, fail) => reject = fail) },
    showIdeProblem: (...args) => calls.push(['problem', ...args]), setStatus: text => calls.push(['status', text]),
    switchPanel: value => calls.push(['panel', value]), focusEditor: () => calls.push(['focus']) });
  return { ide, calls, fail: () => reject(new Error('Отказ проверки')) };
}

test('Отказ проверки прежней вкладки не заменяет диагностику текущей', async () => {
  const { ide, calls, fail } = app();
  const operation = ide.check(true); ide.activeFile = 'b.axm';
  fail(); await operation;
  assert.deepEqual(calls, []);
});

test('Текущий отказ явной проверки остаётся видимым', async () => {
  const { ide, calls, fail } = app();
  const operation = ide.check(true); fail(); await operation;
  assert.ok(calls.some(call => call[0] === 'problem' && call[2] === 'Отказ проверки'));
  assert.ok(calls.some(call => call[0] === 'panel'));
});

for (const stale of [false, true]) {
  test('Исключение фоновой проверки: ' + (stale ? 'смена вкладки' : 'действующий запрос'), async () => {
    const { ide, fail } = app();
    const model = { getLanguageId: () => 'axiom', getValue: () => 'PRINT missing' };
    let active = model;
    Object.assign(ide, { editor: { getModel: () => active }, _liveCheckSeq: 1, log() {} });
    const operation = ide.runLiveCheck();
    if (stale) active = {};
    fail();
    if (stale) await assert.doesNotReject(operation);
    else await assert.rejects(operation, /Отказ проверки/);
  });
}

test('Отменённое открытие навигации не создаёт модель и не переключает вкладку после чтения', async () => {
  const {ide} = app();
  let resolve, current = true, switched = false, created = false;
  const context = {window: {axiom: {readFileContent: () => new Promise(done => {resolve = done;}), fileReadonly: async () => false,
    watchFile: async () => {}, addRecentFile: async () => []}},
    I18N: {t: key => key}, alert() { throw new Error('Неожиданное уведомление'); }};
  vm.createContext(context);
  vm.runInContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  ide.monaco = {Uri: {file: path => path}, editor: {createModel() { created = true; return {}; }}};
  ide.switchToFile = () => { switched = true; };
  ide.createTab = () => {}; ide.scheduleLiveCheck = () => {}; ide.settings = {}; ide.renderRecentFiles = () => {};
  const request = context.window.AxiomIDE.prototype.openFile.call(ide, 'helper.axm', () => current);
  current = false; resolve('PRINT 1'); await request;
  assert.equal(created, false); assert.equal(switched, false);
  assert.equal(ide.openFiles.has('helper.axm'), false);
});
