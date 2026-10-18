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
