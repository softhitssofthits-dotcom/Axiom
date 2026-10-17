const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { SettingsStore } = require('../src/main/settings-store');

function workspace(t) {
  const base = path.resolve(__dirname, '../../.artifacts/settings-store');
  fs.mkdirSync(base, { recursive: true });
  const root = fs.mkdtempSync(path.join(base, 'case-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  return { root, current: path.join(root, 'electron'), legacy: path.join(root, 'recent-files.json') };
}
test('Прежний профиль импортируется один раз без изменения исходника', t => {
  const w = workspace(t), original = JSON.stringify({ RecentFiles: ['C:\\Программы\\один.axm', 'C:\\Программы\\два.axm'],
    LastDirectoryPath: 'C:\\Программы', Theme: 'Light', Language: 'English', IsFilesPaneVisible: false,
    IsStatusBarVisible: false, SmartIdentifierEnabled: true, WindowState: 'Maximized' });
  fs.writeFileSync(w.legacy, original);
  const store = new SettingsStore(w.current, { legacyPath: w.legacy });
  const settings = store.load();
  assert.deepEqual(settings.recentFiles, ['C:\\Программы\\один.axm', 'C:\\Программы\\два.axm']);
  assert.equal(settings.lastOpenDir, 'C:\\Программы');
  assert.equal(settings.theme, 'light'); assert.equal(settings.language, 'en');
  assert.equal(settings.projectExplorer, false); assert.equal(settings.statusBarVisible, false);
  assert.equal(settings.smartIdentifierEnabled, true);
  assert.equal(JSON.parse(fs.readFileSync(path.join(w.current, 'window-state.json'))).maximized, true);
  assert.equal(fs.readFileSync(w.legacy, 'utf8'), original);
  settings.theme = 'borland'; assert.equal(store.save(settings), true);
  fs.writeFileSync(w.legacy, JSON.stringify({ Theme: 'Dark', RecentFiles: ['different.axm'] }));
  const reopened = new SettingsStore(w.current, { legacyPath: w.legacy }).load();
  assert.equal(reopened.theme, 'borland'); assert.deepEqual(reopened.recentFiles, settings.recentFiles);
});
test('Действующий профиль имеет приоритет и сохраняет неизвестные поля', t => {
  const w = workspace(t); fs.mkdirSync(w.current);
  fs.writeFileSync(path.join(w.current, 'settings.json'), JSON.stringify({ theme: 'basic', custom: { value: 42 }, recentFiles: ['x.axm'] }));
  fs.writeFileSync(w.legacy, JSON.stringify({ Theme: 'Light' }));
  const store = new SettingsStore(w.current, { legacyPath: w.legacy }), settings = store.load();
  assert.equal(settings.theme, 'basic'); assert.deepEqual(settings.custom, { value: 42 });
  assert.equal(store.save(settings), true);
  assert.deepEqual(JSON.parse(fs.readFileSync(path.join(w.current, 'settings.json'))).custom, { value: 42 });
});
for (const broken of ['{bad json', 'null', '[]', '42']) {
  test('Повреждённый профиль сохранён до перезаписи: ' + broken, t => {
    const w = workspace(t); fs.mkdirSync(w.current);
    const file = path.join(w.current, 'settings.json'); fs.writeFileSync(file, broken);
    const store = new SettingsStore(w.current, { legacyPath: w.legacy });
    const settings = store.load(); assert.equal(settings.theme, 'dark');
    assert.equal(store.save({ ...settings, theme: 'light' }), true);
    const backup = fs.readdirSync(w.current).find(name => name.startsWith('settings.invalid-'));
    assert.ok(backup); assert.equal(fs.readFileSync(path.join(w.current, backup), 'utf8'), broken);
    assert.equal(JSON.parse(fs.readFileSync(file)).theme, 'light');
  });
}
test('Невозможность сохранить повреждённый оригинал запрещает его перезапись', t => {
  const w = workspace(t); fs.mkdirSync(w.current);
  const file = path.join(w.current, 'settings.json'); fs.writeFileSync(file, '{broken');
  const store = new SettingsStore(w.current, { legacyPath: w.legacy,
    fs: { ...fs, copyFileSync() { throw new Error('Нет записи'); } } });
  const settings = store.load(); assert.equal(store.save(settings), false);
  assert.equal(fs.readFileSync(file, 'utf8'), '{broken');
});
test('Неверные коллекции очищаются без потери пригодных полей и исходного файла', t => {
  const w = workspace(t); fs.mkdirSync(w.current);
  const original = JSON.stringify({ recentFiles: [null, '', 12, 'a.axm'], openTabs: [null, {}, { path: 'a.axm' }], theme: 'basic', lastOpenDir: 13 });
  fs.writeFileSync(path.join(w.current, 'settings.json'), original);
  const settings = new SettingsStore(w.current, { legacyPath: w.legacy }).load();
  assert.deepEqual(settings.recentFiles, ['a.axm']); assert.deepEqual(settings.openTabs, [{ path: 'a.axm' }]);
  assert.equal(settings.lastOpenDir, undefined); assert.equal(settings.theme, 'basic');
  assert.equal(fs.readFileSync(path.join(w.current, 'settings.json'), 'utf8'), original);
});
test('Отсутствующий или повреждённый прежний профиль не мешает первому запуску', t => {
  const w = workspace(t);
  assert.equal(new SettingsStore(w.current, { legacyPath: w.legacy }).load().theme, 'dark');
  fs.writeFileSync(w.legacy, '{broken');
  const store = new SettingsStore(w.current, { legacyPath: w.legacy });
  assert.equal(store.load().language, 'ru'); assert.equal(store.save(store.load()), true);
  assert.equal(fs.readFileSync(w.legacy, 'utf8'), '{broken');
});
test('Неизвестные значения прежних перечислений не становятся настройками', t => {
  const w = workspace(t);
  fs.writeFileSync(w.legacy, JSON.stringify({ Theme: '__proto__', Language: 'constructor', WindowState: 'toString' }));
  const settings = new SettingsStore(w.current, { legacyPath: w.legacy }).load();
  assert.equal(settings.theme, 'dark'); assert.equal(settings.language, 'ru');
  assert.equal(fs.existsSync(path.join(w.current, 'window-state.json')), false);
});
test('Импорт не заменяет существующее состояние окна', t => {
  const w = workspace(t); fs.mkdirSync(w.current);
  const file = path.join(w.current, 'window-state.json'), state = '{"maximized":false,"width":900}';
  fs.writeFileSync(file, state); fs.writeFileSync(w.legacy, '{"WindowState":"Maximized"}');
  new SettingsStore(w.current, { legacyPath: w.legacy }).load();
  assert.equal(fs.readFileSync(file, 'utf8'), state);
});
