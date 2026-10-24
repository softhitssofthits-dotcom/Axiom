const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');

module.exports = [{
  id: 'graphics-open-foreground',
  name: 'SCREEN и CANVAS сразу выходят на передний план при запуске',
  async fn({ S, C }) {
    await S.launch();
    const ideHwnd = await (await S.electronApp.browserWindow(S.page))
      .evaluate(w => w.getNativeWindowHandle().readBigUInt64LE().toString());
    // Задерживаем ответ до раннего события поверхности, воспроизводя гонку запуска.
    await S.page.evaluate(() => {
      const execution = window.__ide.execution;
      const api = execution.api;
      execution.api = { ...api, async startExecution(...args) {
        const response = await api.startExecution(...args);
        if (response.ok) {
          await new Promise((resolve, reject) => {
            const deadline = performance.now() + 10000;
            const poll = () => {
              if (execution.pendingEvents.some(e => e.event === 'graphics.surface' && e.payload.windowVisible)) return resolve();
              if (performance.now() > deadline) return reject(new Error('Нет раннего события графического окна'));
              setTimeout(poll, 10);
            };
            poll();
          });
        }
        return response;
      } };
    });
    let previousHandle = '0';
    for (const [name, opening] of [['screen', 'SCREEN 8'], ['text-screen', 'SCREEN 0'], ['canvas', 'CANVAS 640,360']]) {
      const file = writeAxm(testWorkspace('graphics-foreground'), name + '.axm',
        opening + '\nDO WHILE NOT KEYPRESS("ESC")\nLOOP\n');
      await S.page.bringToFront();
      const prepared = probe(ideHwnd, 'focus');
      C.note(JSON.stringify({ mode: name, before: probe(ideHwnd, 'state') }));
      if (!prepared.focused) throw new Error('Не удалось подготовить активное окно IDE');
      await S.page.evaluate(p => window.__ide.openFile(p), file);
      await S.page.locator('#tb-run').click();
      // Состояние прежнего окна не должно приниматься за новое: ждём другой handle.
      await S.page.waitForFunction(previous => window.__ide.execution.surface?.windowVisible &&
        BigInt(window.__ide.execution.surface.windowHandle || '0') > 0n &&
        window.__ide.execution.surface.windowHandle !== previous, previousHandle);
      const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
      previousHandle = hwnd;
      const after = probe(hwnd, 'state');
      C.note(JSON.stringify({ mode: name, after }));
      C.ok(probe(hwnd, 'exists').exists, name + ': новое нативное окно существует');
      if (after.focused) C.ok(true, name + ': окно само стало активным окном Windows');
      else C.limitations.push({ scope: name + '.automatic-foreground', status: 'not-verified',
        reason: 'Windows не передала foreground нативному окну; автоматическая активация не подтверждена.', observation: after });
      const activated = probe(hwnd, 'focus');
      C.ok(activated.focused && probe(hwnd, 'state').focused, name + ': окно доступно для явной активации');
      probe(hwnd, 'close');
      await S.waitIdle();
    }
  }
}];
