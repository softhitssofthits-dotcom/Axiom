const {test}=require('node:test'); const assert=require('node:assert/strict'); const {classify}=require('../src/renderer/semantic-colors');
test('Модификаторы доступности не скрывают функцию и тип от семантической раскраски',()=>{
 const source='PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION\nPRIVATE TYPE Box\nItem AS INTEGER\nEND TYPE';
 const tokens=source.split('\n').map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^(PUBLIC|PRIVATE|FUNCTION|AS|INTEGER|RETURN|END|TYPE)$/.test(m[0])?'keyword.axiom':'identifier.axiom'})));
 const data=[...classify(source,tokens).data]; let line=0,column=0; const decoded=[];
 for(let i=0;i<data.length;i+=5){line+=data[i];column=data[i]===0?column+data[i+1]:data[i+1];decoded.push({word:source.split('\n')[line].slice(column,column+data[i+2]),kind:data[i+3]});}
 assert.ok(decoded.some(t=>t.word==='Answer'&&t.kind===1));
 assert.ok(decoded.some(t=>t.word==='Box'&&t.kind===2));
});
test('Цвета объявлений различают процедуры, константы, массивы и локальное перекрытие',()=>{
 const source='CONST limit = 2\nDIM items(3) AS INTEGER\nSUB Draw(limit AS INTEGER)\nlimit = items(0)\nEND SUB\nPRINT limit';
 const lines=source.split('\n'); const tokens=lines.map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^[A-Za-z_]/.test(m[0])&&!/^(CONST|DIM|SUB|AS|INTEGER|END|PRINT)$/.test(m[0])?'identifier.axiom':'keyword.axiom'})));
 const data=[...classify(source,tokens).data]; const decoded=[];let line=0,col=0;for(let n=0;n<data.length;n+=5){col=data[n]===0?col+data[n+1]:data[n+1];line+=data[n];decoded.push({name:lines[line].slice(col,col+data[n+2]),line,type:data[n+3],modifier:data[n+4]});}
 assert.ok(decoded.some(t=>t.name==='Draw'&&t.type===1)); assert.ok(decoded.some(t=>t.name==='items'&&t.type===3));
 assert.equal(decoded.find(t=>t.line===3&&t.name==='limit').modifier,0);assert.equal(decoded.find(t=>t.line===5&&t.name==='limit').modifier,1);
});

test('Префиксы двоичных и шестнадцатеричных чисел не являются пользовательскими именами',()=>{
 const source='PRINT &B1010 &HFF';
 const data=classify(source,[[{offset:0,type:'keyword.axiom'},{offset:5,type:'white.axiom'},
  {offset:6,type:'number.binary.axiom'},{offset:12,type:'white.axiom'},{offset:13,type:'number.hex.axiom'}]]).data;
 assert.equal(data.length % 5,0);
 for(let i=3;i<data.length;i+=5) assert.equal(data[i],4,'PRINT остаётся ключевым словом, числа не получают семантический токен');
});

test('Оператор SMOOTH и состояния ON/OFF получают семантическую категорию ключевого слова',()=>{
 const source='SMOOTH ON\nSMOOTH OFF';
 const tokens=[[{offset:0,type:'keyword.axiom'},{offset:6,type:'white.axiom'},{offset:7,type:'keyword.axiom'}],
  [{offset:0,type:'keyword.axiom'},{offset:6,type:'white.axiom'},{offset:7,type:'keyword.axiom'}]];
 const data=[...classify(source,tokens).data];
 const decoded=[];let line=0,column=0;
 for(let i=0;i<data.length;i+=5){line+=data[i];column=data[i]===0?column+data[i+1]:data[i+1];decoded.push({word:source.split('\n')[line].slice(column,column+data[i+2]),kind:data[i+3]});}
 assert.deepEqual(decoded.map(token=>token.word),['SMOOTH','ON','SMOOTH','OFF']);
 assert.ok(decoded.every(token=>token.kind===4));
});
