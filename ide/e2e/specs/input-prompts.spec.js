const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'input-sequential-prompts', name: 'Два последовательных INPUT: приглашение и введённое имя не повторяются',
  async fn({ S, C }) {
    // Копия пользовательского примера: исходный файл не изменяется.
    const source = 'DIM name AS STRING, age AS INTEGER\nDIM BirthDay AS INTEGER\n\nINPUT "Введите ваше имя: ", name\n\nINPUT "Введите ваш возраст"; age\n\nBirthDay = 2026 - age\nPRINT $"привет {name}, ты родился в {BirthDay} году."\n';
    const file = writeAxm(testWorkspace('input-prompts'), 'name-age.axm', source);
    await S.launch(); await S.stubOpenDialog([file]);
    await S.page.evaluate(() => window.__ide.handleAction('open'));
    await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
    await S.page.evaluate(() => {
      window.__inputEvents = [];
      const controller = window.__ide.execution;
      const original = controller.event.bind(controller);
      controller.event = message => { window.__inputEvents.push(message); return original(message); };
    });
    await S.page.evaluate(() => window.__ide.run());
    await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
    await S.waitConsole('Введите ваше имя:');
    const firstId = await S.page.evaluate(() => window.__ide.execution.inputRequestId);
    await S.typeConsole('Анна');
    await S.page.waitForFunction(id => window.__ide.execution.inputRequestId !== null && window.__ide.execution.inputRequestId !== id, firstId);
    await S.page.waitForTimeout(250);
    const waiting = await S.consoleText();
    C.contains(waiting, 'Введите ваше имя: Анна\nВведите ваш возраст?', 'Второй INPUT начинается новой строкой до ввода возраста');
    C.eq((waiting.match(/Введите ваше имя:/g) || []).length, 1, 'Первый запрос показан ровно один раз после ввода имени');
    C.eq((waiting.match(/Анна/g) || []).length, 1, 'Введённое имя показано один раз');
    C.eq((waiting.match(/Введите ваш возраст/g) || []).length, 1, 'Второй запрос остаётся виден после обновления вывода');
    await S.page.keyboard.type('16', { delay: 25 });
    await S.page.waitForTimeout(250);
    C.contains(await S.consoleText(), 'Введите ваш возраст? 16', 'Приглашение возраста и набранные цифры видны до Enter');
    await S.page.keyboard.press('Enter'); await S.waitIdle(); await S.waitConsole('привет Анна, ты родился в 2010 году.');
    await S.page.waitForTimeout(150);
    const finished = await S.consoleText();
    C.eq((finished.match(/Введите ваше имя:/g) || []).length, 1, 'Первый запрос не повторяется в завершённом выводе');
    C.eq((finished.match(/Введите ваш возраст/g) || []).length, 1, 'Второй запрос не повторяется в завершённом выводе');
    C.eq((await S.diagnosticItems()).length, 0, 'Оба значения приняты без ошибок');
    C.eq(await S.page.evaluate(() => window.__inputEvents.filter(e => e.event === 'input.request').length), 2, 'VM запросила только два ввода, без повторного первого INPUT');
    C.note(`Консоль во время второго INPUT: ${waiting}`);
    for (let run = 0; run < 5; run++) {
      await S.page.evaluate(() => window.__ide.run());
      await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
      const id = await S.page.evaluate(() => window.__ide.execution.inputRequestId);
      await S.waitConsole('Введите ваше имя:'); await S.page.evaluate(() => window.__ide.term.focus()); await S.page.keyboard.type('uiuiu', { delay: 1 }); await S.page.keyboard.press('Enter');
      await S.page.waitForFunction(previous => window.__ide.execution.inputRequestId !== null && window.__ide.execution.inputRequestId !== previous, id);
      await S.page.waitForTimeout(100);
      const actual = await S.consoleText();
      C.contains(actual, 'Введите ваше имя: uiuiu\nВведите ваш возраст?', `Быстрый ввод, запуск ${run + 1}: новая строка`);
      C.eq((actual.match(/Введите ваше имя:/g) || []).length, 1, `Быстрый ввод, запуск ${run + 1}: нет дубля`);
      if (run === 4) await S.screenshot('input-fast-sequential-fixed');
      await S.page.keyboard.type('47', { delay: 1 }); await S.page.keyboard.press('Enter');
      await S.waitIdle(); await S.waitConsole('привет uiuiu, ты родился в 1979 году.');
    }
  }
}, {
  id: 'input-snapshot-stream-order', name: 'Снимок INPUT и запоздалый поток вывода: согласование и сохранность набора',
  async fn({ S, C }) {
    await S.launch();
    const state = await S.page.evaluate(() => {
      // Отдельный контроллер проверяет порядок событий без запуска второй VM.
      let rendered = '';
      const ide = { term: { reset() { rendered = ''; }, write(text) {
        if (text.startsWith('\x1bc')) { rendered = ''; text = text.slice(2); }
        rendered += text;
      }, focus() {} }, trace: { update() {} } };
      const api = { onBackendEvent() {}, onBackendFault() {}, acknowledgeOutput() {} };
      const controller = new window.ExecutionController(ide, api);
      controller.sessionId = 'проверка';
      const event = (name, payload) => controller.event({ sessionId: 'проверка', event: name, payload });
      event('input.request', { inputRequestId: '2', prompt: 'Возраст? ', outputText: 'Имя: Анна', outputLineCount: 1 });
      controller.inputText = '16';
      event('output.append', { outputVersion: '1', text: 'Имя: Анна', outputLineCount: 1 });
      const first = { output: controller.outputText, rendered };
      event('output.append', { outputVersion: '2', text: '\n', outputLineCount: 2 });
      const second = rendered;
      event('output.replace', { outputVersion: '3', text: '', outputLineCount: 0 });
      const replaced = rendered;
      return { first, second, replaced };
    });
    C.eq(state.first.output, 'Имя: Анна', 'append не добавляет уже включённый в INPUT снимок второй раз');
    C.eq(state.first.rendered, 'Имя: Анна\r\nВозраст? 16', 'Текущие приглашение и набранные цифры сохраняются после append');
    C.eq(state.second, 'Имя: Анна\r\n\r\nВозраст? 16', 'Последующий append продолжает опубликованный поток');
    C.eq(state.replaced, 'Возраст? 16', 'Замена вывода очищает историю, сохраняя текущий ввод');
    await S.page.evaluate(async () => {
      const ide = window.__ide;
      ide.showPanel(); ide.switchPanel('console'); ide.ensureConsole();
      const controller = new window.ExecutionController({ term: ide.term }, {
        onBackendEvent() {}, onBackendFault() {},
      });
      controller.inputRequestId = '1'; controller.inputPrompt = 'Введите ваше имя: '; controller.inputText = 'uiuiu';
      controller.renderOutput();
      controller.outputText = 'Введите ваше имя: uiuiu';
      controller.inputRequestId = '2'; controller.inputPrompt = 'Введите ваш возраст? '; controller.inputText = '';
      controller.renderOutput();
      await new Promise(resolve => ide.term.write('', resolve));
    });
    const queued = await S.consoleText();
    C.eq((queued.match(/Введите ваше имя:/g) || []).length, 1, 'Два снимка подряд в настоящем xterm не дублируют первый INPUT');
    C.contains(queued, 'Введите ваше имя: uiuiu\nВведите ваш возраст?', 'Очередь xterm сохраняет разделение запросов');
  }
}];
module.exports.push({
  id:'input-name-age-sample', name:'Учебный пример имени и возраста запускается в Electron',
  async fn({S,C}) {
    const path = require('node:path').resolve(__dirname,'../../../samples/operators/input/name_age_transcript.axm');
    await S.launch(); await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); },path);
    await S.page.waitForFunction(() => window.__ide.execution.inputRequestId !== null);
    const first = await S.page.evaluate(() => window.__ide.execution.inputRequestId);
    await S.typeConsole('Анна');
    await S.page.waitForFunction(id => window.__ide.execution.inputRequestId !== null && window.__ide.execution.inputRequestId !== id,first);
    await S.typeConsole('16'); await S.waitIdle(); await S.waitConsole('Привет, Анна! Приблизительный год рождения: 2010.');
    const text = await S.consoleText();
    C.eq((text.match(/Введите ваше имя:/g)||[]).length,1,'Вопрос имени показан один раз');
    C.eq((text.match(/Введите ваш возраст/g)||[]).length,1,'Вопрос возраста показан один раз');
    C.eq((await S.diagnosticItems()).length,0,'Учебный файл завершился без диагностики');
  }
});

module.exports.push({
  id:'input-live-check-keeps-terminal-focus',
  name:'Фоновая диагностика во время INPUT не переводит фокус из консоли в редактор',
  async fn({S,C}) {
    const file = writeAxm(testWorkspace('input-live-focus'),'ввод.axm','DIM value AS STRING\nINPUT "Ответ", value\nPRINT value\n');
    await S.launch(); await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); },file);
    await S.waitStatus('Ожидание ввода');
    const focus = await S.page.evaluate(async () => {
      const i=window.__ide; i.term.focus(); await i.runLiveCheck();
      return document.activeElement === i.term.textarea;
    });
    C.eq(focus,true,'Применение диагностики сохраняет фокус активного INPUT');
    await S.page.keyboard.type('Анна'); await S.page.keyboard.press('Enter');
    if (focus) { await S.waitIdle(); await S.waitConsole('Анна'); C.ok(true,'Весь ответ и Enter получены программой'); }
  },
});

module.exports.push({
  id: 'input-prompt-separators', name: 'INPUT: разделители приглашения, пустая строка и пунктуация',
  async fn({ S, C }) {
    const prompts = ['Текст', 'Текст? ', ':? ', '', '? ', '? '];
    const source = 'DIM value AS STRING\nINPUT "Текст", value\nINPUT "Текст"; value\nINPUT ":"; value\nINPUT "", value\nINPUT ""; value\nINPUT value\nPRINT "Готово: "; value\n';
    const file = writeAxm(testWorkspace('input-separators'), 'prompt-separators.axm', source);
    await S.launch(); await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.run(); }, file);
    let previous = null;
    for (const prompt of prompts) {
      await S.page.waitForFunction(id => window.__ide.execution.inputRequestId !== null && window.__ide.execution.inputRequestId !== id, previous);
      const state = await S.page.evaluate(() => ({ id: window.__ide.execution.inputRequestId, prompt: window.__ide.execution.inputPrompt }));
      C.eq(state.prompt, prompt, 'Приглашение соответствует разделителю и сохраняет буквальный текст');
      previous = state.id;
      await S.typeConsole('ответ');
    }
    await S.waitIdle(); await S.waitConsole('Готово');
    C.eq((await S.diagnosticItems()).length, 0, 'Ввод всех форм завершён без диагностики');
  }
});
