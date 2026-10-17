function graphicsOwnerEnvironment(window, environment = process.env) {
  const result = { ...environment };
  delete result.AXIOM_IDE_OWNER_HWND;
  if (!window || window.isDestroyed() || typeof window.getNativeWindowHandle !== 'function') return result;
  const handle = window.getNativeWindowHandle();
  if (!Buffer.isBuffer(handle) || ![4, 8].includes(handle.length)) return result;
  const value = handle.length === 8 ? handle.readBigUInt64LE() : BigInt(handle.readUInt32LE());
  if (value > 0n) result.AXIOM_IDE_OWNER_HWND = value.toString();
  return result;
}
module.exports = { graphicsOwnerEnvironment };
