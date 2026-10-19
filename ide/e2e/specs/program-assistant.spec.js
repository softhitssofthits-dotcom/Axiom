const path=require('path');
const fs=require('fs');
const {testWorkspace,writeAxm}=require('../helpers');
const routineSource='PRINT Worker(2)\nPRINT "Doubler(999)"\n\' Worker(900)\nFUNCTION Worker(value AS INTEGER) AS INTEGER\nIF value <= 0 THEN\nRETURN 0\nEND IF\nRETURN Worker(value - 1) + Doubler(value)\nEND FUNCTION\nFUNCTION Doubler(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION\n';
async function open(S,tag,source){const file=writeAxm(testWorkspace(tag),'программа.axm',source);await S.page.evaluate(p=>window.__ide.openFile(p),file);return file;}
async function analyse(S){return S.page.evaluate(()=>window.__ide.documentServices.request(window.__ide.activeFile,'document.navigation'));}

module.exports=[{
  id:'assistant-module-navigation',name:'Межфайловые связи: снимок, переход в зависимость и возврат',
  async fn({S,C}){
    const folder=testWorkspace('assistant-modules');
    const main=writeAxm(folder,'main.axm','IMPORT alpha\nIMPORT beta\nPRINT AlphaValue() + BetaValue()');
    const alpha=writeAxm(folder,'alpha.axm','PUBLIC FUNCTION AlphaValue() AS INTEGER\nRETURN Hidden()\nEND FUNCTION\nFUNCTION Hidden() AS INTEGER\nRETURN 1\nEND FUNCTION');
    writeAxm(folder,'beta.axm','PRINT Hidden()\nPUBLIC FUNCTION BetaValue() AS INTEGER\nRETURN Hidden()\nEND FUNCTION\nFUNCTION Hidden() AS INTEGER\nRETURN 2\nEND FUNCTION');
    await S.launch({settings:{smartIdentifierEnabled:false}});
    await S.page.evaluate(p=>window.__ide.openFile(p),main);
    await S.page.evaluate(()=>window.__ide.assistant.show('outgoing',null));
    let graph=await S.page.evaluate(()=>window.__ide.assistant.graph);
    C.eq(graph.complete,true,'Все импорты связаны');
    C.eq(new Set(graph.routines.filter(r=>r.name==='Hidden').map(r=>r.id)).size,2,'Закрытые одноимённые функции различаются');
    C.contains(await S.page.locator('#assistant-body').innerText(),'alpha.axm','Файл указан в дереве');
    const moduleMain=S.page.locator('.assistant-module-main').filter({hasText:'beta.axm'});
    await moduleMain.locator('summary').first().click();
    C.contains(await moduleMain.innerText(),'Hidden','Основной код зависимости доступен отдельной ветвью');
    const branch=S.page.locator('.assistant-call-node').filter({has:S.page.locator('summary',{hasText:'AlphaValue'})}).first();
    await branch.locator('summary').click();
    await branch.locator('.assistant-call-sites > .assistant-link').first().click();
    await S.page.waitForFunction(p=>window.__ide.activeFile===p,alpha);
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),1,'Открыто объявление в закрытой зависимости');
    await S.page.evaluate(()=>{const a=window.__ide.assistant;a.travel(a.back,a.forward);});
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),main,'Возврат в исходный документ');
    await S.page.evaluate(p=>{const i=window.__ide;i.openFiles.get(p).model.setValue('PUBLIC FUNCTION AlphaValue() AS INTEGER\nRETURN 3\nEND FUNCTION');},alpha);
    await S.page.evaluate(()=>window.__ide.assistant.show('outgoing',null));
    graph=await S.page.evaluate(()=>window.__ide.assistant.graph);
    C.eq(graph.calls.filter(c=>c.sourceFilePath===alpha).length,0,'Граф использует несохранённый текст');
    await S.page.evaluate(p=>window.__ide.openFiles.get(p).model.setValue('PUBLIC FUNCTION AlphaValue() AS INTEGER\nRETURN 4\nEND FUNCTION'),alpha);
    C.eq(await S.page.evaluate(()=>window.__ide.assistant.valid()),false,'Изменение зависимости делает панель устаревшей');
    await S.page.evaluate(async()=>{
      const i=window.__ide,a=i.assistant;
      await a.show('outgoing',null);
      const target=a.graph.routines.find(r=>r.name==='BetaValue');
      const original=i.openFile.bind(i);i._restoreNavigationOpen=()=>{i.openFile=original;};
      i.openFile=async file=>{await original(file);await new Promise(resolve=>{i._releaseNavigation=resolve;});};
      i._navigationHistory=a.back.length;
      i._pendingNavigation=a.navigateSource(a.view,target);
    });
    await S.page.waitForFunction(()=>!!window.__ide._releaseNavigation);
    const race=await S.page.evaluate(async alpha=>{
      const i=window.__ide;i.switchToFile(alpha);i._releaseNavigation();await i._pendingNavigation;i._restoreNavigationOpen();
      return {file:i.activeFile,unchanged:i.assistant.back.length===i._navigationHistory};
    },alpha);
    C.eq(race.file,alpha,'Поздний переход не перехватывает выбранную третью вкладку');
    C.eq(race.unchanged,true,'Отменённый переход не изменяет историю');
    C.eq(S.pageErrors.length,0,'Нет ошибок интерфейса');
  }
},{
  id:'assistant-semantic-calls',name:'Связи компилятора: рекурсия, вложенные функции, перегрузки методов и неполный текст',
  async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    await open(S,'assistant-semantics',routineSource);
    let graph=await analyse(S);
    C.eq(graph.complete,true,'Корректный снимок связан');
    C.eq(graph.calls.length,3,'Строки и комментарии не создают вызовы');
    const worker=graph.routines.find(r=>r.name==='Worker');
    C.ok(graph.calls.some(c=>c.caller===worker.id&&c.callee===worker.id),'Рекурсия связана с собственным символом');
    C.eq(graph.calls.filter(c=>c.caller==='main').length,1,'Основная программа учтена отдельно');
    const nested='PRINT Outer() + Other()\nFUNCTION Outer() AS INTEGER\nFUNCTION Same() AS INTEGER\nRETURN 1\nEND FUNCTION\nRETURN Same()\nEND FUNCTION\nFUNCTION Other() AS INTEGER\nFUNCTION Same() AS INTEGER\nRETURN 2\nEND FUNCTION\nRETURN Same()\nEND FUNCTION\n';
    await open(S,'assistant-local',nested);graph=await analyse(S);
    C.eq(graph.complete,true,'Вложенная функция допустима');
    const outer=graph.routines.find(r=>r.name==='Outer'),same=graph.routines.filter(r=>r.name.endsWith('.Same'));
    C.eq(same.length,2,'Одинаковые имена имеют разные объявления');
    C.eq(graph.calls.find(c=>c.caller===outer.id).callee,same.find(r=>r.line===3).id,'Локальная функция разрешена в своей области');
    const overloads=fs.readFileSync(path.resolve(__dirname,'../../../samples/operators/type/method_overloads.axm'),'utf8');
    await open(S,'assistant-overloads',overloads);graph=await analyse(S);
    C.eq(graph.complete,true,'Методы TYPE связаны');
    C.eq(new Set(graph.calls.filter(c=>c.caller==='main').map(c=>c.callee)).size,3,'Три перегрузки имеют независимые цели');
    C.ok(graph.routines.filter(r=>r.name==='Counter.Set').every(r=>!r.signature.includes('SELF')),'Служебный получатель метода не выдаётся за явный параметр');
    await open(S,'assistant-case','SELECT CASE 2\nCASE ValueFn()\nPRINT LEN("a")\nEND SELECT\nFUNCTION ValueFn() AS INTEGER\nRETURN 2\nEND FUNCTION\n');
    graph=await analyse(S);
    C.ok(graph.calls.some(c=>graph.routines.find(r=>r.id===c.callee)?.name==='ValueFn'),'Обход включает выражения CASE');
    C.ok(graph.routines.some(r=>r.isBuiltin&&r.name==='LEN'),'Встроенные функции представлены отдельно');
    await open(S,'assistant-handler','ON KEY("LEFT"), HandleLeft\nSUB HandleLeft()\nPRINT 1\nEND SUB\n');
    graph=await analyse(S);
    C.eq(graph.calls.filter(c=>c.kind==='handler').length,1,'Регистрация обработчика отмечена отдельным видом связи');
    C.eq(graph.calls.filter(c=>c.kind==='call').length,0,'Регистрация не выдана за непосредственный вызов');
    await open(S,'assistant-indirect','DIM callback AS FUNCTION() AS INTEGER\ncallback = ValueFn\nPRINT callback()\nFUNCTION ValueFn() AS INTEGER\nRETURN 2\nEND FUNCTION\n');
    graph=await analyse(S);
    C.eq(graph.indirectCalls.length,1,'Косвенный вызов отмечен без выдуманного адресата');
    C.eq(graph.complete,false,'Неопределённый адресат не выдаётся за полный граф');
    C.eq(graph.calls.find(c=>graph.routines.find(r=>r.id===c.callee)?.name==='ValueFn').kind,'reference','Присваивание ссылки не выдано за непосредственный вызов');
    await open(S,'assistant-invalid','PRINT Missing()\n');graph=await analyse(S);
    C.eq(graph.complete,false,'Неисправный текст явно отмечен как неполный');
    C.eq(graph.calls.length,0,'Неизвестному имени не придуман адресат');
    C.eq(S.pageErrors.length,0,'Нет ошибок интерфейса');
  },
},{
  id:'assistant-workflow-navigation-preview',name:'Дерево вызовов, возврат с выделением и прокруткой, просмотр и устаревшие ответы',
  async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    const source=routineSource+Array.from({length:150},(_,i)=>"' Длинный листинг "+i).join('\n');
    await open(S,'assistant-workflow',source);
    await S.page.evaluate(()=>{const i=window.__ide;i.editor.setSelection(new window.monaco.Range(6,2,6,7));i.editor.setScrollTop(50);});
    const before=await S.page.evaluate(()=>({selection:window.__ide.editor.getSelection(),scroll:window.__ide.editor.getScrollTop()}));
    const worker=S.page.locator('.outline-item').filter({hasText:'Worker'}).locator('.outline-jump');
    await worker.click();
    await S.page.evaluate(()=>window.__ide.assistant.travel(window.__ide.assistant.back,window.__ide.assistant.forward));
    const after=await S.page.evaluate(()=>({selection:window.__ide.editor.getSelection(),scroll:window.__ide.editor.getScrollTop()}));
    C.eq(JSON.stringify(after),JSON.stringify(before),'Возврат сохраняет выделение и прокрутку');
    await worker.click({button:'right'});await S.page.locator('#outline-context-menu [data-action="outgoing"]').click();
    await S.page.waitForFunction(()=>window.__ide.assistant.graph?.calls.length===3);
    C.eq(await S.page.locator('#assistant-content').isVisible(),true,'Панель помощника открыта');
    C.contains(await S.page.locator('#assistant-status').innerText(),'Статические связи','Различаются связи и выполнение');
    C.eq(await S.page.locator('.assistant-call-node').count(),2,'У Worker два адресата');
    C.contains(await S.page.locator('#assistant-body').innerText(),'повторный узел','Рекурсия обозначена и не разворачивается бесконечно');
    const doubler=S.page.locator('.assistant-call-node').filter({has:S.page.locator('summary',{hasText:'Doubler'})});
    await doubler.locator('summary').first().click();
    await doubler.locator('.assistant-call-site').first().click();
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),8,'Переход к фактической строке вызова');
    await S.page.locator('#assistant-back').click();
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),6,'Возврат из дерева');
    await S.page.keyboard.press('Alt+ArrowRight');
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),8,'Alt+Right повторяет переход');
    await S.page.keyboard.press('Alt+ArrowLeft');
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),6,'Alt+Left возвращает назад');
    await worker.click({button:'right'});await S.page.locator('#outline-context-menu [data-action="incoming"]').click();
    await S.page.waitForFunction(()=>window.__ide.assistant.view.mode==='incoming'&&window.__ide.assistant.graphView===window.__ide.assistant.view);
    C.contains(await S.page.locator('#assistant-body').innerText(),'Основная программа','Входящие связи включают основной код');
    const pos=await S.page.evaluate(()=>window.__ide.editor.getPosition());
    await worker.click({button:'right'});await S.page.locator('#outline-context-menu [data-action="preview"]').click();
    C.ok(await S.page.evaluate(()=>window.__ide.assistant.previewEditor.getRawOptions().readOnly),'Просмотр только для чтения');
    C.contains(await S.page.evaluate(()=>window.__ide.assistant.previewModel.getValue()),'RETURN Worker','Просматривается тело выбранной функции');
    C.eq(JSON.stringify(await S.page.evaluate(()=>window.__ide.editor.getPosition())),JSON.stringify(pos),'Просмотр не перемещает основной курсор');
    await S.page.evaluate(()=>window.__ide.editor.executeEdits('assistant-check',[{range:new window.monaco.Range(1,1,1,1),text:"' Правка\n"}]));
    C.contains(await S.page.locator('#assistant-status').innerText(),'изменился','После правки представление отмечено устаревшим');
    C.eq(await S.page.locator('#assistant-preview').count(),0,'Старый просмотр закрыт');
    await S.page.locator('#assistant-refresh').click();
    await S.page.waitForFunction(()=>window.__ide.assistant.previewEditor!==null);
    C.contains(await S.page.evaluate(()=>window.__ide.assistant.previewModel.getValue()),'FUNCTION Worker','Обновление находит сдвинутое объявление');
    await S.page.evaluate(()=>window.__ide.setLanguage('en'));
    C.contains(await S.page.locator('#assistant-status').innerText(),'Read-only','Просмотр переводится');
    await S.page.evaluate(()=>{
      const i=window.__ide,original=i.documentServices.api;
      window.assistantOriginalApi=original;
      i.documentServices.api={...original,documentCommand:async(method,snapshot)=>{
        const result=await original.documentCommand(method,snapshot);
        if(method==='document.navigation')await new Promise(resolve=>{window.releaseAssistantResponse=resolve;});
        return result;
      }};
      window.pendingAssistant=i.assistant.show('outgoing',i.outlinePanel.items.find(item=>item.name==='Worker'));
    });
    await S.page.waitForFunction(()=>typeof window.releaseAssistantResponse==='function');
    await S.page.evaluate(async()=>{
      const i=window.__ide;i.editor.executeEdits('assistant-stale',[{range:new window.monaco.Range(1,1,1,1),text:"' Ещё правка\n"}]);
      window.releaseAssistantResponse();await window.pendingAssistant;i.documentServices.api=window.assistantOriginalApi;
    });
    C.contains(await S.page.locator('#assistant-status').innerText(),'changed','Поздний ответ не заменяет отметку устаревшего текста');
    C.eq(await S.page.locator('.assistant-call-site').count(),0,'Поздний ответ не создаёт устаревшие переходы');
    C.eq(S.pageErrors.length,0,'Рабочий процесс без ошибок страницы');
  },
},{
  id:'assistant-regions-and-problems',name:'Создание и переименование раздела с отменой; отметки проблем и фильтр',
  async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    const source='SUB First()\nPRINT 1\nEND SUB\n\nSUB Second()\nPRINT 2\nEND SUB\n';
    await open(S,'assistant-regions',source);
    await S.page.evaluate(()=>{const i=window.__ide;i.editor.setSelection(new window.monaco.Range(1,1,8,1));i.assistant.regionDialog();});
    await S.page.locator('#assistant-region-name').fill('Общий раздел');
    await S.page.locator('.assistant-dialog-save').click();
    C.eq(await S.page.locator('.outline-group-toggle').innerText(),'Общий раздел','Два объявления объединены из интерфейса');
    C.contains(await S.editorValue(),"' #endregion",'Обе границы созданы');
    await S.page.evaluate(()=>window.__ide.editor.getModel().undo());
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.regions.length===0);
    C.eq(await S.editorValue(),source,'Одна отмена возвращает исходный текст');
    await S.page.evaluate(()=>window.__ide.editor.getModel().redo());
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.regions.length===1);
    await S.page.locator('.outline-group-toggle').click({button:'right'});
    await S.page.locator('#outline-context-menu [data-action="renameRegion"]').click();
    await S.page.locator('#assistant-region-name').fill('Новое имя');await S.page.locator('.assistant-dialog-save').click();
    C.eq(await S.page.locator('.outline-group-toggle').innerText(),'Новое имя','Раздел переименован');
    await S.page.evaluate(()=>{const i=window.__ide;i.applyDiagnostics([{file:i.activeFile,line:3,col:1,length:5,code:'MB2001',severity:'error',message:'Проверочная ошибка'},{file:i.activeFile,line:7,col:1,length:5,code:'MB2046',severity:'warning',message:'Проверочное предупреждение'}],true);});
    await S.page.waitForFunction(()=>document.querySelectorAll('.outline-item .outline-problem').length===2);
    C.eq(await S.page.locator('.outline-group-heading .outline-problem').innerText(),'● 2','Группа показывает общее число проблем');
    await S.page.locator('#outline-filter').selectOption('problems');
    C.eq(await S.page.locator('.outline-item').count(),2,'Фильтр оставляет проблемные объявления');
    await S.page.locator('.outline-item .outline-problem').first().click();
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),3,'Отметка открывает проблемную строку');
    await S.page.evaluate(()=>window.__ide.editor.executeEdits('assistant-check',[{range:new window.monaco.Range(1,1,1,1),text:"' Правка\n"}]));
    await S.page.waitForFunction(()=>document.querySelectorAll('.outline-problem').length===0);
    C.eq(await S.page.locator('.outline-problem').count(),0,'Устаревшие проблемы не показываются');
    C.eq(S.pageErrors.length,0,'Создание разделов и диагностика без ошибок страницы');
  },
},{
  id:'assistant-maze-real-workflow',name:'Связи и просмотр настоящего Maze Chase во всех темах',
  async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false,sidebarWidth:320}});
    const file=path.resolve(__dirname,'../../../samples/graphics/game/maze_chase.axm');
    await S.page.evaluate(p=>window.__ide.openFile(p),file);
    const step=S.page.locator('.outline-item').filter({hasText:'StepWalker'}).locator('.outline-jump');
    await step.click({button:'right'});await S.page.locator('#outline-context-menu [data-action="incoming"]').click();
    await S.page.waitForFunction(()=>window.__ide.assistant.graphView===window.__ide.assistant.view&&window.__ide.assistant.graph);
    C.contains(await S.page.locator('#assistant-body').innerText(),'UpdateGameStep','Реальный вызывающий узел StepWalker');
    const graph=await S.page.evaluate(()=>window.__ide.assistant.graph),node=graph.routines.find(r=>r.name==='StepWalker');
    C.eq(graph.calls.filter(c=>c.callee===node.id).length,2,'Два вызова: герой и призрак');
    C.eq(graph.complete,true,'Maze Chase разобран полностью');
    for(const theme of ['dark','light','basic','borland']){
      await S.page.evaluate(t=>window.__ide.setTheme(t),theme);await S.sleep(160);
      C.eq(await S.page.locator('#assistant-content').isVisible(),true,'Помощник доступен: '+theme);
    }
    await S.screenshot('assistant-maze-borland');
    C.eq(S.pageErrors.length,0,'Maze Chase без ошибок страницы');
  },
},{
  id:'assistant-guards-and-tracked-history',name:'Защита от пересечения разделов, только чтение, актуальная диагностика и возврат после правки',
  async fn({S,C}){
    await S.launch({settings:{smartIdentifierEnabled:false}});
    const source="' #region Один\nSUB Alpha()\nPRINT 1\nEND SUB\n' #endregion\n' #region Два\nSUB Beta()\nPRINT 2\nEND SUB\n' #endregion\n";
    await open(S,'assistant-guards',source);
    await S.page.evaluate(()=>{const i=window.__ide;i.editor.setSelection(new window.monaco.Range(2,1,10,1));i.assistant.regionDialog();});
    await S.page.locator('#assistant-region-name').fill('Неверное пересечение');await S.page.locator('.assistant-dialog-save').click();
    C.contains(await S.page.locator('#assistant-region-error').innerText(),'пересекают','Пересекающиеся границы отклонены');
    C.eq(await S.editorValue(),source,'При отказе исходник сохранён');
    await S.page.locator('#assistant-region-name').press('Escape');
    await S.page.evaluate(()=>window.__ide.editor.updateOptions({readOnly:true}));
    await S.page.locator('.outline-item').filter({hasText:'Alpha'}).locator('.outline-jump').click({button:'right'});
    C.eq(await S.page.locator('#outline-context-menu [data-action="createRegion"]').isDisabled(),true,'Редактирование разделов недоступно при только чтении');
    await S.page.keyboard.press('Escape');
    await S.page.evaluate(()=>{const i=window.__ide;i.editor.updateOptions({readOnly:false});i.editor.setPosition({lineNumber:3,column:1});i.assistant.navigate(i.activeFile,8);i.editor.executeEdits('assistant-history',[{range:new window.monaco.Range(1,1,1,1),text:"' Новая строка\n"}]);i.assistant.travel(i.assistant.back,i.assistant.forward);});
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),4,'История отслеживает сдвиг строк');
    await S.page.evaluate(()=>{const i=window.__ide;i.assistant.navigate(i.activeFile,9);return i.assistant.show('preview',i.outlinePanel.items.find(item=>item.name==='Alpha'));});
    const renamed=path.join(testWorkspace('assistant-renamed'),'переименованный.axm');
    await S.stubSaveDialog(renamed);await S.page.evaluate(()=>window.__ide.saveAs());
    C.contains(await S.page.locator('#assistant-status').innerText(),'изменился','Save As делает прежний просмотр устаревшим');
    C.eq(await S.page.locator('#assistant-preview').count(),0,'После Save As нет старого просмотра');
    await S.page.evaluate(()=>{const a=window.__ide.assistant;a.travel(a.back,a.forward);});
    C.eq(await S.page.evaluate(()=>window.__ide.activeFile),renamed,'История следует за документом при Save As');
    C.eq(await S.page.evaluate(()=>window.__ide.editor.getPosition().lineNumber),4,'Возврат после Save As сохраняет позицию');
    await open(S,'assistant-actual-errors',"' #region Ошибки\nSUB Broken()\nPRINT missingValue\nEND SUB\nSUB Healthy()\nPRINT 2\nEND SUB\n' #endregion\n");
    await S.page.evaluate(()=>window.__ide.runLiveCheck());
    await S.page.waitForFunction(()=>document.querySelectorAll('.outline-item .outline-problem').length>0);
    C.contains(await S.page.locator('.outline-item').filter({has:S.page.locator('.outline-problem')}).innerText(),'Broken','Настоящая ошибка компилятора отмечает нужную подпрограмму');
    await S.page.locator('#outline-filter').selectOption('problems');
    C.eq(await S.page.locator('.outline-item').count(),1,'Настоящая диагностика ограничивает фильтр');
    C.eq(S.pageErrors.length,0,'Защитные сценарии без ошибок страницы');
  },
}];
