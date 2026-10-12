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
