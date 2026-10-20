(function(root) {
  class EditorFileDrop {
    constructor(ide, api) {
      this.ide = ide;
      this.api = api;
      this.pending = Promise.resolve();
    }

    setup(document) {
      for (const type of ['dragenter', 'dragover', 'drop']) {
        document.addEventListener(type, event => this.handle(event), { capture: true });
      }
    }

    handle(event) {
      const data = event.dataTransfer;
      if (!Array.from(data?.types || []).includes('Files')) return;
      // Файлы никогда не должны становиться страницами Chromium, даже вне редактора.
      event.preventDefault();
      event.stopPropagation();
      const editor = this.ide.editor;
      const accepts = !!editor && editor.getRawOptions().dropIntoEditor?.enabled !== false &&
        !!event.target.closest?.('#editor-container');
      data.dropEffect = accepts ? 'copy' : 'none';
      if (!accepts || event.type !== 'drop') return;
      const paths = Array.from(data.files, file => this.api.getDroppedFilePath(file)).filter(Boolean);
      this.pending = this.pending.then(async () => {
        for (const path of paths) await this.ide.openFile(path);
      }).catch(error => this.ide.log('document.drop.failed', { message: error.message }, 'error'));
    }
  }

  root.EditorFileDrop = EditorFileDrop;
})(typeof window === 'undefined' ? globalThis : window);
