const entries=require('../../src/renderer/editor-settings-catalog');
const {runOne,expectedCancellation,configureUI}=require('../editor-settings-live.cjs');
module.exports=entries.map(entry=>({id:'editor-settings-live-'+entry.key,name:entry.ru+' — отдельный сценарий штатной IDE',async fn({S,C}){
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.emulateMedia({forcedColors:null,colorScheme:null,reducedMotion:null});S.page.setDefaultTimeout(5000);
 const result=await runOne(S,entry);
 C.note(JSON.stringify(result));
 C.ok(require('../acceptance-results.cjs').acceptedSetting(result),entry.available===false?'Недоступный элемент бледный, отключён и объяснён':result.status==='verified-configuration'?'Применение настройки проверено; физическое действие отдельно не подтверждено':'Наблюдается результат действия настройки',result.status+' '+result.errors.join('\n'));
 if(result.limitation)C.limitations.push({scope:entry.key,status:'not-verified',reason:result.limitation});
 if(entry.key==='dropIntoEditor.enabled')await require('../editor-file-drop.cjs').checkFileDrop(S,C,entry,configureUI);
 C.eq(S.pageErrors.filter(e=>!expectedCancellation(e)).length,0,'Нет ошибок страницы');
}}));
