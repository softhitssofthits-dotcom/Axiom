// Оформление сохраняет существующие элементы и обработчики команд.
(() => {
const icons = {"new":"M14 2H5v20h14V7L14 2M14 2v5h5M8 14h8M12 10v8","open":"M3 7h7l2 3h9l-3 10H3V7M3 7V4h7l2 3","save":"M4 3h14l3 3v15H3V3h1M7 3v6h10V3M7 21v-8h10v8","all":"M7 3h12l2 2v13H7V3M3 7v14h14M10 3v6h7V3M10 18v-6h7v6","compile":"M8 5 2 11l6 6M16 5l6 6-6 6M9 20l3 2 5-5","run":"M7 3v18l14-9L7 3","debug":"M9 4l-2-2M15 4l2-2M8 8h8v9a4 4 0 0 1-8 0V8M9 8V6a3 3 0 0 1 6 0v2M3 9h5M16 9h5M3 14h5M16 14h5M4 20l4-3M16 17l4 3M12 9v10","stop":"M5 5h14v14H5z","restart":"M4 9a8 8 0 1 1 0 7M4 3v6h6","over":"M3 12a9 9 0 0 1 18 0M17 8l4 4 2-4M12 17v3","into":"M12 2v13M7 10l5 5 5-5M11 21h2","out":"M12 16V3M7 8l5-5 5 5M11 21h2","back":"M20 12H5M10 7l-5 5 5 5M3 3v18","point":"M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18"};
Object.assign(icons,{format:'M4 5h16M4 10h12M4 15h16M4 20h12',search:'M10 3a7 7 0 1 0 0 14 7 7 0 0 0 0-14M15 15l6 6',output:'M3 3h18v18H3zM6 8l3 3-3 3M11 15h6',sidebar:'M3 3h18v18H3zM9 3v18'});
const svg = key => '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="'+icons[key]+'"/></svg>';
const toolbar=document.getElementById('toolbar');
const buttons=[['tb-new','new'],['tb-open','open'],['tb-save','save'],['tb-check','compile'],['tb-run','run'],['tb-stop','stop'],['tb-format','format'],['tb-find','search'],['tb-toggle-output','output']];
buttons.forEach(([id,key])=>{const b=document.getElementById(id);if(id==='tb-save'){b.querySelectorAll('span').forEach(s=>{s.className='tb-save-icon';s.innerHTML=svg(key);});}else b.innerHTML=svg(key);});
function add(id,key,label,action){const b=document.createElement('button');b.id=id;b.className='toolbar-btn';b.title=label;b.setAttribute('aria-label',label);b.innerHTML=svg(key);b.onclick=action;toolbar.append(b);return b;}
const startDebug=()=>window.__ide.execution.stopId?window.__ide.execution.debug('continue'):window.__ide.run('debug');
add('tb-save-all','all',I18N.t('menu.file.saveAll'),()=>window.__ide.saveAll()).setAttribute('data-i18n-title','menu.file.saveAll');
add('tb-toggle-sidebar','sidebar',I18N.t('menu.view.sidebar'),()=>window.__ide.handleAction('toggle-sidebar')).setAttribute('data-i18n-title','menu.view.sidebar');
add('tb-debug','debug','Отладка (F5)',startDebug);
for(const [key,command,label] of [['over','stepOver','Шаг с обходом (F10)'],['into','stepInto','Шаг с заходом (F11)'],['out','stepOut','Шаг с выходом (Shift+F11)'],['back','stepBack','Шаг назад (Ctrl+F10)']])add('tb-'+command,key,label,()=>window.__ide.execution.debug(command));
const stepButtons=[...toolbar.querySelectorAll('[id^="tb-step"]')];
stepButtons.forEach(b=>b.disabled=true);document.getElementById('tb-stop').disabled=true;
window.axiom.onBackendEvent(m=>{if(m.event==='execution.state')document.getElementById('tb-stop').disabled=false;
if(m.event==='debug.paused')stepButtons.forEach(b=>b.disabled=b.id==='tb-stepBack'&&!m.payload.canStepBack);
if(m.event==='execution.finished'){stepButtons.forEach(b=>b.disabled=true);document.getElementById('tb-stop').disabled=true;}});
const groups=[['tb-new','tb-open','tb-save','tb-save-all'],['tb-check','tb-run','tb-debug','tb-stop'],['tb-stepOver','tb-stepInto','tb-stepOut','tb-stepBack'],['tb-format','tb-find','tb-toggle-sidebar','tb-toggle-output']];
toolbar.querySelectorAll('.toolbar-sep').forEach(s=>s.remove());groups.forEach(ids=>{const group=document.createElement('div');group.className='command-group';ids.forEach(id=>group.append(document.getElementById(id)));toolbar.append(group);});
toolbar.querySelectorAll('button').forEach(b=>b.setAttribute('aria-label',b.title));
const bar=document.getElementById('menubar'),menus=[...bar.querySelectorAll(':scope > .menu-item')];
const more=document.createElement('button');more.id='menu-overflow';more.textContent='•••';more.setAttribute('aria-label','Остальные меню');more.setAttribute('aria-expanded','false');bar.append(more);
const overflow=document.createElement('div');overflow.id='menu-overflow-content';overflow.hidden=true;bar.append(overflow);
more.onclick=()=>{overflow.hidden=!overflow.hidden;more.setAttribute('aria-expanded',String(!overflow.hidden));};
function fit(){overflow.hidden=true;more.setAttribute('aria-expanded','false');menus.forEach(m=>bar.insertBefore(m,more));more.hidden=true;
const available=bar.clientWidth-16;let used=menus.reduce((n,m)=>n+m.offsetWidth,0);
if(used>available){more.hidden=false;for(let i=menus.length-1;i>=0&&used>available-more.offsetWidth;i--){used-=menus[i].offsetWidth;overflow.prepend(menus[i]);}}
}
new ResizeObserver(fit).observe(bar);
document.addEventListener('mousedown',e=>{if(!bar.contains(e.target)){overflow.hidden=true;more.setAttribute('aria-expanded','false');}});
document.addEventListener('keydown',e=>{if(e.key==='Escape'){overflow.hidden=true;more.setAttribute('aria-expanded','false');}});
// Клавиши с модификаторами проверяются до прежнего обработчика F5.
document.addEventListener('keydown',e=>{let action=null;if(e.key==='F5'&&!e.shiftKey)action=e.ctrlKey?()=>window.__ide.run('run'):startDebug;
else if(e.key==='F10')action=()=>window.__ide.execution.debug(e.ctrlKey?'stepBack':'stepOver');
else if(e.key==='F11')action=()=>window.__ide.execution.debug(e.shiftKey?'stepOut':'stepInto');
if(action){e.preventDefault();e.stopImmediatePropagation();action();}},true);
document.querySelector('#tb-run').title='Запустить (Ctrl+F5)';
// Структура переносится в левую область без замены её навигации.
const sidebar=document.createElement('aside');sidebar.id='shell-sidebar';
const files=document.getElementById('project-explorer');files.before(sidebar);sidebar.append(files);
const title=document.createElement('div');title.className='shell-section-title';
const heading=document.createElement('span');heading.dataset.i18n='panel.structure';heading.textContent=I18N.t('panel.structure');title.append(heading);
const close=document.createElement('button');close.id='sidebar-close';close.textContent='×';close.dataset.i18nTitle='sidebar.close';close.title=I18N.t('sidebar.close');close.setAttribute('aria-label',close.title);title.append(close);sidebar.prepend(title);
const splitter=document.createElement('div');splitter.id='sidebar-resizer';splitter.dataset.i18nTitle='sidebar.resizeHint';splitter.title=I18N.t('sidebar.resizeHint');sidebar.after(splitter);
sidebar.append(document.getElementById('structure-content'));
const structureTab=document.querySelector('[data-tab="structure"]');if(structureTab)structureTab.hidden=true;
fit();
})();
