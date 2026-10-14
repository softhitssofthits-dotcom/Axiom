/* Применение текстовых правок Monaco, включая заготовки при перетаскивании. */
(function(root){
 const escape=text=>text.replace(/[\\$}]/g,'\\$&');
 function create(ide){return{
  hasPreviewHandler(){return false;},
  async apply(input,options={}){
   if(options.token?.isCancellationRequested)return{isApplied:false,ariaSummary:''};
   const edits=Array.isArray(input)?input:input.edits;
   const groups=new Map();
   for(const edit of edits){
    if(!edit.resource||!edit.textEdit?.range||typeof edit.textEdit.text!=='string')throw Error('Поддерживаются только текстовые правки открытых файлов.');
    const model=ide.monaco.editor.getModels().find(m=>m.uri.toString()===edit.resource.toString());
    if(!model)throw Error('Модель файла не найдена.');
    if(typeof edit.versionId==='number'&&edit.versionId!==model.getVersionId())throw Error('Файл изменился после подготовки правки.');
    if(edit.textEdit.insertAsSnippet&&model!==ide.editor?.getModel())throw Error('Заготовка требует активного редактора.');
    if(!groups.has(model))groups.set(model,[]);groups.get(model).push(edit.textEdit);
   }
   let count=0;
   for(const [model,changes]of groups){
    if(changes.some(e=>e.insertAsSnippet)){
     const controller=ide.editor.getContribution('snippetController2');
     if(!controller)throw Error('Средство вставки заготовок недоступно.');
     const before=model.getVersionId();
     controller.insert(changes.map(e=>({range:ide.monaco.Range.lift(e.range),template:e.insertAsSnippet?e.text:escape(e.text)})),{adjustWhitespace:false});
     if(model.getVersionId()===before)throw Error('Заготовка не была вставлена.');
    }else{
     model.pushStackElement();model.pushEditOperations([],changes.map(e=>({range:ide.monaco.Range.lift(e.range),text:e.text,forceMoveMarkers:true})),()=>[]);model.pushStackElement();
    }
    count+=changes.length;
   }
   return{isApplied:count>0,ariaSummary:ide.settings.language==='en'?`Applied ${count} text edits.`:`Применено текстовых правок: ${count}.`};
  }
 };}
 root.EditorWorkspaceEdits={create};if(typeof module!=='undefined')module.exports={create};
})(typeof window==='undefined'?globalThis:window);
