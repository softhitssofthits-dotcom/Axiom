const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'format-menu-triggers',name:'Проверка всегда включена, сохранение и смена фокуса не форматируют документ',async fn({S,C}){
 const file=writeAxm(testWorkspace('format-triggers'),'форматирование.axm','PRINT 1\n');await S.launch({settings:{checkOnChange:false,formatOnSave:true,formatOnBlur:true}});await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 C.eq(await S.page.locator('[data-action="toggle-format-on-blur"], [data-action="toggle-format-on-save"], [data-action="toggle-check-on-change"]').count(),0,'Лишних пунктов нет');
 await S.page.evaluate(()=>{const i=window.__ide;i.settings.formattingStyle='Modern';i.editor.getModel().setValue('print 7\n');});
 await S.page.click('[data-menu="edit"] .menu-label'); await S.page.click('[data-action="format"]');
 await S.page.waitForFunction(()=>window.__ide.editor.getValue().trim()==='Print 7'); C.ok(true,'Ручная команда оставлена и работает');
 C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'PRINT 1\n','Ручная команда не записывает диск');
 await S.page.evaluate(()=>{const i=window.__ide;i.editor.getModel().setValue('print 8\n');i.editor.focus();});
 await S.page.evaluate(()=>document.querySelector('#tb-new').focus()); await S.sleep(850);
 C.eq((await S.page.evaluate(()=>window.__ide.editor.getValue())).trim(),'print 8','Смена фокуса не меняет регистр');
 await S.page.evaluate(()=>window.__ide.save());
 C.eq((await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile))).trim(),'print 8','Сохранение не форматирует даже со старой настройкой');
 await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('PRINT missing\n'));
 await S.page.waitForFunction(()=>window.__ide.monaco.editor.getModelMarkers({owner:'axiom',resource:window.__ide.editor.getModel().uri}).some(m=>m.message.includes('MB2001')));
 C.ok(true,'Проверка при наборе работает даже при старом checkOnChange=false');
}}];
