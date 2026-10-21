// Live diagnostics (check --stdin) and the Problems panel.
const { testWorkspace, writeAxm } = require('../helpers');
const path = require('path');

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
  {
    id: 'unused-variable-warning',
    name: 'Совет о неиспользуемой переменной выделяет имя, отделён от ошибок и обновляется после чтения',
    async fn({ S, C }) {
      const file = path.resolve(__dirname, '..', '..', '..', 'samples', 'operators', 'dim', 'unused_value_warning.axm');
      await S.launch(); await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(p => window.__ide.activeFile === p, file);
      await S.page.evaluate(() => window.__ide.handleAction('check'));
      await S.page.waitForFunction(() => document.getElementById('diagnostics-content').textContent.includes('MB2047'));
      C.ok(await S.page.locator('#diagnostics-content .diagnostic-icon.advice').count() > 0, 'Сообщение показано как совет');
      const markers = await S.page.evaluate(() => window.monaco.editor.getModelMarkers({ owner: 'axiom' }));
      C.ok(markers.some(marker => marker.message.includes('MB2047') && marker.severity === 1 && marker.startColumn === 5 && marker.endColumn === 10), 'Совет указывает только на имя spare и не рисует предупреждение');
      await S.page.locator('[data-diagnostic-filter="error"]').click();
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-item:visible').count(), 0, 'Совет не попадает в список ошибок');
      await S.page.locator('[data-diagnostic-filter="advice"]').click();
      C.eq(await S.page.locator('#diagnostics-content .diagnostic-item:visible').count(), 1, 'Совет доступен отдельно');
      await S.page.setViewportSize({ width: 520, height: 720 });
      C.ok(await S.page.locator('#diagnostic-filters').evaluate(el => el.getBoundingClientRect().right <= innerWidth), 'Категории помещаются в узком окне');
      await S.screenshot('unused-variable-advice');
      await S.page.setViewportSize({ width: 1280, height: 800 });
      await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
      await S.waitConsole('Предупреждение не мешает выполнению программы.');
      C.ok(true, 'предупреждение не блокирует запуск');
      await S.page.evaluate(() => window.__ide.setLanguage('en'));
      await S.page.evaluate(() => window.__ide.handleAction('check'));
      await S.page.waitForFunction(() => document.getElementById('diagnostics-content').textContent.includes('MB2047'));
      C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes('never used')), 'предупреждение переведено на английский');
      await S.page.evaluate(() => window.__ide.editor.getModel().setValue('DIM spare AS INTEGER = 42\nspare = 123\n'));
      C.ok(!(await S.markers()).some(marker => marker.message.includes('MB2047')), 'Устаревший совет снят сразу после правки');
      await S.page.waitForFunction(() => window.__ide._lastDiagnostics?.some(d => d.code === 'MB2047'));
      C.ok(true, 'Присваивание без чтения по-прежнему не использует значение');
      await S.page.evaluate(() => window.__ide.editor.getModel().setValue('DIM spare AS INTEGER = 42\nPRINT spare\n'));
      await S.page.waitForFunction(() => !window.__ide._lastDiagnostics?.some(d => d.code === 'MB2047'));
      await S.page.evaluate(() => window.__ide.runLiveCheck());
      C.ok(!(await S.markers()).some(marker => marker.message.includes('MB2047')), 'Чтение значения устраняет совет после нового анализа');
      await S.page.evaluate(() => window.__ide.editor.getModel().setValue('DIM spare AS INTEGER\n'));
      await S.page.evaluate(() => window.__ide.runLiveCheck());
      await S.page.evaluate(p => window.__ide.closeFile(p, true), file);
      C.ok(await S.page.locator('[data-diagnostic-filter="advice"]').textContent().then(text => text.includes('(0)')), 'Закрытие документа обновляет число советов');
      await S.page.evaluate(() => window.__ide.switchPanel('console'));
      C.ok(!await S.page.locator('#diagnostic-filters').isVisible(), 'Категории сообщений скрыты в консоли');
    },
  },
  {
    id: 'circle-text-mode-check-run-diagnostic',
    name: 'Check находит графику в SCREEN 0 и блокирует Run; сообщение сохраняется при смене языка',
    async fn({ S, C }) {
      const file = path.resolve(__dirname, '..', '..', '..', 'samples', 'operators', 'circle', 'text_screen_error.axm');
      await S.launch();
      await S.stubOpenDialog([file]);
      await S.page.evaluate(() => window.__ide.handleAction('open'));
      await S.page.waitForFunction(
        (p) => window.__ide && window.__ide.activeFile === p && !window.__ide.dirty.has(p),
        file, { timeout: 8000, polling: 100 }
      );

      await S.page.evaluate(() => window.__ide.handleAction('check'));
      await S.page.waitForFunction(
        () => document.getElementById('diagnostics-content').innerText.includes('MB2046'),
        null, { timeout: 15000, polling: 200 }
      );
      C.ok(true, 'Check показал ошибку MB2046 до запуска');

      await S.page.evaluate(() => {
        window.__ide.setLanguage('en');
        window.__ide.run();
      });
      await S.waitIdle();
      await S.page.waitForFunction(
        () => document.getElementById('diagnostics-content').textContent.includes('MB2046'),
        null, { timeout: 8000, polling: 100 }
      );
      C.ok(true, 'Run сохранил ошибку компиляции в английском интерфейсе');
      C.ok(await S.page.evaluate(() => !window.__ide.execution.surface || window.__ide.execution.surface.state === 'closed'), 'запуск не создал активное окно программы');

      await S.page.evaluate(() => window.__ide.setLanguage('ru'));
      C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes('MB2046')),
        'смена языка на русский не скрыла диагностику');
      await S.page.evaluate(() => window.__ide.setLanguage('en'));
      C.ok(await S.page.evaluate(() => document.getElementById('diagnostics-content').textContent.includes('MB2046')),
        'повторная смена языка не скрыла диагностику и не перезапустила программу');
    },
  },
];

module.exports.push({id:'project-advice-cross-module-reads',name:'Советы учитывают чтения из main и несохранённые правки проекта',async fn({S,C}){
 const root=testWorkspace('project-advice');
 const main=writeAxm(root,'main.axm','IMPORT helper\nPRINT SharedValue + SharedLimit\n');
 const helper=writeAxm(root,'helper.axm','PUBLIC DIM SharedValue AS INTEGER = 1\nPUBLIC CONST SharedLimit AS INTEGER = 2\nPRIVATE DIM LocalUnused AS INTEGER = 3\n');
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),root);
 await S.page.evaluate(p=>window.__ide.openFile(p),helper);await S.page.evaluate(()=>window.__ide.runLiveCheck());
 require('node:fs').writeFileSync(path.join(require('../helpers').ROOT,'.artifacts/project-open-20261020/advice-initial.json'),JSON.stringify(await S.page.evaluate(()=>window.__ide._lastDiagnostics),null,2));
 C.eq(await S.page.evaluate(()=>window.__ide._lastDiagnostics.filter(d=>d.severity==='error').length),0,'Пример не содержит ошибок компиляции');
 const advice=()=>S.page.evaluate(()=>window.__ide._lastDiagnostics.filter(d=>d.code==='MB2047').map(d=>d.message).join('\n'));
 C.ok(!(await advice()).includes("'SharedValue'"),'Импортированная переменная читается в main');
 C.ok(!(await advice()).includes("'SharedLimit'"),'Импортированная константа читается в main');
 C.contains(await advice(),'LocalUnused','Действительно неиспользуемая переменная не скрыта');
 await S.page.evaluate(()=>window.__ide.check());
 C.ok(!(await advice()).includes("'SharedValue'"),'Явная проверка также учитывает main');
 await S.page.evaluate(p=>window.__ide.closeFile(p),main);await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.ok(!(await advice()).includes("'SharedValue'"),'Закрытая вкладка main читается с диска');
 await S.page.evaluate(p=>window.__ide.openFile(p),main);await S.page.evaluate(p=>window.__ide.openFile(p),helper);
 await S.page.evaluate(p=>window.__ide.openFiles.get(p).model.setValue('IMPORT helper\nPRINT 0\n'),main);
 await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.contains(await advice(),'SharedValue','Удалённое несохранённое чтение учтено');
 C.contains(await advice(),'SharedLimit','Удалённое чтение константы учтено');
 const orphan=writeAxm(root,'orphan.axm','DIM UnusedOrphan AS INTEGER\n');await S.page.evaluate(p=>window.__ide.openFile(p),orphan);await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.contains(await advice(),'UnusedOrphan','Не включённый в IMPORT файл проверяется самостоятельно');
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}},{id:'diagnostic-peek-survives-live-analysis',name:'Карточка ошибки сохраняется при проверке и закрывается после устранения диагностики',async fn({S,C}){
 const file=writeAxm(testWorkspace('peek-persistent'),'bad.axm','PRINT MissingValue\n');
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openFile(p),file);await S.page.evaluate(()=>window.__ide.runLiveCheck());
 await S.page.locator('.diagnostic-item').first().click();await S.page.locator('.marker-widget').waitFor({state:'visible'});
 await S.page.evaluate(()=>window.__ide.scheduleLiveCheck());await S.sleep(1100);
 C.eq(await S.page.locator('.marker-widget:visible').count(),1,'Фоновая проверка не закрывает карточку');
 await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('PRINT 1\n'));await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.eq(await S.page.locator('.marker-widget:visible').count(),0,'Исправление ошибки автоматически закрывает карточку');
 C.eq(await S.page.locator('.diagnostic-item').count(),0,'Панель и карточка согласованы');
 await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('PRINT FirstMissing\nPRINT OtherMissing\n'));await S.page.evaluate(()=>window.__ide.runLiveCheck());
 await S.page.locator('.diagnostic-item').first().click();await S.page.locator('.marker-widget').waitFor({state:'visible'});
 await S.page.evaluate(()=>window.__ide.editor.getModel().setValue('PRINT 1\nPRINT OtherMissing\n'));await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.eq(await S.page.locator('.marker-widget:visible').count(),0,'Исправленная карточка закрывается даже при другой ошибке');
 await S.page.locator('.diagnostic-item').first().click();await S.page.locator('.marker-widget').waitFor({state:'visible'});
 await S.page.locator('.peekview-widget .action-label.codicon-close').click();
 C.eq(await S.page.locator('.marker-widget:visible').count(),0,'Крестик закрывает карточку');
 await S.page.evaluate(()=>window.__ide.runLiveCheck());
 C.eq(await S.page.locator('.marker-widget:visible').count(),0,'Закрытая карточка не появляется от проверки');
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});

module.exports.push({id:'project-advice-maze-config',name:'Константы Maze Chase учитываются во всех девяти модулях',async fn({S,C}){
 const folder=path.join(require('../helpers').ROOT,'samples/graphics/game/maze_chase_project');
 await S.launch({settings:{smartIdentifierEnabled:false}});await S.page.evaluate(p=>window.__ide.openProject(p),path.join(folder,'maze_chase.axproj'));
 await S.page.evaluate(p=>window.__ide.openFile(p),path.join(folder,'maze_config.axm'));await S.page.evaluate(()=>window.__ide.runLiveCheck());
 const diagnostics=await S.page.evaluate(()=>window.__ide._lastDiagnostics);
 C.eq(diagnostics.filter(d=>d.severity==='error').length,0,'Проект успешно анализируется');
 for(const name of ['TILE','VIEW_WIDTH','VIEW_HEIGHT','HUD_HEIGHT','PLAYER_SPEED','GHOST_SPEED'])
  C.ok(!diagnostics.some(d=>d.code==='MB2047'&&d.message.includes("'"+name+"'")),'Нет ложного совета для '+name);
 C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
}});
