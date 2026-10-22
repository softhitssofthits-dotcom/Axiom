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
}, {
  id: 'console-paste-enter-order',
  name: 'Ctrl+V и немедленный Enter сохраняют порядок после задержанного чтения буфера',
  async fn({ S, C }) {
    await launch(S, 'console-paste-enter-order');
    await S.electronApp.evaluate(({ ipcMain }) => {
      const state = globalThis.__consolePasteOrder = { reads: 0 };
      ipcMain.removeHandler('read-clipboard-text');
      ipcMain.handle('read-clipboard-text', () => {
        state.reads++;
        return new Promise(resolve => { state.release = resolve; });
      });
    });
    await S.page.evaluate(() => {
      const ide = window.__ide, execution = ide.execution;
      const state = window.__consolePasteOrder = { calls: [], terminalData: [], request: execution.inputRequestId };
      const original = execution.input.bind(execution);
      execution.input = data => { state.calls.push(data); return original(data); };
      ide.term.onData(data => state.terminalData.push(data));
      ide.term.focus();
    });
    await S.page.keyboard.press('Control+V');
    await S.electronApp.evaluate(async () => {
      const deadline = Date.now() + 5000;
      while (!globalThis.__consolePasteOrder.release && Date.now() < deadline) {
        await new Promise(resolve => setTimeout(resolve, 10));
      }
      if (!globalThis.__consolePasteOrder.release) throw new Error('Не получен запрос буфера обмена');
    });
    await S.page.keyboard.press('Enter');
    await S.page.waitForFunction(() => window.__consolePasteOrder.terminalData.includes('\r'));
    const pending = await S.page.evaluate(() => ({
      calls: window.__consolePasteOrder.calls,
      request: window.__ide.execution.inputRequestId,
      expectedRequest: window.__consolePasteOrder.request,
      text: window.__ide.execution.inputText,
    }));
    C.eq(pending.calls.length, 0, 'Enter ждёт завершения чтения буфера');
    C.eq(pending.request, pending.expectedRequest, 'Запрос INPUT ещё не отправлен');
    C.eq(pending.text, '', 'Ответ не изменился до завершения чтения');
    await S.electronApp.evaluate(() => globalThis.__consolePasteOrder.release('Очередь\u001b\u0003'));
    await S.waitIdle();
    await S.waitConsole('Принято: Очередь');
    const calls = await S.page.evaluate(() => window.__consolePasteOrder.calls);
    C.eq(calls.length, 2, 'Ввод получил ровно текст вставки и Enter');
    C.eq(calls[0], 'Очередь', 'Очищенный текст вставлен первым');
    C.eq(calls[1], '\r', 'Enter передан после вставки');
    C.eq(await S.electronApp.evaluate(() => globalThis.__consolePasteOrder.reads), 1, 'Одно сочетание прочитало буфер один раз');
    C.contains(await visible(S), 'Принято: Очередь', 'Настоящая программа получила вставленную строку');
  },
}, {
  id: 'console-ide-shortcuts-preserve-input',
  name: 'Команды IDE из консоли не попадают в INPUT и сохраняют обычный ввод',
  async fn({ S, C }) {
    await launch(S, 'console-ide-shortcuts-preserve-input');
    await S.page.evaluate(() => {
      const ide = window.__ide, execution = ide.execution;
      const state = window.__consoleShortcutInput = {
        calls: [], actions: [], trace: [], session: execution.sessionId, request: execution.inputRequestId,
      };
      for (const name of ['save', 'check']) {
        const original = ide[name].bind(ide);
        ide[name] = (...args) => { state.pendingAction = original(...args); return state.pendingAction; };
      }
      const input = execution.input.bind(execution), action = ide.handleAction.bind(ide), trace = ide.trace.command.bind(ide.trace);
      execution.input = data => { state.calls.push(data); return input(data); };
      ide.handleAction = command => { state.actions.push(command); return action(command); };
      ide.trace.command = command => { state.trace.push(command); return trace(command); };
      ide.term.focus();
    });
    await S.page.keyboard.type('ab');
    await S.page.waitForFunction(() => window.__ide.execution.inputText === 'ab');
    const priorCount = await S.page.evaluate(() => window.__consoleShortcutInput.calls.length);
    for (const key of ['Control+S', 'Control+Shift+C', 'F9']) {
      await S.page.locator('.xterm-helper-textarea').focus();
      await S.page.keyboard.press(key);
      await S.page.evaluate(async () => {
        await window.__consoleShortcutInput.pendingAction;
        window.__ide.switchPanel('console');
        window.__ide.term.focus();
      });
    }
    await S.page.evaluate(async () => { await window.__ide._consoleWriteChain; });
    const state = await S.page.evaluate(() => ({
      calls: window.__consoleShortcutInput.calls,
      actions: window.__consoleShortcutInput.actions,
      trace: window.__consoleShortcutInput.trace,
      session: window.__consoleShortcutInput.session,
      request: window.__consoleShortcutInput.request,
      currentSession: window.__ide.execution.sessionId,
      currentRequest: window.__ide.execution.inputRequestId,
      text: window.__ide.execution.inputText,
    }));
    C.eq(state.actions.join(','), 'save,check', 'Каждое сочетание вызвало одну команду оболочки');
    C.eq(state.trace.join(','), 'breakpoint', 'F9 вызвал только команду точки остановки');
    C.eq(state.calls.length, priorCount, 'Команды IDE не передали символов или escape-последовательностей в INPUT');
    C.eq(state.text, 'ab', 'Набранный ответ сохранён');
    C.eq(state.currentSession, state.session, 'Сессия программы сохранена');
    C.eq(state.currentRequest, state.request, 'Запрос INPUT сохранён');
    await S.page.locator('.xterm-helper-textarea').focus();
    await S.page.keyboard.type('cd');
    await S.page.waitForFunction(() => window.__ide.execution.inputText === 'abcd');
    await S.page.keyboard.press('Enter');
    await S.waitIdle();
    await S.waitConsole('Принято: abcd');
    C.eq(await S.page.evaluate(() => window.__consoleShortcutInput.calls.join('')), 'abcd\r', 'Обычные символы и Enter дошли без посторонних данных');
    C.contains(await visible(S), 'Принято: abcd', 'Программа получила полный ответ после команд IDE');
  },
}];
