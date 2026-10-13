import test from 'node:test';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { pathToFileURL, fileURLToPath } from 'node:url';
import path from 'node:path';
import fs from 'node:fs';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const require = createRequire(path.join(root, 'ide/e2e/package.json'));
const { chromium } = require('playwright');
const manual = path.join(root, 'docs/manual');
const url = name => pathToFileURL(path.join(manual, name)).href;

test('Подсветка в браузере сохраняет программу и использует цвета Axiom IDE', async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage();
    await page.goto(url('const.html'));
    const sample = page.locator('pre code.language-axiom').filter({ hasText: 'CONST LIMIT AS INTEGER = 10' }).last();
    assert.ok(await sample.count(), 'На странице должен быть запускаемый пример константы');
    assert.equal(await sample.locator('.axiom-token--keyword').filter({ hasText: /^CONST$/ }).first().evaluate(el => getComputedStyle(el).color), 'rgb(0, 0, 255)');
    assert.equal(await sample.locator('.axiom-token--type').filter({ hasText: /^INTEGER$/ }).first().evaluate(el => getComputedStyle(el).color), 'rgb(38, 127, 153)');
    assert.equal(await sample.locator('.axiom-token--number').filter({ hasText: /^10$/ }).first().evaluate(el => getComputedStyle(el).color), 'rgb(9, 134, 88)');
    assert.equal(await sample.locator('.axiom-token--variable-readonly').filter({ hasText: /^LIMIT$/ }).first().evaluate(el => getComputedStyle(el).color), 'rgb(143, 68, 0)');
    const source = await sample.textContent();
    await page.evaluate(() => AxiomManualHighlight.highlightAll?.());
    assert.equal(await sample.textContent(), source);
    assert.equal(await page.locator('pre code:not(.language-axiom) .axiom-token--keyword').count(), 0, 'Вывод программы должен остаться обычным текстом');
  } finally { await browser.close(); }
});

test('Локальный справочник: главы, поиск, клавиатура и узкий экран', async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(url('index.html'));
    assert.equal(await page.locator('.manual-nav__section[data-chapter]').count(), 17);
    const search = page.getByRole('searchbox', { name: 'Поиск по справочнику' });
    await search.fill('INPUT');
    await search.press('Enter');
    await page.waitForURL(/search\.html\?q=INPUT/);
    assert.equal(await page.locator('#manual-search-results a[href="input.html"]').count(), 1, 'Статья INPUT должна встречаться в поиске один раз');
    const destinations = await page.locator('#manual-search-results a').evaluateAll(links => links.map(link => link.getAttribute('href')));
    assert.equal(new Set(destinations).size, destinations.length, 'Поиск не должен повторять одну и ту же цель');
    await search.fill('LEN');
    await search.press('Enter');
    await page.waitForURL(/search\.html\?q=LEN/);
    assert.equal(await page.getByRole('searchbox').inputValue(), 'LEN');
    assert.equal(await page.locator('#manual-search-results a').first().getAttribute('href'), 'function-reference.html#fn-len');
    await page.locator('#manual-search-results a').first().click();
    await page.waitForURL(/function-reference\.html#fn-len/);
    assert.equal(await page.locator('.reference-details').count(), 0);
    assert.equal(await page.getByRole('searchbox').count(), 1);
    assert.ok((await page.locator('.topbar').boundingBox()).y >= 0);
    await page.keyboard.press('Alt+/');
    assert.equal(await page.getByRole('searchbox').evaluate(el => el === document.activeElement), true);
    await page.getByRole('searchbox').fill('дата время');
    await page.getByRole('searchbox').press('Enter');
    await page.waitForURL(/search\.html\?q=/);
    assert.ok(await page.locator('#manual-search-results a').count() > 0);
    await page.getByRole('searchbox').fill('несуществующая_сущность_987');
    await page.getByRole('searchbox').press('Enter');
    await page.waitForTimeout(100);
    assert.match(await page.locator('#manual-search-status').innerText(), /Ничего не найдено/);
    await page.getByRole('searchbox').fill('<img src=x onerror=alert(1)>');
    await page.getByRole('searchbox').press('Enter');
    await page.waitForTimeout(100);
    assert.equal(await page.locator('#manual-search-results img').count(), 0);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(url('getting-started.html'));
    const breadcrumbRows = await page.locator('.breadcrumbs > *').evaluateAll(elements => elements.map(element => Math.round(element.getBoundingClientRect().top)));
    assert.equal(new Set(breadcrumbRows).size, 1, 'Хлебные крошки должны располагаться на одной строке');
    const toggle = page.getByRole('button', { name: 'Оглавление' });
    await toggle.click();
    assert.equal(await toggle.getAttribute('aria-expanded'), 'true');
    await page.keyboard.press('Escape');
    assert.equal(await toggle.getAttribute('aria-expanded'), 'false');
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    for (const name of ['print.html', 'date-time-functions.html', 'modern-graphics.html', 'reference.html']) {
      await page.goto(url(name));
      assert.equal(await page.getByRole('searchbox', { name: 'Поиск по справочнику' }).count(), 1, name);
      await page.evaluate(() => scrollTo(0, 800));
      assert.equal(Math.round((await page.locator('.topbar').boundingBox()).y), 0, name);
    }
    await page.goto(url('data-types-strings-and-files.html') + '#fixed-string');
    assert.equal(await page.locator('#fixed-string').evaluate(element => element.nextElementSibling.textContent), 'Фиксированная ширина');
    const anchorTop = await page.locator('#fixed-string').evaluate(element => element.getBoundingClientRect().top);
    assert.ok(anchorTop >= 0 && anchorTop < 844, 'Прежняя ссылка должна открыть объяснение фиксированной строки');
    assert.deepEqual(errors, []);
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(url('index.html'));
    const evidence = path.join(root, '.artifacts/manual-rewrite');
    fs.mkdirSync(evidence, { recursive: true });
    await page.screenshot({ path: path.join(evidence, 'manual-home.png'), fullPage: true });
    await page.goto(url('search.html') + '?q=LEN');
    await page.screenshot({ path: path.join(evidence, 'manual-search.png') });
  } finally {
    await browser.close();
  }
});

test('Все статьи открываются локально и помещаются на узком экране', async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 390, height: 844 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const pages = fs.readdirSync(manual).filter(name => name.endsWith('.html'));
    for (const name of pages) {
      await page.goto(url(name));
      assert.equal(await page.locator('h1').count(), 1, name);
      assert.equal(await page.getByRole('searchbox', { name: 'Поиск по справочнику' }).count(), 1, name);
      const dimensions = await page.evaluate(() => ({ width: innerWidth, content: document.documentElement.scrollWidth }));
      assert.ok(dimensions.content <= dimensions.width, `${name}: ширина ${dimensions.content} > ${dimensions.width}`);
      const rows = await page.locator('.breadcrumbs > *').evaluateAll(elements => elements.map(element => Math.round(element.getBoundingClientRect().top)));
      assert.equal(new Set(rows).size, 1, `${name}: хлебные крошки должны занимать одну строку`);
    }
    assert.deepEqual(errors, []);
    await page.goto(url('arrays-2d.html'));
    await page.screenshot({ path: path.join(root, '.artifacts/manual-rewrite/manual-mobile.png'), fullPage: true });
    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(url('date-time-functions.html'));
    await page.screenshot({ path: path.join(root, '.artifacts/manual-rewrite/manual-dates.png'), fullPage: true });
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto(url('getting-started.html'));
    await page.screenshot({ path: path.join(root, '.artifacts/manual-rewrite/manual-start.png') });
  } finally {
    await browser.close();
  }
});
