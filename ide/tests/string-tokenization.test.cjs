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

// Monaco разбирает каждую физическую строку отдельно, поэтому проверяем первый
// подошедший шаблон на остатке строки — это и есть поведение Monarch.
function firstMatch(state, text) {
  for (const rule of language().tokenizer[state]) {
    const match = new RegExp('^(?:' + rule[0].source + ')').exec(text);
    if (match) return { action: rule[1], text: match[0] };
  }
  return null;
}

function tokenOf(action) {
  return typeof action === 'string' ? action : action.token;
}

test('Обычная строка: остаток строки без закрывающей кавычки завершает состояние', () => {
  const matched = firstMatch('string', 'abc');
  assert.equal(matched.text, 'abc');
  assert.equal(tokenOf(matched.action), 'string');
  assert.equal(matched.action.next, '@pop', 'Незакрытый остаток строки не переносится на следующую строку');
});

test('Обычная строка: текст перед закрывающей кавычкой состояние не завершает', () => {
  const matched = firstMatch('string', 'abc"');
  assert.equal(matched.text, 'abc');
  assert.equal(tokenOf(matched.action), 'string');
  assert.equal(matched.action.next, undefined);
});

test('Вложенная строка: остаток строки завершает состояние', () => {
  const matched = firstMatch('nestedString', 'abc');
  assert.equal(matched.text, 'abc');
  assert.equal(matched.action.next, '@pop');
});

test('Текст интерполяции: остаток строки завершает состояние', () => {
  const matched = firstMatch('interpolatedString', 'abc');
  assert.equal(matched.text, 'abc');
  assert.equal(matched.action.next, '@pop');
});

test('Литерал без закрывающей кавычки целиком относится к строке и не открывает состояние', () => {
  const matched = firstMatch('root', '"abc');
  assert.equal(matched.text, '"abc');
  assert.equal(tokenOf(matched.action), 'string');
  assert.equal(matched.action.next, undefined, 'Состояние не переносится на следующую строку');
});

test('Одиночная кавычка в конце строки не открывает состояние', () => {
  const matched = firstMatch('root', '"');
  assert.equal(matched.text, '"');
  assert.equal(matched.action.next, undefined);
});

test('Полный литерал по-прежнему открывает состояние с кавычкой и удвоением', () => {
  const opener = firstMatch('root', '"abc"');
  assert.equal(opener.text, '"');
  assert.equal(tokenOf(opener.action), 'string.quote');
  assert.equal(opener.action.next, '@string');

  const escape = firstMatch('string', '""');
  assert.equal(escape.text, '""');
  assert.equal(escape.action, 'string.escape');
});
