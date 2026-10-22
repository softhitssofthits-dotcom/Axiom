const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

function keyboard({ modal = false, terminal = false, selected = '', clipboard = Promise.resolve('text') } = {}) {
  const listeners = new Map(), calls = [];
  const textarea = {};
  const runtime = {
    window: {
      axiom: {
        onWindowCloseRequested() {},
        writeClipboardText(text) { calls.push(['copy', text]); return Promise.resolve(true); },
        readClipboardText() { return clipboard; },
      },
      addEventListener(name, listener, capture) { listeners.set(name, { listener, capture }); },
    },
    document: { querySelector() { return modal ? {} : null; }, activeElement: terminal ? textarea : {} },
  };
  const source = fs.readFileSync(path.join(__dirname, '../src/renderer/app.js'), 'utf8')
    .replace('window.__ide = new AxiomIDE();', 'window.TestIDE = AxiomIDE;');
  vm.runInNewContext(source, runtime);
  const prototype = runtime.window.TestIDE.prototype;
  const ide = {
    log() {}, focusState() { return {}; },
    handleAction(action) { calls.push(['action', action]); },
    run(mode) { calls.push(['run', mode]); },
    trace: { command(action) { calls.push(['trace', action]); } },
    execution: { input(text) { calls.push(['input', text]); return Promise.resolve(); } },
    term: { textarea, hasSelection() { return !!selected; }, getSelection() { return selected; } },
    queueConsoleWrite: prototype.queueConsoleWrite,
    writeConsolePaste: prototype.writeConsolePaste,
    consoleInput: prototype.consoleInput,
  };
  prototype.setupKeyboardShortcuts.call(ide);
  const registration = listeners.get('keydown');
  assert.equal(registration.capture, true, 'Команды IDE перехватываются до xterm');
  return {
    calls, ide,
    press(overrides) {
      const consumed = { defaults: 0, propagation: 0 };
      registration.listener({ key: 's', code: 'KeyS', ctrlKey: false, metaKey: false, shiftKey: false, altKey: false,
        preventDefault() { consumed.defaults++; }, stopPropagation() { consumed.propagation++; }, ...overrides });
      return consumed;
    },
  };
}

for (const modifier of ['ctrlKey', 'metaKey']) {
  for (const [key, code, shiftKey, altKey, expected] of [
    ['f', 'KeyF', true, true, 'format'],
    ['f', 'KeyF', true, false, null],
    ['s', 'KeyS', true, false, 'save-as'],
    ['s', 'KeyS', true, true, 'save-as'],
    ['c', 'KeyC', true, true, 'check'],
    ['e', 'KeyE', true, false, 'toggle-project-explorer'],
    ['b', 'KeyB', false, true, 'toggle-sidebar'],
    ['b', 'KeyB', true, false, null],
    ['o', 'KeyO', true, true, 'toggle-output'],
    ['s', 'KeyS', false, true, 'save'],
    ['n', 'KeyN', true, true, 'new'],
    ['o', 'KeyO', false, true, 'open'],
    ['w', 'KeyW', true, true, 'close-tab'],
    [',', 'Comma', true, true, 'editor-settings'],
    ['ы', 'KeyS', false, false, 'save'],
    ['S', '', false, false, 'save'],
  ]) {
    test(`${modifier}: ${code || key}, Shift=${shiftKey}, Alt=${altKey}`, () => {
      const fixture = keyboard({ terminal: true });
      const consumed = fixture.press({ key, code, shiftKey, altKey, [modifier]: true });
      assert.deepEqual(fixture.calls, expected ? [['action', expected]] : []);
      assert.deepEqual(consumed, { defaults: expected ? 1 : 0, propagation: expected ? 1 : 0 });
    });
  }
}

for (const [key, ctrlKey, shiftKey, expected] of [
  ['F5', false, false, ['trace', 'start']], ['F5', false, true, ['trace', 'stop']],
  ['F5', true, false, ['run', 'run']], ['F5', true, true, ['trace', 'restart']],
  ['F9', false, false, ['trace', 'breakpoint']], ['F9', true, false, ['trace', 'breakpoint']],
  ['F9', false, true, ['trace', 'breakpoint']], ['F9', true, true, ['trace', 'clear']],
  ['F10', false, false, ['trace', 'stepOver']], ['F10', true, false, ['trace', 'stepBack']],
  ['F10', false, true, ['trace', 'stepOver']], ['F10', true, true, ['trace', 'stepBack']],
  ['F11', false, false, ['trace', 'stepInto']], ['F11', true, false, ['trace', 'stepInto']],
  ['F11', false, true, ['trace', 'stepOut']], ['F11', true, true, ['trace', 'stepOut']],
]) {
  test(`${key}, Ctrl=${ctrlKey}, Shift=${shiftKey}: одна команда отладки`, () => {
    const fixture = keyboard({ terminal: true });
    const consumed = fixture.press({ key, code: key, ctrlKey, shiftKey });
    assert.deepEqual(fixture.calls, [expected]);
    assert.deepEqual(consumed, { defaults: 1, propagation: 1 });
  });
}

for (const key of ['F5', 'F9', 'F10', 'F11']) {
  test('Alt+' + key + ' сохраняет обычную обработку', () => {
    const fixture = keyboard();
    assert.deepEqual(fixture.press({ key, code: key, ctrlKey: true, altKey: true }), { defaults: 0, propagation: 0 });
    assert.deepEqual(fixture.calls, []);
  });
}

test('Диалог блокирует команды, обычный ввод остаётся у редактора', () => {
  const blocked = keyboard({ modal: true });
  assert.deepEqual(blocked.press({ ctrlKey: true }), { defaults: 0, propagation: 0 });
  assert.deepEqual(blocked.calls, []);
  const ordinary = keyboard();
  assert.deepEqual(ordinary.press({}), { defaults: 0, propagation: 0 });
  assert.deepEqual(ordinary.calls, []);
});

test('Копирование консоли зависит от выделения и фокуса, проверка имеет приоритет', async () => {
  for (const selected of ['', 'answer']) {
    const fixture = keyboard({ terminal: true, selected });
    assert.deepEqual(fixture.press({ key: 'c', code: 'KeyC', ctrlKey: true }), { defaults: 1, propagation: 1 });
    await Promise.resolve();
    assert.deepEqual(fixture.calls, selected ? [['copy', 'answer']] : []);
  }
  const editor = keyboard({ selected: 'answer' });
  assert.deepEqual(editor.press({ key: 'c', code: 'KeyC', ctrlKey: true }), { defaults: 0, propagation: 0 });
  assert.deepEqual(editor.calls, []);
  const checking = keyboard({ terminal: true, selected: 'answer' });
  checking.press({ key: 'c', code: 'KeyC', ctrlKey: true, shiftKey: true });
  assert.deepEqual(checking.calls, [['action', 'check']]);
});

test('Вставка и следующий Enter проходят одну очередь и сохраняют порядок', async () => {
  let releaseClipboard;
  const fixture = keyboard({ terminal: true, clipboard: new Promise(resolve => { releaseClipboard = resolve; }) });
  assert.deepEqual(fixture.press({ key: 'v', code: 'KeyV', ctrlKey: true }), { defaults: 1, propagation: 1 });
  fixture.ide.consoleInput('\r');
  await Promise.resolve();
  assert.deepEqual(fixture.calls, []);
  releaseClipboard('answer\u001b\r\n');
  await fixture.ide._consoleWriteChain;
  assert.deepEqual(fixture.calls, [['input', 'answer\r'], ['input', '\r']]);
  const editor = keyboard();
  assert.deepEqual(editor.press({ key: 'v', code: 'KeyV', ctrlKey: true }), { defaults: 0, propagation: 0 });
});
