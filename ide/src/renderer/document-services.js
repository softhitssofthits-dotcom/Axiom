(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  class DocumentServices {
    constructor(store, api) { this.store = store; this.api = api; this.requests = new WeakMap(); }
    async request(path, method, options = {}) {
      const document = this.store.get(path);
      if (!document || document.model.isDisposed?.()) return null;
      let methods = this.requests.get(document);
      if (!methods) { methods = new Map(); this.requests.set(document, methods); }
      const token = {}; methods.set(method, token);
      const revision = document.revision;
      const canonicalPath = document.path;
      const projectOperation=['document.compile','document.analyse','document.hover','document.navigation'].includes(method);
      const project=projectOperation ? this.store.captureProject?.(canonicalPath) : null;
      const entryId=this.store.navigationEntryId;
      const entry=method==='document.navigation'&&entryId ? [...this.store.values()].find(d=>d.documentId===entryId) : null;
      const sameFolder=entry&&!entry.path.startsWith('untitled')&&String(entry.path).replace(/\\/g,'/').split('/').slice(0,-1).join('/').toLowerCase()===String(canonicalPath).replace(/\\/g,'/').split('/').slice(0,-1).join('/').toLowerCase();
      const snapshot = { ...options, documentId: document.documentId, documentRevision: revision,
        sourceText: document.model.getValue(), sourceFilePath: document.path.startsWith('untitled') ? null : document.path,
        ...(project ? {projectDocuments:project.documents} : {}),
        ...(sameFolder ? {navigationEntryPath:entry.path} : {}) };
      const current = () => methods.get(method) === token && this.store.get(canonicalPath) === document && document.path === canonicalPath &&
          document.documentId === snapshot.documentId && document.revision === revision && !document.model.isDisposed?.() &&
          (!project || project.isCurrent()) && (method!=='document.navigation'||this.store.navigationEntryId===entryId);
      let response;
      try { response = await this.api.documentCommand(method, snapshot); }
      catch (error) { if (!current()) return null; throw error; }
      if (!current()) return null;
      if (!response.ok) throw new Error(response.error?.message || 'Ошибка обработки документа.');
      if (response.payload.documentId !== snapshot.documentId || response.payload.documentRevision !== revision)
        throw new Error('Ответ относится к другому снимку документа.');
      return response.payload;
    }
  }
  return { DocumentServices };
});
