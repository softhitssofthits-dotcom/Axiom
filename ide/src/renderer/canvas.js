const canvas = document.getElementById('program-canvas');
const specialKeys = { ArrowLeft: 'LEFT', ArrowRight: 'RIGHT', ArrowUp: 'UP', ArrowDown: 'DOWN',
  Escape: 'ESC', Enter: 'ENTER', Backspace: 'BACKSPACE', Tab: 'TAB', Space: 'SPACE',
  Delete: 'DELETE', Insert: 'INSERT', Home: 'HOME', End: 'END', PageUp: 'PAGEUP', PageDown: 'PAGEDOWN',
  ShiftLeft: 'SHIFT', ShiftRight: 'SHIFT', ControlLeft: 'CTRL', ControlRight: 'CTRL', AltLeft: 'ALT', AltRight: 'ALT' };
const held = new Map();
function stateKey(event) {
  return specialKeys[event.code] || (/^Key[A-Z]$/.test(event.code) ? event.code.slice(3) :
    /^(Digit|Numpad)[0-9]$/.test(event.code) ? event.code.slice(-1) :
    /^F([1-9]|1[0-2])$/.test(event.code) ? event.code : null);
}
window.addEventListener('keydown', event => {
  if (event.metaKey || event.isComposing) return;
  const key = stateKey(event) || (event.key.length === 1 ? event.key.toUpperCase() : null);
  if (!key) return;
  event.preventDefault(); held.set(event.code, key);
  const text = specialKeys[event.code] && !['SHIFT', 'CTRL', 'ALT'].includes(key) ? key :
    event.key.length === 1 ? event.key : '';
  window.canvasHost.key({ action: 'down', key, text });
});
window.addEventListener('keyup', event => {
  const key = held.get(event.code);
  held.delete(event.code);
  if (key && ![...held.values()].includes(key)) window.canvasHost.key({ action: 'up', key });
});
window.addEventListener('blur', () => {
  held.clear(); window.canvasHost.key({ action: 'clear' });
});
window.canvasHost.onFrame(({ header, pixels }) => {
  canvas.width = header.width; canvas.height = header.height;
  const bgra = new Uint8Array(pixels), rgba = new Uint8ClampedArray(bgra.length);
  for (let i = 0; i < bgra.length; i += 4) {
    rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1];
    rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3];
  }
  canvas.getContext('2d').putImageData(new ImageData(rgba, header.width, header.height), 0, 0);
  document.getElementById('canvas-size').textContent = `${header.width} × ${header.height} · CANVAS`;
  requestAnimationFrame(() => window.canvasHost.painted(header.frameId));
});
