(function(root) {
  const legend = { tokenTypes: ['variable', 'function', 'type', 'array'], tokenModifiers: ['readonly'] };
  function classify(source, tokenLines) {
    const lines = source.split(/\r\n|\r|\n/);
    const scopes = [new Map()];
    const scopeAt = []; const globals = scopes[0];
    const codeLines = lines.map((line, n) => {
      const tokens = tokenLines[n] || [];
      return tokens.map((t, k) => /^(comment|string)/.test(t.type) ? ' '.repeat((tokens[k + 1]?.offset || line.length) - t.offset) : line.slice(t.offset, tokens[k + 1]?.offset || line.length)).join('');
    });
    let scope = globals;
    codeLines.forEach((line, n) => {
      const proc = line.match(/^\s*(SUB|FUNCTION)\s+([A-Za-z_]\w*)\s*\((.*)\)/i);
      if (proc) {
        globals.set(proc[2].toUpperCase(), [1, 0]); scope = new Map(); scopes.push(scope);
        for (const match of proc[3].matchAll(/(?:^|,)\s*([A-Za-z_]\w*)\s*(\()?/g)) scope.set(match[1].toUpperCase(), [match[2] ? 3 : 0, 0]);
      }
      scopeAt[n] = scope;
      for (const match of line.matchAll(/\b(DIM|CONST)\s+([A-Za-z_]\w*)\s*(\()?/gi)) scope.set(match[2].toUpperCase(), [match[3] ? 3 : 0, /^CONST$/i.test(match[1]) ? 1 : 0]);
      const type = line.match(/^\s*TYPE\s+([A-Za-z_]\w*)/i); if (type) globals.set(type[1].toUpperCase(), [2, 0]);
      if (/^\s*END\s+(SUB|FUNCTION)\b/i.test(line)) scope = globals;
    });
    const data = []; let previousLine = 0, previousStart = 0;
    tokenLines.forEach((tokens, n) => tokens.forEach((token, k) => {
      if (!/^identifier(?:\.axiom)?$/.test(token.type)) return;
      const start = token.offset; const end = tokens[k + 1]?.offset ?? lines[n].length;
      for (const match of lines[n].slice(start, end).matchAll(/[A-Za-z_]\w*/g)) {
        const offset = start + match.index;
        const category = scopeAt[n]?.get(match[0].toUpperCase()) || globals.get(match[0].toUpperCase()) || [0, 0];
        data.push(n - previousLine, n === previousLine ? offset - previousStart : offset, match[0].length, category[0], category[1]);
        previousLine = n; previousStart = offset;
      }
    }));
    return { data: new Uint32Array(data) };
  }
  const api = { legend, classify };
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.AxiomSemanticColors = api;
})(typeof window === 'undefined' ? globalThis : window);
