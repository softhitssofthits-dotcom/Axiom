// i18n: ru/en dictionaries stay in parity and every data-i18n key is translated.
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [
  {
    id: 'i18n-editor-context-menu',
    name: 'Контекстное меню редактора полностью переводится и обновляет доступные имена',
    async fn({ S, C }) {
      await S.launch();
      await S.page.evaluate(() => window.__ide.editor.getModel().setValue('DIM price AS DECIMAL = 1\nPRINT price'));
      const expected = {
        ru: ['Изменить все вхождения', 'Вырезать', 'Копировать', 'Вставить', 'Установить / снять точку остановки', 'Очистить все точки остановки', 'Кто вызывает', 'Что вызывает', 'Быстрый просмотр', 'Объединить в раздел…', 'Палитра команд'],
        en: ['Change All Occurrences', 'Cut', 'Copy', 'Paste', 'Toggle Breakpoint', 'Clear All Breakpoints', 'Callers', 'Callees', 'Quick preview', 'Group into region…', 'Command Palette'],
      };
      await S.page.locator('.monaco-editor .view-line').first().click({button: 'right'});
      const menu = S.page.locator('#ax-context-menu');
      await menu.waitFor({state: 'visible'});
      for (const lang of ['ru', 'en', 'ru']) {
        await S.page.evaluate(lang => window.__ide.setLanguage(lang), lang);
        const rows = await menu.locator('[role="menuitem"]').evaluateAll(rows => rows.map(row => ({
          label: row.querySelector('.xs-context-item-label').textContent, aria: row.getAttribute('aria-label'),
          icon: !!row.querySelector('.xs-context-item-icon.codicon'), action: row.dataset.action,
        })));
        C.eq(rows.map(row => row.label).join('|'), expected[lang].join('|'), 'Все подписи меню: ' + lang);
        C.ok(rows.every(row => row.aria === row.label), 'Доступные имена совпадают с подписями: ' + lang);
        C.ok(rows.every(row => row.icon), 'Пиктограммы сохранены: ' + lang);
        C.eq(rows.map(row => row.action).join(','), 'change-all,cut,copy,paste,trace-breakpoint,trace-clear,assistant-incoming,assistant-outgoing,assistant-preview,assistant-createRegion,command-palette', 'Состав команд сохранён: ' + lang);
      }
      C.contains(await menu.innerText(), 'Ctrl+F2', 'Сочетание изменения всех вхождений сохранено');
      C.contains(await menu.innerText(), 'F1', 'Сочетание палитры команд сохранено');
      await S.page.keyboard.press('Escape');
      C.ok(await menu.isHidden(), 'Escape закрывает меню');
      await S.page.locator('.monaco-editor .view-line').first().click({button: 'right'});
      C.eq(await menu.locator('[data-action="copy"] .xs-context-item-label').innerText(), 'Копировать', 'Повторное открытие использует текущий язык');
    },
  },
  {
    id: 'i18n-parity',
    name: 'ru and en dictionaries have identical keys covering index.html',
    async fn({ S, C }) {
      await S.launch();
      const check = await S.page.evaluate(() => {
        const dicts = I18N.dicts;
        const ru = new Set(Object.keys(dicts.ru));
        const en = new Set(Object.keys(dicts.en));
        const onlyRu = [...ru].filter((k) => !en.has(k));
        const onlyEn = [...en].filter((k) => !ru.has(k));
        const used = new Set();
        document.querySelectorAll('[data-i18n]').forEach((el) => used.add(el.dataset.i18n));
        document.querySelectorAll('[data-i18n-title]').forEach((el) => used.add(el.dataset.i18nTitle));
        const missing = [...used].filter((k) => !ru.has(k) || !en.has(k));
        return { onlyRu, onlyEn, missing };
      });
      C.eq(check.onlyRu.length, 0, 'no ru-only keys', check.onlyRu.join(', '));
      C.eq(check.onlyEn.length, 0, 'no en-only keys', check.onlyEn.join(', '));
      C.eq(check.missing.length, 0, 'every data-i18n key exists in both dictionaries', check.missing.join(', '));
    },
  },
  {
    id: 'i18n-switch-to-en-and-back',
    name: 'Switching the locale re-inks the menu labels',
    async fn({ S, C }) {
      await S.launch();
      C.contains(await S.status(), 'Готово', 'ru status by default');
      await S.clickLangAction('lang-en');
      const fileMenu = await S.page.evaluate(() => document.querySelector('.menu-item[data-menu="file"] .menu-label').textContent);
      C.eq(fileMenu, 'File', 'File menu re-inked in English');
      await S.clickLangAction('lang-ru');
      const fileMenuRu = await S.page.evaluate(() => document.querySelector('.menu-item[data-menu="file"] .menu-label').textContent);
      C.eq(fileMenuRu, 'Файл', 'File menu back to Russian');
    },
  },
  {
    id: 'i18n-status-time-follows-language-in-every-theme',
    name: 'Время проверки в строке состояния переводится при смене языка во всех темах',
    async fn({ S, C }) {
      const ws = testWorkspace('i18n-status-time');
      const file = writeAxm(ws, 'timed.axm', 'PRINT 1\n');
      await S.launch({ settings: { checkOnChange: false, formatOnBlur: false } });
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p,
        file, { timeout: 8000, polling: 100 }
      );
      const themes = await S.page.evaluate(() => window.AxiomThemes.ids());
      const readTime = () => S.page.evaluate(() => document.getElementById('status-execution-time').textContent);
      for (const theme of themes) {
        await S.page.evaluate(t => window.__ide.setTheme(t), theme);
        await S.page.evaluate(() => window.__ide.setLanguage('ru'));
        await S.page.evaluate(() => window.__ide.check(true));
        const ru = await readTime();
        C.ok(/^Проверка: \d+ мс$/.test(ru), theme + ': время проверки по-русски', ru);
        await S.page.evaluate(() => window.__ide.setLanguage('en'));
        const en = await readTime();
        C.ok(/^Check: \d+ ms$/.test(en), theme + ': время проверки по-английски после смены языка', en);
        C.eq(en.match(/\d+/)?.[0], ru.match(/\d+/)?.[0], theme + ': измеренное время не пересчитывается при смене языка');
        await S.page.evaluate(() => window.__ide.setLanguage('ru'));
        const back = await readTime();
        C.eq(back, ru, theme + ': надпись возвращается к русскому варианту');
      }
    },
  },
];
