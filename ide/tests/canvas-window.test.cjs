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
  const context = { document: { getElementById: () => ({}) },
    window: { canvasHost: { onFrame() {}, key: data => events.push({ ...data }) },
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
