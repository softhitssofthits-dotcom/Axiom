// Перестановка существующих команд до подключения обработчиков меню.
(() => {
  function build() {
    const bar = document.getElementById('menubar');
    if (!bar || bar.dataset.studioMenus === 'true') return;
    const actions = new Map([...bar.querySelectorAll('.menu-dropdown-item[data-action]')].map(node => [node.dataset.action, node]));
    const translate = (node, key) => {
      node.dataset.i18n = key;
      node.textContent = I18N.t(key);
    };
    function command(action, key, icon, check) {
      let node = actions.get(action);
      if (!node) {
        node = document.createElement('div');
        node.className = 'menu-dropdown-item';
        node.dataset.action = action;
        const marker = document.createElement('span');
        marker.className = check ? 'menu-check' : 'menu-icon codicon codicon-' + (icon || 'circle-outline');
        if (check) { marker.dataset.check = check; marker.textContent = '✓'; }
        else marker.setAttribute('aria-hidden', 'true');
        const label = document.createElement('span');
        label.className = 'menu-text';
        node.append(marker, label);
      }
      if (key) translate(node.querySelector('.menu-text'), key);
      return node;
    }
    const separator = () => { const node = document.createElement('div'); node.className = 'menu-separator'; return node; };
    function menu(name, key, entries, id) {
      let node = id ? document.getElementById(id) : bar.querySelector('[data-menu="' + name + '"]');
      if (!node) {
        node = document.createElement('div'); node.className = 'menu-item';
        const label = document.createElement('span'); label.className = 'menu-label';
        const dropdown = document.createElement('div'); dropdown.className = 'menu-dropdown';
        node.append(label, dropdown);
      }
      node.dataset.menu = name;
      translate(node.querySelector('.menu-label'), key);
      node.querySelector('.menu-dropdown').replaceChildren(...entries);
      return node;
    }
    function submenu(key, entries, icon) {
      const node = document.createElement('div'); node.className = 'menu-item-submenu';
      const trigger = document.createElement('div'); trigger.className = 'menu-dropdown-item submenu-trigger';
      if (icon) {
        const marker = document.createElement('span');
        marker.className = 'menu-icon codicon codicon-' + icon;
        marker.setAttribute('aria-hidden', 'true');
        trigger.append(marker);
      }
      const label = document.createElement('span'); label.className = 'menu-text'; translate(label, key);
      const arrow = document.createElement('span'); arrow.className = 'submenu-arrow codicon codicon-chevron-right'; arrow.setAttribute('aria-hidden', 'true');
      trigger.append(label, arrow);
      const dropdown = document.createElement('div'); dropdown.className = 'submenu-dropdown'; dropdown.append(...entries);
      node.append(trigger, dropdown); return node;
    }
    // Ссылки сохраняются до перемещения дочерних элементов между меню.
    const recent = document.getElementById('recent-submenu');
    const formatting = document.getElementById('formatting-submenu');
    const language = document.getElementById('lang-submenu');
    const smart = command('smart-identifier');
    smart.remove();
    formatting.querySelectorAll('.menu-separator').forEach(node => node.remove());
    translate(formatting.querySelector('.menu-text'), 'studio.formattingStyle');
    const c = command, s = separator;
    const menus = [
      menu('file', 'menu.file', [c('new', 'studio.newFile'), c('open', 'studio.openFile'), recent, s(), c('save'), c('save-as'), c('save-all'), s(), c('close-tab'), c('exit')]),
      menu('edit', 'menu.edit', [c('undo'), c('redo'), s(), c('cut'), c('copy'), c('paste'), s(), c('find'), c('replace')]),
      menu('view', 'menu.view', [c('show-project', 'studio.project', 'files', 'projectVisible'), c('show-outline', 'studio.outline', 'symbol-method', 'outlineVisible'), s(), c('panel-console', 'studio.console', 'terminal', 'panelConsole'), c('panel-diagnostics', 'studio.diagnostics', 'warning', 'panelDiagnostics'), c('panel-assistant', 'studio.assistant', 'references', 'panelAssistant'), s(), c('toggle-sidebar', 'studio.sidebar'), c('toggle-output'), c('toggle-toolbar'), c('toggle-statusbar'), c('reset-layout', 'studio.resetLayout', 'layout')]),
      menu('project', 'studio.project', [c('create-project', 'create.command', 'new-folder'), c('open-project'), c('close-project'), s(), c('new-project-file'), c('refresh-project', 'studio.refreshProject', 'refresh'), s(), c('choose-project-entry', 'studio.chooseEntry', 'go-to-file'), c('use-current-entry', 'studio.useCurrentEntry', 'run')]),
      menu('run', 'menu.run', [c('check'), c('run', 'studio.runWithoutDebug'), c('stop')]),
      menu('trace', 'studio.debug', [c('trace-start', 'studio.startDebug'), c('trace-restart', 'studio.restartDebug'), c('trace-stop'), s(), c('trace-stepOver'), c('trace-stepInto'), c('trace-stepOut'), c('trace-stepBack'), s(), c('trace-breakpoint'), c('trace-clear')], 'menu-trace'),
      menu('tools', 'studio.toolsMenu', [c('editor-settings', 'studio.settingsMenu', 'settings-gear'), s(), submenu('studio.appearance', ['light', 'dark', 'basic', 'borland'].map(theme => c('theme-' + theme)), 'symbol-color'), language, s(), formatting, c('format'), smart, s(), c('event-log'), c('open-log-folder')]),
      menu('help', 'menu.help', [c('manual'), s(), c('about')]),
    ];
    // Старый селектор нужен существующему setupMenubar и обновлению локали.
    const settings = document.getElementById('menu-settings');
    settings.hidden = true;
    const anchor = document.getElementById('menu-overflow') || settings;
    menus.forEach((node, index) => { node.dataset.studioOrder = String(index); bar.insertBefore(node, anchor); });
    bar.dataset.studioMenus = 'true';
  }
  window.StudioMenus = Object.freeze({build});
})();
