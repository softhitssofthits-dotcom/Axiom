const {test}=require('node:test');
const assert=require('node:assert/strict');
const {EditedLines}=require('../src/renderer/edited-lines.js');
const edit=(start,end,text)=>({range:{startLineNumber:start,startColumn:1,endLineNumber:end},text});
test('Переходы не помечают строки; правки независимы для моделей',()=>{
 const tracker=new EditedLines(),a={},b={};assert.equal(tracker.has(a,2),false);
 tracker.record(a,[edit(2,2,'value')]);assert.equal(tracker.has(a,2),true);assert.equal(tracker.has(b,2),false);
 tracker.consume(a,2);assert.equal(tracker.has(a,2),false);
 tracker.record(a,[edit(2,2,'value')]);tracker.clear(a);assert.equal(tracker.has(a,2),false);
});
test('Автоматическая вставка сдвигает ожидающие строки, но не создаёт новых отметок',()=>{
 const tracker=new EditedLines(),model={};tracker.record(model,[edit(14,14,'value')]);
 tracker.record(model,[edit(2,2,'\n')],false);
 assert.equal(tracker.has(model,15),true);assert.equal(tracker.has(model,2),false);assert.equal(tracker.has(model,3),false);
 tracker.record(model,[edit(1,16,'\n'.repeat(15))],false);assert.equal(tracker.has(model,15),true);
});
test('Вставка и удаление строк сохраняют положение ещё не обработанной правки',()=>{
 const tracker=new EditedLines(),model={};tracker.record(model,[edit(14,14,'value')]);
 tracker.record(model,[edit(2,2,'\r\n\r\n')]);assert.equal(tracker.has(model,16),true);assert.equal(tracker.has(model,14),false);
 assert.equal(tracker.has(model,2),true);assert.equal(tracker.has(model,4),true);
 tracker.record(model,[edit(2,4,'')]);assert.equal(tracker.has(model,14),true);assert.equal(tracker.has(model,16),false);
});
