const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const Themes = require('../src/renderer/themes');
const { legend } = require('../src/renderer/semantic-colors');

const SYNTAX_KEYS = ['comment', 'commentStyle', 'string', 'stringEscape', 'interpolation', 'number',
  'keyword', 'type', 'builtin', 'identifier', 'variable', 'variableReadonly', 'function', 'array',
  'operator', 'delimiter', 'invalid'];
const CONSOLE_KEYS = ['black', 'red', 'green', 'yellow', 'blue', 'magenta', 'cyan', 'white', 'brightBlack',
  'brightRed', 'brightGreen', 'brightYellow', 'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'];

test('реестр тем объявляет ровно четыре темы в стабильном порядке', () => {
  assert.deepEqual(Themes.ids(), ['dark', 'light', 'basic', 'borland']);
  assert.equal(Themes.defaultId, 'dark');
  assert.equal(Themes.list.length, 4);
  assert.ok(Themes.has('basic') && Themes.has('borland'));
  assert.equal(Themes.has('nope'), false);
  assert.equal(Themes.byId('nope').id, 'dark', 'неизвестный id откатывается к теме по умолчанию');
});

test('каждая тема описывает один и тот же набор ролей синтаксиса и ANSI', () => {
  for (const theme of Themes.list) {
    assert.ok(theme.labelKey && theme.label && theme.label.ru && theme.label.en, theme.id + ': подпись');
    assert.ok(['modern', 'classic'].includes(theme.kind), theme.id + ': kind');
    assert.ok(['vs', 'vs-dark'].includes(theme.monacoBase), theme.id + ': база Monaco');
    assert.deepEqual(Object.keys(theme.syntax).sort(), [...SYNTAX_KEYS].sort(), theme.id + ': роли синтаксиса');
    assert.deepEqual(Object.keys(theme.console).sort(), [...CONSOLE_KEYS].sort(), theme.id + ': палитра ANSI');
    for (const [key, value] of Object.entries(theme.syntax)) {
      if (key === 'commentStyle') continue;
      assert.match(value, /^#[0-9A-Fa-f]{6}$/, theme.id + ': hex синтаксиса ' + key);
    }
    for (const value of Object.values(theme.console)) assert.match(value, /^#[0-9A-Fa-f]{6}$/, theme.id + ': hex ANSI');
    assert.deepEqual(Object.keys(theme.suggest).sort(), ['match', 'selected'], theme.id + ': цвета подсказок');
    for (const value of Object.values(theme.suggest)) assert.match(value, /^#[0-9A-Fa-f]{6}$/, theme.id + ': hex подсказок');
  }
});

test('правила Monaco точно повторяют палитру темы и совпадают с приёмкой подсветки', () => {
  const rules = id => Object.fromEntries(Themes.syntaxRules(id).map(r => [r.token, r.foreground]));
  const basic = rules('basic');
  assert.equal(basic.keyword, 'FFFF55');
  assert.equal(basic.type, 'FFFFFF');
  assert.equal(basic['support.function'], '55FFFF');
  assert.equal(basic.string, 'AAFFAA');
  assert.equal(basic.comment, 'B8C7E8');
  assert.equal(basic.operator, 'FFFF55');
  assert.equal(Themes.syntaxRules('basic').find(r => r.token === 'comment').fontStyle, 'italic');

  const borland = rules('borland');
  assert.equal(borland.keyword, 'FFFFFF');
  assert.equal(borland.type, 'FFFF55');
  assert.equal(borland.comment, 'FFFF55');
  assert.equal(borland.operator, 'FFFFFF');
  assert.equal(Themes.syntaxRules('borland').find(r => r.token === 'comment').fontStyle, '');

  const dark = rules('dark');
  assert.equal(dark.keyword, 'c586c0');
  assert.equal(dark.type, '4ec9b0');
  assert.equal(dark.function, 'DCDCAA');
  assert.equal(dark['variable.readonly'], 'F0B975');

  const light = rules('light');
  assert.equal(light.keyword, '0000ff');
  assert.equal(light.type, '267f99');
  assert.equal(light['delimiter.interpolation'], 'ee0000');
});

test('semanticTokenColors покрывает все типы семантических токенов языка', () => {
  for (const theme of Themes.list) {
    const colors = Themes.semanticTokenColors(theme.id);
    for (const type of legend.tokenTypes) {
      assert.match(colors[type] || '', /^#[0-9A-Fa-f]{6}$/, theme.id + ': семантический тип ' + type);
    }
  }
  assert.equal(Themes.semanticTokenColors('borland').variable, '#FFFF55');
  assert.equal(Themes.semanticTokenColors('dark').keyword, '#c586c0');
});

test('CSS-палитра классических тем включает общую ретро-тень', () => {
  const css = fs.readFileSync(path.join(__dirname, '..', 'src', 'renderer', 'design-system.css'), 'utf8');
  const blocks = {};
  const re = /(?:^|\n)(:root|\[data-theme="light"\]|html\[data-theme="basic"\]|html\[data-theme="borland"\])\s*\{([\s\S]*?)\n\}/g;
  let match;
  while ((match = re.exec(css))) blocks[match[1]] = match[2];
  const required = ['--xs-shadow-menu', '--xs-editor', '--xs-console-bg', '--xs-accent', '--xs-status-bg',
    '--xs-editor-cursor', '--xs-editor-selection'];
  for (const [label, body] of Object.entries(blocks)) {
    for (const name of required) assert.match(body, new RegExp(name + '\\s*:'), label + ' не задаёт ' + name);
  }
  assert.match(blocks['html[data-theme="basic"]'], /--xs-shadow-menu:\s*var\(--xs-menu-shadow\)/, 'QuickBASIC использует ретро-тень меню');
  assert.match(blocks['html[data-theme="borland"]'], /--xs-shadow-menu:\s*var\(--xs-menu-shadow\)/, 'Borland использует ретро-тень меню');
});

test('тема отмечается общим признаком семейства chrome', () => {
  assert.equal(Themes.kind('basic'), 'classic');
  assert.equal(Themes.kind('borland'), 'classic');
  assert.equal(Themes.kind('dark'), 'modern');
  assert.equal(Themes.kind('light'), 'modern');
  assert.equal(Themes.kind('unknown'), 'modern');
});

test('линия полос изменения размера задана общей переменной и видна в Borland', () => {
  const css = fs.readFileSync(path.join(__dirname, '..', 'src', 'renderer', 'design-system.css'), 'utf8');
  const blocks = {};
  const re = /(?:^|\n)(:root|\[data-theme="light"\]|html\[data-theme="basic"\]|html\[data-theme="borland"\])\s*\{([\s\S]*?)\n\}/g;
  let match;
  while ((match = re.exec(css))) blocks[match[1]] = match[2];
  assert.match(blocks[':root'], /--xs-splitter\s*:\s*var\(--xs-border-subtle\)/, 'общая полоса по умолчанию совпадает с тонкой границей');
  assert.match(blocks['html[data-theme="borland"]'], /--xs-splitter\s*:\s*#00aaaa/i, 'Borland использует различимую бирюзовую полосу');
  const rule = css.match(/#panel-resizer::before,\s*#sidebar-resizer::before\s*\{[^}]*\}/);
  assert.ok(rule && /background:\s*var\(--xs-splitter\)/.test(rule[0]), 'обе полосы берут цвет из общей переменной');
});

test('цвет полоски активной вкладки задан одной переменной во всех темах', () => {
  const css = fs.readFileSync(path.join(__dirname, '..', 'src', 'renderer', 'design-system.css'), 'utf8');
  const blocks = {};
  const re = /(?:^|\n)(:root|\[data-theme="light"\]|html\[data-theme="basic"\]|html\[data-theme="borland"\])\s*\{([\s\S]*?)\n\}/g;
  let match;
  while ((match = re.exec(css))) blocks[match[1]] = match[2];
  for (const [label, body] of Object.entries(blocks)) {
    assert.match(body, /--xs-tab-active-stripe\s*:/, label + ' явно задаёт полоску активной вкладки');
  }
  assert.match(blocks['html[data-theme="borland"]'], /--xs-tab-active-stripe\s*:\s*#00aa00/i, 'Borland использует паскалевский зелёный');
  const rule = css.match(/\.editor-tab\.active::after\s*\{[^}]*\}/);
  assert.ok(rule && /background:\s*var\(--xs-tab-active-stripe\)/.test(rule[0]), 'полоска берёт цвет из общей переменной');
});

test('консоль Borland продолжает синюю гамму рабочего поля, а не чёрный фон', () => {
  const css = fs.readFileSync(path.join(__dirname, '..', 'src', 'renderer', 'design-system.css'), 'utf8');
  const blocks = {};
  const re = /(?:^|\n)(:root|\[data-theme="light"\]|html\[data-theme="basic"\]|html\[data-theme="borland"\])\s*\{([\s\S]*?)\n\}/g;
  let match;
  while ((match = re.exec(css))) blocks[match[1]] = match[2];
  const borland = blocks['html[data-theme="borland"]'];
  const editor = (borland.match(/--xs-editor\s*:\s*(#[0-9a-fA-F]{6})/) || [])[1];
  const console_ = (borland.match(/--xs-console-bg\s*:\s*(#[0-9a-fA-F]{6})/) || [])[1];
  assert.equal(editor, '#0000aa', 'рабочее поле Pascal синее');
  assert.match(console_, /^#[0-9a-fA-F]{6}$/, 'фон консоли задан цветом');
  assert.notEqual(console_.toLowerCase(), '#000000', 'консоль не чёрная');
  const channel = hex => [1, 3, 5].map(i => parseInt(hex.slice(i, i + 2), 16));
  const [er, eg, eb] = channel(editor), [cr, cg, cb] = channel(console_);
  assert.ok(cb > cr && cb > cg, 'консоль в синей гамме (синий канал доминирует)');
  assert.ok(cb <= eb, 'консоль не светлее рабочего поля по синему каналу');
  assert.match(borland, /--xs-output-header-bg\s*:\s*#000055/i, 'шапка вывода в той же синей гамме');
});

test('подсказки и иконки символов берут цвета из палитры темы, а не из встроенной палитры Monaco', () => {
  const colors = id => Themes.monacoTheme(id).colors;
  const dark = colors('dark');
  assert.equal(dark['editorSuggestWidget.highlightForeground'], Themes.byId('dark').suggest.match);
  assert.equal(dark['editorSuggestWidget.focusHighlightForeground'], Themes.byId('dark').suggest.selected);
  assert.equal(dark['list.highlightForeground'], Themes.byId('dark').suggest.match);
  assert.equal(dark['symbolIcon.keywordForeground'], Themes.byId('dark').syntax.keyword);
  assert.equal(dark['symbolIcon.functionForeground'], Themes.byId('dark').syntax.function);
  assert.equal(dark['symbolIcon.methodForeground'], Themes.byId('dark').syntax.function);
  assert.equal(dark['symbolIcon.classForeground'], Themes.byId('dark').syntax.type);
  assert.equal(dark['symbolIcon.structForeground'], Themes.byId('dark').syntax.type);
  assert.equal(dark['symbolIcon.variableForeground'], Themes.byId('dark').syntax.variable);
  assert.equal(dark['symbolIcon.fieldForeground'], Themes.byId('dark').syntax.variable);
  assert.equal(dark['symbolIcon.constantForeground'], Themes.byId('dark').syntax.variableReadonly);
  assert.equal(dark['symbolIcon.snippetForeground'], Themes.byId('dark').syntax.builtin);
  assert.equal(dark['symbolIcon.arrayForeground'], Themes.byId('dark').syntax.array);

  const light = colors('light');
  assert.equal(light['symbolIcon.keywordForeground'], Themes.byId('light').syntax.keyword);
  assert.equal(light['symbolIcon.functionForeground'], Themes.byId('light').syntax.function);
  assert.equal(light['editorSuggestWidget.highlightForeground'], Themes.byId('light').suggest.match);

  // Классические темы показывают подсказки на светлой панели: яркая палитра
  // редактора там нечитаема, поэтому все иконки используют цвет текста меню.
  for (const id of ['basic', 'borland']) {
    const classic = colors(id);
    for (const [key, value] of Object.entries(classic)) {
      if (key.startsWith('symbolIcon.')) assert.equal(value, '#000000', id + ': иконка ' + key + ' использует цвет меню');
    }
    assert.equal(classic['editorSuggestWidget.highlightForeground'], '#0000aa', id + ': совпадение выделено тёмно-синим');
    assert.equal(classic['editorSuggestWidget.focusHighlightForeground'], Themes.byId(id).suggest.selected, id + ': совпадение в выбранной строке');
  }

  // Встроенная палитра Monaco не должна просачиваться ни в одну тему.
  const foreign = ['#B180D7', '#75BEFF', '#EE9D28', '#2AAAFF'];
  for (const id of Themes.ids()) {
    for (const [key, value] of Object.entries(colors(id))) {
      if (!key.startsWith('symbolIcon.') && !key.startsWith('editorSuggestWidget.')) continue;
      assert.ok(!foreign.includes(String(value).toUpperCase()), id + ': ' + key + ' не использует цвет Monaco ' + value);
    }
  }
});
