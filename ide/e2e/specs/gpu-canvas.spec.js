const path = require('node:path');
const { ROOT, testWorkspace, writeAxm } = require('../helpers');
const { execFileSync } = require('node:child_process');
const { nativeCanvas } = require('../native-canvas');

module.exports = [
  {
    id:'gpu-canvas-queue-without-present',
    name:'CANVAS: очередь без PRESENT сохраняет отзывчивость окна и остановку',
    async fn({ S, C }) {
      const file = writeAxm(testWorkspace('gpu-queue'), 'queue.axm',
        'CANVAS 64,48\nBEGINFRAME\nDO\nFILLRECT 0,0,64,48,RGB(10,20,30)\nLOOP\n');
      await S.launch();
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
      const canvas = await nativeCanvas(S);
      await S.page.waitForTimeout(800);
      const latency = JSON.parse(execFileSync('powershell.exe', ['-NoProfile','-NonInteractive','-File',
        path.join(__dirname, '..', 'native-screen-probe.ps1'), '-WindowHandle', canvas.hwnd],
        { encoding:'utf8', timeout:45000 }));
      C.eq(latency.count, 12, 'Двенадцать перемещений и изменений размера доставлены живому окну');
      C.ok(latency.medianMs < 100, 'Поток команд без PRESENT не блокирует обработку окна', JSON.stringify(latency));
      await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
      C.eq(canvas.exists(), false, 'Остановка освобождает окно при заполненной очереди');
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Отмена очереди не создаёт диагностику');
    },
  },
  {
    id:'gpu-canvas-native-tile-layers',
    name:'CANVAS: Tiled рисует видимый слой и явно выбранный скрытый слой в нативном окне',
    async fn({ S, C }) {
      await S.launch();
      const file = path.join(ROOT, 'samples/operators/drawtilemap/visible_and_layer.axm');
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
      const canvas = await nativeCanvas(S);
      const hasTile = (left, top, background) => {
        const frame = canvas.capture(), b = canvas.bounds(frame);
        const backgroundOffset=(Math.floor(b.y+0.5*b.height/canvas.height)*frame.width+Math.floor(b.x+0.5*b.width/canvas.width))*4;
        if(frame.data[backgroundOffset+2]!==background[0] || frame.data[backgroundOffset+1]!==background[1] || frame.data[backgroundOffset]!==background[2]) return false;
        for(let ly=top;ly<top+32;ly++) for(let lx=left;lx<left+32;lx++) {
          const x=b.x+Math.floor((lx+0.5)*b.width/canvas.width), y=b.y+Math.floor((ly+0.5)*b.height/canvas.height);
          const i=(y*frame.width+x)*4;
          if(frame.data[i+2]!==background[0] || frame.data[i+1]!==background[1] || frame.data[i]!==background[2]) return true;
        }
        return false;
      };
      await canvas.wait(() => hasTile(24,32,[16,20,28]));
      C.ok(true, 'Видимый тайловый слой появился в первом GPU-кадре');
      canvas.pressKey('Space');
      await canvas.wait(() => hasTile(88,32,[10,14,22]));
      C.ok(true, 'Явно выбранный скрытый слой появился во втором GPU-кадре');
      canvas.pressKey('Space'); await S.waitIdle();
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Карта и два слоя исполнены без ошибок');
    },
  },
  {
    id:'gpu-canvas-native-game-frame',
    name:'CANVAS: сто движущихся кругов, нативный кадр, фильтр и выход по ESC',
    async fn({ S, C }) {
      await S.launch();
      const file = path.join(ROOT, 'samples/operators/canvas/native_game_frame.axm');
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
      const canvas = await nativeCanvas(S);
      const driver = await S.page.evaluate(() => window.__ide.execution.surface.graphicsDriver);
      C.ok(['Hardware', 'WARP'].includes(driver), 'Служебный процесс сообщил действующий графический драйвер', driver);
      await canvas.wait(() => {
        const frame = canvas.capture();
        for(let i=0;i<frame.data.length;i+=4)
          if(frame.data[i+2]===255 && frame.data[i+1]===190 && frame.data[i]===60) return true;
        return false;
      });
      C.ok(true, 'В клиентской области видны цветные круги из GPU-кадра');
      canvas.pressKey('2');
      await S.page.waitForFunction(() => window.__ide.execution.surface?.smooth === true);
      canvas.pressKey('1');
      await S.page.waitForFunction(() => window.__ide.execution.surface?.smooth === false);
      C.ok(true, 'Клавиши переключили фильтр работающего кадра');
      canvas.pressKey('Escape');
      await S.waitIdle();
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'Игровой пример завершился без ошибок');
    },
  },
  {
    id:'gpu-canvas-get-put-camera',
    name:'CANVAS: GET и PUT сохраняют индексы и координаты камеры в настоящем запуске',
    async fn({ S, C }) {
      await S.launch();
      for(const [folder, name] of [['get','canvas_block_capture.axm'], ['put','canvas_block_restore.axm']]) {
        const file = path.join(ROOT, 'samples/operators', folder, name);
        await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
        await S.waitIdle();
        const canvas = await nativeCanvas(S);
        C.ok(canvas.exists(), `${folder.toUpperCase()} оставил опубликованный нативный кадр`);
        const text = await S.consoleText();
        C.ok(/6\s+9\s+12\s+15/.test(text), `${folder.toUpperCase()} сохранил четыре независимых индекса`, text);
        C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0,
          `${folder.toUpperCase()} исполнился без диагностики`);
      }
    },
  },
];
