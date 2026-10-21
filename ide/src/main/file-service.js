const fs = require('node:fs');
const path = require('node:path');
const { createHash } = require('node:crypto');

function validateProjectSourceName(target) {
  if(/^\d+\.axm$/i.test(path.basename(target)))throw new Error('Имя исходника проекта не может состоять только из цифр. Для IMPORT используйте имя модуля, например arithmetic.axm или sum2.axm.');
}
function createProjectFile(root, target, text) {
  if (typeof root !== 'string' || typeof target !== 'string' || typeof text !== 'string')
    throw new Error('Неверные параметры создания файла.');
  validateProjectSourceName(target);
  const project = fs.realpathSync(root);
  const parent = fs.realpathSync(path.dirname(path.resolve(target)));
  const relative = path.relative(project, parent);
  if (relative === '..' || relative.startsWith('..' + path.sep) || path.isAbsolute(relative))
    throw new Error('Файл должен находиться в папке проекта.');
  const destination = path.join(parent, path.basename(target));
  try { fs.writeFileSync(destination, text, { encoding: 'utf8', flag: 'wx' }); }
  catch (error) {
    if (error.code === 'EEXIST') throw new Error('Файл уже существует: ' + destination);
    throw error;
  }
  return destination;
}
// Monaco нормализует окончания строк при открытии, сохраняя сами строки программы.
function sameSourceText(saved, snapshot) {
  return typeof saved === 'string' && typeof snapshot === 'string' &&
    saved.replace(/^\uFEFF/, '').replace(/\r\n?/g, '\n') === snapshot.replace(/^\uFEFF/, '').replace(/\r\n?/g, '\n');
}
async function writeDocumentFile(target, text, expectedText, confirmOverwrite, attempt) {
  let exact = false;
  let expectedHash = expectedText === undefined ? undefined : expectedText === null ? null :
    createHash('sha256').update(expectedText.replace(/^\uFEFF/, '').replace(/\r\n?/g, '\n')).digest('hex');
  for (;;) {
    const result = await attempt({ path: target, text, expectedHash, exact });
    if (result.saved) return true;
    if (!result.conflict) throw new Error('Не удалось сохранить файл.');
    if (!await confirmOverwrite(result.missing)) return null;
    expectedHash = result.currentHash;
    exact = true;
  }
}
module.exports = { validateProjectSourceName, createProjectFile, sameSourceText, writeDocumentFile };
