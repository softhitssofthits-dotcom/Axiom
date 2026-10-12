const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, tag) {
  const file = writeAxm(testWorkspace(tag), 'первый.axm', 'PRINT 1\n');
  await S.launch(); await S.stubOpenDialog([file]);
  await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 1 }); });
  await S.page.evaluate(async () => { for (const p of [...window.__ide.openFiles.keys()]) await window.__ide.closeFile(p); });
  await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnSave = false; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; });
  return file;
}
module.exports = [
  { id: 'save-all-background-format', name: 'Сохранить всё записывает обе модели без дополнительного форматирования', async fn({ S, C }) {
    const a = await open(S, 'save-all-format'); const b = writeAxm(testWorkspace('save-all-format-second'), 'второй.axm', 'PRINT 2\n');
    await S.page.evaluate(() => window.__ide.editor.getModel().setValue('print 11\n'));
    await S.stubOpenDialog([b]); await S.page.evaluate(() => window.__ide.handleAction('open'));
    await S.page.waitForFunction(p => window.__ide.activeFile === p, b);
    C.ok(await S.page.evaluate(async () => { const i = window.__ide; i.settings.formatOnSave = true; i.editor.getModel().setValue('print 22\n'); return i.saveAll(); }), 'Обе вкладки сохранены');
    C.eq((await S.page.evaluate(p => window.axiom.readFileContent(p), a)).trim(), 'print 11', 'Неактивная вкладка записана без изменения регистра');
    C.eq((await S.page.evaluate(() => window.__ide.editor.getValue())).trim(), 'print 22', 'Сохранение не изменило активную модель');
  } },
  { id: 'save-write-failure', name: 'Ошибка записи сохраняет вкладку и несохранённый текст', async fn({ S, C }) {
    const file = await open(S, 'save-write-failure');
    await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 55\n'));
    await S.electronApp.evaluate(({ ipcMain, dialog }) => { ipcMain.removeHandler('write-file'); ipcMain.handle('write-file', async () => false); dialog.showMessageBox = async () => ({ response: 0 }); });
    await S.page.evaluate(p => window.__ide.closeFile(p), file);
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p) && window.__ide.openFiles.isDirty(p), file), 'Ошибка не закрыла и не очистила документ');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), file), 'PRINT 1\n', 'Файл не изменён');
  } },
  { id: 'save-window-discard-persistence', name: 'Не сохранять исключает правки из восстановления при следующем запуске', async fn({ S, C }) {
    const file = await open(S, 'save-discard'); const profile = S.userData;
    await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 66\n'));
    await S.electronApp.evaluate(({ dialog, BrowserWindow }) => { dialog.showMessageBox = async () => ({ response: 1 }); BrowserWindow.getAllWindows().find(w => w.getTitle().includes('Axiom')).close(); });
    await S.processExit;
    const fs = require('fs'), path = require('path');
    const settings = JSON.parse(fs.readFileSync(path.join(profile, 'settings.json'), 'utf8'));
    C.ok(!settings.openTabs.find(t => t.path === file)?.content, 'Отброшенный текст не сохранён в настройках');
    const slots = [0, 1].map(slot => { const file = path.join(profile, 'documents-recovery-' + slot + '.json'); return { file, data: JSON.parse(fs.readFileSync(file, 'utf8')) }; });
    slots.sort((a, b) => b.data.sequence - a.data.sequence);
    fs.writeFileSync(slots[0].file, '{обрыв');
    await S.launch({ userData: profile });
    await S.page.waitForFunction(p => window.__ide.openFiles.has(p), file);
    C.eq(await S.page.evaluate(p => window.__ide.openFiles.get(p).model.getValue(), file), 'PRINT 1\n', 'После запуска восстановлен текст с диска');
  } },
  { id: 'save-all-documents', name: 'Сохранить всё записывает обе вкладки и сохраняет выбранную', async fn({ S, C }) {
    const a = await open(S, 'save-all'); const b = writeAxm(testWorkspace('save-all-second'), 'второй.axm', 'PRINT 2\n');
    await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 11\n'));
    await S.stubOpenDialog([b]); await S.page.evaluate(() => window.__ide.handleAction('open'));
    await S.page.waitForFunction(p => window.__ide.activeFile === p, b);
    C.ok(await S.page.evaluate(async () => { const i = window.__ide; i.editor.getModel().setValue('PRINT 22\n'); return i.saveAll(); }), 'Все документы сохранены');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), a), 'PRINT 11\n', 'Первая вкладка записана');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), b), 'PRINT 22\n', 'Вторая вкладка записана');
    C.eq(await S.page.evaluate(() => window.__ide.activeFile), b, 'Выбор вкладки сохранён');
  } },
  { id: 'save-close-decisions', name: 'Отмена сохраняет вкладку, Сохранить записывает и закрывает', async fn({ S, C }) {
    const file = await open(S, 'save-close');
    await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 33\n'));
    await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 2 }); });
    await S.page.evaluate(p => window.__ide.closeFile(p), file);
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p), file), 'Отмена оставляет вкладку');
    await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 0 }); });
    await S.page.evaluate(p => window.__ide.closeFile(p), file);
    C.ok(!(await S.page.evaluate(p => window.__ide.openFiles.has(p), file)), 'После сохранения вкладка закрыта');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), file), 'PRINT 33\n', 'Сохранён изменённый текст');
  } },
  { id: 'save-close-untitled-cancel', name: 'Отмена Сохранить как сохраняет безымянную вкладку', async fn({ S, C }) {
    await open(S, 'save-untitled');
    const path = await S.page.evaluate(() => { window.__ide.newFile(); window.__ide.editor.getModel().setValue('PRINT "Несохранённый документ"\n'); return window.__ide.activeFile; });
    await S.stubSaveDialog(null);
    await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 0 }); });
    await S.page.evaluate(p => window.__ide.closeFile(p), path);
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p), path), 'Вкладка осталась после отмены выбора пути');
    const saved = writeAxm(testWorkspace('save-untitled-target'), 'новый.axm', 'PRINT 0\n'); await S.stubSaveDialog(saved);
    await S.page.evaluate(p => window.__ide.closeFile(p), path);
    C.ok(!(await S.page.evaluate(p => window.__ide.openFiles.has(p), saved)), 'Безымянная вкладка сохранилась и закрылась');
    C.eq(require('node:fs').readFileSync(saved,'utf8'),'PRINT "Несохранённый документ"\n','Сохранить как записал кириллический текст без потерь');
  } },
  { id: 'save-window-cancel', name: 'Отмена выхода оставляет окно и изменённый текст', async fn({ S, C }) {
    await open(S, 'save-window'); await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 44\n'));
    await S.electronApp.evaluate(({ dialog, BrowserWindow }) => { dialog.showMessageBox = async () => ({ response: 2 }); BrowserWindow.getAllWindows().find(w => w.getTitle().includes('Axiom')).close(); });
    await S.page.waitForFunction(() => !window.__ide._closingWindow);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 44\n', 'Окно и несохранённый текст остались');
  } },
  { id: 'save-all-error-preserves-dirty-documents', name: 'Ошибка одного файла сообщает путь и сохраняет остальные документы', async fn({ S, C }) {
    const a = writeAxm(testWorkspace('save-all-error-first'), 'первый.axm', 'PRINT 1\n');
    const b = writeAxm(testWorkspace('save-all-error-second'), 'второй.axm', 'PRINT 2\n');
    await S.launch({settings:{checkOnChange:false,formatOnBlur:false,smartIdentifierEnabled:false}});
    await S.page.evaluate(async () => { const i=window.__ide; for (const p of [...i.openFiles.keys()]) await i.closeFile(p,true); });
    await S.page.evaluate(async paths => { const i=window.__ide; await i.openFile(paths[0]); await i.openFile(paths[1]); i.openFiles.get(paths[0]).model.setValue('PRINT 11\n'); i.openFiles.get(paths[1]).model.setValue('PRINT 22\n'); }, [a,b]);
    await S.electronApp.evaluate(({ipcMain}, failedPath) => { global.__saveAllAttempts=[]; ipcMain.removeHandler('write-file'); ipcMain.handle('write-file',async (_event,path,content)=>{global.__saveAllAttempts.push(path); if(path===failedPath) return false; process.getBuiltinModule('node:fs').writeFileSync(path,content,'utf8'); return true;}); }, a);
    await S.page.evaluate(() => { window.__lastSaveError=null; window.alert=message=>{window.__lastSaveError=String(message);}; });
    C.eq(await S.page.evaluate(() => window.__ide.saveAll()),false,'Сохранить всё сообщает отказ при первой ошибке');
    C.eq(JSON.stringify(await S.electronApp.evaluate(() => global.__saveAllAttempts)),JSON.stringify([a,b]),'После отказа первого файла Сохранить всё продолжает обход');
    C.contains(await S.page.evaluate(() => window.__lastSaveError),a,'Объяснение называет файл, который не удалось записать');
    C.eq(JSON.stringify(await S.page.evaluate(paths => paths.map(p=>window.__ide.openFiles.isDirty(p)),[a,b])),JSON.stringify([true,false]),'Неудачная правка остаётся несохранённой, успешная очищает признак');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p),a),'PRINT 1\n','Исходный файл остался без изменений');
    C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p),b),'PRINT 22\n','Следующий файл после ошибки сохранён');
  } },
];

async function recoveryScenario(S, C, rendererCrash) {
  const fs = require('fs');
  const folder = testWorkspace(rendererCrash ? 'recovery-renderer' : 'recovery-shell');
  const changed = writeAxm(folder, 'правки.axm', 'PRINT 1\n');
  const missing = writeAxm(folder, 'исчезнувший.axm', 'PRINT 2\n');
  const clean = writeAxm(folder, 'сохранённый.axm', 'PRINT 3\n');
  const reappeared = writeAxm(folder, 'появившийся.axm', 'PRINT 5\n');
  await S.launch(); const profile = S.userData;
  const untitled = await S.page.evaluate(async ({ changed, missing, clean, reappeared }) => {
    const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false;
    for (const p of [...i.openFiles.keys()]) await i.closeFile(p, true);
    await i.openFile(changed); i.editor.getModel().setValue('PRINT "Несохранённая правка"\n');
    await i.openFile(missing); i.editor.getModel().setValue('PRINT "Удалённый файл"\n');
    await window.axiom.unwatchFile(missing);
    await i.openFile(clean);
    await i.openFile(reappeared); await window.axiom.unwatchFile(reappeared);
    i.newFile(); const untitled = i.activeFile;
    i.editor.getModel().setValue('PRINT "Безымянная программа"\n');
    i.switchToFile(changed); return untitled;
  }, { changed, missing, clean, reappeared });
  fs.unlinkSync(missing);
  fs.unlinkSync(reappeared);
  await S.page.evaluate(p => window.__ide.handleExternalChange({ path: p, exists: false }), reappeared);
  fs.writeFileSync(changed, 'PRINT "Внешняя версия"\n');
  fs.writeFileSync(clean, 'PRINT 4\n');
  fs.writeFileSync(reappeared, 'PRINT 6\n');
  if (rendererCrash) {
    await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 0 }); });
    const restored = S.electronApp.waitForEvent('window', { timeout: 20000, predicate: async page => {
      try { await page.waitForURL('**/index.html', { timeout: 10000 }); return true; } catch { return false; }
    }});
    await S.electronApp.evaluate(({ BrowserWindow }) => { const w = BrowserWindow.getAllWindows().find(w => w.webContents.getURL().endsWith('/index.html')); setTimeout(() => w.webContents.forcefullyCrashRenderer(), 50); });
    S.page = await restored;
    S.page.on('pageerror', error => S.pageErrors.push(String(error)));
    await S.page.waitForSelector('#app.ready', { timeout: 20000 });
  } else {
    const mainPid = await S.electronApp.evaluate(() => process.pid);
    const closed = S.electronApp.waitForEvent('close', { timeout: 15000 });
    process.kill(mainPid); await closed;
    await S.close(); await S.launch({ userData: profile });
  }
  const snapshot = await S.page.evaluate(paths => ({ active: window.__ide.activeFile, running: window.__ide.running,
    entries: paths.map(p => { const e = window.__ide.openFiles.get(p); return e && { text: e.model.getValue(), saved: e.savedText, dirty: window.__ide.dirty.has(p), missing: !!e.missing }; }) }), [changed, missing, untitled, clean, reappeared]);
  C.eq(snapshot.active, changed, 'Восстановлена выбранная вкладка');
  C.eq(snapshot.running, false, 'Программа не запускается автоматически');
  C.eq(snapshot.entries[0]?.text, 'PRINT "Несохранённая правка"\n', 'Восстановлена последняя правка до аварии');
  C.eq(snapshot.entries[0]?.saved, 'PRINT 1\n', 'Сохранён исходный снимок для обнаружения внешнего конфликта');
  C.ok(snapshot.entries[0]?.dirty, 'Правка не объявлена сохранённой');
  C.eq(snapshot.entries[1]?.text, 'PRINT "Удалённый файл"\n', 'Восстановлен текст отсутствующего файла');
  C.ok(snapshot.entries[1]?.missing && snapshot.entries[1]?.dirty, 'Отсутствующий файл требует сохранения');
  C.eq(snapshot.entries[2]?.text, 'PRINT "Безымянная программа"\n', 'Восстановлена безымянная программа');
  C.ok(snapshot.entries[2]?.dirty, 'Безымянная программа требует сохранения');
  C.eq(snapshot.entries[3]?.text, 'PRINT 4\n', 'Неизменённый документ получает актуальную версию с диска');
  C.eq(fs.readFileSync(changed, 'utf8'), 'PRINT "Внешняя версия"\n', 'Восстановление не перезаписало внешний файл');
  C.eq(fs.existsSync(missing), false, 'Восстановление модели не создаёт файл само');
  C.eq(snapshot.entries[4]?.text, 'PRINT 5\n', 'Исходный буфер исчезавшего файла сохранён после его появления');
  C.ok(snapshot.entries[4]?.dirty, 'Восстановленный буфер исчезавшего файла требует решения');
  C.eq(fs.readFileSync(reappeared, 'utf8'), 'PRINT 6\n', 'Новая внешняя версия появившегося файла сохранена');
  await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 1 }); });
  C.eq(await S.page.evaluate(p => window.__ide.save(p), changed), false, 'Отмена конфликта после восстановления сохраняет обе версии');
  C.ok(await S.page.evaluate(p => window.__ide.save(p), missing), 'Явное сохранение восстанавливает отсутствующий файл');
  C.eq(fs.readFileSync(missing, 'utf8'), 'PRINT "Удалённый файл"\n', 'Восстановленная программа записана явно');
}
module.exports.push({ id: 'save-recovery-main-crash', name: 'Авария основного процесса восстанавливает последнюю правку, безымянный и исчезнувший документы', async fn({ S, C }) { await recoveryScenario(S, C, false); } });
module.exports.push({ id: 'save-recovery-renderer-crash', name: 'Авария интерфейса восстанавливает документы без запуска прежней программы', async fn({ S, C }) { await recoveryScenario(S, C, true); } });
