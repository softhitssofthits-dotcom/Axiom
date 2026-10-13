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
 const editor=fs.readFileSync(path.join(root,'ide/src/renderer/app.js'),'utf8');
 const light=editor.split("defineTheme('axiom-light'")[1].split("defineTheme('axiom-dark'")[0];
 const css=fs.readFileSync(path.join(root,'docs/manual/styles/code-colors.css'),'utf8');
 for(const match of light.matchAll(/token: '([^']+)', foreground: '([0-9a-fA-F]+)'/g))assert.ok(css.includes('.axiom-token--'+match[1].replaceAll('.','-')+'{color:#'+match[2]),match[1]);
 assert.equal(fs.readFileSync(path.join(root,'docs/manual/scripts/semantic-colors.js'),'utf8'),fs.readFileSync(path.join(root,'ide/src/renderer/semantic-colors.js'),'utf8'));
});
