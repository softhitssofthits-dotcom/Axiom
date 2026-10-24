const fs=require('fs');const path=require('path');
const open=async S=>{await S.page.click('#menu-settings');await S.page.locator('.editor-settings-dialog').waitFor();};
const tab=async(S,name)=>S.page.click('#settings-tab-'+name);
const apply=async S=>{await S.page.click('[data-settings-action="apply"]');await S.page.waitForFunction(()=>!window.__ide.editorSettingsDialog.busy);};
module.exports=[
{id:'editor-settings-apply-persistence',name:'Диалог: отмена, применение, модели и сохранение после перезапуска',async fn({S,C}){
 await S.launch();
 C.eq(await S.page.locator('[data-menu="settings"]').count(),0,'Настройки не входят в выпадающие меню');
 const window=await S.electronApp.browserWindow(S.page),bounds=await window.evaluate(w=>w.getBounds());
 for(const width of [bounds.width,800]){
  await window.evaluate((w,width)=>w.setSize(width,w.getBounds().height),width);
  await S.page.waitForFunction(()=>{const button=document.getElementById('menu-settings'),group=button.closest('.studio-placement');return group&&button.getBoundingClientRect().width>0;});
  const layout=await S.page.evaluate(()=>{const button=document.getElementById('menu-settings'),group=button.closest('.studio-placement'),bar=document.getElementById('toolbar').getBoundingClientRect(),b=button.getBoundingClientRect(),g=group.getBoundingClientRect();return {placement:!!group,parentToolbar:group.parentElement.id,gap:g.right-b.right,inside:b.left>=bar.left&&b.right<=bar.right};});
  C.ok(layout.placement&&layout.parentToolbar==='toolbar'&&layout.gap>=0&&layout.gap<=10&&layout.inside,'Настройки доступны в правой группе панели: '+width+'px',JSON.stringify(layout));
  await open(S);await S.page.click('[data-settings-action="cancel"]');
 }
 await window.evaluate((w,bounds)=>w.setBounds(bounds),bounds);
 await S.page.locator('#menu-settings').focus();await S.page.keyboard.press('Enter');
 await S.page.locator('.editor-settings-dialog').waitFor();
 C.eq(await S.page.locator('.editor-settings-dialog').count(),1,'Enter сразу открывает диалог');
 await S.page.click('[data-settings-action="cancel"]');
 await open(S);await tab(S,'scroll');await S.page.uncheck('#setting-stickyScroll-enabled');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.stickyScroll).enabled),true,'Черновик не меняет редактор');
 await S.page.click('[data-settings-action="cancel"]');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.stickyScroll).enabled),true,'Отмена сохраняет прежнее закрепление');
 await open(S);await tab(S,'scroll');await S.page.uncheck('#setting-stickyScroll-enabled');await tab(S,'font');await S.page.fill('#setting-fontSize','20');
 C.eq(await S.page.locator('.settings-preview').evaluate(el=>el.style.fontSize),'20px','Предпросмотр шрифта отражает черновик');
 await tab(S,'typing');await S.page.uncheck('#setting-detectIndentation');await S.page.fill('#setting-tabSize','2');await S.page.uncheck('#setting-insertSpaces');await apply(S);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.stickyScroll).enabled),false,'Применение выключает Sticky Scroll');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontSize)),20,'Monaco использует выбранный шрифт');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getModel().getOptions().tabSize),2,'Табуляция обновлена в модели');
 await S.page.keyboard.press('Escape');await S.page.evaluate(()=>window.__ide.newFile());
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getModel().getOptions().insertSpaces),false,'Новая модель использует выбранный вид отступа');
 const profile=S.userData;await S.close();await S.launch({userData:profile});
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.stickyScroll).enabled),false,'Sticky Scroll восстановлен после перезапуска');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontSize)),20,'Шрифт восстановлен после перезапуска');
 C.eq(JSON.parse(fs.readFileSync(path.join(profile,'settings.json'),'utf8')).editorPreferences.tabSize,2,'Профиль содержит сохранённый отступ');
}},
{id:'editor-settings-language-keyboard-validation',name:'Вкладки, обе локали, все поля, четыре темы, поиск и ошибочный ввод',async fn({S,C}){
 await S.launch();await S.page.keyboard.press('Control+,');await S.page.locator('.editor-settings-dialog').waitFor();
 C.eq(await S.page.locator('#app').evaluate(el=>el.inert),true,'Фон диалога недоступен для ввода');
 for(const group of ['appearance','font','typing','suggestions','scroll','selection','structure','advanced']){
  await tab(S,group);
  const invalid=await S.page.evaluate(()=>{const d=window.__ide.editorSettingsDialog;return [...d.controls.values()].filter(({entry})=>!EditorPreferences.valid(entry,d.draft[entry.key])).map(({entry})=>entry.key);});
  C.eq(invalid.join(','),'','Вкладка содержит допустимые начальные значения: '+group);
  C.ok(await S.page.locator('[data-setting-input]').count()>0,'Поля вкладки доступны: '+group);
 }
 await S.page.fill('.settings-search','stickyScroll');C.eq(await S.page.locator('[data-setting-input]').count(),4,'Поиск находит параметры из другой вкладки');
 C.contains(await S.page.locator('#setting-stickyScroll-enabled').getAttribute('title'),'закреплённую область','Пояснение описывает назначение закрепления');
 await tab(S,'font');
 C.eq(await S.page.locator('#setting-fontFamily').getAttribute('list'),null,'Поле шрифта не использует фильтруемый datalist');
 C.eq(await S.page.locator('#setting-fontFamily').evaluate(el=>el.tagName),'SELECT','Шрифт выбирается цельным выпадающим полем');
 const sizes=await S.page.evaluate(()=>['settings-theme','setting-fontFamily','setting-fontWeight'].map(id=>{const r=document.getElementById(id).getBoundingClientRect();return {left:r.left,width:r.width,height:r.height};}));
 C.ok(sizes.every(r=>Math.abs(r.left-sizes[0].left)<1&&Math.abs(r.width-sizes[0].width)<1&&Math.abs(r.height-sizes[0].height)<1),'Шрифт и толщина совпадают с полем темы по размерам',JSON.stringify(sizes));
 await S.page.selectOption('#setting-fontFamily','custom');await S.page.locator('#setting-fontFamily').locator('..').locator('input').fill("'My Custom Font', Consolas, monospace");
 C.ok(await S.page.locator('#setting-fontFamily option').count()>=6,'Список шрифтов доступен при собственной строке');
 await S.page.selectOption('#setting-fontFamily','Consolas');
 C.eq(await S.page.locator('#setting-fontFamily').inputValue(),'Consolas','Выбор шрифта обновляет поле');
 C.eq(await S.page.locator('.settings-preview').evaluate(el=>el.style.fontFamily),'Consolas','Предпросмотр сразу меняет шрифт');
 await S.page.locator('#setting-fontWeight').focus();await S.page.keyboard.press('Home');
 C.eq(await S.page.locator('#setting-fontWeight').inputValue(),'normal','Толщина выбирается клавиатурой');
 await S.page.selectOption('#setting-fontWeight','bold');
 C.eq(await S.page.locator('.settings-preview').evaluate(el=>el.style.fontWeight),'bold','Предпросмотр меняет толщину');
 await apply(S);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontFamily)),'Consolas','Выбранный шрифт применяется к Monaco');
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontWeight)),'bold','Выбранная толщина применяется к Monaco');
 await S.page.screenshot({path:path.resolve(__dirname,'../../../.artifacts/font-dropdown.png')});
 await S.page.selectOption('#setting-fontFamily','custom');await S.page.locator('#setting-fontFamily').locator('..').locator('input').fill("'Custom Font', monospace");await apply(S);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontFamily)),"'Custom Font', monospace",'Собственный список запасных шрифтов сохраняется');
 await S.page.fill('#setting-fontSize','999');await tab(S,'scroll');await S.page.click('[data-settings-action="apply"]');
 C.contains(await S.page.locator('.settings-status').innerText(),'Размер шрифта','Ошибка сохранена при переходе между вкладками');
 C.eq(await S.page.locator('#setting-fontSize').getAttribute('aria-invalid'),'true','Неверное поле доступно для исправления');
 await S.page.fill('#setting-fontSize','16');await S.page.selectOption('#settings-theme','light');await apply(S);
 C.eq(await S.page.locator('html').getAttribute('data-theme'),'light','Светлая тема применена');
 await tab(S,'appearance');await S.page.selectOption('#settings-language','en');await apply(S);
 C.eq(await S.page.locator('#editor-settings-title').innerText(),'Axiom IDE Settings','Заголовок локализован после смены языка');
 C.eq(await S.page.locator('#settings-tab-scroll').innerText(),'Scrolling & Overview','Вкладка локализована');
 await tab(S,'font');await S.page.selectOption('#settings-theme','basic');await apply(S);
 C.eq(await S.page.locator('html').getAttribute('data-theme'),'basic','Тема QuickBASIC применена');
 await tab(S,'font');await S.page.selectOption('#settings-theme','borland');
 C.eq(await S.page.locator('.settings-preview').getAttribute('data-preview-theme'),'borland','Предпросмотр показывает Borland Pascal до применения');
 C.eq(await S.page.locator('.settings-preview').evaluate(el=>getComputedStyle(el).backgroundColor),'rgb(0, 0, 170)','Предпросмотр использует ярко-синий фон Borland Pascal');
 await apply(S);
 C.eq(await S.page.locator('html').getAttribute('data-theme'),'borland','Тема Borland Pascal применяется к IDE');
 await S.page.click('.settings-footer button:first-child');await apply(S);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontSize)),14,'Сброс вкладки возвращает шрифт Axiom');
 C.eq(await S.page.locator('html').getAttribute('data-theme'),'dark','Сброс возвращает тёмную тему');
 C.ok(await S.page.evaluate(()=>{const all=EditorPreferences.entries.flatMap(e=>e.choices);return all.every(v=>Object.hasOwn(EditorSettingsDialog.valueLabels,String(v)));}),'Все значения списков имеют русские и английские подписи');
 await S.page.locator('.settings-search').focus();await S.page.keyboard.press('Shift+Tab');C.eq(await S.page.locator('.settings-close').evaluate(el=>el===document.activeElement),true,'Tab перемещает фокус внутри окна');
 await S.page.keyboard.press('Shift+Tab');C.eq(await S.page.locator('[data-settings-action="cancel"]').evaluate(el=>el===document.activeElement),true,'Фокус не выходит за границы диалога');
 await S.screenshot('editor-settings-en');await S.page.keyboard.press('Escape');C.eq(await S.page.locator('#app').evaluate(el=>el.inert),false,'Escape возвращает доступ к редактору');
 C.eq(S.pageErrors.length,0,'Нет ошибок renderer');
}},
{id:'editor-settings-save-failure',name:'Ошибка записи профиля не применяется и не теряет черновик',async fn({S,C}){
 await S.launch();await open(S);await tab(S,'font');await S.page.fill('#setting-fontSize','21');
 const target=path.join(S.userData,'settings.json'),backup=target+'.backup';fs.renameSync(target,backup);fs.mkdirSync(target);
 try {
  await apply(S);C.contains(await S.page.locator('.settings-status').innerText(),'Не удалось сохранить','Ошибка записи объяснена пользователю');
  C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontSize)),14,'Ошибка записи не меняет редактор');
  C.eq(await S.page.locator('#setting-fontSize').inputValue(),'21','Черновик сохранён для повторной попытки');
 } finally {fs.rmdirSync(target);fs.renameSync(backup,target);}
 await apply(S);
 C.eq(await S.page.evaluate(()=>window.__ide.editor.getOption(monaco.editor.EditorOption.fontSize)),21,'Повторное применение успешно');
}}];
