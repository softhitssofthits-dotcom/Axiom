const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const context = { window: {} };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/lang-features.js'), 'utf8'), context);
const signature = context.window.LangFeatures.builtinSignature;

test('Квадратные объявления сохраняют тип, ранг, параметры и массивный результат', () => {
  const source = 'DIM values[] AS STRING\nDIM grid[1 TO 2, Choose(1, 2) TO 3] AS INTEGER\nDIM inferred[] = Make()\nDIM legacy(1, 2) AS INTEGER\nDIM oldType AS STRING ()\nFUNCTION Make(BYREF rows[,] AS INTEGER, names[] AS STRING) AS STRING[]\nEND FUNCTION\nSUB Work(callback AS FUNCTION(INTEGER[,], STRING[]) AS STRING[], count AS INTEGER)\nEND SUB\nTYPE Box\nitems[] AS STRING\nEND TYPE';
  const doc = context.window.LangFeatures.scanDoc({getValue: () => source});
  assert.equal(doc.vars[0].type, 'STRING[]'); assert.equal(doc.vars[0].arrayRank, 1);
  assert.equal(doc.vars[1].type, 'INTEGER[,]'); assert.equal(doc.vars[1].arrayRank, 2);
  assert.equal(doc.vars[2].type, null); assert.equal(doc.vars[2].arrayRank, 1);
  assert.equal(doc.vars[3].type, 'INTEGER(,)'); assert.equal(doc.vars[4].type, 'STRING()');
  assert.equal(doc.functions[0].returnType, 'STRING[]');
  assert.equal(doc.functions[0].params.length, 2);
  assert.equal(doc.functions[0].params[0].name, 'rows'); assert.equal(doc.functions[0].params[0].type, 'INTEGER[,]');
  assert.equal(doc.subs[0].params.length, 2);
  assert.equal(doc.subs[0].params[0].type, 'FUNCTION(INTEGER[,], STRING[]) AS STRING[]');
  assert.equal(doc.types[0].fields[0].type, 'STRING[]');
});

test('Список DIM отделяет объявления от запятых ранга, вызовов и строк', () => {
  const source = 'DIM grid[,] AS INTEGER, names[] AS STRING = SPLIT("а,б", ","), value AS INTEGER';
  const doc = context.window.LangFeatures.scanDoc({getValue: () => source});
  assert.equal(doc.vars.length, 3);
  assert.equal(doc.vars[0].type, 'INTEGER[,]'); assert.equal(doc.vars[1].type, 'STRING[]'); assert.equal(doc.vars[2].type, 'INTEGER');
});

test('Неполное квадратное объявление не получает выдуманный тип или ранг', () => {
  const doc = context.window.LangFeatures.scanDoc({getValue: () => 'DIM broken[1,\nPRINT broken'});
  assert.equal(doc.vars[0].type, null); assert.equal(doc.vars[0].arrayRank, 0);
});

test('Дополнение массивного имени вставляет [] без дублирования готовых скобок', () => {
  const p = providers('DIM values[] AS STRING\nval');
  const item = () => p.features._completionProvider.provideCompletionItems(p.model, {lineNumber: 2, column: 4}).suggestions.find(s => s.label === 'values');
  assert.equal(item().insertText, 'values[]'); assert.equal(item().detail, 'STRING[]');
  p.edit('DIM values[] AS STRING\nval[2]'); assert.equal(item().insertText, 'values');
});

for (const [source, expected, parameter] of [
  ['LEFT(grid[1, 2], ', 'LEFT', 1],
  ['LEFT(grid[1, ', 'LEFT', 0],
  ['LEFT(grid[LEN(', 'LEN', 0],
  ['LEFT(grid[(1 + 2), 3], ', 'LEFT', 1],
  ['LEFT(Make()[1, 2], ', 'LEFT', 1],
  ['LEFT(grid[other[1, 2], 3], ', 'LEFT', 1],
  ['PRINT grid[1, ', null, 0],
  ['PRINT LEN[1, ', null, 0],
  ['PRINT $"Значение {LEFT(grid[1, 2], ', 'LEFT', 1],
  ['LEFT("значение", $"{grid[1, 2]}", ', 'LEFT', 1],
]) test('Подпись отличает [] от вызова: ' + source, async () => {
  const p = providers(source);
  const help = (await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1})).value;
  assert.equal(help.signatures.length > 0, !!expected);
  if (expected) { assert.ok(help.signatures[0].label.startsWith(expected + '(')); assert.equal(help.activeParameter, parameter); }
});

test('Старое объявление распознаётся без принудительной вставки нового синтаксиса', () => {
  const p = providers('DIM legacy(1 TO 3) AS STRING\nleg');
  const item = p.features._completionProvider.provideCompletionItems(p.model, {lineNumber: 2, column: 4}).suggestions.find(s => s.label === 'legacy');
  assert.equal(item.insertText, 'legacy');
});

test('Массивные параметры резервной подписи разделяются по верхнему уровню', async () => {
  const source = 'SUB Show(values[,] AS INTEGER, count AS INTEGER)\nEND SUB\nShow(grid[1, 2], ';
  const p = providers(source); delete p.ide.documentServices;
  const help = (await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 3, column: 'Show(grid[1, 2], '.length + 1})).value;
  assert.equal(help.activeParameter, 1); assert.equal(help.signatures[0].parameters.length, 2);
  assert.equal(help.signatures[0].parameters[0].label, 'values[,] AS INTEGER');
});

test('Запятые квадратного индекса не смещают третий аргумент функции', async () => {
  const source = 'RGB(grid[1, 2], 3, ';
  const p = providers(source);
  p.ide.langMeta.builtins.push({name: 'RGB', params: [{name: 'red'}, {name: 'green'}, {name: 'blue'}]});
  const help = (await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1})).value;
  assert.equal(help.activeParameter, 2);
});

for (const action of ['edit', 'cancel', 'dispose']) test('Квадратная подпись не применяет поздний семантический ответ: ' + action, async () => {
  const source = 'Show(grid[1, 2], ', p = providers(source);
  let resolve;
  p.ide.documentServices.request = () => new Promise(done => {resolve = done;});
  const cancellation = {isCancellationRequested: false};
  const request = p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1}, cancellation);
  if (action === 'edit') p.edit('PRINT 1');
  else if (action === 'cancel') cancellation.isCancellationRequested = true;
  else p.model.isDisposed = () => true;
  resolve({hovers: [{start: 0, length: 4, kind: 'sub', signature: 'SUB Show(values[,] AS INTEGER, count AS INTEGER)', parameters: ['values[,] AS INTEGER', 'count AS INTEGER']}]});
  assert.equal((await request).value.signatures.length, 0);
});

test('Функция с массивным результатом остаётся функцией, а не массивом', async () => {
  const p = providers('PRINT Make()', [{start: 6, length: 4, kind: 'function', type: 'STRING[]', arrayRank: 1,
    elementType: 'STRING', signature: 'FUNCTION Make() AS STRING[]', parameters: []}]);
  const text = (await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: 7})).contents[0].value;
  assert.match(text, /— функция/); assert.doesNotMatch(text, /— массив/);
});

test('Группировка внутри индекса не скрывает внешнюю функцию', async () => {
  const source = 'LEFT(grid[(1 + ';
  const p = providers(source);
  const help = (await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1})).value;
  assert.ok(help.signatures[0].label.startsWith('LEFT(')); assert.equal(help.activeParameter, 0);
});

test('Квадратный тип в семантической карточке сохраняет категорию массива на обоих языках', async () => {
  const p = providers('PRINT grid[1, 2]', [{start: 6, length: 4, kind: 'variable', type: 'INTEGER[,]'}]);
  for (const locale of ['ru', 'en']) {
    p.language(locale);
    const text = (await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: 7})).contents[0].value;
    assert.match(text, /INTEGER\[,\]/); assert.match(text, locale === 'ru' ? /Измерений: 2/ : /Dimensions: 2/);
  }
});

test('Массив в подсказке назван массивом, показаны тип элементов и число измерений', async () => {
  const p = providers('PRINT grades', [{start: 6, length: 6, name: 'grades', kind: 'variable', type: 'INTEGER(,)'}]);
  const hover = () => p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: 7});
  assert.match((await hover()).contents[0].value, /массив/);
  assert.match((await hover()).contents[0].value, /Тип элементов: `INTEGER`/);
  assert.match((await hover()).contents[0].value, /Измерений: 2/);
  p.language('en');
  assert.match((await hover()).contents[0].value, /array/);
  assert.match((await hover()).contents[0].value, /Element type: `INTEGER`/);
  assert.match((await hover()).contents[0].value, /Dimensions: 2/);
});

for (const declaration of ['DIM fruits[] = SPLIT("яблоко,банан", ",")', 'DIM fruits[] AS STRING = SPLIT("яблоко,банан", ",")']) {
  test('Подсказка массивного инициализатора сохраняет тип массива: ' + declaration, async () => {
    const p = providers(declaration, [{ start: 4, length: 6, name: 'fruits', kind: 'variable', type: 'STRING[]', arrayRank: 1 }]);
    const hover = await p.features._hoverProvider.provideHover(p.model, { lineNumber: 1, column: 5 });
    assert.match(hover.contents[0].value, /Тип: `STRING\[\]`/);
  });
}

test('Семантическая подсказка обновляется после правки зависимости при неизменном основном тексте',async()=>{
 const p=providers('PRINT value');let revision=0,calls=0;
 p.ide.openFiles.captureProject=()=>{const captured=revision;return {isCurrent:()=>captured===revision};};
 p.ide.documentServices.request=async()=>{calls++;return {hovers:[{start:6,length:5,name:'value',kind:'variable',type:revision?'STRING':'INTEGER'}]};};
 const hover=()=>p.features._hoverProvider.provideHover(p.model,{lineNumber:1,column:7});
 assert.match((await hover()).contents[0].value,/INTEGER/);
 revision++;assert.match((await hover()).contents[0].value,/STRING/);assert.equal(calls,2);
});

test('Переименование основного файла обновляет подсказку без изменения текста',async()=>{
 const p=providers('PRINT value');let calls=0;
 p.ide.documentServices.request=async()=>({hovers:[{start:6,length:5,name:'value',kind:'variable',type:++calls===1?'INTEGER':'STRING'}]});
 const hover=()=>p.features._hoverProvider.provideHover(p.model,{lineNumber:1,column:7});
 assert.match((await hover()).contents[0].value,/INTEGER/);
 const entry=p.ide.openFiles.get('test');p.ide.openFiles.delete('test');p.ide.openFiles.set('renamed',entry);
 assert.match((await hover()).contents[0].value,/STRING/);
});

test('Открытые и закрытые объявления сохраняются в обзоре документа', () => {
  const doc=context.window.LangFeatures.scanDoc({getValue:()=>
    'IMPORT tools\nPUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION\nPRIVATE SUB Work()\nEND SUB\nPUBLIC TYPE Box\nItem AS INTEGER\nEND TYPE\nPRIVATE DIM count AS INTEGER'});
  assert.equal(doc.functions[0]?.name,'Answer');
  assert.equal(doc.functions[0]?.endLineNo,4);
  assert.equal(doc.subs[0]?.name,'Work');
  assert.equal(doc.types[0]?.name,'Box');
  assert.equal(doc.vars[0]?.name,'count');
});

test('Заготовка однострочного IF не добавляет END IF и переводится', () => {
  const p=providers('');
  const suggestion=()=>p.features._completionProvider.provideCompletionItems(p.model,{lineNumber:1,column:1}).suggestions.find(s=>s.insertText.startsWith('IF ')&&!s.insertText.includes('\n'));
  assert.equal(suggestion().insertText,'IF ${1:condition} THEN ${2:PRINT "yes"}$0');
  assert.equal(suggestion().label,'IF — однострочный');
  p.language('en');
  assert.equal(suggestion().label,'IF — single line');
});

test('Области TYPE и вложенной функции включают тела и имеют различимые пути', () => {
  const doc=context.window.LangFeatures.scanDoc({getValue:()=>
    'TYPE Counter\nvalue AS INTEGER\nFUNCTION Read() AS INTEGER\nRETURN SELF.value\nEND FUNCTION\nEND TYPE\nFUNCTION Outer() AS INTEGER\nFUNCTION Read() AS INTEGER\nRETURN 2\nEND FUNCTION\nRETURN Read()\nEND FUNCTION\n'});
  assert.equal(doc.types[0].endLineNo,6);
  assert.equal(doc.functions.find(f=>f.name==='Outer').endLineNo,12);
  assert.equal(doc.functions[0].scope.join('.'),'Counter');
  assert.equal(doc.functions.find(f=>f.lineNo===8).scope.join('.'),'Outer');
});

test('Разделы: вложенность, одинаковые названия, CRLF и игнорирование меток в строках и подпрограммах', () => {
  const source = ["PRINT \"' #region Ложный\"", "' #ReGiOn Игра", "SUB Move()", "' #region Внутренний", 'END SUB',
    "' #region Вложенный", 'FUNCTION Read() AS INTEGER', 'RETURN 1', 'END FUNCTION', "' #endregion", "' #endregion",
    "' #region Игра", 'TYPE Item', 'x AS INTEGER', 'END TYPE', "' #endregion"].join('\r\n');
  const doc = context.window.LangFeatures.scanDoc({getValue:()=>source});
  assert.equal(doc.regions.length,3);
  assert.equal(doc.regions[1].parentId,doc.regions[0].id);
  assert.notEqual(doc.regions[0].id,doc.regions[2].id);
  assert.equal(doc.subs[0].regionId,doc.regions[0].id);
  assert.equal(doc.functions[0].regionId,doc.regions[1].id);
  assert.equal(doc.types[0].regionId,doc.regions[2].id);
  assert.equal(doc.regions[0].endLineNo,11);
  assert.equal(doc.regionWarnings.length,0);
  const shifted = context.window.LangFeatures.scanDoc({getValue:()=>"' Начало\r\n"+source});
  assert.equal(shifted.regions[0].id,doc.regions[0].id,'Правка строк не меняет ключ состояния');
});

test('Неверные границы разделов не скрывают объявления и дают отдельные предупреждения панели', () => {
  const doc=context.window.LangFeatures.scanDoc({getValue:()=>"' #endregion\n' #region\n' #region Открытый\nSUB Work()\nEND SUB\n"});
  assert.equal(doc.regions[0].closed,false);
  assert.equal(doc.regions[0].endLineNo,6);
  assert.equal(doc.subs[0].regionId,doc.regions[0].id);
  assert.equal(doc.regionWarnings.map(w=>w.kind).join(','),'orphan,unnamed,unclosed');
});

test('Заготовка раздела вставляет пару комментариев с локализованным названием', () => {
  const p=providers('');
  const suggestion=()=>p.features._completionProvider.provideCompletionItems(p.model,{lineNumber:1,column:1}).suggestions.find(s=>s.label==='#region');
  assert.equal(suggestion().insertText,"' #region ${1:Раздел}\n$0\n' #endregion");
  p.language('en');
  assert.equal(suggestion().insertText,"' #region ${1:Section}\n$0\n' #endregion");
});

test('Границы объявлений исключают промежутки, строки и комментарии; сохраняется пояснение BYREF', () => {
  const doc = context.window.LangFeatures.scanDoc({getValue: () =>
    "SUB Move(BYREF value AS INTEGER) ' Двигает героя\r\nPRINT \"END SUB\"\r\n' END SUB\r\nEND SUB\r\n\r\nFUNCTION Read() AS INTEGER\r\nRETURN 1\r\nEND FUNCTION\r\n\r\n"});
  assert.equal(doc.subs[0].endLineNo, 4);
  assert.equal(doc.subs[0].description, 'Двигает героя');
  assert.equal(doc.subs[0].signature, 'SUB Move(BYREF value AS INTEGER)');
  assert.equal(doc.functions[0].lineNo, 6);
  assert.equal(doc.functions[0].endLineNo, 8);
});

test('Незавершённое объявление ограничивается следующим объявлением или концом документа', () => {
  const doc = context.window.LangFeatures.scanDoc({getValue: () => 'SUB First()\nPRINT 1\nFUNCTION Last() AS INTEGER\nRETURN 2'});
  assert.equal(doc.subs[0].endLineNo, 2);
  assert.equal(doc.functions[0].endLineNo, 4);
});

test('Полиморфная сумма отображается как скаляр типа элементов', () => {
  assert.equal(signature({name: 'ARRAYSUM', params: [{name: 'array', type: 'INTEGER()', displayType: 'T()'}],
    returnType: 'INTEGER', displayReturnType: 'T'}), 'ARRAYSUM(array AS T()) AS T');
});
test('Сохраняется совместимость с прежними метаданными', () => {
  assert.equal(signature({name: 'LEN', params: [{name: 'text', type: 'STRING'}], returnType: 'INTEGER'}),
    'LEN(text AS STRING) AS INTEGER');
});
test('Измерение массива присутствует в подписи', () => {
  assert.equal(signature({name: 'LBOUND', params: [{name: 'array', displayType: 'T()'}, {name: 'dimension', type: 'INTEGER'}],
    returnType: 'INTEGER'}), 'LBOUND(array AS T(), dimension AS INTEGER) AS INTEGER');
});

test('Тип переменной DECIMAL сохраняется в разборе объявления', () => {
  const doc = context.window.LangFeatures.scanDoc({getValue: () => 'DIM price AS DECIMAL = -10.5'});
  assert.equal(doc.vars[0].type, 'DECIMAL');
});

function providers(source, hovers = []) {
  let version = 1, requests = 0, markers = [];
  const model = {
    getLanguageId: () => 'axiom', uri: 'test', getValue: () => source, getVersionId: () => version,
    getLineContent: n => source.split('\n')[n - 1],
    getOffsetAt: p => source.split('\n').slice(0, p.lineNumber - 1).reduce((n, line) => n + line.length + 1, 0) + p.column - 1,
    getWordAtPosition(p) {
      for (const match of this.getLineContent(p.lineNumber).matchAll(/[A-Za-z_]\w*/g))
        if (p.column - 1 >= match.index && p.column - 1 < match.index + match[0].length)
          return {word: match[0], startColumn: match.index + 1, endColumn: match.index + match[0].length + 1};
      return null;
    },
    getWordUntilPosition(p) {
      const prefix = this.getLineContent(p.lineNumber).slice(0, p.column - 1);
      const match = prefix.match(/[A-Za-z_]\w*$/);
      const startColumn = match ? p.column - match[0].length : p.column;
      return {word: match?.[0] || '', startColumn, endColumn: p.column};
    },
    getValueInRange: r => model.getLineContent(r.startLineNumber).slice(r.startColumn - 1, r.endColumn - 1),
  };
  const runtime = {window: {}, document: {querySelectorAll: () => []}};
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/semantic-colors.js'), 'utf8'), runtime);
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/locale.js'), 'utf8'), runtime);
  vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/lang-features.js'), 'utf8'), runtime);
  const features = runtime.window.LangFeatures;
  const meta = {keywords: ['PRINT', 'SMOOTH'], typeNames: ['DECIMAL'], builtins: [
    {name: 'LEN', params: [{name: 'text', type: 'STRING'}], returnType: 'INTEGER'},
    {name: 'LEN', params: [{name: 'text', type: 'STRING'}, {name: 'unit', type: 'STRING'}], returnType: 'INTEGER'},
    {name: 'LEFT', params: [{name: 'text', type: 'STRING'}, {name: 'count', type: 'INTEGER'}], returnType: 'STRING'},
  ]};
  const ide = {langMeta: meta, openFiles: new Map([['test', {model}]]), documentServices: {
    async request() { requests++; return {hovers}; }
  }};
  features.register({languages: {CompletionItemKind: {}, CompletionItemInsertTextRule: {},
    registerCompletionItemProvider() {}, registerSignatureHelpProvider() {}, registerHoverProvider() {}},
    editor: {getModelMarkers: () => markers}, MarkerSeverity: {Error: 8}}, ide);
  return {model, features, ide, requests: () => requests, edit: value => {source = value; version++;},
    markers: value => {markers = value;}, language: locale => vm.runInNewContext(`I18N.setLocale('${locale}')`, runtime)};
}

test('Ключевое слово SMOOTH доступно в редакторе как оператор, а не имя программы', async () => {
  const p = providers('SMOOTH ON');
  const hover = await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: 2});
  assert.match(hover.contents[0].value, /ключевое слово/);
  const suggestions = p.features._completionProvider.provideCompletionItems(p.model, {lineNumber: 1, column: 8}).suggestions;
  assert.ok(suggestions.some(item => item.label === 'SMOOTH'));
});

test('Подсказка использует тип компилятора и обновляет кеш после правки', async () => {
  const symbols = [{start: 6, length: 5, name: 'price', kind: 'variable', type: 'DECIMAL'}];
  const p = providers('PRINT price', symbols);
  const hover = () => p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: 7});
  assert.match((await hover()).contents[0].value, /Тип: `DECIMAL`/);
  await hover(); assert.equal(p.requests(), 1);
  p.edit('PRINT price'); symbols[0].type = 'STRING';
  assert.match((await hover()).contents[0].value, /Тип: `STRING`/);
  assert.equal(p.requests(), 2);
  p.language('en'); assert.match((await hover()).contents[0].value, /Type: `STRING`/);
});

test('Строки, REM и апострофные комментарии не показывают языковые подсказки', async () => {
  for (const [source, column] of [['PRINT "LEN"', 8], ["PRINT 1 ' LEN", 11], ['REM LEN', 5], ['PRINT """LEN"""', 10]]) {
    const p = providers(source);
    assert.equal(await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column}), null, source);
  }
});

test('Интерполяция после Unicode-текста показывает символ, её формат скрыт', async () => {
  const source = 'PRINT $"😀 Цена {price:DECIMAL}"';
  const start = source.indexOf('price');
  const p = providers(source, [{start, length: 5, name: 'price', kind: 'variable', type: 'DECIMAL'}]);
  assert.match((await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: start + 1})).contents[0].value, /DECIMAL/);
  assert.equal(await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column: source.indexOf(':') + 2}), null);
});

test('Встроенная функция показывает все формы, типы и слова получают свою категорию', async () => {
  const p = providers('PRINT LEN("x") + DECIMAL');
  const hover = column => p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column});
  const text = (await hover(7)).contents[0].value;
  assert.match(text, /LEN\(text AS STRING\)/);
  assert.match(text, /LEN\(text AS STRING, unit AS STRING\)/);
  assert.match((await hover(1)).contents[0].value, /ключевое слово/);
  assert.match((await hover(18)).contents[0].value, /тип данных/);
});

test('Диагностика и отмена исключают обычную карточку символа', async () => {
  const p = providers('PRINT price', [{start: 6, length: 5, name: 'price', kind: 'variable', type: 'DECIMAL'}]);
  const pos = {lineNumber: 1, column: 7};
  assert.equal(await p.features._hoverProvider.provideHover(p.model, pos, {isCancellationRequested: true}), null);
  p.markers([{severity: 8, startLineNumber: 1, endLineNumber: 2, startColumn: 7, endColumn: 1}]);
  assert.equal(await p.features._hoverProvider.provideHover(p.model, pos), null);
});

test('Подпись вызова считает только запятые аргументов верхнего уровня', async () => {
  const source = 'LEFT(LEFT("а,б", 1), 2';
  const p = providers(source);
  const help = await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1});
  assert.equal(help.value.activeParameter, 1);
  assert.match(help.value.signatures[0].label, /^LEFT\(/);
});

test('Подпись выбирает перегрузку с нужным аргументом и остаётся после пробела', async () => {
  const source = 'LEN("а,б", ';
  const p = providers(source);
  const help = await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1});
  assert.equal(help.value.activeSignature, 1);
  assert.equal(help.value.activeParameter, 1);
});

test('Подпись пользовательской функции берёт параметры компилятора', async () => {
  const source = 'PRINT Twice (price';
  const p = providers(source, [{start: 6, length: 5, name: 'Twice', kind: 'function',
    signature: 'FUNCTION Twice(BYREF value AS DECIMAL) AS DECIMAL', parameters: ['BYREF value AS DECIMAL']}]);
  const help = await p.features._sigProvider.provideSignatureHelp(p.model, {lineNumber: 1, column: source.length + 1});
  assert.equal(help.value.signatures[0].parameters[0].label, 'BYREF value AS DECIMAL');
});


test('Переменная CATCH только для чтения отличается от CONST на обоих языках', async () => {
  const p = providers('PRINT problem, limit', [
    {start: 6, length: 7, name: 'problem', kind: 'errorVariable', type: 'ERROR'},
    {start: 15, length: 5, name: 'limit', kind: 'constant', type: 'INTEGER'},
  ]);
  const hover = async column => (await p.features._hoverProvider.provideHover(p.model, {lineNumber: 1, column})).contents[0].value;
  assert.match(await hover(7), /переменная ошибки, только для чтения/);
  assert.match(await hover(7), /Тип: `ERROR`/);
  assert.doesNotMatch(await hover(7), /константа/);
  assert.match(await hover(16), /константа/);
  p.language('en');
  assert.match(await hover(7), /error variable, read-only/);
  assert.match(await hover(7), /Type: `ERROR`/);
  assert.match(await hover(16), /constant/);
});

test('Общий уровень дополнений не выдаёт слова Axiom для другого языка', async () => {
  const p=providers('PRINT');p.model.getLanguageId=()=> 'plaintext';
  const result=await p.features._completionProvider.provideCompletionItems(p.model,{lineNumber:1,column:6});
  assert.equal(result.suggestions.length,0);
});

for (const failure of ['empty', 'error']) {
  test('Прежний ответ не удаляет новый запрос подсказок: ' + failure, async () => {
    const p = providers('PRINT price');
    const pending = [];
    p.ide.documentServices.request = () => new Promise((resolve, reject) => pending.push({ resolve, reject }));
    const hover = () => p.features._hoverProvider.provideHover(p.model, { lineNumber: 1, column: 7 });
    const old = hover(); p.edit('PRINT price'); const current = hover();
    if (failure === 'empty') pending[0].resolve(null);
    else pending[0].reject(new Error('Прежний отказ'));
    await old;
    const shared = hover();
    const count = pending.length;
    for (const item of pending.slice(1)) item.resolve({ hovers: [{ start: 6, length: 5, name: 'price', kind: 'variable', type: 'STRING' }] });
    assert.match((await current).contents[0].value, /STRING/);
    assert.match((await shared).contents[0].value, /STRING/);
    assert.equal(count, 2, 'Прежний ответ не должен запускать третью проверку');
  });
}

test('Отказ без объекта ошибки не ломает подсказку и допускает повтор', async () => {
  const p = providers('PRINT price');
  const logs = []; p.ide.log = (...args) => logs.push(args);
  let calls = 0;
  p.ide.documentServices.request = async () => { if (++calls === 1) throw null; return { hovers: [{ start: 6, length: 5, name: 'price', kind: 'variable', type: 'STRING' }] }; };
  const hover = () => p.features._hoverProvider.provideHover(p.model, { lineNumber: 1, column: 7 });
  assert.equal(await hover(), null);
  assert.match((await hover()).contents[0].value, /STRING/);
  assert.equal(calls, 2); assert.equal(logs.length, 1);
});

test('Шаблоны всех блоков с PUBLIC и PRIVATE сохраняют модификатор и закрывают блок',()=>{
 for(const modifier of ['PUBLIC','PRIVATE'])for(const block of ['SUB','FUNCTION','TYPE']){
  const p=providers('');
  const items=p.features._completionProvider.provideCompletionItems(p.model,{lineNumber:1,column:1}).suggestions;
  const item=items.find(s=>s.insertText.startsWith(modifier+' '+block+' '));
  assert.ok(item,modifier+' '+block);
  assert.match(item.insertText,new RegExp('\\nEND '+block+'$'));
  assert.ok(item.label.startsWith(modifier+' '+block));
 }
});

test('Шаблон после уже введённого модификатора не дублирует его и заменяет только слово',()=>{
 for(const modifier of ['public','PRIVATE'])for(const block of ['SUB','FUNCTION','TYPE']){
  const source='    '+modifier+' '+block.slice(0,2),p=providers(source);
  const items=p.features._completionProvider.provideCompletionItems(p.model,{lineNumber:1,column:source.length+1}).suggestions;
  const item=items.find(s=>s.label.startsWith(modifier.toUpperCase()+' '+block+' —'));
  assert.ok(item);assert.ok(item.insertText.startsWith(block+' '));
  assert.equal(item.range.insert.startColumn,6+modifier.length);
  assert.ok(!items.some(s=>s.insertText.startsWith('PUBLIC ')||s.insertText.startsWith('PRIVATE ')));
 }
});
