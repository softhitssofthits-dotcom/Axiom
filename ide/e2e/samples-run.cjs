// Запуск каждого файла через Run настоящей оболочки Electron в отдельной рабочей копии.
const fs = require('node:fs');
const path = require('node:path');
const { Session, ROOT } = require('./helpers');
const { nativeCanvas } = require('./native-canvas');
const output = path.join(ROOT, '.artifacts', 'samples-electron');
const sandbox = path.join(output, 'workspace');
fs.mkdirSync(sandbox, { recursive: true });
fs.cpSync(path.join(ROOT, 'samples'), path.join(sandbox, 'samples'), { recursive: true });
for (const name of ['README.md', 'PROJECT.md']) fs.copyFileSync(path.join(ROOT, name), path.join(sandbox, name));
fs.mkdirSync(path.join(sandbox, 'docs'), { recursive: true });
fs.copyFileSync(path.join(ROOT, 'docs', 'PLANS4.md'), path.join(sandbox, 'docs', 'PLANS4.md'));
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => e.isDirectory()
    ? files(path.join(dir, e.name)) : e.name.endsWith('.axm') ? [path.join(dir, e.name)] : []);
}
async function main() {
  const results = [];
  const session = new Session('samples-run');
  const retry = process.argv[2] === '@pending';
  const previous = retry ? JSON.parse(fs.readFileSync(path.join(output, 'runs.json'),'utf8')) : [];
  const retryPaths = new Set(previous.filter(r => {
    const source = fs.readFileSync(path.join(ROOT,r.path),'utf8');
    const negative = r.path.includes('/negative/') || path.basename(r.path).includes('error') || source.includes('Ожидаемая диагностика');
    return r.pending || r.error || !r.finished || (!negative && r.finished.status !== 'Succeeded');
  }).map(r=>r.path));
  const selected = files(path.join(sandbox, 'samples')).sort().filter(p => {
    const relative = path.relative(sandbox,p).replaceAll('\\','/');
    return retry ? retryPaths.has(relative) : !process.argv[2] || p.replaceAll('\\', '/').includes(process.argv[2]);
  });
  const timeout = Number(process.argv[3]) || 12000;
  const report = process.argv[2] ? 'runs-' + process.argv[2].replace(/[^A-Za-z0-9_-]/g, '_') + '.json' : 'runs.json';
  await session.launch();
  try {
    await session.page.evaluate(() => {
      window.__sampleEvents = [];
      window.axiom.onBackendEvent(event => window.__sampleEvents.push(event));
    });
    for (const file of selected) {
      const source = fs.readFileSync(file, 'utf8');
      const result = { path: path.relative(sandbox, file).replaceAll('\\', '/'), checkedAt: new Date().toISOString(), inputs: [], keys: [] };
      // Файлы с внешними абсолютными путями требуют отдельного подготовленного сценария.
      if (/"[A-Za-z]:\\/.test(source)) {
        result.pending = 'Требуется подготовить файловый сценарий с абсолютным путём';
      } else try {
        fs.writeFileSync(path.join(output, 'active.json'), JSON.stringify({ path: result.path, started: new Date().toISOString() }));
        await session.page.evaluate(async p => {
          const ide = window.__ide;
          for (const previous of [...ide.openFiles.keys()]) await ide.closeFile(previous, true);
          await ide.openFile(p);
          window.__sampleEvents = [];
          await ide.run();
        }, file);
        const started = Date.now();
        const sampleTimeout = /input_record_intake|maze_chase_mixed_frame|print_locate_color/.test(file) ? Math.max(timeout, 60000) : timeout;
        let inputCount = 0, keyIndex = 0, lastKeyAt = 0;
        const inputLines = [...source.matchAll(/^\s*INPUT\s+([^\r\n]*)/gmi)].map(m => m[1].replace(/"(?:""|[^"])*"/g,'').split("'")[0]
          .split(',').map(v=>v.replace(/^\s*;\s*/,'').trim()).filter(Boolean).length);
        const inputValues = new Array(Math.max(1,...inputLines)).fill('1').join(',');
        while (Date.now() - started < sampleTimeout) {
          const state = await session.page.evaluate(() => ({ running: window.__ide.running,
            request: window.__ide.execution.inputRequestId, session: window.__ide.execution.sessionId,
            events: window.__sampleEvents }));
          if (!state.running) break;
          if (state.request) {
            const value = /invalid_numeric_input/.test(file) ? 'не число' : inputValues;
            await session.page.evaluate(async value => await window.__ide.execution.input(value + '\r'), value);
            result.inputs.push(value); inputCount++;
          }
          const canvas = await session.page.evaluate(() => window.__ide.execution.surface?.kind === 'canvas' && window.__ide.execution.surface.windowVisible);
          if (process.argv[4] !== 'manual' && Date.now() - started > 800 && Date.now() - lastKeyAt > 650 && !state.request) {
            const sequence = /canonical_aliases/.test(file) ? ['Control+PageUp'] : /held_modifiers|modified_keypress/.test(file) ? ['Control+ArrowLeft'] :
              /chord_handler/.test(file) ? ['Control+ArrowLeft','Escape'] : ['ArrowLeft','ArrowLeft','Space','Escape'];
            const key = sequence[Math.min(keyIndex++, sequence.length-1)];
            if (canvas && !key.includes('+')) (await nativeCanvas(session)).pressKey(key);
            else { await session.page.locator('.xterm-helper-textarea').focus(); await session.page.keyboard.press(key,{delay:100}); }
            result.keys.push(key); lastKeyAt = Date.now();
          }
          await session.page.waitForTimeout(100);
        }
        if (await session.page.evaluate(() => window.__ide.running)) {
          result.pending = `Программа не завершилась за ${sampleTimeout / 1000} секунд; требуется индивидуальный сценарий взаимодействия`;
          await session.page.evaluate(() => window.__ide.stop());
          await session.waitIdle();
        }
        result.events = await session.page.evaluate(() => window.__sampleEvents);
        result.finished = [...result.events].reverse().find(e => e.event === 'execution.finished')?.payload;
        const nativeSurfaces = result.events.filter(e=>e.event==='graphics.surface' && e.payload.kind==='screen' && e.payload.windowHandle!=='0');
        if (nativeSurfaces.length) result.screenVisible = nativeSurfaces.some(e=>e.payload.windowVisible===true);
        const canvasVisible = await session.page.evaluate(() => window.__ide.execution.surface?.kind === 'canvas' && window.__ide.execution.surface.windowVisible);
        if (canvasVisible) {
          const canvasPage = await nativeCanvas(session);
          const shots = path.join(output,'shots'); fs.mkdirSync(shots,{recursive:true});
          const shot = result.path.replace(/[^A-Za-z0-9_-]/g,'_') + '.png';
          const captured = canvasPage.capture(path.join(shots,shot));
          result.canvasScreenshot = 'shots/' + shot;
          result.canvas = { width:canvasPage.width, height:canvasPage.height,
            nativeWidth:captured.width, nativeHeight:captured.height, windowHandle:canvasPage.hwnd };
        }
        result.markers = await session.page.evaluate(() => {
          const ide = window.__ide;
          return ide.monaco.editor.getModelMarkers({ owner: 'axiom', resource: ide.editor.getModel().uri })
            .map(m => ({ line: m.startLineNumber, col: m.startColumn, message: m.message, severity: m.severity }));
        });
        result.console = await session.consoleText();
      } catch (error) { result.error = String(error.stack || error); }
      results.push(result);
      fs.writeFileSync(path.join(output, report), JSON.stringify(results, null, 2), 'utf8');
      console.log(`${results.length}/${selected.length} ${result.path}: ${result.finished?.status || result.pending || result.error}`);
    }
  } finally { await session.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
