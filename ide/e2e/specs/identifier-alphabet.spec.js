const {testWorkspace,writeAxm}=require('../helpers');
module.exports=[{id:'identifier-alphabet-diagnostics',name:'Русские имена запрещены, строки и комментарии разрешены; диагностика ru/en',async fn({S,C}){
 const file=writeAxm(testWorkspace('identifier-alphabet'),'Имена.axm','DIM aя AS INTEGER\naя = 67\nPRINT aя\n');
 await S.launch();await S.stubOpenDialog([file]);await S.page.evaluate(()=>window.__ide.handleAction('open'));
 await S.page.waitForFunction(p=>window.__ide.activeFile===p,file);
 await S.page.evaluate(()=>window.__ide.check(false));
 C.ok((await S.markers()).some(d=>/MB0001/.test(d.message)),'Смешанное имя подчёркнуто ошибкой');
 const line=await S.page.evaluate(()=>window.__ide.monaco.editor.tokenize('DIM aя AS INTEGER','axiom')[0]);
 C.ok(line.some(t=>t.type.startsWith('invalid')),'Недопустимая буква имеет отдельную подсветку');
 await S.page.evaluate(()=>window.__ide.setLanguage('en'));
 C.ok((await S.markers()).some(d=>d.message.includes('Invalid character.')),'Ошибка по-английски');
 await S.page.evaluate(()=>window.__ide.setLanguage('ru'));
 C.ok((await S.markers()).some(d=>d.message.includes('Недопустимый символ')),'Ошибка по-русски');
 const valid='DIM a AS INTEGER = 67\nCONST Limit AS INTEGER = 100\nPRINT "Русский текст", a \' Комментарий\n';
 await S.page.evaluate(source=>window.__ide.editor.getModel().setValue(source),valid);await S.page.evaluate(()=>window.__ide.check(false));
 C.eq((await S.markers()).length,0,'Строки и комментарии с русскими буквами допустимы');
 await S.page.evaluate(()=>window.__ide.save());await S.page.evaluate(()=>window.__ide.run());await S.waitIdle();await S.waitConsole('Русский текст');C.ok(true,'Русский текст реально выведен');
}}];
