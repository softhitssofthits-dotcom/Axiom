const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'navigation-keeps-saved-document',name:'Щелчки по строкам не форматируют неизменённый файл и не требуют сохранения',async fn({S,C}){
 const source='DIM PlayerSpeed AS INTEGER = 4\nPlayerSpeed=4\n\nPRINT PlayerSpeed\n\n\n\n\n\n\n\n\n\nPRINT PlayerSpeed\n';
 const file=writeAxm(testWorkspace('navigation-clean'),'Просмотр.axm',source);
 await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));
 await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 await S.page.evaluate(()=>window.__ide.handleAction('smart-identifier'));
 const before=await S.page.evaluate(()=>window.__ide.editor.getValue());
 for(const line of [2,14,3,2,14]){
  const point=await S.page.evaluate(line=>{const e=window.__ide.editor;e.revealLineInCenter(line);const p=e.getScrolledVisiblePosition({lineNumber:line,column:1});const r=e.getDomNode().getBoundingClientRect();return {x:r.x+p.left+2,y:r.y+p.top+p.height/2};},line);
  await S.page.mouse.click(point.x,point.y);await S.page.waitForTimeout(180);
 }
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),before,'Навигация сохранила точный текст');
 C.ok(!await S.page.evaluate(()=>window.__ide.dirty.has(window.__ide.activeFile)),'Файл остаётся сохранённым');
 C.eq((await S.activeTab()).dirty,false,'Звёздочка не появляется');
 const dialogs=S.rendererDialogs.length;await S.page.evaluate(()=>window.__ide.run());await S.waitIdle();
 C.eq(S.rendererDialogs.length,dialogs,'Запуск не просил сохранения');
 await S.page.evaluate(()=>{const i=window.__ide;i.editor.setPosition({lineNumber:2,column:14});i.editor.focus();});
 await S.page.keyboard.type(' ');C.eq((await S.activeTab()).dirty,true,'Настоящая правка отмечена');
 await S.page.keyboard.press('Control+z');await S.page.waitForTimeout(200);
 C.eq((await S.activeTab()).dirty,false,'Отмена к сохранённому тексту снимает звёздочку');
 await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:14,column:1}));await S.page.waitForTimeout(250);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),before,'После отмены навигация также не меняет текст');
 C.eq((await S.activeTab()).dirty,false,'После отмены и навигации звёздочки нет');
 await S.page.evaluate(()=>{const i=window.__ide;i.editor.setPosition({lineNumber:2,column:14});i.editor.focus();});
 await S.page.keyboard.type(' ');
 await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:14,column:1}));
 await S.page.waitForFunction(()=>window.__ide.editor.getModel().getLineContent(2)==='PlayerSpeed = 4');
 C.ok(true,'Переход с действительно изменённой строки сохраняет автоматическое форматирование');
}}];
