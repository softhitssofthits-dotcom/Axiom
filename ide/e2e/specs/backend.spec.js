const { testWorkspace, writeAxm, ROOT } = require('../helpers');
const path = require('node:path');
const { nativeCanvas } = require('../native-canvas');
async function open(S, source, tag) {
  const file = writeAxm(testWorkspace(tag), 'программа.axm', source);
  await S.launch(); await S.stubOpenDialog([file]);
  await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
  return file;
}
module.exports = [
  { id: 'backend-maze-chase-controls', name: 'Настоящий Maze Chase: движение героя и единственное окно', async fn({ S, C }) {
    const file = path.join(ROOT, 'samples/graphics/game/maze_chase.axm');
    await S.launch(); await S.stubOpenDialog([file]);
    await S.page.evaluate(() => window.__ide.handleAction('open'));
    await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
    await S.page.evaluate(() => window.__ide.run());
    const page = await nativeCanvas(S);
    C.eq(page.width, 1024, 'Maze Chase открыл нативную логическую поверхность');
    const playerX = () => {
      const frame = page.capture(), bounds = page.bounds(frame), d=frame.data;
      let sum=0, count=0;
      for(let y=bounds.y;y<bounds.y+bounds.height;y++) for(let x=bounds.x;x<bounds.x+bounds.width;x++) {
        const logicalY=(y-bounds.y)*page.height/bounds.height, i=(y*frame.width+x)*4;
        if(logicalY>=30 && logicalY<660 && d[i+2]>200 && d[i+1]>170 && d[i]<70) { sum+=(x-bounds.x)*page.width/bounds.width;count++; }
      }
      return count ? sum/count : null;
    };
    let before;
    for(let i=0;i<30;i++){before=playerX();if(before!==null)break;await S.page.waitForTimeout(50);}
    C.ok(before!==null, 'Герой найден по жёлтым пикселям в стартовом коридоре');
    page.key('down', 'ArrowRight');
    let after=before;
    for(let i=0;i<30;i++){after=playerX();if(after!==null && after>before+12)break;await S.page.waitForTimeout(50);}
    page.key('up', 'ArrowRight');
    C.ok(after!==null && after>before+12, `Стрелка переместила героя: ${before} → ${after}`);
    await page.close(); await S.waitIdle(); await S.page.waitForTimeout(300);
    C.eq(page.exists(), false, 'После закрытия игры нативное окно уничтожено');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Игра остановлена без ошибок; предупреждения о переменных допустимы');
  } },
  { id: 'backend-canvas-keyboard', name: 'Клавиши CANVAS, отпускание, потеря фокуса и закрытие', async fn({ S, C }) {
    await open(S, 'CANVAS 16, 12\nDO\nBEGINFRAME\nIF KEYDOWN("RIGHT") THEN\nCLS RGB(255, 0, 0)\nELSE\nCLS RGB(0, 255, 0)\nEND IF\nIF KEYPRESS("SPACE") THEN\nPRINT "Пауза"\nEND IF\nPRESENT\nSLEEP 10\nLOOP\n', 'canvas-keyboard');
    await S.page.evaluate(() => window.__ide.run());
    const page = await nativeCanvas(S);
    const color = channel => page.wait(() => page.pixel(0, 0)[channel] === 255);
    await color(1); page.key('down', 'ArrowRight'); await color(0);
    C.ok(true, 'KEYDOWN видит удерживаемую стрелку');
    page.key('up', 'ArrowRight'); await color(1); C.ok(true, 'KEYUP отпускает стрелку');
    page.pressKey('Space'); await S.waitConsole('Пауза'); C.ok(true, 'KEYPRESS получает SPACE');
    page.key('down', 'ArrowRight'); await color(0);
    page.action('blur'); await color(1);
    C.ok(true, 'Уход фокуса сбрасывает удерживаемую стрелку');
    await page.close(); await S.waitIdle(); await S.page.waitForTimeout(300);
    C.eq(page.exists(), false, 'Поздние кадры не восстанавливают закрытое нативное окно');
    C.eq((await S.diagnosticItems()).length, 0, 'Закрытие без ошибки');
  } },
  { id: 'backend-input-stop-and-repeat', name: 'Отмена INPUT и чистый повтор', async fn({ S, C }) {
    await open(S, 'DIM value AS INTEGER\nINPUT value\nPRINT value\n', 'backend-input');
    await S.page.evaluate(() => window.__ide.run());
    await S.waitStatus('Ожидание ввода');
    await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(); await S.waitStatus('Остановлено');
    await S.page.evaluate(() => window.__ide.run()); await S.waitStatus('Ожидание ввода');
    await S.typeConsole('314'); await S.waitIdle(); await S.waitConsole('314');
    C.eq((await S.diagnosticItems()).length, 0, 'отмена и повтор не создают MB3003');
  } },
  { id: 'backend-screen-native-window', name: 'Настоящее окно SCREEN из Electron', async fn({ S, C }) {
    await open(S, 'SCREEN 1\nDO\nLOOP\n', 'backend-screen');
    await S.page.evaluate(() => window.__ide.run());
    await S.page.waitForFunction(() => BigInt(window.__ide.execution.surface?.windowHandle || '0') > 0n);
    C.ok(true, 'получен реальный HWND');
    await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
    C.ok(true, 'окно SCREEN действительно видно при скрытом запуске служебного процесса');
    await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
    C.eq((await S.diagnosticItems()).length, 0, 'графическое выполнение отменено без диагностики');
  } },
  { id: 'backend-canvas-present-pixel', name: 'Настоящее отдельное окно CANVAS, пиксели и закрытие', async fn({ S, C }) {
    await open(S, 'CANVAS 16, 12\nBEGINFRAME\nCLS RGB(255, 0, 0)\nPRESENT\nDO\nLOOP\n', 'backend-canvas');
    await S.page.evaluate(() => window.__ide.run());
    const canvasPage = await nativeCanvas(S);
    await canvasPage.wait(() => canvasPage.pixel(0, 0)[0] === 255);
    C.eq(canvasPage.pixel(0, 0).join(','), '255,0,0,255', 'Нативное окно показывает правильный красный пиксель');
    C.eq(await S.page.locator('#program-canvas').count(), 0, 'Кадр отсутствует в консоли');
    const nativeSize = canvasPage.action('size');
    C.ok(nativeSize.width >= 750 && nativeSize.height >= 550, 'Отдельное окно удобно для маленькой логической поверхности', JSON.stringify(nativeSize));
    await canvasPage.close(); await S.waitIdle(); await S.waitStatus('Остановлено');
    C.eq((await S.diagnosticItems()).length, 0, 'Закрытие окна отменяет VM без ошибки');

  } },
  { id: 'backend-canvas-finished-frame', name: 'Последний кадр завершённой программы и повторный запуск', async fn({ S, C }) {
    await open(S, 'CANVAS 16, 12\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nPRINT 42\n', 'backend-canvas-finished');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle(); await S.waitConsole('42');
    const page = await nativeCanvas(S);
    await page.wait(() => page.pixel(0, 0)[1] === 255);
    C.ok(true,'Последний зелёный кадр остаётся после завершения');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    const replacement = await nativeCanvas(S);
    await replacement.wait(() => replacement.pixel(0, 0)[1] === 255);
    C.ok(replacement.exists(), 'Повтор открыл нативное окно последнего кадра');
    C.eq((await S.electronApp.windows()).filter(p=>p.url().endsWith('/canvas.html')).length,0,'CANVAS не создаёт дополнительное окно Electron');
  } },
  { id: 'backend-debug-step-and-continue', name: 'Пауза, шаг и продолжение в Electron', async fn({ S, C }) {
    await open(S, 'DIM value AS INTEGER = 4\nvalue = value + 1\nPRINT value\n', 'backend-debug');
    await S.page.evaluate(() => window.__ide.run('debug'));
    await S.page.waitForFunction(() => window.__ide.execution.stopId !== null);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), 1, 'первая исполняемая строка');
    await S.page.evaluate(() => window.__ide.execution.debug('stepOver'));
    await S.page.waitForFunction(() => window.__ide.execution.stopId !== null && window.__ide.editor.getPosition().lineNumber === 2);
    await S.page.evaluate(() => window.__ide.execution.debug('continue'));
    await S.waitIdle(); await S.waitConsole('5'); C.ok(true, 'продолжение выводит ожидаемое значение');
  } },
];
