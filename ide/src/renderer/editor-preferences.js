/* Только каталогизированные значения передаются Monaco и сохраняются в профиль. */
(function (root) {
  const entries = root.EditorSettingsEntries || require('./editor-settings-catalog');
  const base = {fontSize:14, fontFamily:"'Cascadia Code', Consolas, 'Courier New', monospace",
    'minimap.enabled':false, 'hover.above':false, scrollBeyondLastLine:false, renderLineHighlight:'none',
    tabSize:4, insertSpaces:true, acceptSuggestionOnEnter:'off', 'semanticHighlighting.enabled':true};
  const fallback = {autoIndent:'advanced', cursorStyle:'line', cursorBlinking:'blink', wrappingIndent:'same',
    lineNumbers:'on', 'lightbulb.enabled':'onCode', fontLigatures:false, fontVariations:false,
    detectIndentation:true, trimAutoWhitespace:true, largeFileOptimizations:true,
    wordBasedSuggestions:'offWithInlineSuggestions', wordBasedSuggestionsOnlySameLanguage:true,
    stablePeek:false, maxTokenizationLineLength:20000, autoDetectHighContrast:true, accessibilitySupport:'auto', glyphMargin:true,
    multiCursorModifier:'alt', overtypeCursorStyle:'block', 'wordSegmenterLocales':[], rulers:[]};
  const limits = {fontSize:[6,100], tabSize:[1,16], lineHeight:[0,150], letterSpacing:[-5,20],
    'stickyScroll.maxLineCount':[1,20], 'minimap.scale':[1,3], 'minimap.sectionHeaderLetterSpacing':[-5,20],
    'mouseWheelScrollSensitivity':[0.1,20], fastScrollSensitivity:[1,50], stopRenderingLineAfter:[-1,1000000],
    cursorWidth:[0,20], cursorHeight:[0,100], multiCursorLimit:[1,100000], wordWrapColumn:[1,10000],
    'scrollbar.verticalScrollbarSize':[0,100], 'scrollbar.horizontalScrollbarSize':[0,100]};
  const fractional = new Set(['lineHeight','letterSpacing','minimap.sectionHeaderLetterSpacing','mouseWheelScrollSensitivity','fastScrollSensitivity']);
  const clone = v => JSON.parse(JSON.stringify(v));
  const get = (obj,key) => key === 'semanticHighlighting.enabled' ? obj?.[key] : key.split('.').reduce((v,k)=>v?.[k],obj);
  function set(obj,key,value) {
    if(key==='semanticHighlighting.enabled') {obj[key]=clone(value);return;}
    const parts=key.split('.'); let at=obj;
    for(const part of parts.slice(0,-1)) at=at[part] ||= {};
    at[parts.at(-1)]=clone(value);
  }
  function valid(entry,value) {
    if(entry.kind==='boolean') return typeof value==='boolean';
    if(entry.kind==='select') return entry.choices.some(choice=>choice===value);
    if(entry.kind==='number') {const [min,max]=limits[entry.key]||[0,1000000];return typeof value==='number' && Number.isFinite(value) && value>=min && value<=max && (fractional.has(entry.key)||Number.isInteger(value));}
    if(entry.kind==='text') {
      if(typeof value!=='string'||value.length>2000) return false;
      if(entry.key==='fontWeight'&&!/^(normal|bold|bolder|lighter|[1-9]\d{0,2}|1000)$/.test(value)) return false;
      if(entry.key==='minimap.markSectionHeaderRegex') {try{new RegExp(value);}catch{return false;}}
      return true;
    }
    if(entry.key==='fontLigatures'||entry.key==='fontVariations') return typeof value==='boolean'||typeof value==='string'&&value.length<=2000;
    if(entry.key==='wordSegmenterLocales') return Array.isArray(value)&&value.length<=30&&value.every(v=>typeof v==='string'&&/^[a-zA-Z0-9-]{1,35}$/.test(v));
    if(entry.key==='rulers') return Array.isArray(value)&&value.length<=50&&value.every(v=>Number.isInteger(v)&&v>=1&&v<=10000 || v&&typeof v==='object'&&Number.isInteger(v.column)&&v.column>=1&&v.column<=10000&&Object.keys(v).every(k=>['column','color'].includes(k))&&(v.color===undefined||typeof v.color==='string'&&v.color.length<=50));
    return value!==null && !Array.isArray(value) && typeof value==='object' && Object.keys(value).length<=200 && Object.entries(value).every(([k,v])=>!['__proto__','prototype','constructor'].includes(k)&&k.length<=50&&typeof v==='boolean');
  }
  function defaults(monaco) {
    const options=Object.values(monaco?.editor?.EditorOptions||{}); const result={};
    for(const entry of entries) {
      const [parent,...rest]=entry.key.split('.');
      const option=options.find(o=>o.name===parent);
      if(entry.kind==='number'&&!Object.hasOwn(limits,entry.key)) {
        const schema=rest.length?option?.schema?.['editor.'+entry.key]:option?.schema;
        const min=option?.minimum??schema?.minimum??0,max=option?.maximum??schema?.maximum??1000000;
        if(min!==0||max!==1000000) limits[entry.key]=[min,Math.min(max,1000000)];
      }
      let value=rest.length ? get(option?.defaultValue,rest.join('.')) : option?.defaultValue;
      if(Object.hasOwn(fallback,entry.key)) value=fallback[entry.key];
      if(Object.hasOwn(base,entry.key)) value=base[entry.key];
      if(!valid(entry,value)) value=entry.kind==='boolean'?false:entry.kind==='select'?entry.choices[0]:entry.kind==='number'?(limits[entry.key]?.[0]??0):entry.kind==='json'?{}:'';
      result[entry.key]=clone(value);
    }
    return result;
  }
  function normalize(raw) {
    const result={}; if(!raw||typeof raw!=='object'||Array.isArray(raw)) return result;
    for(const entry of entries) if(entry.available!==false&&Object.hasOwn(raw,entry.key)&&valid(entry,raw[entry.key])) result[entry.key]=clone(raw[entry.key]);
    return result;
  }
  function overrides(values,defaultValues) {const result={};for(const [key,value] of Object.entries(normalize(values))) if(JSON.stringify(value)!==JSON.stringify(defaultValues[key])) result[key]=value;return result;}
  function options(raw,previous={},defaultValues={}) {
    const result={};
    // При частичном объекте Monaco применяет собственные запасные значения,
    // которые для некоторых соседних полей отличаются от объявленных исходных.
    // Заполняем затронутые группы полностью, чтобы изменение одного поля не меняло другие.
    const touched=new Set([...Object.keys(base),...Object.keys(normalize(raw)),...Object.keys(normalize(previous))].filter(k=>k.includes('.')&&k!=='semanticHighlighting.enabled').map(k=>k.split('.')[0]));
    for(const entry of entries)if(touched.has(entry.key.split('.')[0])&&entry.key.includes('.')&&entry.available!==false&&Object.hasOwn(defaultValues,entry.key)&&valid(entry,defaultValues[entry.key]))set(result,entry.key,defaultValues[entry.key]);
    for(const [key,value] of Object.entries(base)) set(result,key,value);
    for(const key of Object.keys(normalize(previous))) if(!Object.hasOwn(raw,key)&&Object.hasOwn(defaultValues,key)) set(result,key,defaultValues[key]);
    for(const [key,value] of Object.entries(normalize(raw))) set(result,key,value);
    return result;
  }
  const api={entries,base,limits,fractional,defaults,normalize,overrides,options,valid,clone};
  root.EditorPreferences=api;
  if(typeof module!=='undefined') module.exports=api;
})(typeof window==='undefined'?globalThis:window);
