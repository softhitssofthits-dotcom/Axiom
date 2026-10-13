const { test } = require('node:test');
const assert = require('node:assert/strict');
test('Диагностика NEXT сохраняет оба имени при переводе', () => {
  const {DiagnosticLanguage}=require('../src/renderer/diagnostic-language');
  const diagnostic={code:'MB2048',message:"Имя счётчика после NEXT 'wrong' должно совпадать с именем счётчика FOR 'i'."};
  assert.equal(DiagnosticLanguage.text(diagnostic,'ru'), 'MB2048: '+diagnostic.message);
  assert.equal(DiagnosticLanguage.text(diagnostic,'en'), "MB2048: NEXT counter 'wrong' must match FOR counter 'i'.");
});
const fs = require('node:fs');
const path = require('node:path');
const { DiagnosticLanguage } = require('../src/renderer/diagnostic-language');
test('Все зарегистрированные коды MB имеют английское описание', () => {
  const source = fs.readFileSync(path.join(__dirname, '../../src/Axiom.Core/DiagnosticCodes.cs'), 'utf8');
  for (const code of source.match(/MB\d{4}(?=")/g)) {
    assert.ok(DiagnosticLanguage.english[code], code);
    assert.doesNotMatch(DiagnosticLanguage.text({ code, message: 'Русское сообщение.' }, 'en'), /[А-Яа-яЁё]/, code);
  }
});
test('Переключение языка сохраняет код и исходный русский текст, имена не теряются', () => {
  const d = { code: 'MB2001', message: "Имя 'missing' не объявлено." };
  assert.equal(DiagnosticLanguage.text(d, 'en'), "MB2001: Name 'missing' is not declared.");
  assert.equal(DiagnosticLanguage.text(d, 'ru'), "MB2001: Имя 'missing' не объявлено.");
  assert.equal(d.message, "Имя 'missing' не объявлено.");
  assert.equal(DiagnosticLanguage.text({ code: 'MB3001', message: 'Деление на ноль.' }, 'en'), 'MB3001: Division by zero.');
  assert.equal(DiagnosticLanguage.text({ code: 'MB2004', message: "Неверное число аргументов у 'LEN': ожидалось 1, получено 3." }, 'en'), "MB2004: Wrong number of arguments for 'LEN': expected 1, received 3.");
  assert.equal(DiagnosticLanguage.text({ code: 'MB2003', message: 'Несовместимые типы: ожидался INTEGER, получен STRING.' }, 'en'), 'MB2003: Type mismatch: expected INTEGER, received STRING.');
});
