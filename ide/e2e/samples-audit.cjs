// Пофайловая проверка настоящей оболочки Electron. Результаты сохраняются после каждого файла.
const fs = require('node:fs');
const path = require('node:path');
const { Session, ROOT } = require('./helpers');
const output = path.join(ROOT, '.artifacts', 'samples-electron');
fs.mkdirSync(output, { recursive: true });
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory()
    ? files(path.join(dir, e.name)) : e.name.endsWith('.axm') ? [path.join(dir, e.name)] : []);
}
async function main() {
  const results = [];
  const session = new Session('samples-audit');
  const selected = files(path.join(ROOT, 'samples')).sort().filter(p => !process.argv[2] || p.replaceAll('\\', '/').includes(process.argv[2]));
  await session.launch();
  try {
    for (const file of selected) {
      const result = { path: path.relative(ROOT, file).replaceAll('\\', '/'), checkedAt: new Date().toISOString() };
      const started = Date.now();
      try {
        await session.page.evaluate(async p => {
          const ide = window.__ide;
          for (const previous of [...ide.openFiles.keys()]) await ide.closeFile(previous, true);
          await ide.openFile(p);
        }, file);
        result.compilation = await session.page.evaluate(async () => {
          const ide = window.__ide;
          await ide.check(true);
          // Этот второй ответ хранит исходные коды и статусы, независимо от языка интерфейса.
          return await ide.documentServices.request(ide.activeFile, 'document.compile');
        });
        result.markers = await session.page.evaluate(() => {
          const ide = window.__ide;
          return ide.monaco.editor.getModelMarkers({ owner: 'axiom', resource: ide.editor.getModel().uri })
            .map(m => ({ line: m.startLineNumber, col: m.startColumn, message: m.message, severity: m.severity }));
        });
        result.problems = await session.diagnosticItems();
        result.sourceOpened = await session.page.evaluate(p => window.__ide.activeFile === p, file);
      } catch (error) { result.error = String(error.stack || error); }
      result.ms = Date.now() - started;
      results.push(result);
      fs.writeFileSync(path.join(output, 'checks.json'), JSON.stringify(results, null, 2), 'utf8');
      console.log(`${results.length}/${selected.length} ${result.path}: ${result.compilation?.status || result.error}`);
    }
  } finally { await session.close(); }
  if (results.some(r => r.error || !r.sourceOpened)) process.exitCode = 1;
}
main().catch(error => { console.error(error); process.exitCode = 1; });
