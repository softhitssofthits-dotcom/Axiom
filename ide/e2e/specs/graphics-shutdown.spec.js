const { execFileSync } = require('node:child_process');
const { testWorkspace, writeAxm } = require('../helpers');

async function ownedHostPid(S) {
  const parent = await S.electronApp.evaluate(() => process.pid);
  if (!Number.isSafeInteger(parent) || parent <= 0) throw new Error('Нет PID своей оболочки');
  const result = execFileSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
    `Get-CimInstance Win32_Process -Filter 'ParentProcessId = ${parent}' | Where-Object { $_.Name -eq 'Axiom.DesktopHost.exe' } | Select-Object -ExpandProperty ProcessId`], { encoding:'utf8', windowsHide:true }).trim();
  const pid = Number(result);
  if (!Number.isSafeInteger(pid) || pid <= 0) throw new Error(`Не найден единственный дочерний DesktopHost для ${parent}: ${JSON.stringify(result)}`);
  return pid;
}

function alive(pid) { try { process.kill(pid, 0); return true; } catch (error) { if (error.code === 'ESRCH') return false; throw error; } }

module.exports = [{
  id:'graphics-close-ide-owns-host',
  name:'Выход из Electron при INPUT, SCREEN/SLEEP и цикле CANVAS освобождает свой процесс',
  async fn({ S, C }) {
    const directory = testWorkspace('graphics-close-ide');
    const cases = [
      ['input', 'CANVAS 40, 30\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\n'],
      ['screen', 'SCREEN 10\nCIRCLE (20, 20), 10, 14\nSLEEP\n'],
      ['canvas', 'CANVAS 40, 30\nDO\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nSLEEP 10\nLOOP\n'],
    ];
    for (const [name, source] of cases) {
      const file = writeAxm(directory, name + '.axm', source);
      await S.launch();
      C.eq(await S.page.evaluate(() => window.__ide.running), false, 'Повторный запуск IDE не исполняет прежнюю программу');
      await S.page.evaluate(async p => {
        for (const path of [...window.__ide.openFiles.keys()]) if (path.startsWith('untitled')) await window.__ide.closeFile(path, true);
        await window.__ide.openFile(p); await window.__ide.run();
      }, file);
      if (name === 'input') await S.waitStatus('Ожидание ввода');
      else await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true || window.__ide.execution.surface?.kind === 'canvas');
      const host = await ownedHostPid(S);
      const closed = S.electronApp.waitForEvent('close', { timeout:10000 });
      await S.page.evaluate(() => window.__ide.handleAction('exit'));
      await closed;
      for (let i=0;i<40 && alive(host);i++) await new Promise(resolve => setTimeout(resolve, 50));
      C.eq(alive(host), false, `После выхода при ${name} свой DesktopHost завершился`);
      await S.close();
    }
  },
}, {
  id:'graphics-host-fault-recover',
  name:'После отказа своего DesktopHost Electron отклоняет старый ввод и запускает новую VM',
  async fn({ S, C }) {
    const file = writeAxm(testWorkspace('graphics-host-fault'), 'input.axm',
      'CANVAS 40, 30\nBEGINFRAME\nCLS RGB(0, 255, 0)\nPRESENT\nDIM value AS INTEGER\nINPUT value\nPRINT value\n');
    await S.launch();
    await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
    await S.waitStatus('Ожидание ввода');
    const previous = await S.page.evaluate(() => ({ session:window.__ide.execution.sessionId, request:window.__ide.execution.inputRequestId }));
    const host = await ownedHostPid(S);
    // PID принадлежит только служебному процессу этой тестовой оболочки.
    process.kill(host);
    await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), null, 'Отказ освободил ожидание ввода');
    await S.page.waitForFunction(() => document.getElementById('diagnostics-content').textContent.includes('Служебный процесс'));
    C.ok(true, 'Отказ показан пользователю');
    await S.page.evaluate(() => window.__ide.run()); await S.waitStatus('Ожидание ввода');
    C.ok(await ownedHostPid(S) !== host, 'Повтор создал новый собственный DesktopHost');
    const stale = await S.page.evaluate(p => window.axiom.submitInput(p.session, p.request, '9'), previous);
    C.eq(stale.ok, false, 'Старый ответ не принят новой сессией');
    await S.typeConsole('2718'); await S.waitIdle(); await S.waitConsole('2718');
    C.eq(await S.page.locator('#diagnostics-content .diagnostic-icon.error').count(), 0, 'После восстановления программа завершилась без ошибок');
  },
}];
