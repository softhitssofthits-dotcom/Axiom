const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const vm=require('node:vm');
const path=require('node:path');
const context={window:{}};
vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../src/renderer/program-assistant.js'),'utf8'),context);

function fixture(){
  const assistant=Object.create(context.window.ProgramAssistant.prototype);
  const model={isDisposed:()=>false,getVersionId:()=>1,getValue:()=> 'original'};
  const docs=new Map([['main.axm',{model}]]);
  const view={file:'main.axm',model,version:1};
  let resolve;
  const pending=new Promise(done=>{resolve=done;});
  assistant.ide={openFiles:docs,openFile:async()=>{await pending;docs.set('helper.axm',{model});}};
  assistant.view=view;assistant.valid=()=>true;
  assistant.graph={sourceDocuments:[{sourceFilePath:'helper.axm',sourceText:'original'}]};
  assistant.capture=()=>({origin:true});assistant.release=()=>{assistant.released=true;};
  assistant.navigate=()=>{assistant.navigated=true;};assistant.t=key=>key;assistant.status={};
  return {assistant,view,resolve,model};
}

test('Позднее открытие не применяет переход после выбора другой панели',async()=>{
  const {assistant,view,resolve}=fixture();
  const request=assistant.navigateSource(view,{sourceFilePath:'helper.axm',line:5,column:1});
  assistant.view={...view};assistant.status.textContent='new graph';resolve();await request;
  assert.equal(assistant.navigated,undefined);assert.equal(assistant.released,true);
  assert.equal(assistant.status.textContent,'new graph');
});

test('Отменённый переход не меняет курсор и историю',async()=>{
  const {assistant,view,resolve}=fixture();
  const request=assistant.navigateSource(view,{sourceFilePath:'helper.axm',line:5,column:1});
  assistant.pendingNavigation=null;resolve();await request;
  assert.equal(assistant.navigated,undefined);assert.equal(assistant.released,true);
});

test('Изменённый текст закрытой зависимости не получает старые координаты',async()=>{
  const {assistant,view,resolve,model}=fixture();
  const request=assistant.navigateSource(view,{sourceFilePath:'helper.axm',line:5,column:1});
  model.getValue=()=> 'changed';resolve();await request;
  assert.equal(assistant.navigated,undefined);assert.equal(assistant.status.textContent,'stale');
});

function symbolFixture(){
  const assistant=Object.create(context.window.ProgramAssistant.prototype);
  let position={lineNumber:1,column:6},resolve;
  const pending=new Promise(done=>{resolve=done;});
  const model={getVersionId:()=>1,getOffsetAt:p=>p.column-1,
    getWordAtPosition:()=>({word:'total',startColumn:1,endColumn:6})};
  const entry={path:'C:\\main.axm',documentId:'main',model};
  const docs=new Map([[entry.path,entry]]);docs.captureProject=()=>({documents:[]});
  assistant.ide={activeFile:entry.path,openFiles:docs,editor:{getPosition:()=>position},
    documentServices:{request:()=>pending},showPanel(){},switchPanel(){}};
  assistant.sequence=0;assistant.valid=()=>true;assistant.localize=()=>{};
  assistant.clearBody=()=>{};assistant.t=key=>key;assistant.status={};
  assistant.body={append(){}};assistant.button=()=>({});
  assistant.navigateSource=async()=>{assistant.navigated=true;};
  const site={symbolId:'total',name:'total',sourceFilePath:entry.path,start:0,length:5,isDeclaration:true,line:1,column:1};
  return {assistant,entry,resolve:()=>resolve({symbols:[site],complete:true}),inside:()=>{position={lineNumber:1,column:3};},
    move:()=>{position={lineNumber:2,column:1};}};
}

test('F12 на правой границе имени находит объявление',async()=>{
  const {assistant,resolve}=symbolFixture();
  const request=assistant.symbols(true);resolve();await request;
  assert.equal(assistant.navigated,true);
});

test('Поздний ответ F12 не возвращает каретку после её перемещения',async()=>{
  const {assistant,resolve,move,inside}=symbolFixture();inside();
  const request=assistant.symbols(true);move();resolve();await request;
  assert.equal(assistant.navigated,undefined);
});

test('Выбор точки входа принимает только именованный файл axm',()=>{
  const {assistant,entry}=symbolFixture();entry.path='C:\\notes.txt';
  assistant.selectEntry();assert.equal(assistant.ide.openFiles.navigationEntryId,undefined);
});

test('Перемещение каретки при чтении зависимости отменяет открытие и переход',async()=>{
  const {assistant,view,resolve}=fixture();
  let position={lineNumber:1,column:3};
  assistant.ide.editor={getPosition:()=>position};
  const open=assistant.ide.openFile;
  let opened=false;
  assistant.ide.openFile=async(file,isCurrent=()=>true)=>{
    await open(file);
    if(isCurrent())opened=true;
    else assistant.ide.openFiles.delete(file);
  };
  const request=assistant.navigateSource(view,{sourceFilePath:'helper.axm',line:5,column:1});
  position={lineNumber:2,column:1};resolve();await request;
  assert.equal(opened,false);assert.equal(assistant.navigated,undefined);
});
