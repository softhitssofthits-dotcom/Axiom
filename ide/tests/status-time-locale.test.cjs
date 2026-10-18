const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function setup() {
  const element = { textContent: '', isConnected: true };
  const context = {
    window: {},
    document: {
      querySelectorAll: () => [],
      getElementById: (id) => (id === 'status-execution-time' ? element : null),
    },
  };
  vm.createContext(context);
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/locale.js'), 'utf8');
  vm.runInContext(source, context);
  return { I18N: context.window.I18N, element };
}

test('Надпись времени в строке состояния переводится при смене языка', () => {
  const { I18N, element } = setup();
  I18N.setText(element, 'status.compileTimeElapsed', 64);
  assert.equal(element.textContent, 'Проверка: 64 мс');
  I18N.setLocale('en');
  assert.equal(element.textContent, 'Check: 64 ms');
  I18N.setLocale('ru');
  assert.equal(element.textContent, 'Проверка: 64 мс');
});

test('Надпись общего времени и времени трассировки следуют текущему языку', () => {
  const { I18N, element } = setup();
  I18N.setLocale('en');
  I18N.setText(element, 'status.totalTimeElapsed', 120);
  assert.equal(element.textContent, 'Total: 120 ms');
  I18N.setText(element, 'trace.elapsed', 7);
  assert.equal(element.textContent, 'Time: 7 ms');
  I18N.setLocale('ru');
  assert.equal(element.textContent, 'Время: 7 мс');
});

test('Отсоединённая надпись больше не обновляется', () => {
  const { I18N, element } = setup();
  I18N.setText(element, 'status.compileTimeElapsed', 5);
  element.isConnected = false;
  I18N.setLocale('en');
  assert.equal(element.textContent, 'Проверка: 5 мс');
});
