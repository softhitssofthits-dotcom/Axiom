(function () {
  const dictionary = window.AxiomCodeDictionary || { keywords: [], types: [], builtins: [] };
  const keywords = new Set(dictionary.keywords.map(word => word.toUpperCase()));
  const types = new Set(dictionary.types.map(word => word.toUpperCase()));
  const builtins = new Set(dictionary.builtins.map(word => word.toUpperCase()));
  function tokenize(source) {
    const tokens = [];
    let position = 0;
    function emit(end, type) {
      if (end > position) tokens.push({ start: position, end, type, text: source.slice(position, end) });
      position = end;
    }
    function string(interpolated) {
      if (interpolated) emit(position + 1, 'delimiter.interpolation');
      emit(position + 1, 'string');
      while (position < source.length) {
        if (source.startsWith('""', position)) { emit(position + 2, 'string.escape'); continue; }
        if (source[position] === '"') { emit(position + 1, 'string'); return; }
        if (interpolated && (source.startsWith('{{', position) || source.startsWith('}}', position))) { emit(position + 2, 'string.escape'); continue; }
        if (interpolated && source[position] === '{') {
          emit(position + 1, 'delimiter.interpolation');
          code(true);
          continue;
        }
        if (source[position] === '\n') return;
        let end = position + 1;
        while (end < source.length && !['"','\n'].includes(source[end]) && !(interpolated && ['{','}'].includes(source[end]))) end++;
        emit(end, 'string');
      }
    }
    function code(hole) {
      let parentheses = 0;
      let braces = 0;
      while (position < source.length) {
        const rest = source.slice(position);
        const char = source[position];
        if (hole && char === '}' && braces === 0) { emit(position + 1, 'delimiter.interpolation'); return; }
        if (hole && char === ':' && source[position + 1] !== '=' && parentheses === 0 && braces === 0) {
          emit(position + 1, 'delimiter');
          const close = source.indexOf('}', position);
          emit(close < 0 ? source.length : close, 'string');
          if (close >= 0) emit(position + 1, 'delimiter.interpolation');
          return;
        }
        if (char === "'" || /^REM\b/i.test(rest)) {
          const newline = source.indexOf('\n', position);
          emit(newline < 0 ? source.length : newline, 'comment'); continue;
        }
        if (source.startsWith('$"', position)) { string(true); continue; }
        if (char === '"') { string(false); continue; }
        const placeholder = rest.match(/^<[A-Za-zА-Яа-яЁё_][A-Za-zА-Яа-яЁё0-9_, ()-]*>/);
        if (placeholder) { emit(position + placeholder[0].length, 'placeholder'); continue; }
        const hexadecimal = rest.match(/^&H[0-9a-f]+/i);
        if (hexadecimal) { emit(position + hexadecimal[0].length, 'number.hex'); continue; }
        const binary = rest.match(/^&B[01]+/i);
        if (binary) { emit(position + binary[0].length, 'number.binary'); continue; }
        const number = rest.match(/^(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?/);
        if (number) { emit(position + number[0].length, 'number'); continue; }
        const word = rest.match(/^[A-Za-z_][A-Za-z_0-9]*/);
        if (word) {
          const name = word[0].toUpperCase();
          const call = /^\s*\(/.test(rest.slice(word[0].length));
          const type = builtins.has(name) && call ? 'support.function' : types.has(name) ? 'type' : keywords.has(name) ? 'keyword' : builtins.has(name) ? 'support.function' : ['PI','ROW','COLUMN'].includes(name) ? 'variable.readonly' : 'identifier';
          emit(position + word[0].length, type); continue;
        }
        const operator = rest.match(/^(?:<>|<=|>=|<<|>>|:=|[=<>+\-*\/\\^&])/);
        if (operator) { emit(position + operator[0].length, 'operator'); continue; }
        if (char === '(') parentheses++;
        if (char === ')') parentheses--;
        if (hole && char === '{') braces++;
        if (hole && char === '}') braces--;
        if ('(),;:{}[]'.includes(char)) { emit(position + 1, 'delimiter'); continue; }
        const whitespace = rest.match(/^\s+/);
        if (whitespace) { emit(position + whitespace[0].length, 'white'); continue; }
        emit(position + 1, 'plain');
      }
    }
    code(false);
    return tokens;
  }
  function semanticCategories(source, tokens) {
    const lines = source.split(/\r\n|\r|\n/);
    const starts = [0];
    for (const match of source.matchAll(/\r\n|\r|\n/g)) starts.push(match.index + match[0].length);
    const tokenLines = lines.map(() => []);
    let line = 0;
    for (const token of tokens) {
      while (line + 1 < starts.length && starts[line + 1] <= token.start) line++;
      let row = line;
      while (row < starts.length && starts[row] < token.end) {
        tokenLines[row].push({ offset: Math.max(0, token.start - starts[row]), type: token.type });
        row++;
      }
    }
    const classified = window.AxiomSemanticColors?.classify(source, tokenLines);
    const categories = new Map();
    if (!classified) return categories;
    const names = window.AxiomSemanticColors.legend.tokenTypes;
    let row = 0; let column = 0;
    for (let i = 0; i < classified.data.length; i += 5) {
      const delta = classified.data[i]; row += delta;
      column = delta ? classified.data[i + 1] : column + classified.data[i + 1];
      const category = names[classified.data[i + 3]] + (classified.data[i + 4] ? '.readonly' : '');
      categories.set(starts[row] + column, category);
    }
    return categories;
  }
  function highlight(element) {
    const source = element.textContent;
    const tokens = tokenize(source);
    const semantic = semanticCategories(source, tokens);
    const fragment = document.createDocumentFragment();
    for (const token of tokens) {
      if (token.type === 'white' || token.type === 'plain') { fragment.append(document.createTextNode(token.text)); continue; }
      const span = document.createElement('span');
      const category = token.type === 'identifier' ? semantic.get(token.start) || 'identifier' : token.type;
      span.className = 'axiom-token--' + category.replaceAll('.', '-');
      span.textContent = token.text;
      fragment.append(span);
    }
    element.replaceChildren(fragment);
  }
  window.AxiomManualHighlight = { tokenize };
  if (typeof document !== 'undefined') document.querySelectorAll('pre code.language-axiom').forEach(highlight);
})();
