const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'documents-tab-selection-scroll-undo',
  name: 'Смена вкладки сохраняет выделение, прокрутку и отдельную историю отмены',
  async fn({S,C}) {
    const dir = testWorkspace('tab-view-state');
    const a = writeAxm(dir,'первый.axm',Array.from({length:220},(_,i) => "' Строка " + i).join('\n'));
    const b = writeAxm(dir,'второй.axm','PRINT 2\n');
    await S.launch({settings:{checkOnChange:false,formatOnBlur:false,smartIdentifierEnabled:false}});
    await S.page.evaluate(p => window.__ide.openFile(p),a);
    const saved = await S.page.evaluate(() => {
      const i = window.__ide, m = i.editor.getModel();
      m.pushStackElement(); i.editor.executeEdits('acceptance',[{range:new window.monaco.Range(1,1,1,1),text:"' Правка\n"}]); m.pushStackElement();
      i.editor.setSelection(new window.monaco.Range(100,2,101,5)); i.editor.setScrollTop(1500);
      return {selection:i.editor.getSelection(),scroll:i.editor.getScrollTop()};
    });
    await S.page.evaluate(p => window.__ide.openFile(p),b);
    await S.page.evaluate(() => { const m = window.__ide.editor.getModel(); m.pushStackElement(); window.__ide.editor.executeEdits('acceptance',[{range:new window.monaco.Range(1,7,1,8),text:'9'}]); m.pushStackElement(); });
    await S.page.evaluate(p => window.__ide.switchToFile(p),a);
    const restored = await S.page.evaluate(() => ({selection:window.__ide.editor.getSelection(),scroll:window.__ide.editor.getScrollTop()}));
    C.eq(JSON.stringify(restored.selection),JSON.stringify(saved.selection),'Выделение сохранено');
    C.eq(restored.scroll,saved.scroll,'Прокрутка сохранена');
    await S.page.keyboard.press('Control+z');
    C.ok(!(await S.editorValue()).startsWith("' Правка"),'Отмена относится к первой вкладке');
    await S.page.evaluate(p => window.__ide.switchToFile(p),b);
    C.eq(await S.editorValue(),'PRINT 9\n','Отмена другой вкладки не изменила вторую');
    await S.page.keyboard.press('Control+z'); C.eq(await S.editorValue(),'PRINT 2\n','Вторая вкладка имеет собственную отмену');
  },
}, {
  id: 'documents-outline-crlf-string-comments',
  name: 'Структура SUB/FUNCTION/TYPE не принимает объявления в строках и комментариях',
  async fn({S,C}) {
    const source = 'PRINT "SUB Fake() REM FUNCTION Phantom()"\r\n' +
      "' TYPE Hidden\r\nREM SUB Ignored()\r\n" +
      'TYPE Item\r\nvalue AS INTEGER\r\nEND TYPE\r\n' +
      'SUB Work()\r\nPRINT "Апостроф: \' и REM внутри строки"\r\nEND SUB\r\n' +
      'FUNCTION Twice(value AS INTEGER) AS INTEGER\r\nRETURN value * 2\r\nEND FUNCTION\r\n';
    await S.launch({settings:{checkOnChange:false,smartIdentifierEnabled:false}});
    await S.page.evaluate(p => window.__ide.openFile(p),writeAxm(testWorkspace('outline-acceptance'),'структура.axm',source));
    C.eq((await S.page.locator('.outline-name').allTextContents()).join(','),'Item,Work,Twice','Только реальные объявления в порядке исходника');
    for (const [name,line] of [['Item',4],['Work',7],['Twice',10]]) {
      await S.page.locator('.outline-item').filter({hasText:name}).click();
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber),line,'Переход к '+name);
    }
  },
}, {
  id: 'documents-find-direction-wrap',
  name: 'Ctrl+F, F3 и Shift+F3 учитывают направление и оборачивание поиска',
  async fn({S,C}) {
    await S.launch({settings:{checkOnChange:false}});
    await S.editorType("' alpha beta alpha\n' alpha\n");
    await S.page.evaluate(() => { window.__ide.editor.setPosition({lineNumber:1,column:1}); window.__ide.editor.focus(); });
    await S.page.keyboard.press('Control+f');
    const input = S.page.getByPlaceholder('Find', { exact: true });
    await input.fill('alpha');
    await S.page.keyboard.press('Escape');
    await S.page.waitForFunction(() => window.__ide.editor.getSelection().startColumn === 3);
    await S.page.keyboard.press('F3');
    await S.page.waitForFunction(() => window.__ide.editor.getSelection().startColumn === 14);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getSelection().startLineNumber),1,'Следующий результат первой строки');
    await S.page.keyboard.press('F3');
    await S.page.waitForFunction(() => window.__ide.editor.getSelection().startLineNumber === 2);
    C.ok(true,'Следующий результат второй строки');
    await S.page.keyboard.press('F3');
    await S.page.waitForFunction(() => window.__ide.editor.getSelection().startLineNumber === 1 && window.__ide.editor.getSelection().startColumn === 3);
    C.ok(true,'Поиск оборачивается к началу');
    await S.page.keyboard.press('Shift+F3');
    await S.page.waitForFunction(() => window.__ide.editor.getSelection().startLineNumber === 2);
    C.ok(true,'Обратный поиск оборачивается к концу');
  },
}, {
  id: 'documents-plain-text-not-formatted',
  name: 'Неподдерживаемый файл остаётся обычным текстом без форматирования',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('plain-text-acceptance'),'заметки.txt','print "Заметка"\n');
    await S.launch({settings:{smartIdentifierEnabled:true}});
    await S.page.evaluate(p => window.__ide.openFile(p),file);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getModel().getLanguageId()),'plaintext','Файл не объявлен программой Axiom');
    await S.page.evaluate(() => window.__ide.formatDocument());
    C.eq(await S.editorValue(),'print "Заметка"\n','Команда форматирования не изменила текст');
    C.eq(require('node:fs').readFileSync(file,'utf8'),'print "Заметка"\n','Файл не перезаписан');
  },
}, {
  id: 'documents-windows-readonly',
  name: 'Атрибут Windows только для чтения запрещает правку и форматирование документа',
  async fn({S,C}) {
    const {execFileSync} = require('node:child_process');
    const file = writeAxm(testWorkspace('readonly-acceptance'),'защищённый.axm','print "Защищённый файл"\n');
    execFileSync('attrib.exe',['+R',file],{windowsHide:true});
    try {
      await S.launch({settings:{smartIdentifierEnabled:true}});
      await S.page.evaluate(p => window.__ide.openFile(p),file);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getOption(window.monaco.editor.EditorOption.readOnly)),true,'Редактор видит атрибут только для чтения');
      await S.page.evaluate(() => window.__ide.formatDocument());
      C.eq(await S.editorValue(),'print "Защищённый файл"\n','Автоматическое и ручное форматирование не меняют защищённый текст');
      C.eq(require('node:fs').readFileSync(file,'utf8'),'print "Защищённый файл"\n','Диск не изменён');
    } finally { execFileSync('attrib.exe',['-R',file],{windowsHide:true}); }
  },
}, {
  id: 'documents-save-as-cancel-and-empty-close',
  name: 'Отмена Сохранить как сохраняет путь и правку; пустая новая вкладка закрывается без вопроса',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('save-as-cancel-acceptance'),'исходный.axm','PRINT 1\n');
    await S.launch({settings:{checkOnChange:false,smartIdentifierEnabled:false}});
    await S.page.evaluate(p => window.__ide.openFile(p),file);
    await S.editorType('PRINT 2\n'); await S.stubSaveDialog(null);
    C.eq(await S.page.evaluate(() => window.__ide.saveAs()),false,'Отмена возвращает отказ сохранения');
    C.eq(await S.page.evaluate(() => window.__ide.activeFile),file,'Путь не изменён');
    C.eq(await S.editorValue(),'PRINT 2\n','Правка сохранена в модели');
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p),file),'Отмена не сбрасывает признак изменения');
    C.eq(require('node:fs').readFileSync(file,'utf8'),'PRINT 1\n','Файл не перезаписан');
    await S.electronApp.evaluate(({dialog}) => { global.emptyCloseQuestions = 0; dialog.showMessageBox = async () => { global.emptyCloseQuestions++; return {response:2}; }; });
    await S.page.evaluate(async () => { window.__ide.newFile(); await window.__ide.closeFile(window.__ide.activeFile); });
    C.eq(await S.electronApp.evaluate(() => global.emptyCloseQuestions),0,'Пустая новая вкладка не задаёт вопрос');
  },
}, {
  id: 'documents-save-as-error-keeps-path-and-text',
  name: 'Ошибка записи Сохранить как сохраняет прежний путь и несохранённый текст',
  async fn({S,C}) {
    const target=writeAxm(testWorkspace('save-as-error-acceptance'),'цель.axm','PRINT 0\n');
    await S.launch({settings:{checkOnChange:false,smartIdentifierEnabled:false}});
    await S.stubSaveDialog(target);
    await S.electronApp.evaluate(({ipcMain}) => { ipcMain.removeHandler('write-file'); ipcMain.handle('write-file',async()=>false); });
    await S.page.evaluate(() => { window.__lastSaveError=null; window.alert=message=>{window.__lastSaveError=String(message);}; });
    const untitled=await S.page.evaluate(() => { const i=window.__ide; i.newFile(); i.editor.getModel().setValue('PRINT "Несохранённый текст"\n'); return i.activeFile; });
    C.eq(await S.page.evaluate(() => window.__ide.saveAs()),false,'Ошибка записи возвращает отказ');
    C.eq(await S.page.evaluate(() => window.__ide.activeFile),untitled,'Ошибка не переименовывает вкладку');
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p),untitled),'Несохранённый текст остаётся изменённым');
    C.contains(await S.page.evaluate(() => window.__lastSaveError),'Не удалось сохранить файл','Показано объяснение ошибки записи');
    C.eq(require('node:fs').readFileSync(target,'utf8'),'PRINT 0\n','Целевой файл не изменён');
  },
}, {
  id: 'documents-inactive-model-dirty-indicator-sync',
  name: 'Изменение модели неактивной вкладки сразу показывает признак несохранённого файла',
  async fn({S,C}) {
    const first=writeAxm(testWorkspace('inactive-dirty-indicator'),'88.axm','PRINT 1\n');
    const second=writeAxm(testWorkspace('inactive-dirty-indicator-second'),'screen_mouse.axm','PRINT 2\n');
    await S.launch({settings:{checkOnChange:false,smartIdentifierEnabled:false}});
    await S.page.evaluate(async paths => { await window.__ide.openFile(paths[0]); await window.__ide.openFile(paths[1]); window.__ide.openFiles.get(paths[0]).model.setValue('PRINT 11\n'); },[first,second]);
    C.eq(await S.page.evaluate(p => window.__ide.dirty.has(p),first),true,'Изменённая фоновая модель отмечена как несохранённая');
    C.eq(await S.page.evaluate(p=>{const tab=[...document.querySelectorAll('.editor-tab')].find(item=>item.dataset.path===p);return tab.querySelector('.dirty-indicator').style.display;},first),'inline','На неактивной вкладке показана звёздочка');
    await S.electronApp.evaluate(({dialog}) => {
      global.__closePrompt = null;
      dialog.showMessageBox = async (_owner, options) => { global.__closePrompt = options.message; return {response:2}; };
    });
    C.eq(await S.page.evaluate(p => window.__ide.closeFile(p),first),false,'Отмена запроса оставляет документ открытым');
    C.ok(await S.electronApp.evaluate(() => global.__closePrompt.includes('88.axm')),'Запрос касается изменённого документа 88.axm');
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p) && window.__ide.openFiles.isDirty(p),first),'Отменённое закрытие сохраняет несохранённую правку');
  },
}, {
  id: 'documents-save-dialog-owned-by-window',
  name: 'Системное окно Сохранить как привязано к окну IDE',
  async fn({S,C}) {
    await S.launch({settings:{checkOnChange:false}});
    await S.electronApp.evaluate(({dialog,BrowserWindow}) => {
      global.__saveDialogHasOwner = false;
      dialog.showSaveDialog = async (...args) => {
        global.__saveDialogHasOwner = args[0] === BrowserWindow.getAllWindows()[0] && !!args[1];
        return {canceled:true};
      };
    });
    C.eq(await S.page.evaluate(async () => { window.__ide.newFile(); return await window.__ide.saveAs(); }),false,'Отмена системного окна сохраняет поведение отмены');
    C.eq(await S.electronApp.evaluate(() => global.__saveDialogHasOwner),true,'Системный диалог получил главное окно как владельца');
  },
}, {
  id: 'documents-find-replace-options-undo',
  name: 'Регистр, целые слова, регулярные выражения и отмена пакетной замены',
  async fn({S,C}) {
    const source = "' Alpha alpha alphabet ALPHA\n";
    await S.launch({settings:{checkOnChange:false}});
    await S.editorType(source);
    await S.page.keyboard.press('Control+h');
    const find = S.page.getByPlaceholder('Find',{exact:true});
    const replace = S.page.getByPlaceholder('Replace',{exact:true});
    await find.fill('alpha');
    await S.page.getByRole('checkbox',{name:/Match Whole Word/}).check();
    await S.page.getByRole('checkbox',{name:/Match Case/}).check();
    await replace.fill('word');
    await S.page.getByRole('button',{name:/Replace All/}).click();
    C.eq(await S.editorValue(),"' Alpha word alphabet ALPHA\r\n",'Совпали только строчная форма и отдельное слово');
    await S.page.evaluate(() => window.__ide.editor.focus());
    await S.page.keyboard.press('Control+z');
    C.eq(await S.editorValue(),source.replace(/\n/g,'\r\n'),'Пакетная замена отменяется одной командой');
    await S.page.getByRole('checkbox',{name:/Match Whole Word/}).uncheck();
    await S.page.getByRole('checkbox',{name:/Match Case/}).uncheck();
    await S.page.getByRole('checkbox',{name:/Use Regular Expression/}).check();
    await find.fill('[');
    await replace.fill('нельзя');
    const invalidButton=S.page.getByRole('button',{name:/Replace All/});
    C.eq(await invalidButton.getAttribute('aria-disabled'),'false','Кнопка остаётся доступна, чтобы проверка подтверждала отсутствие изменения текста');
    await invalidButton.click();
    C.eq(await S.editorValue(),source.replace(/\n/g,'\r\n'),'Некорректный шаблон не меняет документ');
    await find.fill('alph(a|A)');
    await replace.fill('word');
    await invalidButton.click();
    C.eq(await S.editorValue(),"' word word wordbet word\r\n",'Регулярное выражение заменяет все совпадения без учёта регистра');
  },
}];
