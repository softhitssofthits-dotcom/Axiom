import fs from 'node:fs';
import path from 'node:path';
import {
  manual, inventory, rows, byName, names, families, familyOf, navGroups, nav,
  navTitles, signature, resultText, escapeHtml,
} from './registry.mjs';
import {basics} from './content-basics.mjs';
import {control} from './content-control.mjs';
import {structure} from './content-structure.mjs';
import {data} from './content-data.mjs';
import {io} from './content-io.mjs';
import {sys} from './content-sys.mjs';
import {diagnosticsPage} from './content-diagnostics.mjs';

const pages = new Map();
for (const [id, page] of Object.entries({...basics, ...control, ...structure, ...data, ...io, ...sys})) {
  pages.set(id, page);
}
pages.set('diagnostics', diagnosticsPage);

const contracts = {
  numbers: 'Числовые функции принимают INTEGER, LONG, DOUBLE или DECIMAL, если карточка не ограничивает тип. ABS, SGN, FLOOR, CEIL, ROUND, FIX и SETSIGN сохраняют тип аргумента; MIN, MAX и CLAMP возвращают общий числовой тип; тригонометрия, логарифмы, корни и POW возвращают DOUBLE. CDBL, CINT, CLNG и CDEC выполняют явное преобразование, VAL разбирает текст, STR строит текст. Переполнение результата даёт MB3008, недопустимый аргумент — MB3012.',
  strings: 'Длина, позиции и границы строк измеряются в кодовых единицах UTF-16. Функции поиска возвращают позицию в тех же единицах или 0, если совпадения нет. Пустая строка допустима как аргумент и результат. Предел одного значения — 16 777 216 кодовых единиц; превышение даёт MB3007.',
  arrays: 'Массив хранит элементы одного типа и имеет ранг и включающие границы каждого измерения. Дробный индекс даёт MB3007, выход за границу — MB3002, обращение до выделения — MB3005. Многомерные функции используют номер измерения начиная с 1 либо константы ROW и COLUMN.',
  dates: 'DATE хранит календарный день, TIME — время суток, DATETIME — дату и время без пояса, UTCDATETIME — момент UTC. TIME и DATETIME сохраняют доли секунды. Неявное сужение DATETIME до DATE или TIME запрещено; явное извлечение выполняют DATEVALUE и TIMEVALUE.',
  files: 'OPEN связывает путь с переменной FILE и режимом INPUT, OUTPUT или APPEND. Операции чтения и записи требуют открытого файла подходящего режима; ошибка режима даёт MB3005. Пути разрешаются относительно рабочей папки запуска. Функции каталогов не создают отсутствующие родительские папки, если это не указано в карточке.',
  regex: 'Шаблоны используют синтаксис .NET. Параметр modes принимает буквы i, m, s и x. Неверный шаблон сообщает смещение и вид ошибки; один синхронный поиск ограничен пятью секундами и при превышении даёт MB3007.',
  graphics: 'Функции читают состояние поверхности и ресурсов. Без выбранного SCREEN или CANVAS графические операции дают MB2046. Координаты измеряются в пикселях поверхности от левого верхнего угла; цвет задаётся индексом палитры или значением RGB.',
  tilemaps: 'Карта загружается из формата Tiled. Номера слоёв и объектов начинаются с 0. Неподдержанная форма файла отклоняется с причиной в MB3005. Отсутствующее свойство не подменяется пустым значением.',
};

for (const family of families) {
  const familyNames = names.filter(name => familyOf.get(name) === family.id);
  const formCount = familyNames.reduce((n, name) => n + byName.get(name).length, 0);
  const cards = familyNames.map(name => {
    const forms = byName.get(name).map(row => {
      const args = row.Parameters.length
        ? `<table class="args"><thead><tr><th>Аргумент</th><th>Тип</th></tr></thead><tbody>${row.Parameters.map(p => `<tr><td><code>${escapeHtml(p.Name.replaceAll('_', ' '))}</code></td><td><code>${escapeHtml(p.Type)}</code></td></tr>`).join('')}</tbody></table>`
        : '<p class="muted">Аргументов нет.</p>';
      return `<div class="form">
<pre><code class="language-axiom">${escapeHtml(signature(row))}</code></pre>
<p class="result">Результат: <code>${escapeHtml(resultText(row))}</code>.</p>
${args}
</div>`;
    }).join('\n');
    return `<article class="card" id="fn-${name.toLowerCase()}">
<h3>${name}</h3>
${forms}
</article>`;
  }).join('\n');

  pages.set(`${family.id}-functions`, {
    title: `Функции: ${family.title.toLowerCase()}`,
    lead: `${familyNames.length} ${familyNames.length === 1 ? 'функция' : 'функций'} раздела «${family.title}» — ${formCount} ${formCount === 1 ? 'зарегистрированная форма' : 'зарегистрированных форм'} с типами аргументов и результата.`,
    body: `
<section><h2>Общий договор</h2>
<p>${contracts[family.id]}</p>
<p>Все перечисленные аргументы обязательны; необязательный аргумент представлен отдельной формой. Объяснение типов и общего поведения находится в главе <a href="${family.id}.html">${family.title}</a>.</p>
</section>
<section><h2>Карточки</h2>
${cards}
</section>`,
  });
}

const statementHrefs = {
  DIM: 'variables.html', CONST: 'constants.html', 'Присваивание': 'variables.html',
  PRINT: 'print.html', 'PRINT USING': 'print.html', INPUT: 'input.html', SLEEP: 'events.html',
  RANDOMIZE: 'numbers.html',
  IF: 'branching.html', 'SELECT CASE': 'branching.html', FOR: 'loops.html', WHILE: 'loops.html',
  'DO ... LOOP': 'loops.html', 'EXIT FOR': 'loops.html', 'EXIT DO': 'loops.html',
  'CONTINUE FOR': 'loops.html', 'CONTINUE WHILE': 'loops.html', 'CONTINUE DO': 'loops.html',
  SUB: 'sub.html', FUNCTION: 'function.html', RETURN: 'sub.html', TYPE: 'records.html',
  TRY: 'errors.html', ERROR: 'errors.html', IMPORT: 'modules.html',
  OPEN: 'files.html', CLOSE: 'files.html',
  REDIM: 'arrays.html', 'REDIM PRESERVE': 'arrays.html', ERASE: 'arrays.html',
  SCREEN: 'graphics.html', SMOOTH: 'graphics.html', CANVAS: 'graphics.html',
  BEGINFRAME: 'graphics.html', PRESENT: 'graphics.html', CAMERA: 'graphics.html',
  VIEWPORT: 'graphics.html', RESETCAMERA: 'graphics.html', VIEW: 'graphics.html',
  'VIEW PRINT': 'graphics.html', WINDOW: 'graphics.html', CLS: 'graphics.html',
  COLOR: 'graphics.html', LOCATE: 'graphics.html', PALETTE: 'graphics.html',
  PCOPY: 'graphics.html', PSET: 'graphics.html', PRESET: 'graphics.html',
  GET: 'graphics.html', PUT: 'graphics.html', LINE: 'graphics.html',
  CIRCLE: 'graphics.html', PAINT: 'graphics.html', DRAW: 'graphics.html',
  DRAWLINE: 'graphics.html', DRAWCIRCLE: 'graphics.html', FILLCIRCLE: 'graphics.html',
  DRAWRECT: 'graphics.html', FILLRECT: 'graphics.html', DRAWTEXT: 'graphics.html',
  DRAWIMAGE: 'graphics.html', DRAWSPRITE: 'graphics.html', DRAWTILE: 'graphics.html',
  DRAWTILEMAP: 'graphics.html', DRAWTILELAYER: 'graphics.html', SETMAPTILE: 'graphics.html',
  'ON KEY': 'events.html', 'ON MOUSE': 'events.html', 'Вызов как инструкция': 'sub.html',
};

const registered = new Set(
  JSON.parse(fs.readFileSync(path.join(manual, '..', 'manual', 'data', 'language-reference.json'), 'utf8')).statements.map(s => s.name),
);
const missing = [...registered].filter(name => !(name in statementHrefs));
const extra = Object.keys(statementHrefs).filter(name => !registered.has(name));
if (missing.length) throw new Error(`Нет описания оператора: ${missing.join(', ')}`);
if (extra.length) throw new Error(`Неизвестный оператор: ${extra.join(', ')}`);

pages.set('reference', {
  title: 'Указатели',
  lead: 'Все зарегистрированные имена языка в одном месте: встроенные функции, операторы и объявления, типы данных и ключевые слова.',
  body: `
<section><h2>Встроенные функции</h2>
<p>${names.length} имён и ${rows.length} форм вызова. Ссылка ведёт к карточке с точными аргументами и типом результата.</p>
<div class="chips">${names.map(name => `<a href="${familyOf.get(name)}-functions.html#fn-${name.toLowerCase()}">${name}</a>`).join(' ')}</div>
</section>
<section><h2>Операторы и объявления</h2>
<table><thead><tr><th>Запись</th><th>Глава</th></tr></thead><tbody>
${Object.entries(statementHrefs).map(([name, href]) => `<tr><td>${escapeHtml(name)}</td><td><a href="${href}">${escapeHtml(navTitles.get(href.replace('.html', '')) ?? '')}</a></td></tr>`).join('\n')}
</tbody></table>
</section>
<section><h2>Типы данных</h2>
<div class="chips">${inventory.Integrity.TypeNames.map(t => `<a href="types.html">${t}</a>`).join(' ')}</div>
</section>
<section><h2>Ключевые слова</h2>
<div class="chips">${inventory.Integrity.Keywords.map(w => `<span>${w}</span>`).join(' ')}</div>
</section>`,
});

function layout(id, page) {
  const index = nav.findIndex(n => n.id === id);
  const prev = index > 0 ? nav[index - 1] : null;
  const next = index >= 0 && index < nav.length - 1 ? nav[index + 1] : null;
  return `<!doctype html>
<html lang="ru">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escapeHtml(page.title)} — Справочник Axiom</title>
<link rel="stylesheet" href="styles/manual.css">
</head>
<body data-page="${id}">
<a class="skip" href="#content">К содержанию</a>
<header class="topbar">
<a class="brand" href="index.html">Axiom<span>справочник</span></a>
<form class="search" role="search" action="search.html" method="get">
<label class="sr" for="q">Поиск по справочнику</label>
<input id="q" name="q" type="search" placeholder="Имя функции, оператора или тема" autocomplete="off">
<button type="submit">Найти</button>
</form>
<button class="menu-btn" type="button" aria-expanded="false" aria-controls="side">Оглавление</button>
</header>
<div class="shell">
<aside id="side" class="side"><nav id="nav" aria-label="Оглавление"></nav></aside>
<main id="content">
<nav class="crumbs" aria-label="Путь"><a href="index.html">Справочник</a> / ${escapeHtml(page.title)}</nav>
<h1>${escapeHtml(page.title)}</h1>
<p class="lead">${page.lead}</p>
${page.body}
<nav class="pager">${prev ? `<a class="prev" href="${prev.id}.html">← ${escapeHtml(prev.title)}</a>` : '<span></span>'}${next ? `<a class="next" href="${next.id}.html">${escapeHtml(next.title)} →</a>` : '<span></span>'}</nav>
</main>
</div>
<script src="scripts/navigation.js"></script>
<script src="scripts/search-index.js"></script>
<script src="scripts/highlight.js"></script>
<script src="scripts/app.js"></script>
</body>
</html>`;
}

for (const [id, page] of pages) {
  fs.writeFileSync(path.join(manual, `${id}.html`), layout(id, page));
}

fs.writeFileSync(path.join(manual, 'index.html'), `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Справочник Axiom</title><link rel="stylesheet" href="styles/manual.css"></head>
<body data-page="index">
<a class="skip" href="#content">К содержанию</a>
<header class="topbar"><a class="brand" href="index.html">Axiom<span>справочник</span></a>
<form class="search" role="search" action="search.html" method="get"><label class="sr" for="q">Поиск</label><input id="q" name="q" type="search" placeholder="Найти имя или тему" autocomplete="off"><button type="submit">Найти</button></form>
<button class="menu-btn" type="button" aria-expanded="false" aria-controls="side">Оглавление</button></header>
<div class="shell"><aside id="side" class="side"><nav id="nav" aria-label="Оглавление"></nav></aside>
<main id="content"><h1>Справочник языка Axiom</h1>
<p class="lead">Полное описание действующего языка: от первой программы до модулей, файлов, графики и обработки ошибок.</p>
<section><h2>Разделы</h2>
<div class="home-grid">${navGroups.map(group => `<div class="group"><h3>${escapeHtml(group.title)}</h3>${group.items.map(([id, title]) => `<a href="${id}.html">${escapeHtml(title)}</a>`).join('')}</div>`).join('\n')}</div>
</section>
<section><h2>Что внутри</h2>
<p>Справочник построен по реестру компилятора: ${names.length} встроенных функций, ${rows.length} зарегистрированных форм вызова, ${registered.size} операторов и объявлений, ${inventory.Integrity.TypeNames.length} типов данных, ${inventory.Integrity.Keywords.length} ключевых слов. Примеры проверены текущим компилятором; для каждого указан результат.</p>
</section>
</main></div>
<script src="scripts/navigation.js"></script><script src="scripts/search-index.js"></script><script src="scripts/highlight.js"></script><script src="scripts/app.js"></script></body></html>`);

fs.writeFileSync(path.join(manual, 'search.html'), `<!doctype html>
<html lang="ru"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Поиск — Справочник Axiom</title><link rel="stylesheet" href="styles/manual.css"></head>
<body data-page="search">
<a class="skip" href="#content">К содержанию</a>
<header class="topbar"><a class="brand" href="index.html">Axiom<span>справочник</span></a>
<form class="search" role="search" action="search.html" method="get"><label class="sr" for="q">Поиск</label><input id="q" name="q" type="search" placeholder="Найти имя или тему" autocomplete="off"><button type="submit">Найти</button></form>
<button class="menu-btn" type="button" aria-expanded="false" aria-controls="side">Оглавление</button></header>
<div class="shell"><aside id="side" class="side"><nav id="nav" aria-label="Оглавление"></nav></aside>
<main id="content"><h1>Поиск</h1><p class="lead">Введите запрос в поле выше. Поиск охватывает имена, аргументы, главы и описания.</p>
<p id="status" role="status" aria-live="polite"></p><div id="results"></div></main></div>
<script src="scripts/navigation.js"></script><script src="scripts/search-index.js"></script><script src="scripts/highlight.js"></script><script src="scripts/app.js"></script></body></html>`);

fs.writeFileSync(path.join(manual, 'scripts', 'navigation.js'),
  `// Порядок и группировка нового справочника.\nwindow.AxiomManual2Nav = ${JSON.stringify(navGroups)};\n`);

const index = [];
for (const [id, page] of pages) {
  index.push({title: page.title, href: `${id}.html`, kind: 'Глава', text: `${page.title} ${page.lead}`});
}
for (const name of names) {
  const first = byName.get(name)[0];
  index.push({
    title: name,
    href: `${familyOf.get(name)}-functions.html#fn-${name.toLowerCase()}`,
    kind: 'Функция',
    text: `${name} ${byName.get(name).map(signature).join(' ')} ${first.Parameters.map(p => `${p.Name} ${p.Type}`).join(' ')} ${first.ReturnType}`,
  });
}
for (const [name, href] of Object.entries(statementHrefs)) {
  index.push({title: name, href, kind: 'Оператор', text: `${name} ${navTitles.get(href.replace('.html', '')) ?? ''}`});
}
for (const type of inventory.Integrity.TypeNames) {
  index.push({title: type, href: 'types.html', kind: 'Тип', text: `Тип данных ${type}`});
}
for (const word of inventory.Integrity.Keywords) {
  index.push({title: word, href: 'reference.html', kind: 'Ключевое слово', text: `Ключевое слово ${word}`});
}
fs.writeFileSync(path.join(manual, 'scripts', 'search-index.js'),
  `window.AxiomManual2Index = ${JSON.stringify(index)};\n`);

console.log(`Страниц: ${pages.size + 2}; функций: ${names.length}; форм: ${rows.length}; операторов: ${registered.size}; записей поиска: ${index.length}.`);
