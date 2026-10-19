import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const fixtures = path.join(root, '.artifacts', 'versioning-check-tests');
fs.mkdirSync(fixtures, { recursive: true });

function fixture() {
  const directory = fs.mkdtempSync(path.join(fixtures, 'case-'));
  const write = (file, text) => {
    const target = path.join(directory, file);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    fs.writeFileSync(target, text);
  };
  const git = (...args) => execFileSync('git', args, { cwd: directory, stdio: 'pipe' });
  write('scripts/versioning.mjs', fs.readFileSync(path.join(root, 'scripts/versioning.mjs'), 'utf8'));
  write('versions.json', JSON.stringify({ language: '0.1.0', compiler: '0.1.0', ide: '0.1.0' }));
  write('ide/package.json', '{}');
  write('ide/package-lock.json', '{"packages":{"":{}}}');
  write('docs/CHANGELOG.md', 'Язык **0.1.0** · компилятор/CLI **0.1.0** · IDE **0.1.0**\n');
  fs.mkdirSync(path.join(directory, 'src/Axiom.Core'), { recursive: true });
  execFileSync(process.execPath, ['scripts/versioning.mjs', 'sync'], { cwd: directory });
  git('init', '--initial-branch=master');
  git('config', 'user.name', 'Versioning tests');
  git('config', 'user.email', 'versioning-tests@example.invalid');
  git('add', '.');
  git('commit', '-m', 'Исходное состояние проверки');
  const check = (...args) => spawnSync(process.execPath, ['scripts/versioning.mjs', 'check', ...args], { cwd: directory, encoding: 'utf8' });
  return { directory, write, git, check };
}

test('Правки справочника и сопровождающих записей не требуют повышения', () => {
  const f = fixture();
  for (const file of ['docs/manual/isnumeric.html', 'docs/PLANS4.md', 'docs/WORKLOG5.md', 'docs/BYTECODE_COVERAGE.md', 'docs/BYTECODE_PERF_MATRIX.md', 'docs/reports/manual/REVIEW.md', 'scripts/build-manual-index.mjs']) f.write(file, 'Правка справочника\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 0, result.stderr);
});

test('Изменение runnable-примера без изменений реализации не требует повышения', () => {
  const f = fixture();
  f.write('samples/graphics/game/maze_chase.axm', 'Изменение игрового примера\n');
  f.write('samples/graphics/game/maze_chase_pellet_calibration.txt', 'Изменение настроек примера\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 0, result.stderr);
});

test('Обработчик проверяет подготовленный справочник, а не более новые исходники', () => {
  const f = fixture();
  f.write('docs/manual/isnumeric.html', 'Правка\n');
  f.write('docs/PLANS4.md', 'Запись\n');
  f.git('add', 'docs/manual/isnumeric.html', 'docs/PLANS4.md');
  f.write('src/Axiom.Runtime/New.cs', 'Изменение реализации\n');
  assert.equal(f.check('--staged').status, 0);
});

test('Совмещённые правки реализации языка по-прежнему требуют повышения', () => {
  const f = fixture();
  f.write('docs/manual/isnumeric.html', 'Правка\n');
  f.write('src/Axiom.Runtime/New.cs', 'Изменение реализации\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён language/);
});

test('Совмещённые правки реализации IDE по-прежнему требуют повышения', () => {
  const f = fixture();
  f.write('docs/manual/isnumeric.html', 'Правка\n');
  f.write('ide/src/new.js', 'Изменение реализации\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён ide/);
});

function compilerChange(f, version = '0.1.1', declaredVersion = version) {
  f.write('versions.json', JSON.stringify({ language: '0.1.0', compiler: version, ide: '0.1.0',
    compilerOnlyChange: { version: declaredVersion, reason: 'Редакторские метаданные без изменения правил языка',
      paths: ['src/Axiom.Semantics/Binding/Binder.EditorSymbols.cs'] } }));
  f.write('docs/CHANGELOG.md', `Язык **0.1.0** · компилятор/CLI **${version}** · IDE **0.1.0**\n`);
  execFileSync(process.execPath, ['scripts/versioning.mjs', 'sync'], { cwd: f.directory });
  f.write('src/Axiom.Semantics/Binding/Binder.EditorSymbols.cs', 'Редакторские метаданные\n');
  f.git('add', '.');
}

test('Явная классификация редакторской правки повышает компилятор без изменения версии языка', () => {
  const f = fixture(); compilerChange(f);
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 0, result.stderr);
  assert.equal(f.check('--staged').status, 0);
});

test('Классификация прежнего выпуска не действует после нового повышения компилятора', () => {
  const f = fixture(); compilerChange(f, '0.1.2', '0.1.1');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён language/);
});

test('Явная классификация не отменяет обязательное повышение компилятора', () => {
  const f = fixture(); compilerChange(f, '0.1.0');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён compiler/);
});

test('Классификация действует только для перечисленных файлов', () => {
  const f = fixture(); compilerChange(f);
  f.write('src/Axiom.Runtime/New.cs', 'Изменение исполнения\n'); f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён language/);
});

test('Классификация не принимает пути реализации VM и пустую причину', () => {
  const f = fixture(); compilerChange(f);
  const versions = JSON.parse(fs.readFileSync(path.join(f.directory, 'versions.json'), 'utf8'));
  versions.compilerOnlyChange.paths = ['src/Axiom.Runtime/New.cs'];
  f.write('versions.json', JSON.stringify(versions));
  assert.match(f.check('--base', 'HEAD').stderr, /compilerOnlyChange требует/);
  versions.compilerOnlyChange.paths = ['src/Axiom.Semantics/Binding/Binder.EditorSymbols.cs'];
  versions.compilerOnlyChange.reason = ' ';
  f.write('versions.json', JSON.stringify(versions));
  assert.match(f.check('--base', 'HEAD').stderr, /compilerOnlyChange требует/);
});

test('Классификация подготовленного набора не берётся из более новой рабочей версии', () => {
  const f = fixture(); compilerChange(f);
  const versions = JSON.parse(fs.readFileSync(path.join(f.directory, 'versions.json'), 'utf8'));
  delete versions.compilerOnlyChange; f.write('versions.json', JSON.stringify(versions));
  assert.equal(f.check('--staged').status, 0);
  assert.match(f.check('--base', 'HEAD').stderr, /Изменён language/);
});

test('Новое повышение компилятора удаляет классификацию прежнего выпуска', () => {
  const f = fixture(); compilerChange(f);
  const bump = spawnSync(process.execPath, ['scripts/versioning.mjs', 'bump', 'compiler', 'patch', 'Следующий выпуск'],
    { cwd: f.directory, encoding: 'utf8' });
  assert.equal(bump.status, 0, bump.stderr);
  const versions = JSON.parse(fs.readFileSync(path.join(f.directory, 'versions.json'), 'utf8'));
  assert.equal(versions.compiler, '0.1.2');
  assert.equal(versions.compilerOnlyChange, undefined);
});

test('Документация и инструменты сопровождения не требуют продуктовой версии', () => {
  const f = fixture();
  f.write('AGENTS.md', 'Уточнение правил разработки\n');
  f.write('docs/PLANS4.md', 'Запись в план\n');
  f.write('docs/VERSIONING.md', 'Уточнение правил версий\n');
  const versioningScript = fs.readFileSync(path.join(root, 'scripts/versioning.mjs'), 'utf8');
  f.write('scripts/versioning.mjs', `${versioningScript}\n// Изменение проверки версий\n`);
  f.write('scripts/versioning-check.test.mjs', 'Изменение проверки правил\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 0, result.stderr);
});

test('Изменение упаковки IDE требует повышения только IDE', () => {
  const f = fixture();
  f.write('scripts/package-ide.mjs', 'Изменение упаковки приложения\n');
  f.git('add', '.');
  const result = f.check('--base', 'HEAD');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /Изменён ide/);
});

const readme = (language, compiler, ide) => [
  '# Axiom',
  '',
  `![version](https://img.shields.io/badge/version-${language}-blue)`,
  `![IDE](https://img.shields.io/badge/IDE-${ide}-green)`,
  '![platform](https://img.shields.io/badge/platform-Windows-lightgrey)',
  '',
  `Текущие версии: язык \`${language}\`, компилятор \`${compiler}\`, IDE \`${ide}\`.`,
  '',
].join('\n');

test('Устаревшие номера в README обнаруживаются проверкой', () => {
  const f = fixture();
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.git('add', 'README.md');
  f.git('commit', '-m', 'README с текущими номерами');
  assert.equal(f.check().status, 0);
  f.write('versions.json', JSON.stringify({ language: '0.1.1', compiler: '0.1.1', ide: '0.1.0' }));
  f.write('docs/CHANGELOG.md', 'Язык **0.1.0** · компилятор/CLI **0.1.0** · IDE **0.1.0**\n\n## Проверка\n\nЯзык **0.1.1** · компилятор/CLI **0.1.1** · IDE **0.1.0**\n');
  execFileSync(process.execPath, ['scripts/versioning.mjs', 'sync'], { cwd: f.directory });
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.git('add', '.');
  const result = f.check();
  assert.equal(result.status, 1);
  assert.match(result.stderr, /README\.md рассинхронизирован/);
});

test('sync возвращает README к текущим номерам', () => {
  const f = fixture();
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.write('versions.json', JSON.stringify({ language: '0.2.0', compiler: '0.2.1', ide: '0.3.4' }));
  execFileSync(process.execPath, ['scripts/versioning.mjs', 'sync'], { cwd: f.directory });
  const text = fs.readFileSync(path.join(f.directory, 'README.md'), 'utf8');
  assert.match(text, /version-0\.2\.0-blue/);
  assert.match(text, /IDE-0\.3\.4-green/);
  assert.match(text, /язык `0\.2\.0`, компилятор `0\.2\.1`, IDE `0\.3\.4`/);
});

test('bump обновляет README вместе с версиями', () => {
  const f = fixture();
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.git('add', '.');
  f.git('commit', '-m', 'README перед повышением');
  const bump = spawnSync(process.execPath, ['scripts/versioning.mjs', 'bump', 'language', 'patch', 'Проверка README'], { cwd: f.directory, encoding: 'utf8' });
  assert.equal(bump.status, 0, bump.stderr);
  const text = fs.readFileSync(path.join(f.directory, 'README.md'), 'utf8');
  assert.match(text, /version-0\.1\.1-blue/);
  assert.match(text, /язык `0\.1\.1`, компилятор `0\.1\.1`, IDE `0\.1\.0`/);
});

test('Проверка сообщает об отсутствии бейджа версии в README', () => {
  const f = fixture();
  f.write('README.md', '# Axiom\n\nТекущие версии: язык `0.1.0`, компилятор `0.1.0`, IDE `0.1.0`.\n');
  f.git('add', 'README.md');
  const result = f.check();
  assert.equal(result.status, 1);
  assert.match(result.stderr, /не найден бейдж version/);
});

test('Обработчик замечает устаревший README в подготовленном наборе', () => {
  const f = fixture();
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.git('add', '.');
  f.git('commit', '-m', 'README с текущими номерами');
  f.write('versions.json', JSON.stringify({ language: '0.1.1', compiler: '0.1.1', ide: '0.1.1' }));
  execFileSync(process.execPath, ['scripts/versioning.mjs', 'sync'], { cwd: f.directory });
  f.write('docs/CHANGELOG.md', 'Язык **0.1.0** · компилятор/CLI **0.1.0** · IDE **0.1.0**\n\n## Проверка\n\nЯзык **0.1.1** · компилятор/CLI **0.1.1** · IDE **0.1.1**\n');
  f.write('README.md', readme('0.1.0', '0.1.0', '0.1.0'));
  f.git('add', '.');
  const result = f.check('--staged');
  assert.equal(result.status, 1);
  assert.match(result.stderr, /README\.md рассинхронизирован/);
});
