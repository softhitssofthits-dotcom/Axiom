// Axiom IDE — language features: completion + signature help + hover.
// Data source: the language registry via `axiom metadata` (keywords / typeNames / builtins with
// signatures), cached in ide.langMeta. Document context (procedures, types, variables) is a
// document scan for completion. Hover symbols come from the compiler's semantic analysis.
const LangFeatures = (() => {

  // ---- light document scan (BASIC) ----

  // Strips trailing comments ('... and REM ...) and line continuations are not a thing in
  // Axiom, so a purely line-based scan is enough for the outline and completion locals.
  function codeOf(line) {
    const { commentStart } = lexicalCode(line);
    return line.slice(0, commentStart);
  }

  // Preserve offsets while hiding literal text, comments and interpolation formats.
  function lexicalCode(line) {
    const mask = line.split('');
    const stack = [{ kind: 'code' }];
    let commentStart = line.length;
    for (let i = 0; i < line.length; i++) {
      const state = stack[stack.length - 1], ch = line[i], next = line[i + 1];
      if (state.kind === 'string') {
        mask[i] = ' ';
        if (ch === '"' && next === '"') { mask[++i] = ' '; continue; }
        if (ch === '"') stack.pop();
        else if (state.interpolated && (ch === '{' || ch === '}') && next === ch) { mask[++i] = ' '; }
        else if (ch === '{' && state.interpolated) { mask[i] = '{'; stack.push({kind: 'expression', depth: 0, bracketDepth: 0}); }
        continue;
      }
      if (state.kind === 'format') {
        mask[i] = ' ';
        if (ch === '}') { mask[i] = '}'; stack.pop(); }
        continue;
      }
      if (state.kind === 'expression' && ch === '}' && state.depth === 0 && state.bracketDepth === 0) {
        mask[i] = '}'; stack.pop(); continue;
      }
      if (ch === '"') { mask[i] = ' '; stack.push({kind: 'string', interpolated: line[i - 1] === '$'}); continue; }
      if (ch === "'" || (/^REM\b/i.test(line.slice(i)) && (i === 0 || !/[A-Za-z0-9_]/.test(line[i - 1])))) {
        commentStart = i; mask.fill(' ', i); break;
      }
      if (state.kind === 'expression') {
        if (ch === '(') state.depth++;
        if (ch === ')') state.depth = Math.max(0, state.depth - 1);
        if (ch === '[') state.bracketDepth++;
        if (ch === ']') state.bracketDepth = Math.max(0, state.bracketDepth - 1);
        if (ch === ':' && next !== '=' && state.depth === 0 && state.bracketDepth === 0) { mask[i] = ' '; state.kind = 'format'; }
      }
    }
    return { mask: mask.join(''), commentStart };
  }

  // Эти средства читают только форму объявления, не выводят тип начального выражения.
  function closingDelimiter(text, start) {
    const stack = [], pairs = {')': '(', ']': '['};
    const code = lexicalCode(text).mask;
    for (let i = start; i < code.length; i++) {
      const ch = code[i];
      if (ch === '(' || ch === '[') stack.push(ch);
      else if (ch === ')' || ch === ']') {
        if (stack.pop() !== pairs[ch]) return -1;
        if (!stack.length) return i;
      }
    }
    return -1;
  }

  function splitTopLevel(text, separator = ',') {
    const parts = [], code = lexicalCode(text).mask;
    let depth = 0, start = 0;
    for (let i = 0; i < code.length; i++) {
      if ('([{'.includes(code[i])) depth++;
      else if (')]}'.includes(code[i])) depth--;
      else if (code[i] === separator && depth === 0) { parts.push(text.slice(start, i)); start = i + 1; }
    }
    parts.push(text.slice(start));
    return parts;
  }

  function declaration(text) {
    const match = text.trim().match(/^(?:(BYREF|BYVAL)\s+)?([A-Za-z_]\w*)\b/i);
    if (!match) return null;
    let rest = text.trim().slice(match[0].length).trim(), arrayRank = 0, suffix = '';
    if (rest[0] === '[' || rest[0] === '(') {
      const end = closingDelimiter(rest, 0);
      if (end < 0) return {name: match[2], type: null, arrayRank: 0, label: text.trim()};
      arrayRank = splitTopLevel(rest.slice(1, end)).length;
      suffix = rest[0] + ','.repeat(arrayRank - 1) + rest[end];
      rest = rest.slice(end + 1).trim();
    }
    const typeText = /^AS\s+/i.test(rest) ? splitTopLevel(rest.replace(/^AS\s+/i, ''), '=')[0].trim() : null;
    const type = typeText ? typeText.replace(/\s*\[([\s,]*)\]\s*$/, (_, rank) => '[' + rank.replace(/\s/g, '') + ']')
      .replace(/\s*\(([\s,]*)\)\s*$/, (_, rank) => '(' + rank.replace(/\s/g, '') + ')') + suffix : null;
    const typeSuffix = type && !/^FUNCTION\b/i.test(type) ? type.match(/(?:\[([\s,]*)\]|\(([\s,]*)\))$/) : null;
    const typeRank = typeSuffix ? ((typeSuffix[1] ?? typeSuffix[2]).match(/,/g) || []).length + 1 : 0;
    return {name: match[2], type, arrayRank: arrayRank || typeRank,
      squareArray: suffix.startsWith('[') || (!suffix && typeSuffix?.[1] !== undefined),
      label: text.trim()};
  }

  function scanDoc(model) {
    const res = { subs: [], functions: [], types: [], vars: [] };
    const lines = model.getValue().split(/\r\n|\r|\n/);
    for (const [lineIndex, raw] of lines.entries()) {
      const line = codeOf(raw).trim().replace(/^(?:PUBLIC|PRIVATE)\s+/i, '');
      if (!line) continue;
      let m;
      if ((m = line.match(/^(SUB|FUNCTION)\s+([A-Za-z_]\w*)\s*\(/i))) {
        const open = m[0].length - 1, close = closingDelimiter(line, open);
        if (close < 0) continue;
        const paramText = line.slice(open + 1, close).trim();
        const result = line.slice(close + 1).trim().match(/^AS\s+(.+)$/i)?.[1] || null;
        const kind = m[1].toUpperCase();
        const routine = {name: m[2], params: parseParams(paramText), lineNo: lineIndex + 1,
          signature: kind + ' ' + m[2] + '(' + paramText + ')' + (kind === 'FUNCTION' && result ? ' AS ' + result : '')};
        if (kind === 'FUNCTION') res.functions.push({...routine, returnType: result});
        else res.subs.push(routine);
        continue;
      }
      if ((m = line.match(/^TYPE\s+([A-Za-z_]\w*)/i))) {
        const type = { name: m[1], fields: [], lineNo: lineIndex + 1 };
        // TYPE fields live on the following lines until END TYPE.
        for (let j = lineIndex + 1; j < lines.length; j++) {
          const inner = codeOf(lines[j]).trim();
          if (/^END\s+TYPE/i.test(inner)) break;
          const field = declaration(inner);
          if (field?.type) type.fields.push({name: field.name, type: field.type});
        }
        res.types.push(type);
        continue;
      }
      if ((m = line.match(/^(?:DIM|CONST)\s+(.+)$/i))) {
        for (const segment of splitTopLevel(m[1])) {
          const item = declaration(segment);
          if (item) res.vars.push({...item, type: item.type || null, lineNo: lineIndex + 1});
        }
      }
    }
    const declarations = [...res.subs.map(item => ({item, kind:'SUB'})), ...res.functions.map(item => ({item, kind:'FUNCTION'})), ...res.types.map(item => ({item, kind:'TYPE'}))].sort((a,b) => a.item.lineNo-b.item.lineNo);
    const codeLines=lines.map(raw=>codeOf(raw).trim().replace(/^(?:PUBLIC|PRIVATE)\s+/i, ''));
    for (let index=0; index<declarations.length; index++) {
      const {item,kind}=declarations[index];
      const limit=(declarations[index+1]?.item.lineNo || lines.length+1)-1;
      item.endLineNo=limit;
      const nested=[kind];
      for(let row=item.lineNo; row<lines.length; row++) {
        const start=codeLines[row].match(/^(SUB|FUNCTION|TYPE)\s+[A-Za-z_]\w*/i);
        if(start)nested.push(start[1].toUpperCase());
        const end=codeLines[row].match(/^END\s+(SUB|FUNCTION|TYPE)\b/i);
        if(end&&end[1].toUpperCase()===nested.at(-1)){
          nested.pop();
          if(!nested.length){item.endLineNo=row+1;break;}
        }
      }
      const raw=lines[item.lineNo-1], comment=lexicalCode(raw).commentStart;
      item.description=raw.slice(comment).replace(/^(?:'|REM\s*)/i,'').trim();
    }
    for(const {item} of declarations){
      item.scope=declarations.filter(d=>d.item.lineNo<item.lineNo&&d.item.endLineNo>=item.endLineNo).map(d=>d.item.name);
    }
    // Разделы панели содержат целые объявления; внутренние метки остаются
    // комментариями и могут отдельно сворачиваться средствами редактора.
    res.regions = []; res.regionWarnings = [];
    const stack = [], occurrences = new Map();
    let declarationIndex = 0;
    for (let row = 0; row < lines.length; row++) {
      const lineNo = row + 1;
      while (declarationIndex < declarations.length && declarations[declarationIndex].item.endLineNo < lineNo) declarationIndex++;
      const declaration = declarations[declarationIndex]?.item;
      if (declaration && lineNo >= declaration.lineNo && lineNo <= declaration.endLineNo) continue;
      const open = lines[row].match(/^\s*'\s*#region(?:\s+(.*?))?\s*$/i);
      if (open) {
        const name = (open[1] || '').trim();
        if (!name) { res.regionWarnings.push({kind:'unnamed',lineNo}); continue; }
        const parentId = stack.at(-1)?.id || null;
        const key = JSON.stringify([parentId, name.toLowerCase()]);
        const ordinal = (occurrences.get(key) || 0) + 1; occurrences.set(key, ordinal);
        const path = [...(stack.at(-1)?.path || []), [name.toLowerCase(),ordinal]];
        const region = {id:JSON.stringify(path), path, parentId, name, lineNo, endLineNo:lines.length, closed:false};
        res.regions.push(region); stack.push(region);
      } else if (/^\s*'\s*#endregion\s*$/i.test(lines[row])) {
        const region = stack.pop();
        if (region) { region.endLineNo = lineNo; region.closed = true; }
        else res.regionWarnings.push({kind:'orphan',lineNo});
      }
    }
    for (const region of stack) res.regionWarnings.push({kind:'unclosed',lineNo:region.lineNo});
    for (const {item} of declarations) {
      item.regionId = res.regions.filter(r => r.lineNo < item.lineNo && r.endLineNo >= item.endLineNo).at(-1)?.id || null;
    }
    return res;
  }

  // 'a AS INTEGER, b AS STRING' -> [{name, type}]
  function parseParams(paramText) {
    const params = [];
    for (const seg of splitTopLevel(String(paramText || ''))) {
      const t = seg.trim();
      if (!t) continue;
      params.push(declaration(t) || { name: t, type: '', label: t });
    }
    return params;
  }

  // Registry builtins come one entry per overload: {name, params:[{name,type}], returnType}.
  // Signature string in the BASIC style: NAME(arg AS TYPE, ...) AS RET.
  function displayType(type) {
    if (typeof I18N === 'undefined' || I18N.current !== 'en') return type;
    return ({
      'элемент или массив T': 'element or T array',
      'INTEGER или INTEGER[]': 'INTEGER or INTEGER[]',
      'INTEGER[] или INTEGER[,]': 'INTEGER[] or INTEGER[,]',
      'массив результатов selector': 'array of selector results',
      'тип начального значения': 'initial value type',
      'общий числовой тип': 'common numeric type',
    })[type] || type;
  }
  function builtinSignature(b) {
    const args = (b.params || []).map((p) => {
      const type = displayType(p.displayType || p.type);
      return p.name + (type ? ' AS ' + type : '');
    }).join(', ');
    const returnType = displayType(b.displayReturnType || b.returnType);
    const ret = returnType && returnType !== 'VOID' ? ' AS ' + returnType : '';
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
    return /\S/.test(lexicalCode(line).mask[position.column - 1] || '');
  }

  // ---- registration ----

  // Structural templates in the BASIC block style. Labels/details go through I18N.
  function snippets(visibility) {
    const templates = [
      { label: '#region', insert: "' #region ${1:" + I18N.t('outline.regionName') + "}\n$0\n' #endregion", detail: I18N.t('outline.regionSnippet') },
      { label: I18N.t('snippet.if'), insert: 'IF ${1:condition} THEN\n\t$0\nEND IF', detail: I18N.t('snippet.if.d') },
      { label: I18N.t('snippet.ifsingle'), insert: 'IF ${1:condition} THEN ${2:PRINT "yes"}$0', detail: I18N.t('snippet.ifsingle.d') },
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
    return templates.flatMap(template => {
      if (!/^(?:SUB|FUNCTION|TYPE)\s/.test(template.insert)) return [template];
      // Уже введённый модификатор остаётся вне диапазона замены Monaco.
      if (visibility) return [{ ...template, label: visibility + ' ' + template.label }];
      return [template, ...['PUBLIC', 'PRIVATE'].map(modifier => ({
        ...template, label: modifier + ' ' + template.label, insert: modifier + ' ' + template.insert,
      }))];
    });
  }

  function register(monaco, ide) {
    const getMeta = () => ide.langMeta;
    const fallbackNames = (key) => (ide.FALLBACK_DICTS && ide.FALLBACK_DICTS[key]) || [];
    const K = monaco.languages.CompletionItemKind;
    const KIND = { Function: K.Function, Method: K.Method, Field: K.Field, Variable: K.Variable, Keyword: K.Keyword, Constant: K.Constant, Struct: K.Struct, Type: K.Class, Snippet: K.Snippet };
    const Snippet = monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet;

    // Члены словаря. Add, Exists, Remove, Keys и TryGetValue вызываются со
    // скобками; Count — свойство и вставляется без скобок.
    const dictionaryMembers = (type) => [
      { label: 'Add', kind: 'Method', insertText: 'Add($0)', snippet: true, detail: I18N.current === 'en' ? 'Add(key, value)' : 'Add(ключ, значение)' },
      { label: 'Exists', kind: 'Method', insertText: 'Exists($0)', snippet: true, detail: I18N.current === 'en' ? 'Exists(key) AS BOOLEAN' : 'Exists(ключ) AS BOOLEAN' },
      { label: 'Remove', kind: 'Method', insertText: 'Remove($0)', snippet: true, detail: I18N.current === 'en' ? 'Remove(key) AS BOOLEAN' : 'Remove(ключ) AS BOOLEAN' },
      { label: 'Keys', kind: 'Method', insertText: 'Keys()', snippet: false, detail: 'Keys() AS ' + (type?.keyType || (I18N.current === 'en' ? 'key' : 'ключ')) + '[]' },
      { label: 'TryGetValue', kind: 'Method', insertText: 'TryGetValue($0)', snippet: true, detail: I18N.current === 'en' ? 'TryGetValue(key, variable) AS BOOLEAN' : 'TryGetValue(ключ, переменная) AS BOOLEAN' },
      { label: 'Count', kind: 'Field', insertText: 'Count', snippet: false, detail: 'Count AS INTEGER' },
    ];

    // Члены множества. Add, Contains, Remove, ToArray, UnionWith и IntersectWith
    // вызываются со скобками; Count — свойство и вставляется без скобок.
    // UnionWith и IntersectWith — процедуры: результата у них нет.
    const setMembers = (type) => {
      const element = type?.elementType || (I18N.current === 'en' ? 'element' : 'элемент');
      const other = I18N.current === 'en' ? 'other set' : 'другое множество';
      return [
        { label: 'Add', kind: 'Method', insertText: 'Add($0)', snippet: true, detail: 'Add(' + element + ') AS BOOLEAN' },
        { label: 'Contains', kind: 'Method', insertText: 'Contains($0)', snippet: true, detail: 'Contains(' + element + ') AS BOOLEAN' },
        { label: 'Remove', kind: 'Method', insertText: 'Remove($0)', snippet: true, detail: 'Remove(' + element + ') AS BOOLEAN' },
        { label: 'ToArray', kind: 'Method', insertText: 'ToArray()', snippet: false, detail: 'ToArray() AS ' + element + '[]' },
        { label: 'UnionWith', kind: 'Method', insertText: 'UnionWith($0)', snippet: true, detail: 'UnionWith(' + other + ')' },
        { label: 'IntersectWith', kind: 'Method', insertText: 'IntersectWith($0)', snippet: true, detail: 'IntersectWith(' + other + ')' },
        { label: 'Count', kind: 'Field', insertText: 'Count', snippet: false, detail: 'Count AS INTEGER' },
      ];
    };

    // Выражение непосредственно перед точкой: имя с цепочкой полей, индексов
    // и вызовов. Разбор идёт по маскированному коду, поэтому точки в строках и
    // комментариях не считаются обращением к члену.
    function receiverExpression(code) {
      const text = code.replace(/\s+$/, '');
      const dot = text.lastIndexOf('.');
      if (dot < 0) return null;
      const head = text.slice(0, dot).replace(/\s+$/, '');
      const match = /([A-Za-z_]\w*(?:\s*(?:\[[^\[\]]*\]|\([^()]*\)|\.[A-Za-z_]\w*))*)$/.exec(head);
      return match ? match[1] : null;
    }

    // Ближайшее объявление, содержащее строку: его параметры и поля задают тип
    // имени, которое не является переменной модуля.
    function enclosingDeclaration(doc, lineNumber) {
      const routines = [
        ...doc.subs.map((item) => ({...item, kind: 'SUB'})),
        ...doc.functions.map((item) => ({...item, kind: 'FUNCTION'})),
        ...doc.types.map((item) => ({...item, kind: 'TYPE'})),
      ].filter((item) => item.lineNo <= lineNumber && (item.endLineNo || Infinity) >= lineNumber);
      return routines.sort((a, b) => b.lineNo - a.lineNo)[0] || null;
    }

    // Переменная модуля с учётом порядка объявлений: последнее объявление до
    // строки использования перекрывает прежнее.
    function declaredVariable(doc, upperName, lineNumber) {
      const matches = doc.vars.filter((item) => item.name.toUpperCase() === upperName);
      const before = matches.filter((item) => (item.lineNo || 0) <= lineNumber);
      return (before.length ? before : matches).at(-1) || null;
    }

    // Тип выражения-получателя по тем же правилам объявления, что и scanDoc:
    // параметр подпрограммы, поле записи, переменная, результат функции; далее
    // цепочка полей, индексов и вызова. Незнакомое выражение не получает тип,
    // поэтому ложные члены не появляются.
    function receiverType(doc, receiver, lineNumber) {
      const base = /^[A-Za-z_]\w*/.exec(receiver);
      if (!base) return null;
      const name = base[0], upper = name.toUpperCase();
      const owner = enclosingDeclaration(doc, lineNumber);
      let type = null, callable = false;

      if (upper === 'SELF') {
        // SELF относится к записи, внутри которой записан метод, а не к самой
        // подпрограмме метода: её объявление вложено в TYPE.
        const record = doc.types.filter((item) => item.lineNo <= lineNumber && (item.endLineNo || Infinity) >= lineNumber).at(-1);
        if (record) type = record.name;
      }
      if (!type && owner && owner.kind !== 'TYPE') {
        const parameter = owner.params?.find((item) => item.name.toUpperCase() === upper);
        if (parameter?.type) type = parameter.type;
      }
      if (!type && owner?.kind === 'TYPE') {
        const field = owner.fields?.find((item) => item.name.toUpperCase() === upper);
        if (field?.type) type = field.type;
      }
      if (!type) {
        const variable = declaredVariable(doc, upper, lineNumber);
        if (variable?.type) type = variable.type;
      }
      if (!type) {
        const routine = doc.functions.find((item) => item.name.toUpperCase() === upper);
        if (routine?.returnType) { type = routine.returnType; callable = true; }
      }
      if (!type) return null;

      const segment = /\s*(?:\.\s*([A-Za-z_]\w*)|(\[[^\[\]]*\])|(\([^()]*\)))/y;
      segment.lastIndex = base[0].length;
      for (let match = segment.exec(receiver); match; match = segment.exec(receiver)) {
        if (match[1]) {
          const record = doc.types.find((item) => item.name.toUpperCase() === String(type).toUpperCase());
          const field = record?.fields.find((item) => item.name.toUpperCase() === match[1].toUpperCase());
          if (!field?.type) return null;
          type = field.type;
        } else if (match[2]) {
          const element = window.AxiomSemanticColors?.arrayType(type);
          if (!element) return null;
          type = element.elementType;
        } else {
          if (!callable) return null;
          callable = false;
        }
      }
      // Функция без скобок не даёт значения, поэтому членов у неё нет.
      return callable ? null : type;
    }

    // Общий получатель для словаря и множества: один разбор выражения перед
    // точкой, дальше тип сравнивается с DICTIONARY(...) или HASHSET(...).
    function memberReceiver(model, position) {
      const lineText = model.getLineContent(position.lineNumber).slice(0, position.column - 1);
      const receiver = receiverExpression(lexicalCode(lineText).mask);
      if (!receiver) return null;
      const type = receiverType(scanDoc(model), receiver, position.lineNumber);
      return type ? { name: receiver, type } : null;
    }

    // Члены получателя по его объявленному типу: словарь и множество
    // различаются здесь, а не отдельными путями поиска получателя.
    function receiverMembers(type) {
      const set = setType(type);
      if (set) return setMembers(set);
      const dictionary = dictionaryType(type);
      return dictionary ? dictionaryMembers(dictionary) : null;
    }

    // DICTIONARY(ключ, значение) → типы ключа и значения. Разбор повторяет
    // текстовую форму типа и не угадывает её по имени переменной.
    function dictionaryType(type) {
      if (typeof type !== 'string') return null;
      const match = /^DICTIONARY\s*\(([^,]+),\s*(.+)\)$/i.exec(type.trim());
      return match ? { keyType: match[1].trim(), valueType: match[2].trim() } : null;
    }

    // HASHSET(элемент) → тип элемента. Регистр слова и элемента не значим,
    // поэтому разбор не зависит от того, как тип записан в объявлении.
    function setType(type) {
      if (typeof type !== 'string') return null;
      const match = /^HASHSET\s*\(\s*([^(),]+?)\s*\)$/i.exec(type.trim());
      return match ? { elementType: match[1].trim() } : null;
    }

    const completionProvider = {
      provideCompletionItems(model, position) {
        if(model.getLanguageId()!=='axiom')return {suggestions:[]};
        const word = model.getWordUntilPosition(position);
        // Отдельные диапазоны вставки и замещения делают действующей настройку
        // suggest.insertMode: в режиме insert сохраняется хвост слова после курсора,
        // в режиме replace слово замещается целиком.
        const line = position.lineNumber;
        const spanned = model.getWordAtPosition(position);
        const coversCursor = spanned && spanned.startColumn <= position.column && position.column <= spanned.endColumn;
        const range = {
          insert: { startLineNumber: line, endLineNumber: line, startColumn: word.startColumn, endColumn: position.column },
          replace: { startLineNumber: line, endLineNumber: line, startColumn: word.startColumn, endColumn: coversCursor ? Math.max(position.column, spanned.endColumn) : position.column },
        };
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

        // После точки подсказываются только члены объявленного типа получателя:
        // у словаря — методы и Count, у множества — методы, ToArray() и Count.
        // Тип получателя определяется общим разбором выражения перед точкой,
        // поэтому параметры, поля, элементы массивов и результаты функций
        // обрабатываются тем же путём, что и переменная модуля.
        const receiver = memberReceiver(model, position);
        const members = receiver && receiverMembers(receiver.type);
        if (members) {
          for (const member of members) {
            add(member.label, member.kind, member.insertText, member.snippet, member.detail, '0');
          }

          return { suggestions: items.map((i) => ({ ...i, range })) };
        }

        // General context: registry builtins (one suggestion per family, first overload
        // signature in the detail), document symbols, keywords and types.
        const families = builtinFamilies(meta);
        const fallbackBuiltins = fallbackNames('builtins').map((n) => ({ name: n }));
        for (const b of (meta && Array.isArray(meta.builtins)) ? meta.builtins : fallbackBuiltins) {
          if (!b || typeof b.name !== 'string') continue;
          add(b.name, 'Function', b.name + '($0)', true, families.size ? builtinSignature(b) : '', '0');
        }
        const suffixAtCursor = model.getLineContent(line).slice(range.replace.endColumn - 1);
        for (const v of doc.vars) add(v.name, 'Variable', v.name + (v.squareArray && !/^\s*[[(]/.test(suffixAtCursor) ? '[]' : ''), false, v.type || '', '1');
        for (const f of doc.functions) add(f.name, 'Function', f.name + '($0)', true, f.signature, '1');
        for (const s of doc.subs) add(s.name, 'Function', s.name + ' $0', false, s.signature, '1');
        for (const t of doc.types) add(t.name, 'Struct', t.name, false, I18N.t('hover.userType'), '2');
        for (const t of (meta && meta.typeNames) || fallbackNames('typeNames')) add(t, 'Type', t, false, '', '2');
        for (const kw of (meta && meta.keywords) || fallbackNames('keywords')) add(kw, 'Keyword', kw, false, '', '3');
        const prefix = model.getLineContent(line).slice(0, word.startColumn - 1);
        const visibility = /^[ \t]*(PUBLIC|PRIVATE)[ \t]+$/i.exec(prefix)?.[1].toUpperCase();
        for (const s of snippets(visibility)) add(s.label, 'Snippet', s.insert, true, s.detail || '', '4');
        return { suggestions: items.map((i) => ({ ...i, range })) };
      },
    };
    // Общий уровень регистрации позволяет Monaco объединить подсказки языка
    // с собственным провайдером слов. Выше явно проверяется язык модели.
    monaco.languages.registerCompletionItemProvider('*', completionProvider);
    LangFeatures._completionProvider = completionProvider; // exposed for diagnostics

    const signatureProvider = {
      signatureHelpTriggerCharacters: ['(', ','],
      async provideSignatureHelp(model, position, cancellation) {
        const version = model.getVersionId?.();
        const line = model.getValueInRange({ startLineNumber: position.lineNumber, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column });
        const lexical = lexicalCode(model.getLineContent(position.lineNumber));
        const code = lexical.mask.slice(0, line.length);
        const empty = { signatures: [], activeSignature: 0, activeParameter: 0 };
        if (line.length > lexical.commentStart) return { value: empty, dispose() {} };
        // Квадратные скобки и вставки интерполяции ограждают запятые,
        // но не становятся вызовами. Вызов внутри индекса остаётся доступен.
        const stack = [], pairs = {')': '(', ']': '[', '}': '{'};
        for (let i = 0; i < code.length; i++) {
          const ch = code[i];
          if ('([{'.includes(ch)) stack.push({kind: ch, start: i, active: 0});
          else if (')]}'.includes(ch)) {
            if (stack.at(-1)?.kind !== pairs[ch]) return {value: empty, dispose() {}};
            stack.pop();
          } else if (ch === ',' && stack.at(-1)?.kind === '(') stack.at(-1).active++;
        }
        const call = [...stack].reverse().find(frame => frame.kind === '(' && /[A-Za-z_]\w*\s*$/.test(code.slice(0, frame.start)));
        const nm = call ? code.slice(0, call.start).match(/([A-Za-z_]\w*)\s*$/) : null;
        if (!nm || cancellation?.isCancellationRequested || model.isDisposed?.()) return { value: empty, dispose() {} };
        const fnName = nm[1], active = call.active;
        const meta = getMeta();

        // 1. Builtin overload family from the registry — every overload becomes a signature.
        const family = meta && Array.isArray(meta.builtins)
          ? meta.builtins.filter((x) => x && x.name && x.name.toUpperCase() === fnName.toUpperCase())
          : [];
        if (family.length) {
          const signatures = family.map((b) => {
            const sig = builtinSignature(b);
            const params = (b.params || []).map((p) => ({ label: p.name + ((p.displayType || p.type) ? ' AS ' + displayType(p.displayType || p.type) : '') }));
            return { label: sig, parameters: params, documentation: window.AxiomBuiltinHints?.hint?.(b.name, I18N.current || 'ru') || undefined };
          });
          const matching = family.findIndex(b => (b.params || []).length > active);
          const activeSignature = matching >= 0 ? matching : family.length - 1;
          const count = (family[activeSignature].params || []).length;
          return {
            value: {
              signatures,
              activeSignature,
              activeParameter: count ? Math.min(active, count - 1) : 0,
            },
            dispose() {},
          };
        }

        // 2. User-defined FUNCTION/SUB from the document.
        const symbols = await semanticHovers(model);
        if (cancellation?.isCancellationRequested || model.isDisposed?.() || model.getVersionId?.() !== version)
          return { value: empty, dispose() {} };
        const callOffset = model.getOffsetAt?.({lineNumber: position.lineNumber, column: nm.index + 1});
        const callable = symbols?.find(item => item.start === callOffset && item.signature && item.parameters);
        if (callable) return { value: {
          signatures: [{label: callable.signature, parameters: callable.parameters.map(label => ({label}))}],
          activeSignature: 0, activeParameter: Math.min(active, Math.max(0, callable.parameters.length - 1))
        }, dispose() {} };
        if (ide.documentServices) return { value: empty, dispose() {} };
        const doc = scanDoc(model);
        const user = doc.functions.find((f) => f.name.toUpperCase() === fnName.toUpperCase())
          || doc.subs.find((s) => s.name.toUpperCase() === fnName.toUpperCase());
        if (user) {
          const paramLabels = user.params.map((p) => p.label || (p.type ? p.name + ' AS ' + p.type : p.name));
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
    const hoverCache = new WeakMap();
    async function semanticHovers(model) {
      if (!ide.documentServices || !ide.openFiles) return null;
      const file = [...ide.openFiles].find(([, document]) => document.model === model)?.[0];
      if (!file) return null;
      const version = model.getVersionId();
      let cached = hoverCache.get(model);
      if (!cached || cached.file !== file || cached.version !== version || (cached.project && !cached.project.isCurrent())) {
        cached = { version,file,project:ide.openFiles.captureProject?.(file),request: ide.documentServices.request(file, 'document.hover') };
        hoverCache.set(model, cached);
      }
      try {
        const result = await cached.request;
        if(cached.project && !cached.project.isCurrent()) return null;
        if (!result && hoverCache.get(model) === cached) hoverCache.delete(model);
        return result?.hovers || null;
      } catch (error) {
        if (hoverCache.get(model) === cached) hoverCache.delete(model);
        ide.log?.('hover.failed', { message: error?.message || String(error) });
        return null;
      }
    }
    LangFeatures.getSemanticHovers = semanticHovers;
    const hoverProvider = {
      async provideHover(model, position, cancellation) {
        if (!isCodePosition(model, position)) return null;
        let word = model.getWordAtPosition(position);
        if (!word?.word) {
          const line = lexicalCode(model.getLineContent(position.lineNumber)).mask;
          const operator = [...line.matchAll(/<>|<=|>=|<<|>>|[+*/\\^&=<>-]/g)]
            .find(item => position.column - 1 >= item.index && position.column - 1 < item.index + item[0].length);
          if (operator) word = {word: operator[0], startColumn: operator.index + 1, endColumn: operator.index + operator[0].length + 1};
        }
        if (!word || !word.word) return null;
        const name = word.word;
        const range = {
          startLineNumber: position.lineNumber, startColumn: word.startColumn,
          endLineNumber: position.lineNumber, endColumn: word.endColumn,
        };
        const meta = getMeta();
        const version = model.getVersionId?.();
        const overlapsErrorMarker = monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri }).some((marker) =>
          marker.severity === monaco.MarkerSeverity.Error
          && marker.startLineNumber <= position.lineNumber && marker.endLineNumber >= position.lineNumber
          && (marker.startLineNumber < position.lineNumber || marker.startColumn < word.endColumn)
          && (marker.endLineNumber > position.lineNumber || marker.endColumn > word.startColumn));
        // Monaco already renders compiler errors in its marker hover. Suppress the normal
        // language card here so an invalid declaration does not look like a valid builtin.
        if (overlapsErrorMarker) return null;

        const symbols = await semanticHovers(model);
        if (cancellation?.isCancellationRequested || model.isDisposed?.() || model.getVersionId?.() !== version) return null;
        const offset = model.getOffsetAt?.({lineNumber: position.lineNumber, column: word.startColumn});
        const symbol = symbols?.find(item => item.start === offset && item.length === name.length);
        if (symbol) {
          let details = '';
          const callable = ['function', 'sub'].includes(symbol.kind);
          const array = callable ? null : symbol.arrayRank ? {elementType: symbol.elementType, rank: symbol.arrayRank}
            : window.AxiomSemanticColors?.arrayType(symbol.type);
          const dictionary = callable ? null : dictionaryType(symbol.type);
          const set = callable ? null : setType(symbol.type);
          if (symbol.type) details = I18N.t('hover.typeLabel') + ': `' + symbol.type + '`';
          if (dictionary) {
            details += '\n\n' + I18N.t('hover.dictionaryKeyType') + ': `' + dictionary.keyType + '`'
              + '\n\n' + I18N.t('hover.dictionaryValueType') + ': `' + dictionary.valueType + '`';
          }
          if (set) details += '\n\n' + I18N.t('hover.setElementType') + ': `' + set.elementType + '`';
          if (array) details += '\n\n' + I18N.t('hover.elementType') + ': `' + array.elementType + '`\n\n' + I18N.t('hover.dimensions') + ': ' + array.rank;
          if (symbol.signature) details = '`' + symbol.signature + '`';
          if (symbol.fields) details = I18N.t('hover.fields') + ': ' + (symbol.fields.join(', ') || I18N.t('hover.noFields'));
          const role = dictionary ? 'dictionary' : set ? 'set' : array ? 'array' : symbol.kind;
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.' + role) + '\n\n' + details }] };
        }

        // Член коллекции после точки. Категория и подсказка зависят от типа
        // получателя: у словаря Add не возвращает значения, а у множества Add
        // и Remove возвращают BOOLEAN, а UnionWith и IntersectWith результата
        // не имеют. Получатель определяется общим разбором выражения.
        const owner = memberReceiver(model, position);
        const ownerMembers = owner && receiverMembers(owner.type);
        if (ownerMembers) {
          const member = ownerMembers.find((item) => item.label.toUpperCase() === name.toUpperCase());
          if (member) {
            const isSet = !!setType(owner.type);
            const hint = isSet
              ? window.AxiomKeywordHints?.setMemberHint?.(name, I18N.current || 'ru')
              : window.AxiomKeywordHints?.memberHint?.(name, I18N.current || 'ru');
            const role = I18N.t(isSet ? 'hover.setMember' : 'hover.dictionaryMember');
            const value = '`' + member.detail + '`' + (hint ? '\n\n' + hint : '');
            return { range, contents: [{ value: '**' + name + '** — ' + role + '\n\n' + value }] };
          }
        }

        // 1. Builtin family: all overloads, one signature per line.
        const family = meta && Array.isArray(meta.builtins)
          ? meta.builtins.filter((x) => x && x.name && x.name.toUpperCase() === name.toUpperCase())
          : [];
        if (family.length) {
          const lines = family.map((b) => '`' + builtinSignature(b) + '`');
          const hint = window.AxiomBuiltinHints?.hint?.(name, I18N.current || 'ru');
          const typeHint = window.AxiomKeywordHints?.typeHint?.(name, I18N.current || 'ru');
          const after = model.getLineContent(position.lineNumber).slice(word.endColumn - 1);
          if (typeHint && !/^\s*\(/.test(after)) {
            return { range, contents: [{ value: '`' + name + '` — ' + I18N.t('hover.type') + '\n\n' + typeHint }] };
          }
          return { range, contents: [{ value: '**' + name + '** — ' + I18N.t('hover.builtin') + (hint ? '\n\n' + hint : '') + '\n\n' + lines.join('\n\n') }] };
        }

        // Offline fallback is limited to explicit declarations; never invent a type.
        const docScan = !ide.documentServices ? scanDoc(model) : { functions: [], subs: [], vars: [], types: [] };
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
          const type = v.type || I18N.t('hover.unknownType');
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

        // 6. Registry type names and keywords. У ключевого слова подсказка
        // объясняет, что оно делает в языке; EACH и IN описаны отдельно,
        // потому что они контекстные и в реестр ключевых слов не входят.
        if (meta && Array.isArray(meta.typeNames) && meta.typeNames.some((t) => t.toUpperCase() === name.toUpperCase())) {
          const hint = window.AxiomKeywordHints?.typeHint?.(name, I18N.current || 'ru');
          return { range, contents: [{ value: '`' + name + '` — ' + I18N.t('hover.type') + (hint ? '\n\n' + hint : '') }] };
        }
        const isRegistryKeyword = meta && Array.isArray(meta.keywords)
          && meta.keywords.some((k) => k.toUpperCase() === name.toUpperCase());
        const keywordHint = window.AxiomKeywordHints?.hint?.(name, I18N.current || 'ru');
        if (isRegistryKeyword || keywordHint) {
          const value = keywordHint
            ? '`' + name + '` — ' + I18N.t('hover.keyword') + '\n\n' + keywordHint
            : '`' + name + '` — ' + I18N.t('hover.keyword');
          return { range, contents: [{ value }] };
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
