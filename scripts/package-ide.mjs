import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';
import { createRequire } from 'node:module';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(import.meta.url);
const destination = path.join(root, '.artifacts', 'distribution', 'Axiom IDE');
const staging = path.join(root, '.artifacts', 'publish');
function run(command, args) {
  const result = spawnSync(command, args, { cwd: root, stdio: 'inherit', windowsHide: true });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`Команда ${command} завершилась с кодом ${result.status}.`);
}
if (process.platform !== 'win32') throw new Error('Этот выпуск предназначен для Windows x64.');
run(process.execPath, ['scripts/versioning.mjs', 'check']);
for (const [project, folder] of [['Axiom.Cli', 'cli'], ['Axiom.DesktopHost', 'host']]) {
  run('dotnet', ['publish', `src/${project}`, '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
    '-p:NuGetAudit=false', '-o', path.join(staging, folder)]);
}
// Пересоздаётся только собственный каталог выпуска внутри .artifacts.
fs.rmSync(destination, { recursive: true, force: true });
fs.mkdirSync(destination, { recursive: true });
fs.cpSync(path.join(root, 'ide/node_modules/electron/dist'), destination, { recursive: true });
fs.renameSync(path.join(destination, 'electron.exe'), path.join(destination, 'Axiom IDE.exe'));
const application = path.join(destination, 'resources/app');
fs.mkdirSync(application, { recursive: true });
const versions = JSON.parse(fs.readFileSync(path.join(root, 'versions.json'), 'utf8'));
fs.writeFileSync(path.join(application, 'package.json'), JSON.stringify({
  name: 'axiom-ide', productName: 'Axiom IDE', version: versions.ide, main: 'ide/src/main/main.js',
}, null, 2));
fs.cpSync(path.join(root, 'ide/src'), path.join(application, 'ide/src'), { recursive: true });
fs.copyFileSync(path.join(root, 'ide/package.json'), path.join(application, 'ide/package.json'));
const dependencies = JSON.parse(fs.readFileSync(path.join(root, 'ide/package.json'), 'utf8')).dependencies;
// node-pty и его зависимости имеют собственные готовые Windows-модули.
const copied = new Set();
function copyDependency(name) {
  if (copied.has(name)) return;
  copied.add(name);
  const source = path.join(root, 'ide/node_modules', name);
  const pkg = JSON.parse(fs.readFileSync(path.join(source, 'package.json'), 'utf8'));
  fs.cpSync(source, path.join(application, 'ide/node_modules', name), { recursive: true });
  for (const child of Object.keys(pkg.dependencies || {})) copyDependency(child);
}
for (const name of Object.keys(dependencies)) copyDependency(name);
for (const folder of ['cli', 'host']) fs.cpSync(path.join(staging, folder), path.join(application, 'runtime', folder), { recursive: true });
fs.cpSync(path.join(root, 'docs/manual'), path.join(application, 'docs/manual'), { recursive: true });
fs.cpSync(path.join(root, 'samples'), path.join(application, 'samples'), { recursive: true });
const { sourceFingerprint } = require('../ide/src/main/backend-fingerprint.js');
fs.writeFileSync(path.join(application, 'portable-runtime.json'), JSON.stringify({
  version: 1, sourceFingerprint: sourceFingerprint(root), versions,
}, null, 2));
fs.writeFileSync(path.join(destination, 'ПРОЧИТАЙТЕ.txt'),
  'Axiom IDE для Windows x64\r\n\r\nЗапустите Axiom IDE.exe. Установка SDK, Git и Node.js не требуется.\r\n' +
  'Примеры: resources\\app\\samples. Справочник: resources\\app\\docs\\manual\\index.html.\r\n' +
  'Переносите всю папку целиком. Настройки и восстановление документов хранятся в профиле Windows.\r\n');
console.log(`Переносимая поставка: ${destination}`);
