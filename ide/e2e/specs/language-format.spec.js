const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, source, tag) {
  const file = writeAxm(testWorkspace(tag), 'Программа.axm', source);
  await S.launch(); await S.stubOpenDialog([file]); await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; });
  return file;
}
module.exports = [
  { id: 'language-array-colors-hover', name: 'Массивы отличаются от переменных и функций во всех темах и подсказках', async fn({S,C}) {
    const source = 'DIM grade AS INTEGER = 3\nDIM fruits[] AS STRING = SPLIT("яблоко,банан", ",")\nDIM basket[] = fruits[]\nDIM grid[-1 TO 0, 7 TO 8] AS INTEGER\nPRINT grade, basket[2]\nPRINT ARRAYLENGTH(MakeFruits())\nFUNCTION MakeFruits() AS STRING[]\nRETURN fruits[]\nEND FUNCTION\n';
    await open(S, source, 'array-colors-hover');
    await S.page.evaluate(async()=>{const i=window.__ide; await i.check(false);});
    C.ok(await S.page.evaluate(()=>!window.__ide._lastDiagnostics.some(d=>d.severity==='error')), 'Квадратные массивы и круглые вызовы различаются компилятором');
    for (const theme of ['dark','light','basic','borland']) {
      const palette = await S.page.evaluate(theme=>{window.__ide.setTheme(theme);return window.AxiomThemes.semanticTokenColors(theme);},theme);
      C.eq(new Set([palette.variable,palette.array,palette.function]).size,3,'Три разных цвета: '+theme);
      for (const [word,category] of [['grade','variable'],['fruits','array'],['basket','array'],['grid','array'],['MakeFruits','function']]) {
        await S.page.waitForFunction(({word,hex})=>{
          const rgb='rgb('+[1,3,5].map(i=>parseInt(hex.slice(i,i+2),16)).join(', ')+')';
          const spans=[...document.querySelectorAll('.view-lines span')].filter(s=>s.textContent.trim()===word);
          return spans.length>0&&spans.every(s=>getComputedStyle(s).color===rgb);
        },{word,hex:palette[category]},{timeout:10000});
        C.ok(true,word+' имеет цвет '+category+' в '+theme);
      }
    }
    for(const locale of ['ru','en']) {
      await S.page.evaluate(locale=>window.__ide.setLanguage(locale),locale);
      for(const [lineNumber,column,rank,type] of [[2,5,1,'STRING'],[3,5,1,'STRING'],[4,5,2,'INTEGER'],[8,8,1,'STRING']]) {
        const text=await S.page.evaluate(async position=>{
          const h=await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(),position);
          return h?.contents.map(c=>c.value).join('\n')||'';
        },{lineNumber,column});
        C.ok(text.includes(locale==='ru'?'— массив':'— array')&&text.includes((locale==='ru'?'Тип элементов':'Element type')+': `'+type+'`')&&text.includes((locale==='ru'?'Измерений':'Dimensions')+': '+rank),'Подсказка массива, строка '+lineNumber+', '+locale);
      }
    }
    await S.page.evaluate(()=>window.__ide.run()); await S.waitIdle();
    C.eq(await S.page.evaluate(()=>window.__ide.execution.outputText.replace(/\r/g,'').trim()),'3 банан\n2','Массив целиком и вызов функции выполняются');
    await S.page.evaluate(()=>{window.__ide.setLanguage('ru');window.__ide.setTheme('dark');});
    await S.page.waitForFunction(()=>['fruits','basket','grid'].every(word=>[...document.querySelectorAll('.view-lines span')].some(s=>s.textContent.trim()===word&&getComputedStyle(s).color==='rgb(185, 163, 227)')));
    await S.page.evaluate(async()=>{const e=window.__ide.editor;e.setPosition({lineNumber:4,column:6});e.focus();await e.getAction('editor.action.showHover').run();});
    await S.page.waitForFunction(()=>[...document.querySelectorAll('.monaco-hover')].some(e=>e.textContent.includes('Тип элементов: INTEGER')&&e.textContent.includes('Измерений: 2')));
    C.ok(true,'Настоящее окно подсказки показывает тип элементов и число измерений');
    await S.screenshot('array-colors-hover');
  } },
  { id: 'language-array-initializers', name: 'Массивные инициализаторы: явный и выведенный тип, форматирование, подсветка и запуск', async fn({ S, C }) {
    const source = 'DIM inferredFruits[] = SPLIT("яблоко,банан", ",")\nDIM typedFruits[] AS STRING = inferredFruits[]\nPRINT typedFruits[2]\ntypedFruits[2] = "апельсин"\nPRINT inferredFruits[2]\n';
    await open(S, source, 'array-initializers');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    for (const style of ['Classic', 'Modern']) {
      await S.page.evaluate(async ({ source, style }) => {
        const i = window.__ide;
        i.editor.getModel().setValue(source);
        i.settings.formattingStyle = style;
        await i.formatWholeDocument('array-initializers');
        await i.check(false);
      }, { source, style });
      C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(d => d.severity === 'error')), 'Обе формы проверяются в стиле ' + style);
      for (const lineNumber of [1, 2]) {
        const hover = await S.page.evaluate(async lineNumber => {
          const result = await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(), { lineNumber, column: 5 });
          return result?.contents.map(item => item.value).join('\n') || '';
        }, lineNumber);
        C.ok(/STRING\s*\[\s*\]/i.test(hover), 'Тип STRING[] у объявления строки ' + lineNumber + ', ' + style);
      }
      const tokens = await S.page.evaluate(() => {
        const i = window.__ide;
        return i.monaco.editor.tokenize(i.editor.getValue(), 'axiom');
      });
      C.ok(tokens[0].some(t => t.type === 'keyword.axiom') && tokens[0].some(t => t.type === 'support.function.axiom'), 'DIM и SPLIT имеют свои категории, ' + style);
      C.ok(tokens[1].some(t => t.type === 'type.axiom'), 'STRING имеет категорию типа, ' + style);
      await S.page.evaluate(() => window.__ide.run());
      await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim()), 'банан\nапельсин', 'Запуск и общий массив, ' + style);
    }
  } },
  { id: 'language-array-literals', name: 'Литерал массива: форматирование, подсветка, проверка и запуск в двух стилях', async fn({ S, C }) {
    const source = 'DIM fruits[] AS STRING = [ "яблоко" , "банан" ]\nPRINT fruits[2]\nPRINT ARRAYLENGTH( [ "яблоко" , "банан" ] )\n';
    await open(S, source, 'array-literals');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    for (const style of ['Classic', 'Modern']) {
      await S.page.evaluate(async ({ source, style }) => {
        const i = window.__ide;
        i.editor.getModel().setValue(source);
        i.settings.formattingStyle = style;
        await i.formatWholeDocument('array-literals');
        await i.check(false);
      }, { source, style });
      C.eq(await S.page.evaluate(() => window.__ide.editor.getModel().getLineContent(1)),
        style === 'Classic' ? 'DIM fruits[] AS STRING = ["яблоко", "банан"]' : 'Dim fruits[] As String = ["яблоко", "банан"]',
        'Литерал форматируется с пробелом после =, ' + style);
      C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(d => d.severity === 'error')), 'Литерал проверяется без ошибок, ' + style);
      const tokens = await S.page.evaluate(() => {
        const i = window.__ide;
        return i.monaco.editor.tokenize(i.editor.getValue(), 'axiom');
      });
      C.ok(tokens[0].some(t => /^string/.test(t.type)), 'Значения литерала остаются строками, ' + style);
      C.ok(tokens[0].some(t => /^delimiter/.test(t.type)) || tokens[0].some(t => /bracket/.test(t.type)), 'Скобки литерала имеют свою категорию, ' + style);
      await S.page.evaluate(() => window.__ide.run());
      await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim()), 'банан\n2', 'Литерал выполняется и нумеруется с 1, ' + style);
    }
  } },
  { id: 'format-smooth-two-styles', name: 'SMOOTH форматируется через меню и Enter в Classic и Modern', async fn({ S, C }) {
    const source = 'smooth on\nsMoOtH oFf\nprint "smooth on"\n\' smooth off\n';
    await open(S, source, 'format-smooth');
    for (const [style, on, off, print] of [['Classic','SMOOTH ON','SMOOTH OFF','PRINT'], ['Modern','Smooth On','Smooth Off','Print']]) {
      await S.page.evaluate(source => window.__ide.editor.getModel().setValue(source), source);
      await S.page.locator('[data-menu="tools"] .menu-label').click();
      await S.page.locator('#formatting-submenu .submenu-trigger').hover();
      await S.page.locator('[data-action="format-' + style.toLowerCase() + '"]').click();
      await S.page.waitForFunction(({style, on}) => window.__ide.settings.formattingStyle === style &&
        !window.__ide._formatBusy && window.__ide.editor.getModel().getLineContent(1) === on, {style, on});
      C.eq(await S.page.evaluate(() => window.__ide.editor.getValue().replace(/\r/g, '')),
        `${on}\n${off}\n${print} "smooth on"\n' smooth off\n`, 'Обе формы SMOOTH, строки и комментарии в ' + style);
      await S.page.evaluate(() => {
        const i = window.__ide; i.editor.getModel().setValue('smooth on');
        i.editor.setPosition({lineNumber:1, column:10}); i.editor.focus();
      });
      await S.page.keyboard.press('Enter');
      await S.page.waitForFunction(on => window.__ide.editor.getModel().getLineContent(1) === on, on);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getModel().getLineContent(1)), on, 'Enter форматирует SMOOTH в ' + style);
    }
  } },
  { id: 'language-next-counter', name: 'NEXT проверяет имя счётчика и сохраняет диагностику в редакторе', async fn({ S, C }) {
    await open(S, 'FOR i = 1 TO 2\n    PRINT i\nNEXT wrong\n', 'next-counter');
    await S.page.evaluate(() => window.__ide.check(false));
    const bad = await S.page.evaluate(() => {
      const i=window.__ide;
      const markers=i.monaco.editor.getModelMarkers({owner:'axiom',resource:i.editor.getModel().uri});
      return {diagnostics:i._lastDiagnostics,markers};
    });
    C.ok(bad.diagnostics.some(item=>item.code==='MB2048'), 'Неверный счётчик отвергнут компилятором');
    C.ok(bad.markers.some(item=>item.code==='MB2048'&&item.startLineNumber===3&&item.startColumn===6), 'Ошибка отмечена на неверном имени');
    await S.page.evaluate(() => window.__ide.setLanguage('en'));
    C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes("NEXT counter 'wrong' must match FOR counter 'i'")), 'Оба имени сохранены в английском сообщении');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    for(const ending of ['NEXT','NEXT i','NEXT I']){
      await S.page.evaluate(async ending => { const i=window.__ide; i.editor.getModel().setValue('FOR i = 1 TO 2\n    PRINT i\n'+ending+'\n'); await i.check(false); }, ending);
      C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(item=>item.severity==='error')), 'Допустимая форма '+ending+' проверена');
      const tokens=await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(),'axiom'));
      C.ok(tokens[2].some(item=>item.type.includes('keyword')), 'NEXT сохраняет подсветку ключевого слова');
      if(ending!=='NEXT'){
        const hover=await S.page.evaluate(async () => { const i=window.__ide; const card=await window.LangFeatures._hoverProvider.provideHover(i.editor.getModel(),{lineNumber:3,column:6}); return card?.contents.map(item=>item.value).join('\n')||''; });
        C.ok(hover.includes('INTEGER'), 'Имя после NEXT связано со счётчиком компилятора');
      }
    }
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    C.ok(await S.page.evaluate(() => /(?:^|\n)1\r?\n2(?:\r?\n|$)/.test(window.__ide.execution.outputText)), 'Цикл с NEXT I реально исполнен');
  } },
  { id: 'language-binary-hex', name: 'Двоичные и шестнадцатеричные литералы: типы, подсветка, форматирование и выполнение', async fn({ S, C }) {
    const source = 'DIM small AS INTEGER = &b1010\nDIM wide AS LONG = &HFF\nPRINT small\nPRINT wide OR &B1111\nPRINT $"{small:B8}"\nPRINT &HFF&"x"\nPRINT $"{&B1111&"y"}"\nTRY\n    PRINT $"{1.5:B}"\nCATCH problem\n    PRINT problem.Code\nEND TRY\n';
    await open(S, source, 'binary-hex');
    await S.page.evaluate(() => window.__ide.check(false));
    C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(item => item.severity === 'error')), 'Литералы и преобразование HEX в LONG проверены');
    const tokens = await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom'));
    C.ok(tokens[0].some(item => item.type === 'number.binary.axiom'), 'Префикс &b имеет числовую категорию');
    C.ok(tokens[6].some(item => item.type === 'number.binary.axiom'), 'Двоичный литерал выделен внутри интерполяции');
    C.ok(tokens[5].some(item => item.offset === 10 && item.type === 'operator.axiom'), 'Амперсанд после HEX является отдельным оператором');
    for (const [theme, color] of [['light','rgb(9, 134, 88)'], ['dark','rgb(181, 206, 168)'], ['basic','rgb(255, 255, 170)'], ['borland','rgb(255, 255, 85)']]) {
      await S.page.evaluate(theme => window.__ide.setTheme(theme), theme);
      await S.page.waitForFunction(color => [...document.querySelectorAll('.view-lines span')].some(item => item.textContent.trim() === '&b1010' && getComputedStyle(item).color === color), color);
      C.ok(true, 'Двоичный литерал имеет цвет числа в теме ' + theme);
    }
    await S.page.evaluate(async () => { const i = window.__ide; i.settings.formattingStyle = 'Classic'; await i.formatWholeDocument('binary-e2e'); });
    C.ok(await S.page.evaluate(() => window.__ide.editor.getValue().includes('&b1010')), 'Форматирование сохраняет префикс и цифры');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim()), '10\n255\n00001010\n255x\n15y\nMB3007', 'Исполнение двоичной записи, HEX, форматов и перехват ошибки');
    await S.page.evaluate(async () => { const i = window.__ide; i.editor.getModel().setValue('PRINT &B102'); await i.check(false); });
    C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(item => item.code === 'MB0003')), 'Недопустимая двоичная цифра отмечена в редакторе');
  } },
  { id: 'language-catch-message', name: 'Message системной ошибки английское при обоих языках IDE; ERROR сохраняет авторский текст', async fn({ S, C }) {
    await open(S, 'TRY\n    PRINT POW(-2, 0.5)\nCATCH err\n    PRINT err.Message\nEND TRY\nTRY\n    ERROR "MB3007", "Моя ошибка"\nCATCH manual\n    PRINT manual.Message\nEND TRY\n', 'catch-message');
    const expected = "Function POW: argument 'value' cannot be used with the specified exponent.\nМоя ошибка";
    for (const locale of ['ru', 'en']) {
      await S.page.evaluate(locale => window.__ide.setLanguage(locale), locale);
      await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
      const output = await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '').trim());
      C.eq(output, expected, 'Системное сообщение и авторский текст при языке ' + locale);
      C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(item => item.severity === 'error')), 'Ошибки обработаны при языке ' + locale);
    }
  } },
  { id: 'language-hover-catch', name: 'CATCH объявляет переменную ошибки только для чтения, а CONST остаётся константой', async fn({ S, C }) {
    await open(S, 'CONST limit AS INTEGER = 2\nTRY\n    PRINT POW(-2, 0.5)\nCATCH err\n    PRINT err.Code, err.Message, err.Line, err.Column\nEND TRY\nPRINT limit\n', 'hover-catch');
    await S.page.evaluate(() => window.__ide.check(false));
    C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(item => item.severity === 'error')), 'CATCH объявляет err без DIM');
    async function hover(lineNumber, column) {
      return S.page.evaluate(async position => {
        const result = await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(), position);
        return result?.contents.map(item => item.value).join('\n') || '';
      }, {lineNumber, column});
    }
    {
      C.contains(await hover(4, 7), 'переменная ошибки, только для чтения', 'Категория имени после CATCH');
      C.contains(await hover(4, 7), 'Тип: `ERROR`', 'Тип объявления CATCH');
    }
    for (const column of [11, 21, 34, 44])
      C.contains(await hover(5, column), 'переменная ошибки, только для чтения', 'Категория err при чтении поля');
    for (const [column, type] of [[15, 'STRING'], [25, 'STRING'], [38, 'INTEGER'], [48, 'INTEGER']])
      C.contains(await hover(5, column), 'Тип: `' + type + '`', 'Тип поля ERROR');
    C.contains(await hover(7, 7), 'константа', 'CONST остаётся константой');
    await S.page.evaluate(async () => {
      const i = window.__ide; i.editor.setPosition({lineNumber: 4, column: 7});
      i.editor.focus(); await i.editor.getAction('editor.action.showHover').run();
    });
    await S.page.waitForFunction(() => [...document.querySelectorAll('.monaco-hover')].some(el => el.textContent.includes('переменная ошибки, только для чтения') && el.textContent.includes('Тип: ERROR')));
    C.ok(true, 'Настоящая карточка Monaco показывает переменную ошибки');
    await S.page.keyboard.press('Escape');
    await S.page.evaluate(() => window.__ide.setLanguage('en'));
    C.contains(await hover(4, 7), 'error variable, read-only', 'Категория объявления по-английски');
    C.contains(await hover(5, 11), 'error variable, read-only', 'Категория использования по-английски');
    C.contains(await hover(7, 7), 'constant', 'CONST по-английски');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    C.ok(await S.page.evaluate(() => window.__ide.execution.outputText.includes('MB3007')), 'Ошибка POW перехвачена и поля прочитаны');
  } },
  { id: 'language-hover-registry', name: 'Карточки всех встроенных функций, типов и ключевых слов соответствуют реестру', async fn({ S, C }) {
    await open(S, 'PRINT 1\n', 'hover-registry');
    const result = await S.page.evaluate(async () => {
      const i = window.__ide, model = i.editor.getModel(), meta = i.langMeta;
      const families = new Map();
      for (const b of meta.builtins) {
        if (!families.has(b.name)) families.set(b.name, []);
        families.get(b.name).push(b);
      }
      const names = [...new Set([...families.keys(), ...meta.typeNames, ...meta.keywords])];
      model.setValue(names.map(name => 'PRINT ' + name).join('\n'));
      // Это список реестра, а не исполнимая программа: диагностика здесь проверяется отдельно.
      clearTimeout(i._liveCheckTimer); i._liveCheckSeq++;
      i.monaco.editor.setModelMarkers(model, 'axiom', []);
      const failures = [];
      for (let index = 0; index < names.length; index++) {
        const name = names[index];
        const card = await window.LangFeatures._hoverProvider.provideHover(model, {lineNumber: index + 1, column: 7});
        const text = card?.contents.map(c => c.value).join('\n') || '';
        if (families.has(name)) {
          if (!text.includes('встроенная функция') || families.get(name).some(b => !text.includes('`' + window.LangFeatures.builtinSignature(b) + '`')))
            failures.push(name);
        } else if (!text.includes(meta.typeNames.includes(name) ? 'тип данных' : 'ключевое слово')) failures.push(name);
      }
      return { count: names.length, families: families.size, failures };
    });
    C.ok(result.count > 300, 'Проверен полный реестр (' + result.count + ' сущностей)');
    C.eq(result.failures.join(', '), '', 'Все карточки и зарегистрированные подписи корректны');
    C.note('Проверено семейств встроенных функций: ' + result.families);
  } },
  { id: 'language-hover-symbol-types', name: 'Подсказки Monaco показывают типы компилятора, сигнатуры и перегрузки', async fn({ S, C }) {
    const source = [
      'DIM price AS DECIMAL = -10.5', 'DIM title AS STRING = "Цена"',
      'DIM grid[1 TO 2, 1 TO 3] AS INTEGER', 'CONST limit AS INTEGER = 2',
      'TYPE Product', '    Price AS DECIMAL', 'END TYPE', 'DIM item AS Product',
      'FUNCTION Twice(BYREF value AS DECIMAL) AS DECIMAL', '    RETURN value * 2', 'END FUNCTION',
      'SUB Show(value AS STRING)', '    PRINT value', 'END SUB',
      'PRINT price, title, grid[1, 1], limit, item.Price, Twice(price), LEN(title)',
      'PRINT $"Цена {price}"', 'PRINT "price LEN"', "' price LEN", 'REM price LEN',
    ].join('\n');
    await open(S, source, 'hover-symbol-types');
    await S.page.evaluate(() => window.__ide.check(false));
    async function hover(needle, shift = 0) {
      return S.page.evaluate(async ({needle, shift}) => {
        const model = window.__ide.editor.getModel();
        const offset = model.getValue().indexOf(needle) + shift;
        const position = model.getPositionAt(offset);
        const result = await window.LangFeatures._hoverProvider.provideHover(model, position);
        return result?.contents.map(c => c.value).join('\n') || null;
      }, {needle, shift});
    }
    C.contains(await hover('price AS'), 'Тип: `DECIMAL`', 'DECIMAL в объявлении');
    C.contains(await hover('price, title'), 'Тип: `DECIMAL`', 'DECIMAL при использовании');
    C.contains(await hover('title, grid'), 'Тип: `STRING`', 'Строковая переменная');
    C.contains(await hover('grid[1, 1]'), 'Тип: `INTEGER[,]`', 'Ранг массива');
    C.contains(await hover('limit, item'), 'константа', 'Категория CONST');
    C.contains(await hover('value * 2'), 'Тип: `DECIMAL`', 'Параметр функции');
    C.contains(await hover('value\nEND SUB'), 'Тип: `STRING`', 'Одноимённый параметр другой процедуры');
    C.contains(await hover('item.Price', 5), 'Тип: `DECIMAL`', 'Поле TYPE');
    C.contains(await hover('Product\n'), 'Price AS DECIMAL', 'Карточка пользовательского типа');
    C.contains(await hover('Twice(price)'), 'FUNCTION Twice(BYREF value AS DECIMAL) AS DECIMAL', 'Подпись функции');
    const signature = await S.page.evaluate(async () => {
      const model = window.__ide.editor.getModel(), offset = model.getValue().indexOf('Twice(price)') + 'Twice(price'.length;
      return (await window.LangFeatures._sigProvider.provideSignatureHelp(model, model.getPositionAt(offset))).value;
    });
    C.eq(signature.signatures[0].parameters[0].label, 'BYREF value AS DECIMAL', 'Параметр в подсказке вызова');
    C.contains(await hover('Show(value'), 'SUB Show(value AS STRING)', 'Подпись процедуры');
    C.contains(await hover('{price}', 1), 'Тип: `DECIMAL`', 'Символ в интерполяции');
    const builtin = await hover('LEN(title)');
    C.contains(builtin, 'встроенная функция', 'Категория встроенной функции');
    const overloadCount = await S.page.evaluate(() => window.__ide.langMeta.builtins.filter(b => b.name === 'LEN').length);
    C.eq((builtin.match(/`LEN\(/g) || []).length, overloadCount, 'Все зарегистрированные перегрузки LEN');
    C.contains(await hover('PRINT price'), 'ключевое слово', 'Карточка оператора');
    C.contains(await hover('DECIMAL ='), 'тип данных', 'Карточка встроенного типа');
    for (const [needle, shift] of [['"price LEN"', 1], ["' price LEN", 2], ['REM price LEN', 4]])
      C.eq(await hover(needle, shift), null, 'Подсказка скрыта в строке или комментарии: ' + needle);

    await S.page.evaluate(async () => {
      const i = window.__ide, model = i.editor.getModel();
      i.editor.setPosition(model.getPositionAt(model.getValue().indexOf('price, title')));
      i.editor.focus(); await i.editor.getAction('editor.action.showHover').run();
    });
    await S.page.waitForFunction(() => [...document.querySelectorAll('.monaco-hover')].some(el => el.textContent.includes('Тип: DECIMAL')));
    C.ok(true, 'Настоящий тултип Monaco показывает DECIMAL');
    await S.page.keyboard.press('Escape');
    await S.page.evaluate(async () => {
      const i = window.__ide; i.editor.getModel().setValue('DIM price AS STRING = "Цена"\nPRINT price'); await i.check(false);
      i.editor.setPosition({lineNumber: 2, column: 7}); i.editor.focus(); await i.editor.getAction('editor.action.showHover').run();
    });
    await S.page.waitForFunction(() => [...document.querySelectorAll('.monaco-hover')].some(el => el.textContent.includes('Тип: STRING')));
    C.ok(true, 'После правки реальный тултип показывает STRING');
    await S.page.keyboard.press('Escape');
    await S.page.evaluate(() => window.__ide.setLanguage('en'));
    C.contains(await hover('PRINT price', 6), 'Type: `STRING`', 'Английская локализация');
  } },
  { id: 'language-runtime-diagnostic-switch', name: 'Ошибка выполнения переключает язык без повторного запуска', async fn({ S, C }) {
    await open(S, 'DIM divisor AS INTEGER = 0\nPRINT 1 / divisor\n', 'language-runtime');
    await S.page.evaluate(() => { window.__ide.setLanguage('en'); return window.__ide.run(); });
    await S.waitIdle();
    C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes('MB3001: Division by zero.')), 'Ошибка VM по-английски');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    C.ok(await S.page.evaluate(() => /MB3001:.*[А-Яа-яЁё]/.test(document.getElementById('diagnostics-content').textContent)), 'Исходный текст VM восстановлен по-русски');
  } },
  { id: 'language-diagnostics-switch', name: 'Язык диагностик списка и Monaco переключается без повторного анализа', async fn({ S, C }) {
    await open(S, 'PRINT missing\n', 'language-diagnostics');
    for (const source of ['PRINT missing\n', 'PRINT @\n', 'PRINT (\n']) {
      await S.page.evaluate(async source => { const i = window.__ide; i.editor.getModel().setValue(source); await i.check(false); }, source);
      await S.page.evaluate(() => window.__ide.setLanguage('en'));
      const en = await S.page.evaluate(() => { const i = window.__ide; return { list: document.getElementById('diagnostics-content').textContent, markers: i.monaco.editor.getModelMarkers({owner:'axiom',resource:i.editor.getModel().uri}).map(m=>m.message) }; });
      C.ok(/MB\d{4}/.test(en.list) && !/[А-Яа-яЁё]/.test(en.markers.join(' ')), 'Диагностика по-английски для ' + source.trim());
      await S.page.evaluate(() => window.__ide.setLanguage('ru'));
      C.ok(await S.page.evaluate(() => window.__ide.monaco.editor.getModelMarkers({owner:'axiom',resource:window.__ide.editor.getModel().uri}).some(m=>/[А-Яа-яЁё]/.test(m.message))), 'Русский текст восстановлен');
    }
  } },
  { id: 'format-one-enter-two-styles', name: 'Один Enter форматирует строку и создаёт цикл в Classic и Modern', async fn({ S, C }) {
    await open(S, 'PRINT 1\n', 'format-enter');
    await S.page.locator('[data-menu="tools"] .menu-label').click();
    await S.page.locator('#formatting-submenu .submenu-trigger').hover();
    await S.page.locator('[data-action="format-modern"]').click();
    await S.page.waitForFunction(() => window.__ide.settings.formattingStyle === 'Modern' && !window.__ide._formatBusy);
    C.ok(true, 'Стиль выбирается настоящим пунктом меню');
    for (const [style, expected] of [['Classic','FOR index = 1 TO 3'], ['Modern','For index = 1 To 3']]) {
      await S.page.evaluate(async style => { const i = window.__ide; await i.setFormattingStyle(style); i.editor.getModel().setValue('for index = 1 to 3'); i.editor.setPosition({lineNumber:1,column:19}); i.editor.focus(); }, style);
      await S.page.keyboard.press('Enter');
      await S.page.waitForFunction(expected => window.__ide.editor.getModel().getLineContent(1) === expected, expected);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), 2, 'Каретка в теле цикла ' + style);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().column), 5, 'Отступ тела цикла ' + style);
      C.ok(await S.page.evaluate(() => /next/i.test(window.__ide.editor.getValue())), 'Закрывающая строка вставлена ' + style);
      C.ok(await S.page.evaluate(style => document.querySelector('[data-check="format' + style + '"]').classList.contains('on'), style), 'В меню отмечен выбранный стиль');
    }
  } },
  { id: 'language-nested-interpolation-and-scientific-print', name: 'Вложенная интерполяция и PRINT USING: проверка, раскраска, форматирование и исполнение', async fn({ S, C }) {
    const source = 'PRINT $"a{$"b{1}c"}d"\nPRINT USING "###.##^^^^"; 0.001\nPRINT USING "#.################"; 1.0\nTRY\n    PRINT USING "#"; "text"\nCATCH problem\n    PRINT problem.Code\nEND TRY\n';
    await open(S, source, 'nested-string-audit');
    await S.page.evaluate(() => window.__ide.check(false));
    C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(item => item.severity === 'error')), 'Источник проходит проверку');
    const tokens = await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom')[0]);
    C.ok(tokens.some(item => item.type === 'number.axiom'), 'Число во внутренней вставке подсвечено как число');
    for (const style of ['Classic', 'Modern']) {
      await S.page.evaluate(async ({ source, style }) => { const i=window.__ide; i.editor.getModel().setValue(source); i.settings.formattingStyle=style; await i.formatWholeDocument('string-audit'); }, { source, style });
      await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r/g, '')), 'ab1cd\n  1.00E-03\n1.0000000000000000\nMB3007', 'Результат строк после форматирования ' + style);
    }
  } },
];
