const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');

async function holdSave(S, kind = 'run') {
  await S.page.evaluate(kind => {
    const ide = window.__ide, saver = ide.documentSaver, original = saver.api;
    const state = window.__runSave = { original, file: ide.activeFile };
    saver.api = { ...original, writeFile: async (...args) => {
      await new Promise(resolve => { state.release = resolve; });
      return original.writeFile(...args);
    } };
    state.operation = ide.run(kind, false);
  }, kind);
  await S.page.waitForFunction(() => !!window.__runSave.release);
}

async function finishSave(S) {
  await S.page.evaluate(async () => {
    const state = window.__runSave;
    window.__ide.documentSaver.api = state.original;
    state.release(); await state.operation;
  });
}

module.exports = [{
  id: 'run-document-preserved-during-save',
  name: 'Run и отладка сохраняют исходный документ при смене вкладки; Сохранить как сохраняет принадлежность',
  async fn({ S, C }) {
    const directory = testWorkspace('run-document-switch');
    const first = writeAxm(directory, 'первая.axm', 'PRINT "На диске"\n');
    const second = writeAxm(directory, 'другая.axm', 'PRINT "Чужая программа"\n');
    await S.launch();
    await S.page.evaluate(file => window.__ide.openFile(file), second);
    for (const mode of ['run', 'debug', 'untitled']) {
      const target = mode === 'untitled' ? path.join(directory, 'сохранённая.axm') : first;
      if (mode === 'untitled') await S.stubSaveDialog(target);
      await S.page.evaluate(async ({ first, mode }) => {
        const ide = window.__ide;
        if (mode === 'untitled') ide.newFile(); else await ide.openFile(first);
        ide.editor.setValue(`PRINT "Исходный документ ${mode}"\n`);
      }, { first, mode });
      await holdSave(S, mode === 'debug' ? 'debug' : 'run');
      await S.page.evaluate(file => window.__ide.switchToFile(file), second);
      await finishSave(S); await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide._runFile), target, `${mode}: запущен исходный документ`);
      C.eq(await S.page.evaluate(() => window.__ide.execution.outputText), `Исходный документ ${mode}`, `${mode}: выполнен нужный исходник`);
      C.eq(await S.page.evaluate(() => window.__ide.activeFile), target, `${mode}: редактор показывает исполняемый документ`);
      C.eq(fs.readFileSync(target, 'utf8'), `PRINT "Исходный документ ${mode}"\n`, `${mode}: сохранены точные правки`);
      C.eq((await S.diagnosticItems()).length, 0, `${mode}: диагностика отсутствует`);
    }
    C.eq(fs.readFileSync(second, 'utf8'), 'PRINT "Чужая программа"\n', 'Другая программа не перезаписана');
  },
}, {
  id: 'run-document-invalidated-during-save',
  name: 'Правка или закрытие исходного документа во время сохранения отменяют запуск и не запускают соседнюю вкладку',
  async fn({ S, C }) {
    const directory = testWorkspace('run-document-invalidated');
    const first = writeAxm(directory, 'первая.axm', 'PRINT 1\n');
    const second = writeAxm(directory, 'другая.axm', 'PRINT "Не запускать"\n');
    await S.launch();
    await S.page.evaluate(file => window.__ide.openFile(file), second);
    for (const mode of ['edit', 'close']) {
      const snapshot = mode === 'edit' ? 'PRINT 2\n' : 'PRINT 4\n';
      await S.page.evaluate(async ({ file, snapshot }) => {
        const ide = window.__ide;
        await ide.openFile(file); ide.editor.setValue(snapshot);
        const original = ide.execution.api;
        window.__runCalls = { original, count: 0 };
        ide.execution.api = { ...original, startExecution: (...args) => { window.__runCalls.count++; return original.startExecution(...args); } };
      }, { file: first, snapshot });
      await holdSave(S);
      await S.page.evaluate(async ({ first, second, mode }) => {
        const ide = window.__ide;
        if (mode === 'edit') ide.editor.setValue('PRINT 3\n');
        else await ide.closeFile(first, true);
        ide.switchToFile(second);
      }, { first, second, mode });
      await finishSave(S);
      C.eq(await S.page.evaluate(() => window.__runCalls.count), 0, `${mode}: команда исполнения не отправлена`);
      C.eq(await S.page.evaluate(() => window.__ide.running), false, `${mode}: программа не работает`);
      C.eq(await S.page.evaluate(() => window.__ide.activeFile), second, `${mode}: выбранная вкладка не заменена`);
      C.eq(fs.readFileSync(first, 'utf8'), snapshot, `${mode}: запись завершила только взятый снимок`);
      if (mode === 'edit') C.eq(await S.page.evaluate(file => window.__ide.dirty.has(file), first), true, 'Поздние правки остаются несохранёнными');
      await S.page.evaluate(() => { window.__ide.execution.api = window.__runCalls.original; });
    }
  },
}];
