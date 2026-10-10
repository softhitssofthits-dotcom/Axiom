const {test}=require('node:test');const assert=require('node:assert/strict');const {DocumentStore}=require('../src/renderer/documents');
const {DocumentServices}=require('../src/renderer/document-services');
function model(text){let listener;return {getValue:()=>text,onDidChangeContent:fn=>(listener=fn,{dispose(){}}),change(value){text=value;listener();}};}
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
