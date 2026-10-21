(function(root) {
  class ProjectCreateDialog {
    constructor(ide) { this.ide = ide; }
    text(key) { return root.I18N.t('create.' + key); }
    show() {
      if (this.overlay) { this.name.focus(); return; }
      const make = (tag, text, id) => {
        const node = document.createElement(tag);
        if (text) node.textContent = text;
        if (id) node.id = id;
        return node;
      };
      this.previousFocus = document.activeElement;
      const overlay = this.overlay = make('div'); overlay.className = 'modal-overlay';
      const form = make('form', '', 'project-create-dialog');
      form.className = 'editor-settings-dialog project-create-dialog';
      form.setAttribute('role', 'dialog'); form.setAttribute('aria-modal', 'true');
      form.setAttribute('aria-labelledby', 'project-create-title');
      const title = make('h2', this.text('title'), 'project-create-title');
      const nameLabel = make('label', this.text('name')); nameLabel.htmlFor = 'project-create-name';
      this.name = make('input', '', 'project-create-name'); this.name.required = true;
      this.name.maxLength = 255; this.name.value = this.text('defaultName');
      const parentLabel = make('label', this.text('parent')); parentLabel.htmlFor = 'project-create-parent';
      this.parent = make('input', '', 'project-create-parent'); this.parent.readOnly = true;
      const browse = make('button', this.text('browse'), 'project-create-browse'); browse.type = 'button';
      let parentChosen = false;
      browse.onclick = async () => {
        try {
          const parent = await root.axiom.chooseProjectParent(this.parent.value);
          if (this.overlay === overlay && parent) { parentChosen = true; this.parent.value = parent; }
        } catch (error) { if (this.overlay === overlay) this.error.textContent = error.message; }
      };
      const row = make('div'); row.className = 'project-create-location'; row.append(this.parent, browse);
      this.error = make('p', '', 'project-create-error'); this.error.setAttribute('role', 'alert');
      const cancel = make('button', this.text('cancel')); cancel.type = 'button'; cancel.onclick = () => this.close();
      const submit = make('button', this.text('submit'), 'project-create-submit'); submit.type = 'submit';
      const footer = make('footer'); footer.className = 'settings-footer'; footer.append(cancel, submit);
      form.append(title, nameLabel, this.name, parentLabel, row, make('p', this.text('hint')), this.error, footer);
      form.onsubmit = async event => {
        event.preventDefault(); if (this.busy) return;
        this.error.textContent = '';
        this.busy = true;
        const controls = [...form.querySelectorAll('input,button')]; controls.forEach(node => root.UIControls.setEnabled(node, false));
        try {
          if (this.ide.running) throw new Error(root.I18N.t('project.stopFirst'));
          const result = await root.axiom.createProject(this.parent.value, this.name.value);
          if (!result?.ok) throw new Error(result?.message || this.text('failed'));
          if (!await this.ide.openProject(result.path)) throw new Error(this.text('openFailed') + ' ' + result.path);
          this.busy = false; this.close(); this.ide.focusEditor();
        } catch (error) { this.error.textContent = error.message || this.text('failed'); }
        finally { this.busy = false; controls.forEach(node => root.UIControls.setEnabled(node, true)); }
      };
      overlay.onkeydown = event => {
        if (event.key === 'Escape') { event.preventDefault(); this.close(); }
        if (event.key !== 'Tab') return;
        const nodes = [...form.querySelectorAll('input,button')].filter(node => !node.disabled);
        const first = nodes[0], last = nodes.at(-1);
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
      };
      overlay.append(form); this.app = document.getElementById('app');
      this.wasInert = this.app.inert; this.app.inert = true;
      document.body.append(overlay); this.name.focus(); this.name.select();
      root.axiom.getProjectCreationDefaults().then(result => {
        if (this.overlay === overlay && !parentChosen) this.parent.value = result.parent;
      }).catch(error => { if (this.overlay === overlay) this.error.textContent = error.message; });
    }
    close() {
      if (!this.overlay || this.busy) return;
      this.overlay.remove(); this.overlay = null; this.app.inert = this.wasInert;
      if (this.previousFocus?.isConnected) this.previousFocus.focus(); else this.ide.focusEditor();
    }
  }
  root.ProjectCreateDialog = ProjectCreateDialog;
})(window);
