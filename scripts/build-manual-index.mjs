import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';
import {manualPage} from './manual-page.mjs';
import './build-manual-highlighting.mjs';
import {mergeSearchDestinations} from './manual-search-index.mjs';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const dir=path.join(root,'docs/manual');
const context={window:{}};
vm.runInNewContext(fs.readFileSync(path.join(dir,'scripts/chapters.js'),'utf8'),context);
const chapters=context.window.AxiomManualChapters;
const inventory=JSON.parse(fs.readFileSync(path.join(dir,'data/language-reference.json'),'utf8'));
const plain=value=>String(value||'').replace(/<[^>]*>/g,' ').replaceAll('&lt;','<').replaceAll('&gt;','>').replaceAll('&amp;','&').replace(/\s+/g,' ').trim();
const escape=value=>String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const section=(title,body,ids=[])=>`<section class="article-section"${ids.length?' id="'+ids[0]+'"':''}>${ids.slice(1).map(id=>'<span id="'+id+'"></span>').join('')}<h2>${title}</h2>${body}</section>`;
const related=section('См. также','<div class="link-list"><a href="getting-started.html">Первая программа</a><a href="reference.html">Алфавитный указатель</a><a href="examples.html">Примеры программ</a></div>');
const summaries=[
 'Возможности языка, первая программа, проверка и запуск.',
 'Объявления, значения, типы, присваивание и доступность имён.',
 'Вычисления, сравнения, логика, приоритет и преобразования.',
 'Вывод результата, форматирование и получение ответа пользователя.',
 'Проверка условий и выбор ветви по значению.',
 'Повторение по счётчику или условию, выход и продолжение.',
 'Числовые типы, математические функции и случайные числа.',
 'Строковый тип, интерполяция, фрагменты, поиск и коды символов.',
 'Временные типы, календарные вычисления и часовые пояса.',
 'Одномерные и многомерные данные, срезы и операции над элементами.',
 'SUB и FUNCTION, вызовы, параметры и функции как значения.',
 'Записи TYPE, поля, копирование, методы и конструкторы.',
 'Перехват ошибок, завершение обработки и создание ошибки.',
 'Текстовые файлы, пути, каталоги и файловые ресурсы.',
 'Проверка шаблона, совпадения, группы, разбиение и замена.',
 'SCREEN и CANVAS, рисование, изображения, спрайты и карты.',
 'Состояние устройств, обработчики событий и интерактивный цикл.'
];
const home=`<ol class="chapter-list">${chapters.filter(c=>c.number).map((chapter,i)=>`<li><div><h2><a href="${chapter.items[0].href}">${escape(chapter.title)}</a></h2><p>${summaries[i]}</p><div class="chapter-links">${chapter.items.slice(1,4).map(item=>`<a href="${item.href}">${escape(item.label)}</a>`).join('')}</div></div></li>`).join('\n')}</ol>`;
fs.writeFileSync(path.join(dir,'index.html'),manualPage({id:'home',title:'Справочник языка Axiom',eyebrow:'От первой программы к сложным задачам',lead:'Начните с переменных и простых вычислений, затем переходите к условиям, массивам, файлам и графике. Для поиска конкретного оператора или функции используйте поле в шапке или алфавитный указатель.',body:home+section('Справочные материалы','<div class="link-list">'+chapters.at(-1).items.filter(i=>i.href!=='search.html').map(i=>`<a href="${i.href}">${escape(i.label)}</a>`).join('')+'</div>',['complete-reference'])}));
fs.writeFileSync(path.join(dir,'search.html'),manualPage({id:'search',title:'Поиск по справочнику',lead:'Введите запрос в поле в шапке. Можно искать по имени оператора или функции, названию темы и словам из описания.',eyebrow:'Поиск',body:'<p id="manual-search-status" role="status" aria-live="polite"></p><div id="manual-search-results"></div>'+related}));
const index=[];
for(const chapter of chapters)for(const item of chapter.items){
 const html=fs.readFileSync(path.join(dir,item.href.split('#')[0]),'utf8');
 const title=plain(html.match(/<h1[^>]*>([\s\S]*?)<\/h1>/)?.[1]||item.label);
 const description=plain(html.match(/<p class="lead">([\s\S]*?)<\/p>/)?.[1]||item.label);
 index.push({title,href:item.href,kind:chapter.number?'Тема':'Приложение',category:chapter.title,description,keywords:[item.label,chapter.title]});
}
index.push({title:'Справочник Axiom',href:'index.html',kind:'Оглавление',category:'Справочник',description:'Семнадцать глав от первой программы до событий.',keywords:['главная','оглавление']});
const categoryFor=href=>chapters.find(c=>c.items.some(i=>i.href.split('#')[0]===href.split('#')[0]))?.title||'Справочник';
for(const fn of inventory.functions)index.push({title:fn.name,href:`function-reference.html#fn-${fn.name.toLowerCase()}`,kind:'Функция',category:categoryFor(fn.sourceHref),description:fn.description,keywords:[fn.name,fn.sourceHref.split('.')[0],...fn.forms.flatMap(form=>form.parameters.map(p=>p.name))]});
for(const op of inventory.statements)index.push({title:op.name,href:op.href,kind:'Оператор',category:categoryFor(op.href),description:'Синтаксис, параметры, действие и примеры '+op.name+'.',keywords:[op.name,op.syntax]});
for(const type of inventory.types)index.push({title:type,href:'keyword-reference.html#type-'+type.toLowerCase(),kind:'Тип',category:'Типы данных',description:'Тип данных '+type+' и ссылка на его тематическое описание.',keywords:[type,'тип']});
for(const word of inventory.keywords)if(!inventory.statements.some(s=>s.name===word)&&!inventory.types.includes(word))index.push({title:word,href:'keyword-reference.html#kw-'+word.toLowerCase(),kind:'Ключевое слово',category:'Ключевые слова',description:'Часть конструкции языка '+word+'.',keywords:[word]});
for(const op of inventory.expressionOperators)index.push({title:'Операция '+op.name,href:op.href,kind:'Операция',category:'Выражения и операции',description:op.description,keywords:[op.name,op.token]});
const searchIndex=mergeSearchDestinations(index);
fs.writeFileSync(path.join(dir,'scripts/search-index.js'),'window.AxiomSearchIndex = '+JSON.stringify(searchIndex,null,2)+';\n');
const alphabet=[...inventory.functions.map(fn=>({name:fn.name,href:`function-reference.html#fn-${fn.name.toLowerCase()}`,kind:'Функция'})),...inventory.statements.map(op=>({name:op.name,href:op.href,kind:'Оператор'}))].sort((a,b)=>a.name.localeCompare(b.name,'en'));
const body=section('Алфавитный указатель','<table><thead><tr><th>Имя</th><th>Сущность</th></tr></thead><tbody>'+alphabet.map(e=>`<tr><td><a href="${e.href}">${escape(e.name)}</a></td><td>${e.kind}</td></tr>`).join('')+'</tbody></table>',['alphabet','alphabet-index','complete-reference'])+section('Тематические главы','<div class="link-list">'+chapters.filter(c=>c.number).map(c=>`<a href="${c.items[0].href}">${c.number}. ${escape(c.title)}</a>`).join('')+'</div>',['topics','reading-order'])+related;
fs.writeFileSync(path.join(dir,'reference.html'),manualPage({id:'reference',title:'Алфавитный указатель',lead:'Раздел содержит имена встроенных функций и операторов Axiom. Выберите имя для перехода к формам вызова или основной справочной статье.',eyebrow:'Справочный указатель',body}));
console.log(`Собраны 17 глав, ${searchIndex.length} уникальных целей поиска и алфавитный указатель. Объединено ${index.length-searchIndex.length} повторных записей.`);
