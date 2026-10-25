import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import crypto from 'node:crypto';
import { manualPage } from './manual-page.mjs';
import { formalParameterName } from './normalize-manual-syntax.mjs';
import { fileURLToPath } from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const manual=path.join(root,'docs/manual');
const dataPath=path.join(manual,'data/language-reference.json');
const arg=process.argv.indexOf('--inventory');
const previous=fs.existsSync(dataPath)?JSON.parse(fs.readFileSync(dataPath,'utf8')):null;
const actual=arg>=0?JSON.parse(fs.readFileSync(path.resolve(root,process.argv[arg+1]),'utf8')):null;
if(!actual && !previous) throw new Error('Укажите --inventory с результатом текущего реестра компилятора.');
const context={window:{}};
vm.runInNewContext(fs.readFileSync(path.join(manual,'scripts/search-index.js'),'utf8'),context);
const index=context.window.AxiomSearchIndex;
const escape=value=>String(value).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const plain=value=>value.replace(/<[^>]*>/g,'').trim();
const pages=new Map(fs.readdirSync(manual).filter(f=>f.endsWith('.html')&&!f.endsWith('-reference.html')).map(f=>[f,fs.readFileSync(path.join(manual,f),'utf8')]));
const sections=html=>[...html.matchAll(/<section\b[^>]*>[\s\S]*?<\/section>/g)].map(m=>m[0]);
const pattern=name=>new RegExp('\\b'+name+'\\b');
let functions;
if(actual) {
 const names=[...new Set(actual.Rows.map(r=>r.Name))].sort();
 functions=names.map(name=>{
   const old=previous?.functions.find(f=>f.name===name);
   const entry=index.find(e=>e.title===name||e.title==='Функция '+name)||index.find(e=>e.functions?.includes(name));
   if(!entry&&!old) throw new Error(`Нет описания ${name}`);
   const href=old?.sourceHref??entry.href;
   if(href.startsWith('function-reference.html')) throw new Error(`Не указан исходный раздел ${name}`);
   return {name,description:old?.description??entry.description,sourceHref:href,
     forms:actual.Rows.filter(r=>r.Name===name).map((r,i)=>({key:name+'/'+i,id:r.Id,parameters:r.Parameters.map(p=>({name:p.Name,type:p.Type})),returnType:r.ReturnType,binding:r.Binding,operandPolicy:r.OperandPolicy,resultPolicy:r.ResultPolicy}))};
 });
} else functions=previous.functions;
const explanations=JSON.parse(fs.readFileSync(path.join(manual,'data/function-explanations.json'),'utf8'));
for(const fn of functions){
 const entry=explanations[fn.name];
 if(!entry?.description)throw new Error(`Нет полного определения функции ${fn.name}.`);
 for(const form of fn.forms)for(const parameter of form.parameters)if(!entry.arguments?.[parameter.name])throw new Error(`Не объяснён аргумент ${fn.name}.${parameter.name}.`);
 fn.description=entry.description;
}

const statementRows=[
 ['DIM','Dim','dim.html'],['CONST','Const','const.html'],['TYPE','TypeDeclaration','type.html'],
 ['SUB','SubDeclaration','sub.html'],['FUNCTION','FunctionDeclaration','function.html'],
 ['IMPORT','ImportStatement','import.html'],
 ['PRINT','Print','print.html'],['PRINT USING','PrintUsing','print.html'],['INPUT','Input','input.html'],
 ['SLEEP','Sleep','sleep.html'],['RANDOMIZE','Randomize','randomize.html'],
 ['OPEN','Open','open.html'],['CLOSE','Close','close.html'],
 ['IF','If','if.html'],['SELECT CASE','SelectCase','select-case.html'],['FOR','For','for.html'],
 ['WHILE','While','while.html'],['DO ... LOOP','DoLoop','do-loop.html'],
 ['EXIT FOR','ExitFor','exit.html#exit-for'],['EXIT DO','ExitDo','exit.html#exit-do'],
 ['CONTINUE FOR','ContinueFor','continue.html'],['CONTINUE WHILE','ContinueWhile','continue.html'],['CONTINUE DO','ContinueDo','continue.html'],
 ['RETURN','Return','return.html'],['TRY','Try','try.html'],['ERROR','Error','error.html'],
 ['REDIM','Redim','redim.html'],['REDIM PRESERVE','Redim','redim.html'],['ERASE','Erase','erase.html'],
 ['SCREEN','Screen','screen.html'],['SMOOTH','Smooth','smooth.html'],['CANVAS','Canvas','modern-graphics.html'],['BEGINFRAME','BeginFrame','modern-graphics.html'],
 ['PRESENT','Present','modern-graphics.html'],['CAMERA','Camera','modern-graphics.html'],['VIEWPORT','Viewport','modern-graphics.html'],['RESETCAMERA','ResetCamera','modern-graphics.html'],
 ['VIEW','View','view.html'],['VIEW PRINT','ViewPrint','view.html#view-print'],['WINDOW','Window','window.html'],
 ['CLS','Cls','graphics-text.html#cls'],['COLOR','Color','graphics-text.html#color'],['LOCATE','Locate','graphics-text.html#locate'],
 ['PALETTE','Palette','palette.html'],['PCOPY','PCopy','pcopy.html'],['PSET','PSet','graphics-primitives.html#pset'],['PRESET','PSet','graphics-primitives.html#preset'],
 ['GET','Get','get-put.html'],['PUT','Put','get-put.html'],['LINE','Line','graphics-primitives.html#line'],
 ['CIRCLE','Circle','graphics-primitives.html#circle'],['PAINT','Paint','graphics-primitives.html#paint'],['DRAW','Draw','graphics-primitives.html#draw'],
 ['DRAWLINE','DrawLine','modern-graphics.html'],['DRAWCIRCLE','DrawCircle','modern-graphics.html'],['FILLCIRCLE','FillCircle','modern-graphics.html'],
 ['DRAWRECT','DrawRect','modern-graphics.html'],['FILLRECT','FillRect','modern-graphics.html'],['DRAWTEXT','DrawText','modern-graphics.html'],
 ['DRAWIMAGE','DrawImage','images.html#drawimage'],['DRAWSPRITE','DrawSprite','sprites.html#drawsprite'],
 ['DRAWTILE','DrawTile','tilesets.html'],['DRAWTILEMAP','DrawTilemap','tilemaps.html'],['DRAWTILELAYER','DrawTileLayer','tilemaps.html'],['SETMAPTILE','SetMapTile','tilemaps.html'],
 ['ON KEY','OnKey','on-key.html'],['ON MOUSE','OnMouse','on-mouse.html'],
 ['Присваивание','Assignment','assignment.html'],['Вызов как инструкция','Expression','calls.html']
];
const statements=statementRows.map(([name,syntax,href])=>({id:syntax.toLowerCase()+'-'+name.toLowerCase().replace(/[^a-zа-я]+/g,'-').replace(/^-|-$/g,''),name,syntax,href}));
const expressionOperators=[
 ['+','PlusToken','Сложение чисел или объединение двух строк.'],['-','MinusToken','Вычитание чисел; унарная форма меняет знак числа.'],
 ['*','StarToken','Умножение чисел.'],['/','SlashToken','Деление с результатом DOUBLE или DECIMAL по правилам типов.'],
 ['\\','BackslashToken','Целочисленное деление INTEGER или LONG.'],['MOD','ModKeyword','Остаток целочисленного деления.'],['^','CaretToken','Возведение в степень; результат DOUBLE, связывание справа налево.'],
 ['&','AmpersandToken','Объединение текстовых представлений скалярных значений.'],
 ['=','EqualsToken','Сравнение на равенство.'],['<>','NotEqualsToken','Сравнение на неравенство.'],['<','LessToken','Строго меньше.'],['<=','LessOrEqualsToken','Меньше или равно.'],['>','GreaterToken','Строго больше.'],['>=','GreaterOrEqualsToken','Больше или равно.'],
 ['<<','LessLessToken','Сдвиг целого числа влево.'],['>>','GreaterGreaterToken','Сдвиг целого числа вправо.'],
 ['NOT','NotKeyword','Логическое или побитовое отрицание.'],['AND','AndKeyword','Логическая конъюнкция или побитовое И.'],['ANDALSO','AndAlsoKeyword','Логическое И с коротким замыканием.'],
 ['XOR','XorKeyword','Логическое или побитовое исключающее ИЛИ.'],['OR','OrKeyword','Логическое или побитовое ИЛИ.'],['ORELSE','OrElseKeyword','Логическое ИЛИ с коротким замыканием.'],
 ['EQV','EqvKeyword','Логическая или побитовая эквивалентность.'],['IMP','ImpKeyword','Логическая или побитовая импликация.']
].map(([name,token,description])=>({name,token,description,href:'arithmetic-and-logic.html'}));
const sourceFiles=[...fs.readdirSync(path.join(root,'src/Axiom.Semantics/Symbols/Builtins')).filter(f=>f.endsWith('.cs')).map(f=>'src/Axiom.Semantics/Symbols/Builtins/'+f),
 'src/Axiom.Semantics/Symbols/BuiltinFunctionDescriptor.cs','src/Axiom.Core/AxiomLanguageKeywords.cs','src/Axiom.Lexing/Lexer.cs','src/Axiom.Lexing/TokenKind.cs','src/Axiom.Syntax/SyntaxKind.cs',
 'src/Axiom.Parsing/Parser.Statements.cs','src/Axiom.Parsing/Parser.Declarations.cs','src/Axiom.Parsing/Parser.Expressions.cs','src/Axiom.Parsing/Parser.Arrays.cs'].sort();
const sourceFingerprint=crypto.createHash('sha256').update(sourceFiles.map(f=>f+'\n'+fs.readFileSync(path.join(root,f),'utf8').replaceAll('\r\n','\n')).join('\n')).digest('hex');
if(!actual && previous.sourceFingerprint!==sourceFingerprint)throw new Error('Реестр или синтаксис изменились: сначала обновите --inventory.');
const snapshot={sourceFiles,sourceFingerprint,functions,statements,expressionOperators,keywords:actual?.Integrity.Keywords??previous.keywords,types:actual?.Integrity.TypeNames??previous.types,
 internalSyntax:{ModuleStatementSyntax:'Обёртка инструкции с PUBLIC или PRIVATE; модификаторы описаны в разделе видимости.',GlobalStatementSyntax:'Обёртка инструкции верхнего уровня; отдельного оператора GLOBAL нет.',LocalSubDeclarationStatementSyntax:'Локальное объявление SUB; описано вместе с SUB.',LocalFunctionDeclarationStatementSyntax:'Локальное объявление FUNCTION; описано вместе с FUNCTION.'}};
fs.mkdirSync(path.dirname(dataPath),{recursive:true});
fs.writeFileSync(dataPath,JSON.stringify(snapshot,null,2)+'\n');

const shell=(id,title,lead,body)=>manualPage({id,title,lead,body,eyebrow:'Справочный указатель'});

function parameterType(form,param) {
 if(form.binding==='Numeric' && ['PreserveNumericArguments','PromoteNumericArguments'].includes(form.operandPolicy)) return 'числовое выражение';
 if(form.binding==='Numeric' && form.operandPolicy==='ConvertNumericArgumentsToDouble') return 'числовое выражение; перед вычислением преобразуется в DOUBLE';
 if(form.binding==='Array') {
   if(['array','source','template','left','right'].includes(param.name))return 'массив T';
   if(param.name==='value')return 'T — тип элемента массива';
   if(param.name==='seed')return 'A — тип начального значения';
   if(param.name==='predicate')return 'FUNCTION(T) AS BOOLEAN';
   if(param.name==='selector')return 'FUNCTION(T) AS U';
   if(param.name==='reducer')return 'FUNCTION(A, T) AS A';
 }
 return arrayTypeNotation(param.type);
}
function arrayTypeNotation(type){
 return String(type||'').replace(/\b((?!FUNCTION\b)[A-Za-z_]\w*)\s*\((,*)\)/gi,(_,element,commas)=>`${element}[${commas}]`);
}
function returnType(form){return ({
 SameAsFirstArgument:form.binding==='Array'?'Массив того же типа и формы, если правила функции не изменяют его границы.':'Тип первого аргумента.',
 SameAsSecondArgument:'Массив с типом элементов исходного массива и формой массива-образца.',
 ElementTypeOfFirstArrayArgument:'T — тип элемента исходного массива.',
 ArrayOfFirstArrayElementType:'Одномерный массив элементов типа T.',
 ArrayDynamicSliceResult:'Для ранга 1 — элемент T; для ранга 2 и выше — массив T ранга на единицу меньше.',
 ArrayIndexCoordinateResult:'Для ранга 1 — INTEGER; для многомерного массива — INTEGER[] с координатами.',
 ArrayFindAllCoordinateResult:'Для ранга 1 — INTEGER[]; для многомерного массива — INTEGER[,] с координатами.',
 ArrayMapSelectorReturnArray:'Массив результатов типа U с рангом исходного массива.',
 ArrayReduceAccumulator:'A — тип начального значения аккумулятора.',
 PromotedNumeric:'Общий числовой тип аргументов.',
 Integer:'INTEGER',String:'STRING',Double:'DOUBLE'
})[form.resultPolicy]??(form.returnType==='VOID'?'Значение не возвращается; вызов используется как инструкция.':arrayTypeNotation(form.returnType));}


function argumentContract(form,param){
 const type=parameterType(form,param);
 const named={STRING:'строкой типа STRING',INTEGER:'целым числом типа INTEGER',LONG:'целым числом типа LONG',DOUBLE:'числом типа DOUBLE',DECIMAL:'десятичным числом типа DECIMAL',BOOLEAN:'логическим значением типа BOOLEAN',DATE:'датой типа DATE',TIME:'временем суток типа TIME',DATETIME:'датой и временем типа DATETIME',UTCDATETIME:'моментом времени типа UTCDATETIME',FILE:'значением открытого файла типа FILE',IMAGE:'изображением типа IMAGE',SPRITE:'спрайтом типа SPRITE',TILESET:'набором тайлов типа TILESET',TILEMAP:'тайловой картой типа TILEMAP'};
 if(named[type])return 'Значение аргумента должно быть '+named[type]+'.';
 if(type==='массив T')return 'Передаётся массив элементов типа T; требования к его рангу и состоянию определяются этой функцией.';
 if(type==='T — тип элемента массива')return 'Тип передаваемого значения должен совпадать с типом элемента массива.';
 if(type==='A — тип начального значения')return 'Передаётся начальное значение аккумулятора; его тип A определяет тип результата накопления.';
 if(type.startsWith('FUNCTION('))return 'Передаётся пользовательская функция с подписью '+type+'.';
 if(type.startsWith('числовое выражение'))return 'Передаётся числовое выражение. '+(type.includes('DOUBLE')?'Перед вычислением оно преобразуется в DOUBLE.':'Допустимые числовые типы и результат описаны ниже.');
 return 'Допустимый тип значения: '+type+'.';
}
function resultContract(form){
 const named={INTEGER:'целое число типа INTEGER',LONG:'целое число типа LONG',DOUBLE:'число типа DOUBLE',DECIMAL:'десятичное число типа DECIMAL',STRING:'строку типа STRING',BOOLEAN:'логическое значение типа BOOLEAN',DATE:'дату типа DATE',TIME:'время суток типа TIME',DATETIME:'дату и время типа DATETIME',UTCDATETIME:'момент времени типа UTCDATETIME',IMAGE:'изображение типа IMAGE',SPRITE:'спрайт типа SPRITE',TILESET:'набор тайлов типа TILESET',TILEMAP:'тайловую карту типа TILEMAP','STRING[]':'одномерный массив строк типа STRING[]','INTEGER[]':'одномерный массив целых чисел типа INTEGER[]','INTEGER[,]':'двумерный массив целых чисел типа INTEGER[,]'};
 if(form.returnType==='VOID')return 'Функция выполняет действие и не возвращает значение. Используйте вызов как отдельную инструкцию.';
 if(form.resultPolicy==='Default'&&named[arrayTypeNotation(form.returnType)])return 'Функция возвращает '+named[arrayTypeNotation(form.returnType)]+'.';
 const result=returnType(form);
 if(named[result])return 'Функция возвращает '+named[result]+'.';
 const policies={
   SameAsFirstArgument:form.binding==='Array'?'Функция возвращает массив с типом элементов исходного массива. Правила сохранения или изменения границ описаны в тематической статье этой функции.':'Функция возвращает значение того же числового типа, что и первый аргумент.',
   SameAsSecondArgument:'Функция возвращает массив с типом элементов исходного массива и рангом и границами массива-образца.',
   ElementTypeOfFirstArrayArgument:'Функция возвращает значение того же типа, что и элементы исходного массива.',
   ArrayOfFirstArrayElementType:'Функция возвращает одномерный массив с типом элементов исходного массива.',
   ArrayDynamicSliceResult:'Для одномерного массива функция возвращает элемент его типа. Для массива ранга 2 или выше функция возвращает массив с тем же типом элементов и рангом на единицу меньше.',
   ArrayIndexCoordinateResult:'Для одномерного массива функция возвращает индекс типа INTEGER. Для многомерного массива функция возвращает одномерный массив INTEGER[] с координатами найденного элемента.',
   ArrayFindAllCoordinateResult:'Для одномерного массива функция возвращает массив индексов INTEGER[]. Для многомерного массива функция возвращает двумерный массив INTEGER[,] с координатами найденных элементов.',
   ArrayMapSelectorReturnArray:'Функция возвращает массив с типом элементов U, который задаётся результатом переданной функции преобразования. Ранг и границы совпадают с исходным массивом.',
   ArrayReduceAccumulator:'Функция возвращает значение типа A, заданного начальным значением аккумулятора.',
   PromotedNumeric:'Функция возвращает значение общего числового типа своих аргументов.'
 };
 if(policies[form.resultPolicy])return policies[form.resultPolicy];
 throw new Error(`Не объяснён результат формы ${form.key}: ${result}`);
}

let body=`<section class="article-section"><h2>Содержание</h2><p>В указателе представлены ${functions.length} имя встроенных функций и ${functions.reduce((n,f)=>n+f.forms.length,0)} зарегистрированных форм вызова. Каждая форма показана отдельно; все перечисленные в ней аргументы обязательны. Необязательный аргумент представлен отдельной более короткой формой.</p><p><code>T</code> обозначает тип элемента массива, <code>U</code> — тип результата преобразования, <code>A</code> — тип аккумулятора. Это справочные обозначения. Допустимые числовые типы: <code>INTEGER</code>, <code>LONG</code>, <code>DOUBLE</code>, <code>DECIMAL</code>. Ограничения конкретной функции имеют приоритет над общей категорией типа.</p><div class="link-list">${functions.map(f=>`<a href="#fn-${f.name.toLowerCase()}">${f.name}</a>`).join(' ')}</div></section>`;
for(const fn of functions) {
 const samples=path.join(root,'samples/functions',fn.name.toLowerCase());
 const files=fs.existsSync(samples)?fs.readdirSync(samples).filter(f=>f.endsWith('.axm')):[];
 body+=`\n<section class="article-section" id="fn-${fn.name.toLowerCase()}"><h2>${fn.name}</h2><p><code>${fn.name}</code> ${escape(fn.description[0].toLowerCase()+fn.description.slice(1))}</p><h3>Синтаксис, аргументы и результат</h3>`;
 for(const form of fn.forms) {
   body+=`<div class="reference-form" data-form="${form.key}"><pre><code class="language-axiom">${fn.name}(${form.parameters.map(p=>'&lt;'+formalParameterName(p.name)+'&gt;').join(', ')})</code></pre>`;
   body+=form.parameters.length?`<table><thead><tr><th>Аргумент</th><th>Что передать</th></tr></thead><tbody>${form.parameters.map(p=>`<tr><td><code>&lt;${escape(formalParameterName(p.name))}&gt;</code></td><td><p>${escape(argumentContract(form,p))}</p><p>${escape(explanations[fn.name].arguments[p.name])}</p></td></tr>`).join('')}</tbody></table>`:'<p>Функция вызывается без аргументов.</p>';
   body+=`<p><strong>Результат.</strong> ${escape(resultContract(form))}</p></div>`;
 }
 body+=`<p><a href="${fn.sourceHref}">Описание ${fn.name}: поведение, ограничения и примеры</a></p>`;
 if(files.length)body+=`<h3>Запускаемые примеры</h3><ul>${files.map(f=>`<li><a href="../../samples/functions/${fn.name.toLowerCase()}/${escape(f)}">${escape(f)}</a></li>`).join('')}</ul>`;
 body+='</section>';
}
fs.writeFileSync(path.join(manual,'function-reference.html'),shell('function-reference','Встроенные функции','Здесь собраны встроенные функции Axiom и точные зарегистрированные формы их вызова: имя, аргументы, результат и ссылка на тематическую статью, где описаны поведение, ограничения и примеры.',body));

body='<section class="article-section"><h2>Содержание</h2><p>Операторы сгруппированы по роли в программе. В перечень включены объявления, управляющие конструкции, ввод и вывод, графика, события, присваивание и вызов как инструкция. Слова завершения блоков и режимы описываются вместе с соответствующим оператором.</p><div class="link-list">'+statements.map(s=>`<a href="#op-${s.id}">${escape(s.name)}</a>`).join(' ')+'</div></section>';
for(const op of statements){
 const [file]=op.href.split('#');const html=pages.get(file);if(!html)throw new Error(`Нет страницы ${op.name}: ${file}`);
 const lead=html.match(/<p class="lead">([\s\S]*?)<\/p>/)?.[1]??'';
 body+=`<section class="article-section" id="op-${op.id}"><h2>${escape(op.name)}</h2><p>${lead}</p><p><a href="${op.href}">Описание ${escape(op.name)}: синтаксис, параметры и примеры</a></p>`;
 body+='</section>';
}
fs.writeFileSync(path.join(manual,'operator-reference.html'),shell('operator-reference','Операторы и инструкции','Здесь собраны объявления, инструкции и управляющие конструкции Axiom с точной формой записи; каждая запись ведёт к основному описанию соответствующей формы.',body));
const definitions={
 IMPORT:['Подключает открытые объявления другого исходного модуля.','import.html'],
 PUBLIC:['Открывает объявление для импортирующих модулей.','visibility.html'],
 PRIVATE:['Оставляет объявление доступным только внутри текущего модуля.','visibility.html'],
 AS:['Обозначает тип переменной, поля, параметра или результата; в OPEN связывает файл с переменной.','dim.html'],
 APPEND:['Режим OPEN для добавления текста в конец файла.','open.html'],OUTPUT:['Режим OPEN для записи текстового файла.','open.html'],
 BYREF:['Передача параметра по ссылке на изменяемое место хранения.','sub.html'],BYVAL:['Передача параметра по значению.','sub.html'],
 CASE:['Ветвь SELECT CASE.','select-case.html'],CATCH:['Перехват ошибки в блоке TRY.','try.html'],FINALLY:['Завершающий блок обработки TRY.','try.html'],
 ELSE:['Альтернативная ветвь IF или CASE ELSE.','if.html'],ELSEIF:['Дополнительное условие в блоке IF.','if.html'],THEN:['Отделяет условие IF от выполняемых инструкций.','if.html'],
 END:['Первая часть маркера завершения блока: END IF, END SELECT, END WHILE, END TRY, END SUB, END FUNCTION, END TYPE.','getting-started.html'],
 FALSE:['Логический литерал ложного значения BOOLEAN.','boolean.html#values'],TRUE:['Логический литерал истинного значения BOOLEAN.','boolean.html#values'],
 IS:['Сравнение в условии CASE IS.','select-case.html'],KEY:['Обозначение клавиатурного события в ON KEY.','on-key.html'],MOUSE:['Обозначение события мыши в ON MOUSE.','on-mouse.html'],
 LOOP:['Завершает блок DO; может содержать WHILE или UNTIL.','do-loop.html'],NEXT:['Завершает тело FOR и переходит к следующей итерации.','for.html'],
 EACH:['Вводит обход массива в заголовке FOR EACH. Зарезервировано: как имя не используется.','for-each.html'],IN:['Отделяет переменную цикла от массива-источника в заголовке FOR EACH. Зарезервировано: как имя не используется.','for-each.html'],
 OFF:['Отключает регистрацию обработчика ON KEY или ON MOUSE; в SMOOTH OFF отключает сглаживание готового кадра.','keyboard-mouse-events.html'],
 ON:['Начинает регистрацию событий ON KEY/ON MOUSE, после запятой возобновляет зарегистрированный обработчик; в SMOOTH ON включает сглаживание готового кадра.','keyboard-mouse-events.html'],
 PRESERVE:['Сохранение пересечения прежних и новых границ при REDIM.','redim.html'],SELECT:['Начинает оператор SELECT CASE.','select-case.html'],
 SELF:['Текущая запись TYPE внутри её метода.','type.html'],STEP:['Шаг FOR либо относительные координаты в поддерживающих эту форму графических операторах.','for.html'],
 STOP:['Пауза обработчика в ON KEY или ON MOUSE; самостоятельного оператора STOP нет.','keyboard-mouse-events.html'],
 TO:['Разделитель границ в FOR, измерениях и срезах массивов, диапазонах CASE.','arrays.html'],
 TRANSPARENT:['Прозрачный фон экранного текста в форме COLOR foreground, TRANSPARENT.','graphics-text.html#color'],UNTIL:['Условие завершения DO ... LOOP.','do-loop.html'],
 USING:['Формат PRINT USING либо кодировка OPEN ... USING.','print.html']
};
for(const op of statements){const word=op.name.split(' ')[0];if(/^[A-Z]+$/.test(word)&&!definitions[word])definitions[word]=['Начинает конструкцию '+op.name+'. Правила, параметры и формы записи определены в полной статье.',op.href];}
for(const op of expressionOperators)if(/^[A-Z]+$/.test(op.name))definitions[op.name]=[op.description,op.href];
const typeDescriptions={INTEGER:'32-битное целое число.',LONG:'64-битное целое число.',DOUBLE:'Вещественное число двойной точности.',DECIMAL:'Десятичное число.',BOOLEAN:'Логическое значение TRUE или FALSE.',STRING:'Строка; поддерживается фиксированная форма STRING * N.',DATE:'Дата.',TIME:'Время суток.',DATETIME:'Дата и время.',UTCDATETIME:'Момент времени на шкале UTC.',FILE:'Значение открытого текстового файла.',IMAGE:'Изображение.',SPRITE:'Набор кадров спрайта.',TILESET:'Набор графических тайлов.',TILEMAP:'Тайловая карта.'};
const typeHref=name=>({STRING:'data-types-strings-and-files.html',DATE:'date-time-functions.html',TIME:'date-time-functions.html',DATETIME:'date-time-functions.html',UTCDATETIME:'date-time-functions.html',FILE:'file-system-functions.html',IMAGE:'images.html',SPRITE:'sprites.html',TILESET:'tilesets.html',TILEMAP:'tilemaps.html',BOOLEAN:'boolean.html'})[name]??'numeric-functions.html';
body='<section class="article-section"><h2>Ключевые слова</h2><p>Ключевые слова и имена встроенных типов не зависят от регистра. В справочнике используется запись заглавными буквами. Слова завершения блоков, режимы и модификаторы являются частями соответствующих конструкций.</p><table><thead><tr><th>Слово</th><th>Назначение</th><th>Описание</th></tr></thead><tbody>';
for(const word of snapshot.keywords){if(!definitions[word])throw new Error('Нет определения ключевого слова '+word);const [text,href]=definitions[word];body+=`<tr id="kw-${word.toLowerCase()}"><td><code>${word}</code></td><td>${escape(text)}</td><td><a href="${href}">${word}</a></td></tr>`;}
body+='</tbody></table></section><section class="article-section"><h2>Имена встроенных типов</h2><table><thead><tr><th>Тип</th><th>Назначение</th></tr></thead><tbody>';
for(const name of snapshot.types){if(!typeDescriptions[name])throw new Error('Нет определения типа '+name);body+=`<tr id="type-${name.toLowerCase()}"><td><a href="${typeHref(name)}"><code>${name}</code></a></td><td>${escape(typeDescriptions[name])}</td></tr>`;}
body+='</tbody></table></section><section class="article-section"><h2>Операции в выражениях</h2><p>Унарное отрицание числа и операция NOT принимают один операнд. Остальные перечисленные формы принимают два операнда. Приоритет, допустимые типы и ограничения определены в разделе выражений.</p><table><thead><tr><th>Знак</th><th>Назначение</th></tr></thead><tbody>';
for(const op of expressionOperators)body+=`<tr id="expr-${op.token.toLowerCase()}"><td><a href="${op.href}"><code>${escape(op.name)}</code></a></td><td>${escape(op.description)}</td></tr>`;
body+='</tbody></table></section>';
fs.writeFileSync(path.join(manual,'keyword-reference.html'),shell('keyword-reference','Ключевые слова, типы и операции','Здесь собраны ключевые слова и имена встроенных типов Axiom, а также операции, допустимые в выражениях; каждый элемент связан с подробным описанием соответствующей конструкции.',body));
console.log(`Сформированы карточки ${functions.length} функций, ${functions.reduce((n,f)=>n+f.forms.length,0)} форм вызова и ${statements.length} операторов и форм инструкций.`);
