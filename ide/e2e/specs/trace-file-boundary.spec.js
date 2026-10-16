const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');

async function step(S, command = 'stepOver') {
  const previous = await S.page.evaluate(() => window.__ide.execution.stopId);
  await S.page.evaluate(command => window.__ide.trace.command(command), command);
  await S.page.waitForFunction(previous => {
    const ide = window.__ide;
    return !ide.running || (!!ide.execution.stopId && ide.execution.stopId !== previous && !ide.execution.debugBusy);
  }, previous);
}

async function start(S, file, firstLine = 1) {
  await S.launch();
  await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run('debug', true); }, file);
  await S.page.waitForFunction(firstLine => !!window.__ide.execution.stopId && window.__ide.execution.line === firstLine, firstLine);
}

module.exports = [{
  id: 'trace-stepback-file-write-boundary',
  name: 'StepBack не пересекает выполненную WRITEFILE/APPENDFILE и продолжение не повторяет запись',
  async fn({ S, C }) {
    for (const name of ['WRITEFILE', 'APPENDFILE']) {
      const directory = testWorkspace('trace-file-boundary-' + name);
      const marker = path.join(directory, 'marker.txt');
      fs.writeFileSync(marker, 'Начало', 'utf8');
      const file = writeAxm(directory, 'граница.axm', `DIM count AS INTEGER = 0\nDIM done AS BOOLEAN = ${name}("marker.txt", "X")\ncount = count + 1\nPRINT count\n`);
      await start(S, file);
      await step(S); await step(S); await step(S);
      C.eq(await S.page.evaluate(() => window.__ide.execution.line), 4, `${name}: остановка после записи и вычисления`);
      const expected = name === 'APPENDFILE' ? 'НачалоX' : 'X';
      C.eq(fs.readFileSync(marker, 'utf8'), expected, `${name}: запись выполнена один раз`);
      let count = 0;
      while (await S.page.evaluate(() => window.__ide.execution.canStepBack) && count++ < 20) {
        await step(S, 'stepBack');
        C.ok(await S.page.evaluate(() => window.__ide.execution.line >= 2), `${name}: откат не прошёл до записи`);
        C.eq(fs.readFileSync(marker, 'utf8'), expected, `${name}: StepBack не меняет файл`);
      }
      C.ok(count < 20, `${name}: достигнута конечная граница истории`);
      C.eq(await S.page.evaluate(() => window.__ide.execution.line), 2, `${name}: граница находится после эффекта в строке записи`);
      C.eq(await S.page.locator('#tb-stepBack').isEnabled(), false, `${name}: кнопка шага назад недоступна`);
      const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
      await S.page.keyboard.press('Control+F10');
      C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), stop, `${name}: клавиша не пересекает границу`);
      fs.writeFileSync(marker, 'Внешняя правка', 'utf8');
      await step(S);
      C.eq(await S.page.evaluate(() => window.__ide.execution.line), 3, `${name}: продолжение проходит к следующему оператору`);
      C.eq(fs.readFileSync(marker, 'utf8'), 'Внешняя правка', `${name}: шаг вперёд не повторил запись`);
      await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('1');
      C.eq(fs.readFileSync(marker, 'utf8'), 'Внешняя правка', `${name}: завершение не повторило запись`);
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, `${name}: программа завершилась без ошибок`);
      await S.close();
    }
  },
}, {
  id: 'trace-stepback-open-file-boundary',
  name: 'Открытый FILE запрещает StepBack; продолжение завершает запись через CLOSE',
  async fn({ S, C }) {
    const directory = testWorkspace('trace-open-file-boundary');
    const file = writeAxm(directory, 'открытый.axm', 'DIM f AS FILE\nOPEN "marker.txt" FOR APPEND AS f\nDIM done AS BOOLEAN = WRITE(f, "X")\nDIM count AS INTEGER = 1\nCLOSE f\nPRINT count\n');
    await start(S, file, 2);
    await step(S); await step(S);
    C.eq(await S.page.evaluate(() => window.__ide.execution.line), 4, 'Остановка после записи в открытый файл');
    C.eq(await S.page.evaluate(() => window.__ide.execution.canStepBack), false, 'С открытым FILE откат недоступен');
    C.eq(await S.page.locator('#tb-stepBack').isEnabled(), false, 'Кнопка шага назад недоступна');
    const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
    await S.page.keyboard.press('Control+F10');
    C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), stop, 'Недоступная команда не изменила остановку');
    await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('1');
    C.eq(fs.readFileSync(path.join(directory, 'marker.txt'), 'utf8'), 'X', 'CLOSE завершил единственную запись');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Продолжение завершилось без ошибок');
  },
}];
