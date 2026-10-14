module.exports=[{id:'editor-settings-brackets-scroll',name:'Цвета вложенных скобок и видимый без наведения ползунок во всех темах',async fn({S,C}){
 await S.launch({settings:{}});
 for(const theme of ['dark','light','basic','borland']){
  await S.page.evaluate(async theme=>{
   const i=window.__ide;await i.applyEditorPreferences({'bracketPairColorization.enabled':true},theme,'ru');
   i.editor.getModel().setValue('Return Abs(PlayerPixelX() - GhostPixelX()) < 12 And Abs(PlayerPixelY() - GhostPixelY())');i.editor.render(true);
  },theme);
  await S.page.waitForTimeout(300);
  const colors=await S.page.locator('.view-lines').evaluate(el=>Array.from(el.querySelectorAll('[class*="bracket-highlighting"]')).map(n=>getComputedStyle(n).color));
  C.ok(new Set(colors).size>=2,theme+': внутренние и наружные скобки разных цветов',JSON.stringify(colors));
  await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('mazeCols = LEN(maze(1))'));
  for(const enabled of [false,true,false,true,false]){
   await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
   await S.page.click('#settings-tab-structure');
   await S.page.locator('#setting-bracketPairColorization-enabled').setChecked(enabled);
   await S.page.click('[data-settings-action="apply"]');
   await S.page.waitForFunction(()=>!window.__ide.editorSettingsDialog.busy);
   C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.bracketPairColorization).enabled),enabled,theme+': Применить переключает параметр');
   C.ok(await S.page.evaluate(enabled=>monaco.editor.getModels().every(m=>m.getOptions().bracketPairColorizationOptions.enabled===enabled),enabled),theme+': параметр обновлён во всех моделях');
   await S.page.evaluate(()=>window.__ide.editorSettingsDialog.close());
   await S.page.waitForTimeout(300);
   const visible=await S.page.locator('.view-lines').evaluate(el=>Array.from(el.querySelectorAll('span')).filter(n=>/[()]/.test(n.textContent)&&!n.querySelector('span')).map(n=>getComputedStyle(n).color));
   C.eq(new Set(visible).size,enabled?2:1,theme+': LEN(maze(1)) меняет цвета без перезапуска: '+enabled);
  }
  await S.page.evaluate(()=>{window.__ide.newFile();window.__ide.editor.getModel().setValue('mazeCols = LEN(maze(1))');});
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getModel().getOptions().bracketPairColorizationOptions.enabled),false,theme+': новая вкладка использует выключенную раскраску');
  await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
  await S.page.mouse.move(0,0);
  const scroll=await S.page.locator('.settings-content').evaluate(el=>({overflow:el.scrollHeight>el.clientHeight,thumb:getComputedStyle(el,'::-webkit-scrollbar-thumb').backgroundColor,track:getComputedStyle(el,'::-webkit-scrollbar-track').backgroundColor}));
  C.ok(scroll.overflow,theme+': список требует прокрутки');
  C.ok(scroll.thumb!==scroll.track&&scroll.thumb!=='rgba(0, 0, 0, 0)',theme+': ползунок контрастный без наведения',JSON.stringify(scroll));
  await S.page.evaluate(()=>window.__ide.editorSettingsDialog.close());
 }
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
 const profile=S.userData;await S.close();await S.launch({userData:profile});
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getModel().getOptions().bracketPairColorizationOptions.enabled),false,'Выключенная раскраска восстановлена после запуска');
 await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('mazeCols = LEN(maze(1))'));
 await S.page.waitForTimeout(300);
 C.eq(await S.page.locator('.view-lines [class*="bracket-highlighting"]').count(),0,'После запуска выключенная раскраска не рисует цветные скобки');
}}];
