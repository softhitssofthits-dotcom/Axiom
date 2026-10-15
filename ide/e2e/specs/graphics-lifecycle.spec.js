const { testWorkspace, writeAxm } = require('../helpers');
const { execFileSync } = require('child_process');
const path = require('path');
const { nativeCanvas, probe } = require('../native-canvas');

async function canvasPage(S) {
  return nativeCanvas(S);
}

module.exports = [
  {
    id: 'graphics-screen-directx-pages-input',
    name: 'DirectX SCREEN сохраняет палитру, страницы, GET/PUT и нативный INPUT',
    async fn({ S, C }) {
      await S.launch();
      const file = path.resolve(__dirname, '../../../samples/operators/screen/directx_pages_input.axm');
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null &&
        window.__ide.execution.surface?.kind === 'screen' && window.__ide.execution.surface.windowVisible === true);
      const surface = await S.page.evaluate(() => window.__ide.execution.surface);
      C.ok(['Hardware', 'WARP'].includes(surface.graphicsDriver), 'SCREEN использует DirectX-устройство', surface.graphicsDriver);
      C.note(`SCREEN graphicsDriver=${surface.graphicsDriver}`);
      const hwnd = surface.windowHandle;
      for (const character of 'Тест') probe(hwnd, 'char', { Key:character.charCodeAt(0) });
      probe(hwnd, 'down', { Key:13 }); probe(hwnd, 'char', { Key:13 }); probe(hwnd, 'up', { Key:13 });
      let frame, data;
      function pixel(x, y) {
        const scale = Math.min(frame.width / 320, frame.height / 200);
        const width = Math.floor(320 * scale), height = Math.floor(200 * scale);
        const px = Math.floor((frame.width - width) / 2) + Math.floor((x + .5) * width / 320);
        const py = Math.floor((frame.height - height) / 2) + Math.floor((y + .5) * height / 200);
        const offset = (py * frame.width + px) * 4;
        return [data[offset + 2], data[offset + 1], data[offset]].join();
      }
      for (let i = 0; i < 15; i++) {
        frame = probe(hwnd, 'capture'); data = Buffer.from(frame.bgra, 'base64');
        if (pixel(100, 80) === '255,255,85') break;
        await S.page.waitForTimeout(100);
      }
      C.eq(pixel(20, 60), '0,255,0', 'PCOPY сохранил переназначенную палитру');
      C.eq(pixel(30, 60), '0,255,0', 'GET/PUT сохранили палитровый индекс');
      C.eq(pixel(100, 80), '255,255,85', 'Нативный INPUT получил русский текст, последующие рисование и PCOPY работают');
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Диагностики отсутствуют');
      await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
      C.eq(probe(hwnd, 'exists').exists, false, 'Остановка закрыла GPU SCREEN');
    },
  },
  {
    id: 'graphics-screen-regression-boundaries',
    name: 'SCREEN: повтор узора PAINT, короткая дуга и отсечение больших линий',
    async fn({ S, C }) {
      await S.launch();
      const cases = [
        ['paint/background_repeat.axm', 30, 70, [170, 85, 0]],
        ['circle/short_arc.axm', 220, 100, [255, 85, 85]],
        ['line/clipped_bounds.axm', 60, 80, [255, 255, 85]],
      ];
      for (const [sample, x, y, expected] of cases) {
        const file = path.resolve(__dirname, '../../../samples/operators', sample);
        await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
        await S.page.waitForFunction(() => window.__ide.execution.surface?.kind === 'screen' &&
          window.__ide.execution.surface.windowVisible === true);
        const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
        let actual;
        for (let attempt = 0; attempt < 15; attempt++) {
          const frame = probe(hwnd, 'capture');
          const data = Buffer.from(frame.bgra, 'base64');
          const scale = Math.min(frame.width / 320, frame.height / 200);
          const width = Math.floor(320 * scale), height = Math.floor(200 * scale);
          const px = Math.floor((frame.width - width) / 2) + Math.floor((x + 0.5) * width / 320);
          const py = Math.floor((frame.height - height) / 2) + Math.floor((y + 0.5) * height / 200);
          const offset = (py * frame.width + px) * 4;
          actual = [data[offset + 2], data[offset + 1], data[offset]];
          if (actual.join() === expected.join()) break;
          await S.page.waitForTimeout(100);
        }
        C.eq(actual.join(), expected.join(), `Нативный пиксель примера ${sample}`);
        C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0,
          `Пример ${sample} исполняется без ошибок`);
        await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
        C.eq(probe(hwnd, 'exists').exists, false, `Остановка закрывает SCREEN после ${sample}`);
      }
    },
  },
  {
    id: 'graphics-smooth-screen-frame-filter',
    name: 'SMOOTH работает с готовым кадром SCREEN',
    async fn({ S, C }) {
      const file = writeAxm(testWorkspace('graphics-smooth-screen'), 'smooth.axm',
        'SCREEN 8\nSMOOTH ON\nPSET (10, 10), 12\nPRESET (11, 10), 0\n' +
        'LINE (20, 20) - (220, 80), 10\nLINE (30, 100) - (180, 160), 11, B\n' +
        'LINE (220, 100) - (360, 660), 11, BF\nCIRCLE (420, 160), 50, 14\n' +
        'PRINT "Цвет точки (10, 10): "; POINT(10, 10)\nSMOOTH OFF\nSMOOTH ON\n' +
        'DO WHILE NOT KEYPRESS("ESC")\nLOOP\nSCREEN 0\n');
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.page.waitForFunction(() => window.__ide.execution.surface?.kind === 'screen' &&
        BigInt(window.__ide.execution.surface.windowHandle || '0') > 0n &&
        window.__ide.execution.surface.windowVisible === true);
      C.ok(true, 'SMOOTH ON/OFF исполняются с открытым окном SCREEN и готовым графическим кадром');
      const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
      if (!/^\d+$/.test(hwnd)) throw new Error('Некорректный дескриптор SCREEN');
      const latency = JSON.parse(execFileSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-File',
        path.join(__dirname, '..', 'native-screen-probe.ps1'), '-WindowHandle', hwnd],
        { encoding:'utf8', timeout:45000 }));
      C.eq(latency.count, 12, 'Перемещение, изменение размера и перерисовка нативного SCREEN выполнены 12 раз');
      C.ok(latency.medianMs < 100, 'Перерисовка SCREEN 8 с SMOOTH не блокирует окно на сотни миллисекунд',
        `median=${latency.medianMs.toFixed(2)}ms, max=${latency.maxMs.toFixed(2)}ms`);
      C.note(`SCREEN 8 move/resize/paint: median=${latency.medianMs.toFixed(2)}ms, max=${latency.maxMs.toFixed(2)}ms`);
      await S.page.evaluate(() => window.__ide.stop());
      await S.waitIdle();
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0,
        'Переключение фильтра SCREEN не создаёт диагностики');
    },
  },
  {
    id: 'graphics-smooth-canvas-frame-filter',
    name: 'SMOOTH немедленно переключает сглаживание всего кадра CANVAS',
    async fn({ S, C }) {
      const file = writeAxm(testWorkspace('graphics-smooth-canvas'), 'smooth.axm',
        'SMOOTH ON\nCANVAS 64, 48\nCLS RGB(0, 0, 0)\nDRAWLINE 2, 3, 60, 40, RGB(255, 255, 255)\nDRAWTEXT "Текст кадра", 4, 4, RGB(255, 255, 0)\n' +
        'SLEEP 1500\nSMOOTH OFF\nSLEEP 1500\nSMOOTH ON\nSLEEP\n');
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      const page = await canvasPage(S);
      await S.page.waitForFunction(() => window.__ide.execution.surface?.smooth === true);
      C.ok(true, 'SMOOTH ON до CANVAS включает фильтр нативного окна');
      await S.page.waitForFunction(() => window.__ide.execution.surface?.smooth === false);
      C.ok(true, 'SMOOTH OFF сразу возвращает точечный показ текущего кадра');
      await S.page.waitForFunction(() => window.__ide.execution.surface?.smooth === true);
      C.ok(true, 'Повторный SMOOTH ON меняет показ без повторного рисования');
      await S.page.evaluate(() => window.__ide.stop());
      await S.waitIdle();
      C.eq(page.exists(), false, 'Остановка уничтожает нативное окно CANVAS');
    },
  },
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
      C.eq(page.width, 800, 'CANVAS сохраняет логическую ширину');
      for (const factor of [1, 1.25, 1.5, 2]) {
        page.action('resize', { Width:Math.round(460 * factor), Height:Math.round(320 * factor) });
        await S.page.waitForTimeout(150);
        page.pointer(0.25, 0.25);
        const before = await S.page.evaluate(() => window.__ide.execution.outputText.length);
        page.button('press');
        await S.page.waitForFunction(offset => /Нажата=1 x=\d+ y=\d+ состояние=TRUE/.test(window.__ide.execution.outputText.slice(offset)), before);
        page.button('release');
        await S.page.waitForFunction(offset => window.__ide.execution.outputText.slice(offset).includes('Отпущена=1 состояние=FALSE'), before);
        const added = await S.page.evaluate(offset => window.__ide.execution.outputText.slice(offset), before);
        const coordinates = added.match(/Нажата=1 x=(\d+) y=(\d+) состояние=TRUE/);
        // Один пиксель указателя в уменьшенном окне охватывает несколько пикселей холста.
        C.ok(coordinates && Math.abs(Number(coordinates[1]) - 280) <= 3 && Math.abs(Number(coordinates[2]) - 140) <= 3,
          `Координаты CAMERA/VIEWPORT при масштабе ${factor}`, coordinates?.slice(1).join(','));
        C.eq((added.match(/Нажата=1/g) || []).length, 1, 'Один щелчок доставляется один раз');
      }
      for (const [name, number] of [['right', 2], ['middle', 3]]) {
        page.pointer(0.25, 0.25);
        page.button('press', number);
        await S.page.waitForFunction(number => new RegExp(`Нажата=${number} x=\\d+ y=\\d+ состояние=TRUE`).test(window.__ide.execution.outputText), number);
        page.button('release', number);
        await S.waitConsole(`Отпущена=${number} состояние=FALSE`);
        C.ok(true, `Кнопка ${number} нажимается и отпускается`);
      }
      page.action('wheel', { Delta:-120, X:page.lastX, Y:page.lastY }); await S.waitConsole('Колесо=-120');
      page.action('wheel', { Delta:120, X:page.lastX, Y:page.lastY }); await S.waitConsole('Колесо=120');
      C.ok(true, 'Оба направления колеса доставлены в VM');
      C.eq(page.action('focus').focused, true, 'Нативное окно действительно получило системный фокус');
      const beforeBlur = await S.page.evaluate(() => window.__ide.execution.outputText.length);
      page.button('press');
      await S.page.waitForFunction(offset => window.__ide.execution.outputText.slice(offset).includes('состояние=TRUE'), beforeBlur);
      await S.electronApp.evaluate(({ BrowserWindow }) => {
        const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'));
        window.focus(); window.webContents.focus();
      });
      const ideHandle = await S.electronApp.evaluate(({ BrowserWindow }) =>
        BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'))
          .getNativeWindowHandle().readBigUInt64LE(0).toString());
      C.eq(probe(ideHandle, 'focus').focused, true, 'Системный фокус действительно перешёл к IDE');
      C.eq(page.action('state').focused, false, 'Нативный CANVAS действительно потерял фокус');
      await S.page.locator('.xterm-helper-textarea').focus();
      await S.page.keyboard.press('Space');
      await S.waitConsole('Сброс=FALSEFALSEFALSE');
      C.ok(true, 'Настоящая потеря фокуса сбрасывает удержанную кнопку CANVAS');
      page.button('release');
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
        C.ok(page.exists(), 'Открыто нативное окно CANVAS');
        C.eq((await S.electronApp.windows()).filter(p => p.url().endsWith('/canvas.html')).length, 0, 'Дополнительное окно Electron отсутствует');
        page.key('down', 'ArrowRight');
        await page.close(); await S.waitIdle(); await S.waitStatus('Остановлено');
        await S.page.waitForTimeout(150);
        C.eq(page.exists(), false, 'Закрытая нативная поверхность уничтожена');
      }
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Повторы не вызвали ошибок');
    },
  },
];
