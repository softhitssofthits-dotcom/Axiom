const { ROOT } = require('../helpers');
module.exports = [{ id: 'highlight-punctuation-and-maze', name: 'Пунктуация не обрывает подсветку операторов, типов и комментариев', async fn({ S, C }) {
  await S.launch();
  const sample = require('fs').readFileSync(require('path').join(ROOT, 'samples/graphics/game/maze_chase.axm'), 'utf8');
  const result = await S.page.evaluate(source => {
    const m = window.__ide.monaco;
    const text = 'SUB DrawWalls(firstRow AS INTEGER, lastRow AS INTEGER)\nDRAWIMAGE image, 0, 0 : PRINT 1; 2 \' Комментарий\nPRINT $"{RGB(1, 2, 3)}"';
    return { lines: m.editor.tokenize(text, 'axiom'), maze: m.editor.tokenize(source, 'axiom').length };
  }, sample);
  C.ok(result.lines[0].some(t => t.type === 'type.axiom'), 'INTEGER распознан как тип');
  C.ok(result.lines[1].some(t => t.type === 'keyword.axiom'), 'DRAWIMAGE и PRINT распознаны как операторы');
  C.ok(result.lines[1].some(t => t.type === 'comment.axiom'), 'Комментарий после пунктуации окрашен');
  C.ok(result.lines[2].some(t => t.type === 'support.function.axiom'), 'Функция внутри интерполяции распознана');
  C.eq(result.maze, sample.split(/\r?\n/).length, 'Весь maze_chase разобран без ошибки подсветки');
  await S.page.evaluate(() => {
    const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false;
    i.editor.getModel().setValue("DIM count AS INTEGER : PRINT RGB(1, 2, 3) ' Комментарий");
  });
  for (const [theme, typeColor, keywordColor] of [['dark', 'rgb(78, 201, 176)', 'rgb(197, 134, 192)'], ['light', 'rgb(38, 127, 153)', 'rgb(0, 0, 255)']]) {
    await S.page.evaluate(t => window.__ide.setTheme(t), theme);
    await S.page.waitForFunction(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === 'INTEGER' && getComputedStyle(s).color === expected), typeColor);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === 'DIM' && getComputedStyle(s).color === expected), keywordColor), 'Операторы имеют свой цвет в теме ' + theme);
    C.ok(await S.page.evaluate(() => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.includes('Комментарий') && getComputedStyle(s).color === 'rgb(106, 153, 85)')), 'Комментарий имеет свой цвет в теме ' + theme);
  }
  await S.page.evaluate(() => window.__ide.setTheme('dark'));
  await S.screenshot('highlighting-fixed');
} }];
