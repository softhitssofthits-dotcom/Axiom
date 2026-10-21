const { testWorkspace, writeAxm } = require('../helpers');
const fs = require('node:fs');
const path = require('node:path');
async function open(S, tag) {
  const directory = testWorkspace(tag);
  const first = writeAxm(directory, 'первый.axm', 'print 1\nprint 2\n');
  const other = writeAxm(directory, 'другой.axm', 'PRINT missing\n');
  await S.launch();
  await S.page.evaluate(async ({ first, other }) => {
    const i = window.__ide; i.settings.smartIdentifierEnabled = false; i.settings.formatOnBlur = false;
    await i.openFile(other); await i.openFile(first); clearTimeout(i._liveCheckTimer);
  }, { first, other });
  return { first, other };
}
async function hold(S, reject) {
  await S.page.evaluate(reject => {
    const i = window.__ide, original = i.documentServices.api;
    const state = window.__formatDelivery = { original, ready: false };
    i.documentServices.api = { ...original, documentCommand: async (...args) => {
      const response = await original.documentCommand(...args);
      if (args[0] !== 'document.format') return response;
      state.formatted = response.payload.sourceText;
      return new Promise((resolve, fail) => { state.release = () => reject ? fail(new Error('Отказ доставки форматирования')) : resolve(response); state.ready = true; });
    } };
    state.operation = i.formatWholeDocument('delivery-test');
  }, reject);
  await S.page.waitForFunction(() => window.__formatDelivery.ready);
  await S.page.evaluate(() => { window.__ide.documentServices.api = window.__formatDelivery.original; });
}
async function release(S) {
  await S.page.evaluate(async () => { window.__formatDelivery.release(); await window.__formatDelivery.operation; clearTimeout(window.__ide._liveCheckTimer); });
}
module.exports = [{
  id: 'format-delivery-stale-failures',
  name: 'Поздний отказ форматирования сохраняет исправленный, другой, закрытый и исполняемый документ',
  async fn({ S, C }) {
    for (const mode of ['edit', 'switch', 'close', 'run']) {
      const files = await open(S, 'format-failure-' + mode); await hold(S, true);
      C.contains(await S.page.evaluate(() => window.__formatDelivery.formatted), 'PRINT 1', mode + ': удержан настоящий результат форматирования');
      await S.page.evaluate(async ({ mode, first, other }) => {
        const i = window.__ide;
        if (mode === 'edit') i.editor.getModel().setValue('PRINT 42\n');
        if (mode === 'switch') i.switchToFile(other);
        if (mode === 'close') await i.closeFile(first, true);
        if (mode === 'run') await i.trace.command('stepOver');
        clearTimeout(i._liveCheckTimer);
      }, { mode, ...files });
      if (mode === 'run') await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
      await S.page.evaluate(() => {
        const i = window.__ide;
        window.__formatDelivery.before = JSON.stringify({ diagnostics: i._lastDiagnostics, text: i.editor.getValue(),
          panel: document.querySelector('.panel-tab.active')?.dataset.tab });
      });
      await release(S);
      C.eq(await S.page.evaluate(() => {
        const i = window.__ide; return JSON.stringify({ diagnostics: i._lastDiagnostics, text: i.editor.getValue(),
          panel: document.querySelector('.panel-tab.active')?.dataset.tab });
      }), await S.page.evaluate(() => window.__formatDelivery.before), mode + ': отказ не заменил интерфейс и текст');
      C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, mode + ': ожидание завершено');
      C.eq(await S.page.evaluate(file => window.axiom.readFileContent(file), files.first), 'print 1\nprint 2\n', mode + ': файл не записан');
      if (mode === 'run') { await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(); }
      await S.close();
    }
  },
}, {
  id: 'format-delivery-current-retry',
  name: 'Текущий отказ форматирования виден и не блокирует повтор',
  async fn({ S, C }) {
    await open(S, 'format-current'); await hold(S, true); await release(S);
    C.ok(await S.page.evaluate(() => window.__ide._lastDiagnostics.some(d => d.code === 'AXIOM-IDE' && d.message.includes('Отказ доставки форматирования'))), 'Причина текущего отказа видна');
    C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, 'Занятость снята');
    C.eq(await S.page.evaluate(() => window.__ide.formatWholeDocument('retry')), true, 'Повтор завершён');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), 'PRINT 1\nPRINT 2\n', 'Настоящее форматирование применено');
  },
}, {
  id: 'format-delivery-running-success',
  name: 'Поздний успешный результат не меняет уже исполняемый снимок',
  async fn({ S, C }) {
    await open(S, 'format-run-success'); await hold(S, false);
    await S.page.evaluate(() => window.__ide.trace.command('stepOver'));
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
    const snapshot = await S.page.evaluate(() => window.__ide.execution.sourceSnapshot.sourceText);
    await release(S);
    C.eq(await S.page.evaluate(() => window.__ide.editor.getValue()), snapshot, 'Текст исполняемой модели не заменён');
    C.eq(await S.page.evaluate(() => window.__ide.editor.getRawOptions().readOnly), true, 'Защита редактирования сохранена');
    C.eq(await S.page.evaluate(() => window.__ide._formatBusy), 0, 'Ожидание завершено');
    await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle();
  },
}];


for (const descriptor of [false, true]) module.exports.push({
  id: 'format-delivery-style-save-' + (descriptor ? 'axproj' : 'folder'),
  name: 'Смена стиля сохраняет чистый документ и восстанавливает прежние правки: ' + (descriptor ? 'axproj' : 'папка'),
  async fn({S,C}) {
    const root = testWorkspace('style-save-' + (descriptor ? 'axproj' : 'folder'));
    const main = writeAxm(root, 'main.axm', 'PRINT 1\n');
    const dirty = writeAxm(root, 'dirty.axm', 'PRINT 2\n');
    const project = path.join(root, 'style.axproj');
    if (descriptor) fs.writeFileSync(project, JSON.stringify({formatVersion:1,name:'Стиль',entry:'main.axm',sources:['*.axm']}));
    await S.launch({settings:{smartIdentifierEnabled:false}});
    C.eq(await S.page.evaluate(p=>window.__ide.openProject(p), descriptor ? project : root), true, 'Проект открыт');
    const draft = await S.page.evaluate(async ({main,dirty})=>{
      const i=window.__ide; await i.openFile(dirty); i.editor.setValue('PRINT 22\n');
      i.newFile(); const draft=i.activeFile; i.editor.setValue('PRINT 33\n'); i.switchToFile(main); return draft;
    }, {main,dirty});
    await S.page.locator('[data-menu="tools"] .menu-label').click();
    await S.page.locator('#formatting-submenu .submenu-trigger').hover();
    await S.page.locator('[data-action="format-modern"]').click();
    await S.page.waitForFunction(()=>window.__ide.settings.formattingStyle==='Modern'&&!window.__ide._styleSwitchPending);
    C.eq(fs.readFileSync(main,'utf8').replace(/\r/g,''),'Print 1\n','Чистая вкладка записана в новом стиле');
    C.eq(fs.readFileSync(dirty,'utf8'),'PRINT 2\n','Прежние правки не записаны');
    const state = await S.page.evaluate(paths=>paths.map(p=>{const i=window.__ide,e=i.openFiles.get(p);return {text:e.model.getValue().replace(/\r/g,''),dirty:i.openFiles.isDirty(p)};}),[main,dirty,draft]);
    C.eq(state[0].dirty,false,'Звёздочка чистого документа снята');
    C.eq(state[1].text,'Print 22\n','Несохранённый документ отформатирован'); C.eq(state[1].dirty,true,'Его звёздочка сохранена');
    C.eq(state[2].text,'Print 33\n','Черновик отформатирован'); C.eq(state[2].dirty,true,'Черновик не объявлен сохранённым');
    const profile=S.userData, pid=await S.electronApp.evaluate(()=>process.pid);
    const closed=S.electronApp.waitForEvent('close'); process.kill(pid); await closed; await S.close();
    await S.launch({userData:profile});
    const restored=await S.page.evaluate(paths=>({project:window.__ide.projectSession.descriptor?.projectFilePath||window.__ide.projectDir,entries:paths.map(p=>{const i=window.__ide,e=i.openFiles.get(p);return e&&{text:e.model.getValue().replace(/\r/g,''),dirty:i.openFiles.isDirty(p)};})}),[main,dirty,draft]);
    C.eq(restored.project,descriptor?project:root,'Проект восстановлен');
    C.eq(restored.entries[0].dirty,false,'Сохранённое форматирование осталось чистым');
    C.eq(restored.entries[1].text,'Print 22\n','Правки восстановлены после аварии'); C.eq(restored.entries[1].dirty,true,'Правки остаются несохранёнными');
    C.eq(restored.entries[2].text,'Print 33\n','Черновик восстановлен'); C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
  }
});

module.exports.push({
  id:'format-delivery-style-external-conflict',name:'Смена стиля не перезаписывает внешнее изменение после отмены конфликта',
  async fn({S,C}) {
    const root=testWorkspace('style-conflict'),file=writeAxm(root,'main.axm','PRINT 1\n');
    await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openFile(p),file);
    await S.page.evaluate(p=>window.axiom.unwatchFile(p),file);
    fs.writeFileSync(file,'PRINT 99\n');
    await S.electronApp.evaluate(({dialog})=>{dialog.showMessageBox=async()=>({response:1});});
    await S.page.evaluate(()=>window.__ide.setFormattingStyle('Modern'));
    C.eq(fs.readFileSync(file,'utf8'),'PRINT 99\n','Внешний текст не перезаписан');
    C.eq(await S.page.evaluate(p=>window.__ide.openFiles.isDirty(p),file),true,'Отмена не снимает звёздочку');
    C.eq((await S.editorValue()).replace(/\r/g,''),'Print 1\n','Форматирование осталось в буфере');
    C.eq(S.pageErrors.length,0,'Конфликт обработан без ошибки страницы');
  }
});


module.exports.push({
  id:'format-delivery-plaintext-restore',name:'Восстановленный текстовый файл не превращается в Axiom и не форматируется',
  async fn({S,C}) {
    const root=testWorkspace('plaintext-restore'),file=writeAxm(root,'заметки.txt','print 1\n');
    await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openFile(p),file);
    C.eq(await S.page.evaluate(p=>window.__ide.openFiles.get(p).model.getLanguageId(),file),'plaintext','Изначально это обычный текст');
    const profile=S.userData;await S.close();await S.launch({userData:profile});
    C.eq(await S.page.evaluate(p=>window.__ide.openFiles.get(p).model.getLanguageId(),file),'plaintext','Восстановление сохраняет вид документа');
    await S.page.evaluate(()=>window.__ide.setFormattingStyle('Modern'));
    C.eq(fs.readFileSync(file,'utf8'),'print 1\n','Смена стиля не переписывает текстовый файл');
    C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
  }
});
