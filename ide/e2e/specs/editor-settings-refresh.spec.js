const entries=require('../../src/renderer/editor-settings-catalog');
const {text,configureUI,expectedCancellation}=require('../editor-settings-live.cjs');
module.exports=[{id:'editor-settings-refresh-history',name:'Применение параметров сохраняет модель, выделение, прокрутку и отмену',async fn({S,C}){
 await S.launch({settings:{smartIdentifierEnabled:false}});
 const changes={autoIndent:'none',stickyTabStops:false,wordWrapBreakBeforeCharacters:'+',wordWrapBreakAfterCharacters:'+',wrapOnEscapedLineFeeds:true,scrollBeyondLastColumn:9,scrollPredominantAxis:false,'scrollbar.alwaysConsumeMouseWheel':false,inertialScroll:true,maxTokenizationLineLength:40};
 for(const [key,value]of Object.entries(changes)){
  const source=Array.from({length:100},(_,n)=>`' строка ${n}`).join('\n');await text(S,source,{lineNumber:20,column:3});
  await S.page.keyboard.type('marker');
  const before=await S.page.evaluate(()=>{const e=window.__ide.editor;window.__refreshModel=e.getModel();e.setScrollTop(200);return{selection:e.getSelections(),top:e.getScrollTop(),text:e.getValue()};});
  await configureUI(S,entries.find(e=>e.key===key),value);
  const after=await S.page.evaluate(()=>{const e=window.__ide.editor;return{same:e.getModel()===window.__refreshModel,selection:e.getSelections(),top:e.getScrollTop(),text:e.getValue()};});
  C.ok(after.same,key+': сохранена модель');C.eq(after.text,before.text,key+': текст сохранён');C.eq(JSON.stringify(after.selection),JSON.stringify(before.selection),key+': выделение сохранено');C.eq(after.top,before.top,key+': прокрутка сохранена');
  await S.page.evaluate(()=>window.__ide.editor.focus());await S.page.keyboard.press('Control+Z');
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),source,key+': отмена пользовательского ввода');
  await S.page.keyboard.press('Control+Y');C.eq(await S.page.evaluate(()=>window.__ide.editor.getValue()),before.text,key+': повтор пользовательского ввода');
 }
 C.eq(S.pageErrors.filter(e=>!expectedCancellation(e)).length,0,'Нет неожиданных ошибок страницы');
}}];
