const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{
 id:'project-symbol-index-navigation',name:'Индекс символов: точка входа, переменные, типы, поля и параметры',
 async fn({S,C}){
  const folder=testWorkspace('project-symbols');
  const main=writeAxm(folder,'main.axm','IMPORT helper\nDIM record AS Box\nrecord.value = total\nPRINT Echo(record.value)');
  const helperText='PUBLIC DIM total AS INTEGER = 2\nPUBLIC TYPE Box\nvalue AS INTEGER\nEND TYPE\nPUBLIC FUNCTION Echo(value AS INTEGER) AS INTEGER\nRETURN value\nEND FUNCTION';
  const helper=writeAxm(folder,'helper.axm',helperText);
  await S.launch({settings:{smartIdentifierEnabled:false}});
  await S.page.evaluate(async main=>{const i=window.__ide;await i.openFile(main);i.assistant.selectEntry();},main);
  await S.page.evaluate(()=>{
    const i=window.__ide,m=i.editor.getModel();i.editor.setPosition(m.getPositionAt(m.getValue().indexOf('total')+'total'.length));
    i.editor.focus();
  });
  await S.page.keyboard.press('F12');
  await S.page.waitForFunction(helper=>window.__ide.activeFile===helper,helper);
  C.eq(await S.page.evaluate(()=>window.__ide.activeFile),helper,'F12 открывает объявление импортированной переменной');
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),1,'Курсор на объявлении переменной');
  await S.page.evaluate(async()=>{
    const i=window.__ide;i.rebuildOutline();
    await i.assistant.show('incoming',i.outlinePanel.items.find(item=>item.name==='Echo'));
  });
  C.contains(await S.page.locator('#assistant-body').innerText(),'main.axm','Из зависимости виден вызывающий код точки входа');
  await S.page.evaluate(()=>{
    const i=window.__ide,m=i.editor.getModel();i.editor.setPosition(m.getPositionAt(m.getValue().indexOf('total')));i.editor.focus();
  });
  await S.page.keyboard.press('Shift+F12');
  await S.page.waitForFunction(()=>window.__ide.assistant.view.mode==='references'&&document.querySelectorAll('#assistant-body > .assistant-link').length===2);
  C.contains(await S.page.locator('#assistant-body').innerText(),'main.axm','Поиск использований переменной охватывает точку входа');
  C.contains(await S.page.locator('#assistant-body').innerText(),'helper.axm','Список включает объявление');
  for(const [text,line] of [['Box',2],['value',3]]){
    await S.page.evaluate(async({main,text})=>{
      const i=window.__ide;i.switchToFile(main);const m=i.editor.getModel();i.editor.setPosition(m.getPositionAt(m.getValue().indexOf(text)));
      await i.editor.getAction('axiom.navigation.definition').run();
    },{main,text});
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),helper,'Объявление '+text+' находится в зависимости');
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),line,'Точная строка объявления '+text);
  }
  await S.page.evaluate(async()=>{
    const i=window.__ide,m=i.editor.getModel();i.editor.setPosition(m.getPositionAt(m.getValue().lastIndexOf('value')));
    await i.editor.getAction('axiom.navigation.references').run();
  });
  C.eq(await S.page.locator('#assistant-body > .assistant-link').count(),2,'Параметр и его использование не смешиваются с одноимённым полем');
  await S.page.evaluate(async({main,helper})=>{const i=window.__ide;i.switchToFile(main);await i.closeFile(helper,true);},{main,helper});
  await S.electronApp.evaluate(({ipcMain},{helper,helperText})=>{
    ipcMain.removeHandler('read-file-content');
    ipcMain.handle('read-file-content',(_event,file)=>{
      if(file!==helper)throw new Error('Неожиданное чтение '+file);
      return new Promise(resolve=>{global.__releaseSymbolRead=()=>resolve(helperText);});
    });
  },{helper,helperText});
  await S.page.evaluate(()=>{
    const i=window.__ide,m=i.editor.getModel();i.editor.setPosition(m.getPositionAt(m.getValue().indexOf('total')));
    i._symbolHistory=i.assistant.back.length;i._pendingSymbolNavigation=i.assistant.symbols(true);
  });
  await S.electronApp.evaluate(async()=>{
    const deadline=Date.now()+5000;
    while(!global.__releaseSymbolRead&&Date.now()<deadline)await new Promise(resolve=>setTimeout(resolve,10));
    if(!global.__releaseSymbolRead)throw new Error('Навигация не начала чтение зависимости');
  });
  await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:1,column:1}));
  await S.electronApp.evaluate(()=>global.__releaseSymbolRead());
  await S.page.evaluate(()=>window.__ide._pendingSymbolNavigation);
  C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Перемещение каретки во время чтения отменяет поздний F12');
  C.eq(await S.page.evaluate(helper=>window.__ide.openFiles.has(helper),helper),false,'Отменённое чтение не открывает вкладку');
  C.eq(await S.page.evaluate(()=>window.__ide.assistant.back.length===window.__ide._symbolHistory),true,'Отменённый F12 не изменяет историю');
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),1,'Каретка остаётся в выбранной строке');
  await S.page.evaluate(main=>{const i=window.__ide;i.switchToFile(main);i.assistant.selectEntry();},main);
  C.eq(await S.page.evaluate(()=>window.__ide.openFiles.navigationEntryId),null,'Повторный выбор снимает общую точку входа');
  await S.page.evaluate(async main=>{const i=window.__ide;i.assistant.selectEntry();await i.closeFile(main,true);},main);
  C.eq(await S.page.evaluate(()=>window.__ide.openFiles.navigationEntryId),null,'Закрытие выбранного файла снимает точку входа');
  C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
 }
}];
