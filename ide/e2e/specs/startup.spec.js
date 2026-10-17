// Startup: the app boots to a ready state with an editor, a tab and Monaco loaded.
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [
  {
    id: 'startup-ready',
    name: 'IDE boots to #app.ready with an untitled .axm tab',
    async fn({ S, C }) {
      await S.launch();
      C.ok(S.page, 'window opened');
      const tab = await S.activeTab();
      C.ok(tab && /\.axm$/.test(tab.name), 'untitled .axm tab is active', JSON.stringify(tab));
      const value = await S.editorValue();
      C.eq(value, '', 'Новый документ пуст');
      const lang = await S.page.evaluate(() => window.__ide && window.__ide.editor.getModel().getLanguageId());
      C.eq(lang, 'axiom', 'model language is axiom');
      const meta = await S.page.evaluate(() => window.__ide && window.__ide.langMeta);
      C.ok(meta && Array.isArray(meta.keywords) && meta.keywords.includes('PRINT'), 'language metadata loaded from axiom CLI');
      C.ok(meta && Array.isArray(meta.builtins) && meta.builtins.some((b) => b.name === 'LEN'), 'builtin registry present in metadata');
      const status = await S.status();
      C.contains(status, 'Готово', 'status bar shows ready state');
      await S.screenshot('startup-ready');
    },
  },
  {
    id: 'startup-opens-existing-file',
    name: 'Open dialog stub loads an existing .axm file into a tab',
    async fn({ S, C }) {
      const ws = testWorkspace('startup-open');
      const file = writeAxm(ws, 'hello.axm', 'PRINT "Привет, Axiom!"\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p,
        file, { timeout: 8000, polling: 100 }
      );
      const tab = await S.activeTab();
      C.eq(tab && tab.name, 'hello.axm', 'tab shows the file name');
      const value = await S.editorValue();
      C.contains(value, 'Привет, Axiom!', 'file content loaded');
    },
  },
  {
    id: 'startup-russian-path-deduplicates',
    name: 'Русский путь открывается один раз, даже при повторном выборе файла',
    async fn({ S, C }) {
      const file = writeAxm(testWorkspace('russian-path'), 'проверка.axm', 'PRINT "Привет, Axiom!"\n');
      await S.launch();
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.openFile(p); }, file);
      C.eq(await S.page.evaluate(p => window.__ide.activeFile, file), file, 'Открыт документ с русским путём');
      C.eq(await S.page.evaluate(p => [...document.querySelectorAll('.editor-tab')].filter(t => t.dataset.path === p).length, file), 1, 'Для пути создана одна вкладка');
      C.eq(await S.editorValue(), 'PRINT "Привет, Axiom!"\n', 'Содержимое русского файла загружено');
      C.eq(await S.page.evaluate(() => window.__ide.openFiles.size), 2, 'Повторное открытие не создало копию рядом с новой вкладкой');
    },
  },
];

module.exports.push({ id: 'startup-legacy-settings-import', name: 'Прежние настройки импортируются один раз без изменения исходника', async fn({ S, C }) {
  const fs = require('node:fs'), path = require('node:path');
  const profile = testWorkspace('legacy-settings-profile');
  const file = writeAxm(testWorkspace('legacy-settings-files'), 'Программа.axm', 'PRINT 42\n');
  const directory = path.join(profile, 'local', 'Axiom', 'Axiom.Ide'); fs.mkdirSync(directory, { recursive: true });
  const legacy = path.join(directory, 'recent-files.json');
  const original = JSON.stringify({ RecentFiles: [file], LastDirectoryPath: path.dirname(file),
    Theme: 'Light', Language: 'English', IsFilesPaneVisible: false, IsStatusBarVisible: false,
    SmartIdentifierEnabled: true, WindowState: 'Maximized' });
  fs.writeFileSync(legacy, original);
  await S.launch({ userData: profile, settingsAbsent: true });
  C.eq(await S.page.evaluate(() => window.__ide.settings.legacyImportVersion), 1, 'Импорт отмечен в новом профиле');
  C.eq(await S.page.evaluate(() => document.documentElement.dataset.theme), 'light', 'Перенесена светлая тема');
  C.eq(await S.page.evaluate(() => window.__ide.settings.language), 'en', 'Перенесён язык');
  C.ok(await S.page.locator('#project-explorer').evaluate(el => el.classList.contains('hidden')), 'Файловая панель скрыта по прежней настройке');
  C.ok(await S.page.locator('#status-bar').evaluate(el => getComputedStyle(el).display === 'none'), 'Строка состояния скрыта по прежней настройке');
  C.ok(await S.electronApp.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows()[0].isMaximized()), 'Максимизация перенесена до открытия окна');
  C.eq(fs.readFileSync(legacy, 'utf8'), original, 'Прежний файл не изменён');
  await S.page.evaluate(() => window.__ide.setTheme('borland'));
  await S.close();
  fs.writeFileSync(legacy, JSON.stringify({ Theme: 'Dark', Language: 'Russian', RecentFiles: [] }));
  await S.launch({ userData: profile });
  C.eq(await S.page.evaluate(() => document.documentElement.dataset.theme), 'borland', 'Повтор не заменяет выбранную тему');
  C.eq(await S.page.evaluate(() => window.__ide.settings.language), 'en', 'Повтор не заменяет язык');
  C.ok(await S.page.evaluate(file => window.__ide.settings.recentFiles.includes(file), file), 'История не заменена повторным импортом');
} });

module.exports.push({ id: 'startup-corrupt-settings-preserved', name: 'Повреждённые настройки сохраняются отдельно при рабочем запуске IDE', async fn({ S, C }) {
  const fs = require('node:fs'), path = require('node:path');
  const profile = testWorkspace('corrupt-settings-profile');
  const original = Buffer.from('\uFEFF{"theme": "dark", broken', 'utf8');
  fs.writeFileSync(path.join(profile, 'settings.json'), original);
  await S.launch({ userData: profile });
  C.eq(await S.page.evaluate(() => window.__ide.settings.theme), 'dark', 'Запуск использует безопасные значения');
  const backups = fs.readdirSync(profile).filter(name => name.startsWith('settings.invalid-'));
  C.eq(backups.length, 1, 'Сохранена отдельная копия повреждённого профиля');
  if (backups.length) C.eq(fs.readFileSync(path.join(profile, backups[0])).toString('base64'), original.toString('base64'), 'Повреждённые байты сохранены точно');
  await S.page.evaluate(() => window.__ide.setTheme('basic')); await S.close();
  C.eq(JSON.parse(fs.readFileSync(path.join(profile, 'settings.json'))).theme, 'basic', 'Новые настройки записываются');
  await S.launch({ userData: profile });
  C.eq(await S.page.evaluate(() => document.documentElement.dataset.theme), 'basic', 'Новый профиль восстанавливается');
  C.eq(fs.readdirSync(profile).filter(name => name.startsWith('settings.invalid-')).length, 1, 'При повторе не создаётся лишняя копия');
} });

module.exports.push({ id: 'startup-recent-missing-file-and-directory', name: 'Недавние файлы и последний каталог сохраняются после отказа открытия отсутствующего файла', async fn({ S, C }) {
  const path = require('node:path');
  const directory = testWorkspace('recent-files-acceptance'), first = writeAxm(directory, 'Первый.axm', 'PRINT 1\n');
  const second = writeAxm(directory, 'Второй.axm', 'PRINT 2\n'), missing = path.join(directory, 'Отсутствует.axm');
  await S.launch({ settings: { recentFiles: [missing, first, second], lastOpenDir: directory, theme: 'dark' } });
  await S.page.evaluate(file => window.__ide.openFile(file), first);
  await S.page.waitForFunction(file => window.__ide.settings.recentFiles[0] === file, first);
  const before = await S.page.evaluate(() => window.__ide.editor.getValue());
  await S.page.evaluate(() => { const i = window.__ide, original = i.openFile.bind(i); i.openFile = file => window.__recentOpen = original(file); });
  await S.page.locator('[data-menu="file"] > .menu-label').click();
  await S.page.locator('#recent-submenu > .submenu-trigger').hover();
  await S.page.locator('.recent-item').filter({ hasText: 'Отсутствует.axm' }).click();
  await S.page.evaluate(() => window.__recentOpen);
  C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), before, 'Отказ не изменил активный документ');
  C.ok(S.rendererDialogs.some(dialog => dialog.message.includes('Отсутствует.axm')), 'Ошибка называет отсутствующий файл');
  C.ok(await S.page.evaluate(files => files.every(file => window.__ide.settings.recentFiles.includes(file)), [first, second]), 'Остальные недавние файлы сохранены');
  await S.electronApp.evaluate(({ dialog }) => { dialog.showOpenDialog = async (_owner, options) => { global.__recentDefault = options.defaultPath; return { canceled: true, filePaths: [] }; }; });
  await S.page.evaluate(() => window.__ide.openDialog());
  C.eq(await S.electronApp.evaluate(() => global.__recentDefault), directory, 'Диалог использует последний каталог');
  const profile = S.userData; await S.close(); await S.launch({ userData: profile });
  C.ok(await S.page.evaluate(files => files.every(file => window.__ide.settings.recentFiles.includes(file)), [first, second]), 'История сохранена после перезапуска');
  C.eq(await S.page.evaluate(() => window.__ide.settings.lastOpenDir), directory, 'Последний каталог восстановлен');
} });
