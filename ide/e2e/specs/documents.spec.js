const {testWorkspace,writeAxm}=require('../helpers');
async function document(S,source,tag){const file=writeAxm(testWorkspace(tag),'документ.axm',source);await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);await S.page.evaluate(()=>{window.__ide.settings.formatOnBlur=false;window.__ide.settings.checkOnChange=false;});return file;}
module.exports=[
{id:'document-host-format-undo',name:'Форматирование несохранённого документа через Host и отмена',async fn({S,C}){
await document(S,'PRINT 1\n','document-format');await S.page.evaluate(()=>{const i=window.__ide;i.editor.getModel().setValue('print 42\n');i.editor.setPosition({lineNumber:1,column:6});});
await S.page.evaluate(()=>window.__ide.formatWholeDocument('test-format'));
C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'PRINT 1\n','Форматирование не записало файл');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue().trim()),'PRINT 42','Общий formatter');
await S.page.evaluate(()=>window.__ide.editor.trigger('test','undo',null));C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'print 42\n','Отмена возвращает исходный несохранённый текст');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().column),6,'Каретка сохранена');
await S.page.evaluate(()=>window.__ide.save());C.eq(await S.page.evaluate(()=>window.axiom.readFileContent(window.__ide.activeFile)),'print 42\n','Явное сохранение пишет правильный текст');
}},
{id:'document-host-structural-enter',name:'Общий планировщик вставляет блок и сохраняет отмену',async fn({S,C}){
await document(S,'IF a = 0 THEN','document-block');await S.page.evaluate(()=>{const i=window.__ide;i.editor.setPosition({lineNumber:1,column:14});i.editor.focus();});const eol=await S.page.evaluate(()=>window.__ide.editor.getModel().getEOL());await S.page.keyboard.press('Enter');
await S.page.waitForFunction(()=>window.__ide.editor.getValue().includes('END IF'));
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),['IF a = 0 THEN','    ','END IF'].join(eol),'Блок совпадает с общими правилами');
C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().column),5,'Каретка в теле блока');
await S.page.evaluate(()=>window.__ide.editor.trigger('test','undo',null));C.ok(!(await S.page.evaluate(()=>window.__ide.editor.getValue())).includes('END IF'),'Структурная правка отменяется');
}},
{id:'document-host-stale-format',name:'Запоздавший результат не заменяет новую правку Monaco',async fn({S,C}){
await document(S,'print 1\n','document-stale');await S.page.evaluate(()=>{const i=window.__ide,real=i.documentServices.api;i.documentServices.api={documentCommand:async(...args)=>{const reply=await real.documentCommand(...args);return new Promise(resolve=>window.__releaseDocument=()=>resolve(reply));}};window.__pendingFormat=i.formatWholeDocument('test-stale');});
await S.page.waitForFunction(()=>typeof window.__releaseDocument==='function');await S.page.evaluate(()=>{window.__ide.editor.getModel().setValue('PRINT 99\n');window.__releaseDocument();return window.__pendingFormat;});
C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),'PRINT 99\n','Новый текст сохранён');
}}
,{id:'document-host-stale-analysis',name:'Запоздавшая диагностика не возвращает исправленную ошибку',async fn({S,C}){
await document(S,'PRINT missing\n','document-analysis');await S.page.evaluate(()=>{const i=window.__ide,real=i.documentServices.api;let count=0;i.settings.checkOnChange=true;clearTimeout(i._liveCheckTimer);i.documentServices.api={documentCommand:async(...args)=>{const reply=await real.documentCommand(...args);if(++count!==1)return reply;window.__heldCodes=reply.payload.diagnostics.map(d=>d.code);return new Promise(resolve=>window.__releaseAnalysis=()=>resolve(reply));}};window.__oldAnalysis=i.runLiveCheck();});
await S.page.waitForFunction(()=>typeof window.__releaseAnalysis==='function');C.ok((await S.page.evaluate(()=>window.__heldCodes)).includes('MB2001'),'Старый ответ содержит настоящую ошибку');
await S.page.evaluate(async()=>{const i=window.__ide;i.editor.getModel().setValue('PRINT 1\n');clearTimeout(i._liveCheckTimer);await i.runLiveCheck();window.__releaseAnalysis();await window.__oldAnalysis;});
C.eq(await S.page.evaluate(()=>window.__ide._lastDiagnostics.length),0,'Устаревшая ошибка не вернулась в список');
C.eq(await S.page.evaluate(()=>window.__ide.monaco.editor.getModelMarkers({owner:'axiom',resource:window.__ide.editor.getModel().uri}).length),0,'Редактор не получил устаревшие маркеры');
}}
];
