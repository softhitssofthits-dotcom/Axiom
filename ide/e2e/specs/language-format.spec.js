const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, source, tag) {
  const file = writeAxm(testWorkspace(tag), 'Программа.axm', source);
  await S.launch(); await S.stubOpenDialog([file]); await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; });
  return file;
}
module.exports = [
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
      'DIM grid(1 TO 2, 1 TO 3) AS INTEGER', 'CONST limit AS INTEGER = 2',
      'TYPE Product', '    Price AS DECIMAL', 'END TYPE', 'DIM item AS Product',
      'FUNCTION Twice(BYREF value AS DECIMAL) AS DECIMAL', '    RETURN value * 2', 'END FUNCTION',
      'SUB Show(value AS STRING)', '    PRINT value', 'END SUB',
      'PRINT price, title, grid(1, 1), limit, item.Price, Twice(price), LEN(title)',
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
    C.contains(await hover('grid(1, 1)'), 'Тип: `INTEGER(,)`', 'Ранг массива');
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
    await S.page.locator('[data-menu="edit"] .menu-label').click();
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
];
