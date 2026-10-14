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
  C.eq(colors.frame, 'rgb(85, 255, 255)', 'Вкладки используют циановый разделитель палитры');
  C.eq(colors.menu, 'rgb(0, 170, 170)', 'Строка меню бирюзовая по референсу Borland');
  C.eq(colors.status, 'rgb(0, 170, 170)', 'Строка состояния бирюзовая');
  C.eq(colors.console, '#0000aa', 'Консоль использует фон Borland Pascal');
  C.eq(colors.viewport, 'rgb(0, 0, 170)', 'Область прокрутки консоли совпадает с её фоном');
  C.eq(colors.checked, 'true', 'Пункт темы отмечен в меню');
  C.ok(colors.tokens.some(t => t.text.trim() === 'DIM' && t.color === 'rgb(255, 255, 255)'), 'Операторы светлые');
  C.ok(colors.tokens.some(t => t.text.trim() === 'INTEGER' && t.color === 'rgb(85, 255, 255)'), 'Типы циановые');
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
