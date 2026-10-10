const path = require('node:path');

function isolatedEnvironment(parent, profile, axiomExe) {
  const root = path.resolve(profile);
  return {
    ...parent,
    APPDATA: path.join(root, 'roaming'),
    LOCALAPPDATA: path.join(root, 'local'),
    AXIOM_IDE_USER_DATA_DIR: root,
    AXIOM_EXE: axiomExe,
  };
}

function workspacePath(root, tag) {
  if (typeof tag !== 'string' || !/^[a-z0-9_-]+$/i.test(tag)) {
    throw new Error('Invalid test workspace tag');
  }
  const base = path.resolve(root);
  const target = path.resolve(base, tag);
  if (!target.startsWith(base + path.sep)) throw new Error('Invalid test workspace path');
  return target;
}

module.exports = { isolatedEnvironment, workspacePath };
