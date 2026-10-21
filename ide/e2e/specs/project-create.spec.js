const fs=require('node:fs'),path=require('node:path');
const {testWorkspace,writeAxm}=require('../helpers');
async function begin(S,C){
 const item=S.page.locator('[data-menu="project"] [data-action="create-project"]');
 C.eq(await item.count(),1,'Создание проекта находится в меню Проект');
 if(!await item.count())return false;
 C.eq(await S.page.locator('[data-menu="file"] [data-action="open-project"]').count(),0,'Меню Файл содержит только операции отдельных документов');
 await S.clickMenuItem('project','create-project');return true;
}
async function location(S,parent){await S.stubOpenDialog([parent]);await S.page.locator('#project-create-browse').click();await S.page.waitForFunction(p=>document.getElementById('project-create-parent').value===p,parent);}
module.exports=[{
 id:'project-create-new-folder',name:'Создать проект: новая папка и пустой main без потери прежней вкладки',async fn({S,C}){
  const parent=testWorkspace('project-new'),target=path.join(parent,'Мой проект');
  await S.launch({settings:{smartIdentifierEnabled:false}});
  await S.page.evaluate(()=>window.__ide.editor.executeEdits('test',[{range:new window.__ide.monaco.Range(1,1,1,1),text:'PRINT 42\n'}]));
  const old=await S.page.evaluate(()=>window.__ide.activeFile);
  if(!await begin(S,C))return;
  await S.page.locator('#project-create-name').fill('Мой проект');await location(S,parent);
  await S.page.locator('#project-create-submit').click();await S.page.waitForFunction(p=>window.__ide.projectDir===p,target);
  C.eq(fs.readFileSync(path.join(target,'main.axm'),'utf8'),'','Создан пустой main.axm');
  C.eq(await S.page.locator('#project-entry').inputValue(),path.join(target,'main.axm'),'main выбран запускным');
  C.eq(await S.page.locator('#project-title').textContent(),'Мой проект','Заголовок показывает имя новой папки');
  C.eq(await S.page.evaluate(p=>window.__ide.openFiles.get(p)?.model.getValue().replace(/\r\n/g,'\n'),old),'PRINT 42\n','Прежний несохранённый документ сохранён');
  C.eq(await S.page.locator('#project-create-dialog').count(),0,'Успешное создание закрывает диалог');
  const profile=S.userData;await S.close();await S.launch({userData:profile});
  C.eq(await S.page.evaluate(()=>window.__ide.projectDir),target,'Новый проект восстанавливается после перезапуска');
  C.eq(S.pageErrors.length,0,'Ошибок страницы нет');
 }
},{
 id:'project-create-conflict-and-retry',name:'Существующая папка не перезаписывается, исправленное имя создаёт проект',async fn({S,C}){
  const parent=testWorkspace('project-conflict'),existing=path.join(parent,'existing');fs.mkdirSync(existing);const main=writeAxm(existing,'main.axm','PRINT "KEEP"\n');
  await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),existing);
  if(!await begin(S,C))return;
  await location(S,parent);await S.page.locator('#project-create-name').fill('existing');await S.page.locator('#project-create-submit').click();
  await S.page.waitForFunction(()=>document.getElementById('project-create-error').textContent.length>0);
  C.eq(fs.readFileSync(main,'utf8'),'PRINT "KEEP"\n','Содержимое существующего проекта не перезаписано');
  C.eq(await S.page.evaluate(()=>window.__ide.projectDir),existing,'Отказ не меняет открытый проект');
  C.eq(await S.page.locator('#project-create-dialog').isVisible(),true,'Диалог остаётся доступен для исправления');
  await S.page.locator('#project-create-name').fill('../escape');await S.page.locator('#project-create-submit').click();
  await S.page.waitForFunction(()=>!document.getElementById('project-create-submit').disabled&&document.getElementById('project-create-error').textContent.length>0);
  C.eq(fs.existsSync(path.join(parent,'escape')),false,'Нельзя создать проект через выход из выбранной папки');
  await S.page.locator('#project-create-name').fill('corrected');await S.page.locator('#project-create-submit').click();
  await S.page.waitForFunction(p=>window.__ide.projectDir===p,path.join(parent,'corrected'));
  C.eq(fs.existsSync(path.join(parent,'corrected','main.axm')),true,'После исправления доступна повторная попытка');
  C.eq(fs.readFileSync(main,'utf8'),'PRINT "KEEP"\n','Прежний проект сохранён после новой попытки');
  C.eq(S.pageErrors.length,0,'Отказ и повтор без ошибок страницы');
 }
},{
 id:'project-create-cancel',name:'Отмена создания не меняет проект и не создаёт папку, диалог переводится',async fn({S,C}){
  const parent=testWorkspace('project-cancel');await S.launch({settings:{language:'en',smartIdentifierEnabled:false}});
  if(!await begin(S,C))return;
  C.eq(await S.page.locator('#project-create-dialog h2').textContent(),'Create project','Заголовок переведён');
  await location(S,parent);await S.page.locator('#project-create-name').fill('cancelled');
  await S.page.keyboard.press('Escape');C.eq(await S.page.locator('#project-create-dialog').count(),0,'Escape закрывает диалог');
  C.eq(fs.existsSync(path.join(parent,'cancelled')),false,'Отмена не создаёт папку');
  C.eq(await S.page.evaluate(()=>window.__ide.projectDir),null,'Отмена не открывает проект');
  C.eq(S.pageErrors.length,0,'Отмена без ошибок страницы');
 }
}];
