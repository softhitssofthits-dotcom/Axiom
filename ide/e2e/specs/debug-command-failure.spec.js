const { testWorkspace, writeAxm } = require('../helpers');

async function start(S, suffix) {
  const file = writeAxm(testWorkspace('debug-command-' + suffix), 'шаги.axm',
    'DIM value AS INTEGER = 1\nvalue = value + 1\nvalue = value + 1\nPRINT value\n');
  await S.launch();
  await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run('debug', true); }, file);
  await S.page.waitForFunction(() => !!window.__ide.execution.stopId && window.__ide.execution.line === 1);
}

async function step(S) {
  const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
  await S.page.evaluate(() => window.__ide.trace.command('stepOver'));
  await S.page.waitForFunction(stop => !!window.__ide.execution.stopId && window.__ide.execution.stopId !== stop && !window.__ide.execution.debugBusy, stop);
}

module.exports = [{
  id: 'debug-command-active-failure',
  name: 'Отказ шага и исключение доставки сохраняют остановку, указатель и дальнейшее управление',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await start(S, mode);
      const before = await S.page.evaluate(() => window.__ide.execution.stopId);
      await S.page.evaluate(async mode => {
        const e = window.__ide.execution, original = e.api;
        const state = window.__debugFailure = { rejection: null };
        e.api = { ...original, debugCommand: async (session, command) => {
          if (mode === 'rejection') throw new Error('Проверка отказа доставки');
          const result = await original.debugCommand(session, command, 'invalid-stop-id');
          if (result.ok) throw new Error('Мост принял неверную остановку');
          state.message = result.error.message; return result;
        } };
        try { await window.__ide.trace.command('stepOver'); }
        catch (error) { state.rejection = error.message; }
        finally { e.api = original; }
      }, mode);
      C.eq(await S.page.evaluate(() => window.__debugFailure.rejection), null, mode + ': исключение обработано контроллером');
      C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), before, mode + ': исходная остановка сохранена');
      C.eq(await S.page.evaluate(() => !!window.__ide.execution.debugBusy), false, mode + ': занятость снята');
      C.eq(await S.page.locator('.trace-current-line').count(), 1, mode + ': указатель строки восстановлен');
      C.eq(await S.page.locator('#tb-stepOver').isEnabled(), true, mode + ': следующий шаг доступен');
      C.eq(await S.page.locator('#status-text').textContent(), await S.page.evaluate(mode => mode === 'response' ? window.__debugFailure.message : 'Проверка отказа доставки', mode), mode + ': причина видна в строке состояния');
      if (await S.page.evaluate(() => !!window.__ide.execution.stopId && !window.__ide.execution.debugBusy)) {
        await step(S);
        C.eq(await S.page.evaluate(() => window.__ide.execution.line), 2, mode + ': настоящий следующий шаг работает');
        await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('3');
        C.ok(true, mode + ': программа завершилась с ожидаемым значением');
      }
      await S.close();
    }
  },
}, {
  id: 'debug-command-late-pause-failure',
  name: 'Поздний отказ прежнего шага не изменяет новую настоящую остановку той же сессии',
  async fn({ S, C }) {
    for (const mode of ['response', 'rejection']) {
      await start(S, 'late-' + mode);
      await S.page.evaluate(mode => {
        const e = window.__ide.execution, original = e.api;
        const state = window.__lateDebugFailure = { original, ready: false, rejection: null };
        e.api = { ...original, debugCommand: async (...args) => {
          await original.debugCommand(...args);
          const result = await original.debugCommand(...args);
          if (result.ok) throw new Error('Мост принял прежнюю остановку');
          return new Promise((resolve, reject) => {
            state.release = () => mode === 'response' ? resolve(result) : reject(new Error('Поздний отказ доставки'));
            state.ready = true;
          });
        } };
        state.operation = window.__ide.trace.command('stepOver').catch(error => { state.rejection = error.message; });
      }, mode);
      await S.page.waitForFunction(() => window.__lateDebugFailure.ready && window.__ide.execution.line === 2 && !!window.__ide.execution.stopId);
      C.eq(await S.page.evaluate(() => window.__ide.execution.debugBusy), false, mode + ': новая остановка сразу освобождает команды');
      await S.page.evaluate(() => { window.__ide.execution.api = window.__lateDebugFailure.original; });
      if (await S.page.evaluate(() => !window.__ide.execution.debugBusy)) await step(S);
      const before = await S.page.evaluate(() => ({ stop: window.__ide.execution.stopId, line: window.__ide.execution.line, status: document.getElementById('status-text').textContent }));
      await S.page.evaluate(async () => { const state = window.__lateDebugFailure; state.release(); await state.operation; });
      C.eq(await S.page.evaluate(() => window.__lateDebugFailure.rejection), null, mode + ': позднее исключение поглощено');
      C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), before.stop, mode + ': новая остановка сохранена');
      C.eq(await S.page.evaluate(() => window.__ide.execution.line), before.line, mode + ': строка сохранена');
      C.eq(await S.page.locator('#status-text').textContent(), before.status, mode + ': поздний отказ не заменил статус');
      C.eq(await S.page.locator('.trace-current-line').count(), 1, mode + ': указатель текущей строки сохранён');
      await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle(); await S.waitConsole('3');
      C.ok(true, mode + ': продолжение работает после позднего ответа');
      await S.close();
    }
  },
}];
