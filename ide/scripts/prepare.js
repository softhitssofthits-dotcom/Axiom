// Copies Monaco's AMD runtime (min/vs) next to index.html so the renderer
// can load it via a relative path under the same file:// origin (CSP 'self').
const fs = require('fs');
const path = require('path');

const src = path.join(__dirname, '..', 'node_modules', 'monaco-editor', 'min', 'vs');
const dest = path.join(__dirname, '..', 'src', 'renderer', 'vs');

if (!fs.existsSync(src)) {
  console.error('monaco-editor is not installed. Run `npm install` first.');
  process.exit(1);
}

function copyDir(s, d) {
  fs.mkdirSync(d, { recursive: true });
  for (const entry of fs.readdirSync(s, { withFileTypes: true })) {
    const sp = path.join(s, entry.name);
    const dp = path.join(d, entry.name);
    if (entry.isDirectory()) copyDir(sp, dp);
    else fs.copyFileSync(sp, dp);
  }
}

copyDir(src, dest);
console.log('Monaco runtime copied to src/renderer/vs');
