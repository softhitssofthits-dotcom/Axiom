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
    const path = await S.page.evaluate(() => { window.__ide.newFile(); return window.__ide.activeFile; });
    await S.stubSaveDialog(null);
    await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 0 }); });
    await S.page.evaluate(p => window.__ide.closeFile(p), path);
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p), path), 'Вкладка осталась после отмены выбора пути');
    const saved = writeAxm(testWorkspace('save-untitled-target'), 'новый.axm', 'PRINT 0\n'); await S.stubSaveDialog(saved);
    await S.page.evaluate(p => window.__ide.closeFile(p), path);
    C.ok(!(await S.page.evaluate(p => window.__ide.openFiles.has(p), saved)), 'Безымянная вкладка сохранилась и закрылась');
  } },
  { id: 'save-window-cancel', name: 'Отмена выхода оставляет окно и изменённый текст', async fn({ S, C }) {
    await open(S, 'save-window'); await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 44\n'));
    await S.electronApp.evaluate(({ dialog, BrowserWindow }) => { dialog.showMessageBox = async () => ({ response: 2 }); BrowserWindow.getAllWindows().find(w => w.getTitle().includes('Axiom')).close(); });
    await S.page.waitForFunction(() => !window.__ide._closingWindow);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 44\n', 'Окно и несохранённый текст остались');
  } },
];
