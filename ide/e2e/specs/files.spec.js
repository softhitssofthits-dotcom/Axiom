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
  C.ok(await S.page.evaluate(p => { const section = document.getElementById('external-files'); return !section.classList.contains('hidden') && [...section.querySelectorAll('.project-file')].some(item => item.title === p); }, outside), 'Открытый внешний файл показан в отдельном разделе');
  await S.page.locator('#external-files-list .project-file').click();
  C.eq(await S.page.evaluate(() => window.__ide.activeFile), outside, 'Нажатие внешнего файла возвращает его вкладку');
  C.eq(await S.editorValue(), 'PRINT "Вне проекта"\n', 'Текст внешнего файла доступен в редакторе');
  await S.page.evaluate(() => window.__ide.closeProject());
  C.ok(await S.page.evaluate(paths => window.__ide.openFiles.has(paths.outside) && !window.__ide.openFiles.has(paths.inside), { outside, inside }), 'Закрытие проекта оставляет внешний файл и закрывает внутренний');
  C.ok(await S.page.evaluate(() => document.getElementById('external-files').classList.contains('hidden')), 'Внешняя группа скрывается после закрытия проекта');
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
  C.contains(message, `Путь: ${file}`, 'Свойства показывают полный путь');
  C.contains(message, 'Размер: 9 байт', 'Свойства показывают размер файла');
  const directory = await S.page.evaluate(p => window.axiom.projectFileProperties(p, p), root);
  C.ok(directory.ok && directory.kind === 'directory' && directory.path.toLowerCase() === root.toLowerCase(), 'Свойства папки возвращают её тип и путь');
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
