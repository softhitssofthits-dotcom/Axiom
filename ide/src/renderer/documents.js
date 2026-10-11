(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  let nextId = 0;
  const key = path => String(path).replace(/\//g, '\\').toLowerCase();
  class DocumentStore {
    constructor() { this.entries = new Map(); }
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
        if (entry.revision === 2147483647) { entry.documentId = 'document-' + (++nextId); entry.revision = 0; }
        else entry.revision++;
      });
      this.entries.set(key(path), entry); return this;
    }
    *keys() { for (const entry of this.entries.values()) yield entry.path; }
    *values() { yield* this.entries.values(); }
    *[Symbol.iterator]() { for (const entry of this.entries.values()) yield [entry.path, entry]; }
    forEach(callback) { for (const entry of this.entries.values()) callback(entry, entry.path, this); }
    delete(path) { const entry = this.get(path); if (!entry) return false; entry.subscription.dispose(); return this.entries.delete(key(path)); }
    rename(oldPath, newPath) {
      const entry = this.get(oldPath); if (!entry) throw new Error('Документ не найден.');
      const other = this.get(newPath); if (other && other !== entry) throw new Error('Целевой документ уже открыт.');
      this.entries.delete(key(oldPath)); entry.path = newPath; this.entries.set(key(newPath), entry);
      return entry;
    }
    isDirty(path) { const entry = this.get(path); return !!entry && (!!entry.missing || entry.model.getValue() !== entry.savedText); }
    markSaved(path, text) { const entry = this.get(path); if (entry) { entry.missing = false; entry.savedText = text === undefined ? entry.model.getValue() : text; entry.savedRevision = entry.revision; } }
  }
  return { DocumentStore };
});
