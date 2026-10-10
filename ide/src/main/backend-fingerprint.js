const fs = require('node:fs');
const path = require('node:path');
const { createHash } = require('node:crypto');
const { spawnSync } = require('node:child_process');

function sourceFingerprint(root) {
  const files = [];
  function visit(folder) {
    for (const item of fs.readdirSync(folder, { withFileTypes: true })) {
      if (['bin', 'obj', '.artifacts', 'node_modules'].includes(item.name)) continue;
      const absolute = path.join(folder, item.name);
      if (item.isDirectory()) visit(absolute);
      else if (/\.(cs|csproj|props|targets)$/.test(item.name)) files.push(absolute);
    }
  }
  visit(path.join(root, 'src'));
  for (const name of ['Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json', 'NuGet.Config',
    'ide/src/main/backend-fingerprint.js']) {
    const file = path.join(root, name); if (fs.existsSync(file)) files.push(file);
  }
  const hash = createHash('sha256');
  files.sort((a, b) => a < b ? -1 : a > b ? 1 : 0);
  for (const file of files) {
    hash.update(path.relative(root, file).replace(/\\/g, '/') + '\0');
    const bytes = fs.readFileSync(file); hash.update(String(bytes.length) + '\0'); hash.update(bytes);
  }
  return hash.digest('hex');
}
function prepareStamp(root, output) {
  const revision = spawnSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8', windowsHide: true }).stdout?.trim() || null;
  const json = JSON.stringify({ sourceRevision: revision, sourceFingerprint: sourceFingerprint(root), protocolVersion: 1 });
  fs.mkdirSync(path.dirname(output), { recursive: true });
  if (!fs.existsSync(output) || fs.readFileSync(output, 'utf8') !== json) fs.writeFileSync(output, json, 'utf8');
}
if (require.main === module) prepareStamp(path.resolve(process.argv[2]), path.resolve(process.argv[3]));
module.exports = { sourceFingerprint, prepareStamp };
