const {test}=require('node:test'); const assert=require('node:assert/strict'); const {classify}=require('../src/renderer/semantic-colors');
test('Цвета объявлений различают процедуры, константы, массивы и локальное перекрытие',()=>{
 const source='CONST limit = 2\nDIM items(3) AS INTEGER\nSUB Draw(limit AS INTEGER)\nlimit = items(0)\nEND SUB\nPRINT limit';
 const lines=source.split('\n'); const tokens=lines.map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^[A-Za-z_]/.test(m[0])&&!/^(CONST|DIM|SUB|AS|INTEGER|END|PRINT)$/.test(m[0])?'identifier.axiom':'keyword.axiom'})));
 const data=[...classify(source,tokens).data]; const decoded=[];let line=0,col=0;for(let n=0;n<data.length;n+=5){col=data[n]===0?col+data[n+1]:data[n+1];line+=data[n];decoded.push({name:lines[line].slice(col,col+data[n+2]),line,type:data[n+3],modifier:data[n+4]});}
 assert.ok(decoded.some(t=>t.name==='Draw'&&t.type===1)); assert.ok(decoded.some(t=>t.name==='items'&&t.type===3));
 assert.equal(decoded.find(t=>t.line===3&&t.name==='limit').modifier,0);assert.equal(decoded.find(t=>t.line===5&&t.name==='limit').modifier,1);
});
