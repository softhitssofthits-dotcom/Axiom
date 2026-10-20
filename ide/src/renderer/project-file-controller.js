// Файловая панель: внутренний буфер проекта, корзина и свойства.
// Все пути окончательно проверяет main-процесс; здесь только выбор цели
// и согласование уже открытых документов с результатом операции.
(function(root) {
  class ProjectFileController {
    constructor(ide, api = window.axiom) {
      this.ide = ide; this.api = api; this.path = null; this.clipboard = null; this.popup = null;
    }
    reset() { this.clipboard = null; this.path = null; this.popup?.close(false); }
    setup() {
      const menu = document.getElementById('ax-project-context-menu');
      if (!menu || !root.UIControls) return;
      this.popup = UIControls.createContextMenu(menu, {
        translate: I18N.t,
        onClose: () => this.ide.editor?.focus(),
        onAction: id => this.action(id),
      });
      const files = document.getElementById('project-files');
      files?.addEventListener('contextmenu', event => {
        if (!this.ide.projectDir) return;
        const item = event.target.closest?.('.project-file');
        if (!item) return;
        event.preventDefault(); event.stopPropagation();
        this.show(item.dataset.path || item.title, event.clientX, event.clientY);
      });
      const explorer = document.getElementById('project-explorer');
      explorer?.addEventListener('contextmenu', event => {
        if (!this.ide.projectDir || event.target.closest?.('.project-file')) return;
        event.preventDefault(); event.stopPropagation();
        this.show(null, event.clientX, event.clientY);
      });
    }
    items(path) {
      const hasClipboard = !!this.clipboard;
      if (!path) return [
        { id: 'paste', translation: 'files.paste', icon: 'codicon-files', enabled: hasClipboard },
      ];
      return [
        { id: 'open', translation: 'files.open', icon: 'codicon-go-to-file' },
        { separator: true },
        { id: 'copy', translation: 'files.copy', icon: 'codicon-copy' },
        { id: 'cut', translation: 'files.cut', icon: 'codicon-screen-cut' },
        { id: 'paste', translation: 'files.paste', icon: 'codicon-files', enabled: hasClipboard },
        { separator: true },
        { id: 'trash', translation: 'files.delete', icon: 'codicon-trash' },
        { id: 'properties', translation: 'files.properties', icon: 'codicon-info' },
      ];
    }
    show(path, x, y) {
      this.path = path;
      this.popup?.show(this.items(path), x, y);
    }
    contains(parent, child) {
      const relative = requirePath().relative(parent, child);
      return relative === '' || (relative !== '..' && !relative.startsWith('..' + requirePath().sep) && !requirePath().isAbsolute(relative));
    }
    affected(path, directory) {
      const key = value => value.replace(/\//g, '\\').toLowerCase();
      const source = key(path);
      return [...this.ide.openFiles.keys()].filter(open => {
        const current = key(open);
        return directory ? current === source || current.startsWith(source + '\\') : current === source;
      });
    }
    movedPath(source, target, directory, oldPath) {
      if (!directory) return target;
      const path = requirePath();
      return path.join(target, path.relative(source, oldPath));
    }
    async action(id) {
      const path = this.path;
      if (id === 'open') return this.ide.openFile(path);
      if (id === 'copy' || id === 'cut') return this.capture(path, id === 'cut');
      if (id === 'paste') return this.paste();
      if (id === 'trash') return this.trash(path);
      if (id === 'properties') return this.properties(path);
    }
    async capture(path, cut) {
      const root = this.ide.projectDir;
      const result = await this.api.captureProjectFile(root, path, cut);
      if (this.ide.projectDir !== root) return;
      if (!result.ok) { this.ide.setStatus(I18N.t('files.status.failed')); alert(result.error); return; }
      // Main проверяет канонический путь, вкладки сохраняют путь выбранной папки.
      this.clipboard = { ...result, displaySource: path };
      const name = path.split(/[/\\]/).pop();
      this.ide.setStatus(I18N.t(cut ? 'files.status.cut' : 'files.status.copied', name));
    }
    async paste() {
      const root = this.ide.projectDir;
      if (!root || !this.clipboard) return;
      const clip = this.clipboard;
      const displaySource=clip.displaySource||clip.source;
      const affected = clip.cut ? this.affected(displaySource, clip.directory) : [];
      if (affected.some(p => this.ide.running && p.toLowerCase() === this.ide._runFile?.toLowerCase())) {
        alert(I18N.t('files.runningBlocked'));
        return;
      }
      const result = await this.api.pasteProjectFile(root, root);
      if (this.ide.projectDir !== root) return;
      if (!result.ok) { this.ide.setStatus(I18N.t('files.status.failed')); alert(result.error); await this.ide.refreshProjectFiles(); return; }
      if (result.cut) {
        const paths=requirePath();
        const displayTarget=clip.root&&this.contains(clip.root,result.target)
          ? paths.join(root,paths.relative(clip.root,result.target)) : result.target;
        for (const oldPath of affected) {
          const newPath = this.movedPath(displaySource, displayTarget, result.directory, oldPath);
          await this.ide.renameActive(newPath, oldPath);
        }
        this.clipboard = null;
      }
      await this.ide.refreshProjectFiles();
      this.ide.setStatus(I18N.t(result.cut ? 'files.status.moved' : 'files.status.pasted',
        result.target.split(/[/\\]/).pop()));
    }
    async trash(path) {
      const directory = !/\.axm$/i.test(path);
      const affected = this.affected(path, directory);
      if (this.ide.running && affected.some(p => p.toLowerCase() === this.ide._runFile?.toLowerCase())) {
        alert(I18N.t('files.runningBlocked'));
        return;
      }
      const name = path.split(/[/\\]/).pop();
      if (!confirm(I18N.t(directory ? 'files.confirmDirectory' : 'files.confirmFile', name))) {
        this.ide.setStatus(I18N.t('files.status.deleteCancelled'));
        return;
      }
      for (const open of affected) if (!await this.ide.prepareClose(open)) return;
      const root = this.ide.projectDir;
      const result = await this.api.trashProjectFile(root, path);
      if (this.ide.projectDir !== root) return;
      if (!result.ok) {
        this.ide.setStatus(I18N.t('files.status.failed'));
        alert(result.error);
        await this.ide.refreshProjectFiles();
        return;
      }
      for (const open of affected) await this.ide.closeFile(open, true);
      if (this.clipboard && this.contains(result.source, this.clipboard.source)) this.clipboard = null;
      await this.ide.refreshProjectFiles();
      this.ide.setStatus(I18N.t('files.status.recycled', name));
    }
    async properties(path) {
      const root = this.ide.projectDir;
      const result = await this.api.projectFileProperties(root, path);
      if (this.ide.projectDir !== root) return;
      if (!result.ok) { this.ide.setStatus(I18N.t('files.status.failed')); alert(result.error); return; }
      const lines = [
        `${I18N.t('files.properties.name')}: ${result.name}`,
        `${I18N.t('files.properties.type')}: ${I18N.t(result.kind === 'directory' ? 'files.properties.directory' : 'files.properties.file')}`,
        `${I18N.t('files.properties.path')}: ${result.path}`,
      ];
      if (result.kind !== 'directory') lines.push(`${I18N.t('files.properties.size')}: ${result.size} ${I18N.t('files.properties.bytes')}`);
      lines.push(`${I18N.t('files.properties.modified')}: ${result.modified}`);
      lines.push(`${I18N.t('files.properties.created')}: ${result.created}`);
      lines.push(`${I18N.t('files.properties.readonly')}: ${I18N.t(result.readonly ? 'files.properties.yes' : 'files.properties.no')}`);
      alert(lines.join('\n'));
      this.ide.setStatus(I18N.t('files.status.properties', result.name));
    }
  }
  function requirePath() {
    // Renderer не имеет Node.js. Пути здесь уже каноничны для Windows,
    // потому что их вернул main-процесс после realpath.
    if (!requirePath.module) {
      requirePath.module = {
        sep: '\\',
        relative(parent, child) {
          const trim = value => value.replace(/\//g, '\\').replace(/[\\]+$/, '');
          const rawBase = trim(parent), rawTarget = trim(child);
          const base = rawBase.toLowerCase(), target = rawTarget.toLowerCase();
          if (base === target) return '';
          return target.startsWith(base + '\\') ? rawTarget.slice(base.length + 1) : '..';
        },
        isAbsolute(value) { return /^[a-z]:\\/i.test(value); },
        join(base, relative) { return relative ? base.replace(/[\\/]+$/, '') + '\\' + relative : base; },
      };
    }
    return requirePath.module;
  }
  root.ProjectFileController = ProjectFileController;
})(typeof window === 'undefined' ? globalThis : window);
