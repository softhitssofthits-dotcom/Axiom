const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const context = { window: {} };
vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/renderer/lang-features.js'), 'utf8'), context);
const signature = context.window.LangFeatures.builtinSignature;

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
