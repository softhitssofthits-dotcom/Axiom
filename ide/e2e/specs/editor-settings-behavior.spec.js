// Проверки видимого результата и действий редактора, дополняющие матрицу значений.
const configure=async(S,values)=>S.page.evaluate(async values=>{await window.__ide.applyEditorPreferences(values,'dark','ru');window.__ide.editor.focus();},values);
const content=async(S,text)=>S.page.evaluate(text=>{const e=window.__ide.editor;e.getModel().setValue(text);e.setPosition({lineNumber:1,column:1});e.focus();e.render(true);},text);
module.exports=[
{id:'editor-settings-rendered-font-layout',name:'Шрифты, колонки, поля, номера, миникарта и перенос действуют в представлении',async fn({S,C}){
 await S.launch({settings:{}});await content(S,'Print "'+ 'X'.repeat(160)+'"\nPrint 2');
 await configure(S,{fontFamily:'Consolas',fontSize:22,fontWeight:'bold',lineHeight:30,letterSpacing:1.5,'padding.top':24,rulers:[40,80],'minimap.enabled':true,'minimap.side':'right'});
 await S.page.waitForTimeout(250);
 const css=await S.page.locator('.monaco-editor .view-lines').evaluate(el=>{const s=getComputedStyle(el);return {font:s.fontFamily,size:s.fontSize,weight:s.fontWeight,spacing:s.letterSpacing};});
 C.contains(css.font,'Consolas','Шрифт применён к отображаемым строкам');C.eq(css.size,'22px','Размер строк 22 пикселя');C.eq(css.weight,'700','Полужирное начертание');C.eq(css.spacing,'1.5px','Межбуквенный интервал');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getTopForLineNumber(1)),24,'Верхнее поле влияет на положение первой строки');
 C.eq(await S.page.locator('.view-rulers .view-ruler').count(),2,'Две вертикальные линии колонок нарисованы');
 C.ok(await S.page.evaluate(()=>window.__ide.editor.getLayoutInfo().minimap.minimapWidth>0),'Миникарта занимает место в редакторе');
 const right=await S.page.evaluate(()=>window.__ide.editor.getLayoutInfo().minimap.minimapLeft);
 await configure(S,{'minimap.enabled':true,'minimap.side':'left',lineNumbers:'off'});
 C.ok(await S.page.evaluate(()=>window.__ide.editor.getLayoutInfo().minimap.minimapLeft)<right,'Миникарта перемещена влево');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getLayoutInfo().lineNumbersWidth),0,'Номера строк освобождают место после выключения');
 await configure(S,{'minimap.enabled':false,rulers:[],wordWrap:'wordWrapColumn',wordWrapColumn:20});await S.page.waitForTimeout(200);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getLayoutInfo().minimap.minimapWidth),0,'Выключенная миникарта не занимает ширину');
 C.eq(await S.page.locator('.view-rulers .view-ruler').count(),0,'Линии колонок удалены');
 const wrapped=await S.page.evaluate(()=>window.__ide.editor.getTopForLineNumber(2));await configure(S,{wordWrap:'off'});
 C.ok(wrapped>await S.page.evaluate(()=>window.__ide.editor.getTopForLineNumber(2)),'Перенос создаёт дополнительные визуальные строки');
 await configure(S,{cursorStyle:'line',cursorWidth:3,cursorHeight:12});await S.page.waitForTimeout(100);
 const cursor=await S.page.locator('.cursors-layer .cursor').first().boundingBox();const ratio=await S.page.evaluate(()=>devicePixelRatio);C.eq(Math.round(cursor.width*ratio),3,'Курсор имеет выбранную ширину в физических пикселях');C.eq(Math.round(cursor.height),12,'Курсор имеет выбранную высоту');
 await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));await S.page.click('#settings-tab-selection');const row=S.page.locator('[data-setting="cursorWidth"]');
 const bg=await row.evaluate(el=>getComputedStyle(el).backgroundColor);await row.hover();C.eq(await row.evaluate(el=>getComputedStyle(el).backgroundColor),bg,'Наведение не рисует полосу между подписью и полем');
}},
{id:'editor-settings-typing-actions',name:'Скобки, кавычки, обрамление, отступы, копирование и несколько курсоров',async fn({S,C}){
 await S.launch({settings:{}});
 for(const mode of ['always','never']){
  await configure(S,{autoClosingBrackets:mode,autoClosingQuotes:mode,quickSuggestionsDelay:1000});await content(S,'');await S.page.keyboard.type('(');
  C.eq(await S.editorValue(),mode==='always'?'()':'(','Автоматическая пара скобок: '+mode);
  await content(S,'');await S.page.keyboard.type('"');C.eq(await S.editorValue(),mode==='always'?'""':'"','Автоматическая пара кавычек: '+mode);
 }
 for(const mode of ['quotes','never']){
  await configure(S,{autoSurround:mode,autoClosingQuotes:'never'});await content(S,'value');await S.page.evaluate(()=>window.__ide.editor.setSelection({startLineNumber:1,startColumn:1,endLineNumber:1,endColumn:6}));await S.page.keyboard.type('"');
  C.eq(await S.editorValue(),mode==='quotes'?'"value"':'"','Обрамление выделения: '+mode);
 }
 for(const spaces of [true,false]){
  await configure(S,{insertSpaces:spaces,detectIndentation:false,tabSize:2,tabCompletion:'off',tabFocusMode:false,'quickSuggestions.other':'off'});await content(S,'');await S.page.keyboard.press('Tab');
  C.eq(await S.editorValue(),spaces?'  ':'\t','Tab вставляет выбранный вид отступа');
 }
 for(const mode of ['spread','full']){
  await configure(S,{multiCursorPaste:mode,multiCursorLimit:20});await content(S,'a\nb');
  await S.page.evaluate(()=>{const e=window.__ide.editor;e.setSelections([new monaco.Selection(1,2,1,2),new monaco.Selection(2,2,2,2)]);e.trigger('test','paste',{text:'X\nY',pasteOnNewLine:false});});
  C.eq(await S.editorValue(),mode==='spread'?'aX\nbY':'aX\nY\nbX\nY','Вставка с двумя курсорами: '+mode);
 }
}},
{id:'editor-settings-completion-kinds',name:'Категории дополнений реально фильтруют список предложений Monaco',async fn({S,C}){
 await S.launch({settings:{}});
 const kinds={Methods:'Method',Functions:'Function',Constructors:'Constructor',Fields:'Field',Variables:'Variable',Classes:'Class',Structs:'Struct',Interfaces:'Interface',Modules:'Module',Properties:'Property',Events:'Event',Operators:'Operator',Units:'Unit',Values:'Value',Constants:'Constant',Enums:'Enum',EnumMembers:'EnumMember',Keywords:'Keyword',Words:'Text',Colors:'Color',Files:'File',References:'Reference',Folders:'Folder',TypeParameters:'TypeParameter',Issues:'Issue',Users:'User',Snippets:'Snippet'};
 await S.page.evaluate(kinds=>{window.__completionFixture=monaco.languages.registerCompletionItemProvider('axiom',{provideCompletionItems(model,pos){const range={startLineNumber:pos.lineNumber,startColumn:1,endLineNumber:pos.lineNumber,endColumn:pos.column};return {suggestions:[...Object.entries(kinds).map(([name,kind])=>({label:'zz_'+name,kind:monaco.languages.CompletionItemKind[kind],insertText:'zz_'+name,range})),{label:'zz_Deprecated',kind:monaco.languages.CompletionItemKind.Function,tags:[monaco.languages.CompletionItemTag.Deprecated],insertText:'zz_Deprecated',range}]};}});},kinds);
 const base=Object.fromEntries(Object.keys(kinds).map(k=>['suggest.show'+k,false]));
 for(const name of Object.keys(kinds)){
  await configure(S,{...base,['suggest.show'+name]:true,'quickSuggestions.other':'off',snippetSuggestions:'inline',suggestSelection:'first'});await content(S,'zz_');
  await S.page.evaluate(()=>{const e=window.__ide.editor;e.setPosition({lineNumber:1,column:4});e.trigger('test','editor.action.triggerSuggest',{});});
  const item=S.page.locator('.suggest-widget.visible .monaco-list-row').filter({hasText:'zz_'+name});await item.first().waitFor({timeout:5000});
  C.ok(await item.first().isVisible(),'Категория отображается: '+name);await S.page.keyboard.press('Escape');
  await configure(S,{...base,'quickSuggestions.other':'off',snippetSuggestions:'inline'});
  await S.page.evaluate(()=>window.__ide.editor.trigger('test','editor.action.triggerSuggest',{}));await S.page.waitForTimeout(150);
  C.eq(await S.page.locator('.suggest-widget.visible .monaco-list-row').filter({hasText:'zz_'+name}).count(),0,'Категория скрывается: '+name);await S.page.keyboard.press('Escape');
 }
 for(const show of [true,false]){
  await configure(S,{...base,'suggest.showFunctions':true,'suggest.showDeprecated':show,'quickSuggestions.other':'off'});
  await S.page.evaluate(()=>window.__ide.editor.trigger('test','editor.action.triggerSuggest',{}));await S.page.waitForTimeout(150);
  C.eq(await S.page.locator('.suggest-widget.visible .monaco-list-row').filter({hasText:'zz_Deprecated'}).count()>0,show,'Устаревшие дополнения действительно фильтруются: '+show);await S.page.keyboard.press('Escape');
 }
 await S.page.evaluate(()=>window.__completionFixture.dispose());
}},
{id:'editor-settings-provider-rendering',name:'CodeLens, встроенные пояснения и продолжения проверены с временными языковыми провайдерами',async fn({S,C}){
 await S.launch({settings:{}});
 await S.page.evaluate(()=>{
  window.__fixtures=[monaco.languages.registerCodeLensProvider('axiom',{provideCodeLenses(){return {lenses:[{range:{startLineNumber:1,startColumn:1,endLineNumber:1,endColumn:4},command:{id:'fixture.command',title:'CODELENS_FIXTURE'}}],dispose(){}};}}),
   monaco.languages.registerInlayHintsProvider('axiom',{provideInlayHints(){return {hints:[{position:{lineNumber:1,column:8},label:'HINT_FIXTURE',kind:monaco.languages.InlayHintKind.Type}],dispose(){}};}}),
   monaco.languages.registerInlineCompletionsProvider('axiom',{provideInlineCompletions(model){const end=model.getLineMaxColumn(1);return {items:[{insertText:' + 12345',range:{startLineNumber:1,startColumn:end,endLineNumber:1,endColumn:end}}]};},disposeInlineCompletions(){}})];
 });
 await content(S,'Dim score As Integer = 1\nPrint score');
 await configure(S,{codeLens:true,codeLensFontFamily:'Consolas',codeLensFontSize:16,'inlayHints.enabled':'on','inlayHints.fontFamily':'Consolas','inlayHints.fontSize':15,'inlayHints.padding':true});await S.page.locator('.codelens-decoration').filter({hasText:'CODELENS_FIXTURE'}).waitFor({timeout:7000});
 C.ok(true,'CodeLens действительно появился над кодом');await S.page.waitForFunction(()=>document.querySelector('.view-lines').textContent.includes('HINT_FIXTURE'));
 C.ok(true,'Встроенное пояснение действительно появилось в строке');
 const lens=await S.page.locator('.codelens-decoration').filter({hasText:'CODELENS_FIXTURE'}).evaluate(el=>({size:getComputedStyle(el).fontSize,font:getComputedStyle(el).fontFamily}));
 C.eq(lens.size,'16px','Размер текста CodeLens применяется');C.contains(lens.font,'Consolas','Семейство шрифта CodeLens применяется');
 await configure(S,{codeLens:false,'inlayHints.enabled':'off'});await S.page.waitForTimeout(250);
 C.eq(await S.page.locator('.codelens-decoration').filter({hasText:'CODELENS_FIXTURE'}).count(),0,'CodeLens скрыт');
 C.eq(await S.page.locator('.view-lines').evaluate(el=>el.textContent.includes('HINT_FIXTURE')),false,'Встроенное пояснение скрыто');
 await content(S,'Dim score As Integer = 1');await configure(S,{'inlineSuggest.enabled':true});
 await S.page.evaluate(()=>{const e=window.__ide.editor;e.setPosition({lineNumber:1,column:e.getModel().getLineMaxColumn(1)});e.trigger('test','editor.action.inlineSuggest.trigger',{});});
 await S.page.waitForFunction(()=>document.querySelector('.view-lines').textContent.includes('12345'),null,{timeout:7000});
 C.ok(!(await S.editorValue()).includes('12345'),'Продолжение видимо, но ещё не входит в исходный текст');
 await S.page.evaluate(()=>window.__ide.editor.getAction('editor.action.inlineSuggest.commit').run());C.contains(await S.editorValue(),'12345','Принятие продолжения меняет исходный текст');
 await configure(S,{'inlineSuggest.enabled':false}); await S.page.waitForTimeout(250);
 await S.page.evaluate(()=>window.__fixtures.forEach(p=>p.dispose())); await S.page.waitForTimeout(250);
 C.eq(S.pageErrors.filter(error=>!error.startsWith('Canceled: Canceled')).length,0,'Провайдеры не оставляют ошибок, кроме отмены фонового запроса Monaco');
}},
{id:'editor-settings-diagnostic-decorations',name:'Видимые обозначения неиспользуемых и устаревших сущностей и диагностик',async fn({S,C}){
 await S.launch({settings:{}});await content(S,'Dim item As Integer = 1\nPrint item');
 await configure(S,{showUnused:true,showDeprecated:true,renderValidationDecorations:'on'});
 await S.page.evaluate(()=>monaco.editor.setModelMarkers(window.__ide.editor.getModel(),'settings-test',[
  {severity:monaco.MarkerSeverity.Hint,message:'deprecated fixture',startLineNumber:1,startColumn:5,endLineNumber:1,endColumn:9,tags:[monaco.MarkerTag.Deprecated]},
  {severity:monaco.MarkerSeverity.Hint,message:'unused fixture',startLineNumber:2,startColumn:7,endLineNumber:2,endColumn:11,tags:[monaco.MarkerTag.Unnecessary]},
  {severity:monaco.MarkerSeverity.Warning,message:'warning fixture',startLineNumber:2,startColumn:1,endLineNumber:2,endColumn:6}
 ]));await S.page.waitForTimeout(150);
 C.contains(await S.page.locator('.view-lines .squiggly-inline-deprecated').first().evaluate(el=>getComputedStyle(el).textDecorationLine),'line-through','Устаревшая сущность зачёркнута');
 C.ok(await S.page.locator('.view-lines .squiggly-inline-unnecessary').first().evaluate(el=>Number(getComputedStyle(el).opacity)<1),'Неиспользуемая сущность приглушена в тексте');
 C.ok(await S.page.locator('.monaco-editor .squiggly-warning').count()>0,'Предупреждение подчёркнуто');
 await configure(S,{showUnused:false,showDeprecated:false,renderValidationDecorations:'on'});await S.page.waitForTimeout(100);
 C.eq(await S.page.locator('.view-lines .squiggly-inline-deprecated').first().evaluate(el=>getComputedStyle(el).textDecorationLine),'none','Зачёркивание выключено');
 C.eq(await S.page.locator('.view-lines .squiggly-inline-unnecessary').first().evaluate(el=>getComputedStyle(el).opacity),'1','Приглушение неиспользуемого выключено');
 await configure(S,{renderValidationDecorations:'off'});await S.page.waitForTimeout(100);
 C.eq(await S.page.locator('.monaco-editor .squiggly-warning').count(),0,'Подчёркивание диагностики выключено');
}}];
