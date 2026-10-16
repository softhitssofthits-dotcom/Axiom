const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');

async function command(S, name) {
  const old = await S.page.evaluate(() => window.__ide.execution.stopId);
  await S.page.evaluate(name => window.__ide.trace.command(name), name);
  await S.page.waitForFunction(old => !window.__ide.running ||
    (!!window.__ide.execution.stopId && window.__ide.execution.stopId !== old && !window.__ide.execution.debugBusy), old);
}

module.exports = ['screen', 'canvas'].map(kind => ({
  id: `trace-stepback-${kind}-boundary`,
  name: `${kind.toUpperCase()}: StepBack сохраняет кадр и не пересекает графический эффект`,
  async fn({ S, C }) {
    const lines = ['DIM count AS INTEGER = 10', kind === 'screen' ? 'SCREEN 1' : 'CANVAS 320,200'];
    if (kind === 'canvas') lines.push('BEGINFRAME');
    lines.push(kind === 'screen' ? 'LINE (90,70)-(110,90),12,BF' : 'FILLRECT 90,70,20,20,RGB(255,0,0)');
    if (kind === 'canvas') lines.push('PRESENT');
    const boundary = lines.length;
    lines.push('count = count + 1', 'count = count + 1', 'IF count <> 12 THEN ERROR "COUNT", "Неверное значение после отката"');
    const file = writeAxm(testWorkspace(`trace-graphics-${kind}`), 'граница.axm', lines.join('\n') + '\n');
    await S.launch();
    await S.page.evaluate(async ({ file, line }) => {
      const ide = window.__ide;
      await ide.openFile(file);
      ide.editor.setPosition({ lineNumber: line, column: 1 }); ide.trace.toggle();
      await ide.run('debug', false);
    }, { file, line: boundary + 3 });
    await S.page.waitForFunction(line => !!window.__ide.execution.stopId && window.__ide.execution.line === line, boundary + 3);
    await S.page.waitForFunction(kind => window.__ide.execution.surface?.kind === kind && window.__ide.execution.surface.windowVisible, kind);
    const surface = await S.page.evaluate(() => window.__ide.execution.surface);
    C.ok(['Hardware', 'WARP'].includes(surface.graphicsDriver), 'Используется DirectX');
    function pixel() {
      const f = probe(surface.windowHandle, 'capture'), data = Buffer.from(f.bgra, 'base64');
      const scale = Math.min(f.width / 320, f.height / 200);
      const w = Math.floor(320 * scale), h = Math.floor(200 * scale);
      const x = Math.floor((f.width - w) / 2) + Math.floor(100.5 * w / 320);
      const y = Math.floor((f.height - h) / 2) + Math.floor(80.5 * h / 200);
      const offset = (y * f.width + x) * 4;
      return [data[offset + 2], data[offset + 1], data[offset]].join();
    }
    const expected = kind === 'screen' ? '255,85,85' : '255,0,0';
    for (let i = 0; i < 15 && pixel() !== expected; i++) await S.page.waitForTimeout(100);
    C.eq(pixel(), expected, 'Нативный кадр содержит нарисованный прямоугольник');
    C.eq(await S.page.evaluate(() => window.__ide.execution.canStepBack), true, 'После обычных вычислений откат доступен');
    let steps = 0;
    while (await S.page.evaluate(() => window.__ide.execution.canStepBack) && steps++ < 20) {
      await command(S, 'stepBack');
      C.ok(await S.page.evaluate(line => window.__ide.execution.line > line, boundary), 'Откат не пересёк графическую операцию');
      C.eq(pixel(), expected, 'Кадр сохранился при откате вычислений');
    }
    C.ok(steps > 0 && steps < 20, 'Достигнута конечная графическая граница');
    C.eq(await S.page.locator('#tb-stepBack').isEnabled(), false, 'На границе кнопка недоступна');
    const stop = await S.page.evaluate(() => window.__ide.execution.stopId);
    await S.page.keyboard.press('Control+F10');
    C.eq(await S.page.evaluate(() => window.__ide.execution.stopId), stop, 'Клавиша не пересекла графическую границу');
    await command(S, 'stepOver');
    C.eq(pixel(), expected, 'Шаг вперёд сохранил опубликованный кадр');
    for (let i = 0; i < 5 && await S.page.evaluate(line => window.__ide.execution.line < line, boundary + 3); i++) {
      await command(S, 'stepOver');
    }
    C.eq(await S.page.evaluate(() => window.__ide.execution.line), boundary + 3, 'Повторные шаги дошли до проверки значения');
    await S.page.evaluate(() => window.__ide.trace.command('start'));
    await S.waitIdle();
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Продолжение восстановило count = 12 без ошибки');
  },
}));
