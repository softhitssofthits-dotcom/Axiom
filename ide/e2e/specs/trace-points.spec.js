const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');
const source = 'PRINT 1\nPRINT 2\nPRINT 3\n';
async function open(S, tag) {
  const directory = testWorkspace(tag);
  const first = writeAxm(directory, 'первый.axm', source);
  const other = writeAxm(directory, 'другой.axm', source);
  await S.launch();
  await S.page.evaluate(async ({ first, other }) => {
    const i = window.__ide; i.settings.formatOnSave = false; i.settings.formatOnBlur = false;
    i.settings.smartIdentifierEnabled = false; i.settings.checkOnChange = false;
    await i.openFile(other); await i.openFile(first);
  }, { first, other });
  return { first, other, directory };
}
async function f9(S, line) {
  await S.page.evaluate(line => { window.__ide.editor.setPosition({ lineNumber: line, column: 1 }); window.__ide.editor.focus(); }, line);
  await S.page.keyboard.press('F9');
}
async function lines(S, file) {
  return S.page.evaluate(file => window.__ide.trace.lines(window.__ide.openFiles.get(file).model).join(','), file);
}
async function paused(S, line) {
  await S.page.waitForFunction(line => window.__ide.execution.stopId && !window.__ide.execution.debugBusy && window.__ide.execution.line === line, line);
}
module.exports = [{
  id: 'trace-points-documents-edit-clear',
  name: 'F9, перенос при правке и очистка точек в двух документах',
  async fn({ S, C }) {
    const { first, other } = await open(S, 'trace-points-edit');
    await f9(S, 2); await f9(S, 2);
    C.eq(await lines(S, first), '', 'Повторный F9 снимает точку');
    await f9(S, 2);
    await S.page.evaluate(() => {
      const i = window.__ide, m = i.editor.getModel();
      m.pushEditOperations([], [{ range: new i.monaco.Range(1, 1, 1, 1), text: "' Перед программой\n" }], () => null);
    });
    C.eq(await lines(S, first), '3', 'Точка следует за строкой при вставке');
    await S.page.evaluate(() => window.__ide.editor.trigger('test', 'undo', null));
    C.eq(await lines(S, first), '2', 'Отмена возвращает строку точки');
    await S.page.evaluate(file => window.__ide.switchToFile(file), other); await f9(S, 3);
    C.eq(await lines(S, other), '3', 'Другой документ хранит свою точку');
    C.eq(await lines(S, first), '2', 'Точка первого документа сохранена');
    await S.page.keyboard.press('Control+Shift+F9');
    C.eq(await lines(S, first), '', 'Очистка снимает точки неактивной модели');
    C.eq(await lines(S, other), '', 'Очистка снимает точки активной модели');
    C.eq(await S.page.evaluate(() => window.__ide.trace.points.size), 0, 'Реестр очищен');
  },
}, {
  id: 'trace-points-saveas-reopen',
  name: 'Save As сохраняет точки модели, повторное открытие не наследует закрытые точки',
  async fn({ S, C }) {
    const { first, directory } = await open(S, 'trace-points-saveas');
    const copy = path.join(directory, 'копия.axm');
    await f9(S, 2);
    const id = await S.page.evaluate(() => window.__ide.openFiles.get(window.__ide.activeFile).documentId);
    await S.stubSaveDialog(copy); await S.page.evaluate(() => window.__ide.saveAs());
    C.eq(await lines(S, copy), '2', 'Save As сохраняет точку');
    C.eq(await S.page.evaluate(() => window.__ide.openFiles.get(window.__ide.activeFile).documentId), id, 'Запись документа сохранена');
    C.eq(await S.page.evaluate(file => window.__ide.openFiles.has(file), first), false, 'Прежний путь освобождён');
    await S.page.keyboard.press('F5'); await paused(S, 2);
    C.eq(await S.page.evaluate(() => window.__ide._runFile), copy, 'Запуск использует новый путь');
    await S.page.keyboard.press('F5'); await S.waitIdle(); await S.waitConsole('3');
    await S.page.evaluate(async file => { await window.__ide.closeFile(file, true); await window.__ide.openFile(file); }, copy);
    C.eq(await lines(S, copy), '', 'Повторно открытая модель не наследует точку');
    C.eq(await S.page.evaluate(() => [...window.__ide.trace.points.keys()].some(m => m.isDisposed())), false, 'Закрытые модели удалены из реестра');
  },
}, {
  id: 'trace-points-paused-saveas',
  name: 'Save As на паузе сохраняет владельца снимка, точки и защиту исполняемой модели',
  async fn({ S, C }) {
    const { first, other, directory } = await open(S, 'trace-points-paused');
    const copy = path.join(directory, 'на-паузе.axm');
    await f9(S, 2); await f9(S, 3);
    await S.page.keyboard.press('F5'); await paused(S, 2);
    const snapshot = await S.page.evaluate(() => window.__ide.execution.sourceSnapshot);
    await S.stubSaveDialog(copy); await S.page.evaluate(() => window.__ide.saveAs());
    C.eq(await S.page.evaluate(() => window.__ide._runFile), copy, 'Владелец исполнения следует за Save As');
    C.eq(await lines(S, copy), '2,3', 'Точки модели сохранены');
    C.eq(await S.page.evaluate(() => JSON.stringify(window.__ide.execution.sourceSnapshot)), JSON.stringify(snapshot), 'Исходный снимок запуска не переписан');
    await S.page.evaluate(file => window.__ide.switchToFile(file), other);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getRawOptions().readOnly), false, 'Другая модель доступна для редактирования');
    await f9(S, 1); await S.page.keyboard.press('Control+Shift+F9');
    C.eq(await lines(S, other), '', 'F9 не меняет набор во время исполнения');
    C.eq(await lines(S, copy), '2,3', 'Очистка не меняет действующие точки');
    await S.page.evaluate(file => window.__ide.switchToFile(file), copy);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getRawOptions().readOnly), true, 'Исполняемая модель после Save As защищена');
    await S.page.keyboard.press('F10'); await paused(S, 3);
    C.eq(await S.page.evaluate(() => window.__ide.activeFile), copy, 'Шаг показывает текущий путь исполняемого документа');
    C.eq(await S.page.evaluate(() => window.__ide.trace.marker?.model === window.__ide.openFiles.get(window.__ide.activeFile).model), true, 'Указатель шага принадлежит исходной модели');
    const session = await S.page.evaluate(() => window.__ide.execution.sessionId);
    await S.page.evaluate(() => window.__ide.trace.command('restart')); await paused(S, 2);
    C.ok(await S.page.evaluate(session => window.__ide.execution.sessionId !== session, session), 'Restart создал новую сессию после Save As');
    C.eq(await S.page.evaluate(() => window.__ide._runFile), copy, 'Restart использует новый путь');
    C.eq(await lines(S, copy), '2,3', 'Restart сохранил точки модели');
    C.eq(await S.page.evaluate(file => window.__ide.closeFile(file, true), copy), false, 'Исполняемую модель нельзя закрыть после Save As');
    await S.page.keyboard.press('Shift+F5'); await S.waitIdle();
    C.eq(await S.page.evaluate(() => window.__ide.trace.marker), null, 'Stop снял указатель');
  },
}];
