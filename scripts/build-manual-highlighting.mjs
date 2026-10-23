import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const manual=path.join(root,'docs/manual');
const inventory=JSON.parse(fs.readFileSync(path.join(manual,'data/language-reference.json'),'utf8'));
const dictionary={keywords:inventory.keywords,types:inventory.types,builtins:inventory.functions.map(fn=>fn.name)};

// Палитра берётся из общего реестра тем IDE. Раньше цвета читались из app.js,
// но темы переехали в themes.js, и сборка справочника не находила светлую тему.
const themesContext={};
vm.runInNewContext(fs.readFileSync(path.join(root,'ide/src/renderer/themes.js'),'utf8'),themesContext);
const themes=themesContext.AxiomThemes;
if(!themes)throw new Error('Не найден реестр тем IDE.');
const rules=themes.syntaxRules('light');
if(rules.length<10)throw new Error('Не удалось прочитать палитру редактора.');
const css='/* Сформировано из светлой темы в ide/src/renderer/themes.js. */\n'+rules.map(({token,foreground,fontStyle})=>`.axiom-token--${token.replaceAll('.','-')}{color:#${foreground};${fontStyle==='italic'?'font-style:italic;':''}}`).join('\n')+'\n.axiom-token--placeholder{color:#59636e}\n';
const semanticSource=fs.readFileSync(path.join(root,'ide/src/renderer/semantic-colors.js'),'utf8');
const semanticContext={};
vm.runInNewContext(semanticSource,semanticContext);
if(semanticContext.AxiomSemanticColors?.arrayType('INTEGER[,]')?.rank!==2)
 throw new Error('Подсветка IDE ещё не поддерживает квадратные массивы; прежнюю копию справочника не заменяем.');
// Проверки выполняются до первой записи: несовместимый общий модуль не оставляет
// частично обновлённые словарь, палитру и семантическую раскраску справочника.
fs.writeFileSync(path.join(manual,'scripts/code-dictionary.js'),'// Сформировано из действующего реестра языка.\nwindow.AxiomCodeDictionary = '+JSON.stringify(dictionary,null,2)+';\n');
fs.writeFileSync(path.join(manual,'styles/code-colors.css'),css);
fs.writeFileSync(path.join(manual,'scripts/semantic-colors.js'),semanticSource);
console.log(`Подсветка: ${dictionary.builtins.length} функций, ${dictionary.types.length} типов; ${rules.length} цветов из светлой темы.`);
