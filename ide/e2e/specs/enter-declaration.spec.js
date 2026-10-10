module.exports=[{id:'enter-declaration-with-suggestions',name:'Один Enter после набранного DIM с открытым дополнением',async fn({S,C}){
 await S.launch();
 for(const style of ['Classic','Modern']){
  await S.page.evaluate(async style=>{const i=window.__ide;await i.setFormattingStyle(style);i.editor.getModel().setValue('');i.editor.setPosition({lineNumber:1,column:1});i.editor.focus();},style);
  await S.page.keyboard.type('dim a as integer',{delay:35});
  await S.page.evaluate(()=>window.__ide.editor.trigger('test','editor.action.triggerSuggest',{}));
  await S.page.locator('.suggest-widget.visible').waitFor();
  await S.page.keyboard.press('Enter');await S.page.waitForTimeout(450);
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),2,'Первый Enter перевёл каретку: '+style);
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getModel().getLineContent(1)),style==='Classic'?'DIM a AS INTEGER':'Dim a As Integer','Объявление отформатировано тем же Enter');
  await S.page.keyboard.press('Escape');
  await S.page.keyboard.type('a = 67');await S.page.keyboard.press('Enter');await S.page.waitForTimeout(350);
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),3,'Вторая строка также переходит с первого Enter');
 }
 await S.page.evaluate(()=>{const i=window.__ide;i.editor.getModel().setValue('');i.editor.setPosition({lineNumber:1,column:1});i.editor.focus();});
 await S.page.keyboard.type('dim b as integ',{delay:35});await S.page.evaluate(()=>window.__ide.editor.trigger('test','editor.action.triggerSuggest',{}));
 await S.page.locator('.suggest-widget.visible').waitFor();await S.page.keyboard.press('Tab');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),1,'Tab принимает дополнение без новой строки');
 C.ok(await S.page.evaluate(()=>/INTEGER$/i.test(window.__ide.editor.getValue())),'Дополнение типа принято через Tab');
 await S.page.keyboard.press('Enter');await S.page.waitForTimeout(350);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),2,'Enter после Tab переводит строку');
}}];
