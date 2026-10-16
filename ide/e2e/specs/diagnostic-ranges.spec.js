const fs = require('node:fs');
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'diagnostic-ranges-unicode-line-endings',
  name: 'Диагностика Monaco точно выделяет UTF-16 после эмодзи при LF/CRLF и обоих языках',
  async fn({ S, C }) {
    await S.launch();
    const file = writeAxm(testWorkspace('diagnostic-ranges'), 'диапазоны.axm', 'PRINT 1\n');
    await S.page.evaluate(file => window.__ide.openFile(file), file);
    for (const newline of ['\n', '\r\n']) {
      for (const language of ['ru', 'en']) {
        for (const example of [
          { code: 'MB2001', lines: ["' 😀", 'PRINT "е́😀"; missing'], text: 'missing' },
          { code: 'MB0002', lines: ["' 😀", 'PRINT "е́😀'], text: '"е́😀' },
        ]) {
          const source = example.lines.join(newline) + newline;
          await S.page.evaluate(async ({ source, newline, language }) => {
            const ide = window.__ide;
            ide.setLanguage(language);
            ide.editor.setValue(source);
            ide.editor.getModel().setEOL(newline === '\r\n' ? window.monaco.editor.EndOfLineSequence.CRLF : window.monaco.editor.EndOfLineSequence.LF);
            await ide.check(true);
          }, { source, newline, language });
          const state = await S.page.evaluate(code => {
            const ide = window.__ide, model = ide.editor.getModel();
            const diagnostic = ide._lastDiagnostics.find(d => d.code === code);
            const marker = window.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri }).find(m => m.code === code);
            if (!diagnostic || !marker) throw new Error('Диагностика или маркер отсутствуют: ' + code);
            const range = { startLineNumber: marker.startLineNumber, startColumn: marker.startColumn,
              endLineNumber: marker.endLineNumber, endColumn: marker.endColumn };
            return { source: model.getValue(), diagnostic, range, selected: model.getValueInRange(range),
              offset: model.getOffsetAt({ lineNumber: range.startLineNumber, column: range.startColumn }) };
          }, example.code);
          const expectedOffset = source.lastIndexOf(example.text);
          const expectedColumn = example.lines[1].indexOf(example.text) + 1;
          const label = `${example.code}/${language}/${JSON.stringify(newline)}`;
          C.eq(state.source, source, `${label}: буфер сохраняет выбранные переводы строк`);
          C.eq(state.diagnostic.spanStart, expectedOffset, `${label}: точное абсолютное смещение UTF-16`);
          C.eq(state.diagnostic.spanLength, example.text.length, `${label}: точная длина UTF-16`);
          C.eq(state.selected, example.text, `${label}: Monaco выделяет только ошибочный текст`);
          C.eq(state.offset, expectedOffset, `${label}: маркер начинается в том же месте`);
          C.eq(state.range.startLineNumber, 2, `${label}: правильная строка`);
          C.eq(state.range.startColumn, expectedColumn, `${label}: столбец учитывает эмодзи и комбинируемый знак`);
          await S.page.locator(`#diagnostics-content .diagnostic-item[data-code="${example.code}"]`).first().click();
          C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().column), expectedColumn, `${label}: переход из списка ошибок точен`);
          await S.page.keyboard.press('Escape');
        }
      }
    }
    C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 1\n', 'Проверка буфера не записала изменения на диск');
  },
}, {
  id: 'diagnostic-ranges-eof-recovery',
  name: 'Нулевая диагностика конца файла остаётся на EOF и исчезает после исправления при LF/CRLF',
  async fn({ S, C }) {
    await S.launch();
    const file = writeAxm(testWorkspace('diagnostic-ranges-eof'), 'конец.axm', 'PRINT 1\n');
    await S.page.evaluate(file => window.__ide.openFile(file), file);
    for (const newline of ['\n', '\r\n']) {
      await S.page.evaluate(async newline => {
        const ide = window.__ide;
        ide.editor.setValue(['IF TRUE THEN', 'PRINT "😀"', ''].join(newline));
        ide.editor.getModel().setEOL(newline === '\r\n' ? 1 : 0);
        await ide.check(true);
      }, newline);
      const state = await S.page.evaluate(() => {
        const ide = window.__ide, model = ide.editor.getModel();
        const diagnostic = ide._lastDiagnostics.find(d => d.code === 'MB1001');
        const marker = window.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri }).find(m => m.code === 'MB1001');
        if (!diagnostic || !marker) throw new Error('Нет ожидаемой диагностики EOF');
        return { diagnostic, length: model.getValue().length,
          start: model.getOffsetAt({ lineNumber: marker.startLineNumber, column: marker.startColumn }),
          end: model.getOffsetAt({ lineNumber: marker.endLineNumber, column: marker.endColumn }) };
      });
      C.eq(state.diagnostic.spanLength, 0, 'Компилятор передал позиционную диагностику');
      C.eq(state.diagnostic.spanStart, state.length, 'Диагностика находится в конце исходника');
      C.eq(state.start, state.length, 'Начало маркера остаётся на EOF');
      C.eq(state.end, state.length, 'Маркер не захватывает текст другой строки');
      await S.page.locator('#diagnostics-content .diagnostic-item[data-code="MB1001"]').first().click();
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), 3, 'Переход открывает последнюю пустую строку');
      await S.page.keyboard.press('Escape');
      await S.page.evaluate(async newline => {
        const ide = window.__ide;
        ide.editor.setValue(['IF TRUE THEN', 'PRINT "😀"', 'END IF', ''].join(newline));
        await ide.check(true);
      }, newline);
      C.eq((await S.markers()).length, 0, 'Исправление очистило прежний маркер');
      C.eq((await S.diagnosticItems()).length, 0, 'Исправление очистило список ошибок');
    }
  },
}];
