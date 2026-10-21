const { testWorkspace, writeAxm } = require('../helpers');
function mazeWorkspace(tag, descriptor = false) {
  const fs = require('node:fs'), path = require('node:path'), { ROOT } = require('../helpers');
  const root = testWorkspace(tag), source = path.join(ROOT, 'samples/graphics/game/maze_chase_project');
  fs.cpSync(source, root, { recursive: true, filter: file => !file.endsWith('.axproj') || (descriptor && path.basename(file) === 'maze_chase.axproj') });
  return root;
}
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
  C.eq(await S.page.evaluate(p => window.axiom.readFileContent(p), target), '', 'Новый файл пуст');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('.project-file')].some(b => b.title === p), target), 'Дерево обновлено');
} }];
module.exports.push({ id: 'files-save-as-renames-open-project-document', name: 'Сохранить как согласует новый файл, вкладку и дерево проекта', async fn({ S, C }) {
  const fs = require('node:fs'); const path = require('node:path');
  const root = testWorkspace('project-save-as-rename');
  const original = writeAxm(root, 'исходный.axm', 'PRINT 1\n');
  const renamed = path.join(root, 'новое имя.axm');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  await S.page.evaluate(p => window.__ide.openFile(p), original);
  await S.page.evaluate(() => { window.__ide.settings.formatOnBlur = false; window.__ide.editor.getModel().setValue('PRINT 2\n'); });
  await S.stubSaveDialog(renamed);
  C.ok(await S.page.evaluate(() => window.__ide.saveAs()), 'Сохранить как завершилось успешно');
  C.eq(await S.page.evaluate(() => window.__ide.activeFile), renamed, 'Активный путь вкладки обновлён');
  C.ok(await S.page.evaluate(paths => window.__ide.openFiles.has(paths.renamed) && !window.__ide.openFiles.has(paths.original), { renamed, original }), 'Документ переименован в хранилище вкладок');
  C.eq(fs.readFileSync(renamed, 'utf8'), 'PRINT 2\n', 'Новая копия создана с текущим текстом');
  C.eq(fs.readFileSync(original, 'utf8'), 'PRINT 1\n', 'Исходный файл не изменён сохранением под новым именем');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('.editor-tab')].some(tab => tab.dataset.path === p && tab.querySelector('.tab-name').textContent === 'новое имя.axm'), renamed), 'Подпись вкладки соответствует новому имени');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('#project-files .project-file')].some(item => item.title === p), renamed), 'Новое имя сразу появилось в дереве проекта');
} });
module.exports.push({ id: 'files-project-external-open', name: 'Файл вне проекта открывается и остаётся доступен отдельно от дерева проекта', async fn({ S, C }) {
  const root = testWorkspace('project-external-root');
  const inside = writeAxm(root, 'внутри.axm', 'PRINT "В проекте"\n');
  const outside = writeAxm(testWorkspace('project-external-outside'), 'снаружи.axm', 'PRINT "Вне проекта"\n');
  await S.launch();
  await S.page.evaluate(p => window.__ide.openProject(p), root);
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('#project-files .project-file')].some(item => item.title === p), inside), 'Внутренний файл показан в дереве проекта');
  await S.page.evaluate(p => window.__ide.openFile(p), inside);
  await S.stubOpenDialog([outside]);
  await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, outside);
  await S.page.evaluate(p => window.__ide.switchToFile(p), inside);
  C.eq(await S.page.locator('#external-files').count(),0,'Раздел прочих файлов отсутствует');
  C.eq(await S.page.locator('#project-files .project-file').count(),1,'В дереве только файл проекта');
  await S.page.evaluate(p => window.__ide.switchToFile(p), outside);
  C.eq(await S.page.evaluate(() => window.__ide.activeFile), outside, 'Внешний документ остаётся доступен во вкладках');
  C.eq(await S.editorValue(), 'PRINT "Вне проекта"\n', 'Текст внешнего файла доступен в редакторе');
  await S.page.evaluate(() => window.__ide.closeProject());
  C.ok(await S.page.evaluate(paths => window.__ide.openFiles.has(paths.outside) && !window.__ide.openFiles.has(paths.inside), { outside, inside }), 'Закрытие проекта оставляет внешний файл и закрывает внутренний');
  C.eq(await S.page.locator('#external-files').count(),0,'Закрытие проекта не создаёт внешнюю группу');
} });
module.exports.push({ id: 'files-project-hides-windows-hidden-and-system-items', name: 'Дерево проекта исключает скрытые и системные файлы и папки Windows', async fn({ S, C }) {
  const fs = require('node:fs'); const path = require('node:path');
  const { execFileSync } = require('node:child_process');
  const root = testWorkspace('project-windows-attributes');
  const visible = writeAxm(root, 'обычный.axm', 'PRINT 1\n');
  const hiddenFile = writeAxm(root, 'скрытый.axm', 'PRINT 2\n');
  const systemFile = writeAxm(root, 'системный.axm', 'PRINT 3\n');
  const hiddenDir = path.join(root, 'скрытая папка'); fs.mkdirSync(hiddenDir);
  const hiddenChild = writeAxm(hiddenDir, 'внутри скрытой.axm', 'PRINT 4\n');
  const systemDir = path.join(root, 'системная папка'); fs.mkdirSync(systemDir);
  const systemChild = writeAxm(systemDir, 'внутри системной.axm', 'PRINT 5\n');
  const attributed = [hiddenFile, systemFile, hiddenDir, systemDir];
  try {
    execFileSync('attrib.exe', ['+H', hiddenFile], { windowsHide: true });
    execFileSync('attrib.exe', ['+S', systemFile], { windowsHide: true });
    execFileSync('attrib.exe', ['+H', hiddenDir], { windowsHide: true });
    execFileSync('attrib.exe', ['+S', systemDir], { windowsHide: true });
    await S.launch();
    const paths = await S.page.evaluate(p => window.axiom.listProjectFiles(p), root);
    C.ok(paths.includes(visible), 'Обычный файл остаётся в дереве проекта');
    C.ok(!paths.includes(hiddenFile), 'Файл с атрибутом «Скрытый» исключён');
    C.ok(!paths.includes(systemFile), 'Файл с атрибутом «Системный» исключён');
    C.ok(!paths.includes(hiddenChild), 'Обход не входит в скрытую папку');
    C.ok(!paths.includes(systemChild), 'Обход не входит в системную папку');
  } finally {
    for (const item of attributed) {
      try { execFileSync('attrib.exe', ['-H', '-S', item], { windowsHide: true }); } catch {}
    }
  }
} });
module.exports.push({ id: 'files-mixed-line-endings-run', name: 'Run запускает сохранённый файл со смешанными окончаниями строк', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('mixed-lines'), 'mixed.axm', 'DIM value AS INTEGER = 7\r\nPRINT value\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  C.ok(await S.page.evaluate(() => !window.__ide.dirty.has(window.__ide.activeFile)), 'Открытый файл не помечен изменённым');
  await S.page.evaluate(() => window.__ide.run()); await S.waitIdle(); await S.waitConsole('7');
  C.ok(!(await S.diagnosticItems()).some(item => item.text.includes('Сохраните его')), 'Run принимает нормализованные окончания строк');
} });
module.exports.push({ id: 'files-save-external-conflict', name: 'Внешнее изменение перед сохранением требует решения и сохраняет обе версии при отмене', async fn({ S, C }) {
  const fs = require('fs'); const file = writeAxm(testWorkspace('save-external-conflict'), 'конфликт.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(async p => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; await window.axiom.unwatchFile(p); i.editor.getModel().setValue('PRINT 99\n'); }, file);
  fs.writeFileSync(file, 'PRINT 222\n');
  await S.electronApp.evaluate(({ dialog }) => { global.__saveConflictMessages = []; dialog.showMessageBox = async options => { global.__saveConflictMessages.push(options); return { response: 1 }; }; });
  C.eq(await S.page.evaluate(() => window.__ide.save()), false, 'Отмена конфликта отменяет сохранение');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 222\n', 'Внешняя версия не перезаписана');
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 99\n', 'Правка редактора сохранена');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Документ остаётся изменённым');
  C.eq(await S.electronApp.evaluate(() => global.__saveConflictMessages.length), 1, 'Конфликт обнаружен без события наблюдателя');
  await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 0 }); });
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'Явное подтверждение разрешает замену');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 99\n', 'Записана выбранная версия');
} });
module.exports.push({ id: 'files-external-deletion-clean-document', name: 'Удалённый неизменённый файл требует решения перед закрытием и восстанавливается сохранением', async fn({ S, C }) {
  const fs = require('fs'); const file = writeAxm(testWorkspace('delete-clean-document'), 'исчезнувший.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(async () => { for (const p of [...window.__ide.openFiles.keys()]) if (p.startsWith('untitled')) await window.__ide.closeFile(p, true); });
  await S.page.evaluate(p => window.axiom.watchFile(p), file);
  fs.unlinkSync(file);
  await S.page.waitForFunction(p => window.__ide.openFiles.get(p)?.missing, file, { timeout: 10000 });
  await S.page.waitForFunction(() => window.__ide.fileChangePrompts.size === 0);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 1\n', 'Исходный текст сохранён в памяти');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Отсутствующий файл требует сохранения');
  await S.electronApp.evaluate(({ dialog }) => { dialog.showMessageBox = async () => ({ response: 2 }); });
  await S.page.evaluate(p => window.__ide.closeFile(p), file);
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p), file), 'Отмена закрытия сохраняет единственную копию');
  C.ok(await S.page.evaluate(() => window.__ide.saveAll()), 'Сохранить всё восстанавливает неизменённый текст');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 1\n', 'Файл восстановлен');
  C.ok(await S.page.evaluate(p => !window.__ide.openFiles.isDirty(p), file), 'После записи документ сохранён');
  await S.page.evaluate(p => window.axiom.unwatchFile(p), file); fs.unlinkSync(file);
  await S.page.evaluate(p => window.__ide.closeFile(p), file);
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.has(p) && window.__ide.openFiles.isDirty(p), file), 'Закрытие обнаруживает исчезновение до уведомления наблюдателя');
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'Явное сохранение восстанавливает файл после отмены закрытия');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 1\n', 'Единственная копия текста восстановлена');
} });
module.exports.push({ id: 'files-repeated-external-change-during-read', name: 'Повторное внешнее изменение во время чтения приводит к последней версии', async fn({ S, C }) {
  const file = writeAxm(testWorkspace('repeat-external-read'), 'повторные_правки.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.electronApp.evaluate(({ ipcMain }) => {
    let reads = 0; ipcMain.removeHandler('read-file-content');
    ipcMain.handle('read-file-content', () => {
      if (++reads > 1) return 'PRINT 333\n';
      return new Promise(resolve => { global.__releaseRepeatedRead = () => resolve('PRINT 22\n'); });
    });
  });
  await S.page.evaluate(p => { window.__repeatedRead = window.__ide.handleExternalChange({ path: p, exists: true }); }, file);
  await S.electronApp.evaluate(async () => { while (!global.__releaseRepeatedRead) await new Promise(resolve => setTimeout(resolve, 10)); });
  await S.page.evaluate(p => window.__ide.handleExternalChange({ path: p, exists: true }), file);
  await S.electronApp.evaluate(() => global.__releaseRepeatedRead());
  await S.page.evaluate(() => window.__repeatedRead);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 333\n', 'Устаревший ответ пропущен, прочитана последняя версия');
  C.ok(await S.page.evaluate(p => !window.__ide.openFiles.isDirty(p), file), 'Сохранённый снимок соответствует последнему чтению');
  C.eq(await S.page.evaluate(() => window.__ide.fileChangePrompts.size), 0, 'Обработчик освобождён после повторного чтения');
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
  require('fs').unlinkSync(file);
  await S.page.waitForFunction(p => window.__ide.openFiles.get(p)?.missing, file, { timeout: 10000 });
  await S.page.waitForFunction(() => window.__ide.fileChangePrompts.size === 0);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 99\n', 'Текст сохранён в редакторе');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Несохранённая правка остаётся видна');
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'Явное сохранение воссоздаёт файл');
  C.eq(require('fs').readFileSync(file, 'utf8'), 'PRINT 99\n', 'Записан пользовательский текст');
} });
async function projectMenu(S, relativeText) {
  const entry = S.page.locator('#project-files .project-file').filter({ hasText: relativeText });
  await entry.click({ button: 'right' });
  await S.page.locator('#ax-project-context-menu:not([hidden])').waitFor();
}
async function rootMenu(S) {
  await S.page.locator('#project-title').click({ button: 'right' });
  await S.page.locator('#ax-project-context-menu:not([hidden])').waitFor();
}
module.exports.push({ id: 'files-project-copy-paste-conflict', name: 'Контекстное меню копирует файл в корень и не перезаписывает конфликт', async fn({ S, C }) {
  const fs = require('fs'); const path = require('node:path');
  const root = testWorkspace('project-copy-paste');
  const folder = path.join(root, 'вложенная папка'); fs.mkdirSync(folder);
  const source = writeAxm(folder, 'копия.axm', 'PRINT "Источник"\n');
  const conflict = writeAxm(root, 'копия.axm', 'PRINT "Не трогать"\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  await projectMenu(S, 'вложенная папка/копия.axm');
  C.ok(await S.page.getByRole('menuitem', { name: 'Копировать', exact: true }).isEnabled(), 'Копирование доступно для файла проекта');
  await S.page.getByRole('menuitem', { name: 'Копировать', exact: true }).click();
  await S.waitStatus('Скопировано');
  await rootMenu(S);
  C.ok(await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).isEnabled(), 'Вставка доступна после копирования');
  await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).click();
  await S.waitStatus('не выполнена');
  C.eq(fs.readFileSync(conflict, 'utf8'), 'PRINT "Не трогать"\n', 'Конфликт не перезаписал назначение');
  C.eq(fs.readFileSync(source, 'utf8'), 'PRINT "Источник"\n', 'Источник копирования сохранён');
  fs.unlinkSync(conflict);
  await rootMenu(S);
  await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).click();
  await S.waitStatus('Скопировано в проект');
  C.eq(fs.readFileSync(conflict, 'utf8'), 'PRINT "Источник"\n', 'Копия создана в корне проекта');
  C.ok(fs.existsSync(source), 'Копирование сохранило исходный файл');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('#project-files .project-file')].some(item => item.dataset.path === p), conflict), 'Дерево показывает новый файл');
} });
module.exports.push({ id: 'files-project-locked-source-preserves-buffer', name: 'Заблокированный источник не теряет файл и позволяет повторить вставку', async fn({ S, C }) {
  const fs = require('node:fs'); const path = require('node:path'); const { spawn } = require('node:child_process');
  const root = testWorkspace('project-locked-source');
  const folder = path.join(root, 'источник'); fs.mkdirSync(folder);
  const source = writeAxm(folder, 'заблокированный.axm', 'PRINT 7\n');
  const target = path.join(root, 'заблокированный.axm');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  await projectMenu(S, 'источник/заблокированный.axm');
  await S.page.getByRole('menuitem', { name: 'Копировать', exact: true }).click();
  await S.waitStatus('Скопировано');
  const script = "$stream=[System.IO.File]::Open($env:AXIOM_LOCK_FILE,[System.IO.FileMode]::Open,[System.IO.FileAccess]::ReadWrite,[System.IO.FileShare]::None);[Console]::Out.WriteLine('ready');[Console]::Out.Flush();[void][Console]::ReadLine();$stream.Dispose()";
  const child = spawn('powershell.exe', ['-NoProfile', '-NonInteractive', '-EncodedCommand', Buffer.from(script, 'utf16le').toString('base64')], {
    windowsHide: true, env: { ...process.env, AXIOM_LOCK_FILE: source }, stdio: ['pipe', 'pipe', 'pipe'],
  });
  try {
    await new Promise((resolve, reject) => {
      const timer = setTimeout(() => { child.kill(); reject(new Error('Не получено подтверждение блокировки')); }, 10000);
      child.stdout.on('data', data => { if (String(data).includes('ready')) { clearTimeout(timer); resolve(); } });
      child.once('exit', () => { clearTimeout(timer); reject(new Error('Блокировка завершилась раньше времени')); });
    });
    await rootMenu(S);
    await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).click();
    await S.waitStatus('не выполнена');
    C.ok(fs.existsSync(source), 'Заблокированный источник остался на месте');
    C.eq(fs.existsSync(target), false, 'Незавершённая вставка не создала назначение');
  } finally {
    child.stdin.end('release\n');
    await new Promise(resolve => { const timer = setTimeout(resolve, 3000); child.once('exit', () => { clearTimeout(timer); resolve(); }); });
  }
  await rootMenu(S);
  await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).click();
  await S.waitStatus('Скопировано в проект');
  C.eq(fs.readFileSync(target, 'utf8'), 'PRINT 7\n', 'Повтор после снятия блокировки использовал сохранённый буфер');
} });
module.exports.push({ id: 'files-project-cut-moves-open-dirty-document', name: 'Вырезание переносит открытый изменённый документ без потери модели и точки остановки', async fn({ S, C }) {
  const fs = require('fs'); const path = require('node:path');
  const root = testWorkspace('project-cut-open');
  const folder = path.join(root, 'исходная папка'); fs.mkdirSync(folder);
  const source = writeAxm(folder, 'перемещаемый.axm', 'PRINT 1\n');
  const target = path.join(root, 'перемещаемый.axm');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  await S.page.evaluate(p => window.__ide.openFile(p), source);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false;
    i.editor.getModel().setValue('PRINT 2\n'); });
  const documentId = await S.page.evaluate(p => window.__ide.openFiles.get(p).documentId, source);
  await S.page.evaluate(() => { window.__ide.editor.setPosition({ lineNumber: 1, column: 1 }); window.__ide.editor.focus(); });
  await S.page.keyboard.press('F9');
  await projectMenu(S, 'исходная папка/перемещаемый.axm');
  await S.page.getByRole('menuitem', { name: 'Вырезать', exact: true }).click();
  await S.waitStatus('Вырезано');
  await rootMenu(S);
  await S.page.getByRole('menuitem', { name: 'Вставить', exact: true }).click();
  await S.waitStatus('Перемещено');
  C.eq(fs.existsSync(source), false, 'Исходный путь освобождён');
  C.eq(fs.readFileSync(target, 'utf8'), 'PRINT 1\n', 'Диск сохранил прежнее содержимое');
  C.eq(await S.page.evaluate(() => window.__ide.activeFile), target, 'Активная вкладка получила новый путь');
  C.eq(await S.page.evaluate(p => window.__ide.openFiles.get(p)?.documentId, target), documentId, 'Идентификатор открытого документа сохранён');
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 2\n', 'Несохранённая правка осталась в модели');
  C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), target), 'Документ остаётся изменённым после переноса');
  C.eq(await S.page.evaluate(() => window.__ide.trace.lines(window.__ide.openFiles.get(window.__ide.activeFile).model).join(',')), '1', 'Точка остановки перенесена вместе с моделью');
  C.ok(await S.page.evaluate(p => [...document.querySelectorAll('#project-files .project-file')].some(item => item.dataset.path === p), target), 'Дерево обновило путь');
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'Перенесённый документ сохраняется');
  C.eq(fs.readFileSync(target, 'utf8'), 'PRINT 2\n', 'Сохранение записало правку по новому пути');
} });
module.exports.push({ id: 'files-project-trash-confirm', name: 'Удаление в корзину отменяется без потерь и выполняется после подтверждения', async fn({ S, C }) {
  const fs = require('fs'); const path = require('node:path');
  const root = testWorkspace('project-trash');
  const file = writeAxm(root, 'в корзину.axm', 'PRINT 5\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  S.confirmAction = 'dismiss';
  let dialog = S.page.waitForEvent('dialog');
  await projectMenu(S, 'в корзину.axm');
  await S.page.getByRole('menuitem', { name: 'Удалить в корзину', exact: true }).click();
  await dialog;
  await S.waitStatus('Удаление отменено');
  C.ok(fs.existsSync(file), 'Отмена оставила файл на месте');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 5\n', 'Отмена не изменила содержимое');
  S.confirmAction = 'accept';
  dialog = S.page.waitForEvent('dialog');
  await projectMenu(S, 'в корзину.axm');
  await S.page.getByRole('menuitem', { name: 'Удалить в корзину', exact: true }).click();
  await dialog;
  await S.page.waitForFunction(p => !window.__ide.openFiles.has(p) && ![...document.querySelectorAll('#project-files .project-file')].some(item => item.dataset.path === p), file);
  await S.waitStatus('Перемещено в корзину');
  C.eq(fs.existsSync(file), false, 'Подтверждённое удаление убрало файл через корзину Windows');
} });
module.exports.push({ id: 'files-project-properties', name: 'Свойства файла показываются в окне, свойства папки доступны через тот же IPC', async fn({ S, C }) {
  const path = require('node:path');
  const root = testWorkspace('project-properties');
  const file = writeAxm(root, 'свойства.axm', 'PRINT 42\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openProject(p), root);
  const dialog = S.page.waitForEvent('dialog');
  await projectMenu(S, 'свойства.axm');
  await S.page.getByRole('menuitem', { name: 'Свойства', exact: true }).click();
  await dialog;
  const message = S.rendererDialogs.at(-1)?.message || '';
  C.contains(message, 'Имя: свойства.axm', 'Свойства называют файл');
  C.contains(message, `Путь: ${require("node:fs").realpathSync(file)}`, 'Свойства показывают полный путь');
  C.contains(message, 'Размер: 9 байт', 'Свойства показывают размер файла');
  const directory = await S.page.evaluate(p => window.axiom.projectFileProperties(p, p), root);
  C.ok(directory.ok && directory.kind === 'directory' && directory.path.toLowerCase() === require('node:fs').realpathSync(root).toLowerCase(), 'Свойства папки возвращают её тип и путь');
  C.eq(directory.size, null, 'Размер папки не подменяется суммой файлов');
} });
module.exports.push({ id: 'files-save-open-writer', name: 'Открытый дескриптор другого процесса препятствует сохранению без потери текста', async fn({ S, C }) {
  const fs = require('fs'); const file = writeAxm(testWorkspace('save-open-writer'), 'открытый.axm', 'PRINT 1\n');
  await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p), file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false;
    i.editor.getModel().setValue('PRINT 2\n'); window.__saveErrors = []; window.alert = message => window.__saveErrors.push(message); });
  const fd = fs.openSync(file, 'r+');
  try {
    C.eq(await S.page.evaluate(() => window.__ide.save()), false, 'Чужой дескриптор отменяет запись');
    C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 1\n', 'Исходный файл не изменён');
    C.ok(await S.page.evaluate(p => window.__ide.openFiles.isDirty(p), file), 'Правка остаётся несохранённой');
    C.eq(await S.page.evaluate(() => window.__saveErrors.length), 1, 'Ошибка сохранения показана пользователю');
  } finally { fs.closeSync(fd); }
  C.ok(await S.page.evaluate(() => window.__ide.save()), 'После освобождения дескриптора запись проходит');
  C.eq(fs.readFileSync(file, 'utf8'), 'PRINT 2\n', 'Записана правка редактора');
} });

module.exports.push({
  id:'project-open-maze-tabs-entry',name:'Открыть проект Maze Chase из меню: дерево, точка входа и запуск main из модуля',
  async fn({S,C}) {
    const path=require('node:path');const {ROOT}=require('../helpers');
    const folder=mazeWorkspace('maze-project-entry',true),main=path.join(folder,'main.axm');
    await S.launch({settings:{smartIdentifierEnabled:false}});await S.stubOpenDialog([folder]);
    await S.page.evaluate(()=>window.__ide.handleAction('open-project'));
    await S.page.waitForFunction(folder=>window.__ide.projectDir===folder&&!window.__ide.projectSession.loading,folder);
    C.eq(await S.page.locator('.editor-tab').count(),1,'Открыта точка входа без остальных восьми вкладок');
    C.eq(await S.page.evaluate(()=>window.__ide.projectSession.entry),main,'main выбран описанием проекта');
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Точка входа активна после открытия');
    C.eq(await S.page.evaluate(main=>window.__ide.openFiles.navigationEntryId===window.__ide.openFiles.get(main).documentId,main),true,'Помощник получил ту же точку входа');
    await S.page.evaluate(p=>window.__ide.openFile(p),path.join(folder,'maze_player.axm'));
    C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Вкладка героя не становится отдельной программой');
    await S.page.evaluate(()=>window.__ide.run('debug'));
    await S.page.waitForFunction(()=>!!window.__ide.execution.stopId);
    C.eq(await S.page.evaluate(()=>window.__ide.execution.sourceSnapshot.file),main,'Отладка запустила точку входа проекта');
    await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
    for(const theme of ['dark','light','basic','borland']){
      await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
      C.eq(await S.page.locator('#project-entry').isVisible(),true,'Панель показывает точку входа проекта: '+theme);
    }
    C.eq(S.pageErrors.length,0,'Открытие проекта без ошибок страницы');
  }
},{
  id:'project-open-manual-entry-restore',name:'Выбор файла запуска, восстановление проекта и заголовок при смене языка',
  async fn({S,C}) {
    const root=testWorkspace('project-startup-choice');
    const a=writeAxm(root,'a.axm','PRINT "A"\n'),b=writeAxm(root,'b.axm','PRINT "B"\n');
    await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),root);
    C.eq(await S.page.locator('#project-entry').inputValue(),'','Нет произвольного первого запускного файла');
    await S.page.evaluate(()=>window.__ide.run());
    C.eq(await S.page.evaluate(()=>window.__ide.running),false,'Без выбора программа не запускается');
    await S.page.locator('#project-entry').selectOption(b);
    await S.page.evaluate(p=>window.__ide.openFile(p),a);await S.page.evaluate(()=>window.__ide.run());
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide.execution.outputText.includes('B'));
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'B','Запущен выбранный b при активном a');
    await S.page.evaluate(p=>window.__ide.openProject(p),root);
    C.eq(await S.page.locator('#project-entry').inputValue(),b,'Повторное открытие сохраняет выбор');
    C.eq(await S.page.locator('.editor-tab').count(),2,'Повтор не создаёт дубликаты вкладок');
    const profile=S.userData;await S.close();await S.launch({userData:profile});
    C.eq(await S.page.locator('#project-entry').inputValue(),b,'Выбор восстановлен после перезапуска IDE');
    C.eq(await S.page.locator('.editor-tab').count(),2,'Вкладки восстановлены без повторного открытия');
    C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'После перезапуска заголовок показывает папку проекта');
    for(const language of ['en','ru']){
      await S.page.evaluate(lang=>window.__ide.setLanguage(lang),language);
      C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'Перевод интерфейса сохраняет имя проекта: '+language);
    }
    await S.page.evaluate(()=>window.__ide.closeProject());
    C.eq(await S.page.locator('#project-entry').isDisabled(),true,'Без проекта цель показывает текущий файл без выбора');
    C.eq(await S.page.locator('#project-title').textContent(),'Открыть проект','После закрытия показана команда открытия');
    await S.page.evaluate(()=>window.__ide.setLanguage('en'));
    C.eq(await S.page.locator('#project-title').textContent(),'Open Project','После закрытия команда открытия переводится');
    await S.close();await S.launch({userData:profile});
    C.eq(await S.page.evaluate(()=>window.__ide.projectDir),null,'Явно закрытый проект не восстанавливается');
    C.eq(await S.page.locator('#project-title').textContent(),'Open Project','После перезапуска без проекта показана переведённая команда');
    C.eq(S.pageErrors.length,0,'Восстановление без ошибок страницы');
  }
});

module.exports.push({id:'project-files-toolbar-toggle',name:'Кнопка возвращает скрытые файлы проекта без повторного открытия папки',async fn({S,C}){
  const root=testWorkspace('project-files-toggle');
  const main=writeAxm(root,'main.axm','PRINT "main"\n');
  await S.launch({settings:{smartIdentifierEnabled:false}});
  let button=S.page.locator('#tb-toggle-project');
  C.eq(await button.count(),1,'Кнопка файлов проекта присутствует на панели');
  if(!await button.count())return;
  await S.page.evaluate(p=>window.__ide.openProject(p),root);
  await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('PRINT "changed"\n'));
  C.eq(await button.getAttribute('aria-pressed'),'true','Показанный список отмечен на кнопке');
  C.eq(await button.getAttribute('aria-controls'),'project-explorer','Кнопка связана с разделом проекта');
  C.contains(await button.getAttribute('aria-label'),'Ctrl+Shift+E','Доступная подпись содержит сочетание клавиш');
  await S.page.locator('#project-files-toggle').click();
  await S.page.locator('#sidebar-close').click();
  C.eq(await S.page.locator('#project-explorer').isVisible(),false,'Крестик скрывает раздел');
  C.eq(await button.getAttribute('aria-pressed'),'false','Кнопка отражает скрытый раздел');
  await S.page.locator('#toolbar-overflow > summary').click();await button.click();
  C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Кнопка возвращает раздел');
  C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'Имя папки сохранено');
  C.eq(await S.page.locator('#project-files').isVisible(),false,'Свёрнутое состояние списка сохранено');
  C.eq(await S.page.evaluate(()=>window.__ide.projectDir),root,'Папка проекта сохранена');
  C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Файл запуска сохранён');
  C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Активная вкладка сохранена');
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT "changed"\n','Несохранённый текст сохранён');
  C.eq(await S.page.evaluate(p=>window.__ide.openFiles.isDirty(p),main),true,'Признак правок сохранён');
  await S.page.locator('#toolbar-overflow > summary').click();await button.focus();await S.page.keyboard.press('Space');
  C.eq(await S.page.locator('#project-explorer').isVisible(),false,'Пробел нажимает кнопку');
  await S.page.keyboard.press('Control+Shift+e');
  C.eq(await button.getAttribute('aria-pressed'),'true','Сочетание клавиш обновляет кнопку');
  await S.page.locator('#sidebar-close').click();await S.page.locator('[data-menu="view"] .menu-label').click();
  await S.page.locator('[data-action="show-project"]').click();
  C.eq(await button.getAttribute('aria-pressed'),'true','Меню возвращает проект и обновляет кнопку');
  await S.page.evaluate(()=>window.__ide.setLanguage('en'));
  C.eq(await button.getAttribute('aria-label'),'Project files (Ctrl+Shift+E)','Подпись переводится');
  await S.page.evaluate(()=>window.__ide.save());await S.page.locator('#sidebar-close').click();
  const profile=S.userData;await S.close();await S.launch({userData:profile});
  button=S.page.locator('#tb-toggle-project');
  C.eq(await S.page.locator('#project-explorer').isVisible(),false,'Скрытый раздел остаётся скрытым после перезапуска');
  C.eq(await button.getAttribute('aria-pressed'),'false','Кнопка отражает восстановленную видимость');
  await S.page.locator('#toolbar-overflow > summary').click();await button.click();
  C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Кнопка возвращает восстановленный проект');
  await S.page.locator('#sidebar-close').click();
  C.eq(await button.getAttribute('aria-pressed'),'false','Скрытая боковая панель отражена в состоянии кнопки');
  await S.page.locator('#toolbar-overflow > summary').click();await button.click();
  C.eq(await S.page.locator('#project-explorer').isVisible(),true,'Кнопка возвращает также скрытую боковую панель');
  C.eq(await button.getAttribute('aria-pressed'),'true','После возврата кнопка нажата');
  C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
}});

module.exports.push({id:'project-sidebar-controls',name:'Единая геометрия списков и сворачивание файлов проекта',async fn({S,C}){
  const root=testWorkspace('sidebar-controls');
  const main=writeAxm(root,'main.axm','PRINT 1\n');
  const long=writeAxm(root,'очень_длинное_название_вспомогательного_модуля_для_проверки_границ.axm','PRINT 2\n');
  await S.launch();await S.page.evaluate(p=>window.__ide.openProject(p),root);
  for(const language of ['en','ru']){
    await S.page.evaluate(l=>{window.__ide.setLanguage(l);window.I18N.apply();},language);
    C.eq(await S.page.locator('#project-title').textContent(),require('node:path').basename(root),'Имя папки сохраняется при переводе: '+language);
  }
  await S.page.locator('#project-entry').selectOption(long);
  for(const theme of ['dark','light','basic','borland']){
    await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
    const geometry=await S.page.evaluate(()=>{
      const select=document.querySelector('#project-entry'),filter=document.querySelector('.outline-options select');
      const css=getComputedStyle(select),other=getComputedStyle(filter),arrow=getComputedStyle(select,'::picker-icon');
      return {left:css.paddingLeft,right:css.paddingRight,filterLeft:other.paddingLeft,filterRight:other.paddingRight,arrow:arrow.marginRight,overflow:select.scrollWidth>select.clientWidth+1};
    });
    C.eq(geometry.left,geometry.filterLeft,'Отступ текста общий: '+theme);
    C.eq(geometry.right,geometry.filterRight,'Отступ стрелки общий: '+theme);
    C.ok(parseFloat(geometry.left)>=7&&parseFloat(geometry.right)>=8,'Текст и стрелка не у края: '+theme);
    C.eq(geometry.overflow,false,'Длинное имя не расширяет поле: '+theme);
    await S.page.screenshot({path:require('node:path').join(require('../helpers').ROOT,'.artifacts/project-open-20261020/sidebar-'+theme+'.png')});
  }
  await S.page.locator('#project-entry').selectOption(main);
  const toggle=S.page.locator('#project-files-toggle');
  C.eq(await toggle.getAttribute('aria-expanded'),'true','Проект раскрыт');
  const tabs=await S.page.locator('.editor-tab').count();
  await toggle.click();
  C.eq(await S.page.locator('#project-files').isVisible(),false,'Треугольник скрывает файлы');
  C.eq(await S.page.locator('#project-entry').isVisible(),true,'Файл запуска остаётся на общей панели');
  await S.page.evaluate(()=>window.__ide.refreshProjectFiles());
  C.eq(await toggle.getAttribute('aria-expanded'),'false','Обновление перечня не раскрывает проект');
  C.eq(await S.page.locator('#project-entry-controls').isVisible(),true,'Обновление сохраняет общую панель запуска');
  C.eq(await S.page.locator('#project-resizer').isVisible(),false,'У свёрнутого проекта нет лишней высоты и разделителя');
  await toggle.focus();await S.page.keyboard.press('Enter');
  C.eq(await S.page.locator('#project-files').isVisible(),true,'Клавиатура раскрывает файлы');
  C.eq(await S.page.locator('#project-entry').isVisible(),true,'Выбор запуска снова доступен');
  C.eq(await S.page.locator('.editor-tab').count(),tabs,'Вкладки сохранены');
  C.eq(await S.page.evaluate(()=>window.__ide.runTarget()),main,'Точка входа сохранена');
  await S.page.evaluate(()=>window.__ide.closeProject());
  C.eq(await toggle.isVisible(),false,'Без проекта треугольника нет');
  await S.page.evaluate(()=>window.__ide.setLanguage('en'));
  C.eq(await S.page.locator('#project-title').textContent(),'Open Project','После закрытия переводится команда открытия');
  C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
}});

module.exports.push({id:'project-open-does-not-edit-source',name:'Открытие Maze Chase с умными именами не меняет исходники и не помечает вкладки',async fn({S,C}){
 const path=require('node:path'),fs=require('node:fs'),{ROOT}=require('../helpers');
 const folder=mazeWorkspace('maze-source-preservation');
 const disk=new Map(fs.readdirSync(folder).filter(f=>f.endsWith('.axm')).map(f=>[path.join(folder,f),fs.readFileSync(path.join(folder,f),'utf8')]));
 await S.launch({settings:{smartIdentifierEnabled:true}});await S.page.evaluate(p=>window.__ide.openProject(p),folder);
 for(const file of disk.keys())await S.page.evaluate(p=>window.__ide.openFile(p),file);
 const state=await S.page.evaluate(()=>[...window.__ide.openFiles.values()].map(d=>({path:d.path,text:d.model.getValue(),saved:d.savedText,dirty:window.__ide.openFiles.isDirty(d.path)})));
 const differences=state.filter(d=>d.text!==d.saved).map(d=>({file:path.basename(d.path),changes:d.text.split('\n').flatMap((line,i)=>line!==d.saved.split('\n')[i]?[{line:i+1,before:d.saved.split('\n')[i],after:line}]:[])}));
 fs.writeFileSync(path.join(ROOT,'.artifacts/project-open-20261020/open-source-differences.json'),JSON.stringify(differences,null,2));
 C.eq(state.filter(d=>d.dirty).map(d=>path.basename(d.path)).join(','),'','Все девять вкладок чистые после чтения');
 C.eq(differences.length,0,'Текст моделей совпадает с прочитанным текстом');
 C.eq(state.length,9,'Открыты девять исходников');
 for(const d of state)C.eq(fs.readFileSync(d.path,'utf8'),disk.get(d.path),'Исходник на диске не перезаписан: '+path.basename(d.path));
}});
