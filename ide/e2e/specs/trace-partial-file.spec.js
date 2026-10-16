const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'trace-stepback-partial-copy-failure',
  name: 'Частичный отказ COPYDIR: CATCH и StepBack не повторяют внешний эффект',
  async fn({ S, C }) {
    const directory = testWorkspace('trace-partial-copy');
    fs.mkdirSync(path.join(directory, 'source', 'child'), { recursive: true });
    fs.mkdirSync(path.join(directory, 'target'), { recursive: true });
    fs.writeFileSync(path.join(directory, 'source', 'marker.txt'), 'исходник');
    fs.writeFileSync(path.join(directory, 'source', 'child', 'nested.txt'), 'вложенный');
    fs.writeFileSync(path.join(directory, 'target', 'child'), 'препятствие');
    const file = writeAxm(directory, 'частичный.axm', 'DIM count AS INTEGER = 0\nTRY\n    DIM copied AS BOOLEAN = COPYDIR("source", "target", TRUE)\nCATCH err\n    count = 1\nEND TRY\ncount = count + 1\nPRINT count\n');
    await S.launch();
    await S.page.evaluate(async file => {
      const ide = window.__ide;
      await ide.openFile(file);
      ide.editor.setPosition({ lineNumber: 5, column: 1 });
      ide.trace.toggle();
      await ide.run('debug', false);
    }, file);
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId && window.__ide.execution.line === 5);
    const marker = path.join(directory, 'target', 'marker.txt');
    C.eq(fs.readFileSync(marker, 'utf8'), 'исходник', 'Файл скопирован до отказа вложенного каталога');
    C.eq(fs.readFileSync(path.join(directory, 'target', 'child'), 'utf8'), 'препятствие', 'Причина отказа сохранена');
    C.eq(await S.page.evaluate(() => window.__ide.execution.canStepBack), false, 'В начале CATCH откат за эффект недоступен');
    C.eq(await S.page.locator('#tb-stepBack').isEnabled(), false, 'Кнопка StepBack недоступна');
    const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
    await S.page.keyboard.press('Control+F10');
    C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), stop, 'Клавиша не изменила остановку');
    fs.writeFileSync(marker, 'внешняя правка');
    await S.page.keyboard.press('F5');
    await S.waitIdle();
    await S.waitConsole('2');
    C.eq(fs.readFileSync(marker, 'utf8'), 'внешняя правка', 'Продолжение не повторило COPYDIR с перезаписью');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Перехваченный отказ не оставил ошибки исполнения');
  },
}];
