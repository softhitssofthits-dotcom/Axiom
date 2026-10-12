const { execFileSync } = require('node:child_process');
const { testWorkspace, writeAxm } = require('../helpers');
function alive(pid) { try { process.kill(pid, 0); return true; } catch (e) { if(e.code === 'ESRCH') return false; throw e; } }
async function ownedHost(S) {
  const parent = await S.electronApp.evaluate(() => process.pid);
  if (!Number.isSafeInteger(parent) || parent <= 0) throw new Error('Неверный PID оболочки');
  const pid = Number(execFileSync('powershell.exe', ['-NoProfile','-NonInteractive','-Command',
    `Get-CimInstance Win32_Process -Filter 'ParentProcessId = ${parent}' | Where-Object { $_.Name -eq 'Axiom.DesktopHost.exe' } | Select-Object -ExpandProperty ProcessId`], {encoding:'utf8', windowsHide:true}).trim());
  if (!Number.isSafeInteger(pid) || pid <= 0) throw new Error('Не найден единственный собственный DesktopHost');
  return { parent, pid };
}
module.exports = [{
  id:'graphics-shell-hard-crash',
  name:'Аварийное завершение своей оболочки освобождает DesktopHost при CANVAS/INPUT и SCREEN/SLEEP',
  async fn({ S, C }) {
    const dir = testWorkspace('graphics-hard-crash');
    for (const [name, source] of [
      ['canvas', 'CANVAS 40, 30\nBEGINFRAME\nCLS RGB(0,255,0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\n'],
      ['screen', 'SCREEN 10\nCIRCLE (20,20), 10, 14\nSLEEP\n'],
    ]) {
      const file = writeAxm(dir, name + '.axm', source);
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      if(name === 'canvas') await S.waitStatus('Ожидание ввода');
      else await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
      const { parent, pid } = await ownedHost(S);
      const closed = S.electronApp.waitForEvent('close', {timeout:15000});
      // Завершается только основной процесс созданной тестом оболочки.
      process.kill(parent);
      await closed;
      for(let i=0; i<100 && alive(pid); i++) await new Promise(r => setTimeout(r,50));
      const orphan = alive(pid);
      C.eq(orphan, false, `DesktopHost завершился после аварии ${name}`);
      // Убираем только подтверждённый собственный процесс при провале проверки.
      if(orphan) process.kill(pid);
      await S.close();
      await S.launch();
      C.eq(await S.page.evaluate(() => window.__ide.running), false, 'После аварии программа не запускается сама');
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, writeAxm(dir, 'recover.axm','PRINT "Восстановление завершено"\n'));
      await S.waitIdle(); await S.waitConsole('Восстановление завершено');
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(),0,'Повторный запуск исполняется без ошибки');
      await S.close();
    }
  },
}];
module.exports.push({
  id:'graphics-renderer-crash',
  name:'Отказ основного интерфейса прекращает VM и закрывает графику',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('renderer-crash'), 'input.axm', 'CANVAS 40,30\nBEGINFRAME\nCLS RGB(0,255,0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\n');
    await S.launch(); await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
    await S.waitStatus('Ожидание ввода');
    const {pid} = await ownedHost(S);
    await S.electronApp.evaluate(({dialog}) => { dialog.showMessageBox = async () => ({response:0}); });
    const restored = S.electronApp.waitForEvent('window', {timeout:20000, predicate: async page => {
      try { await page.waitForURL('**/index.html', {timeout:10000}); return true; } catch { return false; }
    }});
    await S.electronApp.evaluate(({BrowserWindow}) => { const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html')); setTimeout(() => window.webContents.forcefullyCrashRenderer(),100); });
    for(let i=0;i<100 && alive(pid);i++) await new Promise(r => setTimeout(r,50));
    C.eq(alive(pid), false, 'DesktopHost завершён после отказа интерфейса');
    C.eq(S.electronApp.windows().filter(page => page.url().endsWith('/canvas.html')).length,0,'Графическое окно закрыто');
    S.page = await restored;
    await S.page.waitForSelector('#app.ready', {timeout:20000});
    C.eq(await S.page.evaluate(() => window.__ide.running),false,'Восстановленный интерфейс не запускает прежнюю VM');
    await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, writeAxm(testWorkspace('renderer-recover'),'recover.axm','PRINT 2718\n'));
    await S.waitIdle(); await S.waitConsole('2718');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(),0,'Новая VM работает после восстановления');
    await S.close();
  },
});

module.exports.push({
  id:'graphics-renderer-infinite-stall',
  name:'Бесконечное зависание допускает восстановление из основного процесса без потери завершённых правок',
  async fn({S,C}) {
    const dir = testWorkspace('renderer-infinite-stall');
    const source = 'CANVAS 40,30\nBEGINFRAME\nCLS RGB(0,255,0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\n';
    const file = writeAxm(dir, 'input.axm', source);
    await S.launch();
    await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
    await S.waitStatus('Ожидание ввода');
    await S.page.evaluate(text => window.__ide.editor.getModel().setValue(text), source + "' Завершённая несохранённая правка\n");
    C.eq(await S.editorValue(),source + "' Завершённая несохранённая правка\n",'Правка действительно внесена до зависания');
    const {pid} = await ownedHost(S);
    await S.electronApp.evaluate(({dialog}) => {
      global.stallDialogs = [];
      dialog.showMessageBox = async (_window, options) => {
        global.stallDialogs.push(options);
        return {response:0};
      };
    });
    const restored = S.electronApp.waitForEvent('window', {timeout:25000, predicate: async page => {
      try { await page.waitForURL('**/index.html', {timeout:10000}); return true; } catch { return false; }
    }});
    // Вызов не ожидается: код намеренно никогда не возвращает управление.
    await S.electronApp.evaluate(({BrowserWindow}) => {
      const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'));
      window.webContents.executeJavaScript('while (true) {}').catch(() => {});
      setTimeout(() => window.close(),300);
    });
    try {
      S.page = await restored;
      await S.page.waitForSelector('#app.ready', {timeout:20000});
      for(let i=0;i<100 && alive(pid);i++) await new Promise(r => setTimeout(r,50));
      C.eq(alive(pid),false,'Прежний собственный DesktopHost завершён');
      C.eq(S.electronApp.windows().filter(page => page.url().endsWith('/canvas.html')).length,0,'CANVAS закрыт');
      C.eq(await S.editorValue(),source + "' Завершённая несохранённая правка\n",'Несохранённый текст восстановлен');
      C.eq(await S.page.evaluate(() => window.__ide.running),false,'Прежняя VM не запускается автоматически');
      const dialogs = await S.electronApp.evaluate(() => global.stallDialogs);
      C.eq(dialogs.length,1,'Один диалог восстановления при одновременных событиях');
      C.eq(dialogs[0].buttons.length,3,'Доступны восстановление, ожидание и выход');
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, writeAxm(dir,'recover.axm','PRINT 31415\n'));
      await S.waitIdle(); await S.waitConsole('31415');
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(),0,'Новый запуск после зависания успешен');
    } finally {
      // При исходном дефекте зависшее окно нельзя закрыть через renderer.
      await S.electronApp.evaluate(({BrowserWindow}) => {
        for(const window of BrowserWindow.getAllWindows()) if(window.webContents.getURL().endsWith('/index.html')) window.destroy();
      }).catch(() => {});
    }
  },
});

module.exports.push({
  id:'graphics-renderer-infinite-exit',
  name:'Выход из бесконечно зависшего интерфейса закрывает SCREEN и сохраняет документы',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('renderer-infinite-exit'),'sleep.axm','SCREEN 10\nCIRCLE (20,20),10,14\nSLEEP\n');
    await S.launch();
    await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); },file);
    await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
    const {pid} = await ownedHost(S);
    await S.electronApp.evaluate(({dialog}) => { dialog.showMessageBox = async () => ({response:2}); });
    const closed = S.electronApp.waitForEvent('close',{timeout:25000});
    await S.electronApp.evaluate(({BrowserWindow}) => {
      const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'));
      window.webContents.executeJavaScript('while (true) {}').catch(() => {});
      setTimeout(() => window.close(),300);
    });
    try { await closed; }
    catch(error) {
      await S.electronApp.evaluate(({BrowserWindow}) => BrowserWindow.getAllWindows().forEach(w => w.destroy()));
      throw error;
    }
    for(let i=0;i<100 && alive(pid);i++) await new Promise(r => setTimeout(r,50));
    C.eq(alive(pid),false,'Выход завершил собственный SCREEN/DesktopHost');
    await S.close(); await S.launch();
    C.eq(await S.editorValue(),'SCREEN 10\nCIRCLE (20,20),10,14\nSLEEP\n','Документ доступен после следующего запуска');
    C.eq(await S.page.evaluate(() => window.__ide.running),false,'Программа после выхода не перезапускается');
  },
});

module.exports.push({
  id:'graphics-renderer-bounded-stall',
  name:'После ограниченного зависания интерфейса запрос закрытия завершает собственную VM',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('renderer-stall'),'sleep.axm','SCREEN 10\nCIRCLE (20,20),10,14\nSLEEP\n');
    await S.launch();
    await S.page.evaluate(async p => {
      for(const path of [...window.__ide.openFiles.keys()]) if(path.startsWith('untitled')) await window.__ide.closeFile(path,true);
      await window.__ide.openFile(p); await window.__ide.run();
    },file);
    await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
    const {pid} = await ownedHost(S);
    const closed = S.electronApp.waitForEvent('close',{timeout:15000});
    await S.electronApp.evaluate(({BrowserWindow}) => {
      const window = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html'));
      setTimeout(() => window.close(),300);
    });
    // Блокируется только интерфейс на две секунды; VM и основной процесс независимы.
    await S.page.evaluate(() => { const until = Date.now()+2000; while(Date.now()<until) {} });
    await closed;
    for(let i=0;i<100 && alive(pid);i++) await new Promise(r => setTimeout(r,50));
    C.eq(alive(pid),false,'После восстановления интерфейса закрытие завершило DesktopHost');
    await S.close();
  },
});
