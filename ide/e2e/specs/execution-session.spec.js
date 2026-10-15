const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'execution-late-command-response',
  name: 'Поздние ответы INPUT, Stop и отладки не повреждают следующий настоящий запуск',
  async fn({ S, C }) {
    await S.launch();
    const directory = testWorkspace('execution-late-response');
    const input = writeAxm(directory, 'ввод.axm', 'DIM answer AS STRING\nINPUT answer\nPRINT "Принято: "; answer\n');
    const debug = writeAxm(directory, 'отладка.axm', 'PRINT 42\n');
    for (const method of ['submitInput', 'stopExecution', 'debugCommand']) {
      await S.page.evaluate(async ({ file, debug }) => {
        await window.__ide.openFile(file); await window.__ide.run(debug ? 'debug' : 'run');
      }, { file: method === 'debugCommand' ? debug : input, debug: method === 'debugCommand' });
      await S.page.waitForFunction(debug => debug ? !!window.__ide.execution.stopId : !!window.__ide.execution.inputRequestId, method === 'debugCommand');
      await S.page.evaluate(method => {
        const execution = window.__ide.execution, original = execution.api;
        const state = window.__lateCommand = { original, ready: false, oldSession: execution.sessionId };
        execution.api = { ...original, [method]: async (...args) => {
          await original[method](...args);
          const deadline = Date.now() + 10000;
          while (window.__ide.running && Date.now() < deadline) await new Promise(resolve => setTimeout(resolve, 10));
          if (window.__ide.running) throw new Error('Старый запуск не завершился');
          // Настоящий мост отклоняет повторную команду уже завершённой сессии.
          state.response = await original[method](...args);
          const result = new Promise(resolve => { state.release = () => resolve(state.response); });
          state.ready = true;
          return result;
        } };
        state.operation = method === 'submitInput' ? execution.input('42\r') :
          method === 'stopExecution' ? execution.stop() : execution.debug('continue');
      }, method);
      await S.page.waitForFunction(() => window.__lateCommand.ready);
      C.eq(await S.page.evaluate(() => window.__lateCommand.response.ok), false, `${method}: мост действительно отклонил старую команду`);
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, input);
      await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
      const current = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId }));
      C.ok(current.session !== await S.page.evaluate(() => window.__lateCommand.oldSession), 'Новый запуск получил другую сессию');
      await S.page.evaluate(async () => {
        const state = window.__lateCommand;
        window.__ide.execution.api = state.original;
        state.release(); await state.operation;
      });
      const after = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId,
        input: window.__ide.execution.inputRequestId, running: window.__ide.running }));
      C.eq(after.session, current.session, `${method}: поздний отказ сохранил новую сессию`);
      C.eq(after.input, current.input, `${method}: новый запрос ввода сохранился`);
      C.eq(after.running, true, `${method}: IDE продолжает считать программу работающей`);
      await S.page.evaluate(() => window.__ide.execution.input('Тест\r'));
      await S.waitIdle(); await S.waitConsole('Принято: Тест');
      C.ok(true, `${method}: следующая программа приняла русский ответ и завершилась`);
    }
  },
}];
