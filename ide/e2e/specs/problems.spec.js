// Live diagnostics (check --stdin) and the Problems panel.
const { testWorkspace, writeAxm } = require('../helpers');

module.exports = [
  {
    id: 'live-check-marks-syntax-error',
    name: 'A syntax error squiggles while typing (live check, no save)',
    async fn({ S, C }) {
      await S.launch({ settings: { checkOnChange: true } });
      await S.editorType('PRINT LEN(\n');
      await S.page.waitForFunction(
        () => window.monaco.editor.getModelMarkers({ owner: 'axiom' }).length > 0,
        null, { timeout: 15000, polling: 200 }
      );
      const markers = await S.markers();
      C.ok(markers.length > 0, 'syntax error produced a marker');
      C.ok(markers.every((m) => m.message.includes('MB')), 'marker carries an MB code', JSON.stringify(markers));
      // Fixing the line clears the marker.
      await S.editorType('PRINT LEN("ab")\n');
      await S.page.waitForFunction(
        () => window.monaco.editor.getModelMarkers({ owner: 'axiom' }).length === 0,
        null, { timeout: 15000, polling: 200 }
      );
      C.ok(true, 'marker cleared after the fix');
    },
  },
  {
    id: 'explicit-check-opens-problems-panel',
    name: 'Check command surfaces the Problems panel with the error list',
    async fn({ S, C }) {
      const ws = testWorkspace('problems-check');
      const file = writeAxm(ws, 'bad.axm', 'DIM x AS INTEGER\nx = "текст"\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('check'));
      await S.page.waitForFunction(
        () => document.querySelectorAll('#diagnostics-content .diagnostic-item').length > 0,
        null, { timeout: 15000, polling: 200 }
      );
      const items = await S.diagnosticItems();
      C.ok(items.some((it) => it.text.includes('MB2003')), 'type mismatch MB2003 listed', JSON.stringify(items));
      const panel = await S.activePanel();
      C.eq(panel, 'diagnostics', 'problems tab is active');
      await S.screenshot('problems-check');
    },
  },
  {
    id: 'check-ok-confirms-no-problems',
    name: 'A clean file checks to "Проблем нет"',
    async fn({ S, C }) {
      const ws = testWorkspace('problems-ok');
      const file = writeAxm(ws, 'ok.axm', 'DIM a AS INTEGER = 2 + 2\nPRINT a\n');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );
      await S.page.evaluate(() => window.__ide.handleAction('check'));
      await S.page.waitForFunction(
        () => document.getElementById('diagnostics-content').innerText.includes('Проблем нет'),
        null, { timeout: 15000, polling: 200 }
      );
      C.ok(true, 'clean check reported');
    },
  },
];
