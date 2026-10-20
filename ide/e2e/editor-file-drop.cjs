const { testWorkspace, writeAxm } = require('./helpers');

// Chromium создаёт trusted DragEvent с теми же форматами, что у внешнего источника.
async function dispatchDrop(S, data) {
  // Поиск строки и её координат выполняется вместе: Monaco может заменить узел при отрисовке.
  const position = await S.page.waitForFunction(() => {
    const box = document.querySelector('.view-line')?.getBoundingClientRect();
    return box && box.width > 0 && box.height > 0 ? { x: box.x + 80, y: box.y + 8 } : false;
  });
  const point = await position.jsonValue();
  await position.dispose();
  const cdp = await S.page.context().newCDPSession(S.page);
  try {
    for (const type of ['dragEnter', 'dragOver', 'drop']) {
      await cdp.send('Input.dispatchDragEvent', { type, ...point, data });
    }
  } finally { await cdp.detach(); }
}

async function checkFileDrop(S, C, entry, configureUI) {
  const root = testWorkspace('editor-file-drop');
  const first = writeAxm(root, 'Первый файл.axm', 'PRINT 123');
  const second = writeAxm(root, 'Второй файл.axm', 'PRINT 456');
  const source = await S.page.evaluate(() => { window.__ide.newFile(); return window.__ide.activeFile; });
  await S.editorType("' unsaved original");
  const url = S.page.url();
  const mainId = await (await S.electronApp.browserWindow(S.page)).evaluate(w => w.id);
  await S.page.evaluate(() => {
    window.__fileDrops = [];
    for (const type of ['dragenter', 'dragover', 'drop']) document.addEventListener(type, event => window.__fileDrops.push({
      type: event.type, trusted: event.isTrusted, types: Array.from(event.dataTransfer.types),
      files: Array.from(event.dataTransfer.files, file => file.name),
      prevented: event.defaultPrevented,
    }), { capture: true });
  });
  for (const enabled of [false, true]) {
    await configureUI(S, entry, enabled);
    await S.page.evaluate(p => window.__ide.switchToFile(p), source);
    await dispatchDrop(S, { items: [], files: [first, second], dragOperationsMask: 1 });
    if (enabled) {
      try { await S.page.waitForFunction(p => window.__ide.activeFile === p, second, { timeout: 3000 }); }
      catch { C.note('Файлы не открылись после файлового Drop'); }
    }
    const state = await S.page.evaluate(({ first, second, source }) => {
      const i = window.__ide;
      return { active: i.activeFile, first: i.openFiles.get(first)?.model.getValue(),
        second: i.openFiles.get(second)?.model.getValue(),
        original: i.openFiles.get(source)?.model.getValue(), dirty: i.openFiles.isDirty(source),
        event: window.__fileDrops.at(-1) };
    }, { first, second, source });
    C.eq(state.event?.trusted, true, 'Файловое перетаскивание доставлено Chromium');
    C.ok(state.event?.types.includes('Files'), 'Передан формат Files, как из Проводника');
    C.eq(state.event?.prevented, true, 'Браузерное открытие файла отменено');
    C.eq(state.event?.type === 'drop', enabled, 'Drop принимается только при включённой настройке');
    C.eq(S.page.url(), url, 'Главное окно осталось страницей IDE');
    const popups = await S.electronApp.evaluate(({ BrowserWindow }, mainId) => BrowserWindow.getAllWindows()
      .filter(w => w.id !== mainId).map(w => w.webContents.getURL()), mainId);
    C.eq(popups.length, 0, 'Файл не открыт в дополнительном окне Electron');
    C.eq(state.original, "' unsaved original", 'Несохранённый текст прежнего документа сохранён');
    C.eq(state.dirty, true, 'Прежний документ остаётся изменённым');
    if (enabled) {
      C.eq(state.first, 'PRINT 123', 'Первый файл загружен в документ');
      C.eq(state.second, 'PRINT 456', 'Второй файл загружен в документ');
      C.eq(state.active, second, 'Последний файл выбран в редакторе');
      if (state.active === second) {
        await S.editorType("' modified drop");
        const count = await S.page.evaluate(() => window.__ide.openFiles.size);
        await dispatchDrop(S, { items: [], files: [second], dragOperationsMask: 1 });
        C.eq(await S.editorValue(), "' modified drop", 'Повторное перетаскивание сохраняет правки открытого файла');
        C.eq(await S.page.evaluate(() => window.__ide.openFiles.size), count, 'Повтор не создаёт лишнюю вкладку');
      }
    } else {
      C.eq(state.first, undefined, 'Выключенная настройка не открывает первый файл');
      C.eq(state.second, undefined, 'Выключенная настройка не открывает второй файл');
      C.eq(state.active, source, 'Выключенная настройка сохраняет активный документ');
    }
    // До исправления Chromium создаёт окна; они принадлежат только этому сценарию.
    await S.electronApp.evaluate(({ BrowserWindow }, mainId) => {
      for (const w of BrowserWindow.getAllWindows()) if (w.id !== mainId) w.destroy();
    }, mainId);
  }
}

module.exports = { dispatchDrop, checkFileDrop };
