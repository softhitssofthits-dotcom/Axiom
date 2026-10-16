module.exports = [{
  id: 'theme-registry-consistency',
  name: 'Реестр тем: меню «Вид», признак семейства оформления и предпросмотр настроек совпадают с themes.js',
  async fn({ S, C }) {
    await S.launch();
    const ids = await S.page.evaluate(() => window.AxiomThemes.ids());
    C.eq(JSON.stringify(ids), JSON.stringify(['dark', 'light', 'basic', 'borland']), 'Реестр объявляет четыре темы в стабильном порядке');

    const menuItems = await S.page.evaluate(() => [...document.querySelectorAll('[data-action^="theme-"]')].map(el => el.dataset.action.slice('theme-'.length)));
    C.eq(JSON.stringify([...menuItems].sort()), JSON.stringify([...ids].sort()), 'Пункты меню «Вид» совпадают с реестром тем');

    // Обе полосы изменения размера (консоль и боковая панель) рисуются единообразно
    // во всех темах: 1px линия из палитры, без отдельной заливки полосы.
    for (const theme of ids) {
      await S.page.evaluate(t => window.__ide.setTheme(t), theme);
      const bars = await S.page.evaluate(() => ['panel-resizer', 'sidebar-resizer'].map(id => {
        const el = document.getElementById(id), line = getComputedStyle(el, '::before');
        return { id, background: getComputedStyle(el).backgroundColor, lineColor: line.backgroundColor, thickness: id === 'panel-resizer' ? line.height : line.width };
      }));
      for (const bar of bars) {
        C.eq(bar.background, 'rgba(0, 0, 0, 0)', theme + ': полоса ' + bar.id + ' не заливается отдельным цветом');
        C.eq(bar.thickness, '1px', theme + ': ' + bar.id + ' рисуется линией 1px');
      }
      C.eq(bars[0].thickness, bars[1].thickness, theme + ': обе полосы одинаковой толщины');
      const sidebarBg = await S.page.evaluate(() => getComputedStyle(document.getElementById('shell-sidebar')).backgroundColor);
      C.ok(bars[1].lineColor !== sidebarBg, theme + ': вертикальная полоса различима на фоне панели', bars[1].lineColor + ' vs ' + sidebarBg);

      // Полоска активной вкладки во всех темах берёт цвет из одной переменной.
      const tabStripe = await S.page.evaluate(() => {
        const color = getComputedStyle(document.querySelector('.editor-tab.active'), '::after').backgroundColor;
        const probe = document.createElement('span'); document.body.appendChild(probe);
        probe.style.color = getComputedStyle(document.documentElement).getPropertyValue('--xs-tab-active-stripe').trim();
        const expected = getComputedStyle(probe).color; probe.remove();
        return { color, expected };
      });
      C.eq(tabStripe.color, tabStripe.expected, theme + ': полоска активной вкладки совпадает с --xs-tab-active-stripe');
      if (theme === 'borland') C.eq(tabStripe.color, 'rgb(0, 170, 0)', 'Borland Pascal: полоска активной вкладки зелёная');

      // Консоль и нижняя панель продолжают синюю гамму рабочего поля, а не чёрный фон.
      const panelBg = await S.page.evaluate(() => getComputedStyle(document.getElementById('bottom-panel')).backgroundColor);
      if (theme === 'borland') C.eq(panelBg, 'rgb(0, 0, 128)', 'Borland Pascal: консоль и нижняя панель синие в тон рабочему полю');
      else C.ok(panelBg !== 'rgb(0, 0, 128)', theme + ': фон панели не подменяется палитрой Pascal');
    }

    for (const theme of ids) {
      await S.page.evaluate(t => window.__ide.setTheme(t), theme);
      const state = await S.page.evaluate(t => ({
        dataTheme: document.documentElement.dataset.theme,
        dataChrome: document.documentElement.dataset.chrome,
        kind: window.AxiomThemes.kind(t),
      }), theme);
      C.eq(state.dataTheme, theme, theme + ': атрибут data-theme применён');
      C.eq(state.dataChrome, state.kind, theme + ': атрибут data-chrome совпадает с семейством оформления');

      await S.page.locator('#menu-settings').click();
      await S.page.locator('#settings-tab-font').click();
      await S.page.waitForFunction(t => document.documentElement.dataset.theme === t && document.querySelector('#settings-tab-font')?.getAttribute('aria-selected') === 'true', theme);
      const preview = await S.page.evaluate(() => {
        const el = document.querySelector('.settings-preview');
        const color = sel => getComputedStyle(el.querySelector(sel)).color;
        return {
          keyword: color('.preview-keyword'), type: color('.preview-type'), string: color('.preview-string'),
          number: color('.preview-number'), comment: color('.preview-comment'), bg: getComputedStyle(el).backgroundColor,
        };
      });
      const expected = await S.page.evaluate(t => {
        const probe = document.createElement('span'); document.body.appendChild(probe);
        const toRgb = hex => { probe.style.color = hex; return getComputedStyle(probe).color; };
        const syntax = window.AxiomThemes.byId(t).syntax;
        const editor = getComputedStyle(document.documentElement).getPropertyValue('--xs-editor').trim();
        const result = { keyword: toRgb(syntax.keyword), type: toRgb(syntax.type), string: toRgb(syntax.string), number: toRgb(syntax.number), comment: toRgb(syntax.comment), bg: toRgb(editor) };
        probe.remove(); return result;
      }, theme);
      C.eq(preview.keyword, expected.keyword, theme + ': предпросмотр keyword совпадает с реестром');
      C.eq(preview.type, expected.type, theme + ': предпросмотр type совпадает с реестром');
      C.eq(preview.string, expected.string, theme + ': предпросмотр string совпадает с реестром');
      C.eq(preview.number, expected.number, theme + ': предпросмотр number совпадает с реестром');
      C.eq(preview.comment, expected.comment, theme + ': предпросмотр comment совпадает с реестром');
      C.eq(preview.bg, expected.bg, theme + ': предпросмотр использует фон редактора темы');
      await S.page.keyboard.press('Escape');
    }
    await S.page.evaluate(() => window.__ide.setTheme('dark'));
    C.eq(S.pageErrors.length, 0, 'Нет ошибок страницы');
  },
}];
