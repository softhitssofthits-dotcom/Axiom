const path = require('node:path');
const {ROOT,testWorkspace,writeAxm} = require('../helpers');

module.exports = [{
 id:'language-single-line-if',
 name:'Однострочный IF: исполнение, подсветка, форматирование, заготовка и Enter',
 async fn({S,C}) {
  await S.launch({settings:{formattingStyle:'Classic',smartIdentifierEnabled:false}});
  const file=path.join(ROOT,'samples/control-flow/if/single_line.axm');
  await S.page.evaluate(p=>window.__ide.openFile(p),file);
  const vocabulary=await S.page.evaluate(()=>['IF','THEN','ELSE','PRINT'].every(word=>window.__ide.langMeta.keywords.includes(word)));
  C.ok(vocabulary,'Все слова короткой формы присутствуют в словаре Monaco');
  const words=await S.page.evaluate(()=>{
   const text='IF TRUE THEN PRINT "yes" ELSE PRINT "no"';
   const tokens=window.monaco.editor.tokenize(text,'axiom')[0];
   return tokens.map((token,index)=>({text:text.slice(token.offset,tokens[index+1]?.offset??text.length).trim(),type:token.type}));
  });
  for(const word of ['IF','THEN','ELSE']) C.ok(words.some(t=>t.text.split(/\s+/).includes(word)&&t.type==='keyword.axiom'),word+' сохраняет категорию ключевого слова');
  const semanticWords=await S.page.evaluate(()=>{
   const text='IF TRUE THEN PRINT "yes" ELSE PRINT "no"';
   const data=window.AxiomSemanticColors.classify(text,window.monaco.editor.tokenize(text,'axiom')).data;
   let offset=0;const result=[];
   for(let n=0;n<data.length;n+=5){offset+=data[n+1];if(data[n+3]===4)result.push(text.slice(offset,offset+data[n+2]));}
   return result;
  });
  C.ok(['IF','THEN','ELSE'].every(word=>semanticWords.includes(word)),'Семантическая раскраска сохраняет категории короткой формы');
  const snippet=await S.page.evaluate(()=>{
   const i=window.__ide;
   return window.LangFeatures._completionProvider.provideCompletionItems(i.editor.getModel(),{lineNumber:1,column:1}).suggestions.find(s=>s.label==='IF — однострочный')?.insertText;
  });
  C.ok(snippet?.startsWith('IF ')&&!snippet.includes('\n')&&!snippet.includes('END IF'),'Доступна заготовка без END IF');
  await S.page.evaluate(()=>window.__ide.run());
  await S.waitConsole('Готово'); await S.waitIdle();
  const output=await S.consoleText();
  C.ok(output.includes('Ноль')&&output.includes('ELSE внутри строки')&&!output.includes('Эта ветвь не выполняется'),'Выбраны правильные ветви реального примера');
  await S.page.evaluate(p=>window.__ide.openFile(p),path.join(ROOT,'samples/control-flow/if/single_line_control.axm'));
  await S.page.evaluate(()=>window.__ide.run());
  await S.waitConsole('8'); await S.waitIdle();
  const control=await S.consoleText();
  C.ok(/1\s+3\s+7\s+8/.test(control),'RETURN и управление циклом дают ожидаемый вывод');
  await S.page.evaluate(p=>window.__ide.openFile(p),path.join(ROOT,'samples/control-flow/if/single_line_contexts.axm'));
  await S.page.evaluate(()=>window.__ide.run());
  await S.waitConsole('FINALLY');await S.waitIdle();
  C.ok(/WHILE\s+DO\s+SELECT CASE\s+CATCH\s+FINALLY/.test(await S.consoleText()),'WHILE, DO, SELECT CASE, CATCH и FINALLY работают через реальную IDE');
  for(const [style,expected] of [['Classic','IF TRUE THEN PRINT 1 ELSE PRINT 2'],['Modern','If True Then Print 1 Else Print 2']]) {
   await S.page.evaluate(style=>{const i=window.__ide;i.settings.formattingStyle=style;i.editor.getModel().setValue('if true then print 1 else print 2');},style);
   await S.page.evaluate(()=>window.__ide.formatDocument());
   C.eq((await S.editorValue()).trim(),expected,style+': форматирование сохраняет одну строку');
  }
  await S.page.evaluate(()=>{
   const i=window.__ide;i.editor.getModel().setValue('IF TRUE THEN PRINT 1');
   const original=i.editLineTransition.bind(i);
   i.editLineTransition=async(...args)=>{try{return await original(...args);}finally{window.__singleIfTransitionDone=true;}};
   i.editor.setPosition({lineNumber:1,column:21});i.editor.focus();
  });
  await S.page.keyboard.press('End');await S.page.keyboard.press('Enter');
  await S.page.waitForFunction(()=>window.__singleIfTransitionDone);
  C.eq((await S.editorValue()).split('\n').length,2,'Enter создаёт только следующую строку');
  C.ok(!(await S.editorValue()).includes('END IF'),'Структурное редактирование не добавляет END IF');
  for(const source of ['IF TRUE THEN PRINT 1 : PRINT 2','IF TRUE THEN PRINT 1 ELSE','IF TRUE THEN IF FALSE THEN PRINT 1']) {
   const diagnostics=await S.page.evaluate(async source=>{
    const i=window.__ide;i.editor.getModel().setValue(source);
    return await i.documentServices.request(i.activeFile,'document.compile',{});
   },source);
   C.ok(JSON.stringify(diagnostics).includes('MB1004'),'Неверная короткая форма получает диагностику: '+source);
  }
  C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
 }
}];

module.exports.push({
 id:'language-catch-hover-line-endings',
 name:'Подсказка переменной CATCH: объявление и использование с LF и CRLF',
 async fn({S,C}) {
  await S.launch();
  const file=writeAxm(testWorkspace('catch-hover-line-endings'),'catch.axm','PRINT 1\n');
  await S.page.evaluate(p=>window.__ide.openFile(p),file);
  for(const newline of ['\n','\r\n']) {
   const source=['TRY','    ERROR "DEMO"','CATCH problem','    PRINT problem.Message','END TRY'].join(newline);
   await S.editorType(source);
   for(const language of ['ru','en']) {
    await S.page.evaluate(language=>window.__ide.setLanguage(language),language);
    for(const position of [{lineNumber:3,column:8},{lineNumber:4,column:12}]) {
     const hover=await S.page.evaluate(async position=>{
      const model=window.__ide.editor.getModel();
      return await window.LangFeatures._hoverProvider.provideHover(model,position,{isCancellationRequested:false});
     },position);
     const text=JSON.stringify(hover);
     C.ok(text.includes('problem')&&text.includes('ERROR')&&text.includes(language==='ru'?'только для чтения':'read-only'),
      language+': переменная ошибки правильно описана на строке '+position.lineNumber+' при '+JSON.stringify(newline));
    }
   }
   await S.page.keyboard.press('Escape');
   await S.page.waitForFunction(() => ![...document.querySelectorAll('.monaco-hover')]
    .some(el => getComputedStyle(el).display !== 'none' && el.getBoundingClientRect().width > 0));
   await S.page.evaluate(()=>window.__ide.editor.revealLineInCenter(3));
   await S.page.waitForFunction(() => {
    const editor=window.__ide.editor;
    const point=editor.getScrolledVisiblePosition({lineNumber:3,column:8});
    const node=editor.getDomNode();
    if(!point || !node) return null;
    const box=node.getBoundingClientRect();
    if(!box.width || !box.height || !point.height) return null;
    return true;
   });
   const layout=await S.page.evaluate(() => {
    const editor=window.__ide.editor;
    const point=editor.getScrolledVisiblePosition({lineNumber:3,column:8});
    const box=editor.getDomNode().getBoundingClientRect();
    return {x:box.x+point.left+3,y:box.y+point.top+point.height/2};
   });
   await S.page.mouse.move(0,0);
   await S.page.mouse.move(layout.x,layout.y);
   await S.page.waitForFunction(()=>[...document.querySelectorAll('.monaco-hover')].some(el=>el.textContent.includes('problem')&&el.textContent.includes('ERROR')));
   C.ok(true,'Подсказка действительно открывается мышью при '+JSON.stringify(newline));
   await S.page.keyboard.press('Escape');
  }
  C.eq(S.pageErrors.length,0,'Подсказки не вызывают ошибок страницы');
 }
});
