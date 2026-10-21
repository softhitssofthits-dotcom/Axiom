import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const manual = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const errors = [];
const htmlFiles = fs.readdirSync(manual).filter(name => name.endsWith('.html'));

const read = name => fs.readFileSync(path.join(manual, name), 'utf8');
const ids = new Map();
for (const file of htmlFiles) {
  const html = read(file);
  const found = [...html.matchAll(/\sid="([^"]+)"/g)].map(m => m[1]);
  const duplicates = found.filter((id, index) => found.indexOf(id) !== index);
  if (duplicates.length) errors.push(`${file}: повтор id ${[...new Set(duplicates)].join(', ')}`);
  ids.set(file, new Set(found));
  if (!html.startsWith('<!doctype html>')) errors.push(`${file}: нет doctype`);
  if (!/<h1>/.test(html)) errors.push(`${file}: нет h1`);
  if (!/<title>[^<]+<\/title>/.test(html)) errors.push(`${file}: нет title`);
  if (/&(?!(amp|lt|gt|quot|#\d+);)/.test(html)) errors.push(`${file}: неэкранированный &`);
}

const navigation = read('scripts/navigation.js');
// Новый формат: группы, в каждой items — пары [id, название].
const navJson = JSON.parse(navigation.slice(navigation.indexOf('['), navigation.lastIndexOf(']') + 1));
const chapterIds = navJson.flatMap(group => group.items.map(([id]) => id));
if (chapterIds.length !== 32) errors.push(`navigation.js: ожидалось 32 страницы в оглавлении, найдено ${chapterIds.length}`);
for (const id of chapterIds) {
  if (!htmlFiles.includes(`${id}.html`)) errors.push(`navigation.js: нет страницы ${id}.html`);
}

let links = 0;
for (const file of htmlFiles) {
  const html = read(file);
  for (const match of html.matchAll(/href="([^"]+)"/g)) {
    const href = match[1];
    if (/^(https?:|mailto:|#)/.test(href)) continue;
    if (href.startsWith('styles/') || href.startsWith('scripts/')) continue;
    links++;
    const [target, anchor] = href.split('#');
    const targetFile = target || file;
    if (!htmlFiles.includes(targetFile)) {
      errors.push(`${file}: ссылка на отсутствующий файл ${href}`);
      continue;
    }
    if (anchor && !ids.get(targetFile).has(anchor)) {
      errors.push(`${file}: нет якоря ${href}`);
    }
  }
  for (const match of html.matchAll(/src="([^"]+)"/g)) {
    const src = match[1];
    if (!fs.existsSync(path.join(manual, src))) errors.push(`${file}: нет ресурса ${src}`);
  }
}

// Каждая карточка функции должна существовать и быть достижимой из указателя.
const registry = JSON.parse(read('data-inventory.json'));
const names = [...new Set(registry.Rows.map(r => r.Name))];
const index = read('scripts/search-index.js');
const entryCount = (index.match(/"title":/g) || []).length;
const expectedEntries = chapterIds.length + names.length + 69 + 15 + 99;
if (entryCount !== expectedEntries) errors.push(`search-index.js: ожидалось ${expectedEntries} записей, найдено ${entryCount}`);

const referenceHtml = read('reference.html');
for (const name of names) {
  const slug = name.toLowerCase();
  if (!referenceHtml.includes(`#fn-${slug}`)) errors.push(`reference.html: нет ссылки на ${name}`);
  const familyPages = chapterIds.filter(id => read(`${id}.html`).includes(`id="fn-${slug}"`));
  if (!familyPages.length) errors.push(`${name}: нет карточки функции`);
}

console.log(`Файлов: ${htmlFiles.length}; глав: ${chapterIds.length}; ссылок: ${links}; функций: ${names.length}; записей поиска: ${entryCount}.`);
if (errors.length) {
  console.log(errors.join('\n'));
  process.exitCode = 1;
} else {
  console.log('Структура, ссылки и якоря в порядке.');
}
