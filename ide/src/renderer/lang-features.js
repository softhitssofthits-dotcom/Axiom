// Axiom IDE — language features: completion + signature help + hover.
// Data source: the language registry via `axiom metadata` (keywords / typeNames / builtins with
// signatures), cached in ide.langMeta. Document context (procedures, types, variables) is a
// light client-side scan of the model — no semantics are invented here: everything offered is
// either from the registry or from the user's own document.
const LangFeatures = (() => {

  // ---- light document scan (BASIC) ----

  // Strips trailing comments ('... and REM ...) and line continuations are not a thing in
  // Axiom, so a purely line-based scan is enough for the outline and completion locals.
  function codeOf(line) {
    let text = line;
    const rem = text.search(/(\s|^)rem(\s|$)/i);
    if (rem >= 0) text = text.slice(0, rem);
    const apostrophe = text.indexOf("'");
    if (apostrophe >= 0) text = text.slice(0, apostrophe);
    return text;
  }

  function scanDoc(model) {
    const res = { subs: [], functions: [], types: [], vars: [] };
    const lines = model.getValue().replace(/\r\n/g, '\n').split('\n');
    for (const [lineIndex, raw] of lines.entries()) {
      const line = codeOf(raw).trim();
      if (!line) continue;
      let m;
      if ((m = line.match(/^SUB\s+([A-Za-z_]\w*)\s*\(([^)]*)\)/i))) {
        res.subs.push({ name: m[1], params: parseParams(m[2]), lineNo: lineIndex + 1, signature: 'SUB ' + m[1] + '(' + m[2].trim() + ')' });
        continue;
      }
      if ((m = line.match(/^FUNCTION\s+([A-Za-z_]\w*)\s*\(([^)]*)\)\s*(?:AS\s+(\w+))?/i))) {
        res.functions.push({
          name: m[1], params: parseParams(m[2]), returnType: m[3] || null, lineNo: lineIndex + 1,
          signature: 'FUNCTION ' + m[1] + '(' + m[2].trim() + ')' + (m[3] ? ' AS ' + m[3] : ''),
        });
        continue;
      }
      if ((m = line.match(/^TYPE\s+([A-Za-z_]\w*)/i))) {
        const type = { name: m[1], fields: [], lineNo: lineIndex + 1 };
        // TYPE fields live on the following lines until END TYPE.
        for (let j = lineIndex + 1; j < lines.length; j++) {
          const inner = codeOf(lines[j]).trim();
          if (/^END\s+TYPE/i.test(inner)) break;
          const f = inner.match(/^([A-Za-z_]\w*)\s+AS\s+([A-Za-z_]\w*)/i);
          if (f) type.fields.push({ name: f[1], type: f[2] });
        }
        res.types.push(type);
        continue;
      }
      if ((m = line.match(/^(?:DIM|CONST)\s+([A-Za-z_]\w*)\s*(?:\(|\s+AS\s+([A-Za-z_]\w*))?/i))) {
        res.vars.push({ name: m[1], type: m[2] || null, lineNo: lineIndex + 1 });
      }
    }
    return res;
  }

  // 'a AS INTEGER, b AS STRING' -> [{name, type}]
  function parseParams(paramText) {
    const params = [];
    for (const seg of String(paramText || '').split(',')) {
      const t = seg.trim();
      if (!t) continue;
      const m = t.match(/^([A-Za-z_]\w*)\s*(?:AS\s+([A-Za-z_]\w*))?$/i);
      if (m) params.push({ name: m[1], type: m[2] || '' });
      else params.push({ name: t, type: '' });
    }
    return params;
  }

  // Registry builtins come one entry per overload: {name, params:[{name,type}], returnType}.
  // Signature string in the BASIC style: NAME(arg AS TYPE, ...) AS RET.
  function builtinSignature(b) {
    const args = (b.params || []).map((p) => p.name + (p.type ? ' AS ' + p.type : '')).join(', ');
    const ret = b.returnType && b.returnType !== 'VOID' ? ' AS ' + b.returnType : '';
    return (b.name || '') + '(' + args + ')' + ret;
  }

  function builtinFamilies(meta) {
    const families = new Map();
    for (const b of (meta && Array.isArray(meta.builtins)) ? meta.builtins : []) {
      if (!b || typeof b.name !== 'string') continue;
      if (!families.has(b.name)) families.set(b.name, []);
      families.get(b.name).push(b);
    }
    return families;
  }

  // Monaco can return a word at any character position, including comments and the text of a
  // string literal. Axiom hovers describe language code only, so comment/string interiors are
  // excluded with a small scanner (no full parse).
  function isCodePosition(model, position) {
    const line = model.getLineContent(position.lineNumber);
    const limit = Math.max(0, position.column - 1);
    let inString = false;
    let inInterpolation = false;
    for (let i = 0; i < limit; i++) {
      const ch = line[i];
      const next = line[i + 1];
      if (inString && !inInterpolation) {
        if (ch === '"' && next === '"') { i++; continue; } // doubled quote — escaped
        if (ch === '"') inString = false;
        continue;
      }
      if (inInterpolation) {
        if (ch === '}') inInterpolation = false;
        continue;
      }
      if (ch === "'") return false; // comment — the rest of the line
      if (/\s|^/.test('') && line.slice(i, i + 4).match(/^REM(\s|$)/i)) return false;
      if (ch === '"') inString = true;
      else if (ch === '{') inString = true, inInterpolation = true; // inside $"..."
    }
    return !inString;
  }

  // ---- registration ----

  // Structural templates in the BASIC block style. Labels/details go through I18N.
  function snippets() {
    return [
      { label: I18N.t('snippet.if'), insert: 'IF ${1:condition} THEN\n\t$0\nEND IF', detail: I18N.t('snippet.if.d') },
      { label: I18N.t('snippet.ifelse'), insert: 'IF ${1:condition} THEN\n\t$2\nELSE\n\t$0\nEND IF', detail: I18N.t('snippet.ifelse.d') },
      { label: I18N.t('snippet.while'), insert: 'WHILE ${1:condition}\n\t$0\nEND WHILE', detail: I18N.t('snippet.while.d') },
      { label: I18N.t('snippet.for'), insert: 'FOR ${1:i} = ${2:1} TO ${3:10}\n\t$0\nNEXT ${1:i}', detail: I18N.t('snippet.for.d') },
      { label: I18N.t('snippet.do'), insert: 'DO\n\t$0\nLOOP WHILE ${1:condition}', detail: I18N.t('snippet.do.d') },
      { label: I18N.t('snippet.select'), insert: 'SELECT CASE ${1:x}\n\tCASE ${2:value}\n\t\t$0\n\tCASE ELSE\nEND SELECT', detail: I18N.t('snippet.select.d') },
      { label: I18N.t('snippet.sub'), insert: 'SUB ${1:Name}()\n\t$0\nEND SUB', detail: I18N.t('snippet.sub.d') },
      { label: I18N.t('snippet.function'), insert: 'FUNCTION ${1:Name}() AS ${2:INTEGER}\n\t$3\n\tRETURN ${4:value}\nEND FUNCTION', detail: I18N.t('snippet.function.d') },
      { label: I18N.t('snippet.type'), insert: 'TYPE ${1:Name}\n\t${2:field} AS ${3:INTEGER}\nEND TYPE', detail: I18N.t('snippet.type.d') },
      { label: I18N.t('snippet.trycatch'), insert: 'TRY\n\t$1\nCATCH\n\t$0\nEND TRY', detail: I18N.t('snippet.trycatch.d') },
      { label: I18N.t('snippet.input'), insert: 'INPUT "${1:Введите значение: }"; ${2:x}', detail: I18N.t('snippet.input.d') },
    ];
  }

  function register(monaco, ide) {
    const getMeta = () => ide.langMeta;
    const fallbackNames = (key) => (ide.FALLBACK_DICTS && ide.FALLBACK_DICTS[key]) || [];
    const K = monaco.languages.CompletionItemKind;
    const KIND = { Function: K.Function, Method: K.Method, Field: K.Field, Variable: K.Variable, Keyword: K.Keyword, Constant: K.Constant, Struct: K.Struct, Type: K.Class, Snippet: K.Snippet };
    const Snippet = monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet;

    const completionProvider = {
      provideCompletionItems(model, position) {
        const word = model.getWordUntilPosition(position);
        const range = { startLineNumber: position.lineNumber, endLineNumber: position.lineNumber, startColumn: word.startColumn, endColumn: word.endColumn };
        const meta = getMeta();
        const doc = scanDoc(model);
        const items = [];
        const seen = new Set();
        const add = (label, kind, insertText, snippet, detail, sort) => {
          const key = kind + ':' + label;
          if (seen.has(key)) return;
          seen.add(key);
          items.push({
            label, kind: KIND[kind], insertText, detail,
            insertTextRules: snippet ? Snippet : undefined, sortText: sort + label.toLowerCase(),
          });
        };

        // General context: registry builtins (one suggestion per family, first overload
        // signature in the detail), document symbols, keywords and types.
        const families = builtinFamilies(meta);
        const fallbackBuiltins = fallbackNames('builtins').map((n) => ({ name: n }));
        for (const b of (meta && Array.isArray(meta.builtins)) ? meta.builtins : fallbackBuiltins) {
          if (!b || typeof b.name !== 'string') continue;
          add(b.name, 'Function', b.name + '($0)', true, families.size ? builtinSignature(b) : '', '0');
        }
        for (const v of doc.vars) add(v.name, 'Variable', v.name, false, v.type ? v.type : '', '1');
        for (const f of doc.functions) add(f.name, 'Function', f.name + '($0)', true, f.signature, '1');
        for (const s of doc.subs) add(s.name, 'Function', s.name + ' $0', false, s.signature, '1');
        for (const t of doc.types) add(t.name, 'Struct', t.name, false, I18N.t('hover.userType'), '2');
        for (const t of (meta && meta.typeNames) || fallbackNames('typeNames')) add(t, 'Type', t, false, '', '2');
        for (const kw of (meta && meta.keywords) || fallbackNames('keywords')) add(kw, 'Keyword', kw, false, '', '3');
        for (const s of snippets()) add(s.label, 'Snippet', s.insert, true, s.detail || '', '4');
        return { suggestions: items.map((i) => ({ ...i, range })) };
      },
    };
    monaco.languages.registerCompletionItemProvider('axiom', completionProvider);
    LangFeatures._completionProvider = completionProvider; // exposed for diagnostics

    const signatureProvider = {
      signatureHelpTriggerCharacters: ['(', ','],
      provideSignatureHelp(model, position) {
        const line = model.getValueInRange({ startLineNumber: position.lineNumber, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column });
        const empty = { signatures: [], activeSignature: 0, activeParameter: 0 };
        let depth = 0;
        let callStart = -1;
        for (let i = line.length - 1; i >= 0; i--) {
          const ch = line[i];
          if (ch === ')') depth += 1;
          else if (ch === '(') {
            if (depth === 0) { callStart = i; break; }
            depth -= 1;
          }
        }
        const nm = callStart >= 0 ? line.slice(0, callStart).match(/([A-Za-z_]\w*)\s*$/) : null;
        if (!nm) return { value: empty, dispose() {} };
        const fnName = nm[1];
        const meta = getMeta();
        const active = (line.slice(callStart + 1).match(/,/g) || []).length;

        // 1. Builtin overload family from the registry — every overload becomes a signature.
        const family = meta && Array.isArray(meta.builtins)
          ? meta.builtins.filter((x) => x && x.name && x.name.toUpperCase() === fnName.toUpperCase())
          : [];
        if (family.length) {
          const signatures = family.map((b) => {
            const sig = builtinSignature(b);
            const params = (b.params || []).map((p) => ({ label: p.name + (p.type ? ' AS ' + p.type : '') }));
            return { label: sig, parameters: params };
          });
          const first = family[0];
          const count = (first.params || []).length;
          return {
            value: {
              signatures,
              activeSignature: 0,
              activeParameter: count ? Math.min(active, count - 1) : 0,
            },
            dispose() {},
          };
        }

        // 2. User-defined FUNCTION/SUB from the document.
        const doc = scanDoc(model);
        const user = doc.functions.find((f) => f.name.toUpperCase() === fnName.toUpperCase())
          || doc.subs.find((s) => s.name.toUpperCase() === fnName.toUpperCase());
        if (user) {
          const paramLabels = user.params.map((p) => (p.type ? p.name + ' AS ' + p.type : p.name));
          return {
            value: {
              signatures: [{ label: user.signature, parameters: paramLabels.map((p) => ({ label: p })) }],
              activeSignature: 0,
              activeParameter: user.params.length ? Math.min(active, user.params.length - 1) : 0,
            },
            dispose() {},
          };
        }

        return { value: empty, dispose() {} };
      },
    };
    monaco.languages.registerSignatureHelpProvider('axiom', signatureProvider);
    LangFeatures._sigProvider = signatureProvider; // exposed for diagnostics

    // Hover provider: builtin signatures, user symbols, keywords and types.
    const hoverProvider = {
      provideHover(model, position) {
        if (!isCodePosition(model, position)) return null;
        const word = model.getWordAtPosition(position);
        if (!word || !word.word) return null;
        const name = word.word;
        const range = {
          startLineNumber: position.lineNumber, startColumn: word.startColumn,
          endLineNumber: position.lineNumber, endColumn: word.endColumn,
        };
        const meta = getMeta();
        const docScan = scanDoc(model);
        const overlapsErrorMarker = monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri }).some((marker) =>
          marker.severity === monaco.MarkerSeverity.Error
          && marker.startLineNumber === position.lineNumber
          && marker.endLineNumber === position.lineNumber
          && marker.startColumn < word.endColumn
          && marker.endColumn > word.startColumn);
        // Monaco already renders compiler errors in its marker hover. Suppress the normal
        // language card here so an invalid declaration does not look like a valid builtin.
        if (overlapsErrorMarker) return null;

        // 1. Builtin family: all overloads, one signature per line.
        const family = meta && Array.isArray(meta.builtins)
          ? meta.builtins.filter((x) => x && x.name && x.name.toUpperCase() === name.toUpperCase())
          : [];
        if (family.length) {
          const lines = family.map((b) => '`' + builtinSignature(b) + '`');
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.builtin') + '\n\n' + lines.join('\n\n') }] };
        }

        // 2. User-defined FUNCTION.
        const fn = docScan.functions.find((f) => f.name.toUpperCase() === name.toUpperCase());
        if (fn) {
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.function') + '\n\n`' + fn.signature + '`' }] };
        }

        // 3. User-defined SUB.
        const sub = docScan.subs.find((s) => s.name.toUpperCase() === name.toUpperCase());
        if (sub) {
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.sub') + '\n\n`' + sub.signature + '`' }] };
        }

        // 4. Variables (DIM/CONST).
        const v = [...docScan.vars].reverse().find((x) => x.name.toUpperCase() === name.toUpperCase());
        if (v) {
          const type = v.type || I18N.t('hover.typeLabel');
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.variable') + '\n\n' + I18N.t('hover.typeLabel') + ': `' + type + '`' }] };
        }

        // 5. User-defined TYPE.
        const type = docScan.types.find((x) => x.name.toUpperCase() === name.toUpperCase());
        if (type) {
          const fields = type.fields.length
            ? type.fields.map((f) => f.name + ' AS ' + f.type).join(', ')
            : I18N.t('hover.noFields');
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.userType') + '\n\n' + I18N.t('hover.fields') + ': ' + fields }] };
        }

        // 6. Registry type names and keywords.
        if (meta && Array.isArray(meta.typeNames) && meta.typeNames.some((t) => t.toUpperCase() === name.toUpperCase())) {
          return { range, contents: [{ value: '`' + name + '` — ' + I18N.t('hover.type') }] };
        }
        if (meta && Array.isArray(meta.keywords) && meta.keywords.some((k) => k.toUpperCase() === name.toUpperCase())) {
          return { range, contents: [{ value: '`' + name + '` — ' + I18N.t('hover.keyword') }] };
        }

        return null;
      },
    };
    monaco.languages.registerHoverProvider('axiom', hoverProvider);
    LangFeatures._hoverProvider = hoverProvider; // exposed for diagnostics
  }

  return { register, scanDoc, builtinSignature };
})();

window.LangFeatures = LangFeatures;
