const {test}=require('node:test');
const assert=require('node:assert/strict');
const P=require('../src/renderer/editor-preferences');
test('повреждённый профиль не передаёт служебные и неподдержанные значения Monaco',()=>{
  const input=JSON.parse('{"fontSize":-5,"tabSize":99,"stickyScroll.enabled":false,"fontFamily":"Consolas","unknown":true,"__proto__":{"polluted":true},"unicodeHighlight.allowedLocales":{"constructor":true}}');
  assert.deepEqual(P.normalize(input),{'stickyScroll.enabled':false,fontFamily:'Consolas'});
  assert.equal({}.polluted,undefined);
  assert.deepEqual(P.normalize(null),{});
});
test('сброс ранее сохранённого параметра передаёт значение по умолчанию, сохраняя базовые настройки Axiom',()=>{
  const defaults=P.defaults();
  const options=P.options({}, {'stickyScroll.enabled':false,fontSize:22}, {...defaults,'stickyScroll.enabled':true});
  assert.equal(options.stickyScroll.enabled,true);assert.equal(options.fontSize,14);assert.equal(options.acceptSuggestionOnEnter,'off');
  assert.equal(options['semanticHighlighting.enabled'],true);
});
test('JSON-поля проверяют форму, числа, карты исключений и варианты OpenType',()=>{
  const entry=k=>P.entries.find(e=>e.key===k);
  assert.ok(P.valid(entry('rulers'),[80,{column:120,color:'#fff'}]));
  assert.equal(P.valid(entry('rulers'),[{column:0}]),false);
  assert.ok(P.valid(entry('unicodeHighlight.allowedLocales'),{ru:true}));
  assert.equal(P.valid(entry('unicodeHighlight.allowedLocales'),{ru:'yes'}),false);
  assert.ok(P.valid(entry('fontLigatures'),"'liga' on"));
  assert.equal(P.valid(entry('fontLigatures'),[]),false);
  assert.ok(P.valid(entry('wordSegmenterLocales'),['ru','zh-CN']));
});
test('изменение вложенного поля сохраняет исходное значение соседних полей',()=>{
  const options=P.options({'suggest.filterGraceful':false,'unicodeHighlight.includeComments':false},{},{
    'suggest.filterGraceful':true,'suggest.snippetsPreventQuickSuggestions':false,
    'unicodeHighlight.includeComments':'inUntrustedWorkspace','unicodeHighlight.includeStrings':true});
  assert.equal(options.suggest.filterGraceful,false);
  assert.equal(options.suggest.snippetsPreventQuickSuggestions,false);
  assert.equal(options.unicodeHighlight.includeStrings,true);
});
test('устаревшие поля не записываются, действующая замена и свободный шрифт сохраняются',()=>{
  assert.deepEqual(P.normalize({matchOnWordStartOnly:true,wordBasedSuggestionsOnlySameLanguage:false,
    'suggest.matchOnWordStartOnly':false,'inlineSuggest.fontFamily':'Consolas'}),
    {'suggest.matchOnWordStartOnly':false,'inlineSuggest.fontFamily':'Consolas'});
});
