const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, tag, source) {
  const file = writeAxm(testWorkspace(tag), 'подсказки.axm', source);
  await S.launch();
  await S.page.evaluate(async file => {
    const i = window.__ide; i.settings.smartIdentifierEnabled = false;
    // Здесь запросом управляет сценарий, а не фоновая семантическая раскраска.
    i.editor.updateOptions({ hover: { enabled: false }, parameterHints: { enabled: false }, 'semanticHighlighting.enabled': false });
    await i.openFile(file);
    clearTimeout(i._liveCheckTimer);
  }, file);
  return file;
}
module.exports = [{
  id: 'language-delivery-cache-owner',
  name: 'Прежняя редакция не удаляет общий запрос актуальных подсказок',
  async fn({ S, C }) {
    const first = await open(S, 'language-cache-owner', 'DIM price AS INTEGER = 1\nPRINT price\n');
    await S.page.evaluate(() => {
      const i = window.__ide, original = i.documentServices.api;
      const state = window.__hoverDelivery = { original, calls: 0, releases: [], open: false };
      i.documentServices.api = { ...original, documentCommand: async (...args) => {
        if (args[0] !== 'document.hover') return original.documentCommand(...args);
        state.calls++; const result = await original.documentCommand(...args);
        if (state.open) return result;
        return new Promise(resolve => state.releases.push(() => resolve(result)));
      } };
      // Новая редакция гарантирует запрос через установленный выше перехват,
      // даже если открытие документа уже успело заполнить общий кеш.
      i.editor.getModel().setValue(i.editor.getValue() + "' Проверка владельца запроса\n");
      clearTimeout(i._liveCheckTimer);
      state.hover = () => window.LangFeatures._hoverProvider.provideHover(i.editor.getModel(), { lineNumber: 2, column: 7 });
      state.old = state.hover();
    });
    await S.page.waitForFunction(() => window.__hoverDelivery.releases.length === 1);
    await S.page.evaluate(() => {
      const i = window.__ide; i.editor.getModel().setValue('DIM price AS STRING = "цена"\nPRINT price\n');
      clearTimeout(i._liveCheckTimer); window.__hoverDelivery.current = window.__hoverDelivery.hover();
    });
    await S.page.waitForFunction(() => window.__hoverDelivery.releases.length === 2);
    await S.page.evaluate(async () => {
      const s = window.__hoverDelivery; s.releases[0](); s.oldResult = await s.old;
      s.shared = s.hover(); s.count = s.calls; s.open = true;
      s.releases[1](); s.currentResult = await s.current; s.sharedResult = await s.shared;
      window.__ide.documentServices.api = s.original;
    });
    C.eq(await S.page.evaluate(() => window.__hoverDelivery.oldResult), null, 'Прежняя редакция не показывает карточку');
    C.eq(await S.page.evaluate(() => window.__hoverDelivery.count), 2, 'Два потребителя актуальной редакции разделяют один запрос');
    C.contains(await S.page.evaluate(() => JSON.stringify(window.__hoverDelivery.currentResult)), 'STRING', 'Первый текущий потребитель получил настоящий тип');
    C.contains(await S.page.evaluate(() => JSON.stringify(window.__hoverDelivery.sharedResult)), 'STRING', 'Повторный потребитель получил тот же тип');
    C.eq(await S.page.evaluate(() => window.LangFeatures._completionProvider.provideCompletionItems(window.__ide.editor.getModel(), { lineNumber: 2, column: 12 }).suggestions.find(s => s.label === 'price').detail), 'STRING', 'Дополнение использует исправленное объявление');
    const other = writeAxm(testWorkspace('language-cache-owner'), 'другая.axm', 'DIM price AS DECIMAL = 2.5\nPRINT price\n');
    await S.page.evaluate(file => window.__ide.openFile(file), other);
    C.contains(await S.page.evaluate(async () => JSON.stringify(await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(), { lineNumber: 2, column: 7 }))), 'DECIMAL', 'Другая модель получает собственный тип');
    C.eq(await S.page.evaluate(() => window.LangFeatures._completionProvider.provideCompletionItems(window.__ide.editor.getModel(), { lineNumber: 2, column: 12 }).suggestions.find(s => s.label === 'price').detail), 'DECIMAL', 'Дополнение другой модели независимо');
    await S.page.evaluate(file => window.__ide.switchToFile(file), first);
    C.contains(await S.page.evaluate(async () => JSON.stringify(await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(), { lineNumber: 2, column: 7 }))), 'STRING', 'Возврат к первой модели сохраняет её тип');
  },
}, {
  id: 'language-delivery-failure-retry',
  name: 'Отказ без объекта ошибки не ломает hover и подпись, повтор использует компилятор',
  async fn({ S, C }) {
    await open(S, 'language-hover-retry', 'FUNCTION Twice(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION\nPRINT Twice(1)\n');
    const result = await S.page.evaluate(async () => {
      const i = window.__ide, original = i.documentServices.api;
      i.documentServices.api = { ...original, documentCommand: async (...args) => {
        if (args[0] === 'document.hover') throw null;
        return original.documentCommand(...args);
      } };
      i.editor.getModel().setValue(i.editor.getValue() + "' Проверка отказа доставки\n");
      clearTimeout(i._liveCheckTimer);
      let first = null, error = null;
      try { first = await window.LangFeatures._hoverProvider.provideHover(i.editor.getModel(), { lineNumber: 4, column: 7 }); }
      catch (e) { error = e?.message || String(e); }
      finally { i.documentServices.api = original; }
      const hover = await window.LangFeatures._hoverProvider.provideHover(i.editor.getModel(), { lineNumber: 4, column: 7 });
      const signature = await window.LangFeatures._sigProvider.provideSignatureHelp(i.editor.getModel(), { lineNumber: 4, column: 14 });
      return { first, error, hover, signature: signature.value };
    });
    C.eq(result.error, null, 'Отказ обработан без нового исключения');
    C.eq(result.first, null, 'При отказе не выдана ложная карточка');
    C.contains(JSON.stringify(result.hover), 'Twice', 'Повтор получил символ компилятора');
    C.contains(JSON.stringify(result.signature), 'value AS INTEGER', 'Подпись содержит настоящий параметр');
    await S.page.evaluate(() => {
      const i = window.__ide; i.editor.updateOptions({ hover: { enabled: true } });
      i.editor.setPosition({ lineNumber: 4, column: 8 }); i.editor.focus(); i.editor.trigger('test', 'editor.action.showHover', null);
    });
    await S.page.waitForFunction(() => [...document.querySelectorAll('.monaco-hover')].some(el => el.textContent.includes('Twice')));
    C.ok(true, 'Настоящая карточка Monaco показана после повтора');
  },
}, {
  id: 'language-delivery-cancel-close',
  name: 'Отмена и закрытие модели не показывают поздние hover и подпись',
  async fn({ S, C }) {
    for (const mode of ['cancel', 'close']) {
      const file = await open(S, 'language-hover-' + mode, 'FUNCTION Twice(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION\nPRINT Twice(1)\n');
      await S.page.evaluate(() => {
        const i = window.__ide, original = i.documentServices.api;
        const s = window.__cancelHover = { original, token: { isCancellationRequested: false }, ready: false };
        i.documentServices.api = { ...original, documentCommand: async (...args) => {
          const result = await original.documentCommand(...args);
          if (args[0] !== 'document.hover') return result;
          return new Promise(resolve => { s.release = () => resolve(result); s.ready = true; });
        } };
        i.editor.getModel().setValue(i.editor.getValue() + "' Проверка отмены доставки\n");
        clearTimeout(i._liveCheckTimer);
        s.hover = window.LangFeatures._hoverProvider.provideHover(i.editor.getModel(), { lineNumber: 4, column: 7 }, s.token);
        s.signature = window.LangFeatures._sigProvider.provideSignatureHelp(i.editor.getModel(), { lineNumber: 4, column: 14 }, s.token);
      });
      await S.page.waitForFunction(() => window.__cancelHover.ready);
      const result = await S.page.evaluate(async ({ mode, file }) => {
        const i = window.__ide, s = window.__cancelHover;
        if (mode === 'cancel') s.token.isCancellationRequested = true;
        else await i.closeFile(file, true);
        i.documentServices.api = s.original; s.release();
        return { hover: await s.hover, signature: (await s.signature).value };
      }, { mode, file });
      C.eq(result.hover, null, mode + ': прежняя карточка не возвращена');
      C.eq(result.signature.signatures.length, 0, mode + ': прежняя подпись не возвращена');
      await S.close();
    }
  },
}];
