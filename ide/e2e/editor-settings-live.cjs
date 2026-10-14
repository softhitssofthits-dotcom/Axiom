const fs=require('fs'),path=require('path'),crypto=require('crypto');
const entries=require('../src/renderer/editor-settings-catalog');
const plans=require('./editor-settings-live-plan.cjs');
const {Session,EVIDENCE,ROOT}=require('./helpers');
const shotRoot=path.join(ROOT,'.artifacts','editor-settings','live-shots');fs.mkdirSync(shotRoot,{recursive:true});
const fixtureRoot=path.join(ROOT,'.artifacts','editor-settings','live-fixtures');fs.mkdirSync(fixtureRoot,{recursive:true});
const prerequisites=(S,patch)=>S.page.evaluate(patch=>{const i=window.__ide;return i.applyEditorPreferences({...i.settings.editorPreferences,...patch},'dark','ru');},patch);
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
async function nativeWheel(S){
 const {execFile}=require('child_process'),window=await S.electronApp.browserWindow(S.page);
 const handle=await window.evaluate(w=>w.getNativeWindowHandle().readBigUInt64LE().toString()),pid=await S.electronApp.evaluate(()=>process.pid);
 const b=await S.page.locator('.view-line').first().boundingBox();
 await new Promise((resolve,reject)=>execFile('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(__dirname,'native-wheel.ps1'),'-WindowHandle',handle,'-TargetProcessId',String(pid),'-X',String(Math.round(b.x+100)),'-Y',String(Math.round(b.y+8)),'-Delta','-25','-Count','20'],{windowsHide:true},(err,out,stderr)=>err?reject(Error(stderr)):resolve()));
}
async function nativeDrop(S,file){
 const {spawn}=require('child_process');const window=await S.electronApp.browserWindow(S.page);
 const handle=await window.evaluate(w=>w.getNativeWindowHandle().readBigUInt64LE().toString());
 const html=path.join(fixtureRoot,'drag-source.html');fs.writeFileSync(html,'<!doctype html><html><body><a draggable="true" style="display:block;padding:20px" href="'+require('url').pathToFileURL(file).href+'">Axiom — настоящий файл</a></body></html>');
 const target=await S.page.locator('.view-line').first().boundingBox();
 const source=await S.electronApp.evaluate(async({BrowserWindow},html)=>{const w=new BrowserWindow({x:100,y:100,width:280,height:150,autoHideMenuBar:true});w.removeMenu();await w.loadFile(html);global.__dragSource=w;return{handle:w.getNativeWindowHandle().readBigUInt64LE().toString(),pid:process.pid};},html);
 try{
 const child=spawn('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(__dirname,'native-drag.ps1'),'-WindowHandle',handle,'-TargetProcessId',String(source.pid),'-X',String(Math.round(target.x+80)),'-Y',String(Math.round(target.y+8)),'-SourceHandle',source.handle,'-SourceX','80','-SourceY','30'],{windowsHide:true,stdio:['ignore','pipe','pipe']});
 let err='';child.stderr.on('data',b=>err+=b);const code=await new Promise(r=>child.once('exit',r));if(code!==0)throw Error(err);await S.page.waitForTimeout(900);
 }finally{await S.electronApp.evaluate(()=>global.__dragSource?.destroy());await S.page.bringToFront();}
}
async function systemContrast(S){
 const {spawn}=require('child_process');
 const child=spawn('powershell.exe',['-NoProfile','-ExecutionPolicy','Bypass','-File',path.join(__dirname,'toggle-high-contrast.ps1')],{windowsHide:true,stdio:['pipe','pipe','pipe']});
 let output='',errors='';child.stdout.on('data',b=>output+=b);child.stderr.on('data',b=>errors+=b);
 const ended=new Promise(resolve=>child.once('exit',code=>resolve(code)));
 try {
  await S.page.evaluate(()=>window.__contrastBefore={theme:window.__ide.editor.getDomNode().className,forced:matchMedia('(forced-colors:active)').matches});
  const deadline=Date.now()+12000;while(!output.includes('"changed":true')&&Date.now()<deadline&&!errors)await new Promise(resolve=>setTimeout(resolve,100));
  if(!output.includes('"changed":true'))throw Error('Windows не переключила контраст: '+errors);
  await S.page.waitForTimeout(1800);
  await S.page.evaluate(()=>window.__contrastAfter={theme:window.__ide.editor.getDomNode().className,forced:matchMedia('(forced-colors:active)').matches});
 }finally{
  child.stdin.end('restore\n');await ended;
  if(!output.includes('"restored":true'))throw Error('Не подтверждено возвращение исходного контраста: '+output+' '+errors);
  await S.page.waitForTimeout(700);
 }
}
async function saveReport(report){
 const target=path.join(EVIDENCE,'editor-settings-live-fields.json'),temp=target+'.tmp';
 for(let attempt=0;attempt<10;attempt++)try{fs.writeFileSync(temp,JSON.stringify(report,null,2)+'\n');fs.renameSync(temp,target);return;}catch(error){if(attempt===9)throw error;await new Promise(resolve=>setTimeout(resolve,100));}
}
const text=async(S,source,position={lineNumber:1,column:1})=>{
 const file=path.join(fixtureRoot,process.pid+'-'+(++S.fixtureSequence|| (S.fixtureSequence=1))+'.axm');fs.writeFileSync(file,source);
 await S.page.evaluate(async({file,position,fixtureRoot,shotRoot,keep})=>{
  const i=window.__ide,previous=i.activeFile;await i.openFile(file);
  if(!keep&&previous&&[fixtureRoot,shotRoot].some(root=>previous.toLowerCase().startsWith(root.toLowerCase()+String.fromCharCode(92)))){await i.save(previous);await i.closeFile(previous);}
  const e=i.editor;e.setPosition(position);e.setScrollTop(0);e.setScrollLeft(0);e.focus();e.render(true);
 },{file,position,fixtureRoot,shotRoot,keep:!!S.keepPreviousFixture});
};
const code=Array.from({length:180},(_,n)=>n===0?'Sub LongProcedure'+ 'Name'.repeat(45)+'()':n===1?'    If True Then':n===2?'        For step = 1 To 100':n===178?'    End If':n===179?'End Sub':`            Print Abs(step + ${n}) ' MARK: Section ${n} `+'text '.repeat(45)).join('\n');
async function configureUI(S,entry,value){
 await S.page.evaluate(()=>window.__ide.handleAction('editor-settings'));
 await S.page.fill('.settings-search',entry.key);
 const input=S.page.locator(`[data-setting-input="${entry.key}"]`);
 if(await input.isDisabled())throw Error('Элемент неожиданно недоступен');
 if(entry.kind==='boolean')await input.setChecked(value);
 else if(entry.kind==='select')await input.selectOption(JSON.stringify(value));
 else if(['fontFamily','fontWeight'].includes(entry.key)&&await input.evaluate(n=>n.tagName==='SELECT')){
  const known=await input.evaluate((n,value)=>Array.from(n.options).some(o=>o.value===value),String(value));
  await input.selectOption(known?String(value):'custom');if(!known)await input.locator('..').locator('input').fill(String(value));
 } else if(['fontLigatures','fontVariations'].includes(entry.key)){
  await input.selectOption(typeof value==='string'?'custom':JSON.stringify(value));
  if(typeof value==='string')await input.locator('..').locator('input').fill(value);
 }else await input.fill(entry.kind==='json'?JSON.stringify(value):String(value));
 await S.page.click('[data-settings-action="ok"]');await S.page.locator('.editor-settings-overlay').waitFor({state:'hidden'});await S.page.waitForTimeout(250);
}
async function baseline(S,key,mode){
 await S.page.evaluate(async({key,mode})=>{
  const i=window.__ide;i.editorSettingsDialog?.close();
  await i.applyEditorPreferences({cursorBlinking:'solid',renderLineHighlight:'all',fontFamily:'Consolas',fontSize:14,wordWrap:mode.startsWith('wrap')?'wordWrapColumn':'off',wordWrapColumn:35,'minimap.enabled':mode.startsWith('minimap'), 'minimap.showMarkSectionHeaders':true,'minimap.showRegionSectionHeaders':true,'minimap.autohide':'none', 'stickyScroll.enabled':true,'stickyScroll.defaultModel':'indentationModel',hover:{},'hover.enabled':'on','hover.delay':50,'hover.hidingDelay':0,'parameterHints.enabled':true,quickSuggestions:{},'quickSuggestions.other':'off','quickSuggestions.comments':'off','quickSuggestions.strings':'off',acceptSuggestionOnEnter:'on','suggest.showStatusBar':true,'bracketPairColorization.enabled':true,'guides.bracketPairs':true,'guides.bracketPairsHorizontal':true,'guides.indentation':true,'unicodeHighlight.nonBasicASCII':true,'unicodeHighlight.includeComments':true,'unicodeHighlight.includeStrings':true,accessibilitySupport:'off'},'dark','ru');
 },{key,mode});
 await text(S,code);
 await prerequisites(S,{defaultColorDecorators:'always'});
 await S.page.evaluate(()=>window.__ide.editor.trigger('keyboard','editor.action.fontZoomReset',{}));
 if(mode==='quick-delay')await prerequisites(S,{'quickSuggestions.other':'on'});
 if(key==='suggest.showWords'||mode==='word-suggest')await prerequisites(S,{wordBasedSuggestions:'currentDocument'});
}
async function prepare(S,key,mode){
 const sources={css:'Print "fi != =>"\nPrint 42',lines:'Print 1\nPrint 2\n',whitespace:'Print 1  +  2\n\tPrint 3',control:'Print "A\u0001B"', 'final-line':'Print 1\n', 'line-highlight':'Print 1\nPrint 2', 'line-focus':'Print 1\nPrint 2',rulers:'Print 1',tab:'', 'enter-indent':'If True Then\n    Print 1', 'trim-indent':'If True Then\n    Print 1', 'paste-indent':'If True Then\n    Print 1', 'paste-string':'Print "one two"', 'close-bracket':'Print Abs', 'close-quote':'Print ', 'pair-delete':'', 'pair-overtype':'',surround:'score', 'tab-move':'        Print 1','delete-space':'        Print 1',comment:'Print 1\n\nPrint 2','copy-line':'Print 1','copy-html':'Print 1','paste-overtype':'Print 123456', 'multi-paste':'one\ntwo\nthree','multi-merge':'one\ntwo', 'multi-limit':'same\nsame\nsame\nsame\nsame\nsame\nsame',selection:'Print hello world\nPrint second line', 'selection-match':'Print sharedName\nPrint sharedName', 'selection-multiline':'Print one\nPrint two\nPrint one\nPrint two','word-select':key==='wordSegmenterLocales'?'Print "日本語東京"':'Print snake_case','smart-selection':'Print camelCaseName','double-block':'Print Abs((score))','bracket-match':'Print Abs((score))','bracket-colors':'Return Abs(PlayerPixelX() - GhostPixelX()) < 12 And Abs(PlayerPixelY() - GhostPixelY())',guides:'If True Then\n    Print Abs(\n        Abs(42)\n    )\nEnd If',unicode:'Print а\nPrint "а\u200b"\n\' а\u200b',links:'\' https://example.com/page', 'long-token':'Print "'+ 'x'.repeat(180)+'"', 'long-render':'Print "'+ 'x'.repeat(180)+'"','wrap-escape':'Print "first\\nsecond\\nthird"',wrap:'    Print '+Array.from({length:30},(_,n)=>'longName'+n+'+').join(''), 'find-type':'Print first\nPrint target\nPrint target','find-seed':'Print first\nPrint target\nPrint target','find-selection':'Print first\nPrint target\nPrint target','find-space':'Print first\nPrint target\nPrint target','find-loop':'Print first\nPrint target\nPrint target','find-close':'Print first\nPrint target\nPrint target',semantic:'Dim score As Integer = 42\nPrint score','hover':'Print Abs(42)','hover-delay':'Print Abs(42)','hover-position':'\n\n\n\n\nPrint Abs(42)','hover-sticky':'Print Abs(42)','hover-hide':'Print Abs(42)','signature':'Print Abs','signature-cycle':'Print Mid','unused':'Dim unusedValue As Integer = 1\nPrint 1','diagnostics':'Print missingVariable','deprecated':'Print Abs(42)',colors:'\' #ff0000 #00ff00 rgb(0,0,255)'};
 await text(S,sources[mode]??code);
 if(mode.startsWith('suggest')||mode==='remember-suggest'||mode==='accept-enter'||mode==='tab-completion'||mode==='quick'||mode==='quick-delay'||mode==='word-suggest'||mode==='snippet-quick'){
  await text(S,'Dim appleName As Integer\nDim apricotName As Integer\nType Fruit\n    value As Integer\nEnd Type\nFunction AppleFunction() As Integer\n    Return 1\nEnd Function\nPrint ');
  await S.page.evaluate(()=>{const e=window.__ide.editor;e.setPosition({lineNumber:e.getModel().getLineCount(),column:e.getModel().getLineMaxColumn(e.getModel().getLineCount())});});
 }
 if(mode==='word-suggest'||key==='suggest.showWords'){
  await text(S,"' wordUniqueDocument\nPrint wordU",{lineNumber:2,column:12});
  await prerequisites(S,{wordBasedSuggestions:'currentDocument'});
 }
 if(mode==='minimap-pixels')await S.page.waitForTimeout(1500);
 if(mode==='color-activation')await text(S,"' #ff0000");
 if(mode==='fold'||mode==='unfold-click')await text(S,'If True Then\n    Print 1\nEnd If\nIf True Then\n    Print 2\nEnd If\nIf True Then\n    Print 3\nEnd If');
 if(key==='foldingMaximumRegions')await text(S,Array.from({length:40},(_,n)=>`If True Then\n    Print ${n}\nEnd If`).join('\n'));
 if(key==='accessibilityPageSize'){await prerequisites(S,{accessibilitySupport:'on'});await text(S,Array.from({length:150},()=>"' a").join('\n'));}
 if(mode==='long-token')await prerequisites(S,{'semanticHighlighting.enabled':false});
 if(mode==='detect-indent')await text(S,'');
 if(mode==='trim-indent'){await text(S,"    ' blank indentation");await prerequisites(S,{autoIndent:'keep'});}
 if(mode==='tab-move'||mode==='delete-space')await text(S,'        Print 1',{lineNumber:1,column:8});
 if(key==='useTabStops'||key==='stickyTabStops'){
  await text(S,'        Print 1',{lineNumber:1,column:9});
  await prerequisites(S,{detectIndentation:false,tabSize:4});
 }
 if(key==='trimWhitespaceOnDelete')await text(S,'Print 1\n    Print 2',{lineNumber:1,column:8});
 if(key==='wordSeparators')await text(S,'Print snake_case',{lineNumber:1,column:17});
 if(key==='smartSelect.selectLeadingAndTrailingWhitespace')await text(S,'    Print camelCaseName    \nPrint 2',{lineNumber:1,column:16});
 if(key==='guides.highlightActiveIndentation')await text(S,'If True Then\n    If True Then\n        Print 1\n    End If\nEnd If',{lineNumber:3,column:10});
 if(key==='fontLigatures'||key==='fontVariations'){
  await prerequisites(S,{fontFamily:'Cascadia Code'});
  await text(S,'Print "-> === != <= >= fi"\nPrint 42');
 }
 if(['unicodeHighlight.invisibleCharacters','unicodeHighlight.ambiguousCharacters','unicodeHighlight.allowedLocales'].includes(key)){
  await text(S,'Print pаypal\nPrint "pаypal\u200b"\n\' pаypal\u200b');
  await prerequisites(S,{'unicodeHighlight.nonBasicASCII':false,'unicodeHighlight.allowedLocales':{}});
 }
 if(mode==='paste-indent'||mode==='paste-string')await prerequisites(S,{autoIndent:'full',autoIndentOnPaste:true});
 if(mode==='paste-indent')await text(S,'If True Then\n    \nEnd If',{lineNumber:2,column:5});
 if(mode==='semantic')await text(S,'Const namedValue As Integer = 42\nDim items(3) As Integer\nPrint namedValue + items(1)');
 if(mode==='hover-position')await text(S,'\n'.repeat(15)+'Print Abs(42)');
 if(mode==='long-hover'){
  await prerequisites(S,{maxTokenizationLineLength:40});
  await text(S,'Print "'+ 'x'.repeat(300)+'"');
 }
 if(mode==='signature-cycle'){
  const name=await S.page.evaluate(()=>{const counts={};for(const b of window.__ide.langMeta?.builtins||[])counts[b.name]=(counts[b.name]||0)+1;return Object.keys(counts).find(name=>counts[name]>1)||'MID';});
  await text(S,'Print '+name);
 }
 if(mode==='drop')await S.page.evaluate(()=>{window.__dropEvents=[];if(!window.__dropRecorder){window.__dropRecorder=true;for(const type of ['dragenter','dragover','drop'])document.addEventListener(type,e=>window.__dropEvents.push({type,trusted:e.isTrusted,types:Array.from(e.dataTransfer?.types||[]),target:e.target.className,x:e.clientX,y:e.clientY,files:Array.from(e.dataTransfer?.files||[]).map(f=>({name:f.name,path:f.path}))}),{capture:true});}});
 if(mode==='middle'){
  if(!S.linkServer){S.linkServer=require('http').createServer((req,res)=>{res.end('Axiom: проверка перехода по ссылке');});await new Promise(r=>S.linkServer.listen(0,'127.0.0.1',r));}
  await text(S,"' http://127.0.0.1:"+S.linkServer.address().port+'/axiom-middle');
  await S.electronApp.evaluate(({BrowserWindow})=>{global.__middleWindows=[];if(!global.__middleWatcher){global.__middleWatcher=true;BrowserWindow.getAllWindows()[0].webContents.on('did-create-window',w=>{global.__middleWindows.push(w);});}});
 }
 if(mode==='find-selection')await text(S,'Print target\nPrint target\nPrint target');
 if(mode==='find-seed'){
  await S.page.keyboard.press('Control+F');await S.page.locator('.find-widget .input').first().fill('oldSearchSentinel');await S.page.keyboard.press('Escape');
 }
 if(mode==='suggest-local'){await text(S,'Dim appleFar As Integer\nDim apricotNear As Integer\nPrint Abs(\n    apricotNear +\n    ap\n)',{lineNumber:5,column:7});await S.page.waitForTimeout(1000);}
 if(mode==='suggest-selection'||mode==='snippet-quick')await prerequisites(S,{'quickSuggestions.other':'on'});
 if(mode==='snippet-quick')await text(S,'');
 if(mode==='suggest-filter')await text(S,'Dim appleName As Integer\nPrint ',{lineNumber:2,column:7});
 if(key==='suggest.previewMode')await prerequisites(S,{'suggest.preview':true});
 if(key==='scrollbar.alwaysConsumeMouseWheel'||mode==='paste-selector'||mode==='drop')await text(S,'');
 if(mode==='scrollbar-click')await prerequisites(S,{'scrollbar.vertical':'visible'});
 if(key==='cursorSurroundingLinesStyle')await prerequisites(S,{cursorSurroundingLines:6});
 if(mode==='wheel')await S.page.evaluate(()=>{window.__wheelSamples=[];if(!window.__wheelRecorder){window.__wheelRecorder=true;window.addEventListener('wheel',event=>setTimeout(()=>window.__wheelSamples.push({trusted:event.isTrusted,prevented:event.defaultPrevented,target:event.target.className,x:event.deltaX,y:event.deltaY})),{capture:true});}});
 if(['cursor','cursor-move','overtype-cursor'].includes(mode))await text(S,'Print '+ 'longName '.repeat(8));
 if(key==='wordBreak')await text(S,'Print "abcdefabcdef 日本語東京日本語東京日本語東京 日本語東京日本語東京"');
 if(key==='wordWrapBreakBeforeCharacters'||key==='wordWrapBreakAfterCharacters'){
  await text(S,'Print "'+ 'a'.repeat(20)+'+'+'b'.repeat(20)+'+'+'c'.repeat(20)+'"');
  const opposite=key==='wordWrapBreakBeforeCharacters'?'wordWrapBreakAfterCharacters':'wordWrapBreakBeforeCharacters';
  await prerequisites(S,{[opposite]:''});
 }
 if(mode.startsWith('occurrences'))await text(S,'Dim repeatedName As Integer\nDim anotherName As Integer\nPrint repeatedName\nPrint anotherName\nPrint anotherName',{lineNumber:3,column:10});
 if(mode.startsWith('inline')){
  await text(S,'Print AB',{lineNumber:1,column:9});
  await prerequisites(S,{'inlineSuggest.enabled':true,'quickSuggestions.other':'inline'});
  if(mode==='inline-aria')await prerequisites(S,{accessibilitySupport:'on'});
  if(key==='inlineSuggest.suppressInSnippetMode'){await text(S,'');await prerequisites(S,{'suggest.snippetsPreventQuickSuggestions':false});}
 }
}
async function action(S,key,mode,value){
 const end=()=>S.page.evaluate(()=>{const e=window.__ide.editor,n=e.getModel().getLineCount();e.setPosition({lineNumber:n,column:e.getModel().getLineMaxColumn(n)});e.focus();});
 const select=async sl=>S.page.evaluate(sl=>{window.__ide.editor.setSelection(new monaco.Selection(...sl));window.__ide.editor.focus();},sl);
 const command=async name=>S.page.evaluate(name=>window.__ide.editor.trigger('keyboard',name,{}),name);
 if(mode==='color-activation'){
  await S.page.locator('.colorpicker-color-decoration').first().waitFor({state:'visible'});const b=await S.page.locator('.colorpicker-color-decoration').first().boundingBox();
  await S.page.mouse.move(b.x+b.width/2,b.y+b.height/2);await S.page.waitForTimeout(700);
  const hover=await S.page.locator('.colorpicker-widget').isVisible().catch(()=>false);
  await S.page.keyboard.press('Escape');await S.page.mouse.move(5,5);await S.page.waitForTimeout(200);
  await S.page.mouse.click(b.x+b.width/2,b.y+b.height/2);await S.page.waitForTimeout(300);
  const click=await S.page.locator('.colorpicker-widget').isVisible().catch(()=>false);await S.page.evaluate(state=>window.__colorActivation=state,{hover,click});
 }
 else if(mode==='line-click'){const box=await S.page.locator('.line-numbers').first().boundingBox();await S.page.mouse.click(box.x+box.width/2,box.y+box.height/2);}
 else if(mode==='line-focus')await S.page.evaluate(()=>{const button=document.querySelector('[data-menu="file"]');button.tabIndex=0;button.focus();});
 else if(mode==='tab')await S.page.keyboard.press('Tab');
 else if(mode==='close-bracket'||mode==='close-quote'){await end();await S.page.keyboard.type(mode==='close-bracket'?'(':'"');}
 else if(mode==='pair-delete'||mode==='pair-overtype'){await S.page.keyboard.type('(');await S.page.keyboard.press(mode==='pair-delete'?'Backspace':'Shift+Digit0');}
 else if(mode==='surround'){await select([1,1,1,6]);await S.page.keyboard.type('(');}
 else if(mode==='enter-indent'||mode==='trim-indent'){await end();await S.page.keyboard.press('Enter');if(mode==='trim-indent'){await S.page.keyboard.press('Enter');await S.page.keyboard.type('Print 2');}}
 else if(mode==='tab-move'){await S.page.keyboard.press(key==='useTabStops'?'Backspace':'ArrowLeft');}
 else if(mode==='delete-space'){await S.page.keyboard.press(key==='trimWhitespaceOnDelete'?'Delete':'Backspace');}
 else if(mode==='comment'){await select([1,1,3,8]);await command('editor.action.commentLine');}
 else if(mode==='paste-indent'||mode==='paste-string'||mode==='paste-overtype'||mode==='multi-paste'){
  if(mode!=='paste-indent')await end();if(mode==='paste-string')await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:1,column:10}));
  if(mode==='paste-overtype'){await command('editor.action.toggleOvertypeInsertMode');await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:1,column:8}));}
  if(mode==='multi-paste')await S.page.evaluate(()=>window.__ide.editor.setSelections([new monaco.Selection(1,4,1,4),new monaco.Selection(2,4,2,4)]));
  await S.electronApp.evaluate(({clipboard},mode)=>clipboard.writeText(mode==='multi-paste'?'AAA\nBBB':'    Print 2\n        Print 3'),mode);await S.page.keyboard.press('Control+V');
  await S.page.waitForFunction(mode=>mode==='multi-paste'?window.__ide.editor.getValue().includes('AAA'):/print\s+2/i.test(window.__ide.editor.getValue()),mode,{timeout:5000});
 }
 else if(mode==='copy-line'||mode==='copy-html'){await S.electronApp.evaluate(({clipboard})=>clipboard.clear());if(mode==='copy-html')await select([1,1,1,8]);await S.page.keyboard.press('Control+C');}
 else if(mode==='selection'||mode==='selection-match'){await select([1,7,1,17]);}
 else if(mode==='selection-multiline')await select([1,1,2,10]);
 else if(mode==='multi-merge')await S.page.evaluate(()=>window.__ide.editor.setSelections([new monaco.Selection(1,1,1,4),new monaco.Selection(1,2,1,4)]));
 else if(mode==='multi-limit'){await S.page.keyboard.press('Control+Home');for(let n=0;n<7;n++)await command('editor.action.insertCursorBelow');}
 else if(mode==='column'){await S.page.keyboard.press('Control+Home');await S.page.keyboard.press('Shift+ArrowDown');await S.page.keyboard.press('Shift+ArrowRight');}
 else if(mode==='smart-selection'){if(key==='smartSelect.selectLeadingAndTrailingWhitespace'){for(let n=0;n<3;n++)await command('editor.action.smartSelect.expand');}else{await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:1,column:12}));await command('editor.action.smartSelect.expand');}}
 else if(mode==='word-select'&&key==='wordSeparators')await S.page.keyboard.press('Control+ArrowLeft');
 else if(mode==='word-select'||mode==='double-block'){
  const p=await S.page.evaluate(({mode,key})=>{const e=window.__ide.editor,pos=e.getScrolledVisiblePosition({lineNumber:1,column:mode==='double-block'?11:key==='wordSegmenterLocales'?9:15}),box=e.getDomNode().getBoundingClientRect();return{x:box.x+pos.left+3,y:box.y+pos.top+8};},{mode,key});await S.page.mouse.dblclick(p.x,p.y);
 }
 else if(mode==='multi-mouse'){const p=await S.page.locator('.view-lines').boundingBox();await S.page.keyboard.down('Alt');await S.page.mouse.click(p.x+40,p.y+28);await S.page.keyboard.up('Alt');}
 else if(mode==='focus-tab')await S.page.keyboard.press('Tab');
 else if(mode==='cursor'||mode==='cursor-move'||mode==='overtype-cursor'){await end();if(mode==='overtype-cursor')await command('editor.action.toggleOvertypeInsertMode');if(mode==='cursor-move')await S.page.keyboard.press('Home');}
 else if(mode==='cursor-reveal'){
  if(key==='cursorSurroundingLinesStyle'){
   const p=await S.page.evaluate(()=>{const e=window.__ide.editor,line=e.getVisibleRanges()[0].endLineNumber-2,pos=e.getScrolledVisiblePosition({lineNumber:line,column:20}),b=e.getDomNode().getBoundingClientRect();return{x:b.x+pos.left+3,y:b.y+pos.top+8};});await S.page.mouse.click(p.x,p.y);
  }else await S.page.evaluate(()=>window.__ide.editor.revealPosition({lineNumber:70,column:20}));
 }
 else if(mode==='bracket-match')await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:1,column:10}));
 else if(mode==='guides')await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:3,column:12}));
 else if(mode==='minimap-pixels')await S.page.waitForTimeout(1500);
 if(mode==='color-activation')await text(S,"' #ff0000");
 if(mode==='fold'||mode==='unfold-click'){await S.page.keyboard.press('Control+Home');await command('editor.foldAll');if(mode==='unfold-click'){const p=await S.page.locator('.view-lines').boundingBox();await S.page.mouse.click(p.x+300,p.y+8);}}
 else if(mode.startsWith('sticky')){await S.page.evaluate(()=>window.__ide.editor.setScrollTop(300));if(mode==='sticky-horizontal')await S.page.evaluate(()=>window.__ide.editor.setScrollLeft(100));}
 else if(mode==='scroll-end')await S.page.evaluate(()=>window.__ide.editor.setScrollTop(100000));
 else if(mode==='scroll-right')await S.page.evaluate(()=>window.__ide.editor.setScrollLeft(100000));
 else if(mode==='reveal-right')await S.page.evaluate(()=>window.__ide.editor.revealPosition({lineNumber:1,column:150}));
 else if(mode==='smooth'){
  await S.page.evaluate(()=>{window.__scrollSamples=[];window.__ide.editor.setScrollTop(1500,0);window.__scrollSamples.push(window.__ide.editor.getScrollTop());});
  for(const ms of [16,30,60,100]){await S.page.waitForTimeout(ms);await S.page.evaluate(()=>window.__scrollSamples.push(window.__ide.editor.getScrollTop()));}
 }
 else if(mode==='inertia'){
  const b=await S.page.locator('.view-lines').boundingBox();await S.page.mouse.move(b.x+100,b.y+100);
  await S.page.evaluate(()=>window.__scrollSamples=[]);
  await nativeWheel(S);await S.page.evaluate(()=>window.__scrollSamples.push(window.__ide.editor.getScrollTop()));
  for(const ms of [30,60,100]){await S.page.waitForTimeout(ms);await S.page.evaluate(()=>window.__scrollSamples.push(window.__ide.editor.getScrollTop()));}
 }
 else if(['wheel','fast-wheel','wheel-diagonal','inertia','zoom'].includes(mode)){
  const b=await S.page.locator('.view-line').first().boundingBox();await S.page.mouse.move(b.x+30,b.y+8);
  if(mode==='fast-wheel')await S.page.keyboard.down('Alt');if(mode==='zoom')await S.page.keyboard.down('Control');
  await S.page.mouse.wheel(mode==='wheel-diagonal'?60:0,120);

  if(mode==='fast-wheel')await S.page.keyboard.up('Alt');if(mode==='zoom')await S.page.keyboard.up('Control');
 }
 else if(mode==='scrollbar-click'){const b=await S.page.locator('.monaco-editor .editor-scrollable > .scrollbar.vertical').first().boundingBox();if(b){await S.page.mouse.move(b.x+b.width/2,b.y+b.height*.8);await S.page.waitForTimeout(300);await S.page.mouse.click(b.x+b.width/2,b.y+b.height*.8);}}
 else if(mode==='middle'||mode==='middle-scroll'){const b=await S.page.locator('.view-lines').boundingBox();if(mode==='middle'){const link=await S.page.locator('.detected-link').first().boundingBox();await S.page.mouse.click(link.x+20,link.y+8,{button:'middle'});await S.page.waitForTimeout(700);}else{await S.page.mouse.click(b.x+30,b.y+30,{button:'middle'});await S.page.mouse.move(b.x+30,b.y+100);}}
 else if(mode.startsWith('find-')){
  await S.page.keyboard.press('Escape');
  if(mode==='find-seed')await select([2,7,2,13]);
  if(mode==='find-selection'){await S.page.keyboard.press('Escape');await select([2,7,3,13]);}
  await S.page.keyboard.press('Control+F');
  if(!['find-seed','find-space'].includes(mode))await S.page.locator('.find-widget .input').first().fill('target');
  if(mode==='find-loop'){await S.page.keyboard.press('Enter');await S.page.keyboard.press('Enter');}
  if(mode==='find-close')await S.page.keyboard.press('Enter');
  if(mode==='find-space'){const b=await S.page.locator('.view-line').first().boundingBox();await S.page.mouse.move(b.x+30,b.y+8);await S.page.mouse.wheel(0,-120);}
 }
 else if(mode==='signature'||mode==='signature-cycle'){await end();await S.page.keyboard.type('(');await S.page.waitForTimeout(500);if(mode==='signature-cycle')for(let n=0;n<5;n++)await S.page.keyboard.press('ArrowDown');}
 else if(mode.startsWith('hover')){
  const pos=await S.page.evaluate(()=>{const e=window.__ide.editor,n=e.getModel().getLineCount(),p=e.getScrolledVisiblePosition({lineNumber:n,column:8}),b=e.getDomNode().getBoundingClientRect();return{x:b.x+p.left,y:b.y+p.top+8};});await S.page.mouse.move(pos.x,pos.y);await S.page.waitForTimeout(mode==='hover-hide'?700:180);
  if(mode==='hover-hide'){const popup=await S.page.locator('.monaco-hover').filter({visible:true}).first().boundingBox().catch(()=>null);if(popup){await S.page.mouse.move(popup.x+10,popup.y+10);await S.page.waitForTimeout(100);}const b=await S.page.locator('.view-lines').boundingBox();await S.page.mouse.move(b.x+400,b.y+Math.min(300,b.height-5));}
  if(mode==='hover-sticky'){const b=await S.page.locator('.monaco-hover').first().boundingBox();if(b)await S.page.mouse.move(b.x+10,b.y+10);}
 }
 else if(mode==='quick'||mode==='quick-delay'){
  if(key.includes('comments')){await end();await S.page.keyboard.type("' ");}else if(key.includes('strings')){await end();await S.page.keyboard.type('"');}
  await S.page.keyboard.type('Ab');
 }
 else if(mode.startsWith('suggest')||mode==='accept-enter'||mode==='tab-completion'||mode==='word-suggest'){
  await S.page.keyboard.type(mode==='suggest-filter'?'appel':mode==='suggest-insert'?'ApplName':mode==='suggest-view'||mode==='suggest-selection'?'AB':'');
  if(mode!=='suggest-selection')await command('editor.action.triggerSuggest');await S.page.waitForTimeout(500);
  if(mode==='accept-enter')await S.page.keyboard.press('Enter');
  if(mode==='tab-completion'){await S.page.keyboard.press('Escape');await S.page.keyboard.type('Abs');await S.page.keyboard.press('Tab');}
  if(mode==='suggest-insert')await S.page.keyboard.press('Enter');
 }
 else if(key==='accessibilityPageSize'){await S.page.keyboard.press('ArrowDown');}
 else if(mode==='diagnostics'||mode==='unused')await S.page.evaluate(()=>window.__ide.check(true));
 else if(mode==='system-contrast')await systemContrast(S);
 else if(mode==='detect-indent'||mode==='terminators'){
  const file=path.join(shotRoot,mode+'.axm');fs.writeFileSync(file,mode==='detect-indent'?'If True Then\n  Print 1\nEnd If':'Print 1\u2028Print 2');
  await S.page.evaluate(file=>window.__ide.openFile(file),file);await S.page.keyboard.press('Control+End');if(mode==='detect-indent')await S.page.keyboard.press('Enter');
 }
 else if(mode==='large-file'){
  const file=path.join(shotRoot,'large-file-'+String(value)+'.axm');
  if(!fs.existsSync(file))fs.writeFileSync(file,("' "+'x'.repeat(3000)+'\n').repeat(7400)+'Print 1');
  await S.page.evaluate(file=>window.__ide.openFile(file),file);await S.page.waitForTimeout(1500);
 }
 else if(mode==='drag-text'){
  await text(S,'hello world');await select([1,1,1,6]);const b=await S.page.locator('.view-lines').boundingBox();
  await S.page.mouse.move(b.x+15,b.y+8);await S.page.mouse.down();await S.page.mouse.move(b.x+100,b.y+8,{steps:15});await S.page.mouse.up();
 }
 else if(mode==='drop'){
  const file=path.join(shotRoot,'dropped.axm');fs.writeFileSync(file,'Print 123');
  await nativeDrop(S,file);
 }
 else if(mode==='paste-selector'){
  if(key==='pasteAs.enabled'){await text(S,'Print 123');await select([1,1,1,10]);await S.electronApp.evaluate(({clipboard})=>clipboard.clear());await S.page.keyboard.press('Control+C');return;}
  await S.page.evaluate(()=>{window.__pasteFormats=[];if(!window.__pasteRecorder){window.__pasteRecorder=true;document.addEventListener('paste',event=>window.__pasteFormats.push({trusted:event.isTrusted,types:Array.from(event.clipboardData?.types||[])}),{capture:true});}});
  await S.electronApp.evaluate(({clipboard,ClipboardItem})=>clipboard.write([new ClipboardItem({'text/plain':'Print 123','text/html':'<b>Print 123</b>'})]));
  await S.page.keyboard.press('F1');await S.page.locator('.quick-input-widget input').fill('>Paste As');await S.page.waitForTimeout(300);await S.page.locator('.quick-input-list .monaco-list-row').filter({hasText:'Paste As...'}).first().click();await S.page.keyboard.press('Enter');await S.page.waitForTimeout(700);
  if(!await S.page.locator('.quick-input-widget').isVisible()){
   const b=await S.page.locator('.view-lines').boundingBox();await S.page.mouse.click(b.x+50,b.y+8,{button:'right'});
   const item=S.page.locator('.context-view .action-label').filter({hasText:/^Paste As/}).first();
   if(await item.count())await item.click();else await S.page.keyboard.press('Escape');
   await S.page.waitForTimeout(500);
  }
  if(await S.page.locator('.quick-input-list .monaco-list-row').filter({hasText:'Insert HTML'}).count()){await S.page.locator('.quick-input-list .monaco-list-row').filter({hasText:'Insert HTML'}).first().click();await S.page.keyboard.press('Enter');}
 }
 else if(mode==='remember-suggest'){
  await S.page.keyboard.type('AB');await command('editor.action.triggerSuggest');await S.page.waitForTimeout(500);await S.page.keyboard.press('ArrowDown');await S.page.keyboard.press('Enter');
  await S.page.keyboard.press('Control+Z');await command('editor.action.triggerSuggest');
 }
 else if(mode==='long-hover'){
  const b=await S.page.locator('.view-lines').boundingBox();await S.page.mouse.move(b.x+120,b.y+10);
 }
 else if(mode==='snippet-quick'){
  await S.page.keyboard.type('IF');await command('editor.action.triggerSuggest');await S.page.waitForTimeout(500);
  await S.page.locator('.suggest-widget.visible .monaco-list-row').filter({has:S.page.locator('.codicon-symbol-snippet')}).first().click();await S.page.waitForFunction(()=>window.__ide.editor.getContribution('snippetController2').isInSnippet());await S.page.keyboard.type('AB');
 }
 else if(mode.startsWith('inline')){
  if(key==='inlineSuggest.suppressInSnippetMode'){await S.page.keyboard.type('IF');await command('editor.action.triggerSuggest');await S.page.waitForTimeout(500);await S.page.locator('.suggest-widget.visible .monaco-list-row').filter({has:S.page.locator('.codicon-symbol-snippet')}).first().click();await S.page.waitForFunction(()=>window.__ide.editor.getContribution('snippetController2').isInSnippet());await S.page.keyboard.type('AB');}
  await command('editor.action.inlineSuggest.trigger');
  if(key==='inlineSuggest.keepOnBlur'){await S.page.waitForTimeout(700);await S.page.evaluate(()=>{const b=document.querySelector('[data-menu="file"]');b.tabIndex=0;b.focus();});}
 }
 else if(mode.startsWith('occurrences'))await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:5,column:10}));
 if(mode==='minimap-pixels')await S.page.waitForTimeout(1500);
 await S.page.waitForTimeout(['quick-delay','hover-delay','inline-delay','occurrences-delay'].includes(mode)?120:['cursor','colors','occurrences','inline','inline-aria','long-hover','links'].includes(mode)?1300:300);
}
async function observe(S,key,mode){
 if(key==='pasteAs.enabled')return S.electronApp.evaluate(async({clipboard})=>{const item=(await clipboard.read())[0],type=item.types.find(t=>t.includes('Web Custom'));const text=type?Buffer.from(await(await item.getType(type)).arrayBuffer()).toString('utf16le'):'';return{pasteMetadata:text.includes('application/vnd.code.copymetadata'),text:await clipboard.readText()};});
 if(mode==='middle')return S.electronApp.evaluate(()=>{const urls=global.__middleWindows.map(w=>w.webContents.getURL());for(const w of global.__middleWindows)w.destroy();global.__middleWindows=[];return{urls};});
 if(mode==='copy-html'||mode==='copy-line')return S.electronApp.evaluate(async({clipboard},mode)=>{const items=mode==='copy-html'?await clipboard.read():[],item=items.find(item=>item.types.includes('text/html'));return{text:await clipboard.readText(),html:item?await(await item.getType('text/html')).text():undefined,hasHtml:mode==='copy-html'?await clipboard.has('text/html'):undefined};},mode);
 return S.page.evaluate(({key,mode})=>{
  const e=window.__ide.editor,root=e.getDomNode();
  const q=s=>root.querySelector(s),qa=s=>Array.from(root.querySelectorAll(s));
  const rect=n=>{if(!n)return null;const b=n.getBoundingClientRect();return{x:b.x,y:b.y,w:b.width,h:b.height};};
  const style=n=>{if(!n)return null;const s=getComputedStyle(n);return {font:s.fontFamily,size:s.fontSize,weight:s.fontWeight,height:s.lineHeight,spacing:s.letterSpacing,features:s.fontFeatureSettings,variations:s.fontVariationSettings,cursor:s.cursor,animation:s.animationName,visibility:s.visibility,display:s.display,color:s.color,border:s.borderRadius,background:s.backgroundColor,opacity:s.opacity,decoration:s.textDecorationLine,shadow:s.boxShadow,borderLeft:s.borderLeftColor,borderTop:s.borderTopColor};};
  if(mode==='color-activation')return window.__colorActivation;
  if(mode==='css')return style(q('.view-lines'));
  if(mode==='system-contrast')return {before:window.__contrastBefore,after:window.__contrastAfter,restored:true};
  if(mode==='lines'||mode==='final-line')return {numbers:qa('.line-numbers').map(n=>({text:n.textContent,...rect(n)})),glyph:rect(q('.glyph-margin')),text:rect(q('.monaco-scrollable-element'))};
  if(mode==='whitespace')return {lines:qa('.view-line').map(n=>n.innerHTML),overlay:qa('.whitespace,.view-overlays').map(n=>n.innerHTML),canvas:qa('canvas').map(n=>n.toDataURL())};
  if(mode==='control'||mode==='long-token'||mode==='semantic')return qa('.view-line').map(n=>({text:n.textContent,html:n.innerHTML}));
  if(mode==='long-render')return qa('.view-line').map(n=>n.textContent);
  if(mode==='large-file')return {length:e.getModel().getValueLength(),optimized:e.getModel().isTooLargeForTokenization(),first:q('.view-line')?.innerHTML};
  if(mode==='line-highlight'||mode==='line-focus')return qa('.current-line').map(n=>({rect:rect(n),style:style(n)}));
  if(mode==='rulers')return qa('.view-ruler').map(n=>({rect:rect(n),style:style(n)}));
  if(mode==='padding'||mode==='wrap'||mode==='wrap-escape')return {lines:qa('.view-line').map(n=>({text:n.textContent,rect:rect(n)})),scroll:e.getScrollHeight()};
  if(mode==='cursor'||mode==='overtype-cursor'||mode==='cursor-move')return {layer:q('.cursors-layer')?.className,cursors:qa('.cursor').map(n=>({rect:rect(n),style:style(n)}))};
  if(mode==='mouse-css')return style(q('.view-lines'));
  if(mode==='bracket-colors')return qa('[class*="bracket-highlighting"]').map(n=>({text:n.textContent,color:getComputedStyle(n).color}));
  if(mode==='bracket-match'||mode==='guides'||mode==='selection'||mode==='selection-match'||mode==='selection-multiline'||mode==='fold'||mode==='unfold-click'){
   const selector=mode==='bracket-match'?'.bracket-match':mode==='guides'?'.core-guide':mode==='selection'?'.selected-text':mode.startsWith('selection-')?'.selectionHighlight':'.inline-folded,.folded-background,.codicon-folding-collapsed,.codicon-folding-expanded';
   return qa(selector).map(n=>({class:n.className,text:n.textContent,rect:rect(n),style:style(n)}));
  }
  if(mode==='unicode')return qa('.unicode-highlight').map(n=>({text:n.textContent,line:n.closest('.view-line')?.style.top}));
  if(mode.startsWith('occurrences'))return qa('.wordHighlight,.wordHighlightText,.wordHighlightStrong').map(n=>({rect:rect(n),style:style(n)}));
  if(mode.startsWith('inline'))return {snippet:e.getContribution('snippetController2')?.isInSnippet(),ghost:qa('.ghost-text,.ghost-text-decoration,.ghost-text-decoration-preview').map(n=>({text:n.textContent,style:style(n)})),renderedLines:qa('.view-line').map(n=>n.innerHTML),toolbar:qa('.inline-completions-toolbar').map(n=>({text:n.textContent,style:style(n)})),aria:Array.from(document.querySelectorAll('.monaco-aria-container [role="alert"],.monaco-aria-container [aria-live]')).map(n=>n.textContent)};
  if(mode==='minimap-pixels')return qa('.minimap canvas').map(n=>({w:n.width,h:n.height,pixels:n.toDataURL(),headers:e.getModel().getAllDecorations().filter(d=>d.options.minimap?.sectionHeaderText).map(d=>d.options.minimap.sectionHeaderText)}));
  if(mode==='minimap')return qa('.minimap,.minimap canvas,.minimap-slider').map(n=>({rect:rect(n),style:style(n)}));
  if(mode==='sticky'||mode==='sticky-horizontal')return qa('.sticky-widget,.sticky-widget .view-line,.sticky-widget .sticky-line-content,.sticky-widget span').map(n=>({text:n.textContent,rect:rect(n),style:style(n)}));
  if(mode==='scrollbar')return qa('.scrollbar.vertical,.scrollbar.horizontal').map(n=>({class:n.className,rect:rect(n),style:style(n)}));
  if(['wheel','fast-wheel','wheel-diagonal','inertia','zoom','scroll-end','scroll-right','reveal-right','cursor-reveal','smooth','middle-scroll','scrollbar-click'].includes(mode))return {top:e.getScrollTop(),left:e.getScrollLeft(),height:e.getScrollHeight(),width:e.getScrollWidth(),font:style(q('.view-lines')),trace:mode==='smooth'||mode==='inertia'?window.__scrollSamples:undefined,wheel:mode==='wheel'?window.__wheelSamples:undefined};
  if(mode.startsWith('suggest')||mode==='quick'||mode==='quick-delay'||mode==='remember-suggest'||mode==='word-suggest'||mode==='snippet-quick')return {items:e.getContribution('editor.contrib.suggestController')?.model?._completionModel?.items.map(item=>({label:item.completion.label,kind:item.completion.kind,distance:item.distance})),snippet:e.getContribution('snippetController2')?.isInSnippet(),widget:qa('.suggest-widget.visible .monaco-list-row').map(n=>({text:n.textContent,class:n.className,html:n.innerHTML})),font:style(q('.suggest-widget.visible')),icons:qa('.suggest-widget.visible .suggest-icon').map(n=>({style:style(n),glyph:getComputedStyle(n,'::before').content,display:getComputedStyle(n,'::before').display})),details:qa('.suggest-widget.visible .details-label').map(n=>style(n)),status:qa('.suggest-widget.visible .suggest-status-bar').map(n=>style(n)),ghost:qa('.ghost-text').map(n=>n.textContent),renderedLines:qa('.view-line').map(n=>n.innerHTML)};
  if(mode.startsWith('hover')||mode==='long-hover')return qa('.monaco-hover').map(n=>({text:n.textContent,rect:rect(n),style:style(n)}));
  if(mode.startsWith('signature'))return qa('.parameter-hints-widget').map(n=>({text:n.textContent,style:style(n)}));
  if(mode.startsWith('find-'))return {query:q('.find-widget .input')?.value,widget:style(q('.find-widget')),scope:e.getContribution('editor.contrib.findController')?.getState().searchScope,buttons:qa('.find-widget [aria-checked]').map(n=>({label:n.getAttribute('aria-label'),checked:n.getAttribute('aria-checked')})),matches:qa('.findMatch,.currentFindMatch').length,zones:qa('.view-zones>div').map(n=>({rect:rect(n),display:style(n).display})),position:e.getPosition(),first:rect(q('.view-line'))};
  if(mode==='accessible')return {inputs:qa('textarea,[role="textbox"]').map(n=>({role:n.getAttribute('role'),tag:n.tagName,label:n.getAttribute('aria-label'),text:n.value??n.textContent,html:n.outerHTML})),dom:qa('.screen-reader-text').map(n=>n.textContent)};
  if(mode==='links')return qa('.detected-link').map(n=>({text:n.textContent,style:style(n)}));
  if(mode==='drop')return {text:e.getValue(),events:window.__dropEvents};
  if(mode==='paste-selector')return {text:e.getValue(),formats:window.__pasteFormats,pick:Array.from(document.querySelectorAll('.quick-input-widget .monaco-list-row')).map(n=>({text:n.textContent,style:style(n)})),after:qa('.post-edit-widget').map(n=>({text:n.textContent,style:style(n)}))};
  if(['diagnostics','unused','deprecated','colors','overview'].includes(mode))return {marks:qa('[class*="squiggly"],.color-decoration,.colorpicker-color-decoration').map(n=>({class:n.className,text:n.textContent,style:style(n)})),canvas:mode==='overview'?qa('.decorationsOverviewRuler').map(n=>({rect:rect(n),pixels:n.toDataURL?.()})):[]};
  return {text:e.getValue(),selection:e.getSelections().map(s=>({sl:s.startLineNumber,sc:s.startColumn,el:s.endLineNumber,ec:s.endColumn})),focus:document.activeElement?.className};
 },{key,mode});
}
async function runOne(S,entry){
 const row={source:{head:require('child_process').execSync('git rev-parse HEAD',{cwd:ROOT,encoding:'utf8'}).trim(),app:hash(fs.readFileSync(path.join(ROOT,'ide/src/renderer/app.js'))),dialog:hash(fs.readFileSync(path.join(ROOT,'ide/src/renderer/editor-settings-dialog.js')))},key:entry.key,ru:entry.ru,group:entry.group,scenario:'live-'+entry.key,status:'pending',cases:[],errors:[]};
 if(entry.available===false){
  row.locales=[];
  for(const language of ['ru','en']){
   await S.page.evaluate(async language=>{const i=window.__ide;await i.applyEditorPreferences(i.settings.editorPreferences,i.settings.theme,language);i.handleAction('editor-settings');},language);
   await S.page.fill('.settings-search',entry.key);
   const input=S.page.locator(`[data-setting-input="${entry.key}"]`),parent=input.locator('xpath=ancestor::*[contains(@class,"settings-row")]');
   await parent.locator('label').hover();await input.hover();
   const result=await input.evaluate(n=>{const r=n.closest('.settings-row'),label=r.querySelector('label');return{disabled:n.disabled,title:n.title,muted:r.dataset.unavailable,opacity:Number(getComputedStyle(n).opacity),label:getComputedStyle(label).color,normal:getComputedStyle(document.querySelector('.settings-help')).color,help:document.querySelector('.settings-help').textContent};});
   row.locales.push({language,...result});await S.page.evaluate(()=>window.__ide.editorSettingsDialog.close());
  }
  row.status=row.locales.every(r=>r.disabled&&r.muted==='true'&&r.opacity<.6&&r.help===r.title&&r.title.includes(r.language==='en'?entry.unavailableEn:entry.unavailableRu)&&r.label===r.normal)?'unsupported':'failed';row.reason=entry.unavailableRu;
  return row;
 }
 const plan=plans[entry.key];row.plan=plan;row.criterion=plan.observation;const values=plan.values??(entry.kind==='select'?entry.choices:entry.kind==='boolean'?[false,true]:entry.kind==='number'?[1,3]:[]);
 if(!values.length){row.status='not-verified';row.errors.push('Нужен отдельный сценарий значений');return row;}
  const external=[];
 for(const value of values){
  try{
   await S.page.bringToFront();await baseline(S,entry.key,plan.mode);await prepare(S,entry.key,plan.mode);await configureUI(S,entry,value);await S.page.evaluate(()=>window.__ide.editor.focus());
   await action(S,entry.key,plan.mode,value);const observed=await observe(S,entry.key,plan.mode);
   if(entry.key==='dropIntoEditor.enabled'&&value===true){await S.page.keyboard.press('Control+Z');observed.undo=await S.page.evaluate(()=>window.__ide.editor.getValue());await S.page.keyboard.press('Control+Y');observed.redo=await S.page.evaluate(()=>window.__ide.editor.getValue());}
   const image=await S.page.locator('.monaco-editor').first().screenshot();const shot=path.join(shotRoot,entry.key.replaceAll('.','-')+'-'+row.cases.length+'.png');fs.writeFileSync(shot,image);
   const diagnostic=await S.page.evaluate(key=>{const e=window.__ide.editor;return{saved:window.__ide.settings.editorPreferences[key],raw:e.getRawOptions(),scroll:e._modelData?.view?._scrollbar?.scrollbar?._options};},entry.key);
   row.cases.push({value,observed:plan.mode==='minimap-pixels'?observed.map(o=>({...o,pixels:hash(o.pixels)})):observed,diagnostic:{saved:diagnostic.saved,parent:diagnostic.raw[entry.key.split('.')[0]],scroll:['inertia','wheel'].includes(plan.mode)?diagnostic.scroll:undefined},screenshot:shot,screenshotHash:hash(image)});
  }catch(error){row.errors.push(String(error.stack||error));}
 }
 const observations=row.cases.map(c=>JSON.stringify(plan.mode==='suggest-local'?c.observed.widget.map(x=>x.text):plan.mode==='paste-selector'&&entry.key!=='pasteAs.enabled'?{text:c.observed.text,after:c.observed.after}:c.observed));
 row.status=row.errors.length?'failed':external.includes(plan.mode)?'not-verified':new Set(observations).size>1?'observed-change':'no-observed-change';
 if(row.status==='no-observed-change'&&['foldingStrategy','stickyScroll.defaultModel','suggest.previewMode'].includes(entry.key)){
  const rendered=row.cases.every(c=>Array.isArray(c.observed)?c.observed.length>0:c.observed.renderedLines?.some(line=>line.includes('ghost')));
  if(rendered){row.status='verified-equivalent';row.equivalence=entry.key==='suggest.previewMode'?'При точном совпадении начала ABS все три режима показывают одинаковое продолжение.':'При отсутствии провайдера структуры Axiom используется штатное сворачивание по отступам.';}
 }
 row.expectationErrors=require('./editor-settings-live-assertions.cjs').verify(row);if(row.expectationErrors.length)row.status='failed';
 if(plan.mode==='middle'){S.linkServer?.close();S.linkServer=null;}
 if(row.status==='not-verified')row.errors.push('Действие требует отдельной проверки условий; этот прогон не подтверждает работу параметра.');
 return row;
}
const expectedCancellation=s=>/^Canceled: Canceled\s/.test(s)&&s.includes('editorWorkerHost-');
module.exports={runOne,plans,text,configureUI,expectedCancellation};
if(require.main===module)(async()=>{
 const S=new Session('editor-settings-live'),rows=[];const filter=process.argv[2]||'';
 const prior=filter&&fs.existsSync(path.join(EVIDENCE,'editor-settings-live-fields.json'))?JSON.parse(fs.readFileSync(path.join(EVIDENCE,'editor-settings-live-fields.json'))).rows:[];
 try{await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.emulateMedia({forcedColors:null,colorScheme:null,reducedMotion:null});S.page.setDefaultTimeout(5000);
  for(const entry of entries.filter(e=>!filter||filter.split(',').some(k=>e.key===k))){
   const before=S.pageErrors.length;const row=await runOne(S,entry);row.expectedCancellations=S.pageErrors.slice(before).filter(expectedCancellation);row.pageErrors=S.pageErrors.slice(before).filter(e=>!expectedCancellation(e));if(row.pageErrors.length)row.status='failed';rows.push(row);
   const merged=[...prior.filter(r=>!rows.some(n=>n.key===r.key)),...rows];
   await saveReport({total:261,checked:merged.length,rows:merged});
   console.log(`${rows.length}: ${entry.key} — ${row.status} (${row.cases.length})`);
  }
 }finally{S.linkServer?.close();await S.close();}
 console.log(JSON.stringify(rows.reduce((a,r)=>(a[r.status]=(a[r.status]||0)+1,a),{})));if(rows.some(r=>!['observed-change','unsupported','verified-equivalent'].includes(r.status)))process.exitCode=1;
})().catch(e=>{console.error(e);process.exitCode=1;});
