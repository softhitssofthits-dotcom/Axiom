const { testWorkspace, writeAxm } = require('../helpers');

async function prepare(S, tag) {
  const directory = testWorkspace(tag);
  const first = writeAxm(directory, 'отлаживаемая.axm', 'DIM value AS INTEGER = 1\nPRINT value\nPRINT value + 1\n');
  const other = writeAxm(directory, 'другая.axm', 'DIM answer AS STRING\nINPUT answer\nPRINT "Другой запуск: "; answer\n');
  await S.launch();
  await S.page.evaluate(async ({ first, other }) => {
    const ide = window.__ide;
    await ide.openFile(other); ide.trace.toggle(1);
    await ide.openFile(first); ide.trace.toggle(2);
    await ide.run('debug', false);
  }, { first, other });
  await S.page.waitForFunction(() => !!window.__ide.execution.stopId && window.__ide.execution.line === 2);
  return { first, other };
}

module.exports = [{
  id: 'trace-restart-other-tab',
  name: 'Шаг и Restart из другой вкладки сохраняют исходную программу и её точки',
  async fn({ S, C }) {
    const { first, other } = await prepare(S, 'trace-restart-tabs');
    const old = await S.page.evaluate(() => window.__ide.execution.sessionId);
    await S.page.evaluate(async file => { window.__ide.switchToFile(file); await window.__ide.trace.command('stepOver'); }, other);
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId && window.__ide.execution.line === 3);
    C.eq(await S.page.evaluate(() => window.__ide.activeFile), first, 'Шаг вернул исходную программу');
    C.eq(await S.page.evaluate(() => window.__ide.trace.marker?.model === window.__ide.openFiles.get(window.__ide._runFile).model), true, 'Указатель строки принадлежит исполняемой модели');
    await S.page.evaluate(async file => { window.__ide.switchToFile(file); await window.__ide.trace.command('restart'); }, other);
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId && window.__ide.execution.line === 2);
    C.eq(await S.page.evaluate(() => window.__ide.activeFile), first, 'Restart вернул исходную программу');
    C.ok(await S.page.evaluate(old => window.__ide.execution.sessionId !== old, old), 'Restart создал новую сессию');
    C.eq(await S.page.evaluate(file => window.__ide.trace.lines(window.__ide.openFiles.get(file).model).join(','), first), '2', 'Точка исходной программы сохранена');
    C.eq(await S.page.evaluate(file => window.__ide.trace.lines(window.__ide.openFiles.get(file).model).join(','), other), '1', 'Точка другой программы не подменила остановку');
    await S.page.evaluate(() => window.__ide.trace.command('stop')); await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.trace.marker), null, 'Stop снял указатель строки');
    C.eq(await S.page.evaluate(() => !!window.__ide.trace.restarting), false, 'Ожидание перезапуска завершено');
  },
}, {
  id: 'trace-restart-late-stop-response',
  name: 'Запоздалый Stop не переносит Restart на новый запуск; явный Stop отменяет ожидающий Restart',
  async fn({ S, C }) {
    for (const mode of ['newRun', 'stop', 'reopen']) {
      const { first, other } = await prepare(S, 'trace-restart-late-' + mode);
      await S.page.evaluate(() => {
        const ide = window.__ide, execution = ide.execution, original = execution.api;
        const state = window.__restartDelay = { original, documentId: ide.openFiles.get(ide._runFile).documentId };
        execution.api = { ...original, stopExecution: async (...args) => {
          const response = await original.stopExecution(...args);
          const deadline = Date.now() + 10000;
          while (ide.running && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 10));
          if (ide.running) throw new Error('Прежняя программа не завершилась');
          return new Promise(resolve => { state.release = () => resolve(response); });
        } };
        state.operation = ide.trace.command('restart');
      });
      await S.page.waitForFunction(() => !!window.__restartDelay.release);
      C.eq(await S.page.evaluate(() => !!window.__ide.trace.restarting), true, `${mode}: настоящий ответ Stop ожидается`);
      await S.page.evaluate(() => { window.__ide.execution.api = window.__restartDelay.original; });
      let current;
      if (mode === 'newRun') {
        await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, other);
        await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
        current = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId }));
      } else if (mode === 'stop') {
        C.eq(await S.page.evaluate(() => !!window.__ide.trace.allowed('stop')), true, 'Stop доступен для ожидающего Restart без работающей сессии');
        await S.page.evaluate(() => window.__ide.trace.command('stop'));
      } else {
        await S.page.evaluate(async file => {
          await window.__ide.closeFile(file, true); await window.__ide.openFile(file);
        }, first);
        C.ok(await S.page.evaluate(file => window.__ide.openFiles.get(file).documentId !== window.__restartDelay.documentId, first), 'Файл открыт как новый документ после завершения старой сессии');
      }
      await S.page.evaluate(() => {
        const state = window.__restartDelay;
        state.release(); state.operation.then(() => { state.done = true; });
      });
      await S.page.waitForFunction(() => window.__restartDelay.done, null, { timeout: 5000 });
      C.eq(await S.page.evaluate(() => !!window.__ide.trace.restarting), false, `${mode}: старое ожидание закрыто`);
      if (mode === 'newRun') {
        C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), current.session, 'Новая сессия не заменена старым Restart');
        C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), current.input, 'Ввод новой программы сохранён');
        await S.page.evaluate(() => window.__ide.execution.input('Тест\r'));
        await S.waitIdle(); await S.waitConsole('Другой запуск: Тест');
        C.eq(await S.page.evaluate(() => window.__ide._runFile), other, 'Завершение другой программы не запустило прежнюю');
      } else {
        C.eq(await S.page.evaluate(() => window.__ide.running), false, `${mode}: программа не запустилась снова`);
        C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), null, `${mode}: нет скрытой сессии`);
      }
      await S.close();
    }
  },
}];
