const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup(file = 'a.axm') {
  const context = { window: {}, confirm: () => true, document: { getElementById: () => null } };
  vm.createContext(context);
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8');
  vm.runInContext(source.replace('window.__ide = new AxiomIDE();', 'window.AxiomIDE = AxiomIDE;'), context);
  const ide = Object.create(context.window.AxiomIDE.prototype);
  const entry = { path: file, model: { isDisposed: () => false } };
  const other = { path: 'b.axm', model: {} };
  let finish;
  const pending = new Promise(resolve => { finish = resolve; });
  const starts = [], saves = [];
  Object.assign(ide, { activeFile: file, running: false, dirty: new Set([file]),
    openFiles: new Map([[file, entry], ['b.axm', other]]),
    save: async (target = ide.activeFile) => { saves.push(target); return pending; },
    switchToFile(target) { this.activeFile = target; },
    execution: { async start(target) { starts.push(target); } }, trace: { lines: () => [] } });
  return { ide, entry, starts, saves, finish };
}

test('Run после сохранения запускает исходный документ при переключении вкладки', async () => {
  const { ide, starts, saves, finish } = setup();
  const run = ide.run();
  ide.activeFile = 'b.axm'; ide.dirty.delete('a.axm');
  finish(true); await run;
  assert.deepEqual(saves, ['a.axm']);
  assert.deepEqual(starts, ['a.axm']);
  assert.equal(ide._runFile, 'a.axm');
  assert.equal(ide.activeFile, 'a.axm');
});

test('Отказ сохранения после переключения не запускает другую чистую вкладку', async () => {
  const { ide, starts, finish } = setup();
  const run = ide.run(); ide.activeFile = 'b.axm';
  finish(false); await run;
  assert.deepEqual(starts, []);
});

test('Сохранить как отслеживает новый путь исходного документа', async () => {
  const { ide, entry, starts, finish } = setup('untitled1');
  const run = ide.run();
  ide.openFiles.delete('untitled1'); entry.path = 'saved.axm'; ide.openFiles.set(entry.path, entry);
  ide.activeFile = 'b.axm';
  finish(true); await run;
  assert.deepEqual(starts, ['saved.axm']);
});

test('Закрытый и вновь открытый документ не подменяет исходную попытку запуска', async () => {
  const { ide, starts, finish } = setup();
  const run = ide.run();
  ide.openFiles.set('a.axm', { path: 'a.axm', model: {} }); ide.dirty.clear();
  finish(true); await run;
  assert.deepEqual(starts, []);
});

test('Завершение сохранения не начинает вторую программу поверх уже работающей', async () => {
  const { ide, starts, finish } = setup();
  const run = ide.run();
  ide.running = true; ide.dirty.clear();
  finish(true); await run;
  assert.deepEqual(starts, []);
});
