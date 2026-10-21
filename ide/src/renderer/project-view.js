// Дерево и свойства получают состав только от общего загрузчика .NET.
(function(root){
  const t=key=>I18N.t(key);
  const report=(ide,error,key='project.failed')=>{ide.showIdeProblem(key,error.message||String(error));};
  const relative=(descriptor,file)=>file.slice(descriptor.rootDirectory.replace(/[/\\]+$/,'').length+1).replace(/\\/g,'/');
  function render(ide,list){
    const project=ide.projectSession.descriptor,tree=new Map();
    for(const file of [...project.sourceFiles,...project.assetFiles]){
      const parts=relative(project,file).split('/');let node=tree;
      for(const part of parts.slice(0,-1)){if(!node.has(part))node.set(part,new Map());node=node.get(part);}
      node.set(parts.at(-1),file);
    }
    const description=document.createElement('button');description.className='project-description';description.textContent=project.projectFilePath.split(/[/\\]/).pop();description.onclick=()=>properties(ide);list.append(description);
    function append(nodes,parent,prefix=''){
      for(const [name,value] of [...nodes].sort(([a,x],[b,y])=>Number(y instanceof Map)-Number(x instanceof Map)||a.localeCompare(b))){
        if(value instanceof Map){
          const details=document.createElement('details');details.open=true;details.className='project-directory';
          const summary=document.createElement('summary');summary.textContent=name;details.append(summary);parent.append(details);append(value,details,prefix+name+'/');
        }else{
          const source=project.sourceFiles.includes(value),button=document.createElement('button');
          button.className='project-file'+(value===ide.activeFile?' active':'')+(value===ide.projectSession.entry?' project-entry-file':'');button.dataset.path=value;button.title=value;
          button.textContent=(value===ide.projectSession.entry?'▶ ':'')+name;
          button.onclick=()=>source?ide.openFile(value):ide.projectFiles.properties(value);parent.append(button);
        }
      }
    }
    append(tree,list);
    if(project.entryError){const error=document.createElement('div');error.className='project-entry-error';error.textContent=project.entryError;list.append(error);}
  }
  function creationOptions(ide,suggested){
    return new Promise(resolve=>{
      const previous=document.getElementById('project-create-dialog');
      if(previous){previous.focus();resolve(null);return;}
      const dialog=document.createElement('dialog');dialog.id='project-create-dialog';dialog.className='project-properties';
      const form=document.createElement('form');form.method='dialog';dialog.append(form);
      const heading=document.createElement('h2');heading.textContent=t('project.create');form.append(heading);
      const fields={};
      for(const key of ['name','folder']){
        const label=document.createElement('label');label.textContent=t('project.'+key);
        const input=document.createElement('input');input.id='project-create-'+key;input.name=key;input.required=true;
        input.readOnly=key==='folder';label.htmlFor=input.id;label.append(input);form.append(label);fields[key]=input;
      }
      const browse=document.createElement('button');browse.type='button';browse.textContent=t('project.browse');form.append(browse);
      const hint=document.createElement('p');hint.textContent=t('project.createHint');form.append(hint);
      const error=document.createElement('p');error.className='project-entry-error';form.append(error);
      const actions=document.createElement('div');actions.className='project-properties-actions';form.append(actions);
      const cancel=document.createElement('button');cancel.type='button';cancel.textContent=t('project.cancel');cancel.onclick=()=>dialog.close();actions.append(cancel);
      const submit=document.createElement('button');submit.type='submit';submit.textContent=t('project.createButton');actions.append(submit);
      browse.onclick=async()=>{
        UIControls.setEnabled(browse,false);
        try{
          const folder=await root.axiom.openProjectDialog(fields.folder.value||suggested);
          if(folder&&dialog.isConnected){fields.folder.value=folder;if(!fields.name.value)fields.name.value=folder.replace(/[/\\]+$/,'').split(/[/\\]/).pop();}
        }catch(problem){error.textContent=problem.message;}finally{UIControls.setEnabled(browse,true);}
      };
      let result=null;
      form.onsubmit=event=>{
        event.preventDefault();if(!fields.folder.value){error.textContent=t('project.chooseFolder');return;}const name=fields.name.value.trim();
        if(!name||/[<>:"/\\|?*\x00-\x1f]/.test(name)||/[. ]$/.test(name)||/^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\.|$)/i.test(name)){
          error.textContent=t('project.invalidName');fields.name.focus();return;
        }
        result={name,folder:fields.folder.value};dialog.close();
      };
      dialog.onclose=()=>{dialog.remove();resolve(result);};document.body.append(dialog);dialog.showModal();fields.name.focus();
    });
  }
  async function create(ide){
    if(ide.running)return;
    try{
      const operation=ide.projectSession.sequence;
      const active=ide.activeFile,document=ide.openFiles.get(active);
      const suggested=active&&!active.startsWith('untitled')?active.replace(/[/\\][^/\\]+$/,''):undefined;
      const options=await creationOptions(ide,suggested);if(!options||ide.projectSession.sequence!==operation)return;
      const {folder,name}=options;
      const existing=await root.axiom.projectCommand('project.open',{path:folder});
      if(ide.projectSession.sequence!==operation)return;
      if(!existing.ok)throw new Error(existing.error?.message);
      if(!existing.payload.legacy){
        if(confirm(t('project.existingOpen')))await ide.openProject(existing.payload.projectFilePath);
        return;
      }
      if(document&&document.model.getValue().length>0){
        const contained=!document.path.startsWith('untitled')&&document.path.replace(/\\/g,'/').toLowerCase().startsWith(folder.replace(/\\/g,'/').replace(/\/$/,'').toLowerCase()+'/');
        if(!contained||ide.openFiles.isDirty(document.path)){
          const target=folder+'/'+(document.path.startsWith('untitled')?'main.axm':document.path.split(/[/\\]/).pop());
          if(!await ide.save(document.path,!contained,target))return;
          if(ide.projectSession.sequence!==operation)return;
          if(!document.path.replace(/\\/g,'/').toLowerCase().startsWith(folder.replace(/\\/g,'/').replace(/\/$/,'').toLowerCase()+'/'))throw new Error(t('project.sourceOutside'));
        }
      }
      let files=await root.axiom.listProjectFiles(folder);
      if(ide.projectSession.sequence!==operation)return;
      if(files.length===0){
        const created=await root.axiom.createProjectFile(folder,folder+'/main.axm','');
        if(!created.ok)throw new Error(created.message||created.error);
        files=await root.axiom.listProjectFiles(folder);
      }
      if(ide.projectSession.sequence!==operation)return;
      const entry=files.find(p=>relative({rootDirectory:folder},p).toLowerCase()==='main.axm')||(files.length===1?files[0]:null);
      const file=folder+'/'+name+'.axproj';
      const text=JSON.stringify({formatVersion:1,name,entry:entry?relative({rootDirectory:folder},entry):null,sources:['**/*.axm'],assets:[],exclude:[]},null,2)+'\n';
      const response=await root.axiom.projectCommand('project.create',{path:file,text});
      if(!response.ok)throw new Error(response.error?.message);
      if(ide.projectSession.sequence!==operation)return;
      if(await ide.openProject(file)&&!entry)await properties(ide);
    }catch(error){report(ide,error,'project.createFailed');}
  }
  async function properties(ide){
    const project=ide.projectSession.descriptor;
    if(!project){if(ide.projectDir)return create(ide);ide.setStatus(t('project.noProject'));return;}
    if(ide.running)return;
    ++ide.projectSession.descriptorRequest;
    const existing=document.getElementById('project-properties-dialog');if(existing){existing.focus();return;}
    const dialog=document.createElement('dialog');dialog.id='project-properties-dialog';dialog.className='project-properties';
    const form=document.createElement('form');form.method='dialog';dialog.append(form);
    const heading=document.createElement('h2');heading.textContent=t('project.properties');form.append(heading);
    const original=JSON.parse(project.text.replace(/^\uFEFF/,''));const fields={};
    function field(key,value,multi=false){
      const label=document.createElement('label');label.textContent=t('project.'+key);
      const input=document.createElement(multi?'textarea':'input');input.name=key;input.value=value;input.id='project-property-'+key;label.htmlFor=input.id;label.append(input);form.append(label);fields[key]=input;
    }
    field('name',project.name);field('entry',original.entry||'');
    field('sources',(original.sources||['**/*.axm']).join('\n'),true);
    field('assets',(original.assets||[]).join('\n'),true);field('exclude',(original.exclude||[]).join('\n'),true);
    const hint=document.createElement('p');hint.textContent=t('project.pathsHint');form.append(hint);
    const error=document.createElement('p');error.className='project-entry-error';form.append(error);
    const buttons=document.createElement('div');buttons.className='project-properties-actions';form.append(buttons);
    const cancel=document.createElement('button');cancel.type='button';cancel.textContent=t('project.cancel');cancel.onclick=()=>dialog.close();buttons.append(cancel);
    const save=document.createElement('button');save.type='submit';save.textContent=t('project.save');buttons.append(save);
    form.onsubmit=async event=>{
      event.preventDefault();UIControls.setEnabled(save,false);
      try{
        if(ide.projectSession.descriptor!==project)throw new Error(t('project.changed'));
        const data={formatVersion:1,name:fields.name.value.trim(),entry:fields.entry.value.trim()||null};
        for(const key of ['sources','assets','exclude'])data[key]=fields[key].value.split(/\r?\n/).map(s=>s.trim()).filter(Boolean);
        const response=await root.axiom.projectCommand('project.save',{path:project.projectFilePath,expectedHash:project.contentHash,text:JSON.stringify(data,null,2)+'\n'});
        if(!response.ok)throw new Error(response.error?.message);
        if(ide.projectSession.descriptor!==project)throw new Error(t('project.changed'));
        ide.projectSession.applyDescriptor(response.payload);dialog.close();
      }catch(problem){error.textContent=problem.message;}finally{UIControls.setEnabled(save,true);}
    };
    dialog.onclose=()=>dialog.remove();document.body.append(dialog);dialog.showModal();fields.name.focus();
  }
  async function check(ide,explicit){
    const project=ide.projectSession.descriptor,sequence=ide.projectSession.sequence;
    try{
      const documents=[...ide.openFiles.values()].filter(d=>ide.projectSession.isSource(d.path)).map(d=>({sourceFilePath:d.path,sourceText:d.model.getValue(),revision:d.revision}));
      const response=await root.axiom.projectCommand('project.check',{path:project.projectFilePath,documents});
      if(ide.projectSession.descriptor!==project||ide.projectSession.sequence!==sequence||documents.some(d=>ide.openFiles.get(d.sourceFilePath)?.revision!==d.revision))return;
      if(!response.ok)throw new Error(response.error?.message);
      const diagnostics=response.payload.diagnostics.map(d=>({...d,file:d.sourceFilePath,col:d.column}));ide.applyDiagnostics(diagnostics,true);
      if(explicit)ide.switchPanel('diagnostics');
    }catch(error){report(ide,error);}
  }
  root.ProjectView={render,create,properties,check};
})(window);
