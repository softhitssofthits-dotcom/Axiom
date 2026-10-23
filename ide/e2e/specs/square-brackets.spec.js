const {testWorkspace, writeAxm} = require('../helpers');

// Приёмка после переключения публичного Parser/Binder; до этого этапа не запускать.
const source = 'dim values[ ] as string = split("яблоко,банан", ",")\n' +
  'dim grid[1 to 2, 1 to 2] as integer\n' +
  'values[ 2 ]="груша"\n' +
  'dim fruitView[] = values[1 to 2]\n' +
  'print fruitView[2]\n' +
  'print len(values[2])\n';

async function open(S, tag) {
  const file = writeAxm(testWorkspace(tag), 'Квадратные массивы.axm', source);
  await S.launch();
  await S.page.evaluate(file => window.__ide.openFile(file), file);
  await S.page.waitForFunction(file => window.__ide.activeFile === file, file);
  return file;
}

module.exports = [{
  id: 'editor-square-brackets-format-colors-signature',
  name: 'Квадратные массивы: набор, форматирование, четыре темы, подсказки и сохранение',
  async fn({S, C}) {
    const file = await open(S, 'square-editor');
    for (const [style, print] of [['Classic', 'PRINT'], ['Modern', 'Print']]) {
      await S.page.evaluate(async ({source, style}) => {
        const i = window.__ide;
        i.editor.getModel().setValue(source);
        i.settings.formattingStyle = style;
        await i.formatWholeDocument('square-acceptance');
        await i.check(false);
      }, {source, style});
      C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(d => d.severity === 'error')), 'Квадратная программа проверена в ' + style);
      const text = await S.page.evaluate(() => window.__ide.editor.getValue());
      C.contains(text, 'values[2] = "груша"', 'Скобки примыкают к имени в ' + style);
      C.ok(new RegExp(print + ' (?:LEN|Len)\\(values\\[2\\]\\)').test(text), 'Вызов функции остался круглым в ' + style);

      await S.page.evaluate(() => {
        const i = window.__ide;
        i.editor.getModel().setValue('print values[ 2 ]');
        i.editor.setPosition({lineNumber: 1, column: 18}); i.editor.focus();
      });
      await S.page.keyboard.press('End'); await S.page.keyboard.press('Enter');
      await S.page.waitForFunction(expected => window.__ide.editor.getModel().getLineContent(1) === expected, print + ' values[2]');
      C.ok(true, 'Enter форматирует квадратную строку в ' + style);
    }

    await S.page.evaluate(() => {
      const i = window.__ide;
      i.editor.getModel().setValue('PRINT values');
      i.editor.setPosition({lineNumber: 1, column: 13}); i.editor.focus();
    });
    await S.page.keyboard.type('[');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT values[]', 'Monaco автоматически закрывает квадратную скобку');
    await S.page.keyboard.press('Backspace');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT values', 'Удаление пустой пары не оставляет закрывающую скобку');

    await S.page.evaluate(async source => {
      const i = window.__ide; i.editor.getModel().setValue(source); await i.check(false);
    }, source);
    for (const locale of ['ru', 'en']) {
      await S.page.evaluate(locale => window.__ide.setLanguage(locale), locale);
      const hover = await S.page.evaluate(async () => (await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(), {lineNumber: 2, column: 5}))?.contents.map(x => x.value).join('\n') || '');
      C.contains(hover, locale === 'ru' ? 'Измерений: 2' : 'Dimensions: 2', 'Ранг двумерного массива в ' + locale);
    }
    for (const theme of ['dark', 'light', 'basic', 'borland']) {
      const array = await S.page.evaluate(theme => {
        window.__ide.setTheme(theme); return window.AxiomThemes.semanticTokenColors(theme).array;
      }, theme);
      await S.page.waitForFunction(hex => {
        const color = 'rgb(' + [1, 3, 5].map(i => parseInt(hex.slice(i, i + 2), 16)).join(', ') + ')';
        return [...document.querySelectorAll('.view-lines span')].some(el => el.textContent.trim() === 'values' && getComputedStyle(el).color === color);
      }, array);
      C.ok(true, 'Прежняя категория массива сохранена в ' + theme);
    }
    const help = await S.page.evaluate(async () => {
      const i = window.__ide, model = i.monaco.editor.createModel('LEFT(grid[1, 2], ', 'axiom');
      try { return (await window.LangFeatures._sigProvider.provideSignatureHelp(model, {lineNumber: 1, column: model.getValue().length + 1})).value; }
      finally { model.dispose(); }
    });
    C.eq(help.activeParameter, 1, 'Запятая внутри индекса не считается аргументом LEFT');
    C.ok(help.signatures[0].label.startsWith('LEFT('), 'Подсказка принадлежит функции, а не grid[]');
    await S.page.evaluate(async file => { await window.__ide.save(file); }, file);
    C.eq(require('fs').readFileSync(file, 'utf8').replace(/\r/g, ''), source, 'Сохранён точный квадратный исходник');
    await S.page.evaluate(async file => {
      const i = window.__ide; await i.closeFile(file); await i.openFile(file); await i.check(false);
    }, file);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue().replace(/\r/g, '')), source, 'Повторное открытие сохраняет квадратный исходник');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim()), 'груша\n5', 'Представление и вложенный вызов исполняются после сохранения');
  },
}, {
  id: 'debug-square-array-view-stepback',
  name: 'Квадратный массив и представление сохраняются при обратном шаге',
  async fn({S, C}) {
    await open(S, 'square-debug');
    await S.page.evaluate(() => window.__ide.run('debug'));
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId && !window.__ide.execution.debugBusy);
    async function advance(command) {
      const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
      await S.page.evaluate(command => window.__ide.trace.command(command), command);
      await S.page.waitForFunction(stop => window.__ide.execution.stopId && window.__ide.execution.stopId !== stop && !window.__ide.execution.debugBusy, stop);
    }
    for (let guard = 0; guard < 8 && await S.page.evaluate(() => window.__ide.execution.line) !== 4; guard++) await advance('stepOver');
    C.eq(await S.page.evaluate(() => window.__ide.execution.line), 4, 'Отладчик дошёл до объявления представления');
    await advance('stepOver');
    C.ok(await S.page.evaluate(() => window.__ide.execution.canStepBack), 'История квадратного представления обратима');
    await advance('stepBack'); await advance('stepOver');
    await S.page.evaluate(() => window.__ide.execution.debug('continue')); await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim()), 'груша\n5', 'Повтор после отката сохраняет массив и его представление');
  },
}];
