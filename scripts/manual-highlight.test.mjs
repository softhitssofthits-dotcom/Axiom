import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
function highlighter(){
 const file=path.join(root,'docs/manual/scripts/highlight.js');
 assert.ok(fs.existsSync(file),'В справочнике должен существовать обработчик подсветки примеров');
 const context={window:{}};
 vm.runInNewContext(fs.readFileSync(path.join(root,'docs/manual/scripts/code-dictionary.js'),'utf8'),context);
 vm.runInNewContext(fs.readFileSync(file,'utf8'),context);
 return context.window.AxiomManualHighlight;
}
test('Подсветка различает слова языка и сохраняет исходную программу',()=>{
 const api=highlighter();const source='CONST LIMIT AS INTEGER = 10\nPRINT LEN("Текст") \' LEN и CONST в комментарии\n';
 const tokens=api.tokenize(source);
 assert.equal(tokens.map(token=>token.text).join(''),source);
 for(const [word,type] of [['CONST','keyword'],['INTEGER','type'],['LEN','support.function'],['10','number']])assert.ok(tokens.some(token=>token.text===word&&token.type===type),word);
 assert.ok(tokens.some(token=>token.type==='comment'&&token.text.includes('LEN и CONST')));
});
test('Вставки интерполяции содержат код, вложенные строки и формат',()=>{
 const api=highlighter();const source='PRINT $"{{Итог}} {LEN("CONST")} {10:F2}"';
 const tokens=api.tokenize(source);
 assert.equal(tokens.map(token=>token.text).join(''),source);
 assert.ok(tokens.some(token=>token.text==='LEN'&&token.type==='support.function'));
 assert.ok(tokens.some(token=>token.text==='CONST'&&token.type==='string'));
 assert.ok(tokens.some(token=>token.text==='F2'&&token.type==='string'));
 assert.ok(tokens.some(token=>token.text==='{{'&&token.type==='string.escape'));
});
test('Цвета светлой темы и семантические категории берутся из редактора IDE',()=>{
 highlighter();
 // Темы IDE живут в общем реестре themes.js; цвета справочника обязаны совпадать с ним.
 const themesContext={};
 vm.runInNewContext(fs.readFileSync(path.join(root,'ide/src/renderer/themes.js'),'utf8'),themesContext);
 const rules=themesContext.AxiomThemes.syntaxRules('light');
 const css=fs.readFileSync(path.join(root,'docs/manual/styles/code-colors.css'),'utf8');
 assert.ok(rules.length>=10,'Светлая тема должна содержать правила подсветки');
 for(const rule of rules)assert.ok(css.includes('.axiom-token--'+rule.token.replaceAll('.','-')+'{color:#'+rule.foreground),rule.token);
 assert.equal(fs.readFileSync(path.join(root,'docs/manual/scripts/semantic-colors.js'),'utf8'),fs.readFileSync(path.join(root,'ide/src/renderer/semantic-colors.js'),'utf8'));
});


test('Двоичная и шестнадцатеричная запись выделены как числа и отделены от &',()=>{
 const source='PRINT &b1010&"x"\nPRINT $"{&B1111:B8}"\nPRINT &HFF&"y"';
 const tokens=highlighter().tokenize(source);
 assert.equal(tokens.map(token=>token.text).join(''),source);
 assert.ok(tokens.some(token=>token.text==='&b1010'&&token.type==='number.binary'));
 assert.ok(tokens.some(token=>token.text==='&B1111'&&token.type==='number.binary'));
 assert.ok(tokens.some(token=>token.text==='&HFF'&&token.type==='number.hex'));
 assert.ok(tokens.some(token=>token.text==='&'&&token.type==='operator'));
});


test('Квадратные обращения, вызов внутри индекса и формат интерполяции сохраняются',()=>{
 const source=`DIM fruits[] AS STRING = MakeFruits()
PRINT $"{fruits[ChooseFruit() + (offset * 2)]:F2}"
PRINT MakeFruits()[2]`;
 const tokens=highlighter().tokenize(source);
 assert.equal(tokens.map(token=>token.text).join(''),source);
 for(const bracket of ['[',']'])assert.ok(tokens.some(token=>token.text===bracket&&token.type==='delimiter'));
 assert.ok(tokens.some(token=>token.text==='F2'&&token.type==='string'));
 assert.ok(tokens.some(token=>token.text==='MakeFruits'&&token.type==='identifier'));
 assert.ok(!tokens.some(token=>token.text==='ChooseFruit'&&token.type==='string'));
});

test('Метанотация необязательных частей отличается от буквальных квадратных скобок',()=>{
 const source='FUNCTION <имя>(⟦<параметры>⟧) AS STRING[]';
 const tokens=highlighter().tokenize(source);
 assert.equal(tokens.map(token=>token.text).join(''),source);
 for(const symbol of ['⟦','⟧'])assert.ok(tokens.some(token=>token.text===symbol&&token.type==='placeholder'));
 for(const symbol of ['[',']'])assert.ok(tokens.some(token=>token.text===symbol&&token.type==='delimiter'));
});

test('Семантика справочника отличает массивный параметр и вызов с массивным результатом',()=>{
 const context={window:{}};
 vm.runInNewContext(fs.readFileSync(path.join(root,'docs/manual/scripts/semantic-colors.js'),'utf8'),context);
 const api=context.window.AxiomSemanticColors;
 assert.equal(api.arrayType('STRING[]').rank,1);
 assert.equal(api.arrayType('INTEGER[,]').rank,2);
 assert.equal(api.arrayType('FUNCTION() AS STRING[]'),null);
 const source=`DIM fruits[] AS STRING = MakeFruits()
SUB Show(values[] AS STRING)
PRINT values[2]
END SUB
FUNCTION MakeFruits() AS STRING[]
RETURN fruits[]
END FUNCTION`;
 const lines=source.split('\n');
 const tokenLines=lines.map(line=>[...line.matchAll(/[A-Za-z_]\w*|[^A-Za-z_]+/g)].map(m=>({offset:m.index,type:/^[A-Za-z_]/.test(m[0])&&!/^(DIM|AS|STRING|SUB|PRINT|END|FUNCTION|RETURN)$/.test(m[0])?'identifier':'keyword.axiom'})));
 const data=[...api.classify(source,tokenLines).data];let row=0,column=0;const words=[];
 for(let i=0;i<data.length;i+=5){row+=data[i];column=data[i]===0?column+data[i+1]:data[i+1];words.push({word:lines[row].slice(column,column+data[i+2]),type:data[i+3]});}
 assert.ok(words.some(t=>t.word==='fruits'&&t.type===3));
 assert.ok(words.filter(t=>t.word==='values').every(t=>t.type===3));
 assert.ok(words.filter(t=>t.word==='MakeFruits').every(t=>t.type===1));
});
