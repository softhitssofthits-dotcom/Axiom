// Подсветка программ Axiom. Словарь берётся из реестра компилятора,
// поэтому новые имена не требуют ручного списка в разметке.
(function () {
  'use strict';

  const keywords = new Set(('AND ANDALSO APPEND AS BEGINFRAME BYREF BYVAL CAMERA CANVAS CASE CATCH CIRCLE CLOSE CLS COLOR CONST CONTINUE DIM DO DRAW DRAWCIRCLE DRAWIMAGE DRAWLINE DRAWRECT DRAWSPRITE DRAWTEXT DRAWTILE DRAWTILELAYER DRAWTILEMAP ELSE ELSEIF END EQV ERASE ERROR EXIT FALSE FILLCIRCLE FILLRECT FINALLY FOR FUNCTION GET IF IMP IMPORT INPUT IS KEY LINE LOCATE LOOP MOD MOUSE NEXT NOT OFF ON OPEN OR ORELSE OUTPUT PAINT PALETTE PCOPY PRESENT PRESERVE PRESET PRINT PRIVATE PSET PUBLIC PUT RANDOMIZE REDIM RESETCAMERA RETURN SCREEN SELECT SELF SETMAPTILE SLEEP SMOOTH STEP STOP SUB THEN TO TRANSPARENT TRUE TRY TYPE UNTIL USING VIEW VIEWPORT WHILE WINDOW XOR').split(' '));
  const types = new Set('BOOLEAN DATE DATETIME DECIMAL DOUBLE FILE IMAGE INTEGER LONG SPRITE STRING TILEMAP TILESET TIME UTCDATETIME'.split(' '));
  const builtins = new Set();

  function collectBuiltins() {
    const index = window.AxiomManual2Index || [];
    for (const entry of index) {
      if (entry.kind === 'Функция') builtins.add(entry.title.toUpperCase());
    }
  }

  function escapeHtml(value) {
    return String(value)
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;');
  }

  function highlight(source) {
    const out = [];
    let i = 0;
    const text = source;

    const push = (cls, value) => {
      out.push(cls ? `<span class="${cls}">${escapeHtml(value)}</span>` : escapeHtml(value));
    };

    while (i < text.length) {
      const ch = text[i];

      if (ch === "'") {
        let end = text.indexOf('\n', i);
        if (end < 0) end = text.length;
        push('tok-com', text.slice(i, end));
        i = end;
        continue;
      }

      if (ch === 'R' || ch === 'r') {
        const rest = text.slice(i);
        const rem = rest.match(/^REM(?=\s|$)/i);
        if (rem) {
          let end = text.indexOf('\n', i);
          if (end < 0) end = text.length;
          push('tok-com', text.slice(i, end));
          i = end;
          continue;
        }
      }

      if (ch === '"') {
        let j = i + 1;
        let value = '"';
        while (j < text.length) {
          if (text[j] === '"' && text[j + 1] === '"') {
            value += '""';
            j += 2;
            continue;
          }
          if (text[j] === '"') {
            value += '"';
            j++;
            break;
          }
          value += text[j];
          j++;
        }
        push('tok-str', value);
        i = j;
        continue;
      }

      if (/[0-9]/.test(ch)) {
        const rest = text.slice(i);
        const num = rest.match(/^0[xX][0-9a-fA-F]+|^&[hHbB][0-9a-fA-F]+|^\d+(\.\d+)?([eE][+-]?\d+)?/);
        const value = num ? num[0] : ch;
        push('tok-num', value);
        i += value.length;
        continue;
      }

      if (/[A-Za-z_]/.test(ch)) {
        const rest = text.slice(i);
        const word = rest.match(/^[A-Za-z_][A-Za-z0-9_]*/)[0];
        const upper = word.toUpperCase();
        let cls = null;
        if (keywords.has(upper)) cls = 'tok-kw';
        else if (types.has(upper)) cls = 'tok-type';
        else if (builtins.has(upper)) cls = 'tok-fn';
        push(cls, word);
        i += word.length;
        continue;
      }

      if (/[=<>+\-*/\\^&]/.test(ch)) {
        const rest = text.slice(i);
        const op = rest.match(/^(<>|<=|>=|<<|>>|\^|[-+*/\\&=<>])/);
        const value = op ? op[0] : ch;
        push('tok-op', value);
        i += value.length;
        continue;
      }

      push(null, ch);
      i++;
    }

    return out.join('');
  }

  function highlightAll() {
    collectBuiltins();
    const blocks = document.querySelectorAll('pre code.language-axiom');
    for (const block of blocks) {
      if (block.dataset.highlighted === 'true') continue;
      block.innerHTML = highlight(block.textContent);
      block.dataset.highlighted = 'true';
    }
  }

  window.AxiomManual2Highlight = {highlightAll};
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', highlightAll);
  } else {
    highlightAll();
  }
})();
