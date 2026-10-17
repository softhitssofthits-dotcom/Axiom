const versions = require('../../../versions.json');
module.exports = [{ id: 'help-local-manual-and-window-lifecycle', name: 'Локальный справочник открывается из меню, переходит к статье и не получает Node.js', async fn({ S, C }) {
  await S.launch();
  const [manual] = await Promise.all([
    S.electronApp.waitForEvent('window'),
    S.page.locator('[data-menu="help"] > .menu-label').click().then(() => S.page.locator('[data-action="manual"]').click()),
  ]);
  await manual.waitForSelector('h1');
  C.contains(manual.url(), '/docs/manual/index.html', 'Открыт локальный индекс справочника');
  C.eq(await manual.evaluate(() => typeof require), 'undefined', 'Страница не имеет Node require');
  C.eq(await manual.evaluate(() => typeof window.axiom), 'undefined', 'Страница не получает API основной IDE');
  await manual.locator('a[href="print.html"]:visible').first().click();
  await manual.waitForURL('**/print.html');
  C.contains(await manual.locator('h1').textContent(), 'PRINT', 'Настоящая ссылка открыла статью');
  C.eq(await S.page.evaluate(() => window.axiom.openManual().then(result => result.ok)), true, 'Повторная команда принята');
  C.eq(await S.electronApp.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows().length), 2, 'Повтор не создаёт третьего окна');
  await S.close(); C.eq(manual.isClosed(), true, 'Закрытие IDE освобождает окно справочника');
} }, { id: 'help-about-component-versions', name: 'О программе показывает фактические версии IDE, языка, компилятора и BytecodeVm', async fn({ S, C }) {
  await S.launch();
  for (const language of ['ru', 'en']) {
    await S.page.evaluate(language => window.__ide.setLanguage(language), language);
    await S.page.locator('[data-menu="help"] > .menu-label').click(); await S.page.locator('[data-action="about"]').click();
    await S.page.waitForSelector('.about-dialog');
    const values = await S.page.locator('.about-row code').allTextContents();
    C.eq(values[0], versions.ide, 'Версия IDE соответствует пакету');
    C.eq(values[1], versions.language, 'Версия языка получена от CLI');
    C.eq(values[2], versions.compiler, 'Версия компилятора получена от CLI');
    C.contains(values.join(' '), 'BytecodeVm', 'Указана действующая виртуальная машина');
    await S.page.locator('#about-close').click();
  }
} }];
