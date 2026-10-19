const { testWorkspace, writeAxm } = require('../helpers');

async function launch(S, tag, prefix = '') {
  const file = writeAxm(testWorkspace(tag), 'консоль.axm', prefix + 'DIM answer AS STRING\nINPUT "Ответ: ", answer\nPRINT "Принято: "; answer\n');
  await S.launch();
  await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); }, file);
  await S.page.waitForFunction(() => !!window.__ide.execution.inputRequestId);
  await S.waitConsole('Ответ:');
}

async function visible(S) {
  return S.page.evaluate(() => new Promise(resolve => {
    const t = window.__ide.term;
    t.write('', () => {
      let text = '';
      for (let n = 0; n < t.buffer.active.length; n++) {
        const line = t.buffer.active.getLine(n);
        text += (n && !line.isWrapped ? '\n' : '') + line.translateToString(true);
      }
      resolve(text.trimEnd());
    });
  }));
}

module.exports = [{
  id: 'console-unicode-backspace',
  name: 'Backspace сохраняет согласованность INPUT для широких, комбинируемых символов и переноса строки',
  async fn({ S, C }) {
    for (const item of [{ value: 'А界', expected: 'А' }, { value: 'А🙂', expected: 'А' }, { value: 'Ае\u0301', expected: 'Ае' }, { value: 'я'.repeat(100), expected: 'я'.repeat(99), wrap: true }, { value: 'я'.repeat(14), expected: 'я'.repeat(12), wrap: true, count: 2 }]) {
      await launch(S, 'console-unicode-edit');
      if (item.wrap) await S.page.evaluate(() => window.__ide.term.resize(20, 12));
      await S.page.evaluate(value => window.__ide.execution.input(value), item.value);
      await visible(S);
      await S.page.evaluate(() => window.__ide.term.focus());
      for (let n = 0; n < (item.count || 1); n++) await S.page.keyboard.press('Backspace');
      await S.page.waitForFunction(expected => window.__ide.execution.inputText === expected, item.expected);
      C.eq(await visible(S), 'Ответ: ' + item.expected, 'После удаления видимый текст совпадает с ответом: ' + item.value.slice(-4));
      await S.page.keyboard.type('Б');
      await S.page.waitForFunction(expected => window.__ide.execution.inputText === expected + 'Б', item.expected);
      C.eq(await visible(S), 'Ответ: ' + item.expected + 'Б', 'Следующая буква стоит на правильном месте');
      await S.page.keyboard.press('Enter'); await S.waitIdle();
      C.contains(await visible(S), 'Принято: ' + item.expected + 'Б', 'Программа получила тот же ответ');
      await S.close();
    }
  },
}, {
  id: 'console-clear-copy-unicode-continuation',
  name: 'Очистка и копирование Unicode при INPUT сохраняют продолжение вывода и ответ',
  async fn({ S, C }) {
    await launch(S, 'console-clear-copy-unicode', 'PRINT "Старая строка"\nPRINT\nPRINT "Продолжение";\nPRINT " строки 🙂"\n');
    C.contains(await visible(S), 'Старая строка\n\nПродолжение строки 🙂', 'PRINT без текста и продолжение строки видны');
    await S.electronApp.evaluate(({ ipcMain }) => {
      ipcMain.removeHandler('write-clipboard-text');
      ipcMain.handle('write-clipboard-text', (_event, text) => { globalThis.__copiedConsoleUnicode = text; return true; });
    });
    await S.page.evaluate(() => window.__ide.execution.input('Имя界е\u0301'));
    const before = await S.page.evaluate(() => ({ id: window.__ide.execution.inputRequestId, session: window.__ide.execution.sessionId }));
    for (let n = 0; n < 2; n++) {
      await S.page.locator('#terminal').click({ button: 'right', position: { x: 30, y: 30 } });
      await S.page.locator('#ax-console-context-menu [data-action="clear"]').click();
      C.eq(await visible(S), 'Ответ: Имя界е\u0301', 'Очистка оставила только активное приглашение и ответ');
    }
    await S.page.keyboard.press('Backspace');
    await S.page.waitForFunction(() => window.__ide.execution.inputText === 'Имя界е');
    C.eq(await visible(S), 'Ответ: Имя界е', 'Перерисовка после удаления не возвращает скрытую историю');
    await S.page.evaluate(() => window.__ide.execution.input('\u0301'));
    await S.page.evaluate(() => window.__ide.term.selectAll());
    const selected = await S.page.evaluate(() => window.__ide.term.getSelection());
    await S.page.locator('#terminal').click({ button: 'right', position: { x: 30, y: 30 } });
    await S.page.locator('#ax-console-context-menu [data-action="copy"]').click();
    C.eq(await S.electronApp.evaluate(() => globalThis.__copiedConsoleUnicode), selected, 'Выделенный Unicode передан через IPC без изменения');
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputRequestId), before.id, 'Копирование и очистка сохранили запрос');
    C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), before.session, 'Сессия сохранена');
    await S.page.evaluate(() => window.__ide.term.clearSelection());
    await S.page.keyboard.press('Enter'); await S.waitIdle();
    C.contains(await visible(S), 'Принято: Имя界е\u0301', 'Ответ принят без изменения');
    C.ok(!(await visible(S)).includes('Старая строка'), 'Скрытая история не вернулась после нового вывода');
  },
}, {
  id: 'console-clear-pending-input',
  name: 'Очистка во время отправки INPUT сохраняет повтор после отказа и скрытую историю',
  async fn({ S, C }) {
    await launch(S, 'console-clear-pending', 'PRINT "Старая история"\n');
    await S.page.evaluate(() => {
      const e = window.__ide.execution, original = e.api;
      const state = window.__pendingConsoleClear = { original, ready: false };
      e.api = { ...original, submitInput: async (session, id, text) => {
        const result = await original.submitInput(session, 'invalid-input-id', text);
        if (result.ok) throw new Error('Неверный запрос должен быть отклонён');
        return new Promise(resolve => { state.release = () => resolve(result); state.ready = true; });
      } };
      state.operation = e.input('Анна\r');
    });
    await S.page.waitForFunction(() => window.__pendingConsoleClear.ready);
    await S.page.locator('#terminal').click({ button: 'right', position: { x: 30, y: 30 } });
    await S.page.locator('#ax-console-context-menu [data-action="clear"]').click();
    C.eq(await visible(S), '', 'Очистка убрала показанный вывод во время отправки');
    await S.page.evaluate(async () => {
      const s = window.__pendingConsoleClear; window.__ide.execution.api = s.original;
      s.release(); await s.operation;
    });
    C.eq(await visible(S), 'Ответ: Анна', 'Отказ восстановил только приглашение и ответ');
    C.eq(await S.page.evaluate(() => window.__ide.running), true, 'Программа продолжает ждать ввод');
    C.eq(await S.page.evaluate(() => window.__ide.execution.inputText), 'Анна', 'Текст для повтора сохранён');
    await S.page.keyboard.press('Enter'); await S.waitIdle();
    C.contains(await visible(S), 'Принято: Анна', 'Повтор принят');
    C.ok(!(await visible(S)).includes('Старая история'), 'Скрытый вывод не вернулся');
  },
}, {
  id: 'console-leading-empty-lines',
  name: 'Начальные пустые PRINT сохраняются перед INPUT и удаляются очисткой',
  async fn({ S, C }) {
    for (const count of [1, 2]) {
      await launch(S, 'console-leading-empty-' + count, 'PRINT\n'.repeat(count));
      C.eq(await visible(S), '\n'.repeat(count) + 'Ответ:', 'Сохранено начальных пустых строк: ' + count);
      await S.page.evaluate(() => window.__ide.execution.clearConsole());
      C.eq(await visible(S), 'Ответ:', 'Очистка скрыла пустую историю');
      await S.page.evaluate(() => window.__ide.execution.input('да\r'));
      await S.waitIdle();
      C.ok(!(await visible(S)).startsWith('\n'), 'Пустая история не вернулась после ответа');
      C.contains(await visible(S), 'Принято: да', 'Строковый ответ принят');
      await S.close();
    }
  },
}];
