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
  window.canvasHost.mouse({ action: 'clear' });
});
// Точка публикуется только внутри видимого изображения: клик в поле
// письма и за пределами холста не становится мировой координатой.
function mousePoint(event) {
  const rect = canvas.getBoundingClientRect();
  if (!(rect.width > 0) || !(rect.height > 0)) return null;
  if (event.clientX < rect.left || event.clientY < rect.top ||
      event.clientX >= rect.right || event.clientY >= rect.bottom) return null;
  const x = Math.floor((event.clientX - rect.left) * canvas.width / rect.width);
  const y = Math.floor((event.clientY - rect.top) * canvas.height / rect.height);
  if (!Number.isSafeInteger(x) || !Number.isSafeInteger(y) ||
      x < 0 || y < 0 || x >= canvas.width || y >= canvas.height) return null;
  return { x, y };
}
const buttons = { 0:1, 2:2, 1:3 };
const pressed = new Set();
let lastPoint = null;
// Колесо передаётся в единицах Windows (ступень 120). Дробный остаток
// сохраняется, поэтому трекпад с deltaY=1 не теряет движение.
let wheelRemainder = 0;
const wheelUnit = 120;
function wheelDelta(event) {
  const scale = event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? canvas.clientHeight : 1;
  wheelRemainder += -event.deltaY * scale;
  const steps = Math.trunc(wheelRemainder / wheelUnit) * wheelUnit;
  wheelRemainder -= steps;
  return steps;
}
function isMousePointer(event) {
  return !event.pointerType || event.pointerType === 'mouse';
}
canvas.addEventListener('pointermove', event => {
  if (!isMousePointer(event)) return;
  const point = mousePoint(event);
  if (!point) return;
  lastPoint = point;
  window.canvasHost.mouse({ action:'move', ...point });
});
canvas.addEventListener('pointerdown', event => {
  if (!isMousePointer(event) || !buttons[event.button]) return;
  const point = mousePoint(event);
  if (!point) return;
  event.preventDefault(); canvas.setPointerCapture(event.pointerId);
  pressed.add(event.button);
  lastPoint = point;
  window.canvasHost.mouse({ action:'down', button:buttons[event.button], ...point });
});
canvas.addEventListener('pointerup', event => {
  if (!isMousePointer(event) || !buttons[event.button]) return;
  // Отпускание доставляется и за пределами изображения, если кнопка была
  // нажата здесь: иначе программа осталась бы с удержанной кнопкой.
  if (!pressed.delete(event.button)) return;
  const point = mousePoint(event) || lastPoint;
  if (!point) { window.canvasHost.mouse({ action:'clear' }); return; }
  lastPoint = point;
  window.canvasHost.mouse({ action:'up', button:buttons[event.button], ...point });
});
canvas.addEventListener('pointercancel', () => {
  pressed.clear();
  lastPoint = null;
  wheelRemainder = 0;
  window.canvasHost.mouse({ action: 'clear' });
});
canvas.addEventListener('contextmenu', event => event.preventDefault());
// Отказ доставки означает, что хост уже не принимает события этой сессии:
// сбрасываем локальное удержание, чтобы окно не зависло в нажатом состоянии.
window.canvasHost.onMouseRejected?.(() => {
  pressed.clear();
  lastPoint = null;
  wheelRemainder = 0;
});
canvas.addEventListener('wheel', event => {
  event.preventDefault();
  const point = mousePoint(event);
  if (!point) return;
  const delta = wheelDelta(event);
  if (delta !== 0) window.canvasHost.mouse({ action:'wheel', delta, ...point });
}, { passive:false });
window.canvasHost.onFrame(({ header, pixels }) => {
  canvas.width = header.width; canvas.height = header.height;
  canvas.style.imageRendering = header.smooth ? 'auto' : 'pixelated';
  const bgra = new Uint8Array(pixels), rgba = new Uint8ClampedArray(bgra.length);
  for (let i = 0; i < bgra.length; i += 4) {
    rgba[i] = bgra[i + 2]; rgba[i + 1] = bgra[i + 1];
    rgba[i + 2] = bgra[i]; rgba[i + 3] = bgra[i + 3];
  }
  canvas.getContext('2d').putImageData(new ImageData(rgba, header.width, header.height), 0, 0);
  document.getElementById('canvas-size').textContent = `${header.width} × ${header.height} · CANVAS`;
  requestAnimationFrame(() => window.canvasHost.painted(header.frameId));
});
