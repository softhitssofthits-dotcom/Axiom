const entries=require('../../src/renderer/editor-settings-catalog');
const {runOne,expectedCancellation}=require('../editor-settings-live.cjs');
module.exports=entries.map(entry=>({id:'editor-settings-live-'+entry.key,name:entry.ru+' — отдельный сценарий штатной IDE',async fn({S,C}){
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.emulateMedia({forcedColors:null,colorScheme:null,reducedMotion:null});S.page.setDefaultTimeout(5000);
 const result=await runOne(S,entry);
 C.note(JSON.stringify(result));
 C.ok(['observed-change','unsupported','verified-equivalent'].includes(result.status),entry.available===false?'Недоступный элемент бледный, отключён и объяснён':'Наблюдается результат действия настройки',result.status+' '+result.errors.join('\n'));
 C.eq(S.pageErrors.filter(e=>!expectedCancellation(e)).length,0,'Нет ошибок страницы');
}}));
