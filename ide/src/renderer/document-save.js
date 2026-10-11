(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  class DocumentSaver {
    constructor(store, api) { this.store = store; this.api = api; this.pending = new WeakSet(); }
    async save(path, saveAs = false) {
      const entry = this.store.get(path);
      if (!entry || entry.readonly || this.pending.has(entry)) return null;
      this.pending.add(entry);
      try {
        let target = entry.path;
        if (saveAs || target.startsWith('untitled')) {
          target = await this.api.saveFileDialog(target.startsWith('untitled') ? undefined : target);
          if (!target) return null;
        }
        if (this.store.get(path) !== entry || entry.path !== path) return null;
        const other = this.store.get(target);
        if (other && other !== entry) throw new Error('Этот файл уже открыт в другой вкладке.');
        const text = entry.model.getValue();
        const sameTarget = this.store.get(target) === entry;
        const saved = await this.api.writeFile(target, text, sameTarget ? (entry.missing ? null : entry.savedText) : undefined);
        if (saved === null) return null;
        if (!saved) throw new Error('Не удалось сохранить файл: ' + target);
        if (this.store.get(path) !== entry || entry.path !== path) return null;
        this.store.rename(path, target);
        this.store.markSaved(target, text);
        return { oldPath: path, path: target, entry };
      } finally { this.pending.delete(entry); }
    }
  }
  return { DocumentSaver };
});
