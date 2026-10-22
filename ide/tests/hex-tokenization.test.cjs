const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function language() {
  const runtime = {window: { AxiomThemes: require('../src/renderer/themes') }};
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.TestIDE = AxiomIDE;');
  vm.runInNewContext(source, runtime);
  runtime.window.TestIDE.monacoSurface = () => ({});
  let definition;
  const monaco = {languages: {register() {}, setMonarchTokensProvider(_id, value) {definition = value;},
    registerDocumentSemanticTokensProvider() {}, setLanguageConfiguration() {}}, editor: {defineTheme() {}}};
  runtime.window.TestIDE.prototype.setupLanguage.call({monaco, langMeta: null});
  return definition;
}
for (const state of ['root', 'interpolation']) {
  test('Шестнадцатеричный литерал отделён от & в состоянии ' + state, () => {
    const definition = language();
    const rule = definition.tokenizer[state].find(item => item[1] === 'number.hex')[0];
    const expression = new RegExp('^(?:' + rule.source + ')', definition.ignoreCase ? 'i' : '');
    assert.equal(expression.exec('&HFF&"suffix"')[0], '&HFF');
    assert.equal(expression.exec('&hff')[0], '&hff');
  });
}

for (const state of ['root', 'interpolation']) {
  test('Двоичный литерал имеет числовую категорию в состоянии ' + state, () => {
    const definition = language();
    const item = definition.tokenizer[state].find(item => item[1] === 'number.binary');
    assert.ok(item, 'Должно быть правило двоичного числа');
    const expression = new RegExp('^(?:' + item[0].source + ')', definition.ignoreCase ? 'i' : '');
    assert.equal(expression.exec('&B1010&"suffix"')[0], '&B1010');
    assert.equal(expression.exec('&b1010')[0], '&b1010');
  });
}

test('Вложенная интерполяция открывает полный автомат строк раньше обычной кавычки', () => {
  const rules = language().tokenizer.interpolation;
  const first = rules.findIndex(item => item[0].test('$"'));
  assert.equal(rules[first][1][0], 'delimiter.interpolation');
  assert.equal(rules[first][1][1].next, '@interpolatedString');
});

for (const state of ['root', 'interpolation']) {
  for (const [source, expected] of [
    ['42', [['42', 'number']]],
    ['2147483648', [['2147483648', 'number']]],
    ['12.50', [['12.50', 'number']]],
    ['42&value', [['42', 'number'], ['&', 'operator'], ['value', 'identifier']]],
    ['42D', [['42', 'number'], ['D', 'identifier']]],
    ['42F', [['42', 'number'], ['F', 'identifier']]],
    ['42%D', [['42', 'number'], ['%', 'invalid'], ['D', 'identifier']]],
    ['12.5#', [['12.5', 'number'], ['#', 'invalid']]],
    ['1.', [['1', 'number'], ['.', 'delimiter']]],
    ['.5', [['.', 'delimiter'], ['5', 'number']]],
    ['1e3', [['1', 'number'], ['e3', 'identifier']]],
    ['&hff&value', [['&hff', 'number.hex'], ['&', 'operator'], ['value', 'identifier']]],
    ['&b1010', [['&b1010', 'number.binary']]],
    ['&HFG', [['&HFG', 'invalid']]],
    ['&B102', [['&B102', 'invalid']]],
    ['&H', [['&H', 'invalid']]],
    ['&B', [['&B', 'invalid']]],
  ]) {
    test('Границы чисел ' + source + ' соответствуют лексеру в ' + state, () => {
      const definition = language();
      const rules = definition.tokenizer[state];
      const actual = [];
      let rest = source;
      while (rest) {
        const matched = rules.map(rule => {
          const match = new RegExp('^(?:' + rule[0].source + ')', 'i').exec(rest);
          if (!match) return null;
          const action = rule[1];
          let token = typeof action === 'string' ? action : action.token;
          if (action.cases) token = action.cases['@default'];
          return [match[0], token];
        }).find(Boolean);
        assert.ok(matched, 'Нет правила для ' + rest);
        actual.push(matched);
        rest = rest.slice(matched[0].length);
      }
      assert.deepEqual(actual, expected);
    });
  }
}
