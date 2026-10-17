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
