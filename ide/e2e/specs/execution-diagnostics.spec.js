const { testWorkspace, writeAxm } = require('../helpers');
const source = 'DIM answer AS STRING\nINPUT answer\nPRINT 1 / 0\n';
async function prepare(S, tag) {
  const directory = testWorkspace(tag);
  const first = writeAxm(directory, 'исполняемый.axm', source);
  const other = writeAxm(directory, 'другая.axm', 'PRINT 42\n');
  await S.launch();
  await S.page.evaluate(async ({ first, other }) => {
    const ide = window.__ide;
    await ide.openFile(other); await ide.openFile(first); await ide.run();
  }, { first, other });
  await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
  return { first, other };
}
async function markers(S, file) {
  return S.page.evaluate(file => {
    const model = window.__ide.openFiles.get(file).model;
    return window.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri }).map(m => m.code);
  }, file);
}

module.exports = [{
  id: 'execution-diagnostics-background-document',
  name: 'Ошибка исполнения выделяется в запущенном документе при другой активной вкладке',
  async fn({ S, C }) {
    const { first, other } = await prepare(S, 'execution-diagnostics-background');
    await S.page.evaluate(file => window.__ide.switchToFile(file), other);
    await S.page.evaluate(() => window.__ide.execution.input('Ответ\r'));
    await S.waitIdle();
    C.ok((await markers(S, first)).includes('MB3001'), 'Маркер ошибки относится к исполняемому документу');
    C.eq((await markers(S, other)).length, 0, 'В другой программе нет чужих маркеров');
    await S.page.locator('#diagnostics-content .diagnostic-item[data-code="MB3001"]').first().click();
    C.eq(await S.page.evaluate(() => window.__ide.activeFile), first, 'Переход открыл исполнявшуюся программу');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), 3, 'Переход открыл строку ошибки снимка');
  },
}, {
  id: 'execution-diagnostics-edited-snapshot',
  name: 'Ошибка прежнего снимка не выделяет документ, изменённый до или после завершения программы',
  async fn({ S, C }) {
    for (const mode of ['edit', 'afterFinish']) {
      const { first, other } = await prepare(S, 'execution-diagnostics-' + mode);
      await S.page.evaluate(async mode => {
        const ide = window.__ide;
        if (mode === 'edit') ide.editor.setValue('PRINT "Новый исходник"\n');
        await ide.execution.input('Ответ\r');
      }, mode);
      await S.waitIdle();
      if (mode === 'afterFinish') {
        C.ok((await markers(S, first)).includes('MB3001'), 'До новой правки ошибка относится к текущему тексту');
        await S.page.evaluate(() => window.__ide.editor.setValue('PRINT "Новый исходник"\n'));
      }
      C.eq((await markers(S, first)).length, 0, `${mode}: прежний диапазон не отмечает текущий документ`);
      const row = S.page.locator('#diagnostics-content .diagnostic-item[data-code="MB3001"]').first();
      C.contains(await row.textContent(), 'запущенной версии', `${mode}: ошибка явно относится к прежнему снимку`);
      await S.page.evaluate(file => { window.__ide.switchToFile(file); window.__ide.editor.setPosition({ lineNumber: 1, column: 4 }); }, other);
      await row.click();
      C.eq(await S.page.evaluate(() => window.__ide.activeFile), other, `${mode}: устаревший переход не выбирает другой текст`);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().column), 4, `${mode}: устаревший переход не перемещает курсор`);
      await S.close();
    }
  },
}];
