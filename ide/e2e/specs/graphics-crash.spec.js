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
