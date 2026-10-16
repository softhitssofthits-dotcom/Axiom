const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'document-delivery-stale-failures',
  name: 'Прежний отказ диагностики не меняет исправленный, другой или повторно открытый документ',
  async fn({ S, C }) {
    for (const change of ['edit', 'switch', 'reopen']) {
      const directory = testWorkspace('document-delivery-' + change);
      const first = writeAxm(directory, 'первый.axm', 'PRINT missing\n');
      const second = writeAxm(directory, 'второй.axm', 'PRINT otherMissing\n');
      await S.launch();
      await S.page.evaluate(async ({ first, second }) => {
        const i = window.__ide;
        i.settings.checkOnChange = false; i.settings.formatOnBlur = false;
        await i.openFile(second); await i.openFile(first); clearTimeout(i._liveCheckTimer);
        const original = i.documentServices.api;
        const state = window.__delivery = { original, ready: false };
        i.documentServices.api = { ...original, documentCommand: async (...args) => {
          const response = await original.documentCommand(...args);
          if (args[0] !== 'document.compile') return response;
          state.codes = response.payload.diagnostics.map(d => d.code);
          return new Promise((_, reject) => { state.release = () => reject(new Error('Прежний отказ проверки')); state.ready = true; });
        } };
        state.operation = i.check(true);
      }, { first, second });
      await S.page.waitForFunction(() => window.__delivery.ready);
      C.ok(await S.page.evaluate(() => window.__delivery.codes.includes('MB2001')), change + ': получена настоящая диагностика Host');
      await S.page.evaluate(async ({ change, first, second }) => {
        const i = window.__ide; i.documentServices.api = window.__delivery.original;
        if (change === 'edit') i.editor.getModel().setValue('PRINT 42\n');
        if (change === 'switch') i.switchToFile(second);
        if (change === 'reopen') { await i.closeFile(first, true); await i.openFile(first); }
        clearTimeout(i._liveCheckTimer); await i.runLiveCheck();
        window.__delivery.before = JSON.stringify(i._lastDiagnostics);
        window.__delivery.markers = JSON.stringify(i.monaco.editor.getModelMarkers({ owner: 'axiom', resource: i.editor.getModel().uri }));
        window.__delivery.status = document.getElementById('status-text').textContent;
        window.__delivery.release(); await window.__delivery.operation;
      }, { change, first, second });
      C.eq(await S.page.evaluate(() => JSON.stringify(window.__ide._lastDiagnostics)), await S.page.evaluate(() => window.__delivery.before), change + ': текущие сообщения сохранены');
      C.eq(await S.page.evaluate(() => JSON.stringify(window.__ide.monaco.editor.getModelMarkers({ owner: 'axiom', resource: window.__ide.editor.getModel().uri }))), await S.page.evaluate(() => window.__delivery.markers), change + ': текущие маркеры сохранены');
      C.eq(await S.page.locator('#status-text').textContent(), await S.page.evaluate(() => window.__delivery.status), change + ': статус сохранён');
      C.eq(await S.page.evaluate(file => window.axiom.readFileContent(file), first), 'PRINT missing\n', change + ': проверка не записала файл');
      await S.close();
    }
  },
}, {
  id: 'document-delivery-current-retry',
  name: 'Действующий отказ проверки виден, повтор получает настоящую диагностику',
  async fn({ S, C }) {
    const file = writeAxm(testWorkspace('document-delivery-retry'), 'проверка.axm', 'PRINT missing\n');
    await S.launch();
    await S.page.evaluate(async file => {
      const i = window.__ide; i.settings.checkOnChange = false; i.settings.formatOnBlur = false;
      await i.openFile(file); clearTimeout(i._liveCheckTimer);
      const original = i.documentServices.api;
      i.documentServices.api = { ...original, documentCommand: async () => { throw new Error('Служба временно недоступна'); } };
      try { await i.check(true); } finally { i.documentServices.api = original; }
    }, file);
    C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(d => d.code === 'AXIOM-IDE' && d.message.includes('Служба временно недоступна'))), 'Причина действующего отказа показана');
    await S.page.evaluate(() => window.__ide.check(true));
    C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(d => d.code === 'MB2001')), 'Повтор получил настоящую ошибку программы');
    C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(d => d.code === 'AXIOM-IDE')), 'Прежний служебный отказ заменён результатом');
    C.eq(await S.page.evaluate(file => window.axiom.readFileContent(file), file), 'PRINT missing\n', 'Файл не изменён');
  },
}, {
  id: 'document-live-delivery-order',
  name: 'Фоновая диагностика сохраняет текущий документ при позднем ответе и исключении',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) for (const change of ['edit', 'switch', 'close']) {
      const directory = testWorkspace('document-live-' + mode + '-' + change);
      const first = writeAxm(directory, 'первый.axm', 'PRINT missing\n');
      const second = writeAxm(directory, 'второй.axm', 'PRINT otherMissing\n');
      await S.launch();
      await S.page.evaluate(async ({ first, second, mode }) => {
        const i = window.__ide; i.settings.checkOnChange = false; i.settings.formatOnBlur = false;
        await i.openFile(second); await i.openFile(first); clearTimeout(i._liveCheckTimer);
        const original = i.documentServices.api;
        const state = window.__liveDelivery = { original, error: null, ready: false };
        i.documentServices.api = { ...original, documentCommand: async (...args) => {
          const response = await original.documentCommand(...args);
          if (args[0] !== 'document.analyse') return response;
          state.codes = response.payload.diagnostics.map(d => d.code);
          return new Promise((resolve, reject) => { state.release = () => mode === 'response' ? resolve(response) : reject(new Error('Прежний отказ анализа')); state.ready = true; });
        } };
        state.operation = i.runLiveCheck().catch(error => { state.error = error.message; });
      }, { first, second, mode });
      await S.page.waitForFunction(() => window.__liveDelivery.ready);
      C.ok(await S.page.evaluate(() => window.__liveDelivery.codes.includes('MB2001')), mode + '/' + change + ': удержан настоящий анализ');
      await S.page.evaluate(async ({ change, first, second }) => {
        const i = window.__ide; i.documentServices.api = window.__liveDelivery.original;
        if (change === 'edit') i.editor.getModel().setValue('PRINT 42\n');
        if (change === 'switch') i.switchToFile(second);
        if (change === 'close') await i.closeFile(first, true);
        clearTimeout(i._liveCheckTimer); await i.runLiveCheck();
        window.__liveDelivery.before = JSON.stringify(i._lastDiagnostics);
        window.__liveDelivery.markers = JSON.stringify(i.monaco.editor.getModelMarkers({ owner: 'axiom', resource: i.editor.getModel().uri }));
        window.__liveDelivery.release(); await window.__liveDelivery.operation;
      }, { change, first, second });
      C.eq(await S.page.evaluate(() => window.__liveDelivery.error), null, mode + '/' + change + ': прежний отказ не вышел наружу');
      C.eq(await S.page.evaluate(() => JSON.stringify(window.__ide._lastDiagnostics)), await S.page.evaluate(() => window.__liveDelivery.before), mode + '/' + change + ': сообщения сохранены');
      C.eq(await S.page.evaluate(() => JSON.stringify(window.__ide.monaco.editor.getModelMarkers({ owner: 'axiom', resource: window.__ide.editor.getModel().uri }))), await S.page.evaluate(() => window.__liveDelivery.markers), mode + '/' + change + ': маркеры сохранены');
      C.eq(await S.page.evaluate(file => window.axiom.readFileContent(file), first), 'PRINT missing\n', mode + '/' + change + ': исходник не записан');
      await S.close();
    }
  },
}];
