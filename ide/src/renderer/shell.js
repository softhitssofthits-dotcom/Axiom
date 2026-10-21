// Оформление сохраняет существующие элементы и обработчики команд.
(() => {
const icons = {"new":"M14 2H5v20h14V7L14 2M14 2v5h5M8 14h8M12 10v8","open":"M3 7h7l2 3h9l-3 10H3V7M3 7V4h7l2 3","save":"M4 3h14l3 3v15H3V3h1M7 3v6h10V3M7 21v-8h10v8","all":"M7 3h12l2 2v13H7V3M3 7v14h14M10 3v6h7V3M10 18v-6h7v6","compile":"M8 5 2 11l6 6M16 5l6 6-6 6M9 20l3 2 5-5","run":"M7 3v18l14-9L7 3","debug":"M9 4l-2-2M15 4l2-2M8 8h8v9a4 4 0 0 1-8 0V8M9 8V6a3 3 0 0 1 6 0v2M3 9h5M16 9h5M3 14h5M16 14h5M4 20l4-3M16 17l4 3M12 9v10","stop":"M5 5h14v14H5z","restart":"M4 9a8 8 0 1 1 0 7M4 3v6h6","over":"M3 12a9 9 0 0 1 18 0M17 8l4 4 2-4M12 17v3","into":"M12 2v13M7 10l5 5 5-5M11 21h2","out":"M12 16V3M7 8l5-5 5 5M11 21h2","back":"M20 12H5M10 7l-5 5 5 5M3 3v18","point":"M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18"};
Object.assign(icons,{format:'M4 5h16M4 10h12M4 15h16M4 20h12',search:'M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14M15 15l6 6',output:'M3 3h18v18H3zM6 8l3 3-3 3M11 15h6',sidebar:'M3 3h18v18H3zM9 3v18'});
const iconMarkup = key => icons[key] ? '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="'+icons[key]+'"/></svg>' : '<span class="codicon codicon-'+key+'" aria-hidden="true"></span>';
window.ShellIcons = Object.freeze({markup:iconMarkup});
const toolbar=document.getElementById('toolbar');
const buttons=[['tb-new','new'],['tb-open','open'],['tb-save','save'],['tb-check','check'],['tb-run','run'],['tb-stop','stop'],['tb-format','format'],['tb-find','search'],['tb-toggle-output','output']];
buttons.forEach(([id,key])=>{const b=document.getElementById(id);if(id==='tb-save'){b.querySelectorAll('span').forEach(s=>{s.className='tb-save-icon';s.innerHTML=iconMarkup(s.id==='tb-save-as'?'save-as':key);});}else b.innerHTML=iconMarkup(key);});
function add(id,key,label,action){const b=UIControls.createIconButton({id,label,markup:iconMarkup(key),onClick:action});toolbar.append(b);return b;}
const traceCommand=command=>window.__ide?.trace?.command(command);
add('tb-save-all','all',I18N.t('menu.file.saveAll'),()=>window.__ide.saveAll()).setAttribute('data-i18n-title','menu.file.saveAll');
for(const [command,key] of [['undo','discard'],['redo','redo']])
  add('tb-'+command,key,I18N.t('tb.'+command),()=>window.__ide.handleAction(command)).setAttribute('data-i18n-title','tb.'+command);
add('tb-toggle-sidebar','sidebar',I18N.t('menu.view.sidebar'),()=>window.__ide.handleAction('toggle-sidebar')).setAttribute('data-i18n-title','menu.view.sidebar');
const projectToggle=add('tb-toggle-project','list-tree',I18N.t('tb.project'),()=>window.__ide.handleAction('toggle-project-explorer'));
projectToggle.setAttribute('data-i18n-title','tb.project');
projectToggle.setAttribute('aria-controls','project-explorer');
const shortcuts={start:'F5',stepOver:'F10',stepInto:'F11',stepOut:'Shift+F11',stepBack:'Ctrl+F10',stop:'Shift+F5',restart:'Ctrl+Shift+F5',breakpoint:'F9',clear:'Ctrl+Shift+F9'};
const traceIcons={start:'debug',stepBack:'back',stepInto:'into',stepOver:'over',stepOut:'out',stop:'stop',restart:'restart',breakpoint:'point',clear:'clear-all'};
for(const [command,key] of Object.entries(traceIcons)) {
 const id=command==='start'?'tb-debug':'tb-'+command;
 const label=I18N.t(command==='stop'?'menu.run.stop':'trace.'+command);
 const button=command==='stop'?document.getElementById(id):add(id,key,label,()=>traceCommand(command));
 button.innerHTML=iconMarkup(key);button.dataset.traceCommand=command;
 UIControls.setEnabled(button,false);
}
// Одинаковые команды меню и панели используют единственное описание значка.
const menuIcons={new:'new',open:'open','open-project':'folder-opened',save:'save','save-all':'all','save-as':'save-as',undo:'discard',redo:'redo',check:'check',run:'run',stop:'stop',format:'format',find:'search'};
for(const [action,key] of Object.entries(menuIcons)) {
 const icon=document.querySelector('.menu-dropdown-item[data-action="'+action+'"] .menu-icon');
 if(icon){icon.className='menu-icon';icon.removeAttribute('style');icon.innerHTML=iconMarkup(key);}
}
const traceMenu=document.createElement('div');traceMenu.className='menu-item';traceMenu.id='menu-trace';
traceMenu.innerHTML='<span class="menu-label" data-i18n="menu.trace">'+I18N.t('menu.trace')+'</span><div class="menu-dropdown"></div>';
for(const command of Object.keys(traceIcons)) {
 const item=document.createElement('div');item.className='menu-dropdown-item';item.dataset.action='trace-'+command;item.dataset.traceCommand=command;
 const key=command==='stop'?'menu.run.stop':'trace.'+command;
 item.innerHTML='<span class="menu-icon" aria-hidden="true">'+iconMarkup(traceIcons[command])+'</span><span class="menu-text" data-i18n="'+key+'">'+I18N.t(key)+'</span><span class="menu-shortcut">'+shortcuts[command]+'</span>';
 UIControls.setEnabled(item,false);
 traceMenu.lastElementChild.append(item);
}
document.querySelector('[data-i18n="menu.view"]').parentElement.before(traceMenu);
function translateTrace(){
 toolbar.querySelectorAll('[data-trace-command]').forEach(b=>UIControls.setLabel(b,I18N.t(b.dataset.traceCommand==='stop'?'menu.run.stop':'trace.'+b.dataset.traceCommand)+' ('+shortcuts[b.dataset.traceCommand]+')'));
 window.__ide?.trace?.update();
 window.__ide?.studio?.updateCommandLabels();
}
new MutationObserver(translateTrace).observe(traceMenu.querySelector('.menu-label'),{childList:true});
translateTrace();
const groups=[['tb-new','tb-open','tb-save','tb-save-all'],['tb-undo','tb-redo'],['tb-check','tb-run'],Object.keys(traceIcons).map(command=>command==='start'?'tb-debug':'tb-'+command),['tb-format','tb-find','tb-toggle-sidebar','tb-toggle-project','tb-toggle-output']];
toolbar.querySelectorAll('.toolbar-sep').forEach(s=>s.remove());groups.forEach(ids=>{const group=document.createElement('div');group.className='command-group';ids.forEach(id=>group.append(document.getElementById(id)));toolbar.append(group);});
toolbar.querySelectorAll('button').forEach(b=>UIControls.setLabel(b,b.title));
const bar=document.getElementById('menubar');
const settings=document.getElementById('menu-settings');
const more=document.createElement('button');more.id='menu-overflow';more.textContent='•••';UIControls.setLabel(more,'Остальные меню');UIControls.setExpanded(more,false);bar.insertBefore(more,settings);
const overflow=document.createElement('div');overflow.id='menu-overflow-content';overflow.hidden=true;bar.append(overflow);
more.onclick=()=>{overflow.hidden=!overflow.hidden;UIControls.setExpanded(more,!overflow.hidden);};
function fit(){const menus=[...bar.querySelectorAll('.menu-item')].sort((a,b)=>Number(a.dataset.studioOrder||0)-Number(b.dataset.studioOrder||0));overflow.hidden=true;UIControls.setExpanded(more,false);menus.forEach(m=>bar.insertBefore(m,more));more.hidden=true;
const available=bar.clientWidth-16-settings.offsetWidth-(menus.length+1)*2;let used=menus.reduce((n,m)=>n+m.offsetWidth,0);
if(used>available){more.hidden=false;for(let i=menus.length-1;i>=0&&used>available-more.offsetWidth;i--){used-=menus[i].offsetWidth;overflow.prepend(menus[i]);}}
}
const menuResize=new ResizeObserver(fit);menuResize.observe(bar);menuResize.observe(settings);
window.refreshShellMenus=fit;
document.addEventListener('mousedown',e=>{if(!bar.contains(e.target)){overflow.hidden=true;UIControls.setExpanded(more,false);}});
document.addEventListener('keydown',e=>{if(e.key==='Escape'){overflow.hidden=true;UIControls.setExpanded(more,false);}});
// Клавиши с модификаторами проверяются до прежнего обработчика F5.
document.querySelector('#tb-run').title='Запустить (Ctrl+F5)';
// Структура переносится в левую область без замены её навигации.
const sidebar=document.createElement('aside');sidebar.id='shell-sidebar';
const files=document.getElementById('project-explorer');files.before(sidebar);sidebar.append(files);
const title=document.createElement('div');title.className='shell-section-title';
const heading=document.createElement('span');heading.dataset.i18n='panel.structure';heading.textContent=I18N.t('panel.structure');title.append(heading);
const close=document.createElement('button');close.id='sidebar-close';close.textContent='×';close.dataset.i18nTitle='sidebar.close';UIControls.setLabel(close,I18N.t('sidebar.close'));title.append(close);sidebar.prepend(title);
const splitter=document.createElement('div');splitter.id='sidebar-resizer';splitter.dataset.i18nTitle='sidebar.resizeHint';splitter.title=I18N.t('sidebar.resizeHint');sidebar.after(splitter);
sidebar.append(document.getElementById('structure-content'));
const structureTab=document.querySelector('[data-tab="structure"]');if(structureTab)structureTab.hidden=true;
fit();
})();
