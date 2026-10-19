// Общий рабочий процесс навигации: снимок компилятора, переходы и действия над разделами.
class ProgramAssistant {
  constructor(ide) {
    this.ide = ide;
    this.back = []; this.forward = []; this.sequence = 0;
    this.diagnosticVersions = new WeakMap();
    this.tab = document.createElement('button'); this.tab.className = 'panel-tab'; this.tab.dataset.tab = 'assistant';
    this.tab.addEventListener('click', () => ide.switchPanel('assistant'));
    document.getElementById('panel-tabs').insertBefore(this.tab,document.getElementById('diagnostic-filters'));
    this.root = document.createElement('div'); this.root.id = 'assistant-content'; this.root.className = 'tab-content';
    this.toolbar = document.createElement('div'); this.toolbar.className = 'assistant-toolbar';
    const icon = (id,key,action) => UIControls.createIconButton({id,label:'',markup:window.ShellIcons.markup(key),onClick:action});
    this.backButton = icon('assistant-back','arrow-left',()=>this.travel(this.back,this.forward));
    this.forwardButton = icon('assistant-forward','arrow-right',()=>this.travel(this.forward,this.back));
    this.refreshButton = icon('assistant-refresh','refresh',()=>this.refresh());
    this.entryButton = icon('assistant-entry','home',()=>this.selectEntry());
    this.heading = document.createElement('strong'); this.heading.id = 'assistant-heading';
    this.toolbar.append(this.backButton,this.forwardButton,this.refreshButton,this.entryButton,this.heading);
    this.status = document.createElement('div'); this.status.id = 'assistant-status'; this.status.setAttribute('role','status');
    this.body = document.createElement('div'); this.body.id = 'assistant-body';
    this.root.append(this.toolbar,this.status,this.body); document.getElementById('panel-content').append(this.root);
    this.menu = document.createElement('div'); this.menu.id = 'outline-context-menu'; this.menu.className = 'xs-context-menu';
    this.menu.hidden = true; this.menu.setAttribute('role','menu'); document.body.append(this.menu);
    this.popup = UIControls.createContextMenu(this.menu,{translate:I18N.t,onClose:()=>this.contextElement?.focus(),onAction:id=>this.contextAction(id)});
    ide.editor.onDidChangeModelContent(()=>this.invalidate());
    ide.editor.onDidChangeModel(()=>{
      const pending=this.pendingNavigation;
      if(pending&&![pending.origin,pending.target].some(file=>ide.openFiles.get(file)?.model===ide.editor.getModel()))this.pendingNavigation=null;
      queueMicrotask(()=>{this.invalidate();this.updateHistory();});
    });
    ide.editor.onDidChangeCursorPosition(event=>{
      const pending=this.pendingNavigation;
      if(pending?.position&&ide.editor.getModel()===pending.model&&
        (event.position.lineNumber!==pending.position.lineNumber||event.position.column!==pending.position.column))this.pendingNavigation=null;
    });
    ide.monaco.editor.onDidChangeMarkers(resources=>{
      if(resources.some(uri=>uri.toString()===ide.editor.getModel()?.uri.toString())) ide.outlinePanel?.render();
    });
    this.localize(); this.updateHistory();
  }

  t(key) { return I18N.t('assistant.'+key); }
  acceptDiagnostics(models) {
    for(const model of models)this.diagnosticVersions.set(model,model.getVersionId());
  }
  problemMarkers() {
    const model=this.ide.editor.getModel();
    if(!model||this.diagnosticVersions.get(model)!==model.getVersionId())return [];
    return this.ide.monaco.editor.getModelMarkers({owner:'axiom',resource:model.uri})
      .filter(marker=>marker.severity>=this.ide.monaco.MarkerSeverity.Warning)
      .sort((a,b)=>b.severity-a.severity||a.startLineNumber-b.startLineNumber||a.startColumn-b.startColumn);
  }
  problemBadge(markers) {
    if(!markers.length)return null;
    const first=markers[0];
    const button=this.button((first.severity===this.ide.monaco.MarkerSeverity.Error?'● ':'▲ ')+markers.length,()=>{
      this.navigate(this.ide.activeFile,first.startLineNumber,first.startColumn);
      this.ide.showErrorDetailsForMarker(first);
    },'outline-problem '+(first.severity===this.ide.monaco.MarkerSeverity.Error?'error':'warning'));
    UIControls.setLabel(button,this.t('problemsCount').replace('{count}',markers.length)+'\n'+first.message);
    return button;
  }
  localize() {
    if(this.actionLocale!==I18N.current){
      this.actionLocale=I18N.current;
      this.actions?.forEach(action=>action.dispose());this.actions=[];
      const ide=this.ide;
      for(const name of ['incoming','outgoing','preview','createRegion'])this.actions.push(ide.editor.addAction({
        id:'axiom.assistant.'+name,label:this.t(name),...(name==='createRegion'?{precondition:'editorWritable'}:{}),
        run:()=>name==='createRegion'?this.regionDialog():this.show(name,this.currentItem())}));
      this.actions.push(ide.editor.addAction({id:'axiom.navigation.back',label:this.t('back'),keybindings:[ide.monaco.KeyMod.Alt|ide.monaco.KeyCode.LeftArrow],run:()=>this.travel(this.back,this.forward)}));
      this.actions.push(ide.editor.addAction({id:'axiom.navigation.forward',label:this.t('forward'),keybindings:[ide.monaco.KeyMod.Alt|ide.monaco.KeyCode.RightArrow],run:()=>this.travel(this.forward,this.back)}));
      this.actions.push(ide.editor.addAction({id:'axiom.navigation.definition',label:this.t('definition'),keybindings:[ide.monaco.KeyCode.F12],contextMenuGroupId:'navigation',run:()=>this.symbols(true)}));
      this.actions.push(ide.editor.addAction({id:'axiom.navigation.references',label:this.t('references'),keybindings:[ide.monaco.KeyMod.Shift|ide.monaco.KeyCode.F12],contextMenuGroupId:'navigation',run:()=>this.symbols(false)}));
      this.actions.push(ide.editor.addAction({id:'axiom.navigation.entry',label:this.t('entry'),run:()=>this.selectEntry()}));
    }
    this.tab.textContent=this.t('tab');
    UIControls.setLabel(this.entryButton,this.t('entry'));
    for(const [button,key] of [[this.backButton,'back'],[this.forwardButton,'forward'],[this.refreshButton,'refresh']]) UIControls.setLabel(button,this.t(key));
    if(this.view) this.heading.textContent=this.t(this.view.mode)+': '+(this.view.item?.displayName||this.view.item?.name||this.t('main'));
    if(this.view&&this.valid()&&this.graphView===this.view){
      this.clearBody();this.graphStatus(this.graph);this.renderGraph(this.graph,this.graphRoot,this.view.mode==='incoming',this.view);
    }else if(this.previewEditor&&this.valid()){
      this.clearBody();this.showPreview(this.view);
    }else if(this.view&&!this.valid())this.status.textContent=this.t('stale');
  }
  currentItem() {
    const line=this.ide.editor.getPosition()?.lineNumber;
    return this.ide.outlinePanel?.items.findLast(item=>line>=item.lineNo&&line<=item.endLineNo)||null;
  }
  selectEntry() {
    const doc=this.ide.openFiles.get(this.ide.activeFile);
    if(!doc||doc.path.startsWith('untitled')||!doc.path.toLowerCase().endsWith('.axm'))return;
    this.ide.openFiles.navigationEntryId=this.ide.openFiles.navigationEntryId===doc.documentId?null:doc.documentId;
    this.pendingNavigation=null;this.sequence++;this.clearBody();this.view=null;
    this.status.textContent=this.ide.openFiles.navigationEntryId?this.t('entrySelected')+' '+doc.path:this.t('entryCleared');
    this.ide.showPanel();this.ide.switchPanel('assistant');
  }
  async symbols(definition) {
    const doc=this.ide.openFiles.get(this.ide.activeFile),position=this.ide.editor.getPosition();
    if(!doc||!position)return;
    this.pendingNavigation=null;this.clearBody();
    const view={mode:definition?'definition':'references',file:this.ide.activeFile,model:doc.model,documentId:doc.documentId,
      version:doc.model.getVersionId(),project:this.ide.openFiles.captureProject(this.ide.activeFile)};
    const word=doc.model.getWordAtPosition(position);
    this.view=view;const sequence=++this.sequence,offset=doc.model.getOffsetAt(word?{lineNumber:position.lineNumber,column:word.startColumn}:position);
    this.localize();this.status.textContent=this.t('loading');this.ide.showPanel();this.ide.switchPanel('assistant');
    try{
      const graph=await this.ide.documentServices.request(view.file,'document.navigation');
      if(sequence!==this.sequence||!this.valid(view))return;
      if(!graph){this.status.textContent=this.t('stale');return;}
      const currentPosition=this.ide.editor.getPosition();
      if(currentPosition?.lineNumber!==position.lineNumber||currentPosition.column!==position.column){this.status.textContent=this.t('chooseAgain');return;}
      this.graph=graph;
      const site=graph.symbols?.find(s=>this.sameFile(s.sourceFilePath||view.file,view.file)&&s.start<=offset&&offset<s.start+s.length);
      const sites=site?graph.symbols.filter(s=>s.symbolId===site.symbolId&&(!definition||s.isDeclaration)):[];
      if(!sites.length){this.status.textContent=this.t('unresolved');return;}
      this.status.textContent=this.t(graph.complete?'static':'partial');
      for(const target of sites){
        const label=target.name+' — '+(target.sourceFilePath?.split(/[\\/]/).at(-1)||this.t('main'))+':'+target.line+':'+target.column;
        this.body.append(this.button(label,()=>this.navigateSource(view,target)));
      }
      if(definition&&sites.length===1)await this.navigateSource(view,sites[0]);
    }catch(error){if(sequence===this.sequence&&this.valid(view))this.status.textContent=this.t('failed')+' '+error.message;}
  }
  alive(bookmark) {
    return [...this.ide.openFiles.values()].some(doc=>doc.documentId===bookmark.documentId&&doc.model===bookmark.model&&!doc.model.isDisposed()&&!!doc.model.getDecorationRange(bookmark.decoration));
  }
  capture() {
    const doc=this.ide.openFiles.get(this.ide.activeFile), selection=this.ide.editor.getSelection();
    if(!doc||!selection)return null;
    const decoration=doc.model.deltaDecorations([],[{range:selection,options:{stickiness:this.ide.monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges}}])[0];
    return {documentId:doc.documentId,model:doc.model,version:doc.model.getVersionId(),decoration,view:this.ide.editor.saveViewState()};
  }
  release(bookmark) { if(bookmark&&!bookmark.model.isDisposed())bookmark.model.deltaDecorations([bookmark.decoration],[]); }
  push(stack,bookmark) { if(bookmark)stack.push(bookmark);while(stack.length>100)this.release(stack.shift()); }
  updateHistory() {
    for(const stack of [this.back,this.forward])for(let i=stack.length-1;i>=0;i--)if(!this.alive(stack[i]))this.release(stack.splice(i,1)[0]);
    UIControls.setEnabled(this.backButton,this.back.length>0); UIControls.setEnabled(this.forwardButton,this.forward.length>0);
  }
  navigate(file,line,column=1,bookmark=null) {
    const doc=this.ide.openFiles.get(file);
    if(!doc||doc.model.isDisposed())return;
    this.push(this.back,bookmark||this.capture()); this.forward.splice(0).forEach(b=>this.release(b));
    if(file!==this.ide.activeFile)this.ide.switchToFile(file);
    const position=doc.model.validatePosition({lineNumber:line,column});
    this.ide.editor.setPosition(position); this.ide.editor.revealLineInCenter(position.lineNumber); this.ide.editor.focus();
    this.updateHistory();
  }
  travel(from,to) {
    this.updateHistory(); const target=from.pop(); if(!target)return;
    this.push(to,this.capture());
    const entry=[...this.ide.openFiles].find(([,doc])=>doc.documentId===target.documentId&&doc.model===target.model);
    if(!entry){this.release(target);this.updateHistory();return;}
    if(entry[0]!==this.ide.activeFile)this.ide.switchToFile(entry[0]);
    this.ide.editor.restoreViewState(target.view);
    const range=target.model.getDecorationRange(target.decoration);
    if(range){
      this.ide.editor.setSelection(range);
      if(target.model.getVersionId()!==target.version)this.ide.editor.revealRangeInCenterIfOutsideViewport(range);
    }
    this.release(target);this.ide.editor.focus();this.updateHistory();
  }
  valid(view=this.view) {
    const doc=view&&this.ide.openFiles.get(view.file);
    return !!doc&&doc.model===view.model&&doc.documentId===view.documentId&&doc.model.getVersionId()===view.version&&this.ide.editor.getModel()===doc.model&&(!view.project||view.project.isCurrent());
  }
  sameFile(left,right) {return String(left||'').replace(/\\/g,'/').toLowerCase()===String(right||'').replace(/\\/g,'/').toLowerCase();}
  async navigateSource(view,location) {
    if(!this.valid(view))return;
    const file=location.sourceFilePath||view.file;
    const source=this.graph?.sourceDocuments?.find(s=>this.sameFile(s.sourceFilePath,location.sourceFilePath));
    if(!source)return;
    // Открытие меняет состав проекта. Проверяем старый снимок до открытия,
    // затем фактический текст цели, прежде чем применять координаты.
    const bookmark=this.capture();
    const position=this.ide.editor?.getPosition?.();
    const operation={origin:view.file,target:file,model:view.model,position};this.pendingNavigation=operation;
    const canOpen=()=>{
      const current=this.ide.editor?.getPosition?.();
      return this.pendingNavigation===operation&&this.view===view&&this.valid(view)&&
        (!position||(current?.lineNumber===position.lineNumber&&current.column===position.column));
    };
    try {
      if(!this.ide.openFiles.get(file))await this.ide.openFile(file,canOpen);
      if(this.pendingNavigation!==operation||this.view!==view){this.release(bookmark);return;}
      const doc=this.ide.openFiles.get(file);
      const original=this.ide.openFiles.get(view.file);
      const unchanged=this.pendingNavigation===operation&&this.view===view&&original?.model===view.model&&!view.model.isDisposed()&&view.model.getVersionId()===view.version&&
        (view.project?.documents||[]).every(snapshot=>{
          const entry=this.ide.openFiles.get(snapshot.sourceFilePath);
          return entry?.documentId===snapshot.documentId&&entry.revision===snapshot.documentRevision;
        });
      if(!unchanged||!doc||doc.model.isDisposed()||doc.model.getValue()!==source.sourceText){
        this.release(bookmark);this.status.textContent=this.t('stale');return;
      }
      this.navigate(file,location.line,location.column,bookmark);
    }catch(error){
      this.release(bookmark);
      if(this.pendingNavigation===operation&&this.view===view)this.status.textContent=this.t('failed')+' '+error.message;
    }
    finally{if(this.pendingNavigation===operation)this.pendingNavigation=null;}
  }
  clearBody() {this.previewEditor?.dispose();this.previewEditor=null;this.previewModel?.dispose();this.previewModel=null;this.body.replaceChildren();}
  invalidate() {
    if(this.view&&!this.valid()){
      this.sequence++;this.clearBody();this.status.textContent=this.t('stale');UIControls.setEnabled(this.refreshButton,true);
    }
  }
  refresh() {
    if(!this.view)return;
    if(['definition','references'].includes(this.view.mode)){this.symbols(this.view.mode==='definition');return;}
    if(this.view.file!==this.ide.activeFile){this.status.textContent=this.t('chooseAgain');return;}
    this.ide.rebuildOutline();
    const matches=this.ide.outlinePanel.items.filter(i=>i.id===this.view.item?.id);
    if(this.view.item&&matches.length!==1){this.status.textContent=this.t('chooseAgain');return;}
    this.show(this.view.mode,matches[0]||null);
  }
  showContext(item,element,x,y,region=false) {
    this.context={item,region,file:this.ide.activeFile,model:this.ide.editor.getModel(),version:this.ide.editor.getModel().getVersionId()};this.contextElement=element;
    const descriptors=region?[['renameRegion','edit'],['createRegion','group-by-ref-type']]:[['incoming','references'],['outgoing','call-outgoing'],['preview','preview'],['createRegion','group-by-ref-type']];
    const editable=!this.ide.openFiles.get(this.ide.activeFile)?.readonly&&!this.ide.editor.getRawOptions().readOnly;
    this.popup.show(descriptors.map(([id,icon])=>({id,icon:'codicon-'+icon,translation:'assistant.'+id,
      enabled:['createRegion','renameRegion'].includes(id)?editable:(!['incoming','outgoing'].includes(id)||item.kind!=='type')})),x,y);
  }
  contextAction(id) {
    const c=this.context;if(!c||c.file!==this.ide.activeFile||c.model!==this.ide.editor.getModel()||c.model.getVersionId()!==c.version)return;
    if(id==='renameRegion')this.regionDialog(c.item,true);
    else if(id==='createRegion')this.regionDialog(c.region?null:c.item);
    else this.show(id,c.item);
  }
  async show(mode,item) {
    this.pendingNavigation=null;
    if(item?.kind==='type')mode='preview';
    if(this.ide._formatBusy){this.status.textContent=this.t('stale');this.ide.showPanel();this.ide.switchPanel('assistant');return;}
    this.ide.rebuildOutline();
    if(item){
      const matches=this.ide.outlinePanel.items.filter(candidate=>candidate.id===item.id);
      if(matches.length!==1){this.clearBody();this.status.textContent=this.t('chooseAgain');this.ide.showPanel();this.ide.switchPanel('assistant');return;}
      item=matches[0];
    }
    const doc=this.ide.openFiles.get(this.ide.activeFile);if(!doc)return;
    this.clearBody();
    const view={mode,item,file:this.ide.activeFile,model:doc.model,documentId:doc.documentId,version:doc.model.getVersionId(),
      project:mode==='preview'?null:this.ide.openFiles.captureProject(this.ide.activeFile)};
    this.view=view;const sequence=++this.sequence;this.localize();this.updateHistory();
    this.ide.showPanel();this.ide.switchPanel('assistant');
    UIControls.setEnabled(this.refreshButton,true);
    if(mode==='preview'){this.showPreview(view);return;}
    this.status.textContent=this.t('loading');
    UIControls.setEnabled(this.refreshButton,false);
    try {
      const graph=await this.ide.documentServices.request(view.file,'document.navigation');
      if(sequence!==this.sequence||!this.valid(view))return;
      if(!graph){this.status.textContent=this.t('stale');return;}
      const routine=item?graph.routines.find(r=>this.sameFile(r.sourceFilePath||view.file,view.file)&&r.line===item.lineNo&&r.kind===item.kind&&r.name.split('.').at(-1).toUpperCase()===item.name.toUpperCase()):graph.routines.find(r=>r.kind==='main'&&this.sameFile(r.sourceFilePath||view.file,view.file));
      if(!routine){this.status.textContent=this.t('unresolved');return;}
      this.graph=graph;this.graphView=view;this.graphRoot=routine;
      this.graphStatus(graph);
      this.renderGraph(graph,routine,mode==='incoming',view);
    }catch(error){if(sequence===this.sequence&&this.valid(view))this.status.textContent=this.t('failed')+' '+error.message;}
    finally{if(sequence===this.sequence)UIControls.setEnabled(this.refreshButton,true);}
  }
  button(text,action,className='assistant-link') {
    const button=document.createElement('button');button.type='button';button.className=className;button.textContent=text;button.addEventListener('click',action);return button;
  }
  graphStatus(graph) {
    this.status.textContent=this.t(graph.complete?'static':'partial')+' '+this.t('summary')
      .replace('{routines}',graph.routines.filter(r=>r.kind!=='main'&&!r.isBuiltin).length).replace('{calls}',graph.calls.length);
    if(graph.indirectCalls?.length)this.status.textContent+=' '+this.t('indirectCount').replace('{count}',graph.indirectCalls.length);
  }
  showPreview(view) {
    const item=view.item;if(!item){this.status.textContent=this.t('chooseRoutine');return;}
    this.status.textContent=this.t('previewHint');
    const open=this.button(this.t('openDefinition'),()=>{if(this.valid(view))this.navigate(view.file,item.lineNo);});
    const container=document.createElement('div');container.id='assistant-preview';this.body.append(open,container);
    const text=view.model.getValueInRange(new this.ide.monaco.Range(item.lineNo,1,item.endLineNo,view.model.getLineMaxColumn(item.endLineNo)));
    this.previewModel=this.ide.monaco.editor.createModel(text,'axiom');
    this.previewEditor=this.ide.monaco.editor.create(container,{model:this.previewModel,readOnly:true,domReadOnly:true,automaticLayout:true,minimap:{enabled:false},scrollBeyondLastLine:false,lineNumbers:n=>String(n+item.lineNo-1),fontSize:13});
  }
  renderGraph(graph,root,incoming,view) {
    const routines=new Map(graph.routines.map(r=>[r.id,r]));
    if(root.signature){const signature=document.createElement('p');signature.className='assistant-root-signature';signature.textContent=root.signature;this.body.append(signature);}
    if(view.item?.description){const note=document.createElement('p');note.className='assistant-source-note';note.textContent=this.t('sourceComment')+' '+view.item.description;this.body.append(note);}
    let nodes=0;
    const name=r=>(r.kind==='main'?this.t('main'):r.name)+(r.sourceFilePath?' — '+r.sourceFilePath.split(/[\\/]/).at(-1):'');
    const branch=(routine,path,depth)=>{
      const list=document.createElement('div');list.className='assistant-branches';
      const edges=graph.calls.filter(c=>incoming?c.callee===routine.id:c.caller===routine.id);
      const indirect=incoming?[]:(graph.indirectCalls||[]).filter(c=>c.caller===routine.id);
      for(const site of indirect){
        if(++nodes>1000)break;
        list.append(this.button(this.t('indirect')+' — '+this.t('line')+' '+site.line+':'+site.column,()=>this.navigateSource(view,site),'assistant-call-site'));
      }
      const groups=new Map();
      for(const edge of edges){const id=incoming?edge.caller:edge.callee;if(!groups.has(id))groups.set(id,[]);groups.get(id).push(edge);}
      if(!groups.size){if(!indirect.length){const empty=document.createElement('p');empty.textContent=this.t('noCalls');list.append(empty);}return list;}
      for(const [id,calls] of groups){
        if(++nodes>1000){const note=document.createElement('p');note.textContent=this.t('limit');list.append(note);break;}
        const related=routines.get(id);if(!related)continue;
        const details=document.createElement('details');details.className='assistant-call-node';
        const summary=document.createElement('summary');summary.textContent=name(related)+(related.line?' — '+this.t('line')+' '+related.line:'')+' ('+calls.length+')'+(path.has(id)?' — '+this.t('cycle'):'');
        summary.title=related.signature;details.append(summary);
        const content=document.createElement('div');content.className='assistant-call-sites';
        if(!related.isBuiltin)content.append(this.button(this.t('openDefinition'),()=>this.navigateSource(view,related)));
        if(related.isBuiltin){const signature=document.createElement('p');signature.textContent=related.signature;content.append(signature);}
        for(const call of calls){
          if(++nodes>1000){const note=document.createElement('p');note.textContent=this.t('limit');content.append(note);break;}
          const label=(call.kind==='handler'?this.t('handler')+' — ':call.kind==='reference'?this.t('reference')+' — ':'')+(call.sourceFilePath?call.sourceFilePath.split(/[\\/]/).at(-1)+' — ':'')+this.t('line')+' '+call.line+':'+call.column;
          content.append(this.button(label,()=>this.navigateSource(view,call),'assistant-call-site'));
        }
        details.append(content);
        let expanded=false;
        details.addEventListener('toggle',()=>{
          if(!details.open||expanded||path.has(id)||related.isBuiltin)return;
          expanded=true;
          if(depth>=12){const note=document.createElement('p');note.textContent=this.t('limit');content.append(note);return;}
          content.append(branch(related,new Set([...path,id]),depth+1));
        });
        list.append(details);
      }
      return list;
    };
    if(!root.isBuiltin)this.body.append(this.button(this.t('openDefinition'),()=>this.navigateSource(view,root)));
    this.body.append(branch(root,new Set([root.id]),0));
    if(root.kind==='main'&&!incoming)for(const module of graph.routines.filter(r=>r.kind==='main'&&r.id!==root.id)){
      const details=document.createElement('details');details.className='assistant-module-main';
      const summary=document.createElement('summary');summary.textContent=name(module);details.append(summary);
      details.append(this.button(this.t('openDefinition'),()=>this.navigateSource(view,module)),branch(module,new Set([module.id]),0));
      this.body.append(details);
    }
  }

  regionDialog(contextItem=null,rename=false) {
    if(this.ide._formatBusy)return;
    this.ide.rebuildOutline();
    if(contextItem)contextItem=(rename?this.ide.outlinePanel.regions:this.ide.outlinePanel.items).find(item=>item.id===contextItem.id)||null;
    const model=this.ide.editor.getModel(),doc=this.ide.openFiles.get(this.ide.activeFile);
    if(!doc||doc.readonly||this.ide.editor.getRawOptions().readOnly)return;
    const version=model.getVersionId(),file=this.ide.activeFile,selection=this.ide.editor.getSelection();
    const items=this.ide.outlinePanel.items;
    let chosen;
    if(rename)chosen=contextItem?[contextItem]:[];
    else if(selection&&!selection.isEmpty()){
      const end=selection.endLineNumber-(selection.endColumn===1?1:0);
      chosen=items.filter(i=>i.lineNo<=end&&i.endLineNo>=selection.startLineNumber);
    }else chosen=contextItem?[contextItem]:[this.currentItem()].filter(Boolean);
    if(!rename)chosen=chosen.filter(item=>!chosen.some(owner=>owner!==item&&owner.lineNo<item.lineNo&&owner.endLineNo>=item.endLineNo));
    const overlay=document.createElement('div');overlay.className='modal-overlay';overlay.id='assistant-region-dialog';
    const form=document.createElement('form');form.className='modal assistant-region-form';form.setAttribute('role','dialog');form.setAttribute('aria-modal','true');
    const title=document.createElement('h3');title.id='assistant-region-title';title.textContent=this.t(rename?'renameRegion':'createRegion');form.setAttribute('aria-labelledby',title.id);
    const input=document.createElement('input');input.id='assistant-region-name';input.maxLength=160;input.value=rename?(contextItem?.name||''):'';UIControls.setLabel(input,this.t('regionName'));
    const label=document.createElement('label');label.htmlFor=input.id;label.textContent=this.t('regionName');
    const rangeHint=document.createElement('p');
    if(chosen.length)rangeHint.textContent=this.t('regionRange').replace('{count}',chosen.length).replace('{start}',Math.min(...chosen.map(i=>i.lineNo))).replace('{end}',Math.max(...chosen.map(i=>i.endLineNo)));
    const error=document.createElement('div');error.id='assistant-region-error';error.setAttribute('role','alert');
    const save=this.button(this.t('save'),()=>{},'assistant-dialog-save');save.type='submit';
    const cancel=this.button(this.t('cancel'),()=>close());
    const close=()=>{overlay.remove();this.ide.editor.focus();};
    form.append(title,rangeHint,label,input,error,save,cancel);overlay.append(form);document.body.append(overlay);
    form.addEventListener('submit',event=>{
      event.preventDefault();
      const name=input.value.trim();
      if(!name||name.length>160||/[\r\n]/.test(name)){error.textContent=this.t('nameRequired');return;}
      if(this.ide.activeFile!==file||this.ide.editor.getModel()!==model||model.getVersionId()!==version){error.textContent=this.t('stale');return;}
      if(doc.readonly||this.ide.editor.getRawOptions().readOnly){error.textContent=this.t('readOnly');return;}
      if(!chosen.length){error.textContent=this.t('chooseRoutine');return;}
      const start=Math.min(...chosen.map(i=>i.lineNo)),end=Math.max(...chosen.map(i=>i.endLineNo));
      let edits;
      if(rename){
        const text=model.getLineContent(start),prefix=text.match(/^\s*'\s*#region\s+/i)?.[0];
        if(!prefix){error.textContent=this.t('stale');return;}
        edits=[{range:new this.ide.monaco.Range(start,1,start,model.getLineMaxColumn(start)),text:prefix+name}];
      }else{
        if(chosen.some(item=>items.some(owner=>owner.lineNo<item.lineNo&&owner.endLineNo>=item.endLineNo))){error.textContent=this.t('topLevel');return;}
        const crossing=this.ide.outlinePanel.regions.some(r=>(r.lineNo>=start&&r.lineNo<=end)!==(r.endLineNo>=start&&r.endLineNo<=end));
        if(crossing){error.textContent=this.t('crossing');return;}
        const eol=model.getEOL();
        edits=[{range:new this.ide.monaco.Range(start,1,start,1),text:"' #region "+name+eol},
          {range:new this.ide.monaco.Range(end,model.getLineMaxColumn(end),end,model.getLineMaxColumn(end)),text:eol+"' #endregion"}];
      }
      this.ide.editor.pushUndoStop();this.ide.editor.executeEdits('axiom-region',edits);this.ide.editor.pushUndoStop();
      this.ide.rebuildOutline();close();
    });
    overlay.addEventListener('keydown',event=>{
      if(event.key==='Escape'){event.preventDefault();event.stopPropagation();close();}
      if(event.key==='Tab'){
        const focusable=[input,save,cancel],index=focusable.indexOf(document.activeElement);
        event.preventDefault();focusable[(index+(event.shiftKey?-1:1)+focusable.length)%focusable.length].focus();
      }
    });
    input.focus();input.select();
  }
}
window.ProgramAssistant=ProgramAssistant;
