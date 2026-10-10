const canvas = document.getElementById('program-canvas');
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
