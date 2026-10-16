const { execFileSync } = require('node:child_process');
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'execution-preparation-failure-recovery',
  name: 'Ошибка подготовки после успешного запуска не исполняет старый код; исправление запускается',
  async fn({ S, C }) {
    await S.launch();
    const file = writeAxm(testWorkspace('preparation-compile'), 'программа.axm', 'PRINT "Старый результат"\n');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
    await S.waitIdle(); await S.waitConsole('Старый результат');
    await S.page.evaluate(async () => {
      const ide = window.__ide;
      ide.editor.setValue('PRINT missingVariable\n');
      await ide.save(); await ide.run();
    });
    await S.waitIdle();
    C.ok((await S.diagnosticItems()).length > 0, 'Ошибка компиляции показана');
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText), '', 'Старый код не дал вывод после отказа');
    C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), null, 'Неудачная сессия завершена');
    await S.page.evaluate(async () => {
      const ide = window.__ide;
      ide.editor.setValue('PRINT "Новый результат"\n');
      await ide.save(); await ide.run();
    });
    await S.waitIdle(); await S.waitConsole('Новый результат');
    C.eq((await S.diagnosticItems()).length, 0, 'Исправленная программа запускается без прежней диагностики');
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.includes('Старый результат')), false, 'Исполнен новый исходник');
  },
}, {
  id: 'execution-preparation-save-cancel',
  name: 'Отмена сохранения перед запуском сохраняет правки и не запускает прежний файл',
  async fn({ S, C }) {
    await S.launch();
    const file = writeAxm(testWorkspace('preparation-cancel'), 'программа.axm', 'PRINT "На диске"\n');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
    await S.waitIdle(); await S.waitConsole('На диске');
    S.confirmAction = 'dismiss';
    await S.page.evaluate(async () => {
      const ide = window.__ide;
      ide.editor.setValue('PRINT "В редакторе"\n');
      const execution = ide.execution, original = execution.api;
      window.__startCalls = 0;
      execution.api = { ...original, startExecution: (...args) => { window.__startCalls++; return original.startExecution(...args); } };
      await ide.run();
      execution.api = original;
    });
    C.eq(await S.page.evaluate(() => window.__startCalls), 0, 'Команда запуска не отправлена');
    C.eq(await S.page.evaluate(() => window.__ide.running), false, 'Программа не запущена');
    C.eq(await S.page.evaluate(() => window.__ide.dirty.has(window.__ide.activeFile)), true, 'Правки остаются несохранёнными');
    C.eq(require('node:fs').readFileSync(file, 'utf8'), 'PRINT "На диске"\n', 'Файл не перезаписан');
    S.confirmAction = 'accept';
    await S.page.evaluate(() => window.__ide.run());
    await S.waitIdle(); await S.waitConsole('В редакторе');
    C.ok(true, 'После согласия сохранить запускаются новые правки');
  },
}, {
  id: 'execution-preparation-host-fault-late-response',
  name: 'Авария DesktopHost при ожидании ответа запуска не позволяет позднему ответу заменить новую сессию',
  async fn({ S, C }) {
    await S.launch();
    const file = writeAxm(testWorkspace('preparation-fault'), 'ввод.axm', 'DIM answer AS STRING\nINPUT answer\nPRINT "Принято: "; answer\n');
    await S.page.evaluate(async file => {
      const ide = window.__ide;
      await ide.openFile(file);
      const execution = ide.execution, original = execution.api;
      const state = window.__preparation = { original };
      execution.api = { ...original, startExecution: async (...args) => {
        state.response = await original.startExecution(...args);
        return new Promise(resolve => { state.release = () => resolve(state.response); });
      } };
      state.operation = ide.run();
    }, file);
    await S.page.waitForFunction(() => !!window.__preparation.release);
    C.eq(await S.page.evaluate(() => window.__ide.execution.starting), true, 'Ответ настоящего запуска ожидается контроллером');
    const parent = await S.electronApp.evaluate(() => process.pid);
    if (!Number.isSafeInteger(parent) || parent <= 0) throw new Error('Неверный PID тестовой оболочки');
    const pid = Number(execFileSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
      `Get-CimInstance Win32_Process -Filter 'ParentProcessId = ${parent}' | Where-Object { $_.Name -eq 'Axiom.DesktopHost.exe' } | Select-Object -ExpandProperty ProcessId`],
    { encoding: 'utf8', windowsHide: true }).trim());
    if (!Number.isSafeInteger(pid) || pid <= 0) throw new Error('Не найден единственный собственный DesktopHost');
    process.kill(pid);
    await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.starting), false, 'Авария сбросила подготовку');
    await S.page.evaluate(async () => {
      window.__ide.execution.api = window.__preparation.original;
      await window.__ide.run();
    });
    await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
    const current = await S.page.evaluate(() => ({ session: window.__ide.execution.sessionId, input: window.__ide.execution.inputRequestId }));
    await S.page.evaluate(async () => { window.__preparation.release(); await window.__preparation.operation; });
    C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), current.session, 'Поздний ответ не заменил новую сессию');
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), current.input, 'Запрос новой программы сохранён');
    C.eq(await S.page.evaluate(() => window.__ide.running), true, 'Новая программа продолжает работать');
    await S.page.evaluate(() => window.__ide.execution.input('Тест\r'));
    await S.waitIdle(); await S.waitConsole('Принято: Тест');
    C.ok(true, 'Новый DesktopHost принял ответ и завершил программу');
  },
}];
