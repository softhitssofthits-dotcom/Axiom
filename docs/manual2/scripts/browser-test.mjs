import {createRequire} from 'node:module';
import {pathToFileURL} from 'node:url';
import path from 'node:path';
import fs from 'node:fs';
import {fileURLToPath} from 'node:url';

const manual = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const root = path.resolve(manual, '..', '..');
const require = createRequire(path.join(root, 'ide/e2e/package.json'));
const {chromium} = require('playwright');
const url = name => pathToFileURL(path.join(manual, name)).href;
const shots = path.join(root, '.artifacts/manual2-shots');
fs.mkdirSync(shots, {recursive: true});

const results = [];
const ok = (name, condition, detail = '') => {
  results.push({name, pass: Boolean(condition), detail});
  console.log(`${condition ? 'PASS' : 'FAIL'} ${name}${condition ? '' : ': ' + detail}`);
};

const browser = await chromium.launch({channel: 'msedge', headless: true});
try {
  const page = await browser.newPage({viewport: {width: 1440, height: 900}});
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error') errors.push(message.text());
  });

  await page.goto(url('index.html'));
  ok('главная открывается', await page.locator('h1').count() === 1);
  ok('оглавление содержит все разделы', await page.locator('#nav a').count() >= 32,
    String(await page.locator('#nav a').count()));
  ok('группы разделов видимы', await page.locator('.home-grid .group').count() === 9,
    String(await page.locator('.home-grid .group').count()));

  await page.getByRole('searchbox').fill('ARRAYSLICE');
  await page.getByRole('searchbox').press('Enter');
  await page.waitForURL(/search\.html\?q=ARRAYSLICE/);
  const first = await page.locator('#results .result').first().getAttribute('href');
  ok('поиск находит ARRAYSLICE первым', first === 'arrays-functions.html#fn-arrayslice', String(first));
  ok('запрос восстановлен в поле', (await page.getByRole('searchbox').inputValue()) === 'ARRAYSLICE');
  ok('подсветка совпадения', await page.locator('#results mark').count() > 0);

  await page.getByRole('searchbox').fill('PRINT USING');
  await page.getByRole('searchbox').press('Enter');
  await page.waitForURL(/search\.html\?q=PRINT/);
  ok('поиск по оператору', await page.locator('#results .result').count() > 0);

  await page.getByRole('searchbox').fill('несуществующее_987654');
  await page.getByRole('searchbox').press('Enter');
  await page.waitForTimeout(120);
  ok('пустой результат сообщает об этом', /Ничего не найдено/.test(await page.locator('#status').innerText()));

  await page.getByRole('searchbox').fill('<img src=x onerror=alert(1)>');
  await page.getByRole('searchbox').press('Enter');
  await page.waitForTimeout(120);
  ok('разметка из запроса не исполняется', await page.locator('#results img').count() === 0);

  await page.goto(url('arrays-functions.html'));
  ok('карточка функции найдена', await page.locator('#fn-arrayslice').count() === 1);
  ok('имена функций подсвечены', await page.locator('.tok-fn').count() > 0,
    String(await page.locator('.tok-fn').count()));
  ok('текст программы не потерян', (await page.locator('pre code.language-axiom').first().textContent()).includes('ARRAY'));

  await page.goto(url('branching.html'));
  ok('ключевые слова подсвечены', await page.locator('pre code.language-axiom .tok-kw').count() > 0,
    String(await page.locator('pre code.language-axiom .tok-kw').count()));
  ok('строки подсвечены', await page.locator('.tok-str').count() > 0);
  ok('числа подсвечены', await page.locator('.tok-num').count() > 0);
  ok('подсветка не съела символы', (await page.locator('pre code.language-axiom').first().textContent()).includes('score >= 90'));

  await page.goto(url('types.html'));
  ok('типы данных подсвечены', await page.locator('.tok-type').count() > 0,
    String(await page.locator('.tok-type').count()));

  await page.goto(url('branching.html'));
  ok('страница ветвления открывается', await page.locator('h1').innerText() === 'Ветвление: IF и SELECT CASE');
  await page.goto(url('loops.html'));
  ok('страница циклов открывается', await page.locator('h1').innerText() === 'Циклы: FOR, WHILE, DO');

  await page.setViewportSize({width: 390, height: 844});
  await page.goto(url('intro.html'));
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  ok('нет горизонтального переполнения на 390', overflow <= 0, String(overflow));
  const menu = page.getByRole('button', {name: 'Оглавление'});
  ok('кнопка оглавления видна', await menu.isVisible());
  await menu.click();
  ok('оглавление раскрывается', await page.locator('#side').evaluate(el => el.classList.contains('open')));
  await page.keyboard.press('Escape');
  ok('Escape закрывает оглавление', !(await page.locator('#side').evaluate(el => el.classList.contains('open'))));
  await page.screenshot({path: path.join(shots, 'manual2-mobile-intro.png'), fullPage: false});

  await page.setViewportSize({width: 1440, height: 900});
  for (const name of ['index.html', 'strings.html', 'reference.html', 'diagnostics.html']) {
    await page.goto(url(name));
    const over = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    ok(`нет переполнения: ${name}`, over <= 0, String(over));
  }
  await page.goto(url('index.html'));
  await page.screenshot({path: path.join(shots, 'manual2-home.png'), fullPage: true});
  await page.goto(url('branching.html'));
  await page.screenshot({path: path.join(shots, 'manual2-branching.png'), fullPage: false});

  ok('нет ошибок страницы', errors.length === 0, errors.join(' | '));
} finally {
  await browser.close();
}

fs.writeFileSync(path.join(shots, 'results.json'), JSON.stringify(results, null, 2) + '\n');
const failed = results.filter(r => !r.pass);
console.log(`${results.length - failed.length}/${results.length}`);
if (failed.length) process.exitCode = 1;
