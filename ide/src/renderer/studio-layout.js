// Размещение инструментов не меняет документы, запуск и навигацию языка.
(function(root) {
  const byId=id=>document.getElementById(id);
  const text=(id,key)=>{const el=byId(id);if(el)el.textContent=I18N.t(key);};
  function closeCommands(more,content){
    const close=()=>{const focused=content.contains(document.activeElement);more.open=false;if(focused)more.querySelector('summary').focus();};
    content.addEventListener('click',e=>{if(e.target.closest('button'))close();});
    more.addEventListener('keydown',e=>{if(e.key==='Escape'&&more.open){e.preventDefault();e.stopPropagation();close();}});
    document.addEventListener('pointerdown',e=>{if(more.open&&!more.contains(e.target))more.open=false;});
  }
  function button(id,key,action,icon) {
    const el=UIControls.createIconButton({id,label:I18N.t(key),markup:icon?ShellIcons.markup(icon):'',onClick:action});
    el.dataset.studioLabel=key;return el;
  }
  class StudioLayout {
    static prepare() {
      byId('app').classList.add('studio-shell');
      const sidebar=byId('shell-sidebar'),heading=sidebar.querySelector('.shell-section-title span');
      heading.id='studio-sidebar-title';heading.removeAttribute('data-i18n');
      const pin=button('sidebar-pin','studio.pin',()=>root.__ide.studio.togglePin(),'pin');
      byId('sidebar-close').before(pin);
      const tabs=document.createElement('div');tabs.id='sidebar-tabs';tabs.setAttribute('role','tablist');
      for(const name of ['project','outline']){
        const tab=document.createElement('button');tab.type='button';tab.id='sidebar-tab-'+name;tab.dataset.side=name;tab.setAttribute('role','tab');
        tab.setAttribute('aria-controls',name==='project'?'project-explorer':'structure-content');
        tab.onclick=()=>root.__ide.studio.select(name);tabs.append(tab);
        const panel=byId(name==='project'?'project-explorer':'structure-content');
        panel.setAttribute('role','tabpanel');panel.setAttribute('aria-labelledby',tab.id);
      }
      tabs.addEventListener('keydown',e=>{
        if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;
        e.preventDefault();const name=e.key==='Home'?'project':e.key==='End'?'outline':e.target.dataset.side==='project'?'outline':'project';
        root.__ide.studio.select(name);byId('sidebar-tab-'+name).focus();
      });
      sidebar.append(tabs);
      const rail=document.createElement('nav');rail.id='sidebar-rail';
      for(const name of ['project','outline']){
        const tab=document.createElement('button');tab.type='button';tab.className='sidebar-return';tab.dataset.side=name;
        tab.onclick=()=>root.__ide.studio.select(name);rail.append(tab);
      }
      sidebar.before(rail);
      const doc=document.createElement('div');doc.id='structure-document-name';byId('structure-content').prepend(doc);
      const tools=document.createElement('div');tools.id='project-tools';
      tools.append(button('project-create','menu.file.newProjectFile',()=>root.__ide.newProjectFile(),'new'),
        button('project-refresh','studio.refreshProject',()=>root.__ide.refreshProjectFiles(),'refresh'),
        button('project-collapse','studio.collapseProject',()=>root.__ide.setProjectFilesExpanded(!!root.__ide.projectFilesCollapsed),'fold'));
      byId('project-explorer-header').before(tools);
      byId('project-new-file').hidden=true;
      byId('project-explorer-close').hidden=true;
      const folder=document.createElement('span');folder.className='codicon codicon-folder studio-folder';folder.setAttribute('aria-hidden','true');byId('project-title').before(folder);
      byId('project-entry-controls').querySelector('label').dataset.i18n='studio.entry';
      const toolbar=byId('toolbar');
      const runGroup=document.createElement('div');runGroup.className='command-group studio-run-group';
      runGroup.append(byId('project-entry-controls'));
      for(const [id,key] of [['tb-run','studio.run'],['tb-debug','studio.debug'],['tb-check','studio.check']]){
        const el=byId(id),label=document.createElement('span');label.className='studio-command-label';label.dataset.studioText=key;el.append(label);runGroup.append(el);
      }
      const debugTools=document.createElement('div');debugTools.id='studio-debug-tools';debugTools.className='command-group';
      for(const id of ['tb-stop','tb-restart','tb-stepOver','tb-stepInto','tb-stepOut','tb-stepBack'])debugTools.append(byId(id));
      const more=document.createElement('details');more.id='toolbar-overflow';
      const summary=document.createElement('summary');summary.innerHTML=ShellIcons.markup('ellipsis');summary.dataset.studioLabel='studio.moreCommands';more.append(summary);
      const other=document.createElement('div');other.className='studio-extra-commands';
      for(const id of ['tb-format','tb-find','tb-toggle-project','tb-breakpoint','tb-clear']){
        const el=byId(id);const label=document.createElement('span');label.className='studio-overflow-label';el.append(label);other.append(el);
      }
      more.append(other);
      closeCommands(more,other);
      const placement=document.createElement('div');placement.className='command-group studio-placement';
      const settings=byId('menu-settings');settings.hidden=false;settings.className='toolbar-btn';settings.removeAttribute('data-i18n');settings.innerHTML=ShellIcons.markup('settings-gear');
      placement.append(byId('tb-toggle-sidebar'),byId('tb-toggle-output'),settings);
      const groups=[...toolbar.querySelectorAll(':scope > .command-group')];
      groups.forEach((group,i)=>{if(i>1)group.remove();});
      toolbar.append(runGroup,debugTools,more,placement);
      byId('panel-tabs').append(button('studio-panel-close','studio.hidePanel',()=>root.__ide.setBottomPanelVisible(false),'close'));
      byId('panel-tabs').setAttribute('role','tablist');
      byId('panel-tabs').addEventListener('keydown',e=>{
        if(!e.target.matches('.panel-tab')||!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;
        e.preventDefault();const tabs=[...document.querySelectorAll('.panel-tab')].filter(el=>!el.hidden);
        const index=tabs.indexOf(e.target),next=e.key==='Home'?0:e.key==='End'?tabs.length-1:(index+(e.key==='ArrowRight'?1:-1)+tabs.length)%tabs.length;
        root.__ide.switchPanel(tabs[next].dataset.tab);tabs[next].focus();
      });
      const returns=document.createElement('button');returns.type='button';returns.id='studio-panel-return';returns.hidden=true;
      returns.onclick=()=>root.__ide.showPanel();byId('bottom-panel').after(returns);
    }
    constructor(ide) {
      this.ide=ide;
      this.tab=['project','outline'].includes(ide.settings.sidebarTab)?ide.settings.sidebarTab:ide.settings.projectExplorer===false?'outline':'project';
      this.pinned=ide.settings.sidebarPinned!==false;
      byId('sidebar-close').onclick=()=>ide.setSidebarVisible(false);
      document.addEventListener('pointerdown',e=>{
        if(!this.pinned&&ide.settings.sidebarVisible!==false&&e.target.closest('#editor-container, #bottom-panel'))ide.setSidebarVisible(false);
      });
      document.addEventListener('keydown',e=>{
        if(e.key==='Escape'&&!this.pinned&&byId('shell-sidebar').contains(document.activeElement)){
          e.preventDefault();ide.setSidebarVisible(false);ide.focusEditor();
        }
      });
      this.render();this.localize();
    }
    select(tab,{reveal=true}={}) {
      if(!['project','outline'].includes(tab))return;
      const returning=byId('sidebar-rail').contains(document.activeElement);
      this.tab=tab;this.ide.saveSettings({sidebarTab:tab,projectExplorer:tab==='project'});
      if(reveal)this.ide.setSidebarVisible(true);
      this.render();
      if(returning)byId('sidebar-tab-'+tab).focus();
    }
    toggleProject() {
      if(this.tab==='project'&&this.ide.settings.sidebarVisible!==false)this.ide.setSidebarVisible(false);
      else this.select('project');
    }
    togglePin(){this.pinned=!this.pinned;this.ide.saveSettings({sidebarPinned:this.pinned});this.render();}
    render() {
      const visible=this.ide.settings.sidebarVisible!==false;
      byId('project-explorer').hidden=this.tab!=='project';
      byId('project-explorer').classList.remove('hidden');
      byId('structure-content').hidden=this.tab!=='outline';
      byId('sidebar-rail').hidden=visible;
      byId('shell-sidebar').classList.toggle('studio-unpinned',!this.pinned);
      for(const name of ['project','outline']){
        const selected=this.tab===name,tab=byId('sidebar-tab-'+name);
        tab.setAttribute('aria-selected',String(selected));tab.tabIndex=selected?0:-1;
        UIControls.setMenuChecked(document.querySelector('[data-check="'+(name==='project'?'projectVisible':'outlineVisible')+'"]'),visible&&selected);
      }
      UIControls.setPressed(byId('tb-toggle-project'),visible&&this.tab==='project');
      UIControls.setPressed(byId('sidebar-pin'),this.pinned);
      this.localize();
    }
    localize() {
      text('studio-sidebar-title',this.tab==='project'?'studio.project':'studio.outline');
      for(const name of ['project','outline']){
        const key=name==='project'?'studio.project':'studio.structure';text('sidebar-tab-'+name,key);
        const ret=document.querySelector('#sidebar-rail [data-side="'+name+'"]');
        ret.textContent=I18N.t(key);UIControls.setLabel(ret,I18N.t(key));
      }
      document.querySelectorAll('[data-studio-label]').forEach(el=>UIControls.setLabel(el,I18N.t(el.dataset.studioLabel)));
      document.querySelectorAll('[data-studio-text]').forEach(el=>{el.textContent=I18N.t(el.dataset.studioText);});
      UIControls.setLabel(byId('sidebar-pin'),I18N.t(this.pinned?'studio.unpin':'studio.pin'));
      const rail=byId('sidebar-rail');rail.setAttribute('aria-label',I18N.t('studio.tools'));
      byId('sidebar-tabs').setAttribute('aria-label',I18N.t('studio.tools'));
      this.updateCommandLabels();
      text('studio-panel-return','studio.showPanel');
      byId('panel-tabs').setAttribute('aria-label',I18N.t('studio.showPanel'));
      this.updateDiagnostics();
      this.updateDocument();this.updateExecution();
    }
    updateDocument() {
      const file=this.ide.activeFile;
      byId('structure-document-name').textContent=file?file.split(/[\\/]/).pop():I18N.t('studio.noDocument');
      byId('structure-document-name').title=file||'';
      byId('project-title').title=this.ide.projectDir||I18N.t('menu.file.openProject');
      this.compactOutline();
    }
    updateCommandLabels(){for(const el of document.querySelectorAll('.studio-extra-commands button'))el.querySelector('.studio-overflow-label').textContent=el.getAttribute('aria-label')||el.title;}
    updateDiagnostics(){
      const tab=document.querySelector('.panel-tab[data-tab="diagnostics"]');
      let count=tab.querySelector('.studio-diagnostic-count');
      if(!count){count=document.createElement('span');count.className='studio-diagnostic-count';count.setAttribute('aria-hidden','true');tab.append(count);}
      count.textContent=String(this.ide._lastDiagnostics?.length||0);
      UIControls.setLabel(tab,I18N.t('panel.problems')+': '+count.textContent);
    }
    compactOutline(){
      const panel=this.ide.outlinePanel;
      if(!panel||byId('outline-more'))return;
      const more=document.createElement('details');more.id='outline-more';
      const summary=document.createElement('summary');summary.innerHTML=ShellIcons.markup('ellipsis');summary.dataset.studioLabel='studio.moreCommands';UIControls.setLabel(summary,I18N.t('studio.moreCommands'));more.append(summary);
      const options=document.createElement('div');options.className='studio-outline-extra';
      const label=document.createElement('label');label.htmlFor='outline-sort';label.dataset.i18n='outline.sort';label.textContent=I18N.t('outline.sort');
      options.append(label,panel.sort,panel.groupAll,panel.assistantButton);more.append(options);panel.controls.querySelector('.outline-group-tools').append(more);
      closeCommands(more,options);
    }
    updateExecution() {
      byId('studio-debug-tools').hidden=!this.ide.running&&!this.ide.execution?.starting;
      for(const id of ['project-create','project-refresh','project-collapse'])UIControls.setEnabled(byId(id),!!this.ide.projectDir);
      for(const action of ['refresh-project','choose-project-entry','close-project','new-project-file'])UIControls.setEnabled(document.querySelector('[data-action="'+action+'"]'),!!this.ide.projectDir);
      UIControls.setEnabled(document.querySelector('[data-action="use-current-entry"]'),!!this.ide.projectDir&&this.ide.projectSession.files.includes(this.ide.activeFile));
    }
    bottomVisible(visible){
      const returning=visible&&document.activeElement===byId('studio-panel-return');
      byId('studio-panel-return').hidden=visible;this.updateBottomTabs(this.ide.settings.bottomPanelTab||'console');
      if(returning)document.querySelector('.panel-tab.active')?.focus();
    }
    updateBottomTabs(name){
      for(const tab of ['console','diagnostics','assistant']){
        const selected=tab===name;
        const el=document.querySelector('.panel-tab[data-tab="'+tab+'"]');
        el.id='panel-tab-'+tab;el.tabIndex=selected?0:-1;
        el.setAttribute('role','tab');el.setAttribute('aria-selected',String(selected));el.setAttribute('aria-controls',tab+'-content');
        byId(tab+'-content').setAttribute('role','tabpanel');byId(tab+'-content').setAttribute('aria-labelledby',el.id);
        UIControls.setMenuChecked(document.querySelector('[data-check="panel'+({console:'Console',diagnostics:'Diagnostics',assistant:'Assistant'}[tab])+'"]'),selected&&this.ide.settings.bottomPanelVisible!==false);
      }
    }
    reset() {
      this.pinned=true;this.ide.saveSettings({sidebarPinned:true,sidebarWidth:300,consoleHeight:200});
      if(this.ide.sidebarSplitter){this.ide.sidebarSplitter.preferred=300;this.ide.sidebarSplitter.layout();}
      if(this.ide.bottomSplitter){this.ide.bottomSplitter.preferred=200;this.ide.bottomSplitter.layout();}
      this.select('project');this.ide.setBottomPanelVisible(true);this.ide.switchPanel('console');this.ide.setToolbarVisible(true);this.ide.setStatusBarVisible(true);
    }
  }
  root.StudioLayout=StudioLayout;
  root.StudioMenus.build();
  StudioLayout.prepare();
  root.refreshShellMenus?.();
  document.querySelectorAll('#menubar .menu-label').forEach(el=>new MutationObserver(()=>root.refreshShellMenus?.()).observe(el,{childList:true,characterData:true,subtree:true}));
})(window);
