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
