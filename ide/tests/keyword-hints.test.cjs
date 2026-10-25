const test = require('node:test');
const assert = require('node:assert/strict');
const { hints, hint } = require('../src/renderer/keyword-hints');

test('Каждое ключевое слово языка имеет подсказку на русском и английском', () => {
  const fs = require('node:fs');
  const path = require('node:path');
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8');
  const match = source.match(/keywords: \[([^\]]+)\]/);
  assert.ok(match, 'список ключевых слов найден');
  const keywords = match[1].split(',').map((item) => item.trim().replace(/'/g, '')).filter(Boolean);
  assert.ok(keywords.length > 90);
  const missing = keywords.filter((word) => !hints[word]);
  assert.deepEqual(missing, [], 'нет описания для: ' + missing.join(', '));
});

test('Подсказки контекстных слов EACH и IN объясняют их роль', () => {
  for (const word of ['EACH', 'IN']) {
    const ru = hint(word, 'ru');
    const en = hint(word, 'en');
    assert.ok(ru && ru.length > 20, word + ' описан по-русски');
    assert.ok(en && en.length > 20, word + ' описан по-английски');
    assert.match(ru, /FOR EACH/);
    assert.match(en, /FOR EACH/);
  }
});

test('Подсказка зависит от языка интерфейса и не смешивает их', () => {
  const ru = hint('FOR', 'ru');
  const en = hint('FOR', 'en');
  assert.notEqual(ru, en);
  assert.match(ru, /[А-Яа-я]/);
  assert.doesNotMatch(en, /[А-Яа-я]/);
});

test('Подсказка BYREF описывает действующий режим FOR EACH', () => {
  const ru = hint('BYREF', 'ru');
  const en = hint('BYREF', 'en');
  assert.match(ru, /FOR EACH задаёт адресацию текущего элемента массива/);
  assert.match(en, /FOR EACH it selects the current array element/);
  assert.doesNotMatch(ru + en, /пока не реализован|not implemented yet/i);
});

test('Неизвестное слово не даёт подсказки', () => {
  assert.equal(hint('НЕСЛОВО', 'ru'), null);
  assert.equal(hint('', 'ru'), null);
  assert.equal(hint(null, 'ru'), null);
});
