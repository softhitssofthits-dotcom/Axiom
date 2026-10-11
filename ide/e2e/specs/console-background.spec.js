const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
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
