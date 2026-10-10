const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'format-menu-triggers',name:'Ручное форматирование, выход из редактора и сохранение используют выбранный режим',async fn({S,C}){
 const file=writeAxm(testWorkspace('format-triggers'),'форматирование.axm','PRINT 1\n');await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 await S.page.evaluate(()=>{const i=window.__ide;i.settings.formatOnBlur=false;i.settings.checkOnChange=false;i.settings.formattingStyle='Modern';i.editor.getModel().setValue('print 7\n');});
 await S.page.click('[data-menu="edit"]'); await S.page.click('[data-action="format"]');
 await S.page.waitForFunction(()=>window.__ide.editor.getValue().trim()==='Print 7'); C.ok(true,'Команда меню форматирует весь несохранённый текст в Modern');
 C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'PRINT 1\n','Ручное форматирование не записывает диск');
 await S.page.evaluate(()=>{const i=window.__ide;i.settings.formatOnBlur=true;i.editor.getModel().setValue('print 8\n');i.editor.focus();});
 await S.page.evaluate(()=>{const button=document.querySelector('#tb-new');button.focus();});
 await S.page.waitForFunction(()=>window.__ide.editor.getValue().trim()==='Print 8'); C.ok(true,'Уход фокуса форматирует документ');
 await S.page.evaluate(async()=>{const i=window.__ide;i.settings.formatOnBlur=false;i.settings.formatOnSave=true;i.settings.formattingStyle='Classic';i.editor.getModel().setValue('print 9\n');await i.save();});
 C.eq((await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile))).trim(),'PRINT 9','Перед сохранением применяется Classic');
}}];
