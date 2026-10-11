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

  const api = Object.freeze({ setPressed, setExpanded, setEnabled, setLabel, setMenuChecked, createIconButton });
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.UIControls = api;
})(typeof window === 'undefined' ? globalThis : window);
