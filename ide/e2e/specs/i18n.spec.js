// i18n: ru/en dictionaries stay in parity and every data-i18n key is translated.
module.exports = [
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
];
