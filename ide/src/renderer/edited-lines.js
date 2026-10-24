(function(root,factory){
  if(typeof module==='object'&&module.exports) module.exports=factory();
  else Object.assign(root,factory());
})(typeof window!=='undefined'?window:globalThis,function(){
  // Учитываем только пользовательские правки, а не перемещения каретки.
  class EditedLines {
    constructor(){this.models=new WeakMap();}
    has(model,line){return this.models.get(model)?.has(line) || false;}
    consume(model,line){this.models.get(model)?.delete(line);}
    clear(model){this.models.delete(model);}
    // Замена документа целиком (setValue, восстановление, откат) не является правкой строки:
    // прежние отметки относятся к другому тексту и структурный переход по ним недопустим.
    apply(model,event,mark=true){
      if(event?.isFlush){this.clear(model);return;}
      this.record(model,event?.changes||[],mark);
    }
    record(model,changes,mark=true){
      let lines=this.models.get(model)||new Set();
      for(const change of [...changes].sort((a,b)=>b.range.startLineNumber-a.range.startLineNumber || b.range.startColumn-a.range.startColumn)){
        const start=change.range.startLineNumber,end=change.range.endLineNumber;
        const inserted=(change.text.match(/\r\n|\r|\n/g)||[]).length;
        const shift=inserted-(end-start);
        if(!mark && shift===0)continue;
        lines=new Set([...lines].map(line=>line<start?line:line>end?line+shift:start));
        if(mark)for(let line=start;line<=start+inserted;line++)lines.add(line);
      }
      this.models.set(model,lines);
    }
  }
  return {EditedLines};
});
