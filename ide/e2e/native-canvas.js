const { execFileSync } = require('node:child_process');
const path = require('node:path');
const keys = { ArrowRight:39, ArrowLeft:37, ArrowUp:38, ArrowDown:40, Space:32, Escape:27, Enter:13 };

function probe(hwnd, action, args = {}) {
  const options = ['-NoProfile', '-NonInteractive', '-File', path.join(__dirname, 'native-canvas-probe.ps1'),
    '-WindowHandle', String(hwnd), '-Action', action];
  for (const [name, value] of Object.entries(args)) options.push('-' + name, String(value));
  return JSON.parse(execFileSync('powershell.exe', options, { encoding:'utf8', timeout:15000, maxBuffer:24 * 1024 * 1024 }));
}

async function nativeCanvas(S) {
  await S.page.waitForFunction(() => window.__ide.execution.surface?.kind === 'canvas' &&
    window.__ide.execution.surface.windowVisible === true && BigInt(window.__ide.execution.surface.windowHandle || '0') > 0n);
  const surface = await S.page.evaluate(() => window.__ide.execution.surface);
  return new NativeCanvas(S, surface);
}

class NativeCanvas {
  constructor(S, surface) { this.S = S; this.hwnd = surface.windowHandle; this.width = surface.logicalWidth; this.height = surface.logicalHeight; }
  action(name, args) { return probe(this.hwnd, name, args); }
  async wait(test) {
    for (let i = 0; i < 40; i++) { const value = await test(); if (value) return value; await this.S.page.waitForTimeout(50); }
    throw new Error('Ожидание нативного CANVAS истекло');
  }
  capture(outputPath) { const result = this.action('capture', outputPath ? { OutputPath:outputPath } : {}); return { ...result, data:Buffer.from(result.bgra, 'base64') }; }
  pixel(x, y) {
    const frame = this.capture();
    const bounds = this.bounds(frame);
    const px = bounds.x + Math.floor((x + 0.5) * bounds.width / this.width);
    const py = bounds.y + Math.floor((y + 0.5) * bounds.height / this.height);
    const offset = (py * frame.width + px) * 4;
    return [frame.data[offset + 2], frame.data[offset + 1], frame.data[offset], 255];
  }
  bounds(size = this.action('size')) {
    const scale = Math.min(size.width / this.width, size.height / this.height);
    const width = Math.max(1, Math.floor(this.width * scale)), height = Math.max(1, Math.floor(this.height * scale));
    return { x:Math.floor((size.width - width) / 2), y:Math.floor((size.height - height) / 2), width, height };
  }
  key(action, name) { return this.action(action, { Key:keys[name] ?? name.charCodeAt(0) }); }
  pressKey(name) {
    this.key('down', name);
    if(name === 'Space' || name === 'Enter' || name.length === 1)
      this.action('char', { Key:name.length === 1 ? name.charCodeAt(0) : keys[name] });
    this.key('up', name);
  }
  pointer(x, y, action = 'move', button = 1) {
    const b = this.bounds();
    this.lastX = Math.floor(b.x + b.width * x); this.lastY = Math.floor(b.y + b.height * y);
    return this.action(action, { X:this.lastX, Y:this.lastY, Button:button });
  }
  button(action, button = 1) { return this.action(action, { X:this.lastX || 0, Y:this.lastY || 0, Button:button }); }
  close() { return this.action('close'); }
  exists() { return this.action('exists').exists; }
}

module.exports = { nativeCanvas, probe };
