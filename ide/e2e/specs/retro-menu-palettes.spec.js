const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'retro-menu-palettes',name:'Borland и QuickBASIC: общая палитра выпадающих меню, подменю и контекстных меню',async fn({S,C}){
 const file=writeAxm(testWorkspace('retro-palettes'),'palette.axm',"' Комментарий\nDIM answer AS INTEGER = 42\nPRINT LEN(\"Turbo Pascal\") + answer\n");
 await S.launch({settings:{}});
 await S.page.emulateMedia({reducedMotion:'reduce'});
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));
 const closeMenus=async()=>{await S.page.keyboard.press('Escape');await S.page.keyboard.press('Escape');};
 await S.page.evaluate(async file=>{await window.__ide.openFile(file);window.__ide.ensureConsole();},file);
 for(const [theme,bg,fg,selected] of [['borland','rgb(0, 170, 170)','rgb(0, 0, 0)','rgb(0, 0, 0)'],['basic','rgb(192, 192, 192)','rgb(0, 0, 0)','rgb(0, 0, 0)']]){
  await S.page.evaluate(theme=>window.__ide.setTheme(theme),theme);
  if(theme==='basic'){
   await S.page.waitForFunction(()=>getComputedStyle(document.getElementById('status-bar')).backgroundColor==='rgb(0, 128, 128)');
   C.eq(await S.page.locator('#status-bar').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 128, 128)','QuickBASIC: бирюзовая строка состояния отдельно от серого меню');
   C.eq(await S.page.locator('#status-bar').evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 255)','QuickBASIC: белый текст строки состояния');
   C.eq(await S.page.locator('#status-bar span').first().evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 255)','QuickBASIC: белые подписи внутри строки состояния');
  }
  if(theme==='borland'){
   await S.page.waitForFunction(()=>getComputedStyle(document.getElementById('menubar')).backgroundColor==='rgb(0, 170, 170)'&&getComputedStyle(document.getElementById('toolbar')).backgroundColor==='rgb(0, 153, 153)');
   C.eq(await S.page.locator('#menubar').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 170, 170)','Строка меню сохраняет исходную бирюзу');
   C.eq(await S.page.locator('#toolbar').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 153, 153)','Панель инструментов немного темнее строки меню');
   C.eq(await S.page.locator('[data-menu="file"] > .menu-label').locator('.menu-mnemonic').evaluate(el=>getComputedStyle(el).color),'rgb(170, 0, 0)','Borland Pascal: буква вызова меню выделена красным');
   for(const menu of ['file','edit','run','view','help'])
    C.eq(await S.page.locator(`[data-menu="${menu}"] > .menu-label`).locator('.menu-mnemonic').evaluate(el=>getComputedStyle(el).color),'rgb(170, 0, 0)',`Borland Pascal: первая буква верхнего меню ${menu} красная`);
  }
  await S.page.locator('[data-menu="file"] > .menu-label').dispatchEvent('mousedown');
  const dropdown=S.page.locator('[data-menu="file"] > .menu-dropdown');
  C.eq(await dropdown.evaluate(el=>getComputedStyle(el).backgroundColor),bg,theme+': фон выпадающего меню');
  if(theme==='basic'){
   const shadow=await dropdown.evaluate(el=>getComputedStyle(el).boxShadow);
   C.ok(shadow.includes('inset')&&shadow.includes('rgb(255, 255, 255)')&&shadow.includes('7px 7px 0px'),'QuickBASIC: светлая внутренняя рамка и чёрная тень по референсу');
  }
  if(theme==='borland'){
   const shadow=await dropdown.evaluate(el=>getComputedStyle(el).boxShadow);
   C.ok(shadow.includes('4px inset')&&shadow.includes('7px 7px 0px'),'Borland Pascal: внутренний контур и чёткая чёрная тень');
  }
  const row=dropdown.locator('[data-action="open"]').first();
  if(theme==='borland'){
   const mnemonic=S.page.locator('[data-action="new"] .menu-text');
   C.eq(await mnemonic.locator('.menu-mnemonic').evaluate(el=>getComputedStyle(el).color),'rgb(170, 0, 0)','Borland Pascal: буква пункта выпадающего меню красная');
   await mnemonic.hover();
   C.eq(await mnemonic.locator('.menu-mnemonic').evaluate(el=>getComputedStyle(el).color),'rgb(255, 85, 85)','Borland Pascal: буква выделенного пункта остаётся контрастной');
  }
  C.eq(await row.evaluate(el=>getComputedStyle(el).color),fg,theme+': текст меню');
  await row.hover();
  await S.page.waitForFunction(({theme,selected})=>getComputedStyle(document.querySelector('[data-action="open"]')).backgroundColor===selected,{theme,selected});
  C.eq(await row.evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 255)',theme+': выделенный текст белый');
  C.eq(await row.locator('.menu-shortcut').evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 255)',theme+': сочетание клавиш читаемо в выделении');
  await S.page.locator('#recent-submenu > .submenu-trigger').hover();
  const recent=S.page.locator('#recent-files-list');await recent.waitFor({state:'visible'});
  await S.page.waitForTimeout(30);
  C.eq(await recent.evaluate(el=>getComputedStyle(el).backgroundColor),bg,theme+': список недавних файлов');
  await S.screenshot(theme+'-reference-menu');
  await closeMenus();
  await S.page.locator('[data-menu="view"] > .menu-label').dispatchEvent('mousedown');
  await S.page.locator('#lang-submenu > .submenu-trigger').hover();
  C.eq(await S.page.locator('#lang-list').evaluate(el=>getComputedStyle(el).backgroundColor),bg,theme+': вложенное меню языка');
  await closeMenus();
  await S.page.locator('.monaco-editor .view-line').first().click({button:'right'});
  C.eq(await S.page.locator('#ax-context-menu').evaluate(el=>getComputedStyle(el).backgroundColor),bg,theme+': контекстное меню редактора');
  await closeMenus();
  await S.page.locator('#terminal').click({button:'right',position:{x:30,y:30}});
  C.eq(await S.page.locator('#ax-console-context-menu').evaluate(el=>getComputedStyle(el).backgroundColor),bg,theme+': контекстное меню консоли');
  if(theme==='basic'){
   const disabled=await S.page.locator('#ax-console-context-menu [data-action="copy"]').evaluate(el=>({color:getComputedStyle(el).color,opacity:getComputedStyle(el).opacity}));
   C.eq(disabled.color,'rgb(85, 85, 85)','QuickBASIC: недоступная команда тёмно-серая');
   C.eq(disabled.opacity,'1','QuickBASIC: недоступный текст сохраняет заданный оттенок');
  }
  const clear=S.page.locator('#ax-console-context-menu [data-action="clear"]');await S.page.keyboard.press('ArrowDown');
  await S.page.waitForFunction(selected=>{const s=getComputedStyle(document.querySelector('#ax-console-context-menu [data-action="clear"]'));return s.backgroundColor===selected&&s.color==='rgb(255, 255, 255)';},selected);
  C.eq(await clear.evaluate(el=>getComputedStyle(el).backgroundColor),selected,theme+': выделение меню клавиатурой');
  C.eq(await clear.evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 255)',theme+': контрастный фокус клавиатуры');
  await closeMenus();
 }
 for(const [theme,fieldBg,fieldFg,buttonBg,previewBg] of [
  ['basic','rgb(192, 192, 192)','rgb(0, 0, 0)','rgb(192, 192, 192)','rgb(0, 0, 128)'],
  ['borland','rgb(0, 170, 170)','rgb(0, 0, 0)','rgb(0, 170, 0)','rgb(0, 0, 170)']
 ]) {
  await S.page.evaluate(theme=>window.__ide.setTheme(theme),theme);
  await S.page.locator('#menu-settings').click();await S.page.locator('#settings-tab-font').click();
  const dialog=S.page.locator('.editor-settings-dialog');
  await S.page.waitForFunction(theme=>document.querySelector('#settings-tab-font')?.getAttribute('aria-selected')==='true'&&document.documentElement.dataset.theme===theme,theme);
  C.eq(await dialog.evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(192, 192, 192)',theme+': серый фон окна настроек');
  C.eq(await dialog.locator('#settings-theme').evaluate(el=>getComputedStyle(el).backgroundColor),fieldBg,theme+': фон поля выбора темы');
  C.eq(await dialog.locator('#settings-theme').evaluate(el=>getComputedStyle(el).color),fieldFg,theme+': контрастный текст поля выбора темы');
  C.eq(await dialog.locator('.settings-content').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(192, 192, 192)',theme+': серая поверхность содержимого');
  C.eq(await dialog.locator('#settings-tab-font').getAttribute('aria-selected'),'true',theme+': активна вкладка шрифта и темы');
  C.eq(await dialog.locator('.settings-footer .settings-primary').evaluate(el=>getComputedStyle(el).backgroundColor),buttonBg,theme+': цвет основной кнопки');
  if(theme==='borland'){
   C.eq(await dialog.locator('.settings-search').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(192, 192, 192)','Borland Pascal: поле поиска без синей заливки');
   C.eq(await dialog.locator('.settings-footer .settings-primary .settings-button-default-text').evaluate(el=>getComputedStyle(el).color),'rgb(85, 255, 255)','Borland Pascal: основной текст кнопки бирюзовый');
   C.eq(await dialog.locator('.settings-footer .settings-primary .settings-button-hotkey').evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 85)','Borland Pascal: клавиша кнопки выделена жёлтым');
  }
  C.eq(await dialog.locator('.settings-preview').evaluate(el=>getComputedStyle(el).backgroundColor),previewBg,theme+': предпросмотр сохраняет цвет редактора');
  const shadow=await dialog.evaluate(el=>getComputedStyle(el).boxShadow);
  C.ok(shadow.includes('7px 7px 0px')&&shadow.includes('inset'),theme+': рамка и чёткая тень окна');
  await S.screenshot(theme+'-reference-settings');
  await S.page.keyboard.press('Escape');
 }
 for(const [theme,bg,highlight,fg] of [
  ['dark','rgb(37, 46, 62)','rgb(53, 63, 80)','rgb(228, 235, 245)'],
  ['light','rgb(255, 255, 255)','rgb(220, 231, 245)','rgb(35, 50, 71)'],
  ['basic','rgb(192, 192, 192)','rgb(0, 0, 0)','rgb(255, 255, 255)'],
  ['borland','rgb(0, 170, 170)','rgb(0, 170, 0)','rgb(255, 255, 255)']
 ]) {
  await S.page.evaluate(theme=>window.__ide.setTheme(theme),theme);
  await S.page.locator('#menu-settings').click();
  await S.page.locator('#settings-tab-appearance').click();
  const select=S.page.locator('#settings-language');
  await select.click();
  const option=select.locator('option[value="ru"]');
  await option.hover();
  await S.page.waitForFunction(({highlight,fg})=>{const s=getComputedStyle(document.querySelector('#settings-language option[value="ru"]'));return s.backgroundColor===highlight&&s.color===fg;},{highlight,fg});
  C.eq(await select.evaluate(el=>getComputedStyle(el,'::picker(select)').backgroundColor),bg,theme+': фон реально раскрытого списка');
  C.eq(await option.evaluate(el=>getComputedStyle(el).backgroundColor),highlight,theme+': наведение в раскрытом списке соответствует палитре');
  C.eq(await option.evaluate(el=>getComputedStyle(el).color),fg,theme+': текст наведённого пункта читаем');
  await S.screenshot(theme+'-reference-open-select');
  await option.click();
  await S.page.keyboard.press('Escape');
 }
 await S.page.evaluate(()=>window.__ide.setTheme('borland'));
 await S.page.locator('[data-menu="help"] > .menu-label').dispatchEvent('mousedown');
 await S.page.locator('[data-action="about"]').click();
 const about=S.page.locator('.about-dialog');
 await about.waitFor({state:'visible'});
 C.eq(await about.evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(192, 192, 192)','Borland Pascal: окно «О программе» серое');
 C.eq(await about.evaluate(el=>getComputedStyle(el).color),'rgb(0, 0, 0)','Borland Pascal: текст окна «О программе» чёрный');
 C.eq(await about.locator('.about-row code').first().evaluate(el=>getComputedStyle(el).backgroundColor),'rgba(0, 0, 0, 0)','Borland Pascal: версии без цветных плашек');
 C.eq(await about.locator('#about-close').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 170, 0)','Borland Pascal: кнопка окна «О программе» зелёная');
 C.eq(await about.locator('#about-close .about-button-default-text').evaluate(el=>getComputedStyle(el).color),'rgb(85, 255, 255)','Borland Pascal: первая буква ОК светло-бирюзовая');
 C.eq(await about.locator('#about-close .about-button-hotkey').evaluate(el=>getComputedStyle(el).color),'rgb(255, 255, 85)','Borland Pascal: клавиша К в ОК жёлтая');
 await S.screenshot('borland-reference-about');
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}}];
