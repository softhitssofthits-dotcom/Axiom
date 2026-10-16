const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');

function folders(tag) {
  const root = testWorkspace(tag);
  return ['Первая папка', 'Вторая папка'].map((name, index) => {
    const directory = path.join(root, name);
    fs.mkdirSync(path.join(directory, 'Вложенная папка'), { recursive: true });
    fs.writeFileSync(path.join(directory, 'marker.txt'), `Корень ${index + 1}`, 'utf8');
    fs.writeFileSync(path.join(directory, 'Вложенная папка', 'marker.txt'), `Вложенный ${index + 1}`, 'utf8');
    return directory;
  });
}

module.exports = [{
  id: 'execution-directories-identical-source',
  name: 'Одинаковый исходник в разных папках использует свои ресурсы и сбрасывает CHANGEDIR при повторе',
  async fn({ S, C }) {
    const directories = folders('execution-directory-cache');
    const source = 'PRINT CURRENTDIR()\nPRINT READFILE("marker.txt")\nPRINT CHANGEDIR("Вложенная папка")\nPRINT CURRENTDIR()\nPRINT READFILE("marker.txt")\nPRINT WRITEFILE("result.txt", READFILE("marker.txt"))\n';
    const files = directories.map(directory => writeAxm(directory, 'программа.axm', source));
    await S.launch();
    const processDirectory = await S.electronApp.evaluate(() => process.cwd());
    for (const index of [0, 1, 0]) {
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, files[index]);
      await S.waitIdle();
      const output = await S.page.evaluate(() => window.__ide.execution.outputText);
      C.eq(output.replace(/\r\n/g, '\n'), [directories[index], `Корень ${index + 1}`, 'TRUE',
        path.join(directories[index], 'Вложенная папка'), `Вложенный ${index + 1}`, 'TRUE'].join('\n'), 'Точный вывод относится к папке текущего файла');
      C.eq(fs.readFileSync(path.join(directories[index], 'Вложенная папка', 'result.txt'), 'utf8'), `Вложенный ${index + 1}`, 'Относительная запись использует CHANGEDIR текущего запуска');
      C.eq(fs.existsSync(path.join(directories[index], 'result.txt')), false, 'Запись не попала в корневую папку программы');
      C.eq((await S.diagnosticItems()).length, 0, 'Запуск завершился без диагностики');
    }
    C.eq(await S.electronApp.evaluate(() => process.cwd()), processDirectory, 'Рабочий каталог оболочки не изменился');
  },
}, {
  id: 'execution-directories-error-stop-reset',
  name: 'Ошибочный CHANGEDIR сохраняет каталог; ошибка и Stop не передают каталог следующему запуску',
  async fn({ S, C }) {
    const directories = folders('execution-directory-reset');
    const failed = writeAxm(directories[0], 'ошибка.axm', 'PRINT CHANGEDIR("Вложенная папка")\nTRY\nPRINT CHANGEDIR("Нет такой папки")\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT READFILE("marker.txt")\nPRINT READFILE("Нет такого файла.txt")\n');
    const waiting = writeAxm(directories[0], 'ожидание.axm', 'PRINT CHANGEDIR("Вложенная папка")\nDIM answer AS STRING\nINPUT answer\n');
    const next = directories.map(directory => writeAxm(directory, 'проверка.axm', 'PRINT CURRENTDIR()\nPRINT READFILE("marker.txt")\n'));
    await S.launch();
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, failed);
    await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r\n/g, '\n')), 'TRUE\nMB3007\nВложенный 1', 'Неверный CHANGEDIR сохранил прежний каталог');
    C.ok((await S.diagnosticItems()).some(item => JSON.stringify(item).includes('MB3007')), 'Необработанная ошибка чтения показана');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, next[0]);
    await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r\n/g, '\n')),
      `${directories[0]}\nКорень 1`, 'После ошибки запуск начинает с папки своего файла');
    C.eq((await S.diagnosticItems()).length, 0, 'Успешный запуск очистил прежнюю диагностику');
    for (const index of [0, 1]) {
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, waiting);
      await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
      await S.page.evaluate(() => window.__ide.execution.stop());
      await S.waitIdle();
      await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, next[index]);
      await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide.execution.outputText.replace(/\r\n/g, '\n')),
        `${directories[index]}\nКорень ${index + 1}`, 'После Stop запуск начинает с папки своего файла');
      C.eq((await S.diagnosticItems()).length, 0, 'Новый запуск не сохраняет прежнюю ошибку');
    }
  },
}];
