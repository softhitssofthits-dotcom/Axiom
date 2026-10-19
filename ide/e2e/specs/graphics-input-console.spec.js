const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');

// Число разных цветов в кадре окна: пустой фон даёт одно значение,
// нарисованное приглашение добавляет цвет текста.
function distinctColors(hwnd) {
  const frame = probe(hwnd, 'capture');
  const data = Buffer.from(frame.bgra, 'base64');
  const colors = new Set();
  for (let offset = 0; offset + 3 < data.length; offset += 4) {
    colors.add((data[offset] << 16) | (data[offset + 1] << 8) | data[offset + 2]);
  }
  return colors.size;
}

module.exports = [{
  id: 'graphics-input-console-duplication',
  name: 'INPUT в режиме SCREEN работает только в окне и не дублирует приглашение в консоли',
  async fn({ S, C }) {
    await S.launch();
    for (const [mode, header] of [[0, 'SCREEN 0'], [1, 'SCREEN 1']]) {
      const file = writeAxm(testWorkspace('graphics-input-console'), `mode${mode}.axm`,
        `${header}\nDIM name AS STRING\nINPUT "Имя", name\nPRINT "Привет, " & name\nSLEEP 3000\n`);
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null &&
        window.__ide.execution.surface?.windowVisible);

      const target = await S.page.evaluate(() => window.__ide.execution.inputTarget);
      C.eq(target, 'screen', `${header}: запрос направлен в окно SCREEN`);

      const text = await S.consoleText();
      C.ok(!text.includes('Имя'), `${header}: приглашение не дублируется в консоли`, JSON.stringify(text));

      const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
      C.ok(distinctColors(hwnd) > 1, `${header}: приглашение нарисовано в окне SCREEN`);

      // Ответ из терминала при активном SCREEN не принимается.
      await S.page.evaluate(() => window.__ide.execution.input('чужой ответ\r'));
      C.ok(await S.page.evaluate(() => window.__ide.execution.inputRequestId !== null),
        `${header}: терминал не завершает запрос SCREEN`);

      await S.page.evaluate(() => window.__ide.stop());
      await S.waitIdle();
    }
  },
}];
