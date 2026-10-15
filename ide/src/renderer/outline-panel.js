// Навигация по объявлениям активного документа. Исходный текст не изменяется.
class OutlinePanel {
  constructor(ide) {
    this.ide = ide;
    this.states = new Map();
    this.items = [];
    this.root = document.getElementById('outline-list');
    this.controls = document.createElement('div');
    this.controls.className = 'outline-controls';
    this.search = document.createElement('input');
    this.search.id = 'outline-search';
    this.search.type = 'search';
    this.search.addEventListener('input', () => { this.state.query = this.search.value; this.state.scroll = 0; this.render(); });
    this.search.addEventListener('keydown', e => {
      if (e.key === 'Escape') { e.preventDefault(); this.search.value = ''; this.state.query = ''; this.render(); }
      if (e.key === 'ArrowDown' || e.key === 'Enter') {
        e.preventDefault();
        const target = this.root.querySelector('.outline-current .outline-jump') || this.root.querySelector('.outline-jump');
        if (e.key === 'Enter') target?.click(); else target?.focus();
      }
    });
    this.filter = document.createElement('select');
    this.filter.id = 'outline-filter';
    this.sort = document.createElement('select');
    this.sort.id = 'outline-sort';
    for (const control of [this.filter, this.sort]) control.addEventListener('change', () => {
      this.ide.saveSettings({ outlineFilter: this.filter.value, outlineSort: this.sort.value });
      this.state.scroll = 0; this.localize(); this.render();
    });
    this.follow = UIControls.createIconButton({id:'outline-follow',label:this.t('follow'),markup:window.ShellIcons.markup('target'),onClick:() => {
      this.ide.saveSettings({ outlineFollow: !this.ide.settings.outlineFollow });
      this.localize(); this.updateCurrent('force');
    }});
    this.count = document.createElement('div');
    this.count.id = 'outline-count'; this.count.setAttribute('role', 'status');
    this.groups = UIControls.createIconButton({id:'outline-groups',label:this.t('groupsHint'),markup:window.ShellIcons.markup('list-tree'),onClick:() => {
      this.ide.saveSettings({outlineGrouped:this.ide.settings.outlineGrouped === false});
      this.localize(); this.render();
    }});
    const groupTools = document.createElement('div'); groupTools.className = 'outline-group-tools';
    this.groupAll = UIControls.createIconButton({id:'outline-group-all',label:this.t('collapseAll'),markup:window.ShellIcons.markup('fold'),onClick:()=>this.setAllCollapsed(!this.shouldExpandGroups)});
    groupTools.append(this.groups, this.follow, this.groupAll);
    this.warnings = document.createElement('div'); this.warnings.id = 'outline-region-warnings'; this.warnings.setAttribute('role','status');
    const options = document.createElement('div'); options.className = 'outline-options';
    options.append(this.filter, this.sort);
    this.controls.append(this.search, options, groupTools, this.count, this.warnings);
    this.root.before(this.controls);
    this.root.addEventListener('scroll', () => { if (this.state) this.state.scroll = this.root.scrollTop; });
    this.root.addEventListener('keydown', e => {
      if (['ArrowLeft','ArrowRight'].includes(e.key) && document.activeElement?.classList.contains('outline-group-toggle')) {
        e.preventDefault();
        if ((e.key==='ArrowRight') !== (document.activeElement.getAttribute('aria-expanded')==='true')) document.activeElement.click();
        return;
      }
      if (!['ArrowDown', 'ArrowUp', 'Home', 'End', 'Escape'].includes(e.key)) return;
      if (e.key === 'Escape') { e.preventDefault(); this.search.focus(); return; }
      const buttons = [...this.root.querySelectorAll('.outline-jump, .outline-group-toggle')].filter(button=>!button.disabled);
      const index = buttons.indexOf(document.activeElement);
      if (index < 0) return;
      e.preventDefault();
      const next = e.key === 'Home' ? 0 : e.key === 'End' ? buttons.length - 1 : Math.max(0, Math.min(buttons.length - 1, index + (e.key === 'ArrowDown' ? 1 : -1)));
      buttons[next]?.focus();
    });
  }

  t(key) { return I18N.t('outline.' + key); }

  localize() {
    this.search.placeholder = this.t('search'); UIControls.setLabel(this.search,this.t('search'),{tooltip:false});
    for (const [select, entries, value, label] of [
      [this.filter, [['all', 'all'], ['sub', 'subs'], ['function', 'functions'], ['type', 'types']], this.ide.settings.outlineFilter || 'all', 'filter'],
      [this.sort, [['source', 'source'], ['name', 'alphabet']], this.ide.settings.outlineSort || 'source', 'sort'],
    ]) {
      select.replaceChildren(...entries.map(([v, k]) => { const o = document.createElement('option'); o.value = v; o.textContent = this.t(k); return o; }));
      const selected = document.createElement('button'); selected.type = 'button';
      selected.append(document.createElement('selectedcontent')); select.prepend(selected);
      select.value = value; if (select.selectedIndex < 0) select.selectedIndex = 0;
      UIControls.setLabel(select,this.t(label),{tooltip:false}); select.title = this.t(label) + ': ' + select.selectedOptions[0].textContent;
    }
    UIControls.setLabel(this.follow,this.t('follow') + '\n' + this.t('followHint'));
    UIControls.setPressed(this.follow,!!this.ide.settings.outlineFollow);
    UIControls.setLabel(this.groups,this.t('groupsHint'));
    UIControls.setPressed(this.groups,this.ide.settings.outlineGrouped !== false);
  }

  rebuild() {
    const model = this.ide.editor.getModel();
    this.key = this.ide.activeFile || model?.uri.toString() || '';
    if (!this.states.has(this.key)) this.states.set(this.key, { query: '', scroll: 0 });
    this.state = this.states.get(this.key);
    this.search.value = this.state.query;
    this.localize();
    const doc = model ? window.LangFeatures.scanDoc(model) : { subs: [], functions: [], types: [] };
    this.regions = doc.regions || [];
    this.regionWarnings = doc.regionWarnings || [];
    this.items = [['sub', doc.subs], ['function', doc.functions], ['type', doc.types]]
      .flatMap(([kind, items]) => items.map(item => ({ ...item, kind, id: kind + ':' + item.name.toUpperCase() })))
      .sort((a, b) => a.lineNo - b.lineNo);
    this.render();
  }

  pins() {
    const value = this.ide.settings.outlinePins?.[this.key];
    return new Set(Array.isArray(value) ? value : []);
  }

  collapsed() {
    const value = this.ide.settings.outlineCollapsed?.[this.key];
    return new Set(Array.isArray(value) ? value : []);
  }

  saveCollapsed(value) {
    this.ide.saveSettings({outlineCollapsed:{...this.ide.settings.outlineCollapsed,[this.key]:[...value]}});
  }

  setAllCollapsed(collapse) {
    this.saveCollapsed(new Set(collapse ? this.regions.map(r=>r.id) : [])); this.render();
  }

  render() {
    const scroll = this.state.scroll;
    const pins = this.pins(), query = this.state.query.trim().toLowerCase();
    const grouped = this.ide.settings.outlineGrouped !== false && this.regions.length > 0;
    const regionsById = new Map(this.regions.map(r=>[r.id,r]));
    const matches = item => {
      if (item.name.toLowerCase().includes(query)) return true;
      for(let r=regionsById.get(item.regionId); r; r=regionsById.get(r.parentId)) if(r.name.toLowerCase().includes(query))return true;
      return false;
    };
    let visible = this.items.filter(item => (!this.filter.value || this.filter.value === 'all' || item.kind === this.filter.value) && matches(item));
    this.visibleIds = new Set(visible.map(item=>item.id));
    visible.sort((a, b) => Number(pins.has(b.id)) - Number(pins.has(a.id)) || (this.sort.value === 'name' ? a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }) : a.lineNo - b.lineNo));
    this.root.replaceChildren();
    this.count.textContent = this.t('found').replace('{shown}', visible.length).replace('{total}', this.items.length);
    UIControls.setEnabled(this.groups,this.regions.length>0);
    this.warnings.hidden = !this.regionWarnings.length;
    this.warnings.textContent = this.regionWarnings.map(w=>this.t('region.'+w.kind).replace('{line}',w.lineNo)).join('\n');
    this.warnings.title = this.warnings.textContent;
    if (!visible.length) {
      const empty = document.createElement('div'); empty.className = 'recent-empty';
      empty.textContent = this.items.length ? this.t('noMatches') : this.t('empty'); this.root.append(empty);
    }
    const makeRow = item => {
      const row = document.createElement('div'); row.className = 'outline-item'; row.dataset.symbol = item.id;
      const jump = document.createElement('button'); jump.type = 'button'; jump.className = 'outline-jump';
      jump.title = (item.signature || 'TYPE ' + item.name) + '\n' + this.t('range').replace('{start}', item.lineNo).replace('{end}', item.endLineNo) + (item.description ? '\n\n' + item.description : '');
      const icon = document.createElement('span'); icon.className = 'outline-icon'; icon.textContent = {sub:'S', function:'ƒ', type:'T'}[item.kind]; icon.setAttribute('aria-hidden', 'true');
      const name = document.createElement('span'); name.className = 'outline-name';
      const start = item.name.toLowerCase().indexOf(query);
      if (query && start >= 0) {
        const mark = document.createElement('mark'); mark.textContent = item.name.slice(start, start + query.length);
        name.append(item.name.slice(0, start), mark, item.name.slice(start + query.length));
      } else name.textContent = item.name;
      const line = document.createElement('span'); line.className = 'outline-line'; line.textContent = item.lineNo;
      jump.append(icon, name, line);
      UIControls.setLabel(jump,(item.signature || 'TYPE ' + item.name) + ', ' + this.t('ln') + ' ' + item.lineNo,{tooltip:false});
      jump.addEventListener('click', () => {
        this.ide.editor.setPosition({ lineNumber: item.lineNo, column: 1 });
        this.ide.editor.revealLineInCenter(item.lineNo); this.ide.editor.focus();
      });
      const pin = document.createElement('button'); pin.type = 'button'; pin.className = 'outline-pin';
      pin.textContent = pins.has(item.id) ? '★' : '☆'; UIControls.setPressed(pin,pins.has(item.id));
      UIControls.setLabel(pin,this.t(pins.has(item.id) ? 'unpin' : 'pin') + ': ' + item.name);
      pin.addEventListener('click', () => {
        const updated = this.pins(); if (updated.has(item.id)) updated.delete(item.id); else updated.add(item.id);
        this.ide.saveSettings({ outlinePins: { ...this.ide.settings.outlinePins, [this.key]: [...updated] } });
        this.render();
        [...this.root.querySelectorAll('.outline-item')].find(r => r.dataset.symbol === item.id)?.querySelector('.outline-pin').focus();
      });
      row.append(jump, pin); return row;
    };
    if (!grouped) {
      for(const item of visible) this.root.append(makeRow(item));
    } else {
      for(const item of visible.filter(i=>pins.has(i.id)))this.root.append(makeRow(item));
      const unpinned=visible.filter(i=>!pins.has(i.id)), collapsed=this.collapsed();
      const byParent=new Map();
      const add=(parent,entry)=>{if(!byParent.has(parent))byParent.set(parent,[]);byParent.get(parent).push(entry);};
      const required=new Set();
      for(const item of unpinned){add(item.regionId,item);for(let r=regionsById.get(item.regionId);r;r=regionsById.get(r.parentId))required.add(r.id);}
      for(const region of this.regions)if(required.has(region.id))add(region.parentId,{...region,isRegion:true});
      const append=(parent,target)=>{
        const entries=byParent.get(parent)||[];
        entries.sort((a,b)=>this.sort.value==='name'?a.name.localeCompare(b.name,undefined,{sensitivity:'base'}):a.lineNo-b.lineNo);
        for(const entry of entries){
          if(!entry.isRegion){target.append(makeRow(entry));continue;}
          const section=document.createElement('section');section.className='outline-group';section.dataset.region=entry.id;
          const toggle=document.createElement('button');toggle.type='button';toggle.className='outline-group-toggle';
          const expanded=!!query||!collapsed.has(entry.id);
          UIControls.setExpanded(toggle,expanded); UIControls.setLabel(toggle,entry.name);
          const arrow=document.createElement('span');arrow.className='outline-group-arrow';arrow.innerHTML=window.ShellIcons.markup(expanded?'chevron-down':'chevron-right');
          const label=document.createElement('span');label.className='outline-group-name';label.textContent=entry.name;
          toggle.append(arrow,label);
          toggle.title=entry.name+'\n'+this.t('range').replace('{start}',entry.lineNo).replace('{end}',entry.endLineNo);
          toggle.addEventListener('click',()=>{
            if(query)return;
            const state=this.collapsed();if(state.has(entry.id))state.delete(entry.id);else state.add(entry.id);
            this.saveCollapsed(state);this.render();
            [...this.root.querySelectorAll('.outline-group')].find(e=>e.dataset.region===entry.id)?.querySelector('.outline-group-toggle').focus();
          });
          if(query)UIControls.setEnabled(toggle,false);
          section.append(toggle);
          if(expanded){const contents=document.createElement('div');contents.className='outline-group-contents';append(entry.id,contents);section.append(contents);}
          target.append(section);
        }
      };
      append(null,this.root);
    }
    const groupHeaders = [...this.root.querySelectorAll('.outline-group-toggle')];
    this.shouldExpandGroups = groupHeaders.some(header=>header.getAttribute('aria-expanded')==='false');
    this.groupAll.innerHTML = window.ShellIcons.markup(this.shouldExpandGroups ? 'unfold' : 'fold');
    UIControls.setLabel(this.groupAll,this.t(this.shouldExpandGroups ? 'expandAll' : 'collapseAll'));
    UIControls.setEnabled(this.groupAll,grouped && !query && groupHeaders.length>0);
    this.root.scrollTop = scroll;
    this.updateCurrent(false);
  }

  updateCurrent(follow = true) {
    const line = this.ide.editor.getPosition()?.lineNumber;
    const current = this.items.find(item => line >= item.lineNo && line <= item.endLineNo);
    const changed = this.currentId !== current?.id;
    this.currentId = current?.id;
    if(current && this.visibleIds?.has(current.id) && !this.pins().has(current.id) && follow && (changed || follow==='force') && this.ide.settings.outlineFollow && this.ide.settings.outlineGrouped!==false) {
      const regionsById=new Map(this.regions.map(r=>[r.id,r])), collapsed=this.collapsed();
      let opened=false;
      for(let r=regionsById.get(current.regionId);r;r=regionsById.get(r.parentId))if(collapsed.delete(r.id))opened=true;
      if(opened){this.saveCollapsed(collapsed);this.render();}
    }
    for (const row of this.root.querySelectorAll('.outline-item')) {
      const active = row.dataset.symbol === current?.id;
      row.classList.toggle('outline-current', active);
      const jump = row.querySelector('.outline-jump');
      if (active) jump.setAttribute('aria-current', 'location'); else jump.removeAttribute('aria-current');
      if (active && follow && (changed || follow === 'force') && this.ide.settings.outlineFollow) row.scrollIntoView({ block: 'nearest' });
    }
  }
}
window.OutlinePanel = OutlinePanel;
