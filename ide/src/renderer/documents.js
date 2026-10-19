(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  let nextId = 0;
  const key = path => String(path).replace(/\//g, '\\').toLowerCase();
  class DocumentStore {
    constructor(onChange = () => {}) { this.entries = new Map(); this.onChange = onChange; this.navigationEntryId = null; }
    get size() { return this.entries.size; }
    has(path) { return this.entries.has(key(path)); }
    get(path) { return this.entries.get(key(path)); }
    set(path, value) {
      const existing = this.get(path);
      if (existing) {
        if (existing.model !== value.model) throw new Error('Документ уже открыт.');
        return this;
      }
      const entry = { ...value, path, documentId: 'document-' + (++nextId), revision: 0, savedRevision: 0,
        savedText: value.model.getValue(), breakpoints: new Set(), readonly: !!value.readonly };
      entry.subscription = value.model.onDidChangeContent(() => {
        if (entry.revision === 2147483647) {
          const selected = this.navigationEntryId === entry.documentId;
          entry.documentId = 'document-' + (++nextId); entry.revision = 0;
          if (selected) this.navigationEntryId = entry.documentId;
        }
        else entry.revision++;
        this.onChange();
      });
      this.entries.set(key(path), entry); this.onChange(); return this;
    }
    *keys() { for (const entry of this.entries.values()) yield entry.path; }
    *values() { yield* this.entries.values(); }
    *[Symbol.iterator]() { for (const entry of this.entries.values()) yield [entry.path, entry]; }
    forEach(callback) { for (const entry of this.entries.values()) callback(entry, entry.path, this); }
    delete(path) {
      const entry = this.get(path); if (!entry) return false;
      entry.subscription.dispose(); const deleted = this.entries.delete(key(path));
      if (this.navigationEntryId === entry.documentId) this.navigationEntryId = null;
      this.onChange(); return deleted;
    }
    rename(oldPath, newPath) {
      const entry = this.get(oldPath); if (!entry) throw new Error('Документ не найден.');
      const other = this.get(newPath); if (other && other !== entry) throw new Error('Целевой документ уже открыт.');
      this.entries.delete(key(oldPath)); entry.path = newPath; this.entries.set(key(newPath), entry);
      this.onChange();
      return entry;
    }
    isDirty(path) { const entry = this.get(path); return !!entry && (!!entry.missing || entry.model.getValue() !== entry.savedText); }
    markSaved(path, text) { const entry = this.get(path); if (entry) { entry.missing = false; entry.savedText = text === undefined ? entry.model.getValue() : text; entry.savedRevision = entry.revision; this.onChange(); } }
    captureProject(path) {
      const entryKey=key(path), folder=entryKey.slice(0,entryKey.lastIndexOf('\\')+1);
      const entries=()=>[...this.values()].filter(entry=>{
        const name=key(entry.path);
        return !name.startsWith('untitled') && name.endsWith('.axm') && name!==entryKey &&
          name.slice(0,name.lastIndexOf('\\')+1)===folder && !entry.model.isDisposed?.();
      }).sort((a,b)=>key(a.path).localeCompare(key(b.path)));
      const captured=String(path).startsWith('untitled') ? [] : entries();
      const documents=captured.map(entry=>({documentId:entry.documentId,documentRevision:entry.revision,
        sourceFilePath:entry.path,sourceText:entry.model.getValue()}));
      return {documents,isCurrent:()=>{
        const current=String(path).startsWith('untitled') ? [] : entries();
        return current.length===captured.length && current.every((entry,index)=>entry===captured[index] &&
          entry.path===documents[index].sourceFilePath && entry.documentId===documents[index].documentId &&
          entry.revision===documents[index].documentRevision && entry.model.getValue()===documents[index].sourceText);
      }};
    }
  }
  return { DocumentStore };
});
