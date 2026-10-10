const { test } = require('node:test');
const assert = require('node:assert/strict');
const { programKey } = require('../src/renderer/program-keys');
test('Клавиши программы используют физическое имя и сохраняют русский ввод', () => {
  assert.deepEqual(programKey({ code:'KeyA', key:'ф' }), { key:'A', text:'ф' });
  assert.deepEqual(programKey({ code:'ArrowLeft', key:'ArrowLeft' }), { key:'LEFT', text:'LEFT' });
  assert.deepEqual(programKey({ code:'ControlLeft', key:'Control' }), { key:'CTRL', text:'' });
  assert.deepEqual(programKey({ code:'Space', key:' ' }), { key:'SPACE', text:'SPACE' });
  assert.equal(programKey({ code:'KeyA', key:'a', isComposing:true }), null);
  assert.equal(programKey({ code:'KeyA', key:'a', metaKey:true }), null);
});
