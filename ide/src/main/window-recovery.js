const fs = require('node:fs');
const path = require('node:path');

// Диалог принадлежит main: он работает даже при бесконечном цикле renderer.
function attachWindowRecovery({ window, app, dialog, log, stop, replace, saveState }) {
  let prompt = null;
  let recovering = false;
  let closed = false;
  let unresponsive = false;
  let closeTimer = null;
  let rendererGone = false;
  let stopping = null;
  const stopOnce = () => stopping ||= Promise.resolve().then(stop);
  const clearCloseTimer = () => { clearTimeout(closeTimer); closeTimer = null; };
  const english = () => {
    try { return JSON.parse(fs.readFileSync(path.join(app.getPath('userData'), 'settings.json'), 'utf8')).language === 'en'; }
    catch { return false; }
  };
  async function recover(reason, exit = false) {
    if (recovering || closed) return;
    recovering = true;
    clearCloseTimer();
    log.warn('renderer.recovery.started', { reason, exit });
    try {
      // Блокировка новых команд и освобождение графики выполняются до нового окна.
      await stopOnce();
      if (closed || window.isDestroyed()) return;
      saveState();
      if (!exit) replace();
      window.destroy();
      if (exit) app.quit();
    } catch (error) {
      recovering = false;
      log.error('renderer.recovery.failed', error);
    }
  }
  async function offerRecovery(reason) {
    if (closed || recovering || prompt) return;
    if (reason === 'hang') unresponsive = true;
    const en = english();
    const crashed = reason === 'crash';
    const options = {
      type: crashed ? 'error' : 'warning',
      title: en ? 'Axiom IDE — interface recovery' : 'Axiom IDE — восстановление интерфейса',
      message: crashed
        ? (en ? 'The interface process has stopped. The program will be stopped.' : 'Процесс интерфейса завершился. Выполнение программы будет остановлено.')
        : (en ? 'The interface is not responding.' : 'Интерфейс не отвечает.'),
      detail: en
        ? 'Recovery stops the program and closes its graphics windows. Completed edits are restored from the last intact recovery snapshot. The program is not restarted automatically.'
        : 'Восстановление остановит программу и закроет её графические окна. Завершённые правки восстановятся из последнего целого снимка. Программа не запускается автоматически.',
      buttons: crashed
        ? [en ? 'Restore interface' : 'Восстановить интерфейс']
        : (en ? ['Stop and restore', 'Keep waiting', 'Stop and exit'] : ['Остановить и восстановить', 'Продолжить ожидание', 'Остановить и выйти']),
      defaultId: 0,
      cancelId: crashed ? 0 : 1,
      noLink: true,
    };
    // Отказ renderer сразу прекращает VM, как и до введения диалога зависания.
    if (crashed) await stopOnce();
    if (closed || recovering) return;
    prompt = dialog.showMessageBox(window, options);
    try {
      const { response } = await prompt;
      if (closed || recovering || (!crashed && !rendererGone && !unresponsive)) return;
      if (crashed || rendererGone || response === 0 || response === 2) await recover(rendererGone ? 'crash' : reason, response === 2);
    } catch (error) { log.error('renderer.recovery.dialog.failed', error); }
    finally { prompt = null; }
  }
  window.on('unresponsive', () => {
    log.warn('window.unresponsive');
    void offerRecovery('hang').catch(error => log.error('renderer.recovery.failed', error));
  });
  window.on('responsive', () => {
    unresponsive = false;
    clearCloseTimer();
    log.info('window.responsive');
  });
  window.webContents.on('render-process-gone', (_event, details) => {
    log.error('renderer.gone', details);
    rendererGone = true;
    // Принудительное уничтожение во время восстановления не открывает второй диалог.
    if (!closed && !recovering) void stopOnce().then(() => offerRecovery('crash')).catch(error => log.error('renderer.recovery.failed', error));
  });
  window.once('closed', () => { closed = true; clearCloseTimer(); });
  return {
    // Запрос close перехвачен приложением: Chromium может не выдать unresponsive.
    // Таймер гарантирует доступную отмену без ответа зависшего обработчика close.
    requestClose() {
      if (!closeTimer && !recovering && !closed)
        closeTimer = setTimeout(async () => {
          closeTimer = null;
          let probeTimer;
          try {
            const responds = await Promise.race([
              window.webContents.executeJavaScript('true').then(() => true, () => false),
              new Promise(resolve => { probeTimer = setTimeout(() => resolve(false), 1000); }),
            ]);
            if (!responds && !closed) await offerRecovery('hang');
          } catch (error) { log.error('renderer.recovery.failed', error); }
          finally { clearTimeout(probeTimer); }
        }, 5000);
    },
    cancelClose: clearCloseTimer,
  };
}

module.exports = { attachWindowRecovery };
