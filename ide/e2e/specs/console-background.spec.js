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
  },
}];
