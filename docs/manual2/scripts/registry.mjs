import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const manual = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const inventory = JSON.parse(fs.readFileSync(path.join(manual, 'data-inventory.json'), 'utf8'));

const rows = inventory.Rows;
const byName = new Map();
for (const row of rows) {
  if (!byName.has(row.Name)) byName.set(row.Name, []);
  byName.get(row.Name).push(row);
}
const names = [...byName.keys()].sort((a, b) => a.localeCompare(b, 'en'));

// --- Помощники разметки: код экранируется один раз, в одном месте. ---
export const escapeHtml = value => String(value)
  .replaceAll('&', '&amp;')
  .replaceAll('<', '&lt;')
  .replaceAll('>', '&gt;')
  .replaceAll('"', '&quot;');

// Программа Axiom (подсвечивается).
export const axm = source => `<pre><code class="language-axiom">${escapeHtml(source.trim())}</code></pre>`;
// Ожидаемый вывод (не подсвечивается).
export const out = source => `<pre><code class="language-output">${escapeHtml(source.trim())}</code></pre>`;
// Встроенный код (для прозы).
export const c = value => `<code>${escapeHtml(value)}</code>`;

// --- Группы функций по файлу регистрации в исходниках. ---
const families = [
  {id: 'numbers', title: 'Числа',
    keys: ['Numeric', 'BuiltinNumericHelpers', 'BuiltinArrayAggregateHelpers', 'Conversion', 'cs']},
  {id: 'strings', title: 'Строки',
    keys: ['String', 'String.Basic', 'String.Char', 'String.Edit', 'String.Trim', 'String.Search', 'String.Fill', 'String.Array', 'BuiltinStringHelpers', 'BuiltinDirectoryHelpers', 'BuiltinRegexHelpers', 'BuiltinIOHelpers', 'BuiltinPrintUsingHelpers']},
  {id: 'arrays', title: 'Массивы', keys: ['Array']},
  {id: 'dates', title: 'Дата и время', keys: ['DateTime', 'BuiltinDateTimeHelpers']},
  {id: 'files', title: 'Файлы',
    keys: ['BuiltinFileIOHelpers', 'FileText', 'FileOperations', 'Paths', 'Directories', 'FileMetadata', 'Storage']},
  {id: 'regex', title: 'Регулярные выражения', keys: ['Regex']},
  {id: 'graphics', title: 'Графика', keys: ['Input', 'PrintUsingNumberFormatter']},
  {id: 'tilemaps', title: 'Тайловые карты', keys: ['BuiltinTilemapMetadataHelpers']},
];

const familyOf = new Map();
const builtinDir = path.join(manual, '..', '..', 'src/Axiom.Semantics/Symbols/Builtins');
const registration = new Map();
for (const file of fs.readdirSync(builtinDir).filter(name => name.endsWith('.cs'))) {
  const text = fs.readFileSync(path.join(builtinDir, file), 'utf8');
  for (const name of names) {
    if (registration.has(name)) continue;
    if (new RegExp(`"${name}"`).test(text)) registration.set(name, file.replace(/^BuiltinFunctions\./, '').replace(/\.cs$/, ''));
  }
}
const byRegistration = {
  Numeric: 'numbers', Conversion: 'numbers', String: 'strings',
  'String.Basic': 'strings', 'String.Char': 'strings', 'String.Edit': 'strings',
  'String.Trim': 'strings', 'String.Search': 'strings', 'String.Fill': 'strings',
  'String.Array': 'strings', Array: 'arrays', DateTime: 'dates', FileIO: 'files',
  FileSystem: 'files', Regex: 'regex', Input: 'graphics',
};
for (const name of names) {
  const file = registration.get(name);
  const family = file ? byRegistration[file.replace(/^FileSystem\./, '')] : undefined;
  if (family) familyOf.set(name, family);
}
const unassigned = names.filter(name => !familyOf.has(name));
if (unassigned.length) throw new Error(`Не отнесены к главам: ${unassigned.join(', ')}`);

// --- Оглавление: точные разделения по смыслу. ---
// Каждый блок — самостоятельная страница; операторы ветвления отдельно от циклов,
// пользовательские типы отдельно от процедур.
export const navGroups = [
  {title: 'Основы', items: [
    ['intro', 'Введение'],
    ['variables', 'Переменные'],
    ['constants', 'Константы'],
    ['types', 'Типы данных'],
  ]},
  {title: 'Ветвление и циклы', items: [
    ['branching', 'Ветвление: IF и SELECT CASE'],
    ['loops', 'Циклы: FOR, WHILE, DO'],
  ]},
  {title: 'Операции и данные', items: [
    ['operators', 'Операции и выражения'],
    ['numbers', 'Числа'],
    ['strings', 'Строки'],
    ['dates', 'Дата и время'],
    ['arrays', 'Массивы'],
  ]},
  {title: 'Структура программы', items: [
    ['records', 'Пользовательские типы TYPE'],
    ['sub', 'Процедуры SUB'],
    ['function', 'Функции FUNCTION'],
  ]},
  {title: 'Ввод и вывод', items: [
    ['print', 'PRINT и PRINT USING'],
    ['input', 'INPUT'],
  ]},
  {title: 'Файлы и текст', items: [
    ['files', 'Файлы'],
    ['regex', 'Регулярные выражения'],
  ]},
  {title: 'Графика и события', items: [
    ['graphics', 'Экран и графика'],
    ['events', 'Клавиатура и мышь'],
  ]},
  {title: 'Ошибки и модули', items: [
    ['errors', 'Обработка ошибок'],
    ['modules', 'Модули и IMPORT'],
  ]},
  {title: 'Справочные материалы', items: [
    ['numbers-functions', 'Функции: числа'],
    ['strings-functions', 'Функции: строки'],
    ['arrays-functions', 'Функции: массивы'],
    ['dates-functions', 'Функции: дата и время'],
    ['files-functions', 'Функции: файлы'],
    ['regex-functions', 'Функции: RegEx'],
    ['graphics-functions', 'Функции: графика'],
    ['tilemaps-functions', 'Функции: тайловые карты'],
    ['diagnostics', 'Диагностика'],
    ['reference', 'Указатели'],
  ]},
];

export const nav = navGroups.flatMap(group => group.items.map(([id, title]) => ({id, title})));
export const navTitles = new Map(nav.map(n => [n.id, n.title]));

const formal = value => value.replaceAll('_', ' ');
export const signature = row => `${row.Name}(${row.Parameters.map(p => formal(p.Name).toUpperCase()).join(', ')})`;
export const resultText = row => {
  if (row.ResultPolicy === 'Declared') return row.ReturnType;
  return {
    SameAsFirstArgument: 'тот же тип, что у первого аргумента',
    SameAsSecondArgument: 'форма и тип элементов второго аргумента',
    ElementTypeOfFirstArrayArgument: 'тип элемента массива',
    ArrayOfFirstArrayElementType: 'массив с типом элемента исходного массива',
    ArrayDynamicSliceResult: 'элемент или массив меньшего ранга',
    ArrayIndexCoordinateResult: 'индекс или массив координат',
    ArrayFindAllCoordinateResult: 'массив координат найденных элементов',
    ArrayMapSelectorReturnArray: 'массив результатов отображающей функции',
    ArrayReduceAccumulator: 'тип накопленного значения',
    PromotedNumeric: 'общий числовой тип аргументов',
    Integer: 'INTEGER',
    Double: 'DOUBLE',
    String: 'STRING',
  }[row.ResultPolicy] ?? row.ReturnType;
};

export {manual, inventory, rows, byName, names, families, familyOf};
