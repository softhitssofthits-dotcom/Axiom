(function(root) {
  class EventLogDialog {
    constructor(ide, api = root.axiom) { this.ide = ide; this.api = api; this.sequence = 0; }
    text(key, ...args) { return root.I18N.t('logs.' + key, ...args); }
    element(tag, className, text) {
      const element = root.document.createElement(tag);
      if (className) element.className = className;
      if (text !== undefined) element.textContent = text;
      return element;
    }
    button(key, action) {
      const button = this.element('button', '', this.text(key)); button.type = 'button';
      button.dataset.logAction = key;
      root.UIControls.setLabel(button, this.text(key)); button.onclick = action; return button;
    }
    show() {
      if (this.overlay) { this.search.focus(); return; }
      this.previousFocus = root.document.activeElement;
      this.overlay = this.element('div', 'modal-overlay');
      const dialog = this.element('section', 'editor-settings-dialog event-log-dialog');
      dialog.setAttribute('role', 'dialog'); dialog.setAttribute('aria-modal', 'true');
      root.UIControls.setLabel(dialog, this.text('title'), { tooltip: false });
      const header = this.element('header', 'settings-header');
      header.append(this.element('h2', '', this.text('title')), this.button('close', () => this.close()));
      const filters = this.element('div', 'event-log-filters');
      this.search = this.element('input', ''); this.search.type = 'search';
      this.search.placeholder = this.text('search'); root.UIControls.setLabel(this.search, this.text('search'));
      this.search.oninput = () => { clearTimeout(this.timer); this.timer = setTimeout(() => this.refresh(), 150); };
      this.level = this.element('select', ''); root.UIControls.setLabel(this.level, this.text('level'));
      for (const key of ['', 'info', 'warn', 'error']) { const option = this.element('option', '', this.text(key || 'allLevels')); option.value = key; this.level.append(option); }
      this.level.onchange = () => this.refresh();
      this.file = this.element('select', ''); root.UIControls.setLabel(this.file, this.text('file'));
      this.file.onchange = () => this.refresh();
      const all = this.element('option', '', this.text('allFiles')); all.value = ''; this.file.append(all);
      filters.append(this.search, this.level, this.file);
      this.view = this.element('textarea', 'event-log-text'); this.view.readOnly = true;
      this.view.spellcheck = false; root.UIControls.setLabel(this.view, this.text('entries'));
      this.status = this.element('div', 'settings-status'); this.status.setAttribute('role', 'status');
      const footer = this.element('footer', 'settings-footer event-log-actions');
      this.refreshButton = this.button('refresh', () => this.refresh());
      this.copyButton = this.button('copy', () => this.copy());
      this.openButton = this.button('openFile', () => this.openFile());
      this.folderButton = this.button('openFolder', () => this.openFolder());
      this.clearButton = this.button('clear', () => this.clear());
      footer.append(this.refreshButton, this.copyButton, this.openButton, this.folderButton, this.clearButton);
      dialog.append(header, filters, this.view, this.status, footer); this.overlay.append(dialog);
      this.overlay.onkeydown = event => {
        if (event.key === 'Escape') { event.preventDefault(); this.close(); return; }
        if (event.key !== 'Tab') return;
        const controls = [...dialog.querySelectorAll('button,input,select,textarea')].filter(element => !element.disabled);
        const first = controls[0], last = controls.at(-1), active = root.document.activeElement;
        if (event.shiftKey && active === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && active === last) { event.preventDefault(); first.focus(); }
      };
      this.app = root.document.getElementById('app'); this.wasInert = this.app.inert; this.app.inert = true;
      root.document.body.append(this.overlay); this.search.focus(); this.refresh();
    }
    filters() { return { search: this.search.value, level: this.level.value, file: this.file.value || null }; }
    setBusy(busy) {
      for (const button of [this.refreshButton, this.copyButton, this.openButton, this.clearButton]) root.UIControls.setEnabled(button, !busy);
      if (!busy) {
        root.UIControls.setEnabled(this.copyButton, !!this.view.value);
        root.UIControls.setEnabled(this.openButton, !!this.files?.length);
      }
    }
    showError(message) { this.status.textContent = this.text('failed', message); }
    async refresh() {
      const sequence = ++this.sequence, overlay = this.overlay;
      this.setBusy(true);
      const current = () => overlay && this.overlay === overlay && this.sequence === sequence;
      try {
        const result = await this.api.readEventLog(this.filters());
        if (!current()) return;
        if (!result?.ok) throw new Error(result?.error || this.text('unknown'));
        this.render(result);
      } catch (error) { if (current()) this.showError(error?.message || String(error)); }
      finally { if (current()) this.setBusy(false); }
    }
    render(result) {
      this.files = result.files; const selected = this.file.value;
      this.file.replaceChildren(); const all = this.element('option', '', this.text('allFiles')); all.value = ''; this.file.append(all);
      for (const name of result.files) { const option = this.element('option', '', name); option.value = name; this.file.append(option); }
      this.file.value = result.files.includes(selected) ? selected : '';
      this.view.value = result.entries.map(entry => entry.text).join('\n');
      this.status.textContent = this.text('count', result.entries.length) + (result.truncated ? ' ' + this.text('limited') : '') +
        (result.invalidLines ? ' ' + this.text('invalid', result.invalidLines) : '');
      if (result.errors.length) this.showError(result.errors.map(error => error.file + ': ' + error.message).join('; '));
    }
    async action(callback) {
      const overlay = this.overlay;
      try {
        const result = await callback();
        if (this.overlay !== overlay || !overlay) return null;
        if (!result?.ok) throw new Error(result?.error || result?.errors?.map(error => error.file + ': ' + error.message).join('; ') || this.text('unknown'));
        return result;
      } catch (error) { if (overlay && this.overlay === overlay) this.showError(error?.message || String(error)); return null; }
    }
    async copy() {
      const copied = await this.action(async () => ({ ok: await this.api.writeClipboardText(this.view.value) }));
      if (copied) this.status.textContent = this.text('copied');
    }
    async openFile() { await this.action(() => this.api.openEventLogFile(this.file.value || (this.files.includes('ide.log') ? 'ide.log' : this.files.at(-1)))); }
    async openFolder() { await this.action(() => this.api.openLogFolder()); }
    async clear() { if (await this.action(() => this.api.clearEventLog())) await this.refresh(); }
    close() {
      if (!this.overlay) return;
      clearTimeout(this.timer); this.sequence++;
      this.overlay.remove(); this.overlay = null; this.app.inert = this.wasInert;
      if (this.previousFocus?.isConnected) this.previousFocus.focus(); else this.ide.focusEditor();
    }
  }
  root.EventLogDialog = EventLogDialog;
  if (typeof module === 'object' && module.exports) module.exports = { EventLogDialog };
})(typeof window === 'undefined' ? globalThis : window);
