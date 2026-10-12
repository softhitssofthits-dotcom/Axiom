const { testWorkspace, writeAxm } = require('../helpers');
const path = require('node:path');

module.exports = [{
  id: 'console-context-menu-themes',
  name: 'Меню консоли совпадает с меню редактора во всех трёх темах',
  async fn({ S, C }) {
    const screenshotDir = testWorkspace('console-context-menu-themes');
    await S.launch();
    await S.page.evaluate(() => { window.__ide.ensureConsole(); window.__ide.editor.getModel().setValue('PRINT 1'); });
    async function skin(selector) {
      return S.page.locator(selector).evaluate(menu => {
        const style = getComputedStyle(menu), row = getComputedStyle(menu.querySelector('[role="menuitem"]'));
        return [style.backgroundColor, style.color, style.borderColor, style.borderRadius, style.padding,
          style.boxShadow, style.fontFamily, row.fontSize, row.minHeight, row.padding, row.gap];
      });
    }
    for (const theme of ['light', 'dark', 'basic']) {
      await S.page.evaluate(theme => window.__ide.setTheme(theme), theme);
      await S.page.locator('.monaco-editor .view-line').first().click({button: 'right'});
      const editorSkin = await skin('#ax-context-menu');
      await S.page.keyboard.press('Escape');
      await S.page.locator('#terminal').click({button: 'right', position: {x: 30, y: 30}});
      const menu = S.page.locator('#ax-console-context-menu');
      try { await menu.waitFor({state: 'visible', timeout: 1500}); }
      finally { if (!(await menu.count())) await S.page.keyboard.press('Escape'); }
      C.eq(JSON.stringify(await skin('#ax-console-context-menu')), JSON.stringify(editorSkin), theme + ': общие цвета и геометрия');
      C.eq(await menu.locator('[data-action="copy"]').getAttribute('aria-disabled'), 'true', theme + ': копирование без выделения недоступно');
      C.eq(await menu.locator('[data-action="copy"] .xs-context-item-label').innerText(), 'Копировать', theme + ': русская подпись');
      C.eq(await menu.locator('[data-action="clear"] .xs-context-item-label').innerText(), 'Очистить консоль', theme + ': русская очистка');
      await S.page.screenshot({path: path.join(screenshotDir, 'console-menu-' + theme + '.png')});
      await S.page.keyboard.press('Escape');
      C.ok(await menu.isHidden(), theme + ': Escape закрывает меню');
      C.ok(await S.page.evaluate(() => document.activeElement === window.__ide.term.textarea), theme + ': фокус возвращён консоли');
    }
    await S.page.evaluate(() => new Promise(resolve => {
      const term = window.__ide.term; term.write('Строка\r\n', () => { term.select(0, 0, 6); resolve(); });
    }));
    await S.page.locator('#terminal').click({button: 'right', position: {x: 30, y: 30}});
    C.eq(await S.page.locator('#ax-console-context-menu [data-action="copy"]').getAttribute('aria-disabled'), 'false', 'Копирование выделения доступно');
    await S.page.evaluate(() => window.__ide.setTheme('dark'));
    const darkSkin = await skin('#ax-console-context-menu');
    C.ok(darkSkin[0] !== 'rgb(255, 255, 255)', 'Открытое меню принимает тёмную тему');
    await S.page.evaluate(() => window.__ide.setLanguage('en'));
    C.eq(await S.page.locator('#ax-console-context-menu [data-action="clear"] .xs-context-item-label').innerText(), 'Clear console', 'Открытое меню переводится');
    C.eq(await S.page.locator('#ax-console-context-menu [data-action="copy"] .xs-context-item-label').innerText(), 'Copy', 'Копирование переводится на английский');
    C.ok(await S.page.locator('#ax-console-context-menu [role="menuitem"]').evaluateAll(rows => rows.every(row => row.getAttribute('aria-label') === row.querySelector('.xs-context-item-label').textContent)), 'Доступные имена переведены на английский');
    await S.page.evaluate(() => window.__ide.setLanguage('ru'));
    C.eq(await S.page.locator('#ax-console-context-menu [data-action="copy"] .xs-context-item-label').innerText(), 'Копировать', 'Русское копирование восстановлено');
    C.eq(await S.page.locator('#ax-console-context-menu [data-action="clear"] .xs-context-item-label').innerText(), 'Очистить консоль', 'Русская очистка восстановлена');
    C.ok(await S.page.locator('#ax-console-context-menu [role="menuitem"]').evaluateAll(rows => rows.every(row => row.getAttribute('aria-label') === row.querySelector('.xs-context-item-label').textContent)), 'Доступные имена восстановлены по-русски');
    await S.page.keyboard.press('ArrowDown');
    C.eq(await S.page.evaluate(() => document.activeElement.dataset.action), 'clear', 'Стрелка переводит фокус к очистке');
    await S.page.keyboard.press('Escape');
    C.note('Снимки трёх тем: ' + screenshotDir);
  },
}, {
  id: 'console-context-menu-input-actions',
  name: 'Копирование и очистка через меню сохраняют активный INPUT и набранный ответ',
  async fn({ S, C }) {
    const file = writeAxm(testWorkspace('console-context-input'), 'Приглашение.axm',
      'PRINT "Предыдущий вывод"\nDIM answer AS STRING\nINPUT "Ответ: ", answer\nPRINT "Принято: "; answer\n');
    await S.launch();
    // Проверяем передачу выбранного текста через настоящий IPC, сохраняя буфер пользователя.
    await S.electronApp.evaluate(({ipcMain}) => {
      ipcMain.removeHandler('write-clipboard-text');
      ipcMain.handle('write-clipboard-text', (_event, text) => { globalThis.__consoleCopiedText = text; return true; });
    });
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
    await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
    await S.waitConsole('Ответ:');
    await S.page.evaluate(() => window.__ide.term.focus());
    await S.page.keyboard.type('имя');
    await S.page.waitForFunction(() => window.__ide.execution.inputText === 'имя');
    await S.page.evaluate(() => window.__ide.term.select(0, 0, 'Предыдущий вывод'.length));
    await S.page.locator('#terminal').click({button: 'right', position: {x: 30, y: 30}});
    await S.page.locator('#ax-console-context-menu [data-action="copy"]').click();
    const copied = await S.electronApp.evaluate(async () => {
      for (let i = 0; i < 100 && globalThis.__consoleCopiedText === undefined; i++) await new Promise(resolve => setTimeout(resolve, 10));
      return globalThis.__consoleCopiedText;
    });
    C.eq(copied, 'Предыдущий вывод', 'Скопирован именно выбранный текст');
    C.ok(await S.page.evaluate(() => document.activeElement === window.__ide.term.textarea), 'После копирования фокус консоли');
    const request = await S.page.evaluate(() => window.__ide.execution.inputRequestId);
    await S.page.evaluate(() => window.__ide.term.clearSelection());
    await S.page.locator('#terminal').click({button: 'right', position: {x: 30, y: 30}});
    await S.page.locator('#ax-console-context-menu [data-action="clear"]').click();
    C.ok(await S.page.evaluate(() => window.__ide.running), 'Очистка не останавливает программу');
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), request, 'Запрос INPUT сохранён');
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputText), 'имя', 'Набранный ответ сохранён');
    C.contains(await S.consoleText(), 'Ответ: имя', 'Приглашение и ответ остаются видимыми');
    C.ok(!(await S.consoleText()).includes('Предыдущий вывод'), 'Прежняя история скрыта');
    await S.page.keyboard.press('Enter');
    await S.waitIdle(); await S.waitConsole('Принято: имя');
    C.ok(!(await S.consoleText()).includes('Предыдущий вывод'), 'Прежняя история не возвращается после нового вывода');
  },
}, {
  id: 'console-background-screen-light-theme',
  name: 'SCREEN не оставляет чёрную полосу в светлой консоли',
  async fn({ S, C }) {
    const file = writeAxm(testWorkspace('console-background'), 'circle.axm',
      'SCREEN 10\nCIRCLE (10, 10), 5, 14\nSLEEP\n');
    await S.launch();
    await S.page.evaluate(async p => { window.__ide.setTheme('light'); await window.__ide.openFile(p); await window.__ide.run(); }, file);
    await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
    const background = () => S.page.evaluate(() => getComputedStyle(document.querySelector('#terminal .xterm-viewport')).backgroundColor);
    C.eq(await background(), 'rgb(255, 255, 255)', 'Остаток панели под строками белый во время SCREEN');
    await S.screenshot('console-background-screen-light');
    await S.page.evaluate(() => window.__ide.stop());
    await S.waitIdle();
    C.eq(await background(), 'rgb(255, 255, 255)', 'После остановки белый фон сохраняется');
    await S.page.evaluate(() => window.__ide.setTheme('dark'));
    C.eq(await background(), 'rgb(21, 24, 32)', 'Тёмная тема применяет фон консоли');
    await S.page.evaluate(() => window.__ide.setTheme('light'));
    C.eq(await background(), 'rgb(255, 255, 255)', 'Повторная смена темы не возвращает чёрный фон');
    const geometry = [];
    for (const theme of ['light', 'dark', 'basic']) {
      const skin = await S.page.evaluate(theme => {
        const ide = window.__ide; ide.setTheme(theme);
        const tab = document.querySelector('.editor-tab.active');
        const style = getComputedStyle(tab), stripe = getComputedStyle(tab, '::after');
        const root = getComputedStyle(document.documentElement);
        return { geometry: [style.borderRadius, style.borderTopWidth, stripe.left, stripe.right, stripe.height, stripe.borderRadius],
          border: style.borderTopColor, stripe: stripe.backgroundColor,
          line: ide.editor.getOption(ide.monaco.editor.EditorOption.renderLineHighlight),
          editorToken: root.getPropertyValue('--xs-editor').trim(),
          editor: getComputedStyle(document.querySelector('.monaco-editor-background')).backgroundColor,
          sidebar: getComputedStyle(document.getElementById('shell-sidebar')).backgroundColor,
          console: ide.term.options.theme.background,
          consoleToken: root.getPropertyValue('--xs-console-bg').trim(),
        };
      }, theme);
      geometry.push(skin.geometry);
      C.eq(skin.line, 'none', `${theme}: нет фоновой заливки текущей строки`);
      C.eq(skin.console, skin.consoleToken, `${theme}: xterm использует общий фон консоли`);
      C.eq(skin.border === skin.stripe, false, `${theme}: верхняя рамка не дублирует акцентную полосу`);
      if (theme === 'light') C.eq(skin.editor, 'rgb(255, 255, 255)', 'Светлый редактор белый');
      if (theme === 'basic') C.eq(skin.sidebar === skin.editor, false, 'QuickBasic отделяет боковую панель от поля кода');
      await S.screenshot(`global-theme-${theme}`);
    }
    C.eq(JSON.stringify(geometry[1]), JSON.stringify(geometry[0]), 'Геометрия вкладки одинакова в светлой и тёмной темах');
    C.eq(JSON.stringify(geometry[2]), JSON.stringify(geometry[0]), 'Геометрия вкладки одинакова в QuickBasic');
  },
}];
