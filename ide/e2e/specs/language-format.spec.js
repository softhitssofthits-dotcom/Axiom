const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, source, tag) {
  const file = writeAxm(testWorkspace(tag), 'Программа.axm', source);
  await S.launch(); await S.stubOpenDialog([file]); await S.page.evaluate(() => window.__ide.handleAction('open'));
  await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
  await S.page.evaluate(() => { const i = window.__ide; i.settings.formatOnBlur = false; i.settings.checkOnChange = false; });
  return file;
}
module.exports = [
  { id: 'language-runtime-diagnostic-switch', name: 'Ошибка выполнения переключает язык без повторного запуска', async fn({ S, C }) {
    await open(S, 'DIM divisor AS INTEGER = 0\nPRINT 1 / divisor\n', 'language-runtime');
    await S.page.evaluate(() => { window.__ide.setLanguage('en'); return window.__ide.run(); });
    await S.waitIdle();
    C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes('MB3001: Division by zero.')), 'Ошибка VM по-английски');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    C.ok(await S.page.evaluate(() => /MB3001:.*[А-Яа-яЁё]/.test(document.getElementById('diagnostics-content').textContent)), 'Исходный текст VM восстановлен по-русски');
  } },
  { id: 'language-diagnostics-switch', name: 'Язык диагностик списка и Monaco переключается без повторного анализа', async fn({ S, C }) {
    await open(S, 'PRINT missing\n', 'language-diagnostics');
    for (const source of ['PRINT missing\n', 'PRINT @\n', 'PRINT (\n']) {
      await S.page.evaluate(async source => { const i = window.__ide; i.editor.getModel().setValue(source); await i.check(false); }, source);
      await S.page.evaluate(() => window.__ide.setLanguage('en'));
      const en = await S.page.evaluate(() => { const i = window.__ide; return { list: document.getElementById('diagnostics-content').textContent, markers: i.monaco.editor.getModelMarkers({owner:'axiom',resource:i.editor.getModel().uri}).map(m=>m.message) }; });
      C.ok(/MB\d{4}/.test(en.list) && !/[А-Яа-яЁё]/.test(en.markers.join(' ')), 'Диагностика по-английски для ' + source.trim());
      await S.page.evaluate(() => window.__ide.setLanguage('ru'));
      C.ok(await S.page.evaluate(() => window.__ide.monaco.editor.getModelMarkers({owner:'axiom',resource:window.__ide.editor.getModel().uri}).some(m=>/[А-Яа-яЁё]/.test(m.message))), 'Русский текст восстановлен');
    }
  } },
  { id: 'format-one-enter-two-styles', name: 'Один Enter форматирует строку и создаёт цикл в Classic и Modern', async fn({ S, C }) {
    await open(S, 'PRINT 1\n', 'format-enter');
    await S.page.locator('[data-menu="edit"] .menu-label').click();
    await S.page.locator('#formatting-submenu .submenu-trigger').hover();
    await S.page.locator('[data-action="format-modern"]').click();
    await S.page.waitForFunction(() => window.__ide.settings.formattingStyle === 'Modern' && !window.__ide._formatBusy);
    C.ok(true, 'Стиль выбирается настоящим пунктом меню');
    for (const [style, expected] of [['Classic','FOR index = 1 TO 3'], ['Modern','For index = 1 To 3']]) {
      await S.page.evaluate(async style => { const i = window.__ide; await i.setFormattingStyle(style); i.editor.getModel().setValue('for index = 1 to 3'); i.editor.setPosition({lineNumber:1,column:19}); i.editor.focus(); }, style);
      await S.page.keyboard.press('Enter');
      await S.page.waitForFunction(expected => window.__ide.editor.getModel().getLineContent(1) === expected, expected);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), 2, 'Каретка в теле цикла ' + style);
      C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().column), 5, 'Отступ тела цикла ' + style);
      C.ok(await S.page.evaluate(() => /next/i.test(window.__ide.editor.getValue())), 'Закрывающая строка вставлена ' + style);
      C.ok(await S.page.evaluate(style => document.querySelector('[data-check="format' + style + '"]').classList.contains('on'), style), 'В меню отмечен выбранный стиль');
    }
  } },
];
