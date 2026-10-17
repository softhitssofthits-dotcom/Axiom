const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const element = () => ({ dataset: {}, children: [], appendChild(child) { this.children.push(child); }, addEventListener() {} });
  const panel = element();
  const context = { window: { DiagnosticLanguage: { text: d => d.message } },
    document: { getElementById: () => panel, createElement: element } };
  vm.createContext(context);
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8');
  vm.runInContext(source.replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  let text = 'PRINT 1 / 0';
  const first = { path: 'a.axm', documentId: 'a-id', model: { getValue: () => text, isDisposed: () => false } };
  const other = { path: 'b.axm', documentId: 'b-id', model: { getValue: () => 'PRINT 42' } };
  const markers = new Map([[other.model, ['existing']]]), jumps = [];
  Object.assign(ide, { activeFile: 'b.axm', openFiles: new Map([['a.axm', first], ['b.axm', other]]), settings: { language: 'ru' },
    closeDiagnosticPeek() {}, log() {}, focusState() {}, updateDiagnosticFilters() {}, showPanel() {}, switchPanel() {},
    setStatus() {}, jumpTo(...args) { jumps.push(args); }, findMarkerAt: () => null,
    monaco: { MarkerSeverity: { Error: 8, Warning: 4, Hint: 1 }, editor: { setModelMarkers(model, owner, value) { markers.set(model, value); } } } });
  const diagnostic = { code: 'MB3001', severity: 'error', message: 'Division by zero.', file: 'a.axm', line: 1, col: 9, length: 1,
    executionSnapshot: { documentId: 'a-id', sourceText: text, file: 'a.axm' } };
  return { ide, first, other, markers, diagnostic, jumps, edit: value => { text = value; } };
}

test('Ошибка исполнения обновляет целевой документ и сохраняет маркеры соседнего', () => {
  const { ide, first, other, markers, diagnostic } = setup();
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  assert.equal(markers.get(first.model).length, 1);
  assert.deepEqual(markers.get(other.model), ['existing']);
});

test('Диапазон запущенного снимка не применяется к изменённому тексту и не даёт переход', () => {
  const { ide, first, markers, diagnostic, jumps, edit } = setup();
  edit('PRINT "Исправлено"');
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  assert.equal(markers.get(first.model).length, 0);
  assert.match(ide.localizedDiagText(diagnostic), /запущенной версии/);
  ide.jumpToDiagnostic(diagnostic);
  assert.deepEqual(jumps, []);
});

test('Прежняя ошибка не применяется к новой записи того же пути даже при совпадении текста', () => {
  const { ide, first, markers, diagnostic } = setup();
  first.documentId = 'reopened-id';
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  assert.equal(markers.get(first.model).length, 0);
});

test('Правка после показа ошибки немедленно снимает маркер прежнего снимка', () => {
  const { ide, first, markers, diagnostic, edit } = setup();
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  assert.equal(markers.get(first.model).length, 1);
  edit('PRINT 42');
  ide.refreshExecutionDiagnostics(first.model);
  assert.equal(markers.get(first.model).length, 0);
  assert.equal(ide._lastDiagnostics.length, 1);
  assert.match(ide.localizedDiagText(diagnostic), /запущенной версии/);
  ide.settings.language = 'en';
  assert.match(ide.localizedDiagText(diagnostic), /executed version/);
});

function liveCheck(ide, model, diagnostics = []) {
  Object.assign(ide, {
    editor: { getModel: () => model },
    documentServices: { request: async () => ({ diagnostics }) },
    _liveCheckSeq: 1,
  });
}

test('Фоновая проверка не стирает ошибку исполнения неизменённого снимка', async () => {
  const { ide, first, markers, diagnostic } = setup();
  ide.activeFile = 'a.axm';
  first.model.getLanguageId = () => 'axiom';
  liveCheck(ide, first.model);
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  await ide.runLiveCheck(1);
  assert.ok(ide._lastDiagnostics.some(d => d.code === 'MB3001'));
  assert.equal(markers.get(first.model).length, 1);
});

test('Фоновая проверка заменяет ошибку исполнения после правки текста', async () => {
  const { ide, first, markers, diagnostic, edit } = setup();
  ide.activeFile = 'a.axm';
  first.model.getLanguageId = () => 'axiom';
  liveCheck(ide, first.model, [{ code: 'MB2003', severity: 'error', message: 'Несовпадение типов.', line: 1, column: 1, length: 1 }]);
  ide.applyDiagnostics([diagnostic], false, 'a.axm');
  edit('PRINT missing');
  ide.refreshExecutionDiagnostics(first.model);
  await ide.runLiveCheck(1);
  assert.equal(ide._lastDiagnostics.map(d => d.code).join(','), 'MB2003');
  assert.equal(markers.get(first.model).map(m => m.code).join(','), 'MB2003');
});
