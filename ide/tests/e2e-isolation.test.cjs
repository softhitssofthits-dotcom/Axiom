const test = require('node:test');
const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs');
const { isolatedEnvironment, workspacePath } = require('../e2e/isolation.cjs');

test('UI-проверка изолирует оба AppData и явно задаёт профиль Electron', () => {
  const parent = { APPDATA: 'user-roaming', LOCALAPPDATA: 'user-local', PATH: 'tools' };
  const root = path.resolve('.artifacts/ide-shell/test-profile');
  const env = isolatedEnvironment(parent, root, 'own-axiom.exe');
  assert.equal(env.APPDATA, path.join(root, 'roaming'));
  assert.equal(env.LOCALAPPDATA, path.join(root, 'local'));
  assert.equal(env.AXIOM_IDE_USER_DATA_DIR, root);
  assert.equal(env.AXIOM_EXE, 'own-axiom.exe');
  assert.equal(env.PATH, 'tools');
  assert.equal(parent.APPDATA, 'user-roaming');
});

test('Каталог проверки не может выйти за собственный корень', () => {
  const root = path.resolve('.artifacts/ide-shell/e2e/own');
  assert.equal(workspacePath(root, 'run-input'), path.join(root, 'run-input'));
  for (const tag of ['', '..', '../other', 'nested/other']) {
    assert.throws(() => workspacePath(root, tag), /Invalid test workspace/);
  }
});

test('Группа e2e-all содержит каждый файл сценариев без пропусков и повторов', () => {
  const groups = require('../e2e/groups.json');
  const specs = fs.readdirSync(path.join(__dirname, '../e2e/specs')).filter(name => name.endsWith('.spec.js')).sort();
  assert.deepEqual([...groups['e2e-all']].sort(), specs);
  for (const files of Object.values(groups)) {
    assert.equal(new Set(files).size, files.length);
    assert.ok(files.every(file => specs.includes(file)));
  }
});
