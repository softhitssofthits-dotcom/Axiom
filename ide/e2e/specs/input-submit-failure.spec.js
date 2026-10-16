const { testWorkspace, writeAxm } = require('../helpers');

async function open(S, suffix, source) {
  const directory = testWorkspace('input-submit-' + suffix);
  const file = writeAxm(directory, 'ввод.axm', source);
  await S.launch();
  await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
  await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
  return directory;
}

const one = 'DIM name AS STRING\nINPUT "Имя: ", name\nPRINT "Принято: "; name\n';

async function holdAccepted(S, mode) {
  await S.page.evaluate(mode => {
    const e = window.__ide.execution, original = e.api, originalEvent = e.event.bind(e);
    const state = window.__inputDelivery = { original, originalEvent, ready: false, completed: false, rejection: null };
    const id = e.inputRequestId;
    e.event = message => { if (message.event === 'input.completed' && message.payload.inputRequestId === id) state.completed = true; return originalEvent(message); };
    e.api = { ...original, submitInput: async (...args) => {
      await original.submitInput(...args);
      const result = await original.submitInput(...args);
      if (result.ok) throw new Error('Повтор принятого запроса не был отклонён');
      return new Promise((resolve, reject) => {
        state.release = () => mode === 'response' ? resolve(result) : reject(new Error('Поздний отказ INPUT'));
        state.ready = true;
      });
    } };
    state.operation = e.input('Анна\r').catch(error => { state.rejection = error.message; });
  }, mode);
  await S.page.waitForFunction(() => window.__inputDelivery.ready && window.__inputDelivery.completed);
  await S.page.evaluate(() => { window.__ide.execution.api = window.__inputDelivery.original; });
}

async function release(S) {
  await S.page.evaluate(async () => { const s = window.__inputDelivery; s.release(); await s.operation; window.__ide.execution.event = s.originalEvent; });
}

module.exports = [{
  id: 'input-submit-active-failure',
  name: 'Непринятый INPUT сохраняет приглашение и русский текст для редактирования и повтора',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await open(S, 'active-' + mode, one);
      const before = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId, prompt: window.__ide.execution.inputPrompt }));
      await S.page.evaluate(async mode => {
        const e = window.__ide.execution, original = e.api;
        const state = window.__activeInputFailure = { rejection: null };
        e.api = { ...original, submitInput: async (session, id, text) => {
          if (mode === 'rejection') throw new Error('Проверка отказа доставки INPUT');
          const result = await original.submitInput(session, 'invalid-input-id', text);
          if (result.ok) throw new Error('Мост принял неверный запрос');
          state.message = result.error.message; return result;
        } };
        try { await e.input('Анн\r'); } catch (error) { state.rejection = error.message; }
        finally { e.api = original; }
      }, mode);
      C.eq(await S.page.evaluate(() => window.__activeInputFailure.rejection), null, mode + ': исключение обработано');
      C.eq(await S.page.evaluate(() => window.__ide.running), true, mode + ': программа остаётся работающей');
      C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), before.session, mode + ': сессия сохранена');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), before.input, mode + ': запрос восстановлен');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputPrompt), before.prompt, mode + ': приглашение восстановлено');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputText), 'Анн', mode + ': набранный текст сохранён');
      C.eq(await S.page.locator('#status-text').textContent(), await S.page.evaluate(mode => mode === 'response' ? window.__activeInputFailure.message : 'Проверка отказа доставки INPUT', mode), mode + ': причина видна');
      if (await S.page.evaluate(() => !!window.__ide.execution.inputRequestId && window.__ide.running)) {
        await S.waitConsole(before.prompt + 'Анн');
        C.eq(((await S.consoleText()).match(/Имя:/g) || []).length, 1, mode + ': приглашение не задублировано');
        await S.typeConsole('а'); await S.waitIdle(); await S.waitConsole('Принято: Анна');
        C.eq(((await S.consoleText()).match(/Принято: Анна/g) || []).length, 1, mode + ': исправленный ответ принят один раз');
      }
      await S.close();
    }
  },
}, {
  id: 'input-submit-completed-failure',
  name: 'Подтверждённый ввод не восстанавливается после позднего отказа до следующего INPUT',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await open(S, 'completed-' + mode, one + 'SLEEP\n');
      await holdAccepted(S, mode); await S.waitConsole('Принято: Анна');
      const before = await S.page.locator('#status-text').textContent();
      await release(S);
      C.eq(await S.page.evaluate(() => window.__inputDelivery.rejection), null, mode + ': позднее исключение обработано');
      C.eq(await S.page.evaluate(() => window.__ide.running), true, mode + ': программа продолжает ждать SLEEP');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), null, mode + ': принятый запрос не возвращён');
      C.eq(await S.page.locator('#status-text').textContent(), before, mode + ': поздний отказ не изменил статус');
      C.eq(((await S.consoleText()).match(/Имя:/g) || []).length, 1, mode + ': приглашение осталось единственным');
      if (await S.page.evaluate(() => window.__ide.running)) { await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(); }
      await S.close();
    }
  },
}, {
  id: 'input-submit-late-request-failure',
  name: 'Поздний ответ INPUT сохраняет следующий запрос, частичный текст и новую сессию',
  async fn({ S, C }) {
    for (const target of ['next', 'newSession']) for (const mode of ['response', 'rejection']) {
      const directory = await open(S, target + '-' + mode,
        'DIM first AS STRING, second AS STRING\nINPUT first\nINPUT second\nPRINT first; "/"; second\n');
      await holdAccepted(S, mode);
      await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
      if (target === 'newSession') {
        await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
        const file = writeAxm(directory, 'другой.axm', 'DIM answer AS STRING\nINPUT answer\nPRINT "Другой: "; answer\n');
        await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
        await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
      }
      await S.page.evaluate(() => window.__ide.execution.input('Но'));
      const before = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId, status: document.getElementById('status-text').textContent }));
      await release(S);
      C.eq(await S.page.evaluate(() => window.__inputDelivery.rejection), null, target + '/' + mode + ': позднее исключение обработано');
      C.eq(await S.page.evaluate(() => window.__ide.running), true, target + '/' + mode + ': программа работает');
      C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), before.session, target + '/' + mode + ': сессия сохранена');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), before.input, target + '/' + mode + ': текущий запрос сохранён');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputText), 'Но', target + '/' + mode + ': частичный текст сохранён');
      C.eq(await S.page.locator('#status-text').textContent(), before.status, target + '/' + mode + ': статус сохранён');
      await S.typeConsole('вый'); await S.waitIdle(); await S.waitConsole(target === 'next' ? 'Анна/Новый' : 'Другой: Новый');
      C.ok(true, target + '/' + mode + ': текущая программа приняла ответ');
      await S.close();
    }
  },
}];
