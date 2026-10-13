const fs = require('node:fs');
const path = require('node:path');
const { Session, Checks, testWorkspace, writeAxm } = require('./helpers');
const { nativeCanvas } = require('./native-canvas');
const root = path.resolve(__dirname, '../..');
const source = path.join(root, '.artifacts/distribution/Axiom IDE');
const folder = path.join(testWorkspace('portable-ide'), 'Проверка поставки с пробелами');
fs.cpSync(source, folder, { recursive: true });
const executablePath = path.join(folder, 'Axiom IDE.exe');
const S = new Session('portable-acceptance');
const C = new Checks('portable-acceptance');
const resultPath = path.join(root, '.artifacts/portable-acceptance-results.json');
(async () => {
  try {
    await S.launch({ executablePath, portable: true });
    C.eq(await S.electronApp.evaluate(() => process.mainModule.require('node:child_process').spawnSync('dotnet',['--info']).error?.code),'ENOENT','SDK недоступен в среде приложения');
    for (const command of ['git','node']) C.eq(await S.electronApp.evaluate((_e, name) => process.mainModule.require('node:child_process').spawnSync(name,['--version']).error?.code,command),'ENOENT',command+' не требуется для запуска');
    const versions = await S.page.evaluate(() => window.axiom.getVersion());
    C.eq(versions.ide,JSON.parse(fs.readFileSync(path.join(root,'versions.json'),'utf8')).ide,'Версия поставки совпадает с versions.json');
    const dir = testWorkspace('portable-programs');
    const input = writeAxm(dir,'ввод.axm','DIM value AS STRING\nINPUT "Введите слово", value\nPRINT "Принято: "; value\n');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); },input);
    await S.waitStatus('Ожидание ввода');
    await S.page.bringToFront(); await S.page.evaluate(() => window.__ide.term.focus());
    await S.typeConsole('Привет'); await S.waitIdle(); await S.waitConsole('Принято: Привет');
    C.ok(true,'Ввод и выполнение работают без установленной среды');
    const canvas = writeAxm(dir,'холст.axm','CANVAS 40,30\nBEGINFRAME\nCLS RGB(10,20,30)\nPRESENT\nDIM value AS STRING\nINPUT value\n');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); },canvas);
    await S.waitStatus('Ожидание ввода');
    const canvasPage = await nativeCanvas(S);
    C.ok(!!canvasPage,'CANVAS открыт из поставки');
    if (canvasPage) {
      C.eq(canvasPage.width, 40, 'Логический размер нативного CANVAS сохранён');
      await canvasPage.wait(() => canvasPage.pixel(1,1).join(',') === '10,20,30,255');
      C.eq(canvasPage.pixel(1,1).join(','),'10,20,30,255','Показан правильный пиксель CANVAS');
    }
    await S.page.evaluate(() => window.__ide.handleAction('stop')); await S.waitIdle();
    const screen = writeAxm(dir,'экран.axm','SCREEN 10\nCIRCLE (20,20),10,14\nSLEEP\n');
    await S.page.evaluate(async file => { await window.__ide.openFile(file); await window.__ide.run(); },screen);
    await S.page.waitForFunction(() => window.__ide.execution.surface?.windowVisible === true);
    C.ok(true,'Нативный SCREEN работает из поставки');
    await S.page.evaluate(() => window.__ide.handleAction('stop')); await S.waitIdle();
    const debug = writeAxm(dir,'отладка.axm','DIM value AS INTEGER = 5\nPRINT value\n');
    await S.page.evaluate(file => window.__ide.openFile(file),debug); await S.page.keyboard.press('F11');
    await S.page.waitForFunction(() => window.__ide.execution.stopId && !window.__ide.execution.debugBusy);
    C.eq(await S.page.evaluate(() => window.__ide.execution.line),1,'Трассировка поставки останавливается на первой строке');
    await S.page.keyboard.press('F5'); await S.waitIdle(); await S.waitConsole('5');
    C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
    C.ok(fs.existsSync(path.join(folder,'resources/app/docs/manual/index.html')),'Справочник включён');
  } catch(error) { C.failures.push({msg:error.message,detail:error.stack}); }
  finally {
    await S.close();
    fs.writeFileSync(resultPath,JSON.stringify({passed:C.passed,failures:C.failures,pageErrors:S.pageErrors},null,2));
  }
  console.log(JSON.stringify({passed:C.passed,failures:C.failures,resultPath},null,2));
  process.exitCode = C.failures.length ? 1 : 0;
})().catch(error => { console.error(error); process.exitCode = 1; });
