// Полная матрица: взаимодействие с полями диалога и независимое наблюдение Monaco.
const fs=require('fs');const path=require('path');const assert=require('assert/strict');
const entries=require('../../src/renderer/editor-settings-catalog');
const {EVIDENCE}=require('../helpers');
const numbers={lineNumbersMinChars:7,lineDecorationsWidth:18,overviewRulerLanes:2,'padding.top':8,'padding.bottom':9,colorDecoratorsLimit:150,fontSize:20,lineHeight:28,letterSpacing:1.5,tabSize:2,quickSuggestionsDelay:125,suggestFontSize:18,suggestLineHeight:24,'hover.delay':450,'hover.hidingDelay':500,'stickyScroll.maxLineCount':3,'minimap.maxColumn':90,'minimap.scale':2,'minimap.sectionHeaderFontSize':12,'minimap.sectionHeaderLetterSpacing':2,scrollBeyondLastColumn:7,revealHorizontalRightPadding:24,mouseWheelScrollSensitivity:1.5,fastScrollSensitivity:7,'scrollbar.verticalScrollbarSize':18,'scrollbar.horizontalScrollbarSize':16,wordWrapColumn:60,cursorWidth:3,cursorHeight:12,cursorSurroundingLines:4,multiCursorLimit:20,selectionHighlightMaxLength:80,occurrencesHighlightDelay:250,foldingMaximumRegions:300,codeLensFontSize:16,'inlayHints.fontSize':15,'inlayHints.maximumLength':24,'inlineSuggest.minShowDelay':100,accessibilityPageSize:200,maxTokenizationLineLength:12000,stopRenderingLineAfter:12000};
const texts={fontFamily:'Consolas',fontWeight:'bold','minimap.markSectionHeaderRegex':'\\bMARK:\\s*(?<separator>-?)\\s*(?<label>.*)$',wordWrapBreakBeforeCharacters:'(',wordWrapBreakAfterCharacters:') ',wordSeparators:' .,;',codeLensFontFamily:'Consolas','inlayHints.fontFamily':'Consolas','inlineSuggest.fontFamily':'Consolas'};
const jsons={rulers:[80,{column:100,color:'#ff8800'}],fontLigatures:true,fontVariations:"'wght' 500",wordSegmenterLocales:['ru','en'],'unicodeHighlight.allowedCharacters':{'а':true},'unicodeHighlight.allowedLocales':{ru:true}};
const enableFor={'minimap':'minimap.enabled','stickyScroll':'stickyScroll.enabled','hover':'hover.enabled','parameterHints':'parameterHints.enabled','inlineSuggest':'inlineSuggest.enabled','inlayHints':'inlayHints.enabled','bracketPairColorization':'bracketPairColorization.enabled'};
function same(a,b){try{assert.deepEqual(a,b);return true;}catch{return false;}}
async function read(S,key,value) {
 return S.page.evaluate(({key,value})=>{
  const e=window.__ide.editor;const descriptors=Object.values(monaco.editor.EditorOptions);
  const [parent,...child]=key.split('.');const d=descriptors.find(d=>d.name===parent);
  const get=(o,parts)=>parts.reduce((v,k)=>v?.[k],o);
  const raw=key==='semanticHighlighting.enabled'?e.getRawOptions()[key]:get(e.getRawOptions(),[parent,...child]);
  if(!d){
   const effective=key==='autoDetectHighContrast'?e._standaloneThemeService?._autoDetectHighContrast:e._configurationService.getValue('editor.'+key);
   const model=['tabSize','insertSpaces','trimAutoWhitespace'].includes(key)?e.getModel().getOptions()[key]:undefined;
   return {raw,effective,expected:value,model,kind:'global'};
  }
  let input;if(child.length){input=JSON.parse(JSON.stringify(e.getRawOptions()[parent]||{}));input[child[0]]=value;}else input=value;
  const validated=d.validate(input);const expected=child.length?get(validated,child):validated;
  let effective=get(e.getOption(d.id),child);
  // lineHeight и размеры подсказок вычисляются из шрифта; положительные тестовые размеры заданы в пикселях.
  return {raw,effective,expected,kind:'editor',recognized:expected!==undefined};
 },{key,value});
}
async function setField(S,entry,value) {
 await S.page.fill('.settings-search',entry.key);
 const input=S.page.locator(`[data-setting-input="${entry.key}"]`);
 if(await input.isDisabled()) {
  const parent=enableFor[entry.key.split('.')[0]];
  if(!parent)throw new Error('Неожиданно недоступное поле '+entry.key);
  await S.page.fill('.settings-search',parent);
  const master=S.page.locator(`[data-setting-input="${parent}"]`);
  if(await master.getAttribute('type')==='checkbox')await master.check();else await master.selectOption(JSON.stringify('on'));
  await S.page.fill('.settings-search',entry.key);
 }
 if(entry.kind==='boolean') await input.setChecked(value);
 else if(entry.kind==='select') await input.selectOption(JSON.stringify(value));
 else if(['fontLigatures','fontVariations'].includes(entry.key)) {
  await input.selectOption(typeof value==='string'?'custom':JSON.stringify(value));
  if(typeof value==='string')await input.locator('..').locator('input').fill(value);
 }else await input.fill(entry.kind==='json'?JSON.stringify(value):String(value));
}
async function apply(S){await S.page.click('[data-settings-action="apply"]');await S.page.waitForFunction(()=>!window.__ide.editorSettingsDialog.busy);}
const allRows=[];
module.exports=Object.keys(Object.groupBy(entries,e=>e.group)).map(group=>({
 id:'editor-settings-all-'+group,name:'Все параметры вкладки '+group+': ввод, Monaco, профиль и перезапуск',async fn({S,C}){
  await S.launch();const baseline=await S.page.evaluate(()=>EditorPreferences.defaults(monaco));const combined={};const rows=[];
  for(const entry of entries.filter(e=>e.group===group)) {
   const row={key:entry.key,ru:entry.ru,group,cases:[],failures:[]};rows.push(row);
   try {
    if(entry.available===false){
     await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));await S.page.fill('.settings-search',entry.key);
     const input=S.page.locator(`[data-setting-input="${entry.key}"]`);
     const dropped=await S.page.evaluate(key=>!Object.hasOwn(EditorPreferences.normalize({[key]:true}),key),entry.key);
     row.availability={disabled:await input.isDisabled(),reason:await input.getAttribute('title'),rejectedByProfile:dropped};
     if(!row.availability.disabled||!row.availability.reason.includes('Устаревшее')||!dropped)row.failures.push('Устаревшее поле не защищено от бессмысленной записи');
     await S.page.keyboard.press('Escape');row.boundary='unsupported-api';
     C.ok(row.failures.length===0,entry.key+' явно недоступен',row.failures.join('\n'));continue;
    }
    row.boundary=/^(codeLens|lightbulb|inlayHints|inlineSuggest|linkedEditing|renameOnType|formatOn|gotoLocation|definitionLink|peekWidget|stablePeek|occurrencesHighlight|links)/.test(entry.key)?'native-setting-requires-language-provider':'native-setting';
    let value=entry.kind==='boolean'?!baseline[entry.key]:entry.kind==='number'?numbers[entry.key]:entry.kind==='text'?texts[entry.key]:entry.kind==='json'?jsons[entry.key]:entry.choices.find(v=>!same(v,baseline[entry.key]));
    if(value===undefined)throw new Error('Нет независимого тестового значения');
    if(entry.kind==='number'&&same(value,baseline[entry.key]))value=value===2?1:value+1;
    if(same(value,baseline[entry.key])&&entry.key==='minimap.markSectionHeaderRegex')value='\\bSECTION:\\s*(?<separator>-?)\\s*(?<label>.*)$';
    await S.page.evaluate(async()=>{const i=window.__ide;i.editorSettingsDialog?.close();await i.applyEditorPreferences({},'dark','ru');i.handleAction('editor-settings');});
    const before=await read(S,entry.key,baseline[entry.key]);
    const beforeProfile=JSON.parse(fs.readFileSync(path.join(S.userData,'settings.json'),'utf8')).editorPreferences;
    await setField(S,entry,value);await S.page.click('[data-settings-action="cancel"]');
    row.cancel={pass:same((await read(S,entry.key,baseline[entry.key])).effective,before.effective)&&same(JSON.parse(fs.readFileSync(path.join(S.userData,'settings.json'),'utf8')).editorPreferences,beforeProfile)};
    if(!row.cancel.pass)row.failures.push('Отмена изменила редактор или профиль');
    await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
    row.invalid=[];
    const bounds=await S.page.evaluate(key=>EditorPreferences.limits[key]||[0,1000000],entry.key);
    const invalid=entry.kind==='number'?[bounds[0]-1,bounds[1]+1]:entry.kind==='text'?[entry.key==='minimap.markSectionHeaderRegex'?'[':'x'.repeat(2001)]:entry.kind==='json'&&!['fontLigatures','fontVariations'].includes(entry.key)?['{broken']:[];
    for(const bad of invalid){
     await setField(S,entry,value);await S.page.locator(`[data-setting-input="${entry.key}"]`).fill(String(bad));await apply(S);
     const rejected=await S.page.locator(`[data-setting-input="${entry.key}"]`).getAttribute('aria-invalid')==='true'&&(await S.page.locator('.settings-status').innerText()).startsWith('Проверьте значение:')&&same(JSON.parse(fs.readFileSync(path.join(S.userData,'settings.json'),'utf8')).editorPreferences,beforeProfile);
     row.invalid.push({input:String(bad).slice(0,80),pass:rejected});if(!rejected)row.failures.push('Неверный ввод не отклонён');
    }
    const variants=entry.kind==='select'?entry.choices.filter(v=>!same(v,baseline[entry.key])):[value];
    for(const variant of variants) {
     await setField(S,entry,variant);await apply(S);
     const status=await S.page.locator('.settings-status').innerText();
     if(!status.includes('Настройки сохранены'))throw new Error('Применение: '+status);
     const actual=await read(S,entry.key,variant);
     const persisted=JSON.parse(fs.readFileSync(path.join(S.userData,'settings.json'),'utf8')).editorPreferences?.[entry.key];
     const pass=same(persisted,variant)&&same(actual.effective,actual.expected)&&actual.recognized!==false&&(actual.model===undefined||same(actual.model,variant));
     row.cases.push({value:variant,...actual,persisted,pass});
     if(!pass)row.failures.push('Не совпали значение поля, состояние Monaco или сохранённый профиль: '+JSON.stringify(actual));
    }
    combined[entry.key]=variants.at(-1);
    await S.page.keyboard.press('Escape');
    await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
    await S.page.fill('.settings-search',entry.key);
    const reopened=await S.page.evaluate(key=>window.__ide.editorSettingsDialog.draft[key],entry.key);
    if(!same(reopened,combined[entry.key]))row.failures.push('Повторное открытие не восстановило значение');
    row.reopen={pass:same(reopened,combined[entry.key])};
    await S.page.click('#settings-tab-'+entry.group);await S.page.click('.settings-footer button:first-child');await apply(S);
    const reset=await read(S,entry.key,baseline[entry.key]);
    row.reset={effective:reset.effective,before:before.effective,pass:same(reset.effective,before.effective)&&!Object.hasOwn(JSON.parse(fs.readFileSync(path.join(S.userData,'settings.json'),'utf8')).editorPreferences,entry.key)};
    if(!row.reset.pass)row.failures.push('Сброс не восстановил исходное действие параметра');
   }catch(error){row.failures.push(String(error.message));}
   C.ok(row.failures.length===0,entry.key,row.failures.join('\n'));
   if(rows.length%15===0)console.log(`\n  ${group}: ${rows.length} полей проверено`);
  }
  // Совместное сохранение ранее проверенных значений проверяет восстановление каждого поля при новом процессе Electron.
  await S.page.evaluate(async values=>{const i=window.__ide;i.editorSettingsDialog?.close();await i.applyEditorPreferences(values,'dark','ru');},combined);
  const profile=S.userData;await S.close();await S.launch({userData:profile});
  for(const row of rows)if(Object.hasOwn(combined,row.key)){
   const value=combined[row.key];const observed=await read(S,row.key,value);
   const pass=same(observed.effective,observed.expected)&&observed.recognized!==false;
   row.restart={...observed,value,pass};C.ok(pass,row.key+' после перезапуска',JSON.stringify(observed));
  }
  C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
  allRows.push(...rows);
  fs.writeFileSync(path.join(EVIDENCE,'editor-settings-all-fields.json'),JSON.stringify({monaco:'0.56.0',total:entries.length,checked:allRows.length,rows:allRows},null,2)+'\n','utf8');
 }
}));
