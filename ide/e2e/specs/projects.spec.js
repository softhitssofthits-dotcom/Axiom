async function createProject(S,name){
 await S.page.evaluate(()=>{window.__createProjectDone=false;window.__ide.handleAction('create-project').finally(()=>window.__createProjectDone=true);});
 await S.page.locator('#project-create-dialog').waitFor();
 await S.page.locator('#project-create-dialog button').filter({hasText:/Выбрать папку|Choose folder/}).click();
 if(name)await S.page.locator('#project-create-name').fill(name);
 await S.page.locator('#project-create-dialog button[type=submit]').click();
 await S.page.waitForFunction(()=>window.__createProjectDone);
}
const {testWorkspace,writeAxm,ROOT}=require('../helpers');
const fs=require('node:fs'),path=require('node:path');
function fixture(tag){
 const root=testWorkspace('axproj-'+tag);
 const main=writeAxm(root,'main.axm','IMPORT helper\nPRINT value\n');
 const helper=writeAxm(root,'helper.axm','PUBLIC DIM value AS INTEGER=7\n');
 const spare=writeAxm(root,'spare.axm','PRINT missing\n');
 fs.mkdirSync(path.join(root,'art'),{recursive:true});fs.writeFileSync(path.join(root,'art','pixel.txt'),'asset');
 const project=path.join(root,'game.axproj');fs.writeFileSync(project,JSON.stringify({formatVersion:1,name:'Проверочный проект',entry:'main.axm',sources:['*.axm'],assets:['art/**'],exclude:[]}));
 return {root,main,helper,spare,project};
}
module.exports=[{id:'axproj-open-run-check',name:'axproj: дерево, одна вкладка, несохранённый модуль, полный анализ и точка запуска',async fn({S,C}){
 const f=fixture('run');await S.launch();C.eq(await S.page.evaluate(p=>window.__ide.openProject(p),f.project),true,'Проект открыт');
 C.eq(await S.page.locator('#project-title').textContent(),'Проверочный проект','Постоянное имя проекта');
 C.eq(await S.page.locator('.editor-tab').count(),1,'Открыта только точка входа');
 C.eq(await S.page.locator('#project-entry-controls').isVisible(),false,'Постоянный список запуска заменён свойством проекта');
 C.contains(await S.page.locator('#project-files').innerText(),'art','Дерево содержит папку ресурсов');
 C.contains(await S.page.locator('#project-files').innerText(),'pixel.txt','Ресурс виден в дереве');
 await S.page.evaluate(p=>window.__ide.openFile(p),f.helper);
 await S.page.evaluate(()=>{const i=window.__ide;i.settings.formatOnBlur=false;i.editor.setValue('PUBLIC DIM value AS INTEGER=8\n');});
 await S.page.evaluate(()=>window.__ide.run());await S.waitIdle();await S.waitConsole('8');C.ok(true,'Выполнение использует несохранённую зависимость');
 await S.page.evaluate(()=>window.__ide.check(true));C.contains(await S.page.locator('#diagnostics-content').innerText(),'missing','Проверка находит ошибку недостижимого исходника');
 const external=writeAxm(testWorkspace('axproj-outside'),'outside.axm','PRINT 999\n');await S.page.evaluate(p=>window.__ide.openFile(p),external);
 C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),f.main,'Внешняя вкладка не подменяет запуск проекта');
 for(const theme of ['dark','light','basic','borland']){await S.page.evaluate(t=>window.__ide.setTheme(t),theme);await S.page.screenshot({path:path.join(ROOT,'.artifacts/axproj/tree-'+theme+'.png')});}
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));C.eq(await S.page.locator('#project-title').textContent(),'Проверочный проект','Перевод не меняет имя');
 C.eq(S.pageErrors.length,0,'Нет ошибок JavaScript');
}}, {id:'axproj-properties-conflict-entry',name:'axproj: свойства, запись entry, конфликт внешней правки и исключённый IMPORT',async fn({S,C}){
 const f=fixture('properties');await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);
 await S.page.evaluate(p=>window.__ide.projectSession.select(p),f.helper);C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).entry,'helper.axm','Точка входа записана в проект');
 await S.page.evaluate(()=>window.__ide.handleAction('project-properties'));await S.page.screenshot({path:path.join(ROOT,'.artifacts/axproj/properties.png')});await S.page.locator('#project-property-entry').fill('main.axm');await S.page.locator('#project-properties-dialog button[type=submit]').click();
 await S.page.waitForFunction(()=>!document.getElementById('project-properties-dialog')||document.querySelector('#project-properties-dialog .project-entry-error')?.textContent);
 if(await S.page.locator('#project-properties-dialog').count())throw new Error(await S.page.locator('#project-properties-dialog .project-entry-error').innerText());
 C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).entry,'main.axm','Свойства меняют entry');
 await S.page.evaluate(()=>window.__ide.handleAction('project-properties'));const changed=JSON.parse(fs.readFileSync(f.project,'utf8'));changed.name='Внешняя правка';fs.writeFileSync(f.project,JSON.stringify(changed));
 await S.page.locator('#project-property-name').fill('Потерянная правка');await S.page.locator('#project-properties-dialog button[type=submit]').click();
 await S.page.waitForFunction(()=>document.querySelector('#project-properties-dialog .project-entry-error').textContent.length>0);
 C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).name,'Внешняя правка','Внешняя правка не потеряна');
 await S.page.keyboard.press('Escape');await S.page.evaluate(()=>window.__ide.refreshProjectFiles());
 const result=await S.page.evaluate(async p=>{const project=window.__ide.projectSession.descriptor;const data=JSON.parse(project.text);data.exclude=['helper.axm'];return window.axiom.projectCommand('project.save',{path:p,text:JSON.stringify(data),expectedHash:project.contentHash});},f.project);
 C.ok(result.ok,'Исключение записано');
 const check=await S.page.evaluate(p=>window.axiom.projectCommand('project.check',{path:p}),f.project);C.eq(check.ok,false,'IMPORT исключённого файла отвергнут');C.contains(check.error.message,'исключённому','Ошибка сообщает причину');
 const hover=await S.page.evaluate(p=>window.axiom.documentCommand('document.hover',{documentId:'hover',documentRevision:0,sourceText:'IMPORT helper\nPRINT value',sourceFilePath:p.main,projectFilePath:p.project}),f);C.eq(hover.ok,false,'Подсказки также не обходят исключённый IMPORT');
 C.eq(S.pageErrors.length,0,'Нет ошибок JavaScript');
}}, {id:'axproj-restore-relocate-create',name:'axproj: перенос папки, восстановление, создание и ремонт удалённой entry',async fn({S,C}){
 const f=fixture('restore');await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);await S.page.evaluate(()=>window.__ide.setProjectExplorerVisible(false));await S.close();await S.launch();
 C.eq(await S.page.locator('#project-explorer').isVisible(),false,'Восстановление проекта сохраняет скрытую область');
 C.eq(await S.page.locator('#project-resizer').isVisible(),false,'У скрытой восстановленной области нет разделителя');
 C.eq(await S.page.evaluate(()=>window.__ide.projectSession.descriptor.projectFilePath),f.project,'Восстанавливается описание проекта');
 const target=testWorkspace('axproj-relocated');fs.cpSync(f.root,target,{recursive:true});const moved=path.join(target,'game.axproj');await S.page.evaluate(p=>window.__ide.openProject(p),moved);
 C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),path.join(target,'main.axm'),'Относительная entry работает после переноса');
 const renamed=path.join(target,'start.axm');fs.renameSync(path.join(target,'main.axm'),renamed);await S.page.evaluate(p=>window.__ide.renameActive(p.target,p.old),{target:renamed,old:path.join(target,'main.axm')});
 C.eq(JSON.parse(fs.readFileSync(moved,'utf8')).entry,'start.axm','Переименование согласует entry');
 fs.unlinkSync(renamed);await S.page.evaluate(()=>window.__ide.refreshProjectFiles());C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),null,'Удалённая entry не подменяется другим файлом');
 C.contains(await S.page.locator('#project-files').innerText(),'отсутствует','Дерево объясняет неготовность');
 const plain=testWorkspace('axproj-create');writeAxm(plain,'main.axm','PRINT 1\n');await S.page.evaluate(p=>window.__ide.openProject(p),plain);await S.stubOpenDialog([plain]);await createProject(S);
 C.ok(fs.existsSync(path.join(plain,path.basename(plain)+'.axproj')),'Явная команда создаёт описание');
 C.ok(await S.page.evaluate(()=>!!window.__ide.projectSession.descriptor),'Созданный проект открыт');C.eq(S.pageErrors.length,0,'Нет ошибок JavaScript');
}}];

module.exports.push({id:'axproj-paths-resources-saveas',name:'axproj: корень ресурсов, пути, ссылки и сохранение за границы проекта',async fn({S,C}){
 const f=fixture('paths');fs.mkdirSync(path.join(f.root,'nested'),{recursive:true});const nested=writeAxm(path.join(f.root,'nested'),'start.axm','PRINT CURRENTDIR()\nPRINT FILEEXISTS("art/pixel.txt")\n');
 const data=JSON.parse(fs.readFileSync(f.project,'utf8'));data.entry='nested/start.axm';data.sources=['**/*.axm'];fs.writeFileSync(f.project,JSON.stringify(data));
 await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);await S.page.evaluate(()=>window.__ide.run());await S.waitIdle();await S.waitConsole('TRUE');
 const output=(await S.consoleText()).replace(/\\/g,'/').toLowerCase();C.contains(output,f.root.replace(/\\/g,'/').toLowerCase(),'CURRENTDIR и ресурс используют корень проекта');
 const outside=path.join(testWorkspace('axproj-copy'),'copy.axm');await S.stubSaveDialog(outside);C.eq(await S.page.evaluate(p=>window.__ide.save(p,true),nested),true,'Save As за пределы проекта сохраняет документ');
 C.eq(await S.page.evaluate(()=>window.__ide.activeFile),outside,'Активная вкладка использует новый путь');C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).entry,'nested/start.axm','Сохранение копии не меняет entry проекта');
 await S.page.evaluate(()=>window.__ide.handleAction('run-current-file'));await S.waitIdle();C.eq(await S.page.evaluate(()=>window.__ide.execution.sourceSnapshot.file),outside,'Отдельная команда запускает внешнюю вкладку');
 await S.page.evaluate(p=>window.__ide.openFile(p),nested);
 const externalChange=JSON.parse(fs.readFileSync(f.project,'utf8'));externalChange.name='Внешнее изменение';fs.writeFileSync(f.project,JSON.stringify(externalChange));
 const copy=path.join(f.root,'saved-copy.axm');await S.stubSaveDialog(copy);C.eq(await S.page.evaluate(p=>window.__ide.save(p,true),nested),true,'Исходник сохраняется даже при конфликте обновления описания');
 C.eq(await S.page.evaluate(()=>window.__ide.activeFile),copy,'Отказ обновления описания не рассогласует вкладку');
 C.eq(await S.page.evaluate(p=>window.__ide.openFiles.has(p),copy),true,'Сохранённый документ доступен по новому пути');
 C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).name,'Внешнее изменение','Конфликт Save As не теряет внешнюю правку');
 const bad=path.join(f.root,'bad.axproj');
 for(const content of ['{',JSON.stringify({formatVersion:2}),JSON.stringify({formatVersion:1,entry:'../outside.axm'})]){
  fs.writeFileSync(bad,content);const result=await S.page.evaluate(p=>window.axiom.projectCommand('project.open',{path:p}),bad);C.eq(result.ok,false,'Некорректное описание отвергнуто');
 }
 const junction=path.join(f.root,'linked');const target=testWorkspace('axproj-link-target');writeAxm(target,'external.axm','PRINT 1');
 require('node:child_process').execFileSync('cmd.exe',['/d','/c','mklink','/J',junction,target],{windowsHide:true});
 try{fs.writeFileSync(bad,JSON.stringify({formatVersion:1,entry:'linked/external.axm'}));const result=await S.page.evaluate(p=>window.axiom.projectCommand('project.open',{path:p}),bad);C.eq(result.ok,false,'Точка входа через Windows Junction отвергнута');}finally{fs.rmdirSync(junction);}
 C.eq(S.pageErrors.length,0,'Нет ошибок JavaScript');
}});

module.exports.push({id:'axproj-dialog-remembers-directory',name:'Диалог axproj сохраняет последнюю папку при повторе, отмене и перезапуске',async fn({S,C}){
 const f=fixture('dialog'),other=testWorkspace('axproj-dialog-other');
 await S.launch({settings:{lastProjectDir:other}});
 const mock=async selected=>S.electronApp.evaluate(({dialog},selected)=>{dialog.showOpenDialog=async(_owner,options)=>{global.__projectDialogDefault=options.defaultPath;return {canceled:!selected,filePaths:selected?[selected]:[]};};},selected);
 const currentDefault=()=>S.electronApp.evaluate(()=>global.__projectDialogDefault);
 await mock(f.project);C.eq(await S.page.evaluate(()=>window.axiom.openProjectFileDialog()),f.project,'Диалог возвращает выбранный файл');
 C.eq(await currentDefault(),other,'Первое открытие использует известную папку проекта');
 await mock(null);C.eq(await S.page.evaluate(()=>window.axiom.openProjectFileDialog()),null,'Отмена возвращает null');C.eq(await currentDefault(),f.root,'Следующее открытие использует последнюю выбранную папку');
 await S.close();await S.launch();await mock(null);await S.page.evaluate(()=>window.axiom.openProjectFileDialog());C.eq(await currentDefault(),f.root,'Папка сохраняется после перезапуска и отмены');
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});

module.exports.push({id:'axproj-create-from-draft-after-close',name:'Закрытие прежнего проекта и создание нового из черновика: два INPUT и сумма',async fn({S,C}){
 const old=fixture('old-create'),folder=testWorkspace('axproj-sum-new');
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),old.project);
 await S.page.evaluate(async()=>{for(const file of [...window.__ide.openFiles.keys()])await window.__ide.closeFile(file);});
 await S.page.locator('#project-explorer-close').click();await S.page.waitForFunction(()=>window.__ide.projectDir===null);
 C.eq(await S.page.evaluate(()=>window.__ide.projectSession.descriptor),null,'Крестик закрывает сеанс проекта');
 C.eq(await S.page.evaluate(()=>window.__ide.settings.lastProjectFile),null,'Старый проект не остаётся для восстановления');
 const helper=path.join(folder,'arithmetic.axm'),main=path.join(folder,'main.axm');
 const code='PUBLIC FUNCTION AddNumbers(a AS INTEGER, b AS INTEGER) AS INTEGER\n RETURN a+b\nEND FUNCTION\n';
 await S.page.evaluate(code=>{const i=window.__ide;i.newFile();i.editor.setValue(code);i.settings.formatOnBlur=false;},code);
 await S.stubOpenDialog([folder]);await S.stubSaveDialog(helper);await createProject(S);
 const project=path.join(folder,path.basename(folder)+'.axproj');C.ok(fs.existsSync(project),'Создан проект в явно выбранной новой папке');
 C.eq(fs.readFileSync(helper,'utf8').replace(/\r\n/g,'\n'),code,'Черновик сохранён без потери текста');
 C.eq(await S.page.evaluate(()=>window.__ide.projectDir),folder,'Сеанс относится к новой папке');
 C.eq(fs.readdirSync(old.root).filter(f=>f.endsWith('.axproj')).length,1,'В прежней папке не появился лишний проект');
 await S.stubSaveDialog(main);await S.page.evaluate(()=>window.__ide.newProjectFile());
 const program='IMPORT arithmetic\nDIM first AS INTEGER, second AS INTEGER\nINPUT "Первое число"; first\nINPUT "Второе число"; second\nPRINT "Сумма двух чисел равна "; AddNumbers(first,second)\n';
 await S.page.evaluate(async({program,main})=>{const i=window.__ide;i.editor.setValue(program);await i.save(main);await i.projectSession.select(main);await i.run();},{program,main});
 await S.page.waitForFunction(()=>!!window.__ide.execution.inputRequestId);const request=await S.page.evaluate(()=>window.__ide.execution.inputRequestId);await S.typeConsole('8');
 await S.page.waitForFunction(previous=>!!window.__ide.execution.inputRequestId&&window.__ide.execution.inputRequestId!==previous,request);await S.typeConsole('13');await S.waitIdle();await S.waitConsole('Сумма двух чисел равна 21');
 C.ok(true,'Проект вычислил сумму 8 и 13 через два INPUT');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-create-empty-existing-cancel',name:'Создание проекта до кода, существующее описание и отмена закрытия с правками',async fn({S,C}){
 const folder=testWorkspace('axproj-empty-create');await S.launch();await S.stubOpenDialog([folder]);await createProject(S);
 const project=path.join(folder,path.basename(folder)+'.axproj'),main=path.join(folder,'main.axm');
 C.ok(fs.existsSync(project)&&fs.existsSync(main),'В пустой папке создаются описание и main.axm');C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Пустой главный файл открыт для набора');
 await createProject(S);C.eq(fs.readdirSync(folder).filter(f=>f.endsWith('.axproj')).length,1,'Повтор открывает существующий проект без дубликата');
 await S.page.evaluate(()=>window.__ide.editor.setValue('PRINT 42'));
 await S.electronApp.evaluate(({dialog})=>{dialog.showMessageBox=async()=>({response:2});});await S.page.locator('#project-explorer-close').click();
 C.eq(await S.page.evaluate(()=>window.__ide.projectDir),folder,'Отмена закрытия сохраняет сеанс');C.eq(await S.editorValue(),'PRINT 42','Отмена сохраняет несохранённый текст');
 await S.electronApp.evaluate(({dialog})=>{dialog.showMessageBox=async()=>({response:1});});await S.page.locator('#project-explorer-close').click();await S.page.waitForFunction(()=>window.__ide.projectDir===null);
 C.eq(await S.page.locator('#project-resizer').isVisible(),false,'Закрытие убирает ненужный регулятор');await S.close();await S.launch();C.eq(await S.page.evaluate(()=>window.__ide.projectDir),null,'Закрытый проект не восстанавливается');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-create-cancel-restoration',name:'Отмена создания не восстанавливает выбранную папку как проект',async fn({S,C}){
 const folder=testWorkspace('axproj-cancel-create');await S.launch();
 await S.page.evaluate(()=>{window.__ide.newFile();window.__ide.editor.setValue('PRINT 123');});
 await S.stubOpenDialog([folder]);await S.stubSaveDialog(null);await createProject(S);
 C.eq(fs.readdirSync(folder).filter(f=>f.endsWith('.axproj')).length,0,'Отмена сохранения не создаёт описание');
 C.eq(await S.page.evaluate(()=>window.__ide.projectDir),null,'Отмена не открывает проект');
 C.eq(await S.editorValue(),'PRINT 123','Черновик сохранён в редакторе');
 C.ok(!await S.page.evaluate(()=>window.__ide.settings.lastProjectDir),'Выбор папки не становится сеансом');
 await S.close();await S.launch();C.eq(await S.page.evaluate(()=>window.__ide.projectDir),null,'После перезапуска отменённый проект не появляется');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-create-dialog-name-cancel',name:'Меню Создать проект: имя, папка, отмена и отсутствие неожиданных диалогов',async fn({S,C}){
 const folder=testWorkspace('axproj-named-dialog');await S.launch();await S.page.evaluate(()=>{void window.__ide.handleAction('create-project');});
 C.eq(await S.page.locator('#project-create-dialog').isVisible(),true,'Сначала открывается окно создания проекта');
 C.eq(await S.page.locator('[data-action="create-project"] .menu-text').textContent(),'Создать проект…','Меню ясно называет действие');
 await S.page.locator('#project-create-name').fill('Моя сумма');await S.page.locator('#project-create-dialog button[type=button]').last().click();
 C.eq(fs.readdirSync(folder).length,0,'Отмена окна ничего не создаёт');
 await S.stubOpenDialog([folder]);await createProject(S,'Моя сумма');
 const file=path.join(folder,'Моя сумма.axproj');C.ok(fs.existsSync(file),'Описание получает введённое имя');C.eq(JSON.parse(fs.readFileSync(file,'utf8')).name,'Моя сумма','Имя сохранено в проекте');
 C.eq(await S.page.evaluate(()=>window.__ide.projectSession.descriptor.name),'Моя сумма','Созданный проект открыт с заданным именем');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-save-add-second-source',name:'Второй исходник появляется после сохранения; существующий добавляется без перезаписи',async fn({S,C}){
 const f=fixture('second-source');await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);
 const second=path.join(f.root,'second.axm');await S.page.evaluate(()=>{window.__ide.newFile();window.__ide.editor.setValue('PRINT 22');});await S.stubSaveDialog(second);await S.page.evaluate(()=>window.__ide.save());
 C.ok(await S.page.locator('.project-file').evaluateAll((items,p)=>items.some(i=>i.dataset.path===p),second),'Сохранённый новый исходник сразу в дереве');
 const third=writeAxm(f.root,'third.axm','PRINT 33');await S.stubOpenDialog([third]);
 await S.electronApp.evaluate(({dialog})=>{dialog.showSaveDialog=async()=>{throw new Error('Неожиданный диалог сохранения');};});
 await S.page.locator('#project-new-file').click();await S.page.waitForFunction(p=>window.__ide.activeFile===p,third);
 C.eq(fs.readFileSync(third,'utf8'),'PRINT 33','Добавление существующего не меняет содержимое');
 C.ok(await S.page.locator('.project-file').evaluateAll((items,p)=>items.some(i=>i.dataset.path===p),third),'Существующий исходник появился в дереве');
 const external=writeAxm(testWorkspace('axproj-external-add'),'outside.axm','PRINT 44');await S.stubOpenDialog([external]);await S.page.evaluate(()=>window.__ide.addExistingProjectFile());
 C.eq(fs.readFileSync(path.join(f.root,'outside.axm'),'utf8'),'PRINT 44','Внешний исходник скопирован в проект');
 C.eq(fs.readFileSync(external,'utf8'),'PRINT 44','Внешний оригинал сохранён');
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-rename-context-file',name:'Контекстное переименование файла сохраняет правки, вкладку и точку входа',async fn({S,C}){
 const f=fixture('rename-context');await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);
 await S.page.evaluate(()=>window.__ide.editor.setValue('PRINT 123'));
 await S.page.locator('.project-file').filter({hasText:'main.axm'}).click({button:'right'});
 await S.page.locator('#ax-project-context-menu [data-action="rename"]').click();
 await S.page.locator('#project-rename-dialog input').fill('renamed.axm');await S.page.locator('#project-rename-dialog button[type=submit]').click();
 const renamed=path.join(f.root,'renamed.axm');await S.page.waitForFunction(p=>window.__ide.activeFile===p,renamed);
 await S.page.waitForFunction(p=>window.__ide.projectSession.entry===p,renamed);
 C.ok(fs.existsSync(renamed)&&!fs.existsSync(f.main),'Файл переименован на диске');C.eq(await S.editorValue(),'PRINT 123','Несохранённый текст сохранён во вкладке');
 C.eq(JSON.parse(fs.readFileSync(f.project,'utf8')).entry,'renamed.axm','Точка входа обновлена в описании');
 C.ok(await S.page.locator('.project-file').evaluateAll((items,p)=>items.some(i=>i.dataset.path===p),renamed),'Дерево обновлено');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
module.exports.push({id:'axproj-reject-numeric-source',name:'Числовое имя нового исходника отклоняется без потери черновика',async fn({S,C}){
 const f=fixture('numeric-name');await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),f.project);
 const target=path.join(f.root,'222.axm');await S.page.evaluate(()=>{window.__ide.newFile();window.__ide.editor.setValue('PRINT 123');});await S.stubSaveDialog(target);
 C.eq(await S.page.evaluate(()=>window.__ide.save()),false,'Сохранение с числовым именем отклонено');C.eq(fs.existsSync(target),false,'Файл не создан');C.eq(await S.editorValue(),'PRINT 123','Черновик сохранён');C.ok(S.rendererDialogs.some(d=>d.message.includes('цифр')),'Показано объяснение имени для IMPORT');C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
