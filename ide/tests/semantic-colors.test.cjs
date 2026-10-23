const {test}=require('node:test'); const assert=require('node:assert/strict'); const {classify,arrayType}=require('../src/renderer/semantic-colors');

test('Тип массива понимает квадратный и прежний суффиксы, но не массивный результат функции', () => {
 for (const [type, elementType, rank] of [['INTEGER[]','INTEGER',1], ['STRING * 4 [ , ]','STRING * 4',2], ['INTEGER(,)','INTEGER',2]]) {
  assert.deepEqual(arrayType(type), {elementType,rank});
 }
 for (const type of ['FUNCTION() AS INTEGER[]', 'FUNCTION() AS STRING()', 'INTEGER[)', 'INTEGER(]', 'INTEGER[1]', '[]']) assert.equal(arrayType(type), null, type);
});

test('Предварительные цвета квадратных объявлений и параметров не принимают запятые ранга за параметры', () => {
 const source='DIM grid[,] AS INTEGER\nDIM typed AS STRING[]\nFUNCTION Make() AS STRING[]\nRETURN typed[]\nEND FUNCTION\nSUB Work(BYREF rows[,] AS INTEGER, names[] AS STRING, value AS INTEGER)\nPRINT rows[1, 2], names[], value\nEND SUB';
 const result=decode(source);
 for (const name of ['grid','typed','rows','names']) assert.ok(result.filter(t=>t.name===name).every(t=>t.type===3), name);
 assert.equal(result.find(t=>t.name==='Make').type,1);
 assert.ok(result.filter(t=>t.name==='value').every(t=>t.type===0));
});

test('Несколько квадратных объявлений DIM сохраняют отдельные категории', () => {
 const source='DIM grid[,] AS INTEGER, names[] AS STRING, count AS INTEGER\nPRINT grid[1, 2], names[], count';
 const result=decode(source);
 for (const name of ['grid','names']) assert.ok(result.filter(t=>t.name===name).every(t=>t.type===3), name);
 assert.ok(result.filter(t=>t.name==='count').every(t=>t.type===0));
});

test('Функциональный результат не входит в список параметров предварительной раскраски', () => {
 const source='FUNCTION Make(values[] AS INTEGER) AS FUNCTION(names[] AS STRING) AS STRING[]\nPRINT values[], names\nEND FUNCTION';
 const result=decode(source);
 assert.equal(result.find(t=>t.line===1&&t.name==='values').type,3);
 assert.equal(result.find(t=>t.line===1&&t.name==='names').type,0);
});

function decode(source, symbols = []) {
 const lines=source.split('\n');
 const tokens=lines.map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^[A-Za-z_]/.test(m[0])&&!/^(CONST|DIM|BYREF|SUB|FUNCTION|RETURN|AS|INTEGER|STRING|END|PRINT)$/.test(m[0])?'identifier.axiom':'keyword.axiom'})));
 const data=[...classify(source,tokens,symbols).data], result=[];let line=0,col=0;
 for(let n=0;n<data.length;n+=5){col=data[n]===0?col+data[n+1]:data[n+1];line+=data[n];result.push({name:lines[line].slice(col,col+data[n+2]),line,type:data[n+3],modifier:data[n+4]});}
 return result;
}

test('Тип компилятора различает выведенный массив, переменную и функцию с учётом точной позиции', () => {
 for (const nl of ['\n', '\r\n', '\r']) {
  const lines = ['DIM fruits[] = MakeFruits()', 'PRINT fruits[2]', 'SUB Read(fruits AS INTEGER)', 'PRINT fruits', 'END SUB'];
  const source = lines.join(nl);
  const tokens = lines.map(line => [...line.matchAll(/[A-Za-z_]\w*/g)].map(m => ({offset:m.index,type:'identifier.axiom'})));
  const symbols = [];
  let start = 0;
  lines.forEach((line, row) => {
   for(const match of line.matchAll(/fruits|MakeFruits/g)) symbols.push({start:start+match.index,length:match[0].length,
    kind:match[0]==='MakeFruits'?'function':'variable',type:row<2?'STRING[]':'INTEGER'});
   start += line.length+nl.length;
  });
  const data = [...classify(source,tokens,symbols).data]; let row=0,col=0; const result=[];
  for(let i=0;i<data.length;i+=5){col=data[i]===0?col+data[i+1]:data[i+1];row+=data[i];const word=lines[row].slice(col,col+data[i+2]);if(word==='fruits'||word==='MakeFruits')result.push(data[i+3]);}
  assert.deepEqual(result,[3,1,3,0,0]);
 }
});
test('Модификаторы доступности не скрывают функцию и тип от семантической раскраски',()=>{
 const source='PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION\nPRIVATE TYPE Box\nItem AS INTEGER\nEND TYPE';
 const tokens=source.split('\n').map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^(PUBLIC|PRIVATE|FUNCTION|AS|INTEGER|RETURN|END|TYPE)$/.test(m[0])?'keyword.axiom':'identifier.axiom'})));
 const data=[...classify(source,tokens).data]; let line=0,column=0; const decoded=[];
 for(let i=0;i<data.length;i+=5){line+=data[i];column=data[i]===0?column+data[i+1]:data[i+1];decoded.push({word:source.split('\n')[line].slice(column,column+data[i+2]),kind:data[i+3]});}
 assert.ok(decoded.some(t=>t.word==='Answer'&&t.kind===1));
 assert.ok(decoded.some(t=>t.word==='Box'&&t.kind===2));
});
test('Цвета объявлений различают процедуры, константы, массивы и локальное перекрытие',()=>{
 const source='CONST limit = 2\nDIM items[] AS INTEGER\nSUB Draw(limit AS INTEGER)\nlimit = items[0]\nEND SUB\nPRINT limit';
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
