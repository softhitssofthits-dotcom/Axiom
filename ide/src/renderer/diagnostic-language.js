(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  const english = {
    MB0001:'Invalid character.', MB0002:'Unterminated string.', MB0003:'Invalid numeric literal.', MB0004:'Numeric literal is out of range.',
    MB1001:'Expected token is missing.', MB1002:'Expected an identifier.', MB1003:'Expected an expression.', MB1004:'Unexpected token.', MB1005:'Duplicate ELSE.', MB1006:'Duplicate CASE ELSE.',
    MB2001:'Name is not declared.', MB2002:'Name is already declared in this scope.', MB2003:'Type mismatch.', MB2004:'Wrong number of arguments.', MB2005:'Unknown callable.',
    MB2006:'A SUB cannot return a value.', MB2007:'A return value is required.', MB2008:'Invalid condition.', MB2009:'Invalid FOR control variable.', MB2010:'Cannot assign to a constant.',
    MB2011:'Invalid array access.', MB2012:'RETURN is not valid in this context.', MB2013:'EXIT FOR is not valid in this context.', MB2014:'EXIT DO is not valid in this context.', MB2015:'Invalid SELECT CASE range.',
    MB2016:'Declaration shadows an existing name.', MB2017:'Variable type is required.', MB2018:'Constant is out of range.', MB2019:'A procedure cannot be used as a value.', MB2020:'Value cannot be called.',
    MB2021:'Unknown type.', MB2022:'Invalid TYPE member.', MB2023:'Unknown field.', MB2024:'Invalid field access.', MB2025:'Operation is not supported for a fixed TYPE array field.',
    MB2026:'Invalid fixed-length string declaration.', MB2027:'Invalid default value for a TYPE field.', MB2028:'Invalid BYREF argument.', MB2029:'BYREF is not supported for this callable value.',
    MB2031:'Named and positional TYPE initializers cannot be mixed.', MB2032:'Wrong number of positional TYPE initializers.', MB2033:'Ambiguous TYPE constructor.',
    MB2034:'Invalid ON KEY handler.', MB2035:'Invalid ON MOUSE handler.', MB2036:'Invalid ON MOUSE event.', MB2037:'CONTINUE FOR is not valid in this context.', MB2038:'CONTINUE WHILE is not valid in this context.',
    MB2039:'CONTINUE DO is not valid in this context.', MB2040:'Expression statement has no effect.', MB2044:'Field access requires SELF.', MB2045:'Name conflicts with another declaration category.',
    MB2046:'The operation requires a compatible graphics mode. Text mode cannot be used for graphics.', MB2047:'Variable is declared but its value is never used.', MB2048:'NEXT counter name must match the FOR counter.',
    MB2049:'Imported module was not found.', MB2050:'Module name is declared by several files.', MB2051:'Entry module was not found.',
    MB2052:'Cyclic module import.', MB2053:'Module member is private.', MB2054:'Imported name is ambiguous.', MB2055:'Imported name conflicts with a declaration.', MB2056:'Visibility modifier is not valid in this context.',
    MB2057:'Sort field must name an orderable field of a one-dimensional record array.', MB2058:'Sort direction must be STEP 1 or STEP -1, and only in ARRAYSORT or ARRAYTOSORTED.',
    MB2059:'An empty array literal requires a known element type.', MB2060:'Array literal element is not compatible with the element type.', MB2061:'An array literal creates a one-dimensional array only.',
    MB2062:'FOR EACH source must be an array.', MB2063:'FOR EACH loop variable type is not compatible with the element type.',
    MB2065:'Dictionary key type must be STRING, INTEGER or LONG.', MB2066:'Dictionary value type is not supported.', MB2067:'Invalid dictionary member or call form.',
    MB3001:'Division by zero.', MB3002:'Array index is out of bounds.', MB3003:'Input cannot be converted to the required type.', MB3004:'FOR STEP cannot be zero.', MB3005:'Invalid runtime state.',
    MB3006:'Function completed without returning a value.', MB3007:'Invalid built-in argument.', MB3008:'Numeric overflow.', MB3009:'Function value is empty.', MB3010:'Required graphics mode is not active.', MB3011:'Call stack overflow.', MB3012:'Argument is outside the numeric domain.',
    MB3013:'Failed to allocate array: the element count exceeds the supported limit.', MB3014:'Array view refers to released storage.',
    MB3015:'Dictionary does not contain this key.', MB3016:'Dictionary already contains this key.',
    MB4001:'Bytecode does not support this construct.', MB4002:'Cannot capture the value in bytecode.', MB4003:'Cannot create a bytecode JSON snapshot.',
    MB4004:'Cannot read an imported dependency file.',
  };
  const specific = [
    [/^Имя счётчика после NEXT '(.+)' должно совпадать с именем счётчика FOR '(.+)'\.$/, (_m,actual,expected) => "NEXT counter '"+actual+"' must match FOR counter '"+expected+"'."],
    [/^Имя '(.+)' не объявлено\.(.*)$/s, (_m,n,tail) => "Name '"+n+"' is not declared."+(tail.includes('CALL')?' Axiom has no CALL statement; call a procedure as Foo().':'')],
    [/^Имя '(.+)' уже объявлено в этой области видимости\.$/, (_m,n) => "Name '"+n+"' is already declared in this scope."],
    [/^Ожидалось выражение\.$/, () => 'Expected an expression.'],
    [/^Ожидался (.+)\.$/, (_m,n) => 'Expected '+(n.includes('идентификатор')?'an identifier':n.includes('конец')?'end of statement':n)+'.'],
    [/^Неизвестный тип '(.+)'\.$/, (_m,n) => "Unknown type '"+n+"'."],
    [/^Неверное число аргументов у '(.+)': ожидалось (.+), получено (\d+)\.$/, (_m,n,expected,actual) => "Wrong number of arguments for '"+n+"': expected "+expected.replace('не менее','at least').replace('от ','from ').replace(' до ',' to ').replace(' или ',' or ')+', received '+actual+'.'],
    [/^Несовместимые типы: ожидался (.+), получен (.+)\.$/, (_m,expected,actual) => 'Type mismatch: expected '+expected+', received '+actual+'.'],
    [/^Для переменной '(.+)' в DIM нужно указать тип через AS или инициализатор для вывода типа\.$/, (_m,n) => "Variable '"+n+"' in DIM requires an AS type or an initializer for type inference."],
    [/^Константное числовое значение выходит за диапазон (.+)\.$/, (_m,type) => 'Constant numeric value is outside the range of '+type+'.'],
  ];
  function text(d, locale) {
    if (d.code === 'IDE0001') {
      return `${d.code}: ${locale === 'en'
        ? 'An internal error occurred while preparing or running the program. See the IDE log for technical details.'
        : 'Внутренняя ошибка при подготовке или выполнении программы. Технические подробности находятся в журнале IDE.'}`;
    }
    if (locale !== 'en' || !/^MB\d{4}$/.test(d.code)) return `${d.code}: ${d.message}`;
    if (d.messageEn) return `${d.code}: ${d.messageEn}`;
    if (!/[А-Яа-яЁё]/.test(String(d.message))) return `${d.code}: ${d.message}`;
    for (const [pattern, translate] of specific) {
      const match = String(d.message).match(pattern);
      if (match) { const result = translate(...match); if (!/[А-Яа-яЁё]/.test(result.replace(/'[^']*'/g,''))) return `${d.code}: ${result}`; }
    }
    const description = english[d.code] || 'Language diagnostic.';
    const details = [...String(d.message).matchAll(/'([^']+)'/g)].map(m => m[0]);
    return `${d.code}: ${description}${details.length ? ' Context: '+details.join(',')+'.' : ''}`;
  }
  return { DiagnosticLanguage: { english, text } };
});
