import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const args = process.argv.slice(2), command = args.shift() || 'check';
const components = ['language', 'compiler', 'ide'];
const gitRaw = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
const git = (...args) => gitRaw(...args).trim();
const staged = args.includes('--staged');
const read = file => staged ? gitRaw('show', ':' + file) : fs.readFileSync(path.join(root, file), 'utf8');
const write = (file, text) => fs.writeFileSync(path.join(root, file), text, 'utf8');
const json = file => JSON.parse(read(file));
const valid = value => typeof value === 'string' && /^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(value);
const fail = message => { throw new Error(message); };
const validate = versions => {
  for (const c of components) if (!valid(versions[c])) fail(`Некорректная версия ${c}: ${versions[c]}`);
};
const coreSource = v => `// Создан scripts/versioning.mjs из versions.json. Не редактировать вручную.\nnamespace Axiom.Core;\n\npublic static class AxiomVersionInfo\n{\n    public const string LanguageVersion = "${v.language}";\n    public const string CompilerVersion = "${v.compiler}";\n}\n`;
const propsSource = v => `<Project>\n  <!-- Создан scripts/versioning.mjs из versions.json. -->\n  <PropertyGroup>\n    <AxiomLanguageVersion>${v.language}</AxiomLanguageVersion>\n    <AxiomCompilerVersion>${v.compiler}</AxiomCompilerVersion>\n    <AxiomIdeVersion>${v.ide}</AxiomIdeVersion>\n    <Version Condition="'$(MSBuildProjectName)' != 'Axiom.DesktopHost'">${v.compiler}</Version>\n    <Version Condition="'$(MSBuildProjectName)' == 'Axiom.DesktopHost'">${v.ide}</Version>\n  </PropertyGroup>\n</Project>\n`;
const row = v => `Язык **${v.language}** · компилятор/CLI **${v.compiler}** · IDE **${v.ide}**`;
function sync(v) {
  validate(v);
  write('src/Axiom.Core/AxiomVersionInfo.cs', coreSource(v));
  write('Versions.props', propsSource(v));
  for (const file of ['ide/package.json', 'ide/package-lock.json']) {
    const data = JSON.parse(fs.readFileSync(path.join(root, file), 'utf8'));
    data.version = v.ide;
    if (data.packages?.['']) data.packages[''].version = v.ide;
    write(file, JSON.stringify(data, null, 2) + '\n');
  }
}
function check() {
  const v = json('versions.json'); validate(v);
  for (const [file, expected] of [['Versions.props', propsSource(v)], ['src/Axiom.Core/AxiomVersionInfo.cs', coreSource(v)]]) {
    if (read(file).replace(/\r\n/g, '\n') !== expected) fail(`${file} рассинхронизирован. Выполните node scripts/versioning.mjs sync.`);
  }
  if (json('ide/package.json').version !== v.ide || json('ide/package-lock.json').version !== v.ide || json('ide/package-lock.json').packages[''].version !== v.ide)
    fail('Версия пакета IDE не соответствует versions.json. Выполните sync.');
  const headings = [...read('docs/CHANGELOG.md').matchAll(/^Язык \*\*[^\n]+$/gm)];
  if (headings.at(-1)?.[0].trim() !== row(v)) fail('Последняя запись docs/CHANGELOG.md не соответствует текущим версиям.');
  const baseIndex = args.indexOf('--base');
  const base = baseIndex >= 0 ? args[baseIndex + 1] : staged ? 'HEAD' : null;
  if (base) {
    if (!base || !/^[\w./^~:-]+$/.test(base)) fail('Некорректная базовая ревизия.');
    let old;
    // Единственное исключение: создание системы версий в репозитории без versions.json.
    const present = git('ls-tree', '--name-only', base, '--', 'versions.json');
    if (present) {
      old = JSON.parse(git('show', base + ':versions.json')); validate(old);
      const paths = git('diff', ...(staged ? ['--cached'] : []), '--name-only', base, '--').split('\n');
      const required = new Set();
      for (const p of paths) {
        if (/^src\/Axiom\.(Core|Lexing|Syntax|Parsing|Semantics|Bytecode|Runtime|Execution|Formatting)\//.test(p) && !p.endsWith('/AxiomVersionInfo.cs')) {
          required.add('language'); required.add('compiler');
        } else if (/^src\/Axiom\.Cli\//.test(p)) required.add('compiler');
        else if (/^(ide\/(src\/|scripts\/|package(?:-lock)?\.json$)|src\/Axiom\.(DesktopHost|Ide\.Backend|Ide\.Contracts)\/)/.test(p)) required.add('ide');
        else if (/^samples\//.test(p)) { required.add('language'); required.add('compiler'); }
        else if (/^src\//.test(p)) required.add('compiler');
      }
      const compare = (a, b) => { const x=a.split('.').map(Number), y=b.split('.').map(Number); for(let n=0;n<3;n++) if(x[n]!==y[n]) return Math.sign(x[n]-y[n]); return 0; };
      for (const c of components) {
        if (compare(v[c], old[c]) < 0) fail(`Версия ${c} уменьшилась.`);
        if (required.has(c) && compare(v[c], old[c]) <= 0) fail(`Изменён ${c}, но версия ${old[c]} не повышена. Используйте bump.`);
      }
      const substantiveDocs = paths.some(p => !/^(docs\/manual\/|docs\/VERSIONING\.md$|AGENTS\.md$|scripts\/versioning\.mjs$)/.test(p) && /^(docs\/(?!archive\/|reports\/|CHANGELOG\.md|WORKLOG5\.md)|README\.md|PROJECT\.md|AGENTS\.md|scripts\/)/.test(p));
      if (substantiveDocs && components.every(c => v[c] === old[c])) fail('Изменены действующие документы/инструменты, но ни одна версия не повышена. Выберите затронутый компонент.');
      if (components.some(c => v[c] !== old[c]) && git('show', base + ':docs/CHANGELOG.md') === read('docs/CHANGELOG.md').trim()) fail('Повышение версии требует записи в docs/CHANGELOG.md.');
    }
  }
  console.log('Версии согласованы: ' + row(v).replaceAll('**', ''));
}
try {
  if (command === 'sync') sync(json('versions.json'));
  else if (command === 'check') check();
  else if (command === 'bump') {
    const scope = args[0], level = args[1] || 'patch', message = args.slice(2).join(' ').trim();
    if (!['language', 'compiler', 'ide', 'all'].includes(scope) || !['patch', 'minor', 'major'].includes(level) || !message) fail('Команда: node scripts/versioning.mjs bump <language|compiler|ide|all> <patch|minor|major> "Описание изменения"');
    if (level === 'major') fail('Переход MAJOR выполняется отдельно по прямому решению автора; измените versions.json, выполните sync и добавьте запись в журнал.');
    const v = json('versions.json'); validate(v);
    const affected = scope === 'all' ? components : scope === 'language' ? ['language', 'compiler'] : [scope];
    for (const c of affected) { const n=v[c].split('.').map(Number); if(level==='minor'){n[1]++;n[2]=0;}else n[2]++; v[c]=n.join('.'); }
    write('versions.json', JSON.stringify(v, null, 2) + '\n'); sync(v);
    const date = new Intl.DateTimeFormat('sv-SE', {timeZone:'Europe/Moscow',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hour12:false}).format(new Date());
    fs.appendFileSync(path.join(root, 'docs/CHANGELOG.md'), `\n## ${date} — ${message}\n\n${row(v)}\n\n${message}\n`, 'utf8');
    check();
  } else if (command === 'install-hooks') {
    let configured = '';
    try { configured = git('config', '--get', 'core.hooksPath'); } catch { /* настройка ещё не задана */ }
    if (configured && configured !== '.githooks') fail(`Уже настроены обработчики Git: ${configured}. Подключите проверку версий к ним вручную.`);
    git('config', 'core.hooksPath', '.githooks');
    console.log('Проверка версий перед коммитом подключена.');
  } else fail('Неизвестная команда: ' + command);
} catch (error) { console.error('Версионирование: ' + error.message); process.exitCode = 1; }
