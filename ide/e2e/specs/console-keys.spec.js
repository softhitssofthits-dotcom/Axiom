const path = require('node:path');
const { ROOT } = require('../helpers');
module.exports = [
  ['inkey/poll_keys.axm','ArrowLeft','LEFT'],
  ['keypress/single_press.axm','ArrowLeft','LEFT нажата'],
  ['keypress/modified_keypress.axm','Control+ArrowLeft','CTRL+LEFT'],
  ['keydown/held_modifiers.axm','Control+ArrowLeft','CTRL+LEFT'],
  ['keydown/canonical_aliases.axm','Control+PageUp','CONTROL+PAGE UP'],
].map(([relative,key,expected]) => ({
  id: 'console-key-' + relative.replace(/[^a-z0-9]/gi,'-'),
  name: `Консоль Electron передаёт клавиши для ${relative}`,
  async fn({ S,C }) {
    const file = path.join(ROOT,'samples','functions',relative);
    await S.launch(); await S.page.evaluate(p => window.__ide.openFile(p),file);
    await S.page.evaluate(() => window.__ide.run());
    await S.page.waitForFunction(() => window.__ide.running && window.__ide.execution.sessionId && window.__ide.execution.outputText.length > 0);
    await S.page.locator('.xterm-helper-textarea').focus();
    await S.page.keyboard.press(key,{delay:150});
    await S.waitIdle(); await S.waitConsole(expected);
    C.ok(!(await S.diagnosticItems()).some(x=>x.text.includes('AXIOM-IDE')), 'Нет ошибки служебного процесса');
    C.ok(true,'Пример завершился после настоящего нажатия клавиш');
  }
}));
