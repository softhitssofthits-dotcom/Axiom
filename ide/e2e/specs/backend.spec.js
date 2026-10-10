const { testWorkspace, writeAxm, ROOT } = require('../helpers');
const path = require('node:path');
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
    let page;
    for (let i=0;i<200;i++) { page=(await S.electronApp.windows()).find(p=>p.url().endsWith('/canvas.html')); if(page)break;await S.page.waitForTimeout(50); }
    if (!page) throw new Error('Maze Chase не открыл холст');
    await page.waitForFunction(() => document.getElementById('program-canvas').width === 1024);
    const playerX = () => page.evaluate(() => {
      const d=document.getElementById('program-canvas').getContext('2d').getImageData(0,30,1024,630).data;
      let sum=0, count=0;
      for(let i=0;i<d.length;i+=4) if(d[i]>200 && d[i+1]>170 && d[i+2]<70) { sum+=(i/4)%1024;count++; }
      return count ? sum/count : null;
    });
    let before;
    for(let i=0;i<100;i++){before=await playerX();if(before!==null)break;await page.waitForTimeout(50);}
    C.ok(before!==null, 'Герой найден по жёлтым пикселям в стартовом коридоре');
    await page.keyboard.down('ArrowRight');
    let after=before;
    for(let i=0;i<100;i++){after=await playerX();if(after!==null && after>before+12)break;await page.waitForTimeout(50);}
    await page.keyboard.up('ArrowRight');
    C.ok(after!==null && after>before+12, `Стрелка переместила героя: ${before} → ${after}`);
    await page.close(); await S.waitIdle(); await S.page.waitForTimeout(300);
    C.eq((await S.electronApp.windows()).filter(p=>p.url().endsWith('/canvas.html')).length, 0, 'После закрытия игры второе окно не появляется');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Игра остановлена без ошибок; предупреждения о переменных допустимы');
  } },
  { id: 'backend-canvas-keyboard', name: 'Клавиши CANVAS, отпускание, потеря фокуса и закрытие', async fn({ S, C }) {
    await open(S, 'CANVAS 16, 12\nDO\nBEGINFRAME\nIF KEYDOWN("RIGHT") THEN\nCLS RGB(255, 0, 0)\nELSE\nCLS RGB(0, 255, 0)\nEND IF\nIF KEYPRESS("SPACE") THEN\nPRINT "Пауза"\nEND IF\nPRESENT\nSLEEP 10\nLOOP\n', 'canvas-keyboard');
    await S.page.evaluate(() => window.__ide.run());
    let page;
    for (let i=0;i<100;i++) { page=(await S.electronApp.windows()).find(p=>p.url().endsWith('/canvas.html')); if(page)break;await S.page.waitForTimeout(50); }
    if (!page) throw new Error('Нет окна CANVAS');
    const color = channel => page.waitForFunction(c => document.getElementById('program-canvas').getContext('2d').getImageData(0,0,1,1).data[c] === 255, channel);
    await color(1); await page.keyboard.down('ArrowRight'); await color(0);
    C.ok(true, 'KEYDOWN видит удерживаемую стрелку');
    await page.keyboard.up('ArrowRight'); await color(1); C.ok(true, 'KEYUP отпускает стрелку');
    await page.keyboard.press('Space'); await S.waitConsole('Пауза'); C.ok(true, 'KEYPRESS получает SPACE');
    await page.keyboard.down('ArrowRight'); await color(0);
    await page.evaluate(() => window.dispatchEvent(new Event('blur'))); await color(1);
    C.ok(true, 'Уход фокуса сбрасывает удерживаемую стрелку');
    await page.close(); await S.waitIdle(); await S.page.waitForTimeout(300);
    C.eq((await S.electronApp.windows()).filter(p=>p.url().endsWith('/canvas.html')).length, 0, 'Поздние кадры не создают второе окно');
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
    let canvasPage;
    await S.page.waitForFunction(() => window.__ide.execution.surface?.kind === 'canvas');
    for (let i = 0; i < 100; i++) {
      canvasPage = (await S.electronApp.windows()).find(p => p.url().endsWith('/canvas.html'));
      if (canvasPage) break;
      await S.page.waitForTimeout(50);
    }
    if (!canvasPage) throw new Error('Нет отдельного окна CANVAS');
    await canvasPage.waitForFunction(() => document.getElementById('program-canvas').getContext('2d').getImageData(0,0,1,1).data[0] === 255);
    C.eq(await canvasPage.evaluate(() => [...document.getElementById('program-canvas').getContext('2d').getImageData(0,0,1,1).data].join(',')), '255,0,0,255', 'Настоящий BGRA кадр преобразован в RGBA');
    C.eq(await S.page.locator('#program-canvas').count(), 0, 'Кадр отсутствует в консоли');
    C.ok((await canvasPage.evaluate(() => innerWidth)) > 1000, 'Большое отдельное окно');
    await canvasPage.close(); await S.waitIdle(); await S.waitStatus('Остановлено');
    C.eq((await S.diagnosticItems()).length, 0, 'Закрытие окна отменяет VM без ошибки');

  } },
  { id: 'backend-canvas-finished-frame', name: 'Последний кадр завершённой программы и повторный запуск', async fn({ S, C }) {
    await open(S, 'CANVAS 16, 12\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nPRINT 42\n', 'backend-canvas-finished');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle(); await S.waitConsole('42');
    let page;
    for (let i=0;i<100;i++) { page=(await S.electronApp.windows()).find(p=>p.url().endsWith('/canvas.html')); if(page)break;await S.page.waitForTimeout(50); }
    if(!page)throw new Error('Последний кадр потерян после finished');
    await page.waitForFunction(()=>document.getElementById('program-canvas').getContext('2d').getImageData(0,0,1,1).data[1]===255);
    C.ok(true,'Последний зелёный кадр остаётся после завершения');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    let replacement;
    for(let i=0;i<100;i++){replacement=(await S.electronApp.windows()).find(p=>p.url().endsWith('/canvas.html'));if(replacement)break;await S.page.waitForTimeout(50);}
    if(!replacement)throw new Error('Повтор не открыл окно последнего кадра');
    await replacement.waitForFunction(()=>document.getElementById('program-canvas').getContext('2d').getImageData(0,0,1,1).data[1]===255);
    C.eq((await S.electronApp.windows()).filter(p=>p.url().endsWith('/canvas.html')).length,1,'Повтор заменяет прежнее окно');
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
