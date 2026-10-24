const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');

function enter(hwnd, text) {
  for (const char of text) probe(hwnd, 'char', { Key: char.charCodeAt(0) });
  probe(hwnd, 'down', { Key: 13 }); probe(hwnd, 'char', { Key: 13 }); probe(hwnd, 'up', { Key: 13 });
}

module.exports = [{
  id: 'graphics-input-native-lifecycle',
  name: 'SCREEN: нативный INPUT сохраняет фокус, закрывает запрос и повторяет неверное число',
  async fn({ S, C }) {
    await S.launch();
    const ideHwnd = await (await S.electronApp.browserWindow(S.page))
      .evaluate(w => w.getNativeWindowHandle().readBigUInt64LE().toString());
    for (const mode of [1, 0]) {
      const file = mode === 1 ? path.resolve(__dirname, '../../../samples/operators/input/screen_lifecycle.axm') :
        writeAxm(testWorkspace('graphics-input'), 'text.axm',
          'SCREEN 0\nDIM number AS INTEGER, name AS STRING\nDIM accepted AS BOOLEAN\nDO\nTRY\nINPUT "Число", number\naccepted = TRUE\nCATCH err\nPRINT "Введите целое число"\nEND TRY\nLOOP UNTIL accepted\nSLEEP 4000\nINPUT "Имя", name\nPRINT name\nSLEEP\n');
      await S.page.evaluate(() => {
        window.__nativeInputEvents = [];
        const execution = window.__ide.execution, original = execution.event.bind(execution);
        execution.event = message => { window.__nativeInputEvents.push(message); return original(message); };
      });
      await S.page.bringToFront();
      await S.page.evaluate(p => window.__ide.openFile(p), file);
      // bringToFront не гарантирует нативный foreground: это условие подготовки, не активация SCREEN.
      const focused = probe(ideHwnd, 'focus');
      const before = probe(ideHwnd, 'state');
      C.note(JSON.stringify({ mode, stage: 'before-run', focused, native: before }));
      if (!focused.focused || !before.focused) throw new Error('Не удалось подготовить активное окно IDE: ' + JSON.stringify(before));
      await S.page.locator('#tb-run').click();
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId && window.__ide.execution.surface?.windowVisible);
      const state = () => S.page.evaluate(() => ({ id: window.__ide.execution.inputRequestId,
        target: window.__ide.execution.inputTarget, running: window.__ide.running, prompt: window.__ide.execution.inputPrompt }));
      const hwnd = await S.page.evaluate(() => window.__ide.execution.surface.windowHandle);
      const checkFocus = (stage, message) => {
        const native = probe(hwnd, 'state');
        C.note(JSON.stringify({ mode, stage, native }));
        C.ok(native.focused, message, JSON.stringify(native));
      };
      const first = await state();
      C.eq(first.target, 'screen', `SCREEN ${mode}: запрос относится к нативному окну`);
      const initialFocus = probe(hwnd, 'state');
      if (!initialFocus.focused) {
        C.limitations.push({ scope: 'screen-' + mode + '.automatic-foreground', status: 'not-verified',
          reason: 'Автоматическая активация SCREEN при первом INPUT не подтверждена; дальнейший lifecycle проверяется после явной активации.', observation: initialFocus });
        C.ok(probe(hwnd, 'focus').focused, `SCREEN ${mode}: возможна явная активация окна ввода`);
      }
      checkFocus('first-input', `SCREEN ${mode}: запрос ввода доступен в активном нативном окне`);
      enter(hwnd, 'x');
      await S.page.waitForFunction(id => window.__ide.execution.inputRequestId && window.__ide.execution.inputRequestId !== id, first.id);
      const retry = await state();
      C.eq(retry.target, 'screen', 'Повтор после неверного числа остаётся в SCREEN');
      checkFocus('retry-input', 'Повторный ввод сохраняет активное окно');
      enter(hwnd, '42');
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId === null);
      const completed = await state();
      C.eq(completed.prompt, '', 'После нативного Enter старое приглашение удалено');
      C.eq(completed.running, true, 'Программа продолжает работу после ввода');
      await S.page.evaluate(() => window.__ide.execution.input('лишний ответ\r'));
      C.eq((await state()).running, true, 'Ввод в IDE без запроса не создаёт ложного отказа');
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
      const second = await state();
      C.ok(second.id !== retry.id, 'Следующий INPUT получил новый идентификатор');
      checkFocus('second-input', 'Следующий INPUT сохраняет фокус SCREEN');
      enter(hwnd, 'Тест');
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId === null);
      C.eq((await state()).running, true, 'Русский ответ принят без завершения программы');
      const events = await S.page.evaluate(() => window.__nativeInputEvents.filter(e => e.event === 'input.completed').map(e => e.payload.inputRequestId));
      C.ok([first.id, retry.id, second.id].every(id => events.includes(id)), 'Все три запроса закрыты событиями хоста');
      await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
      C.eq(await S.page.evaluate(() => window.__ide.execution.sessionId), null, 'Stop завершил сессию');
      // SCREEN 0 может сохранять последнее текстовое окно; при новом запуске оно сбрасывается.
      if (probe(hwnd, 'exists').exists) probe(hwnd, 'close');
      await S.page.evaluate(() => { window.__nativeInputEvents = []; });
      await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
      const cancelled = await state();
      await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
      C.eq((await state()).id, null, 'Stop во время ожидания очистил запрос');
      C.ok(await S.page.evaluate(id => window.__nativeInputEvents.some(e => e.event === 'input.completed' && e.payload.inputRequestId === id), cancelled.id),
        'Отмена ожидания также публикует завершение запроса');
    }
  },
}];
