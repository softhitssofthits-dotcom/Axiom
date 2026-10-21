const {test} = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const renderer = path.join(__dirname, '../src/renderer');

// Минимальное дерево проверяет перенос настоящих узлов без Electron.
class Element {
  constructor(tag = 'div') { this.tag = tag; this.children = []; this.dataset = {}; this.className = ''; }
  append(...nodes) { for (const node of nodes) { node.remove(); node.parent = this; this.children.push(node); } }
  remove() { if (this.parent) { this.parent.children.splice(this.parent.children.indexOf(this), 1); this.parent = null; } }
  insertBefore(node, anchor) { node.remove(); node.parent = this; this.children.splice(this.children.indexOf(anchor), 0, node); }
  replaceChildren(...nodes) { for (const child of [...this.children]) child.remove(); this.append(...nodes); }
  setAttribute(name, value) { this[name] = value; }
  querySelectorAll(selector) {
    const matches = node => {
      const cls = selector.match(/^\.([\w-]+)/)?.[1];
      if (cls && !node.className.split(' ').includes(cls)) return false;
      const attr = selector.match(/\[data-([\w-]+)(?:="([^"]*)")?\]/);
      if (!attr) return !!cls;
      const key = attr[1].replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
      return attr[2] === undefined ? key in node.dataset : node.dataset[key] === attr[2];
    };
    return this.children.flatMap(child => [...(matches(child) ? [child] : []), ...child.querySelectorAll(selector)]);
  }
  querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
}

function fixture() {
  const document = new Element('document'); document.createElement = tag => new Element(tag);
  document.getElementById = id => {
    const find = root => root.id === id ? root : root.children.map(find).find(Boolean);
    return find(document) || null;
  };
  const source = fs.readFileSync(path.join(renderer, 'index.html'), 'utf8');
  const html = source.slice(source.indexOf('<div id="menubar"'), source.indexOf('<!-- Toolbar'));
  const stack = [document];
  for (const match of html.matchAll(/<\/?([\w-]+)\b([^>]*)>/g)) {
    if (match[0].startsWith('</')) { if (stack.length > 1) stack.pop(); continue; }
    const node = new Element(match[1]);
    for (const attr of match[2].matchAll(/([\w-]+)="([^"]*)"/g)) {
      if (attr[1] === 'class') node.className = attr[2];
      else if (attr[1].startsWith('data-')) node.dataset[attr[1].slice(5).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase())] = attr[2];
      else node[attr[1]] = attr[2];
    }
    stack.at(-1).append(node);
    if (!['input', 'img', 'br', 'meta', 'link', 'hr'].includes(match[1])) stack.push(node);
  }
  const bar = document.getElementById('menubar');
  const trace = new Element(); trace.id = 'menu-trace'; trace.className = 'menu-item';
  const label = new Element('span'); label.className = 'menu-label';
  const dropdown = new Element(); dropdown.className = 'menu-dropdown'; trace.append(label, dropdown); bar.append(trace);
  for (const command of ['start', 'stepBack', 'stepInto', 'stepOver', 'stepOut', 'stop', 'restart', 'breakpoint', 'clear']) {
    const node = new Element(); node.className = 'menu-dropdown-item disabled'; node.dataset.action = 'trace-' + command; node.dataset.traceCommand = command;
    const text = new Element('span'); text.className = 'menu-text'; node.append(text); dropdown.append(node);
  }
  const window = {};
  vm.runInNewContext(fs.readFileSync(path.join(renderer, 'studio-menus.js'), 'utf8'), {document, window, I18N: {t: key => key}});
  return {document, window, bar};
}

test('Меню сохраняют узлы команд, подменю, отметки и трассировку', () => {
  const {document, window, bar} = fixture();
  const ids = ['recent-submenu', 'recent-files-list', 'formatting-submenu', 'lang-submenu', 'menu-settings', 'menu-trace'];
  const saved = ids.map(id => document.getElementById(id));
  const original = bar.querySelectorAll('.menu-dropdown-item[data-action]');
  const trace = original.find(node => node.dataset.action === 'trace-start');
  window.StudioMenus.build();
  assert.deepEqual(bar.children.filter(node => node.className === 'menu-item').map(node => node.dataset.menu), ['file', 'edit', 'view', 'project', 'run', 'trace', 'tools', 'help']);
  assert.deepEqual(bar.children.filter(node => node.className === 'menu-item').map(node => node.dataset.studioOrder), ['0', '1', '2', '3', '4', '5', '6', '7']);
  ids.forEach((id, index) => assert.equal(document.getElementById(id), saved[index], id));
  for (const node of original) {
    if (node.dataset.action === 'toggle-project-explorer') continue; // Прямой переход «Проект» заменяет старый переключатель.
    assert.ok(bar.querySelectorAll('.menu-dropdown-item[data-action]').includes(node), node.dataset.action);
  }
  assert.equal(trace.dataset.traceCommand, 'start');
  assert.ok(trace.className.includes('disabled'));
  assert.equal(document.getElementById('menu-settings').hidden, true);
  assert.equal(bar.querySelector('[data-menu="tools"]').querySelector('[data-action="editor-settings"]').dataset.action, 'editor-settings');
  const count = bar.querySelectorAll('.menu-dropdown-item[data-action]').length;
  window.StudioMenus.build();
  assert.equal(bar.querySelectorAll('.menu-dropdown-item[data-action]').length, count);
});

test('Перестройка возвращает меню из переполнения и оставляет команды в нужных группах', () => {
  const {document, window, bar} = fixture();
  const overflow = new Element(); overflow.id = 'menu-overflow-content';
  const more = new Element('button'); more.id = 'menu-overflow';
  bar.insertBefore(more, document.getElementById('menu-settings')); bar.append(overflow);
  overflow.append(bar.querySelector('[data-menu="view"]'), document.getElementById('menu-trace'));
  window.StudioMenus.build();
  assert.equal(overflow.children.length, 0);
  assert.equal(bar.querySelector('[data-menu="trace"]').parent, bar);
  const actions = name => bar.querySelector('[data-menu="' + name + '"]').querySelectorAll('.menu-dropdown-item[data-action]').map(node => node.dataset.action);
  assert.deepEqual(actions('run'), ['check', 'run', 'run-current-file', 'stop']);
  assert.deepEqual(actions('project'), ['create-project', 'create-project-descriptor', 'open-project-file', 'open-project', 'close-project', 'project-properties', 'new-project-file', 'add-existing-project-file', 'refresh-project', 'choose-project-entry', 'use-current-entry']);
  assert.deepEqual(actions('tools'), ['editor-settings', 'theme-light', 'theme-dark', 'theme-basic', 'theme-borland', 'lang-ru', 'lang-en', 'format-classic', 'format-modern', 'format', 'smart-identifier', 'event-log', 'open-log-folder']);
  for (const check of ['projectVisible', 'outlineVisible', 'panelConsole', 'panelDiagnostics', 'panelAssistant']) assert.ok(bar.querySelector('[data-check="' + check + '"]'), check);
});

test('Все новые подписи меню определены для русского и английского языка', () => {
  const context = {window: {}, document: {}};
  vm.runInNewContext(fs.readFileSync(path.join(renderer, 'locale.js'), 'utf8'), context);
  const keys = [...fs.readFileSync(path.join(renderer, 'studio-menus.js'), 'utf8').matchAll(/'(studio\.[\w]+)'/g)].map(match => match[1]);
  for (const key of keys) for (const locale of ['ru', 'en']) assert.ok(context.window.I18N.dicts[locale][key], locale + ': ' + key);
});
