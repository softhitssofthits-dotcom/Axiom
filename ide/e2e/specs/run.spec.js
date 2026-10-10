// Run / Stop through the integrated ConPTY console, including an INPUT program.
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [
  {
    id: 'run-prints-output-and-finishes',
    name: 'F5 runs the active saved file; output lands in the console',
    async fn({ S, C }) {
      const ws = testWorkspace('run-hello');
      const file = writeAxm(ws, 'hello.axm', 'PRINT "Привет, мир!"\nPRINT 2 + 2\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('run'));
      await S.waitConsole('Привет, мир!');
      await S.waitConsole('4');
      await S.waitIdle();
      await S.waitStatus('Завершено');
      C.ok(true, 'program output and completion status observed');
      await S.screenshot('run-hello');
    },
  },
  {
    id: 'run-input-program-accepts-typed-line',
    name: 'A program with INPUT reads a typed line from the console',
    async fn({ S, C }) {
      const ws = testWorkspace('run-input');
      const file = writeAxm(
        ws,
        'ask.axm',
        'DIM name AS STRING\nINPUT "Как вас зовут: "; name\nPRINT "Здравствуйте, "; name\n'
      );
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('run'));
      await S.waitConsole('Как вас зовут:');
      await S.typeConsole('Иван');
      await S.waitConsole('Здравствуйте, Иван');
      await S.waitIdle();
      C.ok(true, 'INPUT prompt answered, greeting printed');
    },
  },
  {
    id: 'stop-kills-running-program',
    name: 'Stop (Shift+F5) terminates an infinite loop; status shows Остановлено',
    async fn({ S, C }) {
      const ws = testWorkspace('run-stop');
      const file = writeAxm(ws, 'loop.axm', 'WHILE TRUE\nEND WHILE\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('run'));
      await S.page.waitForFunction(
        () => window.__ide && window.__ide.running === true,
        null, { timeout: 15000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('stop'));
      await S.waitIdle();
      await S.waitStatus('Остановлено');
      C.ok(true, 'program stopped and status updated');
    },
  },
  {
    id: 'run-screen-program-uses-shared-graphics-host',
    name: 'SCREEN выполняется через общие службы и завершается без ограничения CLI',
    async fn({ S, C }) {
      const ws = testWorkspace('run-graphics');
      const file = writeAxm(ws, 'screen.axm', 'SCREEN 1\nPRINT "после экрана"\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('run'));
      await S.waitIdle();
      const panel = await S.activePanel();
      C.eq(panel, 'console', 'после успешного SCREEN панель ошибок не открывается');
      const items = await S.diagnosticItems();
      C.eq(items.length, 0, 'общий хост поддерживает SCREEN');
      await S.waitStatus('Завершено');
    },
  },
];
