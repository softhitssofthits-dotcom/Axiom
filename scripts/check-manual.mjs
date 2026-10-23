import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';

// Проверяет опубликованный справочник без сервера и изменения файлов.
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const manual = path.join(root, 'docs/manual');
const errors = [];
const pages = new Map();
const voidTags=new Set('area base br col embed hr img input link meta param source track wbr'.split(' '));
for (const name of fs.readdirSync(manual).filter(name => name.endsWith('.html'))) {
  const text = fs.readFileSync(path.join(manual, name), 'utf8');
  const ids = [...text.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]);
  if (new Set(ids).size !== ids.length) errors.push(`${name}: повторяющиеся якоря`);
  if ([...text.matchAll(/<h1\b/g)].length !== 1) errors.push(`${name}: требуется один h1`);
  if (!/<html lang="ru">/.test(text)) errors.push(`${name}: язык страницы`);
  if (!/class="header-search"/.test(text) || !/type="search"/.test(text) || !/for="manual-search-input"/.test(text)) errors.push(`${name}: нет доступного поиска в шапке`);
  if (!text.includes('scripts/chapters.js')) errors.push(`${name}: нет общего оглавления`);
  if (/QuickBASIC|QBasic|Visual Basic|\bBASIC\b|Зельднер|Мокрушин/i.test(text)) errors.push(`${name}: внешнее сравнение`);
  if (/class="language-basic"/.test(text)) errors.push(`${name}: устаревшая категория примера`);
  const markup=text.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi,'').replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi,'').replace(/<!--[\s\S]*?-->/g,'');
  const stack=[];
  for(const match of markup.matchAll(/<(\/?)([a-z][a-z0-9-]*)\b[^>]*>/gi)){
    const tag=match[2].toLowerCase();if(voidTags.has(tag)||match[0].endsWith('/>'))continue;
    if(match[1]){if(stack.pop()!==tag){errors.push(`${name}: неверная вложенность тега ${tag}`);break;}}
    else stack.push(tag);
  }
  if(stack.length)errors.push(`${name}: незакрытые теги`);
  pages.set(name, { text, ids: new Set(ids) });
}
let linkCount = 0;
function checkLink(href, from) {
  if (!href || /^(?:https?:|mailto:|data:)/.test(href)) return;
  linkCount++;
  const [file, anchor] = href.replaceAll('&amp;', '&').split('#');
  const target = path.resolve(manual, file || from);
  if (!fs.existsSync(target)) { errors.push(`${from}: отсутствует ${href}`); return; }
  if (anchor && target.endsWith('.html')) {
    const info = pages.get(path.basename(target));
    const ids = info?.ids ?? new Set([...fs.readFileSync(target, 'utf8').matchAll(/\bid="([^"]+)"/g)].map(m => m[1]));
    if (!ids.has(decodeURIComponent(anchor))) errors.push(`${from}: отсутствует якорь ${href}`);
  }
}
for (const [name, page] of pages) {
  for (const match of page.text.matchAll(/\b(?:href|src)="([^"]+)"/g)) checkLink(match[1], name);
}
const context = { window: {} };
vm.runInNewContext(fs.readFileSync(path.join(manual, 'scripts/search-index.js'), 'utf8'), context);
const index = context.window.AxiomSearchIndex;
const searchDestinations=new Set();
for(const entry of index){
  if(searchDestinations.has(entry.href))errors.push(`Повторная цель в поиске: ${entry.href}`);
  searchDestinations.add(entry.href);
}
for (const entry of index) checkLink(entry.href, 'scripts/search-index.js');
const nav = fs.readFileSync(path.join(manual, 'scripts/manual.js'), 'utf8');
for (const match of nav.matchAll(/href:\s*"([^"]+)"/g)) checkLink(match[1], 'scripts/manual.js');
vm.runInNewContext(fs.readFileSync(path.join(manual, 'scripts/chapters.js'), 'utf8'), context);
const chapters = context.window.AxiomManualChapters;
if (chapters.filter(chapter => chapter.number).length !== 17) errors.push('Оглавление должно содержать 17 тематических глав.');
const navFiles = new Set();
for (const chapter of chapters) for (const item of chapter.items) {
  checkLink(item.href, 'scripts/chapters.js');
  if (navFiles.has(item.href)) errors.push(`Повторная тема в оглавлении: ${item.href}`);
  navFiles.add(item.href);
}
const compatibilityPages = new Set(['language-syntax.html','data-types-scalars.html','data-types-records-and-arrays.html','arrays-1d-visual.html','arrays-2d-visual.html']);
for (const [name] of pages) if (name !== 'index.html' && !compatibilityPages.has(name) && !navFiles.has(name)) errors.push(`${name}: отсутствует в тематическом оглавлении`);
for (const name of ['function-reference.html','operator-reference.html']) if (pages.get(name)?.text.includes('class="reference-details"')) errors.push(`${name}: каталог повторяет полное описание тематической статьи`);
const limits = pages.get('data-type-limits.html')?.text ?? '';
for (const boundary of ['2147483647','9223372036854775807','79228162514264337593543950335','1.7976931348623157E+308']) if (!limits.includes(boundary)) errors.push(`Нет числового предела ${boundary} в таблице диапазонов.`);
if (fs.existsSync(path.join(manual, 'data/language-reference.json'))) {
  const inventory = JSON.parse(fs.readFileSync(path.join(manual, 'data/language-reference.json'), 'utf8'));
  const catalog = pages.get('function-reference.html')?.text ?? '';
  for (const fn of inventory.functions) {
    if (!catalog.includes(`id="fn-${fn.name.toLowerCase()}"`)) errors.push(`Нет карточки функции ${fn.name}`);
    if (!index.some(entry => entry.title === fn.name && entry.href === `function-reference.html#fn-${fn.name.toLowerCase()}`)) errors.push(`Нет точной записи указателя ${fn.name}`);
    for (const form of fn.forms) if (!catalog.includes(`data-form="${form.key}"`)) errors.push(`Нет формы ${form.key}`);
  }
  const operators = pages.get('operator-reference.html')?.text ?? '';
  for (const op of inventory.statements) {
    if (!operators.includes(`id="op-${op.id}"`)) errors.push(`Нет карточки оператора ${op.name}`);
    checkLink(op.href, 'data/language-reference.json');
  }
  const fingerprint=crypto.createHash('sha256').update(inventory.sourceFiles.map(f=>f+'\n'+fs.readFileSync(path.join(root,f),'utf8').replaceAll('\r\n','\n')).join('\n')).digest('hex');
  if(fingerprint!==inventory.sourceFingerprint)errors.push('Снимок языка устарел: обновите реестр и справочник.');
  const words=pages.get('keyword-reference.html')?.text??'';
  for(const word of inventory.keywords)if(!words.includes(`id="kw-${word.toLowerCase()}"`))errors.push(`Нет ключевого слова ${word}`);
  for(const type of inventory.types)if(!words.includes(`id="type-${type.toLowerCase()}"`))errors.push(`Нет типа ${type}`);
  for(const op of inventory.expressionOperators)if(!words.includes(`id="expr-${op.token.toLowerCase()}"`))errors.push(`Нет операции ${op.name}`);
  const precedence=fs.readFileSync(path.join(root,'src/Axiom.Parsing/Parser.Expressions.cs'),'utf8').split('private static int GetUnaryOperatorPrecedence')[1]?.split('private static bool IsRightAssociativeBinaryOperator')[0]??'';
  for(const match of precedence.matchAll(/SyntaxKind\.(\w+)/g))if(!inventory.expressionOperators.some(op=>op.token===match[1]))errors.push(`Не описана операция парсера ${match[1]}`);
  // Проверяем настоящую функцию ранжирования без браузера и изменения её логики.
  const searchContext={window:{AxiomSearchIndex:index},document:{getElementById:()=>null},HTMLInputElement:class{}};
  const searchSource=fs.readFileSync(path.join(manual,'scripts/search.js'),'utf8').replace('  const input = document.getElementById','  window.searchForCheck = search;\n  const input = document.getElementById');
  vm.runInNewContext(searchSource,searchContext);
  for(const fn of inventory.functions){
    const first=searchContext.window.searchForCheck(index,fn.name)[0];
    if(first?.href!==`function-reference.html#fn-${fn.name.toLowerCase()}`)errors.push(`Поиск ${fn.name} не ведёт первым результатом к карточке функции`);
  }
  const arg=process.argv.indexOf('--inventory');
  if(arg>=0){
    const actual=JSON.parse(fs.readFileSync(path.resolve(root,process.argv[arg+1]),'utf8'));
    const forms=inventory.functions.flatMap(f=>f.forms.map(form=>f.name+'('+form.parameters.map(p=>p.name+':'+p.type).join(',')+'):'+form.returnType+':'+form.binding+':'+form.operandPolicy+':'+form.resultPolicy)).sort();
    const registered=actual.Rows.map(r=>r.Name+'('+r.Parameters.map(p=>p.Name+':'+p.Type).join(',')+'):'+r.ReturnType+':'+r.Binding+':'+r.OperandPolicy+':'+r.ResultPolicy).sort();
    if(JSON.stringify(forms)!==JSON.stringify(registered))errors.push('Формы вызова не совпадают с загруженным реестром компилятора.');
    const covered=new Set(inventory.statements.map(s=>s.syntax.endsWith('Statement') ? s.syntax+'Syntax' : s.syntax+'StatementSyntax'));
    for(const row of actual.Integrity.SyntaxStatements)if(!covered.has(row.Name)&&!inventory.internalSyntax[row.Name])errors.push(`Не описана инструкция ${row.Name}`);
    for(const type of ['Type','Sub','Function'])if(!inventory.statements.some(s=>s.syntax===type+'Declaration'))errors.push(`Не описано объявление ${type}`);
  }
  console.log(`Функции: ${inventory.functions.length}; формы: ${inventory.functions.reduce((n,f)=>n+f.forms.length,0)}; операторы и формы инструкций: ${inventory.statements.length}.`);
}
console.log(`Страницы: ${pages.size}; ссылки: ${linkCount}; записи поиска: ${index.length}.`);
if (errors.length) {
  console.error(errors.slice(0, 30).join('\n'));
  console.error(`Всего замечаний: ${errors.length}`);
  process.exitCode = 1;
} else console.log('Проверка справочника пройдена.');
