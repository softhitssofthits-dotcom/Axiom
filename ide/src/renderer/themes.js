/* Axiom IDE theme registry.
   Single source of truth for the list of themes and for everything that is not a
   plain CSS custom property: Monaco token palette, xterm ANSI palette and the
   settings-dialog preview. Surface colours stay in design-system.css and are read
   here at runtime, so a theme is described in one place per concern instead of
   being copied into app.js, editor-settings.css and CONSOLE_THEMES separately. */
(function (root, factory) {
  const api = factory(root);
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.AxiomThemes = api;
})(typeof window === 'undefined' ? globalThis : window, function (root) {
  'use strict';

  const DEFAULT_ID = 'dark';

  // Per-theme Monaco syntax palette. Values keep the leading '#'; rules strip it
  // and semanticTokenColors keep it, matching Monaco's two APIs.
  const THEMES = [
    {
      id: 'dark', labelKey: 'menu.view.themeDark', label: { ru: 'Тёмная', en: 'Dark' },
      kind: 'modern', logo: 'dark', monacoBase: 'vs-dark',
      syntax: {
        comment: '#6a9955', commentStyle: 'italic', string: '#ce9178', stringEscape: '#d7ba7d',
        interpolation: '#d7ba7d', number: '#b5cea8', keyword: '#c586c0', type: '#4ec9b0',
        builtin: '#dcdcaa', identifier: '#9CDCFE', variable: '#9CDCFE', variableReadonly: '#F0B975',
        function: '#DCDCAA', array: '#B9A3E3', operator: '#E69BC8', delimiter: '#D7BA7D', invalid: '#F44747',
      },
      monacoColors: {
        'editorLineNumber.foreground': '#5f6c80', 'editorLineNumber.activeForeground': '#a5afbf',
        'editorIndentGuide.background1': '#2a3240', 'editorIndentGuide.activeBackground1': '#465064',
      },
      suggest: { match: '#79b4ff', selected: '#cfe3ff' },
      console: {
        black: '#000000', red: '#cd3131', green: '#0dbc79', yellow: '#e5e510', blue: '#2472c8', magenta: '#bc3fbc',
        cyan: '#11a8cd', white: '#e5e5e5', brightBlack: '#666666', brightRed: '#f14c4c', brightGreen: '#23d18b',
        brightYellow: '#f5f543', brightBlue: '#3b8eea', brightMagenta: '#d670d6', brightCyan: '#29b8db', brightWhite: '#e5e5e5',
      },
    },
    {
      id: 'light', labelKey: 'menu.view.themeLight', label: { ru: 'Светлая', en: 'Light' },
      kind: 'modern', logo: 'light', monacoBase: 'vs',
      syntax: {
        comment: '#6a9955', commentStyle: 'italic', string: '#a31515', stringEscape: '#ee0000',
        interpolation: '#ee0000', number: '#098658', keyword: '#0000ff', type: '#267f99',
        builtin: '#795e26', identifier: '#215E9C', variable: '#215E9C', variableReadonly: '#8F4400',
        function: '#795E26', array: '#7552A5', operator: '#AF1970', delimiter: '#705A00', invalid: '#C40000',
      },
      monacoColors: {
        'editorLineNumber.foreground': '#8a98ab', 'editorLineNumber.activeForeground': '#536174',
        'editorIndentGuide.background1': '#e6ebf2', 'editorIndentGuide.activeBackground1': '#c4d1e1',
      },
      suggest: { match: '#235fa9', selected: '#0b3f74' },
      console: {
        black: '#24292f', red: '#cf222e', green: '#1a7f37', yellow: '#9a6700', blue: '#0969da', magenta: '#8250df',
        cyan: '#0550ae', white: '#f6f8fa', brightBlack: '#57606a', brightRed: '#a40e26', brightGreen: '#116329',
        brightYellow: '#7d4e00', brightBlue: '#0550ae', brightMagenta: '#6639ba', brightCyan: '#0349a3', brightWhite: '#ffffff',
      },
    },
    {
      id: 'basic', labelKey: 'menu.view.themeBasic', label: { ru: 'QuickBASIC', en: 'QuickBASIC' },
      kind: 'classic', logo: 'dark', monacoBase: 'vs-dark',
      syntax: {
        comment: '#B8C7E8', commentStyle: 'italic', string: '#AAFFAA', stringEscape: '#55FFFF',
        interpolation: '#55FFFF', number: '#FFFFAA', keyword: '#FFFF55', type: '#FFFFFF',
        builtin: '#55FFFF', identifier: '#FFFFFF', variable: '#FFFFFF', variableReadonly: '#FFFFAA',
        function: '#55FFFF', array: '#AAFFAA', operator: '#FFFF55', delimiter: '#FFFFFF', invalid: '#FFAAAA',
      },
      monacoColors: {
        'editorLineNumber.foreground': '#B8C7E8', 'editorLineNumber.activeForeground': '#FFFFFF',
        'editorIndentGuide.background1': '#3030A0', 'editorIndentGuide.activeBackground1': '#B8C7E8',
        'editorError.foreground': '#FFAAAA', 'editorWarning.foreground': '#FFFF55',
      },
      // Подсказки в классических темах выводятся на светлой панели, поэтому
      // совпадение выделяется тёмно-синим, а не жёлтым цветом редактора.
      suggest: { match: '#0000aa', selected: '#ffff55' },
      console: {
        black: '#000000', red: '#AA0000', green: '#00AA00', yellow: '#AAAA00', blue: '#0000AA', magenta: '#AA00AA',
        cyan: '#00AAAA', white: '#AAAAAA', brightBlack: '#555555', brightRed: '#FF5555', brightGreen: '#55FF55',
        brightYellow: '#FFFF55', brightBlue: '#5555FF', brightMagenta: '#FF55FF', brightCyan: '#55FFFF', brightWhite: '#FFFFFF',
      },
    },
    {
      id: 'borland', labelKey: 'menu.view.themeBorland', label: { ru: 'Borland Pascal', en: 'Borland Pascal' },
      kind: 'classic', logo: 'dark', monacoBase: 'vs-dark',
      syntax: {
        comment: '#FFFF55', commentStyle: '', string: '#FFFF55', stringEscape: '#FFFFFF',
        interpolation: '#FFFFFF', number: '#FFFF55', keyword: '#FFFFFF', type: '#FFFF55',
        builtin: '#FFFF55', identifier: '#FFFF55', variable: '#FFFF55', variableReadonly: '#FFFF55',
        function: '#FFFF55', array: '#FFFF55', operator: '#FFFFFF', delimiter: '#FFFFFF', invalid: '#FF5555',
      },
      monacoColors: {
        'editor.foreground': '#FFFF55', 'editor.selectionForeground': '#0000AA',
        'editorLineNumber.foreground': '#AAAAAA', 'editorLineNumber.activeForeground': '#FFFFFF',
        'editorIndentGuide.background1': '#00AAAA', 'editorIndentGuide.activeBackground1': '#AAAAAA',
        'editorError.foreground': '#FF5555', 'editorWarning.foreground': '#FFFF55',
        'editorGutter.background': '#0000AA', 'editor.inactiveSelectionBackground': '#00AAAA',
        'editorBracketMatch.background': '#00AAAA', 'editorBracketMatch.border': '#FFFFFF',
        'editorBracketHighlight.foreground1': '#FFFFFF', 'editorBracketHighlight.foreground2': '#FFFF55',
        'editorBracketHighlight.foreground3': '#55FFFF', 'editorBracketHighlight.foreground4': '#55FF55',
        'editorBracketHighlight.foreground5': '#FF55FF', 'editorBracketHighlight.foreground6': '#FF5555',
        'editorBracketHighlight.unexpectedBracket.foreground': '#FF5555', 'editor.findMatchBackground': '#00AA00',
        'editor.findMatchHighlightBackground': '#00AAAA', 'editor.wordHighlightBackground': '#00AAAA',
        'editor.wordHighlightStrongBackground': '#00AA00', 'editorWhitespace.foreground': '#AAAAAA',
        'scrollbarSlider.background': '#00AAAA80', 'scrollbarSlider.hoverBackground': '#00AAAA',
        'scrollbarSlider.activeBackground': '#55FFFF', 'minimap.background': '#0000AA', 'input.background': '#0000AA',
        'input.foreground': '#FFFFFF', 'input.border': '#AAAAAA', 'input.placeholderForeground': '#AAAAAA',
        'focusBorder': '#FFFFFF', 'button.background': '#00AA00', 'button.foreground': '#000000',
        'button.hoverBackground': '#00AA00',
      },
      suggest: { match: '#0000aa', selected: '#ffffff' },
      console: {
        black: '#000000', red: '#AA0000', green: '#00AA00', yellow: '#AA5500', blue: '#0000AA', magenta: '#AA00AA',
        cyan: '#00AAAA', white: '#AAAAAA', brightBlack: '#555555', brightRed: '#FF5555', brightGreen: '#55FF55',
        brightYellow: '#FFFF55', brightBlue: '#5555FF', brightMagenta: '#FF55FF', brightCyan: '#55FFFF', brightWhite: '#FFFFFF',
      },
    },
  ];

  // Monaco token name -> syntax palette key.
  const RULES = [
    ['comment', 'comment'], ['string', 'string'], ['string.escape', 'stringEscape'],
    ['delimiter.interpolation', 'interpolation'], ['number', 'number'], ['number.hex', 'number'],
    ['number.binary', 'number'], ['keyword', 'keyword'], ['type', 'type'], ['support.function', 'builtin'],
    ['identifier', 'identifier'], ['invalid', 'invalid'], ['variable', 'variable'],
    ['variable.readonly', 'variableReadonly'], ['function', 'function'], ['array', 'array'],
    ['operator', 'operator'], ['delimiter', 'delimiter'],
  ];
  // Semantic token types reported by semantic-colors.js -> syntax palette key.
  const SEMANTIC = {
    keyword: 'keyword', type: 'type', function: 'function', variable: 'variable',
    array: 'array', 'variable.readonly': 'variableReadonly',
  };

  const byId = new Map(THEMES.map(theme => [theme.id, theme]));
  const stripHash = value => (typeof value === 'string' && value[0] === '#' ? value.slice(1) : value);

  function ids() { return THEMES.map(theme => theme.id); }
  function has(id) { return byId.has(id); }
  function get(id) { return byId.get(id) || byId.get(DEFAULT_ID); }
  function kind(id) { return (byId.get(id) || byId.get(DEFAULT_ID)).kind; }

  // Pure builders: no DOM access, so they are unit-testable in Node.
  function syntaxRules(id) {
    const syntax = get(id).syntax;
    return RULES.map(([token, key]) => {
      const rule = { token, foreground: stripHash(syntax[key]) };
      if (token === 'comment' && syntax.commentStyle !== undefined) rule.fontStyle = syntax.commentStyle;
      return rule;
    });
  }
  function semanticTokenColors(id) {
    const syntax = get(id).syntax, out = {};
    for (const sem of Object.keys(SEMANTIC)) out[sem] = syntax[SEMANTIC[sem]];
    return out;
  }
  function monacoExtraColors(id) { return get(id).monacoColors; }
  function consolePalette(id) { return get(id).console; }

  // Runtime readers: read surface colours from the shared CSS variables.
  function readVars(id, names) {
    const doc = root.document;
    if (!doc || !root.getComputedStyle) return {};
    const el = doc.documentElement, previous = el.dataset.theme;
    el.dataset.theme = id;
    const styles = root.getComputedStyle(el), read = name => styles.getPropertyValue(name).trim();
    const out = {};
    try { for (const name of names) out[name] = read(name); }
    finally { if (previous === undefined) el.removeAttribute('data-theme'); else el.dataset.theme = previous; }
    return out;
  }

  // Monaco surface colours that mirror the workbench CSS. Kept here so a theme
  // still reads its own variables instead of duplicating the values.
  function surfaceColors(id) {
    const read = name => readVars(id, [name])[name] || '';
    const editor = read('--xs-editor');
    const colors = {
      'editor.background': editor, 'editor.foreground': read('--xs-text'),
      'editor.lineHighlightBackground': editor,
      'editor.selectionBackground': read('--xs-editor-selection'),
      'editorCursor.foreground': read('--xs-editor-cursor'),
      'editorWidget.background': read('--xs-menu-bg') || read('--xs-surface-raised'),
      'editorWidget.foreground': read('--xs-menu-text') || read('--xs-text'),
      'editorWidget.border': read('--xs-menu-border') || read('--xs-border'),
      'editorSuggestWidget.background': read('--xs-menu-bg') || read('--xs-surface-raised'),
      'editorSuggestWidget.foreground': read('--xs-menu-text') || read('--xs-text'),
      'editorSuggestWidget.border': read('--xs-menu-border') || read('--xs-border'),
      'editorSuggestWidget.selectedBackground': read('--xs-menu-hover-bg') || read('--xs-surface-active'),
      'editorSuggestWidget.selectedForeground': read('--xs-menu-hover-text') || read('--xs-text'),
    };
    if (read('--xs-menu-bg')) Object.assign(colors, {
      'editorHoverWidget.background': read('--xs-menu-bg'), 'editorHoverWidget.foreground': read('--xs-menu-text'),
      'editorHoverWidget.border': read('--xs-menu-border'),
      'quickInput.background': read('--xs-menu-bg'), 'quickInput.foreground': read('--xs-menu-text'),
      'list.focusBackground': read('--xs-menu-hover-bg'), 'list.focusForeground': read('--xs-menu-hover-text'),
      'list.activeSelectionBackground': read('--xs-menu-hover-bg'), 'list.activeSelectionForeground': read('--xs-menu-hover-text'),
    });
    return colors;
  }

  // Popup colours (suggest list, command palette). Matched characters and symbol icons
  // must come from the theme palette: Monaco's built-in blue highlight and purple/blue
  // symbol palette make one list look unevenly coloured in every Axiom theme.
  function suggestColors(id) {
    const def = get(id);
    const selectedInk = readVars(id, ['--xs-menu-hover-text', '--xs-text']);
    return {
      'editorSuggestWidget.highlightForeground': def.suggest.match,
      'editorSuggestWidget.focusHighlightForeground': def.suggest.selected,
      'editorSuggestWidget.selectedIconForeground': selectedInk['--xs-menu-hover-text'] || selectedInk['--xs-text'] || '',
      'list.highlightForeground': def.suggest.match,
      'list.focusHighlightForeground': def.suggest.selected,
    };
  }

  // Symbol icons reuse the syntax roles of the theme. Classic themes paint Monaco
  // popups on the light chrome surface, where the editor palette (bright on the dark
  // blue field) is unreadable, so there every icon uses the menu ink colour.
  function symbolIconColors(id) {
    const def = get(id);
    const ink = def.kind === 'classic' ? (readVars(id, ['--xs-menu-text'])['--xs-menu-text'] || '#000000') : '';
    const pick = key => ink || def.syntax[key];
    return {
      'symbolIcon.arrayForeground': pick('array'),
      'symbolIcon.booleanForeground': pick('number'),
      'symbolIcon.classForeground': pick('type'),
      'symbolIcon.constantForeground': pick('variableReadonly'),
      'symbolIcon.constructorForeground': pick('function'),
      'symbolIcon.enumeratorForeground': pick('type'),
      'symbolIcon.enumeratorMemberForeground': pick('variable'),
      'symbolIcon.eventForeground': pick('function'),
      'symbolIcon.fieldForeground': pick('variable'),
      'symbolIcon.functionForeground': pick('function'),
      'symbolIcon.interfaceForeground': pick('type'),
      'symbolIcon.keywordForeground': pick('keyword'),
      'symbolIcon.methodForeground': pick('function'),
      'symbolIcon.moduleForeground': pick('identifier'),
      'symbolIcon.namespaceForeground': pick('identifier'),
      'symbolIcon.numberForeground': pick('number'),
      'symbolIcon.operatorForeground': pick('operator'),
      'symbolIcon.propertyForeground': pick('variable'),
      'symbolIcon.snippetForeground': pick('builtin'),
      'symbolIcon.stringForeground': pick('string'),
      'symbolIcon.structForeground': pick('type'),
      'symbolIcon.typeParameterForeground': pick('type'),
      'symbolIcon.variableForeground': pick('variable'),
    };
  }

  function monacoTheme(id) {
    const def = get(id);
    return {
      base: def.monacoBase, inherit: true, semanticHighlighting: true,
      semanticTokenColors: semanticTokenColors(def.id),
      rules: syntaxRules(def.id),
      colors: {
        ...surfaceColors(def.id), ...suggestColors(def.id), ...symbolIconColors(def.id), ...def.monacoColors,
      },
    };
  }

  function consoleTheme(id) {
    const def = get(id);
    const css = readVars(def.id, ['--xs-console-bg', '--xs-text', '--xs-editor-cursor', '--xs-editor-selection']);
    return {
      ...def.console,
      background: css['--xs-console-bg'] || def.console.black,
      foreground: css['--xs-text'] || def.console.white,
      cursor: css['--xs-editor-cursor'] || def.console.white,
      selectionBackground: css['--xs-editor-selection'] || def.console.blue,
    };
  }

  // Preview palette for the settings dialog. Roles are returned as ready values
  // (surface colours from CSS, syntax colours from the theme) so the dialog can set
  // them as plain inline styles. Setting them directly avoids a one-frame delay that
  // var() substitution shows when a custom property is written and read in the same
  // tick — the preview must never paint the fallback colour.
  function previewPalette(id) {
    const def = get(id);
    const css = readVars(def.id, ['--xs-editor', '--xs-text']);
    return {
      background: css['--xs-editor'] || '', foreground: css['--xs-text'] || '',
      comment: def.syntax.comment, keyword: def.syntax.keyword, type: def.syntax.type,
      string: def.syntax.string, number: def.syntax.number,
    };
  }

  return Object.freeze({
    defaultId: DEFAULT_ID, list: THEMES, ids, has, byId: get, kind,
    syntaxRules, semanticTokenColors, monacoExtraColors, consolePalette,
    surfaceColors, monacoTheme, consoleTheme, previewPalette,
  });
});
