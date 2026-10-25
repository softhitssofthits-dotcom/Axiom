const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function language() {
  const runtime = {window: {AxiomThemes: require('../src/renderer/themes'), AxiomSemanticColors: require('../src/renderer/semantic-colors')}};
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.TestIDE = AxiomIDE;');
  vm.runInNewContext(source, runtime);
  let definition, configuration, semantic;
  const monaco = {languages: {register() {}, setMonarchTokensProvider(_id, value) {definition = value;},
    registerDocumentSemanticTokensProvider(_id, value) {semantic = value;},
    setLanguageConfiguration(_id, value) {configuration = value;}}, editor: {defineTheme() {}, tokenize: () => [[]]}};
  runtime.window.TestIDE.prototype.setupLanguage.call({monaco, langMeta: null});
  return {runtime, definition, configuration, semantic};
}

for (const state of ['root', 'interpolation']) {
  test('Monaco распознаёт квадратные скобки и вызовы в ' + state, () => {
    const {definition} = language();
    const rule = definition.tokenizer[state].find(item => item[1] === '@brackets');
    assert.ok(rule);
    for (const ch of ['[', ']', '(', ')']) assert.equal(new RegExp('^(?:' + rule[0].source + ')').exec(ch)?.[0], ch);
    assert.ok(!rule[0].test(','));
  });
}

test('Сопоставление, цвета, автозакрытие и обрамление регистрируют [] вместе с ()', () => {
  const {configuration} = language();
  for (const name of ['brackets', 'colorizedBracketPairs']) {
    assert.equal(JSON.stringify(configuration[name]), JSON.stringify([['(', ')'], ['[', ']']]));
  }
  for (const name of ['autoClosingPairs', 'surroundingPairs']) {
    assert.ok(configuration[name].some(pair => pair.open === '[' && pair.close === ']'));
    assert.ok(configuration[name].some(pair => pair.open === '(' && pair.close === ')'));
  }
});

test('Скобки в тексте обычной и интерполированной строки не становятся кодом', () => {
  const {definition} = language();
  for (const state of ['string', 'interpolatedString']) {
    const rule = definition.tokenizer[state].find(item => new RegExp('^(?:' + item[0].source + ')').test('[2]'));
    assert.ok(rule);
    assert.equal(typeof rule[1] === 'string' ? rule[1] : rule[1].token, 'string');
  }
});

test('EACH и IN выделяются только в заголовке FOR EACH', () => {
  const {definition} = language();
  const rules = definition.tokenizer.root.filter(item => item[0] && String(item[0].source).includes('EACH'));
  assert.equal(rules.length, 2);
  const [eachRule, inRule] = rules;
  assert.ok(new RegExp(eachRule[0].source, 'i').test('FOR EACH fruit IN fruits[]'));
  assert.ok(new RegExp(inRule[0].source, 'i').test('FOR EACH fruit IN fruits[]'));
  // Вне заголовка это обычные имена, поэтому правила не срабатывают.
  assert.ok(!new RegExp(eachRule[0].source, 'i').test('DIM each AS INTEGER'));
  assert.ok(!new RegExp(inRule[0].source, 'i').test('DIM each AS INTEGER'));
  assert.ok(!new RegExp(eachRule[0].source, 'i').test('FOR i = 1 TO 3'));
});

test('Литерал массива раскрашивается теми же скобками, что и обращение', () => {
  const {definition} = language();
  const brackets = definition.tokenizer.root.find(item => item[1] === '@brackets');
  assert.ok(brackets);
  for (const ch of ['[', ']']) assert.equal(new RegExp('^(?:' + brackets[0].source + ')').exec(ch)?.[0], ch);
  // Значения внутри литерала остаются обычными строками и числами.
  const stringRule = definition.tokenizer.root.find(item => new RegExp('^(?:' + item[0].source + ')').test('"яблоко"'));
  assert.ok(stringRule);
  assert.match(typeof stringRule[1] === 'string' ? stringRule[1] : stringRule[1].token, /^string/);
  const numberRule = definition.tokenizer.root.find(item => new RegExp('^(?:' + item[0].source + ')').test('30'));
  assert.ok(numberRule);
  assert.match(typeof numberRule[1] === 'string' ? numberRule[1] : numberRule[1].token, /^number/);
});

for (const reason of ['edit', 'cancel', 'dispose']) {
  test('Квадратная семантическая раскраска отклоняет устаревший ответ: ' + reason, async () => {
    const {runtime, semantic} = language();
    let resolve, version = 1, disposed = false;
    runtime.window.LangFeatures = {getSemanticHovers: () => new Promise(done => {resolve = done;})};
    const model = {getVersionId: () => version, getValue: () => 'PRINT grid[1, 2]', isDisposed: () => disposed};
    const cancellation = {isCancellationRequested: false};
    const request = semantic.provideDocumentSemanticTokens(model, null, cancellation);
    if (reason === 'edit') version++;
    else if (reason === 'dispose') disposed = true;
    else cancellation.isCancellationRequested = true;
    resolve([{start: 6, length: 4, kind: 'variable', type: 'INTEGER[,]'}]);
    assert.equal(await request, null);
  });
}
