const {test}=require('node:test');
const assert=require('node:assert/strict');
const {ProjectSession}=require('../src/renderer/project-session.js');

function setup(files=['C:/game/main.axm','C:/game/helper.axm']) {
  const models=new Map(),opened=[],settings={};
  const ide={projectDir:null,activeFile:null,settings,openFiles:models,
    saveSettings(patch){Object.assign(settings,patch);},
    renderProjectFiles(){},renderMiscellaneousFiles(){},renderProjectEntry(){},
    projectFiles:{reset(){}},setProjectExplorerVisible(){},setStatus(){},
    switchToFile(path){this.activeFile=path;},
    async closeFile(path){models.delete(path);return true;},
    async openFile(path,current=()=>true){if(!current())return;opened.push(path);
      if(!models.has(path))models.set(path,{path,documentId:path,model:{getValue:()=>'',isDisposed:()=>false}});
      this.activeFile=path;}
  };
  const api={listProjectFiles:async()=>files};
  const session=new ProjectSession(ide,api);
  return {session,ide,api,models,opened,settings};
}

test('Открытие проекта открывает все модули и выбирает main для запуска и помощника',async()=>{
  const {session,ide,models}=setup();await session.open('C:/game');
  assert.equal(models.size,2);assert.equal(ide.activeFile,'C:/game/main.axm');
  assert.equal(session.entry,'C:/game/main.axm');assert.equal(models.navigationEntryId,'C:/game/main.axm');
});
test('Переключение на модуль не меняет файл запуска; внешний файл запускается отдельно',async()=>{
  const {session,ide}=setup();await session.open('C:/game');
  ide.activeFile='C:/game/helper.axm';assert.equal(session.target(),'C:/game/main.axm');
  ide.activeFile='C:/other/test.axm';assert.equal(session.target(),'C:/other/test.axm');
});
test('Без main несколько файлов требуют выбора, а единственный выбирается автоматически',async()=>{
  const a=setup(['C:/game/a.axm','C:/game/b.axm']);await a.session.open('C:/game');
  assert.equal(a.session.entry,null);assert.equal(a.session.target(),null);
  const b=setup(['C:/game/only.axm']);await b.session.open('C:/game');assert.equal(b.session.entry,'C:/game/only.axm');
});
test('Выбор сохраняется для папки и имеет приоритет перед main при повторном открытии',async()=>{
  const {session,ide,settings}=setup();await session.open('C:/game');session.select('C:/game/helper.axm');
  assert.equal(session.target(),'C:/game/helper.axm');assert.ok(settings.projectEntries);
  await session.open('C:/game');assert.equal(session.entry,'C:/game/helper.axm');
});
test('Повторное открытие сохраняет существующую модель и её несохранённый текст',async()=>{
  const {session,models}=setup();const existing={documentId:'edited',model:{getValue:()=> 'PRINT 99',isDisposed:()=>false}};
  models.set('C:/game/helper.axm',existing);await session.open('C:/game');
  assert.equal(models.get('C:/game/helper.axm'),existing);
});
test('Удаляется лишь пустая стартовая вкладка; непустой документ сохраняется',async()=>{
  const {session,models}=setup();models.set('untitled1',{path:'untitled1',model:{getValue:()=>''}});
  models.set('untitled2',{path:'untitled2',model:{getValue:()=> 'PRINT 7'}});await session.open('C:/game');
  assert.equal(models.has('untitled1'),false);assert.equal(models.has('untitled2'),true);
});
test('Поздний список старой папки не подменяет новый проект',async()=>{
  const {session,api,ide}=setup();let finish;
  api.listProjectFiles=dir=>dir==='C:/old'?new Promise(r=>finish=r):Promise.resolve(['C:/new/main.axm']);
  const old=session.open('C:/old');await session.open('C:/new');finish(['C:/old/main.axm']);await old;
  assert.equal(ide.projectDir,'C:/new');assert.equal(session.entry,'C:/new/main.axm');
});
test('Закрытие отменяет запоздалый список и открытие файлов',async()=>{
  const {session,api,models}=setup();let finish;api.listProjectFiles=()=>new Promise(r=>finish=r);
  const opening=session.open('C:/game');session.reset();finish(['C:/game/main.axm']);await opening;
  assert.equal(models.size,0);assert.equal(session.entry,null);
});
test('Переименование выбранного файла сохраняет точку входа и новое имя в настройках',async()=>{
  const {session}=setup();await session.open('C:/game');session.renamed('C:/game/main.axm','C:/game/start.axm');
  session.refresh(['C:/game/start.axm','C:/game/helper.axm']);assert.equal(session.entry,'C:/game/start.axm');
});
test('Удаление точки входа снимает выбор без запуска другого файла',async()=>{
  const {session,ide}=setup();await session.open('C:/game');session.refresh(['C:/game/helper.axm']);
  ide.activeFile='C:/game/helper.axm';assert.equal(session.entry,null);assert.equal(session.target(),null);
});
test('Сохранённый путь за пределами проекта отвергается',async()=>{
  const {session,settings}=setup(['C:/game/a.axm','C:/game/b.axm']);
  settings.projectEntries={'c:\\game':'../outside.axm'};await session.open('C:/game');assert.equal(session.entry,null);
});
test('Восстановление не переоткрывает закрытые вкладки, но восстанавливает выбор запуска',async()=>{
  const {session,opened}=setup();await session.open('C:/game',{restore:true});
  assert.deepEqual(opened,[]);assert.equal(session.entry,'C:/game/main.axm');
});

test('Смена проекта во время чтения не добавляет вкладку старой папки',async()=>{
  const {session,ide,api,models}=setup();let finish;
  api.listProjectFiles=async dir=>[dir+'/main.axm'];
  const normal=ide.openFile.bind(ide);
  ide.openFile=(file,current,options)=>file.startsWith('C:/old')?new Promise(resolve=>{
    finish=async()=>{await normal(file,current,options);resolve();};
  }):normal(file,current,options);
  const old=session.open('C:/old');await Promise.resolve();
  await session.open('C:/new');await finish();await old;
  assert.equal(models.has('C:/old/main.axm'),false);assert.equal(session.entry,'C:/new/main.axm');
});

test('Ошибка списка файлов сохраняет прежний проект и завершает состояние загрузки',async()=>{
  const {session,ide,api}=setup();await session.open('C:/game');
  api.listProjectFiles=async()=>{throw new Error('Недоступная папка');};
  await assert.rejects(session.open('C:/unavailable'),/Недоступная/);
  assert.equal(ide.projectDir,'C:/game');assert.equal(session.entry,'C:/game/main.axm');assert.equal(session.loading,false);
});

test('Закрытая вкладка запуска остаётся выбранной, даже если все вкладки закрыты',async()=>{
  const {session,ide,models}=setup();await session.open('C:/game');models.clear();ide.activeFile=null;
  assert.equal(session.target(),'C:/game/main.axm');
});

test('Пустой проект не получает выдуманную точку входа',async()=>{
  const {session,models}=setup([]);await session.open('C:/game');assert.equal(models.size,0);assert.equal(session.target(),null);
});
