// Собирает ESM Monaco и отдельный worker для локальной работы без AMD.
const path = require('node:path');
const { build } = require('esbuild');
const root = path.resolve(__dirname, '..');
build({
  absWorkingDir: root,
  entryPoints: {
    monaco: 'src/renderer/monaco-entry.js',
    'editor.worker': 'node_modules/monaco-editor/esm/vs/editor/editor.worker.js',
  },
  outdir: 'src/renderer/monaco',
  bundle: true,
  format: 'esm',
  platform: 'browser',
  target: 'chrome154',
  minify: true,
  metafile: true,
  plugins: [{
    name: 'patched-dompurify',
    setup(builder) {
      // Monaco поставляет собственную копию: заменяем её исправленным пакетом.
      builder.onResolve({ filter: /[/\\]dompurify[/\\]dompurify\.js$/ }, () => ({
        path: require.resolve('dompurify', { paths: [path.join(root, 'node_modules/monaco-editor')] }),
      }));
    },
  }],
  loader: { '.ttf': 'file' },
  assetNames: '[name]-[hash]',
  logLevel: 'info',
}).then(result => {
  const inputs = Object.keys(result.metafile.inputs);
  if (!inputs.some(file => file.includes('node_modules/dompurify/dist/')) ||
      inputs.some(file => file.includes('/vs/base/browser/dompurify/'))) {
    throw new Error('Сборка должна использовать исправленный пакет DOMPurify вместо копии Monaco.');
  }
}).catch(error => { console.error(error.message); process.exitCode = 1; });
