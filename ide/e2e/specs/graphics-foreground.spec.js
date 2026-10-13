const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');

module.exports = [{
  id: 'graphics-open-foreground',
  name: 'SCREEN и CANVAS сразу выходят на передний план при запуске',
  async fn({ S, C }) {
    await S.launch();
    for (const [name, opening] of [['screen', 'SCREEN 8'], ['text-screen', 'SCREEN 0'], ['canvas', 'CANVAS 640,360']]) {
      const file = writeAxm(testWorkspace('graphics-foreground'), name + '.axm',
        opening + '\nDO WHILE NOT KEYPRESS("ESC")\nLOOP\n');
      await S.page.bringToFront();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible &&
        BigInt(window.__ide.execution.surface.windowHandle || '0') > 0n);
      const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
      C.eq(probe(hwnd, 'state').focused, true, name + ': окно само стало активным окном Windows');
      probe(hwnd, 'close');
      await S.waitIdle();
    }
  }
}];
