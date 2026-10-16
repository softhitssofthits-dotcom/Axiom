const { testWorkspace, writeAxm } = require('../helpers');

async function open(S, suffix, debug) {
  const dir = testWorkspace('stop-failure-' + suffix);
  const file = writeAxm(dir, 'программа.axm', debug ?
    'DIM value AS INTEGER = 1\nvalue = value + 1\nPRINT value\n' :
    'DIM answer AS STRING\nINPUT answer\nPRINT "Принято: "; answer\n');
  await S.launch();
  await S.page.evaluate(async ({ file, debug }) => {
    const ide = window.__ide; await ide.openFile(file);
    if (debug) { ide.editor.setPosition({ lineNumber: 2, column: 1 }); ide.trace.toggle(); }
    await ide.run(debug ? 'debug' : 'run', false);
  }, { file, debug });
  await S.page.waitForFunction(debug => debug ? !!window.__ide.execution.stopId : !!window.__ide.execution.inputRequestId, debug);
  return file;
}

async function failStop(S, mode, restart) {
  await S.page.evaluate(async ({ mode, restart }) => {
    const e = window.__ide.execution, original = e.api;
    const state = window.__stopFailure = { rejection: null };
    e.api = { ...original, stopExecution: async () => {
      if (mode === 'rejection') throw new Error('Проверка отказа доставки Stop');
      const result = await original.stopExecution('invalid-session');
      if (result.ok) throw new Error('Мост принял неверную сессию');
      state.message = result.error.message; return result;
    } };
    try { await (restart ? window.__ide.trace.command('restart') : window.__ide.stop()); }
    catch (error) { state.rejection = error.message; }
    finally { e.api = original; }
  }, { mode, restart });
}

module.exports = [{
  id: 'stop-failure-keeps-input',
  name: 'Отказ Stop сохраняет настоящий INPUT и возможность продолжить программу',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await open(S, 'input-' + mode, false);
      const before = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId }));
      await failStop(S, mode, false);
      C.eq(await S.page.evaluate(() => window.__stopFailure.rejection), null, mode + ': исключение обработано');
      C.eq(await S.page.evaluate(() => window.__ide.running), true, mode + ': работающая VM сохранена');
      C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), before.session, mode + ': сессия сохранена');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), before.input, mode + ': запрос INPUT сохранён');
      C.eq(await S.page.evaluate(() => window.__ide._stopRequested), false, mode + ': не осталось ложного признака остановки');
      C.eq(await S.page.locator('#status-text').textContent(), await S.page.evaluate(mode => mode === 'response' ? window.__stopFailure.message : 'Проверка отказа доставки Stop', mode), mode + ': причина видна');
      if (await S.page.evaluate(() => window.__ide.running && !!window.__ide.execution.inputRequestId)) {
        await S.page.evaluate(() => window.__ide.execution.input('Тест\r')); await S.waitIdle(); await S.waitConsole('Принято: Тест');
        C.ok(true, mode + ': действующий INPUT принят после отказа Stop');
      }
      await S.close();
    }
  },
}, {
  id: 'restart-stop-failure-retry',
  name: 'Отказ Stop отменяет только попытку Restart; следующий Restart успешно работает',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await open(S, 'restart-' + mode, true);
      const before = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, stop: window.__ide.execution.stopId }));
      await failStop(S, mode, true);
      C.eq(await S.page.evaluate(() => window.__stopFailure.rejection), null, mode + ': Restart обработал отказ');
      C.eq(await S.page.evaluate(() => !!window.__ide.trace.restarting), false, mode + ': ожидание Restart закрыто');
      C.eq(await S.page.evaluate(() => window.__ide.running), true, mode + ': исходная программа работает');
      C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), before.session, mode + ': новая сессия не началась');
      C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), before.stop, mode + ': исходная остановка сохранена');
      C.eq(await S.page.locator('.trace-current-line').count(), 1, mode + ': указатель сохранён');
      C.eq(await S.page.locator('#tb-restart').isEnabled(), true, mode + ': повторный Restart доступен');
      if (await S.page.evaluate(() => window.__ide.running && !!window.__ide.execution.stopId)) {
        await S.page.evaluate(() => window.__ide.trace.command('restart'));
        await S.page.waitForFunction(session => !!window.__ide.execution.stopId && window.__ide.execution.sessionId !== session, before.session);
        C.eq(await S.page.evaluate(() => window.__ide.execution.line), 2, mode + ': повтор дошёл до прежней точки');
        await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('2');
        C.ok(true, mode + ': перезапущенная программа завершилась');
      }
      await S.close();
    }
  },
}, {
  id: 'restart-late-stop-rejection',
  name: 'Поздний отказ доставки Stop после реального завершения не мешает новой программе или отмене Restart',
  async fn({ S, C }) {
    for (const mode of ['finish', 'newRun', 'cancel']) {
      await open(S, 'late-' + mode, true);
      await S.page.evaluate(() => {
        const e = window.__ide.execution, original = e.api;
        const state = window.__lateStopFailure = { original, ready: false, rejection: null };
        e.api = { ...original, stopExecution: async (...args) => {
          await original.stopExecution(...args);
          return new Promise((resolve, reject) => { state.release = () => reject(new Error('Поздний отказ Stop')); state.ready = true; });
        } };
        state.operation = window.__ide.trace.command('restart').catch(error => { state.rejection = error.message; });
      });
      await S.page.waitForFunction(() => window.__lateStopFailure.ready && !window.__ide.running);
      await S.page.evaluate(() => { window.__ide.execution.api = window.__lateStopFailure.original; });
      let current;
      if (mode === 'newRun') {
        const file = writeAxm(testWorkspace('late-stop-new-input'), 'ввод.axm', 'DIM answer AS STRING\nINPUT answer\nPRINT "Новый: "; answer\n');
        await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
        await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
        current = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId }));
      } else if (mode === 'cancel') await S.page.evaluate(() => window.__ide.stop());
      await S.page.evaluate(async () => { const s = window.__lateStopFailure; s.release(); await s.operation; });
      C.eq(await S.page.evaluate(() => window.__lateStopFailure.rejection), null, mode + ': поздний отказ обработан');
      C.eq(await S.page.evaluate(() => !!window.__ide.trace.restarting), false, mode + ': ожидание закрыто');
      if (mode === 'finish') {
        await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
        C.eq(await S.page.evaluate(() => window.__ide.execution.line), 2, 'Подтверждённое завершение разрешило Restart');
        await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('2');
      } else if (mode === 'newRun') {
        C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), current.session, 'Новая сессия сохранена');
        C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), current.input, 'Новый INPUT сохранён');
        await S.page.evaluate(() => window.__ide.execution.input('Тест\r')); await S.waitIdle(); await S.waitConsole('Новый: Тест');
      } else C.eq(await S.page.evaluate(() => window.__ide.running), false, 'Отменённый Restart не создал скрытый запуск');
      await S.close();
    }
  },
}];
