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
