const {testWorkspace,writeAxm}=require('../helpers');
async function document(S,source,tag){const file=writeAxm(testWorkspace(tag),'документ.axm',source);await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);await S.page.evaluate(()=>{window.__ide.settings.formatOnBlur=false;window.__ide.settings.checkOnChange=false;});return file;}
module.exports=[
{id:'document-host-format-undo',name:'Форматирование несохранённого документа через Host и отмена',async fn({S,C}){
await document(S,'PRINT 1\n','document-format');await S.page.evaluate(()=>{const i=window.__ide;i.editor.getModel().setValue('print 42\n');i.editor.setPosition({lineNumber:1,column:6});});
await S.page.evaluate(()=>window.__ide.formatWholeDocument('test-format'));
C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'PRINT 1\n','Форматирование не записало файл');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue().trim()),'PRINT 42','Общий formatter');
await S.page.evaluate(()=>window.__ide.editor.trigger('test','undo',null));C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'print 42\n','Отмена возвращает исходный несохранённый текст');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().column),6,'Каретка сохранена');
await S.page.evaluate(()=>window.__ide.save());C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'print 42\n','Явное сохранение пишет правильный текст');
}},
{id:'document-host-structural-enter',name:'Общий планировщик вставляет блок и сохраняет отмену',async fn({S,C}){
await document(S,'IF a = 0 THEN','document-block');await S.page.evaluate(()=>{const i=window.__ide;i.editor.setPosition({lineNumber:1,column:14});i.editor.focus();});const eol=await S.page.evaluate(()=>window.__ide.editor.getModel().getEOL());await S.page.keyboard.press('Enter');
await S.page.waitForFunction(()=>window.__ide.editor.getValue().includes('END IF'));
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),['IF a = 0 THEN','    ','END IF'].join(eol),'Блок совпадает с общими правилами');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().column),5,'Каретка в теле блока');
await S.page.evaluate(()=>window.__ide.editor.trigger('test','undo',null));C.ok(!(await S.page.evaluate(()=>window.__ide.editor.getValue())).includes('END IF'),'Структурная правка отменяется');
}},
{id:'document-host-stale-format',name:'Запоздавший результат не заменяет новую правку Monaco',async fn({S,C}){
await document(S,'print 1\n','document-stale');await S.page.evaluate(()=>{const i=window.__ide,real=i.documentServices.api;i.documentServices.api={documentCommand:async(...args)=>{const reply=await real.documentCommand(...args);return new Promise(resolve=>window.__releaseDocument=()=>resolve(reply));}};window.__pendingFormat=i.formatWholeDocument('test-stale');});
await S.page.waitForFunction(()=>typeof window.__releaseDocument==='function');await S.page.evaluate(()=>{window.__ide.editor.getModel().setValue('PRINT 99\n');window.__releaseDocument();return window.__pendingFormat;});
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 99\n','Новый текст сохранён');
}}
,{id:'document-host-stale-analysis',name:'Запоздавшая диагностика не возвращает исправленную ошибку',async fn({S,C}){
await document(S,'PRINT missing\n','document-analysis');await S.page.evaluate(()=>{const i=window.__ide,real=i.documentServices.api;let count=0;i.settings.checkOnChange=true;clearTimeout(i._liveCheckTimer);i.documentServices.api={documentCommand:async(...args)=>{const reply=await real.documentCommand(...args);if(++count!==1)return reply;window.__heldCodes=reply.payload.diagnostics.map(d=>d.code);return new Promise(resolve=>window.__releaseAnalysis=()=>resolve(reply));}};window.__oldAnalysis=i.runLiveCheck();});
await S.page.waitForFunction(()=>typeof window.__releaseAnalysis==='function');C.ok((await S.page.evaluate(()=>window.__heldCodes)).includes('MB2001'),'Старый ответ содержит настоящую ошибку');
await S.page.evaluate(async()=>{const i=window.__ide;i.editor.getModel().setValue('PRINT 1\n');clearTimeout(i._liveCheckTimer);await i.runLiveCheck();window.__releaseAnalysis();await window.__oldAnalysis;});
C.eq(await S.page.evaluate(()=>window.__ide._lastDiagnostics.length),0,'Устаревшая ошибка не вернулась в список');
C.eq(await S.page.evaluate(()=>window.__ide.monaco.editor.getModelMarkers({owner:'axiom',resource:window.__ide.editor.getModel().uri}).length),0,'Редактор не получил устаревшие маркеры');
}}
];
module.exports.push({ id: 'document-empty-tabs-scroll', name: 'Пустые документы и горизонтальная прокрутка множества вкладок кнопками', async fn({ S, C }) {
  await S.launch();
  await S.electronApp.evaluate(({ BrowserWindow }) => { const w = BrowserWindow.getAllWindows()[0]; w.unmaximize(); w.setMinimumSize(0, 0); w.setSize(520, 500); });
  await S.page.waitForFunction(() => innerWidth < 600);
  await S.page.evaluate(() => { for (let n = 0; n < 18; n++) window.__ide.newFile(); });
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), '', 'Новые документы пусты');
  await S.page.waitForFunction(() => document.getElementById('editor-tabs').scrollLeft > 0);
  C.ok(await S.page.evaluate(() => { const t = document.getElementById('editor-tabs'); return t.scrollHeight <= t.clientHeight && t.scrollWidth > t.clientWidth; }), 'Вкладки в одну строку без вертикальной прокрутки');
  const before = await S.page.locator('#editor-tabs').evaluate(el => el.scrollLeft);
  await S.page.locator('#tabs-scroll-left').click();
  await S.page.waitForFunction(v => document.getElementById('editor-tabs').scrollLeft < v, before);
  C.ok(await S.page.locator('#tabs-scroll-right').isEnabled(), 'Доступна прокрутка вправо');
  await S.page.locator('#tabs-scroll-right').click();
  await S.page.waitForFunction(v => document.getElementById('editor-tabs').scrollLeft >= v - 1, before);
  await S.page.evaluate(() => window.__ide.switchToFile([...window.__ide.openFiles.keys()][0]));
  await S.page.waitForFunction(() => document.getElementById('editor-tabs').scrollLeft <= 1);
  C.ok(await S.page.locator('#tabs-scroll-left').isDisabled(), 'У левого края кнопка выключена');
  C.ok(await S.page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Окно не переполняется');
  await S.screenshot('document-tabs-scroll');
  await S.page.evaluate(() => { window.__ide.setTheme('light'); window.__ide.setLanguage('en'); });
  C.eq(await S.page.locator('#tabs-scroll-right').getAttribute('aria-label'), 'Scroll tabs right', 'Подсказка переводится');
  await S.electronApp.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows()[0].setSize(1280, 800));
  await S.page.waitForFunction(() => innerWidth > 1000);
  C.ok(await S.page.evaluate(() => { const t = document.getElementById('editor-tabs'); return t.scrollHeight <= t.clientHeight && document.documentElement.scrollWidth <= innerWidth; }), 'Широкое светлое окно без переполнения');
  await S.screenshot('document-tabs-light');
} });
module.exports.push({ id: 'document-basic-theme-persistence', name: 'Тема QuickBASIC окрашивает оболочку, код и консоль и сохраняется после запуска', async fn({ S, C }) {
  await S.launch();
  await S.page.evaluate(() => {
    const i = window.__ide; i.handleAction('theme-basic');
    i.editor.getModel().setValue("' Проверка цветов QuickBASIC\nDIM value AS INTEGER = 42\nPRINT LEN(\"Строка\") + value\n");
    i.ensureConsole();
  });
  await S.page.waitForFunction(() => getComputedStyle(document.querySelector('.monaco-editor')).backgroundColor === 'rgb(0, 0, 128)');
  await S.page.waitForFunction(() => [...document.querySelectorAll('.view-lines span')].some(el => el.textContent.trim() === 'LEN' && getComputedStyle(el).color === 'rgb(85, 255, 255)'));
  const colors = await S.page.evaluate(() => ({
    theme: document.documentElement.dataset.theme,
    frame: getComputedStyle(document.getElementById('document-tab-bar')).borderBottomColor,
    console: window.__ide.term.options.theme.background,
    viewport: getComputedStyle(document.querySelector('.xterm-viewport')).backgroundColor,
    tokens: [...document.querySelectorAll('.view-line span')].map(el => ({ text: el.textContent, color: getComputedStyle(el).color })),
  }));
  C.eq(colors.theme, 'basic', 'Тема выбрана через команду меню');
  C.eq(colors.frame, 'rgb(104, 104, 184)', 'Синий разделитель действующей палитры QuickBASIC');
  C.eq(colors.console, '#000080', 'Синий фон консоли');
  C.eq(colors.viewport, 'rgb(0, 0, 128)', 'Прокручиваемая область консоли тоже синяя');
  C.ok(colors.tokens.some(t => t.text.includes('DIM') && t.color === 'rgb(255, 255, 85)'), 'Оператор жёлтый');
  C.ok(colors.tokens.some(t => t.text.includes('INTEGER') && t.color === 'rgb(255, 255, 255)'), 'Тип белый');
  C.ok(colors.tokens.some(t => t.text.includes('LEN') && t.color === 'rgb(85, 255, 255)'), 'Функция светло-циановая');
  const menu = await S.page.evaluate(() => {
    const items = [...document.querySelectorAll('[data-action^="theme-"]')];
    return items.map(item => { const text = item.querySelector('.menu-text'); const style = getComputedStyle(text); return { name: text.textContent, icons: item.querySelectorAll('.codicon').length, font: style.fontSize + '/' + style.fontWeight }; });
  });
  C.eq(menu.find(item => item.name.includes('QuickBASIC'))?.name, 'Тема QuickBASIC', 'Название темы согласовано с меню');
  C.ok(menu.length === 4 && menu.every(item => item.icons === 0 && item.font === menu[0].font), 'Все четыре пункта без пиктограмм с одинаковым шрифтом');
  await S.screenshot('quickbasic-theme');
  await S.page.locator('[data-menu="view"] .menu-label').click();
  await S.screenshot('quickbasic-theme-menu');
  await S.page.keyboard.press('Escape');
  await S.close(); await S.launch();
  C.eq(await S.page.evaluate(() => document.documentElement.dataset.theme), 'basic', 'Тема восстановлена после запуска');
} });
module.exports.push({ id: 'document-borland-theme-persistence', name: 'Borland Pascal оформляет оболочку, редактор и консоль и сохраняется после запуска', async fn({ S, C }) {
  await S.launch();
  await S.page.evaluate(() => {
    const i = window.__ide;
    i.editor.getModel().setValue("' Проверка цветов Borland Pascal\nDIM value AS INTEGER = 42\nPRINT LEN(\"Строка\") + value\n");
    i.ensureConsole();
  });
  await S.page.locator('[data-menu="view"] .menu-label').click();
  await S.page.locator('[data-action="theme-borland"]').click();
  await S.page.waitForFunction(() => getComputedStyle(document.querySelector('.monaco-editor')).backgroundColor === 'rgb(0, 0, 170)');
  await S.page.waitForFunction(() => [...document.querySelectorAll('.view-lines span')].some(el => el.textContent.trim() === 'DIM' && getComputedStyle(el).color === 'rgb(255, 255, 255)'));
  const colors = await S.page.evaluate(() => ({
    theme: document.documentElement.dataset.theme,
    frame: getComputedStyle(document.getElementById('document-tab-bar')).borderBottomColor,
    menu: getComputedStyle(document.getElementById('menubar')).backgroundColor,
    status: getComputedStyle(document.getElementById('status-bar')).backgroundColor,
    console: window.__ide.term.options.theme.background,
    viewport: getComputedStyle(document.querySelector('.xterm-viewport')).backgroundColor,
    checked: document.querySelector('.menu-check[data-check="theme-borland"]').parentElement.getAttribute('aria-checked'),
    tokens: [...document.querySelectorAll('.view-line span')].map(el => ({ text: el.textContent, color: getComputedStyle(el).color })),
  }));
  C.eq(colors.theme, 'borland', 'Тема выбрана из общего меню «Вид»');
  C.eq(colors.frame, 'rgb(255, 255, 255)', 'Вкладки используют белую рамку Borland');
  C.eq(colors.menu, 'rgb(170, 170, 170)', 'Строка меню светло-серая по стандарту Borland');
  C.eq(colors.status, 'rgb(170, 170, 170)', 'Строка состояния светло-серая');
  C.eq(colors.console, '#000080', 'Консоль продолжает синюю гамму рабочего поля');
  C.eq(colors.viewport, 'rgb(0, 0, 128)', 'Область прокрутки консоли совпадает с её фоном');
  C.eq(colors.checked, 'true', 'Пункт темы отмечен в меню');
  C.ok(colors.tokens.some(t => t.text.trim() === 'DIM' && t.color === 'rgb(255, 255, 255)'), 'Операторы светлые');
  C.ok(colors.tokens.some(t => t.text.trim() === 'INTEGER' && t.color === 'rgb(255, 255, 85)'), 'Типы используют жёлтый обычный текст');
  C.ok(colors.tokens.some(t => t.text.trim() === 'LEN' && t.color === 'rgb(255, 255, 85)'), 'Встроенные функции жёлтые');
  C.ok(colors.tokens.some(t => t.text.includes('Строка') && t.color === 'rgb(255, 255, 85)'), 'Строки жёлтые');
  const menu = await S.page.evaluate(() => [...document.querySelectorAll('[data-action^="theme-"]')].map(item => item.querySelector('.menu-text').textContent));
  C.ok(menu.some(text => text.includes('Borland Pascal')) && menu.length === 4, 'Тема есть среди четырёх пунктов меню');
  await S.screenshot('borland-pascal-theme');
  const profile = S.userData;
  await S.close(); await S.launch({ userData: profile });
  C.eq(await S.page.evaluate(() => document.documentElement.dataset.theme), 'borland', 'Глобальная тема восстановлена после запуска');
  C.eq(await S.electronApp.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows()[0].getBackgroundColor().toLowerCase()), '#0000aa', 'Окно запуска сразу использует фон темы');
} });

module.exports.push({ id: 'document-structural-delivery', name: 'Поздние ответы структурной правки сохраняют вкладку, каретку, настройки и исполнение', async fn({ S, C }) {
  for (const mode of ['switch', 'caret', 'style', 'indent', 'run', 'edit', 'close', 'success-switch', 'success-style']) {
    await document(S, mode === 'run' ? 'print 1\n' : 'if 1 = 1 then\n', 'structural-' + mode);
    await S.page.evaluate(() => {
      const i = window.__ide, model = i.editor.getModel();
      i.editor.setPosition({ lineNumber: 2, column: 1 }); clearTimeout(i._liveCheckTimer);
      const original = i.documentServices.api;
      const state = window.__structuralDelivery = { original, path: i.activeFile };
      i.documentServices.api = { ...original, documentCommand: async (...args) => {
        const response = await original.documentCommand(...args);
        if (args[0] !== 'document.editTransition') return response;
        state.edits = response.payload.edits;
        return new Promise((resolve, reject) => { state.resolve = () => resolve(response); state.reject = () => reject(new Error('Отказ структурной правки')); });
      } };
      state.operation = i.editLineTransition(model, 1, { lineNumber: 2, column: 1 });
    });
    await S.page.waitForFunction(() => !!window.__structuralDelivery.reject);
    C.ok(await S.page.evaluate(() => window.__structuralDelivery.edits.length > 0), mode + ': удержан настоящий план правок DesktopHost');
    await S.page.evaluate(async mode => {
      const i = window.__ide, state = window.__structuralDelivery;
      i.documentServices.api = state.original;
      if (mode.endsWith('switch')) i.newFile();
      if (mode === 'caret') i.editor.setPosition({ lineNumber: 1, column: 1 });
      if (mode.endsWith('style')) i.settings.formattingStyle = 'Modern';
      if (mode === 'indent') i.settings.editorPreferences = { ...i.settings.editorPreferences, autoIndent: 'none' };
      if (mode === 'run') await i.trace.command('stepOver');
      if (mode === 'edit') i.editor.getModel().setValue('PRINT 42\n');
      if (mode === 'close') await i.closeFile(state.path, true);
      clearTimeout(i._liveCheckTimer);
    }, mode);
    if (mode === 'run') await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
    const before = await S.page.evaluate(() => JSON.stringify({ text: window.__ide.editor.getValue(), position: window.__ide.editor.getPosition(), diagnostics: window.__ide._lastDiagnostics }));
    await S.page.evaluate(async success => {
      const state = window.__structuralDelivery;
      if (success) state.resolve(); else state.reject();
      await state.operation;
    }, mode.startsWith('success'));
    C.eq(await S.page.evaluate(() => JSON.stringify({ text: window.__ide.editor.getValue(), position: window.__ide.editor.getPosition(), diagnostics: window.__ide._lastDiagnostics })), before, mode + ': текст, каретка и сообщения сохранены');
    if (mode === 'run') { await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(); }
    await S.close();
  }
} });

module.exports.push({ id: 'document-structural-delivery-retry', name: 'Текущий отказ структурной правки виден, повтор вставляет блок и сохраняет Undo', async fn({ S, C }) {
  await document(S, 'if 1 = 1 then\n', 'structural-retry');
  await S.page.evaluate(async () => {
    const i = window.__ide, original = i.documentServices.api;
    i.editor.setPosition({ lineNumber: 2, column: 1 });
    i.documentServices.api = { ...original, documentCommand: async (...args) => {
      const response = await original.documentCommand(...args);
      if (args[0] === 'document.editTransition') throw null;
      return response;
    } };
    await i.editLineTransition(i.editor.getModel(), 1, { lineNumber: 2, column: 1 });
    i.documentServices.api = original; clearTimeout(i._liveCheckTimer);
  });
  C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(d => d.message.includes('null'))), 'Отказ без объекта показан без вторичного исключения');
  const before = await S.page.evaluate(() => window.__ide.editor.getValue());
  await S.page.evaluate(() => window.__ide.editLineTransition(window.__ide.editor.getModel(), 1, { lineNumber: 2, column: 1 }));
  C.contains(await S.page.evaluate(() => window.__ide.editor.getValue()), 'END IF', 'Повтор применил настоящий план блока');
  C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().column), 5, 'Каретка внутри блока');
  await S.page.evaluate(() => window.__ide.editor.trigger('test', 'undo', null));
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), before, 'Undo отменяет структурную правку');
} });

module.exports.push({id:'document-visibility-block-endings',name:'PUBLIC и PRIVATE: закрытие всех допустимых блоков и готовые шаблоны',async fn({S,C}){
 await document(S,'','visibility-endings');
 await S.page.evaluate(()=>{window.__ide.settings.smartIdentifierEnabled=false;});
 for(const style of ['Classic','Modern'])for(const modifier of ['PUBLIC','PRIVATE'])for(const [block,header] of [['SUB','Work()'],['FUNCTION','Work() AS INTEGER'],['TYPE','Box']]){
  const opening=modifier+' '+block+' '+header;
  await S.page.evaluate(({style})=>{const i=window.__ide;i.settings.formattingStyle=style;i.editor.getModel().setValue('');i.editor.setPosition({lineNumber:1,column:1});i.editor.focus();},{style});
  await S.page.keyboard.type(opening);await S.page.keyboard.press('Escape');await S.page.keyboard.press('Enter');
  await S.page.waitForFunction(block=>window.__ide.editor.getValue().toUpperCase().includes('END '+block),block,{timeout:3000});
  C.eq(await S.page.evaluate(block=>(window.__ide.editor.getValue().toUpperCase().match(new RegExp('END '+block,'g'))||[]).length,block),1,'Один END '+modifier+' '+block+' '+style);
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().column),5,'Каретка внутри блока');
 }
 for(const modifier of ['PUBLIC','PRIVATE'])for(const [block,header] of [['SUB','Work()'],['FUNCTION','Work() AS INTEGER'],['TYPE','Box']]){
  await S.page.evaluate(block=>{const i=window.__ide;i.editor.getModel().setValue('\n\nEND '+block);i.editor.setPosition({lineNumber:1,column:1});i.editor.focus();},block);
  await S.page.keyboard.type(modifier.toLowerCase()+' '+block.toLowerCase()+' '+header+" ' комментарий");
  await S.page.keyboard.press('Escape');await S.page.keyboard.press('Enter');
  await S.page.waitForFunction(()=>window.__ide.editor.getPosition().column===5);
  C.eq((await S.editorValue()).toUpperCase().split('END '+block).length-1,1,'Готовое завершение не дублируется: '+modifier+' '+block);
 }
 for(const modifier of ['PUBLIC','PRIVATE'])for(const block of ['SUB','FUNCTION','TYPE']){
  await S.page.evaluate(()=>{const i=window.__ide;i.editor.getModel().setValue('');i.editor.setPosition({lineNumber:1,column:1});i.editor.focus();});
  await S.page.keyboard.type(modifier+' '+block.slice(0,2));
  await S.page.keyboard.press('Control+Space');
  const row=S.page.locator('.suggest-widget.visible .monaco-list-row').filter({hasText:modifier+' '+block+' —'}).first();
  await row.waitFor({state:'visible',timeout:3000});await row.click();await S.page.keyboard.press('Tab');
  C.ok((await S.editorValue()).startsWith(modifier+' '+block+' '),'Шаблон сохраняет начало '+modifier+' '+block);
  C.eq((await S.editorValue()).split(new RegExp('END '+block,'i')).length-1,1,'Шаблон вставил одно завершение');
  await S.page.keyboard.press('Escape');
  await S.page.evaluate(()=>window.__ide.editor.getContribution('snippetController2')?.cancel());
 }
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
