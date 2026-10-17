const {testWorkspace,writeAxm}=require('../helpers');
const source='DIM value AS INTEGER = 1\nChange()\nPRINT value\n\nSUB Change()\nvalue = value + 4\nvalue = value * 2\nEND SUB\n';
async function open(S){const file=writeAxm(testWorkspace('trace'),'шаги.axm',source);await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);return file;}
async function paused(S,line){await S.page.waitForFunction(n=>!!window.__ide.execution.stopId && !window.__ide.execution.debugBusy && window.__ide.execution.line===n,line);}
async function menu(S,command){await S.page.locator('#menu-trace > .menu-label').click();await S.page.locator(`#menu-trace [data-action="trace-${command}"]`).click();}
module.exports=[
{id:'trace-type-self-stepback',name:'Шаг назад восстанавливает поле TYPE, изменённое методом через адресуемый SELF',async fn({S,C}){
const source='TYPE Cell\n    Value AS INTEGER = 1\n\n    SUB Change()\n        SELF.Value = SELF.Value + 4\n        SELF.Value = SELF.Value * 2\n    END SUB\nEND TYPE\n\nDIM cells(1 TO 1) AS Cell\ncells(1).Change()\nPRINT cells(1).Value\n';
const file=writeAxm(testWorkspace('trace-type-self'),'тип-шаг-назад.axm',source);
await S.launch();await S.page.evaluate(p=>window.__ide.openFile(p),file);
await S.page.evaluate(()=>{const i=window.__ide;i.settings.formatOnBlur=false;i.settings.checkOnChange=false;});
await S.page.evaluate(()=>window.__ide.run('debug'));
await S.page.waitForFunction(()=>!!window.__ide.execution.stopId&&!window.__ide.execution.debugBusy);
async function advance(command){
const prior=await S.page.evaluate(()=>window.__ide.execution.stopId);
await S.page.evaluate(c=>window.__ide.trace.command(c),command);
await S.page.waitForFunction(stop=>window.__ide.execution.stopId&&window.__ide.execution.stopId!==stop&&!window.__ide.execution.debugBusy,prior);
return {line:await S.page.evaluate(()=>window.__ide.execution.line),stop:await S.page.evaluate(()=>window.__ide.execution.stopId)};
}
for(let guard=0;guard<12&&await S.page.evaluate(()=>window.__ide.execution.line)!==11;guard++)await advance('stepOver');
C.eq(await S.page.evaluate(()=>window.__ide.execution.line),11,'Обычные шаги дошли до вызова метода');
const entered=await advance('stepInto');
C.eq(entered.line,5,'Шаг с заходом открыл тело метода TYPE');
const firstStop=entered.stop;
const second=await advance('stepOver');
C.eq(second.line,6,'Шаг выполнил первое изменение поля');
C.ok(second.stop!==firstStop,'Шаг вперёд создал новую остановку');
C.ok(await S.page.evaluate(()=>window.__ide.execution.canStepBack),'Остановка внутри метода имеет обратимую историю');
const backed=await advance('stepBack');
C.eq(backed.line,5,'Откат вернул первую строку изменения поля');
C.eq((await S.diagnosticItems()).length,0,'Откат TYPE не создал диагностик');
const replay=await advance('stepOver');
C.eq(replay.line,6,'Повторный шаг снова выполнил первое изменение поля');
await S.page.evaluate(()=>window.__ide.execution.debug('continue'));
await S.waitIdle();await S.waitConsole('10');
C.ok(true,'Повторное выполнение восстановило исходное поле: результат 10, а не 18');
}},
{id:'trace-context-menu',name:'Прежние команды точек остановки в контекстном меню ru/en',async fn({S,C}){
await open(S);
async function context(line){
 const point=await S.page.evaluate(n=>{const ide=window.__ide;ide.editor.revealLineInCenter(n);const p=ide.editor.getScrolledVisiblePosition({lineNumber:n,column:2});const r=ide.editor.getDomNode().getBoundingClientRect();return {x:r.x+p.left,y:r.y+p.top+p.height/2};},line);
 await S.page.mouse.click(point.x,point.y,{button:'right'});
}
await context(2);await S.page.getByRole('menuitem',{name:'Установить / снять точку остановки',exact:true}).click();
C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).join(',')),'2','Правая кнопка установила точку на выбранной строке');
await context(2);await S.page.getByRole('menuitem',{name:'Установить / снять точку остановки',exact:true}).click();
C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).length),0,'Повтор снял точку');
await S.page.evaluate(()=>window.__ide.handleAction('lang-en'));await context(2);
await S.page.getByRole('menuitem',{name:'Toggle Breakpoint',exact:true}).click();
C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).join(',')),'2','Английский пункт устанавливает точку');
await context(2);await S.page.getByRole('menuitem',{name:'Clear All Breakpoints',exact:true}).click();
C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).length),0,'Английский пункт очищает точки');
await S.page.evaluate(()=>window.__ide.handleAction('lang-ru'));await context(2);
C.ok(await S.page.getByRole('menuitem',{name:'Очистить все точки остановки',exact:true}).isVisible(),'Перевод вернулся на русский');await S.page.keyboard.press('Escape');
}},
{id:'trace-function-and-loop',name:'Функция, другая вкладка и повторная точка в цикле',async fn({S,C}){
await open(S);const file=writeAxm(testWorkspace('trace-function'),'функция.axm','DIM result AS INTEGER = Twice(3)\nPRINT result\n\nFUNCTION Twice(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION\n');
await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
await S.page.keyboard.press('F11');await paused(S,1);await S.page.keyboard.press('F11');await paused(S,5);C.ok(true,'Заход внутрь функции');
const other=await S.page.evaluate(()=>[...window.__ide.openFiles.keys()].find(p=>p!==window.__ide._runFile));await S.page.evaluate(p=>window.__ide.switchToFile(p),other);
const prior=await S.page.evaluate(()=>window.__ide.execution.stopId);
await menu(S,'stepOut');await S.page.waitForFunction(id=>window.__ide.execution.stopId && window.__ide.execution.stopId!==id && !window.__ide.execution.debugBusy,prior);
if(await S.page.evaluate(()=>window.__ide.execution.line)===1){await S.page.keyboard.press('F10');}
await paused(S,2);C.eq(await S.page.evaluate(()=>window.__ide.activeFile),file,'Остановка вернула выполняемый файл');
C.ok(await S.page.evaluate(()=>window.__ide.editor.getOption(window.__ide.monaco.editor.EditorOption.readOnly)),'Исполняемый снимок защищён от редактирования');
await S.page.keyboard.press('F5');await S.waitIdle();await S.waitConsole('6');C.ok(true,'Функция вернула правильное значение');
const loop=writeAxm(testWorkspace('trace-loop'),'цикл.axm','FOR index = 1 TO 2\nPRINT index\nNEXT\n');await S.stubOpenDialog([loop]);await S.page.evaluate(()=>window.__ide.handleAction('open'));await S.page.waitForFunction(p=>window.__ide.activeFile===p,loop);
await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:2,column:1}));await S.page.keyboard.press('F9');await S.page.keyboard.press('F5');await paused(S,2);
const stop=await S.page.evaluate(()=>window.__ide.execution.stopId);await S.page.keyboard.press('F5');await S.page.waitForFunction(id=>window.__ide.execution.stopId && window.__ide.execution.stopId!==id,stop);await paused(S,2);await S.waitConsole('1');C.ok(true,'Та же точка сработала на втором проходе');
await S.page.keyboard.press('F5');await S.waitIdle();await S.waitConsole('2');C.ok(true,'Продолжение не зациклилось на одной точке');
}},
{id:'trace-all-steps',name:'Все шаги трассировки, откат и обход вызова',async fn({S,C}){
await open(S);await S.page.keyboard.press('F10');await paused(S,1);C.ok(true,'F10 начинает трассировку с первой строки');
await menu(S,'stepOver');await paused(S,2);await S.page.locator('#tb-stepInto').click();await paused(S,6);C.ok(true,'Заход открыл тело процедуры');
await S.page.keyboard.press('F10');await paused(S,7);await S.page.keyboard.press('Control+F10');await paused(S,6);C.ok(true,'Шаг назад вернул предыдущую строку');
await S.page.keyboard.press('Shift+F11');await paused(S,3);C.ok(true,'Выход вернул строку после вызова');
await S.page.keyboard.press('F5');await S.waitIdle().catch(async e=>{throw new Error(e.message+' '+JSON.stringify(await S.page.evaluate(()=>({stop:window.__ide.execution.stopId,line:window.__ide.execution.line,status:document.getElementById('status-text').textContent}))));});await S.waitConsole('10');C.ok(true,'Откат восстановил значение: результат 10');
await S.page.keyboard.press('F11');await paused(S,1);await S.page.keyboard.press('F10');await paused(S,2);await S.page.keyboard.press('F10');await paused(S,3);C.ok(true,'Обход исполнил процедуру целиком');
await S.page.keyboard.press('Shift+F5');await S.waitIdle();await S.page.waitForFunction(()=>!document.querySelector('.trace-current-line'));C.eq(await S.page.locator('.trace-current-line').count(),0,'Остановка убирает отметку строки');
}},
{id:'trace-breakpoints-restart-language',name:'Точки остановки, продолжение, перезапуск и перевод',async fn({S,C}){
await open(S);
for(const line of [2,7]){await S.page.evaluate(n=>window.__ide.editor.setPosition({lineNumber:n,column:1}),line);await S.page.keyboard.press('F9');}
C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).join(',')),'2,7','F9 установил две точки');
await menu(S,'start');await paused(S,2);C.ok(true,'Запуск дошёл до выбранной точки, не первой строки');
await S.page.keyboard.press('F5');await paused(S,7);C.ok(true,'Продолжение дошло до точки внутри процедуры');
await menu(S,'restart');await paused(S,2);C.ok(true,'Перезапуск снова дошёл до первой точки');
await S.page.evaluate(()=>window.__ide.handleAction('lang-en'));
for(const [id,title] of [['tb-debug','Start / Continue (F5)'],['tb-stepOver','Step Over (F10)'],['tb-stepInto','Step Into (F11)'],['tb-stepOut','Step Out (Shift+F11)'],['tb-stepBack','Step Back (Ctrl+F10)']])C.eq(await S.page.locator('#'+id).getAttribute('title'),title,'Подсказка '+id);
C.eq(await S.page.locator('#menu-trace > .menu-label').textContent(),'Debug','Заголовок переведён');
C.ok((await S.page.locator('#status-text').textContent()).includes('Debug paused: line 2'),'Пауза переведена');
await S.page.keyboard.press('Shift+F5');await S.waitIdle();await S.page.keyboard.press('Control+Shift+F9');C.eq(await S.page.evaluate(()=>window.__ide.trace.lines(window.__ide.editor.getModel()).length),0,'Очистка всех точек');
await S.page.keyboard.press('Control+F5');await S.waitIdle();await S.waitConsole('10');C.ok(true,'Обычный запуск завершён без остановки');
}}
];
