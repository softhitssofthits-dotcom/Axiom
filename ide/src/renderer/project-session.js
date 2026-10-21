// Сеанс папки проекта: состав вкладок и явная точка входа.
// Здесь нет правил связывания IMPORT; их по-прежнему определяет DesktopHost.
(function(root,factory){
  if(typeof module==='object'&&module.exports)module.exports=factory();
  else Object.assign(root,factory());
})(typeof window!=='undefined'?window:globalThis,function(){
  const key=value=>String(value||'').replace(/\//g,'\\').replace(/\\+$/,'').toLowerCase();
  const record=value=>value&&typeof value==='object'&&!Array.isArray(value)?value:{};
  class ProjectSession {
    constructor(ide,api){this.ide=ide;this.api=api;this.files=[];this.entry=null;this.descriptor=null;this.descriptorRequest=0;this.sequence=0;this.loading=false;}
    contains(file){if(this.descriptor)return [...this.descriptor.sourceFiles,...this.descriptor.assetFiles].some(p=>key(p)===key(file));return !!this.ide.projectDir&&key(file).startsWith(key(this.ide.projectDir)+'\\');}
    isSource(file){return this.descriptor?this.descriptor.sourceFiles.some(p=>key(p)===key(file)):this.contains(file);}
    relative(file){return file.slice(this.ide.projectDir.replace(/[\\/]+$/,'').length+1).replace(/\\/g,'/');}
    initialEntry(files){
      const saved=record(this.ide.settings.projectEntries),folder=key(this.ide.projectDir);
      if(Object.hasOwn(saved,folder)){
        if(saved[folder]===null)return null;
        if(typeof saved[folder]==='string'){
          const matched=files.find(file=>key(this.relative(file))===key(saved[folder]));
          if(matched)return matched;
        }
      }
      return files.find(file=>key(this.relative(file))==='main.axm')||(files.length===1?files[0]:null);
    }
    persist(){
      if(!this.ide.projectDir||this.descriptor)return;
      this.ide.saveSettings({projectEntries:{...record(this.ide.settings.projectEntries),
        [key(this.ide.projectDir)]:this.entry?this.relative(this.entry):null}});
    }
    navigation(){
      const document=this.entry&&this.ide.openFiles.get(this.entry);
      this.ide.openFiles.navigationEntryId=document?.documentId||null;
      this.ide.assistant?.invalidate();
    }
    select(file){
      if(this.descriptor)return this.setDescriptorEntry(file||null);
      if(this.loading)return false;
      const entry=file?this.files.find(p=>key(p)===key(file)):null;
      if(file&&!entry)return false;
      const previous=this.entry&&this.ide.openFiles.get(this.entry);
      this.entry=entry||null;
      if(!entry&&previous?.documentId===this.ide.openFiles.navigationEntryId)this.ide.openFiles.navigationEntryId=null;
      this.persist();this.navigation();this.render();return true;
    }
    render(){this.ide.renderProjectEntry?.(this.files,this.entry,this.loading);}
    refresh(files){
      this.files=files.filter(file=>this.contains(file)&&/\.axm$/i.test(file));
      if(this.entry&&!this.files.some(file=>key(file)===key(this.entry))){
        this.entry=null;this.persist();
      }
      this.render();
    }
    renamed(oldPath,newPath){
      if(this.descriptor&&key(this.entry)===key(oldPath)){
        if(!key(newPath).startsWith(key(this.ide.projectDir)+'\\'))return false;
        return this.setDescriptorEntry(this.relative(newPath),true);
      }
      if(this.entry&&key(this.entry)===key(oldPath)){
        this.entry=this.contains(newPath)&&/\.axm$/i.test(newPath)?newPath:null;
        this.persist();this.navigation();
      }
    }
    reset(){
      const selected=this.entry&&this.ide.openFiles.get(this.entry);
      if(selected?.documentId===this.ide.openFiles.navigationEntryId)this.ide.openFiles.navigationEntryId=null;
      this.sequence++;this.files=[];this.entry=null;this.descriptor=null;this.loading=false;this.render();
    }
    target(){
      if(this.loading)return null;
      if(this.descriptor)return this.entry;
      if(!this.ide.projectDir)return this.ide.activeFile;
      return !this.ide.activeFile||this.contains(this.ide.activeFile)?this.entry:this.ide.activeFile;
    }
    applyDescriptor(descriptor){
      this.descriptor=descriptor;this.ide.projectDir=descriptor.rootDirectory;
      this.files=descriptor.sourceFiles;this.entry=descriptor.entryError?null:descriptor.sourceFiles.find(p=>key(p)===key(descriptor.entryPath))||null;
      this.ide.renderProjectFiles(this.files);this.navigation();
    }
    async openDescriptor(descriptor,current,restore){
      this.ide.projectFiles.reset();
      this.applyDescriptor(descriptor);
      this.ide.saveSettings({lastProjectFile:descriptor.projectFilePath,lastProjectDir:descriptor.rootDirectory});
      if(!restore)this.ide.setProjectExplorerVisible?.(true);
      if(!restore&&this.entry){
        await this.ide.openFile(this.entry,current);
        if(!current())return false;
        for(const document of [...this.ide.openFiles.values()])if(document.path?.startsWith('untitled')&&!document.model.getValue())await this.ide.closeFile(document.path,true);
      }
      if(current())this.navigation();return current();
    }
    async reload(){
      const descriptor=this.descriptor;if(!descriptor||this.loading)return;
      const operation=this.sequence,request=++this.descriptorRequest;
      const response=await this.api.projectCommand('project.open',{path:descriptor.projectFilePath});
      if(this.sequence!==operation||this.descriptor!==descriptor||this.descriptorRequest!==request)return;
      if(!response.ok)throw new Error(response.error?.message||'Не удалось обновить проект.');
      this.applyDescriptor(response.payload);
    }
    async setDescriptorEntry(file,relative=false){
      if(this.loading||this.ide.running)return false;
      const descriptor=this.descriptor,operation=this.sequence,request=++this.descriptorRequest;
      const entry=file?(relative?file:this.relative(file)):null;
      const response=await this.api.projectCommand('project.entry',{path:descriptor.projectFilePath,expectedHash:descriptor.contentHash,entry});
      if(this.sequence!==operation||this.descriptor!==descriptor||this.descriptorRequest!==request)return false;
      if(!response.ok)throw new Error(response.error?.message||'Не удалось сохранить точку входа.');
      this.applyDescriptor(response.payload);return true;
    }
    async open(dir,{restore=false}={}){
      if(this.ide.running)return false;
      const request=++this.sequence;
      const current=()=>request===this.sequence;
      this.loading=true;this.render();
      try{
        if(this.api.projectCommand){
          const response=await this.api.projectCommand('project.open',{path:dir});
          if(!current())return false;
          if(!response.ok)throw new Error(response.error?.message||'Не удалось открыть проект.');
          if(!response.payload.legacy)return await this.openDescriptor(response.payload,current,restore);
        }
        this.descriptor=null;
        this.ide.saveSettings({lastProjectFile:null});
        const files=await this.api.listProjectFiles(dir);
        if(!current())return false;
        this.ide.projectDir=dir;
        this.files=files.filter(file=>this.contains(file)&&/\.axm$/i.test(file));
        this.entry=this.initialEntry(this.files);
        this.ide.projectFiles.reset();
        this.ide.saveSettings({lastProjectDir:dir});
        this.ide.renderProjectFiles(this.files);
        if(restore){this.navigation();return true;}
        this.ide.setProjectExplorerVisible?.(true);
        // Непустые документы и правки не закрываем. Удаляем только стартовый пустой лист.
        const empty=[...this.ide.openFiles.values()].filter(d=>d.path?.startsWith('untitled')&&!d.model.getValue());
        for(const file of this.files){
          if(!current())return false;
          await this.ide.openFile(file,current,{activate:false});
        }
        if(!current())return false;
        if(this.files.some(file=>this.ide.openFiles.has(file))){
          for(const document of empty){
            if(!current())return false;
            if(this.ide.openFiles.get(document.path)===document&&!document.model.getValue())await this.ide.closeFile(document.path,true);
          }
          if(!current())return false;
          const active=this.entry||this.files.find(file=>this.ide.openFiles.has(file));
          if(active)this.ide.switchToFile(active);
        }
        this.navigation();this.persist();return true;
      }finally{if(current()){this.loading=false;this.render();}}
    }
  }
  return {ProjectSession};
});
