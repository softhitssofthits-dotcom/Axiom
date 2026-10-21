const fs = require('node:fs');
const path = require('node:path');

function existingDirectory(value) {
  if (typeof value !== 'string' || !value || value.length > 32767 || value.includes('\0') || !path.isAbsolute(value)) return null;
  try { return fs.statSync(value).isDirectory() ? fs.realpathSync(value) : null; }
  catch { return null; }
}

function projectCreationParent(settings, documents) {
  const project = existingDirectory(settings?.lastProjectDir);
  return (project && path.dirname(settings.lastProjectDir)) || existingDirectory(settings?.lastOpenDir) || documents;
}

function createProject(parent, name) {
  if (typeof name !== 'string' || !name || name.length > 255 || name === '.' || name === '..' ||
      /[\\/\x00-\x1f\x7f<>:"|?*]/.test(name) || /[. ]$/.test(name) ||
      /^(CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)/i.test(name))
    throw new Error('Недопустимое имя проекта. Укажите имя одной папки без служебных символов.');
  const directory = existingDirectory(parent);
  if (!directory) throw new Error('Выберите существующую папку для создания проекта.');
  const target = path.join(directory, name);
  if (path.dirname(target) !== directory) throw new Error('Проект должен находиться в выбранной папке.');
  try { fs.mkdirSync(target); }
  catch (error) {
    if (error.code === 'EEXIST') throw new Error('Папка или файл с таким именем уже существует.');
    throw error;
  }
  const created = fs.lstatSync(target);
  const main = path.join(target, 'main.axm');
  try {
    if (fs.realpathSync(target) !== target) throw new Error('Папка проекта была изменена во время создания.');
    fs.writeFileSync(main, '', { encoding: 'utf8', flag: 'wx' });
  } catch (error) {
    // Удаляется только созданная нами папка, если она осталась пустой.
    try {
      const current = fs.lstatSync(target);
      if (current.isDirectory() && current.dev === created.dev && current.ino === created.ino) fs.rmdirSync(target);
    } catch {}
    throw error;
  }
  return { path: target, main };
}

module.exports = { createProject, projectCreationParent, existingDirectory };
