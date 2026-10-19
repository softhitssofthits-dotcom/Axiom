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

function matchOf(rule, text) {
  const expression = new RegExp('^(?:' + rule.source + ')');
  return expression.exec(text)?.[0];
}

test('Удвоенные скобки интерполяции раскрашиваются как текст раньше вставки выражения', () => {
  const rules = language().tokenizer.interpolatedString;
  const doubledBrace = rules.findIndex(item => item[1] === 'string.escape' && matchOf(item[0], '{{') === '{{');
  const expressionStart = rules.findIndex(item => item[1] && item[1].next === '@interpolation');
  assert.ok(doubledBrace >= 0, 'Должно быть правило удвоенной открывающей скобки');
  assert.ok(expressionStart >= 0, 'Должно быть правило вставки выражения');
  assert.ok(doubledBrace < expressionStart, 'Правило {{ должно стоять раньше правила вставки');
});

test('Удвоенная закрывающая скобка и удвоенная кавычка относятся к тексту интерполяции', () => {
  const rules = language().tokenizer.interpolatedString;
  const escapes = rules.filter(item => item[1] === 'string.escape').map(item => item[0]);
  assert.ok(escapes.some(rule => matchOf(rule, '}}') === '}}'), '}} раскрашивается как текст');
  assert.ok(escapes.some(rule => matchOf(rule, '""') === '""'), '"" раскрашивается как текст');
});

test('Одиночная фигурная скобка по-прежнему открывает вставку выражения', () => {
  const rules = language().tokenizer.interpolatedString;
  const expressionStart = rules.find(item => item[1] && item[1].next === '@interpolation');
  assert.ok(expressionStart, 'Правило вставки выражения существует');
  assert.equal(matchOf(expressionStart[0], '{n:F2}'), '{');
});
