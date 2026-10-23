(function(root) {
  const legend = { tokenTypes: ['variable', 'function', 'type', 'array', 'keyword'], tokenModifiers: ['readonly'] };
  function arrayType(type) {
    if (/^FUNCTION\b/i.test(type || '')) return null;
    if (typeof type !== 'string') return null;
    const match = type.trim().match(/^(.*?)\s*(?:\[([\s,]*)\]|\(([\s,]*)\))$/);
    if (!match || !match[1].trim()) return null;
    return { elementType: match[1].trim(), rank: ((match[2] ?? match[3]).match(/,/g) || []).length + 1 };
  }
  function parameterList(line, open) {
    let depth = 0;
    for (let i = open; i < line.length; i++) {
      if (line[i] === '(') depth++;
      else if (line[i] === ')' && --depth === 0) return line.slice(open + 1, i);
    }
    return '';
  }
  function parameterSegments(text) {
    const result = []; let start = 0, depth = 0;
    for (let i = 0; i < text.length; i++) {
      if ('([{'.includes(text[i])) depth++;
      else if (')]}'.includes(text[i])) depth--;
      else if (text[i] === ',' && depth === 0) {result.push(text.slice(start, i)); start = i + 1;}
    }
    result.push(text.slice(start)); return result;
  }
  function declaredCategory(text) {
    const match = text.match(/^\s*(?:(?:BYREF|BYVAL)\s+)?([A-Za-z_]\w*)\s*([\[(])?/i);
    if (!match) return null;
    const explicitType = text.match(/\bAS\s+(.+?)(?:\s*=|$)/i)?.[1];
    return {name: match[1].toUpperCase(), category: [match[2] || arrayType(explicitType) ? 3 : 0, 0]};
  }
  function classify(source, tokenLines, symbols = []) {
    const lines = source.split(/\r\n|\r|\n/);
    const scopes = [new Map()];
    const scopeAt = []; const globals = scopes[0];
    const codeLines = lines.map((line, n) => {
      const tokens = tokenLines[n] || [];
      return tokens.map((t, k) => /^(comment|string)/.test(t.type) ? ' '.repeat((tokens[k + 1]?.offset || line.length) - t.offset) : line.slice(t.offset, tokens[k + 1]?.offset || line.length)).join('');
    });
    let scope = globals;
    codeLines.forEach((line, n) => {
      const proc = line.match(/^\s*(?:(?:PUBLIC|PRIVATE)\s+)?(SUB|FUNCTION)\s+([A-Za-z_]\w*)\s*\(/i);
      if (proc) {
        globals.set(proc[2].toUpperCase(), [1, 0]); scope = new Map(); scopes.push(scope);
        for (const segment of parameterSegments(parameterList(line, proc[0].length - 1))) {
          const item = declaredCategory(segment);
          if (item) scope.set(item.name, item.category);
        }
      }
      scopeAt[n] = scope;
      const variable = line.match(/^\s*(?:(?:PUBLIC|PRIVATE)\s+)?(DIM|CONST)\s+(.+)$/i);
      if (variable) {
        for (const segment of parameterSegments(variable[2])) {
          const item = declaredCategory(segment);
          if (item) scope.set(item.name, [item.category[0], /^CONST$/i.test(variable[1]) ? 1 : 0]);
        }
      }
      const type = line.match(/^\s*(?:(?:PUBLIC|PRIVATE)\s+)?TYPE\s+([A-Za-z_]\w*)/i); if (type) globals.set(type[1].toUpperCase(), [2, 0]);
      if (/^\s*END\s+(SUB|FUNCTION)\b/i.test(line)) scope = globals;
    });
    const semanticSymbols = new Map(symbols.map(symbol => [symbol.start, symbol]));
    const lineStarts = []; let absolute = 0;
    for (const line of source.matchAll(/[^\r\n]*(?:\r\n|\r|\n|$)/g)) {
      if (line[0].length === 0) break;
      lineStarts.push(absolute); absolute += line[0].length;
    }
    const data = []; let previousLine = 0, previousStart = 0;
    tokenLines.forEach((tokens, n) => tokens.forEach((token, k) => {
      const start = token.offset; const end = tokens[k + 1]?.offset ?? lines[n].length;
      const tokenKind = token.type === 'keyword.axiom' ? 4 : /^identifier(?:\.axiom)?$/.test(token.type) ? 0 : -1;
      if (tokenKind < 0) return;
      for (const match of lines[n].slice(start, end).matchAll(/[A-Za-z_]\w*/g)) {
        const offset = start + match.index;
        let category = scopeAt[n]?.get(match[0].toUpperCase()) || globals.get(match[0].toUpperCase()) || [0, 0];
        const symbol = semanticSymbols.get((lineStarts[n] || 0) + offset);
        if (symbol && symbol.length === match[0].length) {
          const kind = symbol.kind;
          category = ['function', 'sub'].includes(kind) ? [1, 0]
            : kind === 'userType' ? [2, 0]
            : symbol.arrayRank || arrayType(symbol.type) ? [3, 0]
            : [0, ['constant', 'errorVariable'].includes(kind) ? 1 : 0];
        }
        data.push(n - previousLine, n === previousLine ? offset - previousStart : offset, match[0].length,
          tokenKind === 4 ? 4 : category[0], tokenKind === 4 ? 0 : category[1]);
        previousLine = n; previousStart = offset;
      }
    }));
    return { data: new Uint32Array(data) };
  }
  const api = { legend, classify, arrayType };
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.AxiomSemanticColors = api;
})(typeof window === 'undefined' ? globalThis : window);
