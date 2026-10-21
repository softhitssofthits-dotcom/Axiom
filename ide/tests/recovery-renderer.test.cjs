const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const { DocumentStore } = require('../src/renderer/documents');
function fixture() {
  const calls = [], api = { saveRecoverySync: tabs => { calls.push(tabs); return { ok: true }; } };
  const context = { window: { axiom: api } };
  vm.runInNewContext(fs.readFileSync(require.resolve('../src/renderer/app.js'), 'utf8').replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  const store = new DocumentStore(); let text = 'PRINT 1\n', changed;
  const model = { getValue: () => text, onDidChangeContent: callback => { changed = callback; return { dispose() {} }; },
    edit: value => { text = value; changed(); } };
  const path = 'C:\\work\\main.axm'; store.set(path, { model });
  Object.assign(ide, { _recoveryEnabled: true, openFiles: store, activeFile: path, setStatus() {} });
  return { ide, api, calls, model, path, store };
}
test('Повторные уведомления одной правки не отправляют полный снимок заново', () => {
  const { ide, calls, model } = fixture();
  ide.persistRecovery(); ide.persistRecovery();
  assert.equal(calls.length, 1);
  model.edit('PRINT 2\n'); ide.persistRecovery(); ide.persistRecovery();
  assert.equal(calls.length, 2); assert.equal(calls[1][0].content, 'PRINT 2\n');
});
test('Сохранение, активность, исчезновение и исключение документа меняют снимок', () => {
  const { ide, calls, model, path, store } = fixture();
  ide.persistRecovery(); model.edit('PRINT 2\n'); ide.persistRecovery();
  store.markSaved(path); ide.persistRecovery();
  assert.equal(calls.at(-1)[0].isDirty, false);
  ide.activeFile = null; ide.persistRecovery(); assert.equal(calls.at(-1)[0].isActive, false);
  store.get(path).missing = true; ide.persistRecovery(); assert.equal(calls.at(-1)[0].missing, true);
  ide.persistRecovery(new Set([store.get(path)])); assert.equal(calls.at(-1).length, 0);
  ide.persistRecovery(); assert.equal(calls.at(-1).length, 1);
});
test('Отказ записи и отказ IPC не скрываются; неизменённый снимок повторяется', () => {
  const { ide, api, calls } = fixture(); let attempts = 0;
  api.saveRecoverySync = () => { attempts++; throw new Error('Нет связи'); };
  assert.equal(ide.persistRecovery(), false); assert.equal(ide._recoveryFailed, true);
  api.saveRecoverySync = () => { attempts++; return { ok: false, message: 'Ошибка диска' }; };
  assert.equal(ide.persistRecovery(), false);
  api.saveRecoverySync = tabs => { attempts++; calls.push(tabs); return { ok: true }; };
  assert.equal(ide.persistRecovery(), true); assert.equal(ide._recoveryFailed, false);
  ide.persistRecovery(); assert.equal(attempts, 3);
});
