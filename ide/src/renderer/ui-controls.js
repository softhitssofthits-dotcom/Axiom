// Общие свойства элементов управления; состояние хранится у каждого элемента.
(function(root) {
  function setPressed(element, pressed) {
    element?.setAttribute('aria-pressed', String(!!pressed));
  }

  function setExpanded(element, expanded) {
    element?.setAttribute('aria-expanded', String(!!expanded));
  }

  function setEnabled(element, enabled) {
    if (!element) return;
    element.classList.toggle('disabled', !enabled);
    element.setAttribute('aria-disabled', String(!enabled));
    if ('disabled' in element) element.disabled = !enabled;
  }

  function setLabel(element, label, { tooltip = true } = {}) {
    if (!element) return;
    element.setAttribute('aria-label', label);
    if (tooltip) element.title = label;
  }

  function setMenuChecked(check, checked) {
    if (!check) return;
    check.classList.toggle('on', !!checked);
    check.parentElement?.setAttribute('aria-checked', String(!!checked));
    check.parentElement?.setAttribute('role', 'menuitemcheckbox');
  }

  function createIconButton({ id, label, markup, onClick, translationKey }, doc = root.document) {
    const button = doc.createElement('button');
    button.type = 'button';
    button.id = id;
    button.className = 'toolbar-btn';
    button.innerHTML = markup;
    button.onclick = onClick;
    if (translationKey) button.dataset.i18nTitle = translationKey;
    setLabel(button, label);
    return button;
  }

  const contextMenus = new WeakMap();
  function createContextMenu(menu, {translate, onAction, onClose}, doc = root.document) {
    const close = (restoreFocus = true) => {
      if (menu.hidden) return;
      menu.hidden = true; menu.replaceChildren();
      if (restoreFocus) onClose?.();
    };
    const show = (items, x, y) => {
      doc.querySelectorAll('.xs-context-menu').forEach(other => {
        if (other !== menu) contextMenus.get(other)?.close(false);
      });
      menu.replaceChildren();
      for (const item of items) {
        if (item.separator) {
          const separator = doc.createElement('div');
          separator.className = 'xs-context-separator'; separator.setAttribute('role', 'separator');
          menu.appendChild(separator); continue;
        }
        const row = doc.createElement('button');
        row.type = 'button'; row.tabIndex = -1; row.className = 'xs-context-item';
        row.dataset.action = item.id; row.dataset.translation = item.translation;
        row.setAttribute('role', 'menuitem');
        const label = translate(item.translation);
        setLabel(row, label, {tooltip: false}); setEnabled(row, item.enabled !== false);
        const icon = doc.createElement('span');
        icon.className = 'xs-context-item-icon codicon ' + item.icon;
        icon.setAttribute('aria-hidden', 'true'); row.appendChild(icon);
        const text = doc.createElement('span');
        text.className = 'xs-context-item-label'; text.dataset.i18n = item.translation;
        text.textContent = label; row.appendChild(text);
        if (item.key) {
          const key = doc.createElement('span'); key.className = 'xs-context-item-key';
          key.textContent = item.key; row.appendChild(key);
        }
        menu.appendChild(row);
      }
      menu.hidden = false;
      const rect = menu.getBoundingClientRect(), view = doc.defaultView;
      menu.style.left = Math.max(4, Math.min(x, view.innerWidth - rect.width - 4)) + 'px';
      menu.style.top = Math.max(4, Math.min(y, view.innerHeight - rect.height - 4)) + 'px';
      menu.querySelectorAll('button:not(:disabled)')[0]?.focus();
    };
    menu.addEventListener('click', event => {
      const item = event.target.closest?.('[data-action]');
      if (!item || !menu.contains(item) || item.disabled) return;
      const id = item.dataset.action;
      close();
      Promise.resolve(onAction(id)).catch(error => console.error('context menu:', error));
    });
    doc.addEventListener('mousedown', event => {
      if (!menu.hidden && !menu.contains(event.target)) close(false);
    }, true);
    doc.addEventListener('keydown', event => {
      if (menu.hidden) return;
      if (event.key === 'Escape' || event.key === 'Tab') {
        event.preventDefault(); event.stopPropagation(); close(); return;
      }
      if (!['ArrowDown', 'ArrowUp', 'Home', 'End'].includes(event.key)) return;
      event.preventDefault(); event.stopPropagation();
      const rows = [...menu.querySelectorAll('button:not(:disabled)')];
      if (!rows.length) return;
      const current = rows.indexOf(doc.activeElement);
      const next = event.key === 'Home' ? 0 : event.key === 'End' ? rows.length - 1
        : (current + (event.key === 'ArrowDown' ? 1 : -1) + rows.length) % rows.length;
      rows[next].focus();
    }, true);
    const api = {show, close}; contextMenus.set(menu, api); return api;
  }

  const api = Object.freeze({ setPressed, setExpanded, setEnabled, setLabel, setMenuChecked, createIconButton, createContextMenu });
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.UIControls = api;
})(typeof window === 'undefined' ? globalThis : window);
