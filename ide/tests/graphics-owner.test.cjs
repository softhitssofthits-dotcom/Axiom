const { test } = require('node:test');
const assert = require('node:assert/strict');
const { graphicsOwnerEnvironment } = require('../src/main/graphics-owner');

test('Окно IDE передаёт HWND без потери точности и изменения среды родителя', () => {
  for (const size of [4, 8]) {
    const handle = Buffer.alloc(size);
    const value = size === 8 ? 9007199254740993n : 2147483649n;
    if (size === 8) handle.writeBigUInt64LE(value); else handle.writeUInt32LE(Number(value));
    const original = { PATH: 'test', AXIOM_IDE_OWNER_HWND: 'stale' };
    const result = graphicsOwnerEnvironment({ isDestroyed: () => false, getNativeWindowHandle: () => handle }, original);
    assert.equal(result.AXIOM_IDE_OWNER_HWND, String(value));
    assert.equal(result.PATH, 'test');
    assert.equal(original.AXIOM_IDE_OWNER_HWND, 'stale');
  }
});
test('Отсутствующий или закрытый владелец не наследует устаревший HWND', () => {
  for (const window of [null, { isDestroyed: () => true }, { isDestroyed: () => false }])
    assert.equal(graphicsOwnerEnvironment(window, { AXIOM_IDE_OWNER_HWND: '123' }).AXIOM_IDE_OWNER_HWND, undefined);
});
