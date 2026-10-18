const { testWorkspace, writeAxm } = require('../helpers');
async function open(S, tag, source) {
  const file = writeAxm(testWorkspace('regex-' + tag), 'Регулярные выражения.axm', source);
  await S.launch({ axiomExe: process.env.AXIOM_REGEX_TEST_CLI });
  await S.page.evaluate(async file => {
    const ide = window.__ide;
    ide.settings.formatOnBlur = false; ide.settings.checkOnChange = false;
    await ide.openFile(file); clearTimeout(ide._liveCheckTimer);
  }, file);
}
async function step(S, command, line) {
  const before = await S.page.evaluate(() => window.__ide.execution.stopId);
  await S.page.evaluate(command => window.__ide.trace.command(command), command);
  await S.page.waitForFunction(({before, line}) => {
    const ide = window.__ide;
    return ide.execution.stopId && ide.execution.stopId !== before && !ide.execution.debugBusy && ide.editor.getPosition().lineNumber === line;
  }, {before, line});
}
module.exports = [{
  id: 'regex-language-contract',
  name: 'RegEx: все формы, подсветка, подписи, выполнение и конкретные причины ошибок',
  async fn({ S, C }) {
    const source = 'PRINT REGEXREPLACE("A12 B34", "(?<letter>[A-Z])(\\d+)", "${letter}-$1")\n' +
      'PRINT REGEXISMATCH("a1" + CHR(10), "\\Aa\\d+\\z")\n' +
      'TRY\nPRINT REGEXISMATCH("a", "[")\nCATCH e\nPRINT e.Message\nEND TRY\n' +
      'TRY\nPRINT REGEXEXTRACT("a", "a", -1)\nCATCH e\nPRINT e.Message\nEND TRY\n';
    await open(S, 'contract', source);
    await S.page.evaluate(() => window.__ide.check(false));
    C.ok(await S.page.evaluate(() => !window.__ide._lastDiagnostics.some(d => d.severity === 'error')), 'Программа проходит полный компилятор');
    const meta = await S.page.evaluate(() => window.__ide.langMeta.builtins.filter(b => b.name.startsWith('REGEX')));
    C.eq(meta.length, 18, 'В редактор доставлены все 18 форм');
    C.eq(new Set(meta.map(b => b.name)).size, 8, 'В редактор доставлены восемь функций');
    C.ok(await S.page.evaluate(() => window.__ide.monaco.editor.tokenize(window.__ide.editor.getValue(), 'axiom')[0].some(t => t.type === 'support.function.axiom')), 'Функция окрашена в реальном Monaco');
    const signature = await S.page.evaluate(() => {
      const ide = window.__ide;
      return window.LangFeatures._sigProvider.provideSignatureHelp(ide.editor.getModel(), {lineNumber: 1, column: 20});
    });
    C.eq(signature.value.signatures.length, 4, 'Подсказка REGEXREPLACE содержит четыре формы');
    await S.page.evaluate(() => window.__ide.run()); await S.waitIdle();
    await S.waitConsole('A-12 B-34'); await S.waitConsole('Invalid pattern'); await S.waitConsole('cannot be negative');
    C.ok(await S.page.evaluate(() => window.__ide.execution.outputText.includes('FALSE')), 'Строгая граница отвергла завершающий LF');
    C.eq((await S.diagnosticItems()).length, 0, 'Перехваченные ошибки не остались в панели диагностики');
  },
}, {
  id: 'regex-stop-between-matches',
  name: 'RegEx: остановка настоящего длительного перебора из Electron',
  async fn({ S, C }) {
    const text = ('a'.repeat(20) + ';').repeat(2000);
    await open(S, 'stop', 'DIM text AS STRING = "' + text + '"\nPRINT "regex-ready"\nPRINT REGEXCOUNT(text, "(?:a|aa)+b|;")\nPRINT "after-regex"\n');
    await S.page.evaluate(() => window.__ide.run());
    await S.waitConsole('regex-ready');
    C.ok(await S.page.evaluate(() => window.__ide.running), 'Остановка подаётся работающей программе');
    const start = Date.now();
    await S.page.evaluate(() => window.__ide.stop()); await S.waitIdle(4000);
    C.ok(Date.now() - start < 4000, 'Stop завершает перебор до окончания всех совпадений');
    C.ok(await S.page.evaluate(() => !window.__ide.execution.outputText.includes('after-regex')), 'Программа не дошла до инструкции после RegEx');
    C.eq((await S.diagnosticItems()).length, 0, 'Отмена не стала ошибкой MB3007');
  },
}, {
  id: 'regex-debug-array-step-back',
  name: 'RegEx: массивный результат после шага назад и повторного исполнения',
  async fn({ S, C }) {
    await open(S, 'debug', 'DIM values() AS STRING\nvalues = REGEXMATCHES("A12 B34", "\\d+")\nPRINT values(2)\nvalues = REGEXEXTRACTALL("C56", "([A-Z])(\\d+)", 2)\nPRINT values(1)\n');
    await S.page.evaluate(() => window.__ide.run('debug'));
    await S.page.waitForFunction(() => !!window.__ide.execution.stopId);
    C.eq(await S.page.evaluate(() => window.__ide.execution.line), 2, 'Первая исполняемая строка — присваивание массива');
    await step(S, 'stepOver', 3);
    C.ok(await S.page.evaluate(() => window.__ide.execution.canStepBack), 'Результат RegEx допускает обратный шаг');
    await step(S, 'stepBack', 2); await step(S, 'stepOver', 3);
    await S.page.evaluate(() => window.__ide.trace.command('start')); await S.waitIdle();
    await S.waitConsole('34'); await S.waitConsole('56');
    C.eq((await S.diagnosticItems()).length, 0, 'Повторное исполнение массивов прошло без ошибки');
  },
}];
