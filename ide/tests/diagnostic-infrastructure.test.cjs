const { test, } = require('node:test');
const assert = require('node:assert/strict');
const { DiagnosticLanguage } = require('../src/renderer/diagnostic-language');
test('Внутренняя ошибка показана на выбранном языке без английского исключения в русской IDE', () => {
  const diagnostic = { code: 'IDE0001', message: 'Unexpected execution error: Unsupported expression.' };
  assert.match(DiagnosticLanguage.text(diagnostic, 'ru'), /Внутренняя ошибка/);
  assert.doesNotMatch(DiagnosticLanguage.text(diagnostic, 'ru'), /Unexpected|Unsupported/);
  assert.match(DiagnosticLanguage.text(diagnostic, 'en'), /internal error/);
});
