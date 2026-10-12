const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const controls = require('../src/renderer/ui-controls');

function element(button = true) {
  const attributes = new Map(), classes = new Set();
  return {
    ...(button ? { disabled: false } : {}),
    dataset: {}, title: '',
    setAttribute(key, value) { attributes.set(key, value); },
    getAttribute(key) { return attributes.get(key); },
    classList: { toggle(key, enabled) { if (enabled) classes.add(key); else classes.delete(key); }, contains(key) { return classes.has(key); } },
  };
}

test('Включённое состояние независимо у кнопок и обновляется общим правилом', () => {
  const first = element(), second = element();
  controls.setPressed(first, true); controls.setPressed(second, false);
  assert.equal(first.getAttribute('aria-pressed'), 'true');
  assert.equal(second.getAttribute('aria-pressed'), 'false');
  controls.setPressed(first, false);
  assert.equal(first.getAttribute('aria-pressed'), 'false');
});

test('Доступность синхронизирует кнопку, отметку меню и доступное свойство', () => {
  for (const target of [element(), element(false)]) {
    controls.setEnabled(target, false);
    assert.equal(target.getAttribute('aria-disabled'), 'true');
    assert.ok(target.classList.contains('disabled'));
    if ('disabled' in target) assert.equal(target.disabled, true);
    controls.setEnabled(target, true);
    assert.equal(target.getAttribute('aria-disabled'), 'false');
    assert.ok(!target.classList.contains('disabled'));
    if ('disabled' in target) assert.equal(target.disabled, false);
  }
});

test('Раскрытие меню задаётся общим свойством', () => {
  const target = element();
  controls.setExpanded(target, true);
  assert.equal(target.getAttribute('aria-expanded'), 'true');
  controls.setExpanded(target, false);
  assert.equal(target.getAttribute('aria-expanded'), 'false');
});

test('Подсказка и доступное имя используют одну строку', () => {
  const target = element();
  controls.setLabel(target, 'Отменить (Ctrl+Z)');
  assert.equal(target.title, target.getAttribute('aria-label'));
  controls.setLabel(target, 'Undo (Ctrl+Z)');
  assert.equal(target.title, 'Undo (Ctrl+Z)');
  controls.setLabel(target, 'Имя без подсказки', { tooltip: false });
  assert.equal(target.getAttribute('aria-label'), 'Имя без подсказки');
  assert.equal(target.title, 'Undo (Ctrl+Z)');
});

test('Отметка меню синхронизирует видимость, роль и состояние', () => {
  const check = element(false); check.parentElement = element(false);
  controls.setMenuChecked(check, true);
  assert.ok(check.classList.contains('on'));
  assert.equal(check.parentElement.getAttribute('role'), 'menuitemcheckbox');
  assert.equal(check.parentElement.getAttribute('aria-checked'), 'true');
  controls.setMenuChecked(check, false);
  assert.ok(!check.classList.contains('on'));
  assert.equal(check.parentElement.getAttribute('aria-checked'), 'false');
});

test('Создание пиктограммы использует общие свойства кнопки', () => {
  const target = element(), onClick = () => {};
  const created = controls.createIconButton({ id: 'tb-undo', label: 'Отменить', markup: '<span></span>', onClick, translationKey: 'tb.undo' }, { createElement: () => target });
  assert.equal(created, target);
  assert.equal(target.type, 'button');
  assert.equal(target.className, 'toolbar-btn');
  assert.equal(target.id, 'tb-undo');
  assert.equal(target.dataset.i18nTitle, 'tb.undo');
  assert.equal(target.title, target.getAttribute('aria-label'));
  assert.equal(target.onclick, onClick);
});

test('Общие правила допускают отсутствующий необязательный элемент', () => {
  controls.setPressed(null, true); controls.setEnabled(null, true);
  controls.setExpanded(null, true);
  controls.setLabel(null, 'Подсказка'); controls.setMenuChecked(null, true);
});

test('Общее контекстное меню создаёт доступные строки и пропускает недоступные команды', () => {
  assert.equal(typeof controls.createContextMenu, 'function');
  const events = new Map();
  let menu, restored = 0;
  const doc = { activeElement: null, defaultView: {innerWidth: 640, innerHeight: 400},
    addEventListener: (name, callback) => events.set(name, callback), querySelectorAll: () => [menu],
    createElement(tag) {
      const node = element(tag === 'button');
      node.children = []; node.style = {}; node.hidden = true;
      node.handlers = {};
      node.addEventListener = (name, callback) => { node.handlers[name] = callback; };
      node.appendChild = child => { node.children.push(child); return child; };
      node.replaceChildren = () => { node.children = []; };
      node.querySelectorAll = () => node.children.filter(child => 'disabled' in child && !child.disabled);
      node.getBoundingClientRect = () => ({width: 252, height: 86});
      node.focus = () => { doc.activeElement = node; };
      node.contains = target => node === target || node.children.includes(target);
      node.closest = () => node;
      return node;
    },
  };
  menu = doc.createElement('div');
  const actions = [];
  const popup = controls.createContextMenu(menu, {translate: key => key === 'copy' ? 'Копировать' : 'Очистить',
    onAction: id => actions.push(id), onClose: () => { restored++; }}, doc);
  popup.show([{id: 'copy', translation: 'copy', icon: 'codicon-copy', enabled: false},
    {separator: true}, {id: 'clear', translation: 'clear', icon: 'codicon-clear-all'}], 635, 395);
  const [copy, separator, clear] = menu.children;
  assert.equal(copy.getAttribute('aria-label'), 'Копировать');
  assert.equal(copy.getAttribute('aria-disabled'), 'true');
  assert.equal(copy.children[1].textContent, 'Копировать');
  assert.equal(separator.getAttribute('role'), 'separator');
  assert.equal(doc.activeElement, clear);
  assert.equal(menu.style.left, '384px');
  assert.equal(menu.style.top, '310px');
  const press = key => events.get('keydown')({key, preventDefault() {}, stopPropagation() {}});
  press('ArrowUp'); assert.equal(doc.activeElement, clear);
  menu.handlers.click({target: copy}); assert.equal(actions.length, 0);
  menu.handlers.click({target: clear}); assert.deepEqual(actions, ['clear']);
  assert.equal(menu.hidden, true); assert.equal(restored, 1);
  popup.show([{id: 'copy', translation: 'copy'}, {id: 'clear', translation: 'clear'}], 20, 20);
  const [enabledCopy, enabledClear] = menu.children;
  press('ArrowDown'); assert.equal(doc.activeElement, enabledClear);
  press('ArrowUp'); assert.equal(doc.activeElement, enabledCopy);
  press('End'); assert.equal(doc.activeElement, enabledClear);
  press('Home'); assert.equal(doc.activeElement, enabledCopy);
  press('Tab'); assert.equal(menu.hidden, true); assert.equal(restored, 2);
});

test('Повторяющиеся свойства и палитры не определяются повторно в обработчиках', () => {
  const directory = path.resolve(__dirname, '../src/renderer');
  for (const name of fs.readdirSync(directory).filter(name => name.endsWith('.js') && name !== 'ui-controls.js')) {
    const source = fs.readFileSync(path.join(directory, name), 'utf8');
    assert.doesNotMatch(source, /setAttribute\(['"]aria-(?:pressed|expanded|disabled|checked|label)['"]/, name);
    assert.doesNotMatch(source, /classList\.toggle\(['"](?:disabled|on)['"]|\.disabled\s*=/, name);
  }
  for (const name of ['styles.css', 'shell.css']) {
    assert.doesNotMatch(fs.readFileSync(path.join(directory, name), 'utf8'), /--(?:xs-(?:canvas|surface|accent|editor)|bg-primary|text-primary)\s*:/, name);
  }
});
