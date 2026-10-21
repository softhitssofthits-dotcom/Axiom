async function drag(S,id,dx,dy,release=true){const b=await S.page.locator('#'+id).boundingBox();await S.page.mouse.move(b.x+b.width/2,b.y+b.height/2);await S.page.mouse.down();await S.page.mouse.move(b.x+b.width/2+dx,b.y+b.height/2+dy,{steps:8});await S.sleep(100);if(release)await S.page.mouse.up();}
async function size(S,id,axis){return S.page.locator('#'+id).evaluate((el,axis)=>el.getBoundingClientRect()[axis],axis);}
const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[
{id:'studio-workspace-layout',name:'Согласованная оболочка: боковые вкладки, общий запуск и сохранение состояния',async fn({S,C}){
 const root=testWorkspace('studio-layout');const main=writeAxm(root,'main.axm','PRINT "main"\n'),other=writeAxm(root,'other.axm','SUB Work()\nEND SUB\n');
 await S.launch({settings:{smartIdentifierEnabled:false}});
 C.eq(await S.page.locator('#sidebar-tabs').count(),1,'Боковая область имеет отдельные вкладки');
 if(!await S.page.locator('#sidebar-tabs').count())return;
 await S.page.evaluate(p=>window.__ide.openProject(p),root);
 C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Открытие проекта выбирает его вкладку');
 C.eq(await S.page.locator('#structure-content').isVisible(),false,'Структура не конкурирует за высоту');
 C.eq(await S.page.locator('#toolbar #project-entry').count(),1,'Файл запуска находится в общей панели');
 C.eq(await S.page.locator('#project-entry').inputValue(),main,'Настоящий файл запуска сохранён');
 await S.page.evaluate(p=>window.__ide.openFile(p),other);
 await S.page.evaluate(()=>window.__ide.editor.executeEdits('test',[{range:new window.__ide.monaco.Range(1,1,1,1),text:"' edit\n"}]));
 const source=await S.page.evaluate(()=>window.__ide.editor.getValue());
 await S.page.locator('#sidebar-tab-outline').click();
 C.eq(await S.page.locator('#structure-content').isVisible(),true,'Структура видна по отдельной вкладке');
 C.eq(await S.page.locator('#project-explorer').isVisible(),false,'Файлы проекта скрыты без закрытия');
 C.eq(await S.page.locator('#structure-document-name').textContent(),'other.axm','Структура явно называет активный файл');
 await S.page.locator('#sidebar-close').click();
 C.eq(await S.page.locator('#shell-sidebar').isVisible(),false,'Крестик скрывает область');
 C.eq(await S.page.locator('#sidebar-rail [data-side="outline"]').isVisible(),true,'Подписанная вкладка возврата доступна');
 C.eq(await S.page.locator('#project-entry').isVisible(),true,'Цель запуска остаётся видна');
 await S.page.locator('#sidebar-rail [data-side="outline"]').click();
 C.eq(await S.page.locator('#structure-content').isVisible(),true,'Вкладка возвращает структуру');
 await S.page.locator('#sidebar-close').click();
 await S.page.locator('#sidebar-rail [data-side="outline"]').focus();await S.page.keyboard.press('Enter');
 C.eq(await S.page.evaluate(()=>document.activeElement.id),'sidebar-tab-outline','Возврат с клавиатуры переносит фокус в выбранную вкладку');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),source,'Переключение не меняет текст');
 await S.page.evaluate(()=>window.__ide.editor.trigger('test','undo'));
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'SUB Work()\nEND SUB\n','История Undo сохранена');
 C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Активный модуль не меняет запуск');
 const profile=S.userData;await S.close();await S.launch({userData:profile});
 C.eq(await S.page.locator('#sidebar-tab-outline').getAttribute('aria-selected'),'true','Выбранная боковая вкладка восстановлена');
 C.eq(await S.page.locator('#structure-content').isVisible(),true,'Восстановленная структура видна');
 C.eq(await S.page.locator('#project-entry').inputValue(),main,'Выбор запуска восстановлен');
 await S.page.locator('#sidebar-tab-project').click();
 C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'Имя папки сохранено');
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));
 C.eq(await S.page.locator('#sidebar-tab-project').textContent(),'Project','Боковая вкладка переводится');
 C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'Перевод сохраняет имя папки');
 const menus=await S.page.locator('#menubar > .menu-item > .menu-label').allTextContents();
 C.eq(menus.join('|'),'File|Edit|View|Project|Run|Debug|Tools|Help','Порядок меню согласован с макетом');
 C.eq(await S.page.locator('#studio-debug-tools').isVisible(),false,'Шаговые команды скрыты без сеанса');
 for(const theme of ['light','dark','basic','borland']){
  await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
  C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Проект виден в теме '+theme);
  C.eq(await S.page.locator('#structure-content').isVisible(),false,'Неактивная вкладка скрыта в теме '+theme);
 }
 await S.page.locator('#studio-panel-close').click();await S.page.locator('#studio-panel-return').focus();await S.page.keyboard.press('Enter');
 C.eq(await S.page.evaluate(()=>document.activeElement.dataset.tab),'console','Возврат нижней панели переносит фокус в её вкладку');
 await S.page.evaluate(()=>window.__ide.closeProject());
 C.eq(await S.page.locator('#structure-document-name').textContent(),await S.page.evaluate(()=>I18N.t('studio.noDocument')),'Закрытие последнего файла очищает имя структуры');
 C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
}},
{id:'panels-visibility-persistence',name:'Боковая панель закрывается и восстанавливается через меню, кнопку и профиль',async fn({S,C}){
 await S.launch({settings:{sidebarWidth:300,consoleHeight:220}});
 await S.page.locator('#sidebar-tab-outline').click();C.contains(await S.page.locator('.shell-section-title').innerText(),'Структура файла','Понятный заголовок');C.contains(await S.page.locator('#outline-list').innerText(),'процедур SUB','Понятная пустая структура');
 await S.page.locator('#sidebar-close').click();C.ok(!(await S.page.locator('#shell-sidebar').isVisible()),'Крестик скрывает колонку');C.ok(!(await S.page.locator('#sidebar-resizer').isVisible()),'Граница скрыта вместе с колонкой');
 await S.page.locator('#tb-toggle-sidebar').click();C.ok(await S.page.locator('#shell-sidebar').isVisible(),'Пиктограмма возвращает колонку');
 await S.page.keyboard.press('Control+b');C.ok(!(await S.page.locator('#shell-sidebar').isVisible()),'Ctrl+B скрывает колонку');
 await S.page.locator('[data-menu="view"] .menu-label').click();await S.page.locator('[data-action="toggle-sidebar"]').click();C.ok(await S.page.locator('#shell-sidebar').isVisible(),'Пункт Вид возвращает колонку');
 C.ok(Math.abs(await size(S,'shell-sidebar','width')-300)<2,'Ширина восстановлена');
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));C.contains(await S.page.locator('.shell-section-title').innerText(),'File Outline','Заголовок переводится');C.contains(await S.page.locator('#outline-list').innerText(),'No SUB','Пустой список переводится');
 await S.page.locator('#sidebar-close').click();await S.page.locator('#tb-toggle-output').click();C.ok(!(await S.page.locator('#panel-resizer').isVisible()),'Граница нижней панели скрывается вместе с ней');
 await S.close();await S.launch();C.ok(!(await S.page.locator('#shell-sidebar').isVisible()),'Скрытая колонка остаётся скрытой после перезапуска');C.ok(!(await S.page.locator('#bottom-panel').isVisible()),'Скрытая нижняя панель сохраняется');
 await S.page.locator('#tb-toggle-sidebar').click();await S.page.locator('#tb-toggle-output').click();C.ok(Math.abs(await size(S,'shell-sidebar','width')-300)<2,'Ширина сохраняется после перезапуска');C.ok(Math.abs(await size(S,'bottom-panel','height')-220)<2,'Высота сохраняется после перезапуска');
}},
{id:'panels-drag-cancel-keyboard',name:'Оба разделителя изменяют размер, отменяют жест и поддерживают клавиатуру',async fn({S,C}){
 await S.launch();const width=await size(S,'shell-sidebar','width');const height=await size(S,'bottom-panel','height');
 await drag(S,'sidebar-resizer',90,0);C.ok(Math.abs(await size(S,'shell-sidebar','width')-width-90)<2,'Ширина следует мыши');
 await drag(S,'panel-resizer',0,-70);C.ok(Math.abs(await size(S,'bottom-panel','height')-height-70)<2,'Высота следует мыши');
 const saved=await S.page.evaluate(()=>({w:window.__ide.settings.sidebarWidth,h:window.__ide.settings.consoleHeight}));C.eq(saved.w,Math.round(width+90),'Ширина записана');C.eq(saved.h,Math.round(height+70),'Высота записана');
 await drag(S,'sidebar-resizer',70,0,false);await S.page.keyboard.press('Escape');await S.page.mouse.up();C.eq(Math.round(await size(S,'shell-sidebar','width')),saved.w,'Escape возвращает ширину');C.eq(await S.page.evaluate(()=>window.__ide.settings.sidebarWidth),saved.w,'Отмена не сохраняется');
 await drag(S,'panel-resizer',0,-70,false);await S.page.evaluate(()=>{const i=window.__ide;document.getElementById('panel-resizer').dispatchEvent(new PointerEvent('pointercancel',{pointerId:i.bottomSplitter.drag.id}));});await S.page.mouse.up();C.eq(Math.round(await size(S,'bottom-panel','height')),saved.h,'pointercancel возвращает высоту');
 await drag(S,'sidebar-resizer',30,0,false);await S.page.evaluate(()=>{const i=window.__ide;document.getElementById('sidebar-resizer').releasePointerCapture(i.sidebarSplitter.drag.id);});await S.page.mouse.up();C.eq(Math.round(await size(S,'shell-sidebar','width')),saved.w,'Потеря захвата отменяет жест');
 await S.page.locator('#sidebar-resizer').focus();await S.page.keyboard.press('ArrowRight');C.eq(Math.round(await size(S,'shell-sidebar','width')),saved.w+10,'Стрелка увеличивает ширину');await S.page.keyboard.press('Enter');C.eq(Math.round(await size(S,'shell-sidebar','width')),300,'Enter сбрасывает ширину');
 await S.page.locator('#panel-resizer').dblclick();C.eq(Math.round(await size(S,'bottom-panel','height')),200,'Двойной щелчок сбрасывает высоту');
 await drag(S,'sidebar-resizer',20,0);await drag(S,'sidebar-resizer',20,0);C.eq(Math.round(await size(S,'shell-sidebar','width')),340,'Два быстрых перетягивания не вызывают ложный сброс');
 await drag(S,'sidebar-resizer',10,0,false);
 await S.page.evaluate(()=>{const i=window.__ide;document.getElementById('sidebar-resizer').dispatchEvent(new PointerEvent('lostpointercapture',{pointerId:i.sidebarSplitter.drag.id}));});
 C.ok(await S.page.evaluate(()=>!!window.__ide.sidebarSplitter.drag),'Поздняя потеря захвата прежнего жеста не отменяет новый захват');
 await S.page.mouse.up();C.eq(Math.round(await size(S,'shell-sidebar','width')),350,'Новый жест завершается после позднего события');
 C.eq(await S.page.locator('#sidebar-resizer').getAttribute('aria-orientation'),'vertical','Боковой разделитель доступен');C.eq(await S.page.locator('#panel-resizer').getAttribute('aria-orientation'),'horizontal','Нижний разделитель доступен');await S.screenshot('resizable-panels');
}},
{id:'panels-window-size-bounds',name:'Размеры ограничены окном и возвращаются при его расширении',async fn({S,C}){
 await S.launch({settings:{sidebarWidth:450,consoleHeight:400}});const preferred=await S.page.evaluate(()=>({w:window.__ide.settings.sidebarWidth,h:window.__ide.settings.consoleHeight}));
 await S.electronApp.evaluate(({BrowserWindow})=>{const w=BrowserWindow.getAllWindows()[0];w.setMinimumSize(0,0);w.setSize(520,420);});await S.sleep(300);
 const layout=await S.page.evaluate(()=>({side:document.getElementById('shell-sidebar').getBoundingClientRect().width,panel:document.getElementById('bottom-panel').getBoundingClientRect().height,editor:document.getElementById('editor-container').getBoundingClientRect(),overflow:document.documentElement.scrollWidth>innerWidth}));C.ok(!layout.overflow&&layout.editor.width>=150&&layout.editor.height>=130,'Редактор сохраняет доступное место');C.ok(layout.side<450&&layout.panel<400,'Панели ограничены маленьким окном');
 C.eq(await S.page.evaluate(()=>window.__ide.settings.sidebarWidth),preferred.w,'Сужение окна не затирает выбранную ширину');C.eq(await S.page.evaluate(()=>window.__ide.settings.consoleHeight),preferred.h,'Уменьшение окна не затирает выбранную высоту');
 await S.electronApp.evaluate(({BrowserWindow})=>BrowserWindow.getAllWindows()[0].setSize(1280,900));await S.sleep(300);C.eq(Math.round(await size(S,'shell-sidebar','width')),450,'Ширина возвращается');C.eq(Math.round(await size(S,'bottom-panel','height')),400,'Высота возвращается');
}}];
