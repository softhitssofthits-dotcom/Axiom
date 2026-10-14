module.exports=[{id:'editor-settings-brackets-scroll',name:'Цвета вложенных скобок и видимый без наведения ползунок во всех темах',async fn({S,C}){
 await S.launch({settings:{}});
 for(const theme of ['dark','light','basic']){
  await S.page.evaluate(async theme=>{
   const i=window.__ide;await i.applyEditorPreferences({'bracketPairColorization.enabled':true},theme,'ru');
   i.editor.getModel().setValue('Return Abs(PlayerPixelX() - GhostPixelX()) < 12 And Abs(PlayerPixelY() - GhostPixelY())');i.editor.render(true);
  },theme);
  await S.page.waitForTimeout(300);
  const colors=await S.page.locator('.view-lines').evaluate(el=>Array.from(el.querySelectorAll('[class*="bracket-highlighting"]')).map(n=>getComputedStyle(n).color));
  C.ok(new Set(colors).size>=2,theme+': внутренние и наружные скобки разных цветов',JSON.stringify(colors));
  await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
  await S.page.mouse.move(0,0);
  const scroll=await S.page.locator('.settings-content').evaluate(el=>({overflow:el.scrollHeight>el.clientHeight,thumb:getComputedStyle(el,'::-webkit-scrollbar-thumb').backgroundColor,track:getComputedStyle(el,'::-webkit-scrollbar-track').backgroundColor}));
  C.ok(scroll.overflow,theme+': список требует прокрутки');
  C.ok(scroll.thumb!==scroll.track&&scroll.thumb!=='rgba(0, 0, 0, 0)',theme+': ползунок контрастный без наведения',JSON.stringify(scroll));
  await S.page.evaluate(()=>window.__ide.editorSettingsDialog.close());
 }
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}}];
