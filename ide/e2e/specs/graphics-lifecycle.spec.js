const { testWorkspace, writeAxm } = require('../helpers');

async function canvasPage(S) {
  for (let i = 0; i < 100; i++) {
    const page = (await S.electronApp.windows()).find(p => p.url().endsWith('/canvas.html'));
    if (page) return page;
    await S.page.waitForTimeout(50);
  }
  throw new Error('Окно CANVAS не появилось');
}

module.exports = [
  {
    id: 'graphics-canvas-input-close-repeat',
    name: 'Закрытие CANVAS при INPUT отменяет запрос и допускает новый ввод',
    async fn({ S, C }) {
      const file = writeAxm(testWorkspace('graphics-canvas-input'), 'input.axm',
        'CANVAS 40, 30\nBEGINFRAME\nCLS RGB(255, 0, 0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\nPRINT value\n');
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.waitStatus('Ожидание ввода');
      const previous = await S.page.evaluate(() => ({ session:window.__ide.execution.sessionId, request:window.__ide.execution.inputRequestId }));
      const page = await canvasPage(S);
      await page.close(); await S.waitIdle(); await S.waitStatus('Остановлено');
      C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), null, 'Запрос ввода освобождён');
      const stale = await S.page.evaluate(p => window.axiom.submitInput(p.session, p.request, '9'), previous);
      C.eq(stale.ok, false, 'Ответ закрытой сессии отвергнут');
      await S.page.evaluate(() => window.__ide.run()); await S.waitStatus('Ожидание ввода');
      C.ok(await S.page.evaluate(p => window.__ide.execution.sessionId !== p.session && window.__ide.execution.inputRequestId !== p.request, previous), 'Новый запуск получил новую сессию и запрос');
      await S.typeConsole('7'); await S.waitIdle(); await S.waitConsole('7');
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Отмена и повтор не создали ошибки ввода');
    },
  },
  {
    id: 'graphics-mouse-scale-buttons-wheel',
    name: 'CANVAS: размеры, масштаб, три кнопки, отпускание и колесо',
    async fn({ S, C }) {
      const source = 'CANVAS 800, 400\nVIEWPORT (20, 10)-(780, 390)\nCAMERA 100, 50\n' +
        'ON MOUSE(1), PressMark\nON MOUSE(2), PressMark\nON MOUSE(3), PressMark\n' +
        'ON MOUSE(1, "UP"), ReleaseMark\nON MOUSE(2, "UP"), ReleaseMark\nON MOUSE(3, "UP"), ReleaseMark\n' +
        'ON MOUSE("WHEEL"), WheelMark\nDO\nBEGINFRAME\nCLS RGB(20, 40, 60)\nPRESENT\n' +
        'IF KEYPRESS("SPACE") THEN\nPRINT "Сброс="; MOUSEBUTTON(1); MOUSEBUTTON(2); MOUSEBUTTON(3)\nEND IF\nSLEEP 10\nLOOP\n' +
        'SUB PressMark(button AS INTEGER, x AS LONG, y AS LONG)\n' +
        'PRINT "Нажата=" + TRIM(STR(button)) + " x=" + TRIM(STR(x)) + " y=" + TRIM(STR(y)) + " состояние="; MOUSEBUTTON(button)\nEND SUB\n' +
        'SUB ReleaseMark(button AS INTEGER, x AS LONG, y AS LONG)\n' +
        'PRINT "Отпущена=" + TRIM(STR(button)) + " состояние="; MOUSEBUTTON(button)\nEND SUB\n' +
        'SUB WheelMark(delta AS INTEGER, x AS LONG, y AS LONG)\nPRINT "Колесо=" + TRIM(STR(delta))\nEND SUB\n';
      const file = writeAxm(testWorkspace('graphics-mouse-scale'), 'mouse.axm', source);
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      const page = await canvasPage(S);
      await page.waitForFunction(() => document.getElementById('program-canvas').width === 800);
      for (const factor of [1, 1.25, 1.5, 2]) {
        await S.electronApp.evaluate(({ BrowserWindow }, factor) => {
          const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/canvas.html'));
          window.setSize(460, 320); window.webContents.setZoomFactor(factor);
        }, factor);
        await page.waitForTimeout(150);
        const bounds = await page.locator('#program-canvas').boundingBox();
        await page.mouse.move(bounds.x + bounds.width * 0.25, bounds.y + bounds.height * 0.25);
        const before = await S.page.evaluate(() => window.__ide.execution.outputText.length);
        await page.mouse.down();
        await S.page.waitForFunction(offset => /Нажата=1 x=\d+ y=\d+ состояние=TRUE/.test(window.__ide.execution.outputText.slice(offset)), before);
        await page.mouse.up();
        await S.page.waitForFunction(offset => window.__ide.execution.outputText.slice(offset).includes('Отпущена=1 состояние=FALSE'), before);
        const added = await S.page.evaluate(offset => window.__ide.execution.outputText.slice(offset), before);
        const coordinates = added.match(/Нажата=1 x=(\d+) y=(\d+) состояние=TRUE/);
        // Один пиксель указателя в уменьшенном окне охватывает несколько пикселей холста.
        C.ok(coordinates && Math.abs(Number(coordinates[1]) - 280) <= 3 && Math.abs(Number(coordinates[2]) - 140) <= 3,
          `Координаты CAMERA/VIEWPORT при масштабе ${factor}`, coordinates?.slice(1).join(','));
        C.eq((added.match(/Нажата=1/g) || []).length, 1, 'Один щелчок доставляется один раз');
      }
      for (const [name, number] of [['right', 2], ['middle', 3]]) {
        const bounds = await page.locator('#program-canvas').boundingBox();
        await page.mouse.move(bounds.x + bounds.width / 4, bounds.y + bounds.height / 4);
        await page.mouse.down({ button:name });
        await S.page.waitForFunction(number => new RegExp(`Нажата=${number} x=\\d+ y=\\d+ состояние=TRUE`).test(window.__ide.execution.outputText), number);
        await page.mouse.up({ button:name });
        await S.waitConsole(`Отпущена=${number} состояние=FALSE`);
        C.ok(true, `Кнопка ${number} нажимается и отпускается`);
      }
      await page.mouse.wheel(0, 120); await S.waitConsole('Колесо=-120');
      await page.mouse.wheel(0, -120); await S.waitConsole('Колесо=120');
      C.ok(true, 'Оба направления колеса доставлены в VM');
      // Playwright по умолчанию имитирует постоянный фокус. Здесь нужен настоящий blur.
      const cdp = await page.context().newCDPSession(page);
      await cdp.send('Emulation.setFocusEmulationEnabled', { enabled:false });
      await S.electronApp.evaluate(({ BrowserWindow }) => {
        const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/canvas.html'));
        window.focus(); window.webContents.focus();
      });
      await page.waitForFunction(() => document.hasFocus());
      const beforeBlur = await S.page.evaluate(() => window.__ide.execution.outputText.length);
      await page.mouse.down();
      await S.page.waitForFunction(offset => window.__ide.execution.outputText.slice(offset).includes('состояние=TRUE'), beforeBlur);
      await S.electronApp.evaluate(({ BrowserWindow }) => {
        const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'));
        window.focus(); window.webContents.focus();
      });
      await page.waitForFunction(() => !document.hasFocus());
      await S.page.locator('.xterm-helper-textarea').focus();
      await S.page.keyboard.press('Space');
      await S.waitConsole('Сброс=FALSEFALSEFALSE');
      C.ok(true, 'Настоящая потеря фокуса сбрасывает удержанную кнопку CANVAS');
      await page.mouse.up(); await cdp.detach();
      await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
    },
  },
  {
    id: 'graphics-surfaces-stop-repeat',
    name: 'Повтор SCREEN и CANVAS не наследует поверхность или удержанные клавиши',
    async fn({ S, C }) {
      const directory = testWorkspace('graphics-surfaces-repeat');
      const screen = writeAxm(directory, 'screen.axm', 'SCREEN 10\nCIRCLE (20, 20), 10, 14\nSLEEP\n');
      const canvas = writeAxm(directory, 'canvas.axm', 'CANVAS 40, 30\nPRINT KEYDOWN("RIGHT")\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nDO\nSLEEP 10\nLOOP\n');
      await S.launch();
      for (let cycle = 0; cycle < 3; cycle++) {
        await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, screen);
        await S.page.waitForFunction(() => window.__ide.execution.surface?.kind === 'screen' && window.__ide.execution.surface.windowVisible === true);
        await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
        await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, canvas);
        const page = await canvasPage(S);
        await S.waitConsole('FALSE');
        C.eq((await S.electronApp.windows()).filter(p => p.url().endsWith('/canvas.html')).length, 1, 'Открыто одно окно CANVAS');
        await page.keyboard.down('ArrowRight');
        await page.close(); await S.waitIdle(); await S.waitStatus('Остановлено');
        await S.page.waitForTimeout(150);
        C.eq((await S.electronApp.windows()).filter(p => p.url().endsWith('/canvas.html')).length, 0, 'Закрытая поверхность не открывается поздним кадром');
      }
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Повторы не вызвали ошибок');
    },
  },
];
