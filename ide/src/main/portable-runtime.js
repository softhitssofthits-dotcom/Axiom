const fs = require('node:fs');
const path = require('node:path');

function portableRuntime(root = path.resolve(__dirname, '../../..')) {
  const manifestPath = path.join(root, 'portable-runtime.json');
  if (!fs.existsSync(manifestPath)) return null;
  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  if (manifest.version !== 1 || !/^[a-f0-9]{64}$/.test(manifest.sourceFingerprint))
    throw new Error('Повреждён состав переносимой поставки Axiom.');
  return {
    cli: path.join(root, 'runtime/cli/axiom.exe'),
    host: path.join(root, 'runtime/host/Axiom.DesktopHost.exe'),
    sourceFingerprint: manifest.sourceFingerprint,
  };
}
module.exports = { portableRuntime };
