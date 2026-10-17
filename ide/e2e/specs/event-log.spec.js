const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace } = require('../helpers');
module.exports = [{ id: 'event-log-view-filter-copy-clear', name: 'Журнал IDE: просмотр, фильтр, копирование, открытие и очистка отделены от вывода программы', async fn({ S, C }) {
  const profile = testWorkspace('event-log-profile'), logs = path.join(profile, 'logs'); fs.mkdirSync(logs);
  const record = (level, event, data) => JSON.stringify({ time: new Date().toISOString(), level, event, data }) + '\n';
  fs.writeFileSync(path.join(logs, 'ide.log'), record('info', 'acceptance.info', { text: 'Обычная запись' }) +
    record('error', 'acceptance.error', { text: 'Русская ошибка <img src=x onerror="window.__logInjected=true">' }) + '{broken}\n');
  await S.launch({ userData: profile });
  await S.electronApp.evaluate(({ ipcMain, shell }) => {
    global.__logOpened = []; shell.openPath = async target => { global.__logOpened.push(target); return ''; };
    ipcMain.removeHandler('write-clipboard-text'); ipcMain.handle('write-clipboard-text', (_event, text) => { global.__logCopied = text; return true; });
  });
  const before = await S.page.evaluate(() => window.__ide.execution.outputText);
  await S.page.locator('[data-menu="help"] > .menu-label').click(); await S.page.locator('[data-action="event-log"]').click();
  await S.page.waitForFunction(() => document.querySelector('.event-log-text')?.value.includes('acceptance.error'));
  C.ok(await S.page.evaluate(() => document.getElementById('app').inert), 'Диалог удерживает ввод внутри себя');
  C.eq(await S.page.evaluate(() => window.__logInjected), undefined, 'Содержимое журнала не исполняется как HTML');
  await S.page.locator('.event-log-filters select').first().selectOption('error');
  await S.page.locator('.event-log-filters input').fill('Русская');
  await S.page.waitForFunction(() => { const value = document.querySelector('.event-log-text').value; return value.includes('acceptance.error') && !value.includes('acceptance.info'); });
  await S.page.locator('[data-log-action="copy"]').click();
  C.contains(await S.electronApp.evaluate(() => global.__logCopied), 'Русская ошибка', 'Отфильтрованный русский текст передан через IPC копирования');
  await S.page.locator('[data-log-action="openFile"]').click();
  await S.electronApp.evaluate(async () => { for (let n = 0; n < 100 && !global.__logOpened.length; n++) await new Promise(resolve => setTimeout(resolve, 10)); });
  C.ok((await S.electronApp.evaluate(() => global.__logOpened)).some(file => file.endsWith('ide.log')), 'Команда открытия передала собственный файл журнала');
  await S.page.locator('[data-log-action="openFolder"]').click();
  await S.electronApp.evaluate(async logs => { for (let n = 0; n < 100 && !global.__logOpened.includes(logs); n++) await new Promise(resolve => setTimeout(resolve, 10)); }, logs);
  C.ok((await S.electronApp.evaluate(() => global.__logOpened)).includes(logs), 'Открывается папка текущего профиля');
  await S.page.locator('[data-log-action="clear"]').click();
  await S.page.waitForFunction(() => !document.querySelector('.event-log-text').value.includes('acceptance.error'));
  C.eq(await S.page.evaluate(() => window.__ide.execution.outputText), before, 'Просмотр и очистка не меняют вывод программы');
  C.ok(!fs.readdirSync(logs).some(file => file.startsWith('ide-')), 'Старые файлы журнала очищены');
  await S.page.keyboard.press('Escape');
  C.eq(await S.page.locator('.event-log-dialog').count(), 0, 'Escape закрывает диалог');
  C.eq(await S.page.evaluate(() => document.getElementById('app').inert), false, 'Основной интерфейс снова принимает ввод');
} }, { id: 'event-log-locales-and-themes', name: 'Журнал использует общий язык, темы и клавиатурный фокус', async fn({ S, C }) {
  await S.launch();
  for (const language of ['ru', 'en']) {
    await S.page.evaluate(language => window.__ide.setLanguage(language), language);
    await S.page.evaluate(() => window.__ide.handleAction('event-log'));
    await S.page.waitForSelector('.event-log-dialog');
    C.contains(await S.page.locator('.event-log-dialog h2').textContent(), language === 'ru' ? 'Журнал' : 'Log', 'Заголовок соответствует языку ' + language);
    for (const theme of ['dark', 'light', 'basic', 'borland']) {
      await S.page.evaluate(theme => window.__ide.setTheme(theme), theme);
      C.ok(await S.page.locator('.event-log-dialog').evaluate(element => {
        const style = getComputedStyle(element); return style.backgroundColor !== 'rgba(0, 0, 0, 0)' && style.color !== style.backgroundColor;
      }), 'Общая тема сохраняет читаемость журнала: ' + theme);
    }
    await S.page.locator('.event-log-filters input').focus();
    await S.page.keyboard.press('Shift+Tab');
    C.eq(await S.page.evaluate(() => document.activeElement.dataset.logAction), 'close', 'Shift+Tab достигает кнопки закрытия');
    await S.page.keyboard.press('Escape');
  }
} }];
