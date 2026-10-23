const { ROOT } = require('../helpers');
const interpolationColors = [['dark', 'rgb(215, 186, 125)'], ['light', 'rgb(238, 0, 0)'], ['basic', 'rgb(85, 255, 255)'], ['borland', 'rgb(255, 255, 255)']];
module.exports = [{ id: 'highlight-punctuation-and-maze', name: 'Пунктуация не обрывает подсветку операторов, типов и комментариев', async fn({ S, C }) {
  await S.launch();
  const sample = require('fs').readFileSync(require('path').join(ROOT, 'samples/graphics/game/maze_chase.axm'), 'utf8');
  const result = await S.page.evaluate(source => {
    const m = window.__ide.monaco;
    const text = 'SMOOTH ON\nSMOOTH OFF\nSUB DrawWalls(firstRow AS INTEGER, lastRow AS INTEGER)\nDRAWIMAGE image, 0, 0 : PRINT 1; 2 \' Комментарий\nPRINT $"{RGB(1, 2, 3)}"';
    const meta = window.__ide.langMeta;
    return {
      lines: m.editor.tokenize(text, 'axiom'),
      maze: m.editor.tokenize(source, 'axiom').length,
      metadata: { keyword: meta?.keywords?.includes('PRINT'), smooth: meta?.keywords?.includes('SMOOTH'), type: meta?.typeNames?.includes('INTEGER'), builtin: meta?.builtins?.some(b => b.name === 'RGB') },
    };
  }, sample);
  C.ok(result.lines[0].some(t => t.type === 'keyword.axiom') && result.lines[1].some(t => t.type === 'keyword.axiom'), 'Оператор SMOOTH и его обе формы ON/OFF распознаны');
  C.ok(result.lines[2].some(t => t.type === 'type.axiom'), 'INTEGER распознан как тип');
  C.ok(result.lines[3].some(t => t.type === 'keyword.axiom'), 'DRAWIMAGE и PRINT распознаны как операторы');
  C.ok(result.lines[3].some(t => t.type === 'comment.axiom'), 'Комментарий после пунктуации окрашен');
  C.ok(result.lines[4].some(t => t.type === 'support.function.axiom'), 'Функция внутри интерполяции распознана');
  C.eq(result.maze, sample.split(/\r?\n/).length, 'Весь maze_chase разобран без ошибки подсветки');
  C.ok(result.metadata.keyword && result.metadata.smooth && result.metadata.type && result.metadata.builtin, 'Операторы, типы и функции соответствуют реестру language metadata');
  await S.page.evaluate(() => {
    const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false;
    i.editor.getModel().setValue('SMOOTH ON\nDIM count AS INTEGER = 7 : PRINT RGB(1, 2, 3), "текст" \' Комментарий');
  });
  for (const [theme, typeColor, keywordColor, functionColor, stringColor, commentColor, operatorColor] of [
    ['dark', 'rgb(78, 201, 176)', 'rgb(197, 134, 192)', 'rgb(220, 220, 170)', 'rgb(206, 145, 120)', 'rgb(106, 153, 85)', 'rgb(230, 155, 200)'],
    ['light', 'rgb(38, 127, 153)', 'rgb(0, 0, 255)', 'rgb(121, 94, 38)', 'rgb(163, 21, 21)', 'rgb(106, 153, 85)', 'rgb(175, 25, 112)'],
    ['basic', 'rgb(255, 255, 255)', 'rgb(255, 255, 85)', 'rgb(85, 255, 255)', 'rgb(170, 255, 170)', 'rgb(184, 199, 232)', 'rgb(255, 255, 85)'],
    ['borland', 'rgb(255, 255, 85)', 'rgb(255, 255, 255)', 'rgb(85, 255, 255)', 'rgb(255, 255, 85)', 'rgb(255, 255, 85)', 'rgb(255, 255, 255)'],
  ]) {
    await S.page.evaluate(t => window.__ide.setTheme(t), theme);
    await S.page.waitForFunction(({typeColor,keywordColor}) => {
      const spans=[...document.querySelectorAll('.view-lines span')];
      return [['INTEGER',typeColor],['DIM',keywordColor],['SMOOTH',keywordColor]].every(([word,color])=>spans.some(s=>s.textContent.trim()===word&&getComputedStyle(s).color===color));
    }, {typeColor,keywordColor});
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === 'SMOOTH' && getComputedStyle(s).color === expected), keywordColor), 'SMOOTH сохраняет цвет ключевого слова в теме ' + theme);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === 'DIM' && getComputedStyle(s).color === expected), keywordColor), 'Операторы имеют свой цвет в теме ' + theme);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === 'RGB' && getComputedStyle(s).color === expected), functionColor), 'Встроенная функция имеет свой цвет в теме ' + theme);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.includes('текст') && getComputedStyle(s).color === expected), stringColor), 'Строка имеет свой цвет в теме ' + theme);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.includes('Комментарий') && getComputedStyle(s).color === expected), commentColor), 'Комментарий имеет свой цвет в теме ' + theme);
    C.ok(await S.page.evaluate(expected => [...document.querySelectorAll('.view-lines span')].some(s => s.textContent.trim() === '=' && getComputedStyle(s).color === expected), operatorColor), 'Оператор выражения имеет свой цвет в теме ' + theme);
  }
  await S.page.evaluate(() => window.__ide.setTheme('dark'));
  await S.screenshot('highlighting-fixed');
} }, { id: 'highlight-user-symbols', name: 'Пользовательские имена и операции получают цвета выбранной темы', async fn({S,C}) {
  await S.launch();
  await S.page.evaluate(() => { const i=window.__ide; i.settings.formatOnBlur=false; i.settings.checkOnChange=false; i.editor.getModel().setValue('CONST limit = 2\nDIM items[1 TO 3] AS INTEGER\nSUB PaintWorld(value AS INTEGER)\nvalue = items[1] + limit\nEND SUB\nPaintWorld(1)'); });
  for (const [theme, variable, constant, array, procedure, operator] of [
    ['dark','rgb(156, 220, 254)','rgb(240, 185, 117)','rgb(185, 163, 227)','rgb(220, 220, 170)','rgb(230, 155, 200)'],
    ['light','rgb(33, 94, 156)','rgb(143, 68, 0)','rgb(117, 82, 165)','rgb(121, 94, 38)','rgb(175, 25, 112)'],
    ['basic','rgb(255, 255, 255)','rgb(255, 255, 170)','rgb(170, 255, 170)','rgb(85, 255, 255)','rgb(255, 255, 85)'],
    ['borland','rgb(255, 255, 85)','rgb(255, 255, 85)','rgb(170, 255, 170)','rgb(85, 255, 255)','rgb(255, 255, 255)']]) {
    await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
    for(const [word,color] of [['value',variable],['limit',constant],['items',array],['PaintWorld',procedure],['=',operator]]) {
      await S.page.waitForFunction(({word,color})=>[...document.querySelectorAll('.view-lines span')].some(s=>s.textContent.trim()===word&&getComputedStyle(s).color===color),{word,color}, {timeout:7000});
      C.ok(true, word+' окрашен отдельно в '+theme);
    }
  }
  await S.page.evaluate(()=>window.__ide.setTheme('dark')); await S.screenshot('symbol-colors');
  const sample=require('fs').readFileSync(require('path').join(ROOT,'samples/graphics/game/maze_chase.axm'),'utf8');
  const declarations = await S.page.evaluate(source=> {
    const i=window.__ide, model=i.editor.getModel();model.setValue(source);
    const matches=model.findMatches('^\\s*SUB\\s+UpdatePlayerAnimation\\s*\\(',false,true,false,null,false);
    if(matches.length===1)i.editor.revealLineInCenter(matches[0].range.startLineNumber);
    return matches.map(match=>match.range.startLineNumber);
  },sample);
  C.eq(declarations.length,1,'В Maze Chase найдено единственное объявление UpdatePlayerAnimation');
  if(declarations.length!==1)throw new Error('Не найдено единственное объявление процедуры UpdatePlayerAnimation');
  await S.page.waitForFunction(line=>window.__ide.editor.getVisibleRanges().some(range=>range.startLineNumber<=line&&range.endLineNumber>=line),declarations[0]);
  C.ok(true,'Объявление процедуры находится в видимой области редактора');
  await S.page.waitForFunction(()=>[...document.querySelectorAll('.view-lines span')].some(s=>s.textContent==='UpdatePlayerAnimation'&&getComputedStyle(s).color==='rgb(220, 220, 170)'));
  C.ok(true,'Процедура из пользовательского maze_chase имеет отдельный цвет');
  await S.screenshot('maze-symbol-colors');
} }];
module.exports.push({ id: 'highlight-interpolation-dollar', name: 'Знак доллара интерполяции совпадает по цвету с фигурными скобками', async fn({ S, C }) {
  await S.launch();
  await S.page.evaluate(() => { const i = window.__ide; i.editor.getModel().setValue('PRINT $"Число {42}"\nPRINT "$"'); });
  const tokens = await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom'));
  C.ok(tokens[0].some(t => t.offset === 6 && t.type === 'delimiter.interpolation.axiom'), 'Префикс $ имеет отдельную категорию интерполяции');
  for (const [theme, color] of interpolationColors) {
    await S.page.evaluate(t => window.__ide.setTheme(t), theme);
    await S.page.waitForFunction(expected => [...document.querySelector('.view-line').querySelectorAll('span')].some(el => el.textContent.trim() === '$' && getComputedStyle(el).color === expected), color);
    const markers = await S.page.evaluate(() => [...document.querySelector('.view-line').querySelectorAll('span')].filter(el => ['$','{','}'].includes(el.textContent.trim())).map(el => ({ text: el.textContent.trim(), color: getComputedStyle(el).color })));
    C.ok(markers.length === 3 && markers.every(m => m.color === color), '$ и обе скобки имеют цвет интерполяции в теме ' + theme);
  }
} });

module.exports.push({ id: 'highlight-unterminated-string-stops-at-line-end', name: 'Незакрытый литерал не продолжается на следующей строке', async fn({ S, C }) {
  await S.launch();
  const source = 'PRINT "abc\nPRINT "def"';
  await S.page.evaluate(text => { window.__ide.editor.getModel().setValue(text); }, source);
  const tokens = await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom'));
  const secondLine = tokens[1];
  C.ok(secondLine.some(t => t.offset === 0 && t.type === 'keyword.axiom'), 'Вторая строка начинается как код, а не как текст литерала', JSON.stringify(secondLine));
  C.ok(secondLine.some(t => t.offset === 6 && t.type === 'string.quote.axiom'), 'Собственный литерал второй строки распознан');
  const firstLine = tokens[0];
  C.ok(firstLine.some(t => t.type === 'string.axiom'), 'Текст первой строки остаётся строкой');
  C.ok(!firstLine.some(t => t.type === 'string.axiom' && t.offset >= source.indexOf('\n')), 'Перевод строки не относится к строке');
  await S.screenshot('unterminated-string-line-end');
} });

module.exports.push({ id: 'highlight-interpolation-text-escapes', name: 'Удвоенные скобки и кавычки интерполяции относятся к тексту, а не к вставке', async fn({ S, C }) {
  await S.launch();
  const source = 'PRINT $"{{x}}""y""{1}"';
  await S.page.evaluate(text => { window.__ide.editor.getModel().setValue(text); }, source);
  const tokens = await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom')[0]);
  const typeAt = offset => tokens.filter(t => t.offset <= offset).pop();
  C.eq(typeAt(source.indexOf('{{')).type, 'string.escape.axiom', 'Удвоенная открывающая скобка относится к тексту');
  C.eq(typeAt(source.indexOf('}}')).type, 'string.escape.axiom', 'Удвоенная закрывающая скобка относится к тексту');
  C.eq(typeAt(source.indexOf('""')).type, 'string.escape.axiom', 'Удвоенная кавычка относится к тексту');
  const interpolationOffsets = tokens.filter(t => t.type === 'delimiter.interpolation.axiom').map(t => t.offset);
  C.eq(JSON.stringify(interpolationOffsets), JSON.stringify([6, 18, 20]), 'Скобки вставки остались единственной парой фигурных скобок');
  C.ok(!tokens.some(t => t.offset >= 8 && t.offset < 18 && t.type === 'delimiter.interpolation.axiom'), 'Удвоенные пары не открывают вставку');
  await S.screenshot('interpolation-text-escapes');
} });

module.exports.push({ id: 'highlight-number-token-boundaries', name: 'Числа Monaco сохраняют границы лексера в коде и интерполяции', async fn({ S, C }) {
  await S.launch();
  const cases = [
    ['42', [['42', 'number']]],
    ['2147483648', [['2147483648', 'number']]],
    ['12.50', [['12.50', 'number']]],
    ['42&"x"', [['42', 'number'], ['&', 'operator'], ['"', 'string.quote'], ['x', 'string'], ['"', 'string.quote']]],
    ['42D', [['42', 'number'], ['D', 'identifier']]],
    ['42F', [['42', 'number'], ['F', 'identifier']]],
    ['42%D', [['42', 'number'], ['%', 'invalid'], ['D', 'identifier']]],
    ['12.5#', [['12.5', 'number'], ['#', 'invalid']]],
    ['1.', [['1', 'number'], ['.', 'delimiter']]],
    ['.5', [['.', 'delimiter'], ['5', 'number']]],
    ['1e3', [['1', 'number'], ['e3', 'identifier']]],
    ['&hff', [['&hff', 'number.hex']]],
    ['&b1010', [['&b1010', 'number.binary']]],
    ['&HFG', [['&HFG', 'invalid']]],
    ['&B102', [['&B102', 'invalid']]],
    ['&H', [['&H', 'invalid']]],
    ['&B', [['&B', 'invalid']]],
  ];
  const results = await S.page.evaluate(inputs => {
    const ide = window.__ide;
    const forms = inputs.flatMap(([expression]) => [
      { expression, prefix: 'PRINT ', suffix: '' },
      { expression, prefix: 'PRINT $"[{', suffix: '}]"' },
    ]);
    ide.editor.getModel().setValue(forms.map(f => f.prefix + f.expression + f.suffix).join('\n'));
    return ide.monaco.editor.tokenize(ide.editor.getValue(), 'axiom').map((tokens, line) => {
      const form = forms[line], text = form.prefix + form.expression + form.suffix;
      const start = form.prefix.length, end = start + form.expression.length;
      return tokens.map((token, index) => ({ token, end: tokens[index + 1]?.offset ?? text.length }))
        .filter(item => item.token.offset >= start && item.token.offset < end)
        .map(item => [text.slice(item.token.offset, Math.min(item.end, end)), item.token.type.replace(/\.axiom$/, '')]);
    });
  }, cases);
  for (let index = 0; index < cases.length; index++) {
    const [expression, expected] = cases[index];
    C.eq(JSON.stringify(results[index * 2]), JSON.stringify(expected), expression + ': обычный код');
    C.eq(JSON.stringify(results[index * 2 + 1]), JSON.stringify(expected), expression + ': интерполяция');
  }
  const nested = await S.page.evaluate(() => {
    const source = 'PRINT $"outer {$"inner {42D}"}"';
    return window.__ide.monaco.editor.tokenize(source, 'axiom')[0].map((token, index, tokens) =>
      [source.slice(token.offset, tokens[index + 1]?.offset ?? source.length), token.type]);
  });
  C.ok(nested.some(([text, type]) => text === '42' && type === 'number.axiom'), 'Вложенная интерполяция сохраняет число');
  C.ok(nested.some(([text, type]) => text === 'D' && type === 'identifier.axiom'), 'Суффикс не поглощён и во вложенной интерполяции');
} });
