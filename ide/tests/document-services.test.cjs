const {test}=require('node:test');const assert=require('node:assert/strict');const {DocumentStore}=require('../src/renderer/documents');
const {DocumentServices}=require('../src/renderer/document-services');
function model(text){let listener;return {getValue:()=>text,onDidChangeContent:fn=>(listener=fn,{dispose(){}}),change(value){text=value;listener();}};}
test('Проверка передаёт снимок открытой зависимости и отвергает ответ после её правки',async()=>{
 const store=new DocumentStore(),main=model('IMPORT helper\nPRINT Answer()'),helper=model('PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 2\nEND FUNCTION');
 store.set('C:/project/main.axm',{model:main});store.set('C:/project/helper.axm',{model:helper});store.set('C:/other/hidden.axm',{model:model('PRINT 0')});
 let respond,snapshot;const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(resolve=>respond=resolve);}});
 const pending=service.request('C:/project/main.axm','document.compile');
 assert.equal(snapshot.projectDocuments?.length,1);
 assert.equal(snapshot.projectDocuments[0].sourceText,helper.getValue());
 helper.change('PRINT missing');respond({ok:true,payload:{...snapshot,diagnostics:[]}});
 assert.equal(await pending,null);
});

test('Закрытие зависимости и Save As отклоняют прежний проектный ответ',async()=>{
 for(const action of ['close','rename']){
  const store=new DocumentStore();store.set('C:/project/main.axm',{model:model('IMPORT helper')});store.set('C:/project/helper.axm',{model:model('PRINT 2')});
  let respond,snapshot;const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(resolve=>respond=resolve);}});
  const pending=service.request('C:/project/main.axm','document.hover');
  if(action==='close')store.delete('C:/project/helper.axm');else store.rename('C:/project/helper.axm','C:/project/renamed.axm');
  respond({ok:true,payload:{...snapshot,hovers:[]}});assert.equal(await pending,null);
 }
});

test('Навигация использует явную точку входа и отклоняет ответ после её смены',async()=>{
 const store=new DocumentStore();store.set('C:/project/main.axm',{model:model('IMPORT helper')});store.set('C:/project/helper.axm',{model:model('PRINT 1')});
 store.navigationEntryId=store.get('C:/project/main.axm').documentId;
 let respond,snapshot;const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(resolve=>respond=resolve);}});
 const pending=service.request('C:/project/helper.axm','document.navigation');
 assert.equal(snapshot.navigationEntryPath,'C:/project/main.axm');
 store.navigationEntryId=null;respond({ok:true,payload:{...snapshot,symbols:[]}});
 assert.equal(await pending,null);
});

test('Форматирование не устаревает из-за правки зависимости',async()=>{
 const store=new DocumentStore(),helper=model('PRINT 2');store.set('C:/project/main.axm',{model:model('print 1')});store.set('C:/project/helper.axm',{model:helper});
 let respond,snapshot;const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(resolve=>respond=resolve);}});
 const pending=service.request('C:/project/main.axm','document.format');helper.change('PRINT 3');
 respond({ok:true,payload:{...snapshot,sourceText:'PRINT 1'}});assert.ok(await pending);
});
test('Ответы документа переставлены: старый запрос не заменяет новый',async()=>{
 const store=new DocumentStore(),m=model('PRINT 1');store.set('a.axm',{model:m});const replies=[];
 const service=new DocumentServices(store,{documentCommand:(_method,snapshot)=>new Promise(resolve=>replies.push({resolve,snapshot}))});
 const old=service.request('a.axm','document.analyse');const latest=service.request('a.axm','document.analyse');
 replies[1].resolve({ok:true,payload:{...replies[1].snapshot,diagnostics:[]}});assert.ok(await latest);
 replies[0].resolve({ok:true,payload:{...replies[0].snapshot,diagnostics:[{code:'old'}]}});assert.equal(await old,null);
});
test('Правка во время форматирования и закрытая вкладка отклоняют ответ',async()=>{
 const store=new DocumentStore(),m=model('print 1');store.set('a.axm',{model:m});let respond,snapshot;
 const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(r=>respond=r);}});
 let pending=service.request('a.axm','document.format');m.change('PRINT 2');respond({ok:true,payload:{...snapshot,sourceText:'PRINT 1'}});assert.equal(await pending,null);
 pending=service.request('a.axm','document.format');store.delete('a.axm');respond({ok:true,payload:{...snapshot,sourceText:'PRINT 2'}});assert.equal(await pending,null);
});

test('SaveAs и новый идентификатор снимка отклоняют прежний ответ',async()=>{
 const store=new DocumentStore(),m=model('PRINT 1');store.set('a.axm',{model:m});let resolve,snapshot;
 const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(r=>resolve=r);}});
 let pending=service.request('a.axm','document.analyse');store.rename('a.axm','b.axm');resolve({ok:true,payload:{...snapshot,diagnostics:[]}});assert.equal(await pending,null);
 pending=service.request('b.axm','document.analyse');store.get('b.axm').documentId='new-identity';resolve({ok:true,payload:{...snapshot,diagnostics:[]}});assert.equal(await pending,null);
});

test('Регистр пути Windows не мешает применению правильного ответа',async()=>{
 const store=new DocumentStore(),m=model('PRINT 1');store.set('C:/Папка/A.axm',{model:m});
 const service=new DocumentServices(store,{documentCommand:async(_method,s)=>({ok:true,payload:{...s,diagnostics:[]}})});
 assert.ok(await service.request('c:\\папка\\a.axm','document.analyse'));
});

test('Анализ и проверка используют выбранный запуск и отклоняют ответ после смены запуска',async()=>{
 for(const method of ['document.analyse','document.compile']){
  const store=new DocumentStore();store.set('C:/project/main.axm',{model:model('IMPORT helper')});store.set('C:/project/helper.axm',{model:model('PUBLIC DIM Shared AS INTEGER')});
  let selected='C:/project/main.axm',respond,snapshot;
  const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(resolve=>respond=resolve);}},()=>selected);
  const pending=service.request('C:/project/helper.axm',method);
  assert.equal(snapshot.navigationEntryPath,selected);
  selected='C:/project/other.axm';respond({ok:true,payload:{...snapshot,diagnostics:[]}});
  assert.equal(await pending,null);
 }
});

test('Закрытый выбранный запуск передаётся анализу, а внешняя папка не получает его',async()=>{
 const store=new DocumentStore();store.set('C:/project/helper.axm',{model:model('PRINT 1')});store.set('C:/other/file.axm',{model:model('PRINT 2')});
 const snapshots=[];const service=new DocumentServices(store,{documentCommand:async(_method,s)=>{snapshots.push(s);return {ok:true,payload:{...s,diagnostics:[]}};}},()=> 'C:/project/main.axm');
 assert.ok(await service.request('C:/project/helper.axm','document.analyse'));
 assert.equal(snapshots[0].navigationEntryPath,'C:/project/main.axm');
 assert.ok(await service.request('C:/other/file.axm','document.analyse'));
 assert.equal(snapshots[1].navigationEntryPath,undefined);
});

test('Повторное открытие того же проекта отклоняет прежний ответ анализа',async()=>{
 const store=new DocumentStore();store.set('C:/project/helper.axm',{model:model('PUBLIC DIM Shared AS INTEGER')});
 for(const method of ['document.analyse','document.compile','document.hover','document.navigation']){
  let generation=1,respond,snapshot;
  const service=new DocumentServices(store,{documentCommand:(_method,s)=>{snapshot=s;return new Promise(r=>respond=r);}},()=> 'C:/project/main.axm',()=> 'C:/project/demo.axproj',()=>generation);
  const pending=service.request('C:/project/helper.axm',method);generation++;
  respond({ok:true,payload:{...snapshot,diagnostics:[]}});assert.equal(await pending,null);
 }
});
