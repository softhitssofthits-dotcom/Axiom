const { test } = require('node:test');
const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

test('Закрытая сессия CANVAS не открывается поздним кадром, новый запуск открывается', () => {
  const windows = [], acknowledgements = [], stops = [], ipc = new EventEmitter();
  class Window extends EventEmitter {
    constructor() {
      super(); windows.push(this); this.webContents = new EventEmitter();
      this.webContents.setWindowOpenHandler = () => {}; this.webContents.send = () => {};
    }
    setMenu() {} loadURL() {} show() {} isDestroyed() { return false; }
    destroy() { this.emit('closed'); }
  }
  const filename = path.resolve(__dirname, '../src/main/canvas-window.js');
  const context = { require: name => name === 'electron' ? { BrowserWindow: Window, ipcMain: ipc } : require(name),
    __dirname: path.dirname(filename), module: { exports: {} } };
  vm.runInNewContext(fs.readFileSync(filename, 'utf8'), context);
  const canvas = new context.module.exports.CanvasWindow({
    acknowledge: async (id, frame) => acknowledgements.push([id, frame]),
    stop: async id => stops.push(id), input: async () => {} });
  const frame = (sessionId, frameId) => ({ header: { sessionId, frameId }, pixels: [] });
  canvas.frame(frame('old', '1')); windows[0].emit('closed');
  canvas.frame(frame('old', '2')); canvas.frame(frame('old', '3'));
  assert.equal(windows.length, 1); assert.deepEqual(stops, ['old']);
  assert.deepEqual(acknowledgements, [['old', '1'], ['old', '2'], ['old', '3']]);
  canvas.reset(); canvas.frame(frame('new', '1')); assert.equal(windows.length, 2);
});

test('CANVAS передаёт стрелки, текст раскладки и сбрасывает состояние при потере фокуса', () => {
  const handlers = {}, events = [];
  const context = { document: { getElementById: () => ({ addEventListener() {} }) },
    window: { canvasHost: { onFrame() {}, mouse() {}, key: data => events.push({ ...data }) },
      addEventListener: (name, fn) => { handlers[name] = fn; } } };
  vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../src/renderer/canvas.js'), 'utf8'), context);
  const send = (action, code, key) => handlers[action]({ code, key, preventDefault() {} });
  for (const direction of ['Left', 'Right', 'Up', 'Down']) {
    send('keydown', 'Arrow'+direction, 'Arrow'+direction); send('keyup', 'Arrow'+direction, 'Arrow'+direction);
    assert.equal(events.at(-2).key, direction.toUpperCase()); assert.equal(events.at(-1).action, 'up');
  }
  send('keydown', 'KeyK', 'л'); send('keyup', 'KeyK', 'k');
  assert.deepEqual(events.at(-2), { action: 'down', key: 'K', text: 'л' });
  assert.deepEqual(events.at(-1), { action: 'up', key: 'K' });
  send('keydown', 'Space', ' '); assert.equal(events.at(-1).text, 'SPACE');
  send('keydown', 'Escape', 'Escape'); assert.equal(events.at(-1).text, 'ESC');
  handlers.blur(); assert.deepEqual(events.at(-1), { action: 'clear' });
});

test('CANVAS пересчитывает координаты мыши, различает кнопки и передаёт колесо', () => {
  const handlers = {}, events = [];
  const canvas = { width:200, height:100, addEventListener: (name, fn) => { handlers[name] = fn; },
    getBoundingClientRect: () => ({ left:10, top:20, width:400, height:200 }), setPointerCapture() {} };
  const context = { document: { getElementById: () => canvas },
    window: { canvasHost: { onFrame() {}, key() {}, mouse: data => events.push({ ...data }) }, addEventListener() {} } };
  vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../src/renderer/canvas.js'), 'utf8'), context);
  const event = { clientX:130, clientY:100, pointerId:1, preventDefault() {} };
  handlers.pointermove(event);
  assert.deepEqual(events.at(-1), { action:'move', x:60, y:40 });
  for (const [button, expected] of [[0,1],[2,2],[1,3]]) {
    handlers.pointerdown({ ...event, button });
    assert.deepEqual(events.at(-1), { action:'down', button:expected, x:60, y:40 });
    handlers.pointerup({ ...event, button });
    assert.deepEqual(events.at(-1), { action:'up', button:expected, x:60, y:40 });
  }
  handlers.wheel({ ...event, deltaY:120 });
  assert.deepEqual(events.at(-1), { action:'wheel', delta:-120, x:60, y:40 });
  handlers.wheel({ ...event, deltaY:-120 });
  assert.equal(events.at(-1).delta, 120);
  // Пиксельный режим трекпада накапливается до полной ступени Windows.
  handlers.wheel({ ...event, deltaY:60 });
  handlers.wheel({ ...event, deltaY:60 });
  assert.deepEqual(events.at(-1), { action:'wheel', delta:-120, x:60, y:40 });
  handlers.pointercancel();
  assert.deepEqual(events.at(-1), { action:'clear' });
});

test('CANVAS не превращает касание, перо и точки вне изображения в мышь', () => {
  const handlers = {}, events = [];
  const canvas = { width:200, height:100, addEventListener: (name, fn) => { handlers[name] = fn; },
    getBoundingClientRect: () => ({ left:10, top:20, width:400, height:200 }), setPointerCapture() {} };
  const context = { document: { getElementById: () => canvas },
    window: { canvasHost: { onFrame() {}, key() {}, mouse: data => events.push({ ...data }) }, addEventListener() {} } };
  vm.runInNewContext(fs.readFileSync(path.resolve(__dirname, '../src/renderer/canvas.js'), 'utf8'), context);
  const inside = { clientX:130, clientY:100, pointerId:1, button:0, preventDefault() {} };
  const before = events.length;
  handlers.pointerdown({ ...inside, pointerType:'touch' });
  handlers.pointermove({ ...inside, pointerType:'pen' });
  handlers.pointermove({ clientX:5, clientY:100, pointerId:1, preventDefault(){} });
  handlers.pointerdown({ clientX:500, clientY:100, pointerId:1, button:0, preventDefault(){} });
  assert.equal(events.length, before);
});
