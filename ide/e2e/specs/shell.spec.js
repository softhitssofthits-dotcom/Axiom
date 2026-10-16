const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'shell-responsive-commands',name:'Утверждённая оболочка: темы, меню, размеры и команды',async fn({S,C}){
const file=writeAxm(testWorkspace('shell'),'расчёт.axm','PRINT 42\n');await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
C.eq(await S.page.locator('#shell-sidebar #outline-list').count(),1,'Структура расположена слева');
for(const width of [320,520,800,1280]){await S.electronApp.evaluate(({BrowserWindow},w)=>{const b=BrowserWindow.getAllWindows()[0];b.setMinimumSize(0,0);b.setSize(w,800);},width);await S.page.waitForTimeout(150);for(const theme of ['light','dark','basic','borland']){await S.page.evaluate(t=>window.__ide.handleAction('theme-'+t),theme);const layout=await S.page.evaluate(()=>({overflow:document.documentElement.scrollWidth>innerWidth,clipped:[...document.querySelectorAll('#toolbar button,#menubar > .menu-item')].filter(b=>b.getBoundingClientRect().width).some(b=>b.getBoundingClientRect().right>innerWidth),labels:[...document.querySelectorAll('#toolbar button')].every(b=>!b.textContent.trim()&&b.getAttribute('aria-label'))}));C.ok(!layout.overflow&&!layout.clipped&&layout.labels,theme+' '+width+' без обрезания и подписей',layout);}}
await S.page.locator('#menu-overflow').isVisible().then(async visible=>{if(visible){await S.page.locator('#menu-overflow').click();C.ok(await S.page.locator('#menu-overflow-content .menu-item').count()>0,'Лишние меню доступны');}});
await S.electronApp.evaluate(({BrowserWindow})=>BrowserWindow.getAllWindows()[0].setSize(1280,900));await S.page.locator('#tb-run').click();await S.waitIdle();await S.waitConsole('42');C.ok(true,'Пиктограмма запуска исполняет VM');C.eq(S.pageErrors.length,0,'Ошибки страницы отсутствуют');await S.screenshot('shell-approved');
}},
{id:'shell-toolbar-history-and-panel-state',name:'Индикатор нижней панели, отмена и повтор на панели инструментов',async fn({S,C}){
 const file=writeAxm(testWorkspace('toolbar-history'),'history.axm','PRINT 42\n');
 await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'Свежий документ не имеет истории');
 C.eq(await S.page.locator('#tb-check .codicon-check').count(),1,'Проверка обозначена галочкой');
 const groups=await S.page.locator('#toolbar .command-group').evaluateAll(groups=>groups.map(g=>({ids:[...g.querySelectorAll('button')].map(b=>b.id),border:getComputedStyle(g).borderRightWidth})));
 C.ok(groups[0].ids.at(-1)==='tb-save-all'&&groups[1].ids.join(',')==='tb-undo,tb-redo'&&parseFloat(groups[0].border)>0&&parseFloat(groups[1].border)>0,'Стрелки идут после Сохранить всё и отделены с обеих сторон',JSON.stringify(groups));
 await S.page.evaluate(()=>{const i=window.__ide,m=i.editor.getModel();m.pushStackElement();i.editor.executeEdits('test',[{range:new window.monaco.Range(1,7,1,9),text:'99'}]);m.pushStackElement();});
 await S.page.waitForFunction(()=>!document.getElementById('tb-undo').disabled);
 await S.page.locator('#tb-undo').click();C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 42\n','Стрелка отменяет правку');
 await S.page.locator('#tb-redo').click();C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 99\n','Стрелка повторяет правку');
 await S.page.evaluate(()=>window.__ide.editor.updateOptions({readOnly:true}));C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'История отключена в режиме только чтения');
 await S.page.evaluate(()=>window.__ide.editor.updateOptions({readOnly:false}));
 await S.page.evaluate(()=>window.__ide.handleAction('new'));C.ok(await S.page.locator('#tb-undo').isDisabled()&&await S.page.locator('#tb-redo').isDisabled(),'Новая вкладка не наследует историю');
 await S.page.evaluate(p=>window.__ide.switchToFile(p),file);C.ok(!await S.page.locator('#tb-undo').isDisabled(),'Возврат на вкладку восстанавливает историю');
 const settlePanelColors=()=>S.page.evaluate(async()=>{const buttons=[document.getElementById('tb-toggle-output'),document.getElementById('tb-toggle-sidebar')];buttons.forEach(b=>getComputedStyle(b).color);await Promise.all(buttons.flatMap(b=>b.getAnimations()).map(a=>a.finished.catch(()=>{})));});
 for(const theme of ['light','dark','basic','borland']){
   await S.page.evaluate(t=>{window.__ide.setTheme(t);window.__ide.setSidebarVisible(true);window.__ide.showPanel();},theme);
   await settlePanelColors();
   const commandColors=await S.page.evaluate(()=>['tb-run','tb-stop'].map(id=>getComputedStyle(document.getElementById(id)).color.match(/\d+/g).map(Number)));
   C.ok(commandColors[0][1]>commandColors[0][0]&&commandColors[0][1]>commandColors[0][2],theme+': значок запуска остаётся зелёным');
   C.ok(commandColors[1][0]>commandColors[1][1]&&commandColors[1][0]>commandColors[1][2],theme+': значок остановки остаётся красным');
   const enabled=await S.page.locator('#tb-toggle-output').evaluate(b=>({pressed:b.getAttribute('aria-pressed'),color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   const side=await S.page.locator('#tb-toggle-sidebar').evaluate(b=>({color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   C.ok(enabled.pressed==='true'&&enabled.color===side.color&&enabled.bg===side.bg,theme+': активные панели выделены одинаково',JSON.stringify({enabled,side}));
   if(theme==='borland'){
     C.eq(enabled.color,'rgb(0, 0, 0)','Borland Pascal: включённая панель использует чёрный значок');
     C.eq(enabled.bg,'rgba(0, 0, 0, 0)','Borland Pascal: включённая панель без отдельной заливки');
     await S.page.keyboard.press('Tab');
     await S.page.locator('#tb-toggle-sidebar').focus();
     const focus=await S.page.locator('#tb-toggle-sidebar').evaluate(b=>getComputedStyle(b).outlineColor);
     C.eq(focus,'rgb(85, 255, 255)','Borland Pascal: фокус панели обведён бирюзовым, не жёлтым');
     await S.page.evaluate(()=>window.__ide.editor.focus());
     const surfaces=await S.page.evaluate(()=>Object.fromEntries([
       ['sidebar','#shell-sidebar'],['sidebar-divider','#sidebar-resizer'],['document-tabs','#document-tab-bar'],
       ['toolbar','#toolbar'],['bottom-panel','#bottom-panel'],['panel-tabs','#panel-tabs'],['console','#console-content'],
     ].map(([key,selector])=>[key,getComputedStyle(document.querySelector(selector)).backgroundColor])));
      C.eq(surfaces.sidebar,'rgb(0, 0, 128)','Borland Pascal: боковая колонка сохраняет глубину синего');
      const dividers=await S.page.evaluate(()=>['sidebar-resizer','panel-resizer'].map(id=>{const el=document.getElementById(id),line=getComputedStyle(el,'::before');return{background:getComputedStyle(el).backgroundColor,lineColor:line.backgroundColor,width:line.width,height:line.height};}));
      C.eq(dividers[0].background,'rgba(0, 0, 0, 0)','Borland Pascal: полоса разделителя не заливается отдельным цветом');
      C.eq(dividers[1].background,'rgba(0, 0, 0, 0)','Borland Pascal: полоса изменения высоты консоли не заливается отдельным цветом');
      C.eq(dividers[0].lineColor,'rgb(0, 170, 170)','Borland Pascal: вертикальный разделитель рисуется видимой бирюзовой линией');
      C.eq(dividers[0].width,'1px','Borland Pascal: вертикальный разделитель толщиной 1px');
      C.eq(dividers[1].height,'1px','Borland Pascal: горизонтальный разделитель толщиной 1px');
     C.eq(surfaces['document-tabs'],'rgb(136, 136, 136)','Borland Pascal: подложка вкладок ещё темнее панели инструментов');
     const inactiveTab=S.page.locator('.editor-tab:not(.active)').first();
     await inactiveTab.hover();
     await S.page.waitForFunction(()=>{const el=document.querySelector('.editor-tab:not(.active)');return el&&getComputedStyle(el).backgroundColor==='rgb(170, 170, 170)';});
     C.eq(await inactiveTab.evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(170, 170, 170)','Borland Pascal: наведение на неактивную вкладку использует серый оттенок шапки');
     C.eq(surfaces.toolbar,'rgb(153, 153, 153)','Borland Pascal: панель инструментов немного темнее серой шапки');
     C.eq(surfaces['bottom-panel'],'rgb(0, 0, 128)','Borland Pascal: нижняя панель продолжает синюю гамму рабочего поля');
     C.eq(surfaces['panel-tabs'],'rgb(0, 0, 85)','Borland Pascal: шапка вывода темнее консоли, но в той же синей гамме');
     C.eq(surfaces.console,'rgb(0, 0, 128)','Borland Pascal: консоль синяя в тон рабочему полю, а не чёрная');
     C.eq(await S.page.locator('.panel-tab.active').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 170, 0)','Borland Pascal: активная вкладка использует зелёную роль выбора');
     await S.page.locator('.panel-tab[data-tab="diagnostics"]').click();
     for(const filter of ['all','error','warning','advice']){
       const button=S.page.locator(`[data-diagnostic-filter="${filter}"]`);
       await button.click();
       await S.page.waitForFunction(filter=>getComputedStyle(document.querySelector(`[data-diagnostic-filter="${filter}"]`)).backgroundColor==='rgb(0, 170, 0)',filter);
       C.eq(await button.evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 170, 0)','Borland Pascal: зелёное выделение фильтра '+filter);
     }
     await S.page.evaluate(()=>window.__ide.applyDiagnostics([
       {file:window.__ide.activeFile,line:1,col:1,severity:'error',code:'MB0001',message:'Ошибка для проверки контраста'},
       {file:window.__ide.activeFile,line:1,col:1,severity:'warning',code:'MB2046',message:'Предупреждение для проверки контраста'},
       {file:window.__ide.activeFile,line:1,col:1,severity:'warning',code:'MB2047',message:'Совет для проверки контраста'}
     ]));
     for(const category of ['error','warning','advice']){
       const row=S.page.locator(`.diagnostic-item[data-category="${category}"]`);
       await row.hover();
       await S.page.waitForFunction(category=>{const el=document.querySelector(`.diagnostic-item[data-category="${category}"]`);return getComputedStyle(el).backgroundColor==='rgb(0, 170, 0)'&&getComputedStyle(el).color==='rgb(0, 0, 0)';},category);
       C.ok(await row.evaluate(el=>[el,...el.querySelectorAll('span')].every(node=>getComputedStyle(node).color==='rgb(0, 0, 0)')),'Borland Pascal: текст, значок и координаты '+category+' чёрные на зелёном выделении');
     }
     await S.screenshot('borland-diagnostic-hover');
     await S.page.evaluate(()=>window.__ide.applyDiagnostics([]));
     await S.page.locator('.panel-tab[data-tab="console"]').click();
     await S.page.mouse.move(0,0);
   }
   await S.screenshot('toolbar-'+theme);
   await S.page.evaluate(()=>window.__ide.setBottomPanelVisible(false));
   await settlePanelColors();
   const disabled=await S.page.locator('#tb-toggle-output').evaluate(b=>({pressed:b.getAttribute('aria-pressed'),color:getComputedStyle(b).color,bg:getComputedStyle(b).backgroundColor}));
   C.ok(disabled.pressed==='false'&&(disabled.color!==enabled.color||disabled.bg!==enabled.bg),theme+': скрытая панель не выделена');
   if(theme==='borland') C.eq(disabled.color,'rgb(51, 51, 51)','Borland Pascal: выключенная панель тёмно-серая как неактивная вкладка');
 }
 await S.page.locator('#tb-toggle-output').click();C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'true','Кнопка включает индикатор');
 await S.page.keyboard.press('Control+Shift+o');C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'false','Клавиатура выключает индикатор');
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));C.eq(await S.page.locator('#tb-undo').getAttribute('aria-label'),'Undo (Ctrl+Z)','Подсказка отмены переводится');C.eq(await S.page.locator('#tb-redo').getAttribute('title'),'Redo (Ctrl+Y)','Подсказка повтора переводится');
 await S.close();await S.launch();C.eq(await S.page.locator('#tb-toggle-output').getAttribute('aria-pressed'),'false','Индикатор восстанавливается из профиля');
}}];

module.exports.push({
 id:'shell-trace-startup-readiness',
 name:'Команды трассировки недоступны до создания контроллера и работают после запуска',
 async fn({S,C}) {
  await S.launch();
  await S.page.addInitScript(() => {
   let ide;
   Object.defineProperty(window,'__ide',{configurable:true,get:()=>ide,set:value=>{
    ide=value;
    if(!value || value.trace) return;
    const buttons=[...document.querySelectorAll('#toolbar [data-trace-command]')];
    window.__traceStartup={count:buttons.length,disabled:buttons.every(b=>b.disabled)};
    // Воспроизводим событие в промежутке между созданием оболочки и контроллера.
    for(const button of buttons) button.dispatchEvent(new MouseEvent('click',{bubbles:true}));
   }});
  });
  await S.page.reload();
  await S.page.waitForSelector('#app.ready');
  const startup=await S.page.evaluate(()=>window.__traceStartup);
  C.eq(startup.count,9,'Проверены все девять команд панели до готовности');
  C.ok(startup.disabled,'До готовности контроллера кнопки отключены');
  C.eq(S.pageErrors.length,0,'Ранние события не вызывают ошибок страницы');
  const file=writeAxm(testWorkspace('trace-startup'),'ready.axm','PRINT 1\n');
  await S.page.evaluate(p=>window.__ide.openFile(p),file);
  await S.page.locator('#tb-breakpoint').click();
  C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).length),1,'После готовности команда ставит точку останова');
  await S.page.locator('#tb-clear').click();
  C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).length),0,'Команда удаления точек также работает');
  C.eq(S.pageErrors.length,0,'Работа команд после запуска не вызывает ошибок страницы');
 }
});
