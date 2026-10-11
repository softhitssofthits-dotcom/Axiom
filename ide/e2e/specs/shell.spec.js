const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'shell-responsive-commands',name:'Утверждённая оболочка: темы, меню, размеры и команды',async fn({S,C}){
const file=writeAxm(testWorkspace('shell'),'расчёт.axm','PRINT 42\n');await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
C.eq(await S.page.locator('#shell-sidebar #outline-list').count(),1,'Структура расположена слева');
for(const width of [320,520,800,1280]){await S.electronApp.evaluate(({BrowserWindow},w)=>{const b=BrowserWindow.getAllWindows()[0];b.setMinimumSize(0,0);b.setSize(w,800);},width);await S.page.waitForTimeout(150);for(const theme of ['light','dark']){await S.page.evaluate(t=>window.__ide.handleAction('theme-'+t),theme);const layout=await S.page.evaluate(()=>({overflow:document.documentElement.scrollWidth>innerWidth,clipped:[...document.querySelectorAll('#toolbar button,#menubar > .menu-item')].filter(b=>b.getBoundingClientRect().width).some(b=>b.getBoundingClientRect().right>innerWidth),labels:[...document.querySelectorAll('#toolbar button')].every(b=>!b.textContent.trim()&&b.getAttribute('aria-label'))}));C.ok(!layout.overflow&&!layout.clipped&&layout.labels,theme+' '+width+' без обрезания и подписей',layout);}}
await S.page.locator('#menu-overflow').isVisible().then(async visible=>{if(visible){await S.page.locator('#menu-overflow').click();C.ok(await S.page.locator('#menu-overflow-content .menu-item').count()>0,'Лишние меню доступны');}});
await S.electronApp.evaluate(({BrowserWindow})=>BrowserWindow.getAllWindows()[0].setSize(1280,900));await S.page.locator('#tb-run').click();await S.waitIdle();await S.waitConsole('42');C.ok(true,'Пиктограмма запуска исполняет VM');C.eq(S.pageErrors.length,0,'Ошибки страницы отсутствуют');await S.screenshot('shell-approved');
}},
{id:'shell-toolbar-history-and-panel-state',name:'Индикатор нижней панели, отмена и повтор на панели инструментов',async fn({S,C}){
 const file=writeAxm(testWorkspace('toolbar-history'),'history.axm','PRINT 42\n');
 await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'Свежий документ не имеет истории');
 C.eq(await S.page.locator('#tb-check .codicon-check').count(),1,'Проверка обозначена галочкой');
 const groups=await S.page.locator('#toolbar .command-group').evaluateAll(groups=>groups.map(g=>({ids:[...g.querySelectorAll('button')].map(b=>b.id),border:getComputedStyle(g).borderRightWidth})));
 C.ok(groups[0].ids.at(-1)==='tb-save-all'&&groups[1].ids.join(',')==='tb-undo,tb-redo'&&groups[0].border==='1px'&&groups[1].border==='1px','Стрелки идут после Сохранить всё и отделены с обеих сторон');
 await S.page.evaluate(()=>{const i=window.__ide,m=i.editor.getModel();m.pushStackElement();i.editor.executeEdits('test',[{range:new window.monaco.Range(1,7,1,9),text:'99'}]);m.pushStackElement();});
 await S.page.waitForFunction(()=>!document.getElementById('tb-undo').disabled);
 await S.page.locator('#tb-undo').click();C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 42\n','Стрелка отменяет правку');
 await S.page.locator('#tb-redo').click();C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 99\n','Стрелка повторяет правку');
 await S.page.evaluate(()=>window.__ide.editor.updateOptions({readOnly:true}));C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'История отключена в режиме только чтения');
 await S.page.evaluate(()=>window.__ide.editor.updateOptions({readOnly:false}));
 await S.page.evaluate(()=>window.__ide.handleAction('new'));C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'Новая вкладка не наследует историю');
 await S.page.evaluate(p=>window.__ide.switchToFile(p),file);C.ok(!await S.page.locator('#tb-undo').isDisabled(),'Возврат на вкладку восстанавливает историю');
 const settlePanelColors=()=>S.page.evaluate(async()=>{const buttons=[document.getElementById('tb-toggle-output'),document.getElementById('tb-toggle-sidebar')];buttons.forEach(b=>getComputedStyle(b).color);await Promise.all(buttons.flatMap(b=>b.getAnimations()).map(a=>a.finished.catch(()=>{})));});
 for(const theme of ['light','dark','basic']){
   await S.page.evaluate(t=>{window.__ide.setTheme(t);window.__ide.setSidebarVisible(true);window.__ide.showPanel();},theme);
   await settlePanelColors();
   const enabled=await S.page.locator('#tb-toggle-output').evaluate(b=>({pressed:b.getAttribute('aria-pressed'),color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   const side=await S.page.locator('#tb-toggle-sidebar').evaluate(b=>({color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   C.ok(enabled.pressed==='true'&&enabled.color===side.color&&enabled.bg===side.bg,theme+': активные панели выделены одинаково',JSON.stringify({enabled,side}));
   await S.screenshot('toolbar-'+theme);
   await S.page.evaluate(()=>window.__ide.setBottomPanelVisible(false));
   await settlePanelColors();
   const disabled=await S.page.locator('#tb-toggle-output').evaluate(b=>({pressed:b.getAttribute('aria-pressed'),color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   C.ok(disabled.pressed==='false'&&(disabled.color!==enabled.color||disabled.bg!==enabled.bg),theme+': скрытая панель не выделена');
 }
 await S.page.locator('#tb-toggle-output').click();C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'true','Кнопка включает индикатор');
 await S.page.keyboard.press('Control+Shift+o');C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'false','Клавиатура выключает индикатор');
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));C.eq(await S.page.locator('#tb-undo').getAttribute('aria-label'),'Undo (Ctrl+Z)','Подсказка отмены переводится');C.eq(await S.page.locator('#tb-redo').getAttribute('title'),'Redo (Ctrl+Y)','Подсказка повтора переводится');
 await S.close();await S.launch();C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'false','Индикатор восстанавливается из профиля');
}}];
