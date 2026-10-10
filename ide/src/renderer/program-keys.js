(function(root, factory) {
  if (typeof module === 'object' && module.exports) module.exports = factory();
  else Object.assign(root, factory());
})(typeof window !== 'undefined' ? window : globalThis, function() {
  const special = { ArrowLeft:'LEFT', ArrowRight:'RIGHT', ArrowUp:'UP', ArrowDown:'DOWN',
    Escape:'ESC', Enter:'ENTER', Backspace:'BACKSPACE', Tab:'TAB', Space:'SPACE',
    Delete:'DELETE', Insert:'INSERT', Home:'HOME', End:'END', PageUp:'PAGEUP', PageDown:'PAGEDOWN',
    ShiftLeft:'SHIFT', ShiftRight:'SHIFT', ControlLeft:'CTRL', ControlRight:'CTRL', AltLeft:'ALT', AltRight:'ALT' };
  function programKey(event) {
    if (event.metaKey || event.isComposing) return null;
    const key = special[event.code] || (/^Key[A-Z]$/.test(event.code) ? event.code.slice(3) :
      /^(Digit|Numpad)[0-9]$/.test(event.code) ? event.code.slice(-1) :
      /^F([1-9]|1[0-2])$/.test(event.code) ? event.code : event.key?.length === 1 ? event.key.toUpperCase() : null);
    if (!key) return null;
    const text = special[event.code] && !['SHIFT','CTRL','ALT'].includes(key) ? key : event.key?.length === 1 ? event.key : '';
    return { key, text };
  }
  return { programKey };
});
