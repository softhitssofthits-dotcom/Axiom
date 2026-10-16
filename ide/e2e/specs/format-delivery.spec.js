const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, tag) {
  const directory = testWorkspace(tag);
  const first = writeAxm(directory, 'первый.axm', 'print 1\nprint 2\n');
  const other = writeAxm(directory, 'другой.axm', 'PRINT missing\n');
  await S.launch();
  await S.page.evaluate(async ({ first, other }) => {
    const i = window.__ide; i.settings.smartIdentifierEnabled = false; i.settings.formatOnBlur = false;
    await i.openFile(other); await i.openFile(first); clearTimeout(i._liveCheckTimer);
  }, { first, other });
  return { first, other };
}
async function hold(S, reject) {
  await S.page.evaluate(reject => {
    const i = window.__ide, original = i.documentServices.api;
    const state = window.__formatDelivery = { original, ready: false };
    i.documentServices.api = { ...original, documentCommand: async (...args) => {
      const response = await original.documentCommand(...args);
      if (args[0] !== 'document.format') return response;
      state.formatted = response.payload.sourceText;
      return new Promise((resolve, fail) => { state.release = () => reject ? fail(new Error('Отказ доставки форматирования')) : resolve(response); state.ready = true; });
    } };
    state.operation = i.formatWholeDocument('delivery-test');
  }, reject);
  await S.page.waitForFunction(() => window.__formatDelivery.ready);
  await S.page.evaluate(() => { window.__ide.documentServices.api = window.__formatDelivery.original; });
}
async function release(S) {
  await S.page.evaluate(async () => { window.__formatDelivery.release(); await window.__formatDelivery.operation; clearTimeout(window.__ide._liveCheckTimer); });
}
module.exports = [{
  id: 'format-delivery-stale-failures',
  name: 'Поздний отказ форматирования сохраняет исправленный, другой, закрытый и исполняемый документ',
  async fn({ S, C }) {
    for (const mode of ['edit', 'switch', 'close', 'run']) {
      const files = await open(S, 'format-failure-' + mode); await hold(S, true);
      C.contains(await S.page.evaluate(() => window.__formatDelivery.formatted), 'PRINT 1', mode + ': удержан настоящий результат форматирования');
      await S.page.evaluate(async ({ mode, first, other }) => {
        const i = window.__ide;
        if (mode === 'edit') i.editor.getModel().setValue('PRINT 42\n');
        if (mode === 'switch') i.switchToFile(other);
        if (mode === 'close') await i.closeFile(first, true);
        if (mode === 'run') await i.trace.command('stepOver');
        clearTimeout(i._liveCheckTimer);
      }, { mode, ...files });
      if (mode === 'run') await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
      await S.page.evaluate(() => {
        const i = window.__ide;
        window.__formatDelivery.before = JSON.stringify({ diagnostics: i._lastDiagnostics, text: i.editor.getValue(),
          panel: document.querySelector('.panel-tab.active')?.dataset.tab });
      });
      await release(S);
      C.eq(await S.page.evaluate(() => {
        const i = window.__ide; return JSON.stringify({ diagnostics: i._lastDiagnostics, text: i.editor.getValue(),
          panel: document.querySelector('.panel-tab.active')?.dataset.tab });
      }), await S.page.evaluate(() => window.__formatDelivery.before), mode + ': отказ не заменил интерфейс и текст');
      C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, mode + ': ожидание завершено');
      C.eq(await S.page.evaluate(file => window.axiom.readFileContent(file), files.first), 'print 1\nprint 2\n', mode + ': файл не записан');
      if (mode === 'run') { await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(); }
      await S.close();
    }
  },
}, {
  id: 'format-delivery-current-retry',
  name: 'Текущий отказ форматирования виден и не блокирует повтор',
  async fn({ S, C }) {
    await open(S, 'format-current'); await hold(S, true); await release(S);
    C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(d => d.code === 'AXIOM-IDE' && d.message.includes('Отказ доставки форматирования'))), 'Причина текущего отказа видна');
    C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, 'Занятость снята');
    C.eq(await S.page.evaluate(() => window.__ide.formatWholeDocument('retry')), true, 'Повтор завершён');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 1\nPRINT 2\n', 'Настоящее форматирование применено');
  },
}, {
  id: 'format-delivery-running-success',
  name: 'Поздний успешный результат не меняет уже исполняемый снимок',
  async fn({ S, C }) {
    await open(S, 'format-run-success'); await hold(S, false);
    await S.page.evaluate(() => window.__ide.trace.command('stepOver'));
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
    const snapshot = await S.page.evaluate(() => window.__ide.execution.sourceSnapshot.sourceText);
    await release(S);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), snapshot, 'Текст исполняемой модели не заменён');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getRawOptions().readOnly), true, 'Защита редактирования сохранена');
    C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, 'Ожидание завершено');
    await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
  },
}];
