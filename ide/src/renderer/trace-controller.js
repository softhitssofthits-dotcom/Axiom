// Меню, панель и клавиатура используют одни условия доступности и команды.
class TraceController {
  constructor(ide) {
    this.ide = ide; this.points = new Map(); this.marker = null;
    ide.editor.updateOptions({ glyphMargin: true });
    ide.editor.onMouseDown(e => {
      if (e.target.type === ide.monaco.editor.MouseTargetType.GUTTER_GLYPH_MARGIN && e.target.position)
        this.toggle(e.target.position.lineNumber);
    });
    ide.editor.onDidChangeModel(() => this.update());
    ide.editor.onDidChangeModelContent(() => this.update());
    this.update();
  }
  lines(model) {
    return [...new Set((this.points.get(model) || []).map(id => model.getDecorationRange(id)?.startLineNumber).filter(Boolean))].sort((a,b)=>a-b);
  }
  toggle(line = this.ide.editor.getPosition()?.lineNumber) {
    const model=this.ide.editor.getModel();
    if (!model || !line || this.ide.running) return;
    const lines=this.lines(model); const next=lines.includes(line)?lines.filter(n=>n!==line):[...lines,line];
    this.points.set(model, model.deltaDecorations(this.points.get(model)||[], next.map(n=>({
      range:new this.ide.monaco.Range(n,1,n,1), options:{glyphMarginClassName:'trace-breakpoint',
        stickiness:this.ide.monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        glyphMarginHoverMessage:{value:I18N.t('trace.breakpoint')} }}))));
    this.update();
  }
  clear() {
    if (this.ide.running) return;
    for(const [model,ids] of this.points) if(!model.isDisposed()) model.deltaDecorations(ids,[]);
    this.points.clear(); this.update();
  }
  mark(payload) {
    this.clearMarker();
    const file=this.ide.openFiles.get(this.ide._runFile);
    if(!file || file.model.isDisposed()) return;
    this.ide.switchToFile(this.ide._runFile);
    if(payload.line) this.marker={model:file.model,ids:file.model.deltaDecorations([], [{
      range:new this.ide.monaco.Range(payload.line,1,payload.line,1),
      options:{isWholeLine:true,className:'trace-current-line',glyphMarginClassName:'trace-current-arrow'} }])};
  }
  clearMarker() {
    if(this.marker && !this.marker.model.isDisposed()) this.marker.model.deltaDecorations(this.marker.ids,[]);
    this.marker=null;
  }
  allowed(command) {
    if((this.restarting || this.launching) && command!=='stop') return false;
    const e=this.ide.execution, paused=!!e.stopId && !e.debugBusy;
    const idle=!this.ide.running && !!this.ide.activeFile && !this.restarting;
    if(command==='start'||command==='stepInto'||command==='stepOver') return paused||idle;
    if(command==='stepBack') return paused && e.canStepBack;
    if(command==='stepOut') return paused && e.callDepth>0;
    if(command==='stop') return this.ide.running || !!this.restarting;
    if(command==='restart') return paused;
    if(command==='breakpoint'||command==='clear') return idle;
    return false;
  }
  update() {
    const ide=this.ide;
    for(const [model] of this.points) if(model.isDisposed()) this.points.delete(model);
    if(this.locale!==I18N.current) {
      this.locale=I18N.current;
      for(const [model,ids] of this.points) model.changeDecorations(accessor=>{
        for(const id of ids) accessor.changeDecorationOptions(id,{
          glyphMarginClassName:'trace-breakpoint',
          stickiness:ide.monaco.editor.TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
          glyphMarginHoverMessage:{value:I18N.t('trace.breakpoint')} });
      });
    }
    document.querySelectorAll('[data-trace-command]').forEach(el=>{
      const enabled=this.allowed(el.dataset.traceCommand);
      UIControls.setEnabled(el, enabled);
    });
    const model=ide.editor.getModel(), runModel=ide.openFiles.get(ide._runFile)?.model;
    ide.editor.updateOptions({ readOnly:!!ide.openFiles.get(ide.activeFile)?.readonly || (!!ide.running && model===runModel) });
    if(ide.execution.stopId && ide.execution.line) ide.setStatus(ide.execution.debugError || I18N.t('trace.paused',ide.execution.line));
  }
  async command(command) {
    if(!this.allowed(command)) return;
    if(command==='breakpoint') return this.toggle();
    if(command==='clear') return this.clear();
    if(command==='stop') return this.ide.stop();
    if(command==='restart') {
      const file=this.ide._runFile, entry=this.ide.openFiles.get(file);
      if(!entry) return;
      const snapshot=this.ide.execution.sourceSnapshot, request={};
      this.restartRequest=request;this.restarting=true;this.update();
      const current=()=>this.restartRequest===request && this.ide.execution.sourceSnapshot===snapshot && this.ide.openFiles.get(file)===entry;
      try {
        await this.ide.execution.stop();
        if(!current()) return;
        if(this.ide.running) await new Promise(resolve=>{this.restartResolve=resolve;});
        if(!current() || this.ide.running) return;
        this.ide.switchToFile(file);await this.ide.run('debug',false);
      } finally {
        if(this.restartRequest===request){this.restartRequest=null;this.restarting=false;this.update();}
      }
      return;
    }
    if(this.ide.execution.stopId) return this.ide.execution.debug(command==='start'?'continue':command);
    this.launching=true;this.update();
    try { return await this.ide.run('debug',command!=='start'); }
    finally { this.launching=false;this.update(); }
  }
  cancelRestart() {
    if(!this.restarting) return;
    this.restartRequest=null;this.restarting=false;
    this.restartResolve?.();this.restartResolve=null;this.update();
  }
  finished() { this.clearMarker();this.restartResolve?.();this.restartResolve=null;this.update(); }
}
window.TraceController=TraceController;
