const { testWorkspace, writeAxm } = require('../helpers');
module.exports = [{ id: 'files-create-without-overwrite', name: 'Создание файла не перезаписывает существующий и сохраняет открытую модель', async fn({ S, C }) {
  const root = testWorkspace('project-create'); const old = writeAxm(root, 'существующий.axm', 'PRINT 123\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  await S.page.evaluate(p => window.__ide.openFile(p), old);
  await S.page.evaluate(() => { window.__ide.settings.formatOnBlur = false; window.__ide.editor.getModel().setValue('PRINT 456\n'); });
  await S.stubSaveDialog(old); await S.page.evaluate(() => window.__ide.newProjectFile());
  C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), old), 'PRINT 123\n', 'Существующий файл не перезаписан');
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 456\n', 'Несохранённая модель не заменена');
  const target = require('path').join(root, 'Новая программа.axm'); await S.stubSaveDialog(target);
  await S.page.evaluate(() => window.__ide.newProjectFile());
  C.eq(await S.page.evaluate(() => window.__ide.activeFile), target, 'Созданный файл открыт');
  C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), target), "' Новая программа на Axiom.\n", 'Русский текст записан');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('.project-file')].some(b => b.title === p), target), 'Дерево обновлено');
} }];
module.exports.push({ id: 'files-mixed-line-endings-run', name: 'Run запускает сохранённый файл со смешанными окончаниями строк', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('mixed-lines'), 'mixed.axm', 'DIM value AS INTEGER = 7\r\nPRINT value\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  C.ok(await S.page.evaluate(() => !window.__ide.dirty.has(window.__ide.activeFile)), 'Открытый файл не помечен изменённым');
  await S.page.evaluate(() => window.__ide.run()); await S.waitIdle(); await S.waitConsole('7');
  C.ok(!(await S.diagnosticItems()).some(item => item.text.includes('Сохраните его')), 'Run принимает нормализованные окончания строк');
} });

module.exports.push({ id: 'files-external-reload-preserves-new-edit', name: 'Запоздавшее чтение внешнего файла сохраняет новую правку редактора', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('external-reload-race'), 'правки.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; });
  await S.electronApp.evaluate(({ ipcMain }) => {
    ipcMain.removeHandler('read-file-content');
    ipcMain.handle('read-file-content', () => new Promise(resolve => { global.__releaseExternalRead = () => resolve('PRINT 2\n'); }));
  });
  await S.page.evaluate(p => { window.__externalReload = window.__ide.handleExternalChange({ path: p, exists: true }); }, file);
  await S.electronApp.evaluate(async () => {
    while (!global.__releaseExternalRead) await new Promise(resolve => setTimeout(resolve, 10));
  });
  await S.page.evaluate(() => window.__ide.editor.getModel().setValue('PRINT 99\n'));
  await S.electronApp.evaluate(() => { global.__releaseExternalRead(); });
  await S.page.evaluate(() => window.__externalReload);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 99\n', 'Поздний ответ не уничтожил новую правку');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Новая правка остаётся несохранённой');
} });

module.exports.push({ id: 'files-external-change-decisions', name: 'Внешнее изменение можно отклонить без потери правок и затем принять', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('external-change-decisions'), 'внешний.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; i.editor.getModel().setValue('PRINT 99\n'); });
  S.confirmAction = 'dismiss';
  await S.page.evaluate(p => window.axiom.watchFile(p), file);
  require('fs').writeFileSync(file, 'PRINT 222\n', 'utf8');
  await S.page.waitForEvent('dialog');
  await S.page.waitForFunction(() => window.__ide.fileChangePrompts.size === 0);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 99\n', 'Отказ сохраняет пользовательский текст');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Правки не объявлены сохранёнными');
  S.confirmAction = 'accept';
  require('fs').writeFileSync(file, 'PRINT 3333\n', 'utf8');
  await S.page.waitForFunction(() => window.__ide.editor.getValue() === 'PRINT 3333\n');
  C.ok(await S.page.evaluate(p => !window.__ide.openFiles.isDirty(p), file), 'Принятая версия совпадает с диском');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p), file), 'Документ остаётся открыт');
} });

module.exports.push({ id: 'files-external-reload-reopened-document', name: 'Поздняя перезагрузка не меняет повторно открытую вкладку', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('external-reopen'), 'повтор.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.electronApp.evaluate(({ ipcMain }) => {
    let first = true;
    ipcMain.removeHandler('read-file-content');
    ipcMain.handle('read-file-content', (_e, p) => {
      if (!first) return 'PRINT 1\n';
      first = false;
      return new Promise(resolve => { global.__releaseExternalRead = () => resolve('PRINT 2\n'); });
    });
  });
  await S.page.evaluate(p => { window.__externalReload = window.__ide.handleExternalChange({ path: p, exists: true }); }, file);
  await S.electronApp.evaluate(async () => {
    while (!global.__releaseExternalRead) await new Promise(resolve => setTimeout(resolve, 10));
  });
  await S.page.evaluate(async p => { await window.__ide.closeFile(p); await window.__ide.openFile(p); }, file);
  await S.electronApp.evaluate(() => global.__releaseExternalRead());
  await S.page.evaluate(() => window.__externalReload);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 1\n', 'Ответ для старой модели не применён к новой');
  C.ok(await S.page.evaluate(p => !window.__ide.openFiles.isDirty(p), file), 'Повторно открытый документ согласован с диском');
} });

module.exports.push({ id: 'files-external-deletion-preserves-buffer', name: 'Удаление файла снаружи не уничтожает открытую модель', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('external-delete'), 'удалённый.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; i.editor.getModel().setValue('PRINT 99\n'); });
  S.confirmAction = 'dismiss';
  await S.page.evaluate(p => window.axiom.watchFile(p), file);
  const dialog = S.page.waitForEvent('dialog');
  require('fs').unlinkSync(file);
  await dialog;
  await S.page.waitForFunction(() => window.__ide.fileChangePrompts.size === 0);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 99\n', 'Текст сохранён в редакторе');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Несохранённая правка остаётся видна');
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'Явное сохранение воссоздаёт файл');
  C.eq(require('fs').readFileSync(file, 'utf8'), 'PRINT 99\n', 'Записан пользовательский текст');
} });
