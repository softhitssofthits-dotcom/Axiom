const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const context = { window: {} };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/lang-features.js'), 'utf8'), context);
const signature = context.window.LangFeatures.builtinSignature;

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
  return {model, features, requests: () => requests, edit: value => {source = value; version++;},
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
