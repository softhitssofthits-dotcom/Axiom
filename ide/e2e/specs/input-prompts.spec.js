const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [{
  id: 'input-sequential-prompts', name: 'Два последовательных INPUT: приглашение и введённое имя не повторяются',
  async fn({ S, C }) {
    // Копия пользовательского примера: исходный файл не изменяется.
    const source = 'DIM name AS STRING, age AS INTEGER\nDIM BirthDay AS INTEGER\n\nINPUT "Введите ваше имя: ", name\nPRINT\nINPUT "Введите ваш возраст", age\n\nBirthDay = 2026 - age\nPRINT $"привет {name}, ты родился в {BirthDay} году."\n';
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
  }
}, {
  id: 'input-snapshot-stream-order', name: 'Снимок INPUT и запоздалый поток вывода: согласование и сохранность набора',
  async fn({ S, C }) {
    await S.launch();
    const state = await S.page.evaluate(() => {
      // Отдельный контроллер проверяет порядок событий без запуска второй VM.
      let rendered = '';
      const ide = { term: { reset() { rendered = ''; }, write(text) { rendered += text; }, focus() {} }, trace: { update() {} } };
      const api = { onBackendEvent() {}, onBackendFault() {}, acknowledgeOutput() {} };
      const controller = new window.ExecutionController(ide, api);
      controller.sessionId = 'проверка';
      const event = (name, payload) => controller.event({ sessionId: 'проверка', event: name, payload });
      event('input.request', { inputRequestId: '2', prompt: 'Возраст? ', outputText: 'Имя: Анна\n' });
      controller.inputText = '16';
      event('output.append', { outputVersion: '1', text: 'Имя: Анна\n' });
      const first = { output: controller.outputText, rendered };
      event('output.append', { outputVersion: '2', text: '\n' });
      const second = rendered;
      event('output.replace', { outputVersion: '3', text: '' });
      const replaced = rendered;
      return { first, second, replaced };
    });
    C.eq(state.first.output, 'Имя: Анна\n', 'append не добавляет уже включённый в INPUT снимок второй раз');
    C.eq(state.first.rendered, 'Имя: Анна\r\nВозраст? 16', 'Текущие приглашение и набранные цифры сохраняются после append');
    C.eq(state.second, 'Имя: Анна\r\n\r\nВозраст? 16', 'Последующий append продолжает опубликованный поток');
    C.eq(state.replaced, 'Возраст? 16', 'Замена вывода очищает историю, сохраняя текущий ввод');
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
