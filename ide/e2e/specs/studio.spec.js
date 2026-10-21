const {testWorkspace,writeAxm}=require('../helpers');

const focusedId=S=>S.page.evaluate(()=>document.activeElement.id);
const editorClick=S=>S.page.locator('#editor-container').click({position:{x:100,y:80}});
async function returnOutline(S){
  await S.page.locator('#sidebar-rail [data-side="outline"]').focus();
  await S.page.keyboard.press('Enter');
}

module.exports=[{
  id:'studio-panel-controls',
  name:'Панели Studio: узкая структура, закрепление, возврат и сброс размещения',
  async fn({S,C}){
    const root=testWorkspace('studio-panel-controls');
    const main=writeAxm(root,'main.axm','PRINT 1\n');
    const file=writeAxm(root,'module.axm','SUB Zebra()\nEND SUB\n\nSUB Alpha()\nEND SUB\n');
    await S.launch({settings:{sidebarWidth:180,smartIdentifierEnabled:false}});
    await S.stubOpenDialog([root]);
    await S.page.evaluate(()=>window.__ide.handleAction('open-project'));
    await S.page.waitForFunction(p=>window.__ide.projectDir===p&&!window.__ide.projectSession.loading,root);
    await S.stubOpenDialog([file]);
    await S.page.evaluate(()=>window.__ide.handleAction('open'));
    await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
    const source="' Несохранённая правка\nSUB Zebra()\nEND SUB\n\nSUB Alpha()\nEND SUB\n";
    await S.editorType(source);
    await S.page.locator('#sidebar-tab-outline').click();
    await S.page.waitForSelector('#outline-more > summary');
    C.eq(await S.page.locator('#structure-content').isVisible(),true,'Открыта структура временного модуля');
    C.eq(Math.round((await S.page.locator('#shell-sidebar').boundingBox()).width),180,'Проверяется минимальная ширина колонки');
    C.eq(await S.page.locator('#sidebar-pin').getAttribute('aria-pressed'),'true','Первоначально область закреплена');
    await S.page.locator('#outline-more > summary').click();
    C.eq(await S.page.locator('.studio-outline-extra').isVisible(),true,'Меню структуры открывается кнопкой');
    const sidebar=await S.page.locator('#shell-sidebar').boundingBox();
    const menu=await S.page.locator('.studio-outline-extra').boundingBox();
    if(!sidebar||!menu)throw new Error('Не получены видимые границы структуры и её меню');
    C.ok(menu.x>=sidebar.x-1,'Левая граница меню находится внутри колонки');
    C.ok(menu.x+menu.width<=sidebar.x+sidebar.width+1,'Правая граница меню находится внутри колонки');
    C.ok(menu.y>=sidebar.y&&menu.y+menu.height<=sidebar.y+sidebar.height,'Меню целиком помещается по высоте');
    await S.page.locator('#outline-sort').selectOption('name');
    C.eq(await S.page.locator('#outline-sort').inputValue(),'name','В раскрытом меню можно выбрать сортировку');
    await S.page.waitForFunction(()=>window.__ide.settings.outlineSort==='name');
    C.eq(await S.page.evaluate(()=>window.__ide.settings.outlineSort),'name','Выбор сортировки сохраняется');
    await S.page.locator('#outline-more > summary').click();
    await S.page.locator('#sidebar-pin').click();
    C.eq(await S.page.locator('#sidebar-pin').getAttribute('aria-pressed'),'false','Область откреплена');
    await editorClick(S);
    C.eq(await S.page.locator('#shell-sidebar').isVisible(),false,'Щелчок редактора скрывает откреплённую область');
    C.eq(await S.page.locator('#sidebar-rail').isVisible(),true,'Доступны подписанные кнопки возврата');
    await returnOutline(S);
    C.eq(await S.page.locator('#structure-content').isVisible(),true,'Enter возвращает структуру');
    C.eq(await focusedId(S),'sidebar-tab-outline','Возврат переносит фокус в выбранную вкладку');
    await S.page.keyboard.press('Escape');
    C.eq(await S.page.locator('#shell-sidebar').isVisible(),false,'Escape скрывает откреплённую структуру');
    await returnOutline(S);
    await S.page.locator('#sidebar-pin').click();
    C.eq(await S.page.locator('#sidebar-pin').getAttribute('aria-pressed'),'true','Область вновь закреплена');
    await editorClick(S);
    C.eq(await S.page.locator('#shell-sidebar').isVisible(),true,'Закреплённая область остаётся при работе в редакторе');
    C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'До сброса выбран основной файл проекта');
    await S.page.locator('#studio-panel-close').click();
    C.eq(await S.page.locator('#bottom-panel').isVisible(),false,'Крестик скрывает нижнюю панель');
    await S.page.locator('#studio-panel-return').focus();
    await S.page.keyboard.press('Enter');
    C.eq(await S.page.locator('#bottom-panel').isVisible(),true,'Enter возвращает нижнюю панель');
    C.eq(await S.page.locator('.panel-tab.active').evaluate(el=>el===document.activeElement),true,'Фокус переходит в активную нижнюю вкладку');
    await S.page.locator('#sidebar-close').click();
    await S.page.locator('#studio-panel-close').click();
    await S.page.evaluate(()=>window.__ide.handleAction('reset-layout'));
    await S.page.waitForFunction(()=>Math.round(document.getElementById('shell-sidebar').getBoundingClientRect().width)===300&&Math.round(document.getElementById('bottom-panel').getBoundingClientRect().height)===200);
    C.eq(Math.round((await S.page.locator('#shell-sidebar').boundingBox()).width),300,'Сброс восстанавливает ширину 300');
    C.eq(Math.round((await S.page.locator('#bottom-panel').boundingBox()).height),200,'Сброс восстанавливает высоту 200');
    C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Сброс возвращает проект');
    C.eq(await S.page.locator('#bottom-panel').isVisible(),true,'Сброс возвращает нижнюю панель');
    C.eq(await S.page.locator('#sidebar-pin').getAttribute('aria-pressed'),'true','Сброс закрепляет область');
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),file,'Сброс сохраняет активный документ');
    C.eq(await S.editorValue(),source,'Сброс сохраняет исходный текст');
    C.eq((await S.activeTab()).dirty,true,'Сброс сохраняет признак правок');
    C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Сброс сохраняет цель выполнения');
    C.eq(S.pageErrors.length,0,'Управление панелями не вызывает ошибок страницы');
  }
},{
  id:'studio-layout-migration',
  name:'Повреждённая вкладка и старые настройки боковой области безопасно восстанавливаются',
  async fn({S,C}){
    await S.launch({settings:{sidebarVisible:false,projectExplorer:false,sidebarTab:'bad',smartIdentifierEnabled:false}});
    C.eq(await S.page.locator('#shell-sidebar').isVisible(),false,'Старое скрытое состояние сохраняется');
    C.eq(await S.page.locator('#sidebar-rail [data-side="outline"]').isVisible(),true,'Структуру можно вернуть при повреждённой настройке вкладки');
    await returnOutline(S);
    C.eq(await S.page.locator('#structure-content').isVisible(),true,'Возврат открывает структуру');
    C.eq(await focusedId(S),'sidebar-tab-outline','После возврата вкладка получает фокус');
    await S.page.locator('#sidebar-close').click();
    const profile=S.userData;
    await S.close();
    await S.launch({userData:profile});
    C.eq(await S.page.locator('#shell-sidebar').isVisible(),false,'Скрытие сохраняется при перезапуске');
    await returnOutline(S);
    C.eq(await S.page.locator('#sidebar-tab-outline').getAttribute('aria-selected'),'true','Профиль восстанавливает выбранную структуру');
    C.eq(await focusedId(S),'sidebar-tab-outline','После перезапуска работает клавиатурный возврат');
    C.eq(S.pageErrors.length,0,'Миграция и восстановление не вызывают ошибок страницы');
  }
}, {
  id:'studio-effective-run-target',name:'Общая цель запуска честно отражает внешний файл и явный выбор проекта',async fn({S,C}){
    const root=testWorkspace('studio-target'),main=writeAxm(root,'main.axm','PRINT "PROJECT_MAIN"\n');
    const external=writeAxm(testWorkspace('studio-external'),'outside.axm','PRINT "EXTERNAL_FILE"\n');
    await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),root);
    await S.page.evaluate(p=>window.__ide.openFile(p),external);
    C.eq(await S.page.locator('#project-entry').inputValue(),external,'Общая панель показывает фактически выполняемый внешний файл');
    C.eq(await S.page.evaluate(()=>window.__ide.projectSession.entry),main,'Выбор запуска проекта сохранён отдельно');
    await S.page.locator('#tb-run').click();await S.waitConsole('EXTERNAL_FILE');await S.waitIdle();
    C.ok(true,'Внешний файл выполняется согласно общей панели');
    await S.page.locator('#project-entry').selectOption(main);
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Явный выбор запуска проекта переключает внешний документ');
    C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Действительная цель совпадает со списком');
    await S.page.locator('#tb-run').click();await S.waitConsole('PROJECT_MAIN');await S.waitIdle();
    C.ok(true,'Выбранный проект запускается');
    await S.page.evaluate(p=>window.__ide.openFile(p),external);
    await S.page.locator('#project-files .project-file').first().click({button:'right'});
    await S.page.locator('#ax-project-context-menu [data-action="use-as-entry"]').click();
    C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Контекстный выбор также задаёт действительную цель');
    C.eq(S.pageErrors.length,0,'Смена цели без ошибок страницы');
  }
}, {
  id:'studio-keyboard-menus',name:'Меню и дополнительные команды доступны с клавиатуры без скрытого фокуса',async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    const view=S.page.locator('[data-menu="view"] > .menu-label');
    C.eq(await view.getAttribute('tabindex'),'0','Меню входит в порядок клавиатурного фокуса');
    await view.focus();await S.page.keyboard.press('Enter');
    C.eq(await S.page.evaluate(()=>document.activeElement.dataset.action),'show-project','Enter открывает меню и выбирает первую команду');
    await S.page.keyboard.press('ArrowDown');await S.page.keyboard.press('Enter');
    C.eq(await S.page.locator('#sidebar-tab-outline').getAttribute('aria-selected'),'true','Стрелка и Enter выбирают структуру');
    await S.page.locator('#toolbar-overflow > summary').focus();await S.page.keyboard.press('Enter');
    await S.page.locator('#tb-breakpoint').focus();await S.page.keyboard.press('Enter');
    C.eq(await S.page.locator('#toolbar-overflow').evaluate(el=>el.open),false,'Команда закрывает дополнительное меню');
    C.eq(await S.page.evaluate(()=>document.activeElement.tagName),'SUMMARY','Фокус возвращается на видимую кнопку меню');
    const tools=S.page.locator('[data-menu="tools"] > .menu-label');await tools.focus();await S.page.keyboard.press('Enter');
    await S.page.keyboard.press('Escape');
    C.eq(await tools.getAttribute('aria-expanded'),'false','Escape закрывает меню');
    C.eq(await tools.evaluate(el=>el===document.activeElement),true,'Escape возвращает фокус заголовку');
    await S.page.locator('#panel-tab-console').focus();await S.page.keyboard.press('ArrowRight');
    C.eq(await S.page.locator('#panel-tab-diagnostics').getAttribute('aria-selected'),'true','Стрелки переключают нижние вкладки');
    await S.page.keyboard.press('ArrowRight');
    C.eq(await S.page.locator('#panel-tab-assistant').getAttribute('aria-selected'),'true','Помощник доступен с клавиатуры');
    const profile=S.userData;await S.close();await S.launch({userData:profile});
    C.eq(await S.page.locator('#panel-tab-assistant').getAttribute('aria-selected'),'true','Нижняя вкладка восстанавливается после перезапуска');
    C.eq(S.pageErrors.length,0,'Клавиатурные команды без ошибок');
  }
}, {
  id:'studio-toolbar-theme-contrast',name:'Подписи основных действий читаются во всех четырёх темах',async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    for(const theme of ['dark','light','basic','borland']){
      await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
      const colors=await S.page.evaluate(()=>{
        const rgb=s=>(s.match(/[\d.]+/g)||[]).slice(0,3).map(Number);
        const lum=values=>values.map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4;}).reduce((s,v,i)=>s+v*[.2126,.7152,.0722][i],0);
        return [...document.querySelectorAll('.studio-command-label,#project-entry-controls label,#toolbar-overflow > summary')].map(el=>{
          let parent=el,bg;while(parent){bg=getComputedStyle(parent).backgroundColor;if(!['transparent','rgba(0, 0, 0, 0)'].includes(bg))break;parent=parent.parentElement;}
          const front=lum(rgb(getComputedStyle(el).color)),back=lum(rgb(bg));
          return {name:el.textContent||el.tagName,ratio:(Math.max(front,back)+.05)/(Math.min(front,back)+.05)};
        });
      });
      for(const color of colors)C.ok(color.ratio>=4.5,'Контраст подписи '+theme+': '+color.name,color.ratio);
    }
    C.eq(S.pageErrors.length,0,'Проверка тем без ошибок');
  }
}];
