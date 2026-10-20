const { testWorkspace, writeAxm } = require('../helpers');
const fs=require('node:fs');
const path=require('node:path');
module.exports = [{
  id: 'modules-keywords-formatting',
  name: 'Модули: ключевые слова, объявления и форматирование в редакторе',
  async fn({S,C}) {
    const file=writeAxm(testWorkspace('modules-editor'),'main.axm','import helper\npublic function Answer() as integer\nreturn 42\nend function\nprivate dim count as integer');
    await S.launch();
    await S.page.evaluate(file=>window.__ide.openFile(file),file);
    const result=await S.page.evaluate(async()=>{
      const i=window.__ide;
      const tokens=i.monaco.editor.tokenize(i.editor.getValue(),'axiom');
      const declarations=window.LangFeatures.scanDoc(i.editor.getModel());
      const formatted=await i.documentServices.request(i.activeFile,'document.format',{formattingStyle:'Classic'});
      return {tokens,declarations,formatted};
    });
    for(const line of [0,1,4]) C.ok(result.tokens[line].some(t=>t.offset===0&&t.type==='keyword.axiom'),'Модификатор или IMPORT распознан как ключевое слово');
    C.eq(result.declarations.functions[0]?.name,'Answer','Открытая функция находится в обзоре');
    C.eq(result.declarations.vars[0]?.name,'count','Закрытая переменная находится в обзоре');
    C.contains(JSON.stringify(result.formatted),'PUBLIC FUNCTION','Форматирование знает новые слова');
  }
},{
  id:'modules-import-run-refresh',
  name:'Модули: запуск импортированной функции и обновление зависимости',
  async fn({S,C}) {
    const folder=testWorkspace('modules-run');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nPRINT Answer()');
    writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 41\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(file=>window.__ide.openFile(file),file);
    await S.page.evaluate(()=>window.__ide.run());
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide.execution.outputText.includes('41'));
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'41','Выполнена импортированная функция');
    writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION');
    await S.page.evaluate(()=>window.__ide.run());
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide.execution.outputText.includes('42'));
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'42','Правка зависимости обновила программу');
  }
},{
  id:'modules-document-breakpoint',
  name:'Модули: точка останова и текущая строка в импортированном документе',
  async fn({S,C}) {
    const folder=testWorkspace('modules-debug');
    const file=writeAxm(folder,'main.axm','IMPORT HeLpEr\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async({file,helper})=>{
      const i=window.__ide; await i.openFile(helper); i.trace.toggle(2); await i.openFile(file);
      await i.run('debug',false);
    },{file,helper});
    await S.page.waitForFunction(helper=>!!window.__ide.execution.stopId&&window.__ide.activeFile===helper,helper);
    C.eq(await S.page.evaluate(()=>window.__ide.execution.line),2,'Остановка на RETURN зависимости');
    C.eq(await S.page.evaluate(()=>window.__ide.trace.marker.model===window.__ide.editor.getModel()),true,'Маркер принадлежит файлу зависимости');
    await S.page.evaluate(()=>window.__ide.execution.debug('continue'));
    await S.page.waitForFunction(()=>!window.__ide.running);
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'42','Продолжение завершило вызов');
  }
},{
  id:'modules-diagnostic-open-file',
  name:'Модули: переход по ошибке открывает зависимость и показывает диапазон',
  async fn({S,C}) {
    const folder=testWorkspace('modules-diagnostics');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nPRINT 1');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN missingValue\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(file=>window.__ide.openFile(file),file);
    await S.page.evaluate(()=>window.__ide.check(true));
    await S.page.waitForFunction(helper=>window.__ide._lastDiagnostics?.some(d=>d.file===helper),helper);
    await S.page.evaluate(async helper=>{
      const i=window.__ide, diagnostic=i._lastDiagnostics.find(d=>d.file===helper);
      await i.jumpToDiagnostic(diagnostic);
    },helper);
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),helper,'Открыт исходный файл ошибки');
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),2,'Переход на строку ошибки');
    C.ok(await S.page.evaluate(()=>window.__ide.monaco.editor.getModelMarkers({resource:window.__ide.editor.getModel().uri}).length>0),'В зависимости установлен маркер ошибки');
  }
},{
  id:'modules-unsaved-project-snapshot',
  name:'Модули: проверка, подсказка и запуск используют несохранённую зависимость',
  async fn({S,C}) {
    const folder=testWorkspace('modules-unsaved');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nPRINT Answer()');
    const saved='PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 1\nEND FUNCTION';
    const helper=writeAxm(folder,'helper.axm',saved);
    await S.launch();
    await S.page.evaluate(async({file,helper})=>{const i=window.__ide;await i.openFile(helper);await i.openFile(file);},{file,helper});
    const initial=await S.page.evaluate(async()=>JSON.stringify(await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(),{lineNumber:2,column:7})));
    C.contains(initial,'INTEGER','Исходная подсказка использует сохранённый тип');
    await S.page.evaluate(helper=>{
      const i=window.__ide;i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS STRING\nRETURN "черновик"\nEND FUNCTION');
    },helper);
    const updated=await S.page.evaluate(async()=>JSON.stringify(await window.LangFeatures._hoverProvider.provideHover(window.__ide.editor.getModel(),{lineNumber:2,column:7})));
    C.contains(updated,'STRING','Подсказка обновилась без правки основного документа');
    const result=await S.page.evaluate(()=>{const i=window.__ide;return i.documentServices.request(i.activeFile,'document.compile');});
    C.eq(result.status,'Success','Проверка использует несохранённый модуль');
    await S.page.evaluate(()=>window.__ide.run());
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide.execution.outputText.includes('черновик'));
    C.eq(fs.readFileSync(helper,'utf8'),saved,'Запуск не сохранил зависимость на диск');
    C.eq(await S.page.evaluate(helper=>window.__ide.openFiles.isDirty(helper),helper),true,'Зависимость остаётся изменённой');
  }
},{
  id:'modules-stale-project-response',
  name:'Модули: правка зависимости отменяет поздний ответ проверки',
  async fn({S,C}) {
    const folder=testWorkspace('modules-stale');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 1\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async({file,helper})=>{
      const i=window.__ide;await i.openFile(helper);await i.openFile(file);clearTimeout(i._liveCheckTimer);
      const state=window.__projectCheck={original:i.documentServices.api,release:null};
      i.documentServices.api={...state.original,documentCommand:async(...args)=>{
        const result=await state.original.documentCommand(...args);
        if(args[0]==='document.compile') return new Promise(resolve=>{state.release=()=>resolve(result);});
        return result;
      }};
      state.old=i.documentServices.request(file,'document.compile');
    },{file,helper});
    await S.page.waitForFunction(()=>!!window.__projectCheck.release);
    await S.page.evaluate(helper=>{const i=window.__ide;i.openFiles.get(helper).model.setValue('PRINT missingValue');clearTimeout(i._liveCheckTimer);},helper);
    const old=await S.page.evaluate(async()=>{const s=window.__projectCheck;s.release();const result=await s.old;window.__ide.documentServices.api=s.original;return result;});
    C.eq(old,null,'Прежняя успешная проверка отвергнута');
    const fresh=await S.page.evaluate(()=>{const i=window.__ide;return i.documentServices.request(i.activeFile,'document.compile');});
    C.eq(fresh.status,'SemanticFailure','Новый снимок проверен заново');
    C.ok(fresh.diagnostics.some(d=>d.sourceFilePath===helper&&d.code==='MB2001'),'Ошибка относится к открытой зависимости');
  }
},{
  id:'modules-runtime-snapshot-diagnostic',
  name:'Модули: ошибка запущенной версии не подчёркивает исправленную зависимость',
  async fn({S,C}) {
    const folder=testWorkspace('modules-runtime-snapshot');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nDIM value AS INTEGER\nINPUT value\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 1\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async({file,helper})=>{
      const i=window.__ide;await i.openFile(helper);
      i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS DOUBLE\nRETURN 1/0\nEND FUNCTION');
      await i.openFile(file);await i.run();
    },{file,helper});
    await S.page.waitForFunction(()=>!!window.__ide.execution.inputRequestId);
    await S.page.evaluate(async helper=>{
      const i=window.__ide;i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 2\nEND FUNCTION');
      clearTimeout(i._liveCheckTimer);await i.execution.input('1\r');
    },helper);
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide._lastDiagnostics?.some(d=>d.code==='MB3001'));
    const diagnostic=await S.page.evaluate(helper=>{
      const i=window.__ide,d=i._lastDiagnostics.find(d=>d.code==='MB3001');
      return {source:d.executionSnapshot?.sourceText,current:i.diagnosticMatchesSnapshot(d),
        markers:i.monaco.editor.getModelMarkers({owner:'axiom',resource:i.openFiles.get(helper).model.uri}).length};
    },helper);
    C.contains(diagnostic.source,'1/0','Ошибка хранит текст запущенного снимка');
    C.eq(diagnostic.current,false,'Изменённый текст не совпадает с запущенной версией');
    C.eq(diagnostic.markers,0,'В исправленном модуле нет старого подчёркивания');
  }
},{
  id:'modules-debug-immutable-snapshot',
  name:'Модули: продолжение отладки использует снимок до правки зависимости',
  async fn({S,C}) {
    const folder=testWorkspace('modules-debug-snapshot');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 1\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async({file,helper})=>{
      const i=window.__ide;await i.openFile(helper);i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 2\nEND FUNCTION');
      i.trace.toggle(2);await i.openFile(file);await i.run('debug',false);
    },{file,helper});
    await S.page.waitForFunction(helper=>!!window.__ide.execution.stopId&&window.__ide.activeFile===helper,helper);
    await S.page.evaluate(helper=>{const i=window.__ide;i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 3\nEND FUNCTION');clearTimeout(i._liveCheckTimer);},helper);
    C.eq(await S.page.evaluate(()=>window.__ide.trace.marker),null,'Правка убрала старый маркер строки');
    await S.page.evaluate(()=>window.__ide.execution.debug('continue'));
    await S.page.waitForFunction(()=>!window.__ide.running);
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'2','VM завершила исходный снимок');
  }
}];
module.exports.push({
  id:'modules-closed-runtime-snapshot',
  name:'Модули: фактический текст закрытой зависимости защищает диапазон ошибки',
  async fn({S,C}) {
    const folder=testWorkspace('modules-closed-runtime');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nDIM value AS INTEGER\nINPUT value\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS DOUBLE\nRETURN 1/0\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async file=>{const i=window.__ide;await i.openFile(file);await i.run();},file);
    await S.page.waitForFunction(()=>!!window.__ide.execution.inputRequestId);
    await S.page.evaluate(async helper=>{
      const i=window.__ide;await i.openFile(helper);i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS DOUBLE\nRETURN 2\nEND FUNCTION');
      clearTimeout(i._liveCheckTimer);await i.execution.input('1\r');
    },helper);
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide._lastDiagnostics?.some(d=>d.code==='MB3001'));
    const result=await S.page.evaluate(helper=>{
      const i=window.__ide,d=i._lastDiagnostics.find(d=>d.code==='MB3001');
      return {source:d.executionSnapshot?.sourceText,current:i.diagnosticMatchesSnapshot(d),id:d.executionSnapshot?.documentId,
        markers:i.monaco.editor.getModelMarkers({owner:'axiom',resource:i.openFiles.get(helper).model.uri}).length};
    },helper);
    C.contains(result.source,'1/0','Служба вернула действительно исполнявшийся текст');
    C.eq(result.id,null,'Закрытому документу не приписан чужой идентификатор');
    C.eq(result.current,false,'Ошибка не относится к исправленной модели');
    C.eq(result.markers,0,'Старый диапазон не подчёркивает новую модель');
  }
},{
  id:'modules-closed-debug-snapshot',
  name:'Модули: шаг в закрытую зависимость не отмечает изменённый текст',
  async fn({S,C}) {
    const folder=testWorkspace('modules-closed-debug');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nDIM value AS INTEGER\nINPUT value\nPRINT Answer()');
    const helper=writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 1\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(async file=>{
      const i=window.__ide;await i.openFile(file);i.trace.toggle(4);await i.run('debug',false);
    },file);
    await S.page.waitForFunction(()=>!!window.__ide.execution.inputRequestId);
    await S.page.evaluate(()=>window.__ide.execution.input('1\r'));
    await S.page.waitForFunction(()=>!!window.__ide.execution.stopId);
    await S.page.evaluate(async helper=>{
      const i=window.__ide;await i.openFile(helper);i.openFiles.get(helper).model.setValue('PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 3\nEND FUNCTION');
      clearTimeout(i._liveCheckTimer);await i.execution.debug('stepInto');
    },helper);
    await S.page.waitForFunction(helper=>!!window.__ide.execution.stopId&&window.__ide.execution.sourceFilePath===helper,helper);
    C.eq(await S.page.evaluate(()=>window.__ide.trace.marker),null,'Стрелка не перенесена на новую версию файла');
    for(let attempt=0;attempt<3;attempt++) {
      if(!await S.page.evaluate(()=>window.__ide.running)) break;
      await S.page.evaluate(()=>window.__ide.execution.debug('continue'));
      await S.page.waitForFunction(()=>!window.__ide.running||!!window.__ide.execution.stopId);
    }
    await S.page.waitForFunction(()=>!window.__ide.running);
    C.contains(await S.page.evaluate(()=>window.__ide.execution.outputText),'1','Выполнение сохранило исходный снимок');
  }
});

module.exports.push({
  id:'modules-private-type-identities',
  name:'Модули: независимые закрытые TYPE в рабочем примере',
  async fn({S,C}) {
    const file=path.resolve(__dirname,'../../../samples/operators/import/private_types_same_name/main.axm');
    await S.launch();
    await S.page.evaluate(async file=>{await window.__ide.openFile(file);await window.__ide.run();},file);
    await S.page.waitForFunction(()=>!window.__ide.running&&window.__ide.execution.outputText.includes('hello'));
    const output=await S.page.evaluate(()=>window.__ide.execution.outputText);
    C.contains(output,'11','Числовая запись первого модуля сохраняет значение');
    C.contains(output,'hello','Строковая запись второго модуля сохраняет значение');
  }
},{
  id:'modules-hover-with-entry-error',
  name:'Модули: подсказка импорта сохраняется при ошибке точки входа',
  async fn({S,C}) {
    const folder=testWorkspace('modules-hover-error');
    const file=writeAxm(folder,'main.axm','IMPORT helper\nDIM localCount AS INTEGER\nPRINT missingValue\nPRINT Answer()');
    writeAxm(folder,'helper.axm','PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION');
    await S.launch();
    await S.page.evaluate(file=>window.__ide.openFile(file),file);
    const response=await S.page.evaluate(()=>window.__ide.documentServices.request(window.__ide.activeFile,'document.hover'));
    C.ok(response.hovers.some(h=>h.name==='Answer'&&h.kind==='function'),'Подсказка открытой функции доступна при ошибке main');
    C.ok(response.hovers.some(h=>h.name==='localCount'&&h.kind==='variable'),'Локальные подсказки сохранены');
  }
});
