const fs = require('node:fs');
const path = require('node:path');

function createProjectFile(root, target, text) {
  if (typeof root !== 'string' || typeof target !== 'string' || typeof text !== 'string')
    throw new Error('Неверные параметры создания файла.');
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
module.exports = { createProjectFile };
