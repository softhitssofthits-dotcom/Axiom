import test from 'node:test';
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const { expectedCancellation, configurationOnly, acceptedSetting, summarize } = require('../ide/e2e/acceptance-results.cjs');
const { verify } = require('../ide/e2e/editor-settings-live-assertions.cjs');

function configuration(key, values) {
  return { key, status: 'verified-configuration', limitation: 'Физический эффект не подтверждён.', errors: [],
    cases: values.map(value => ({ value, diagnostic: { parent: value }, observed: { urls: [], trace: [0, 0] } })) };
}

test('Отмена Monaco определяется по точному типу и источнику', () => {
  assert.equal(expectedCancellation('Canceled: Canceled\n    at file:///editorWorkerHost-abc.js:1'), true);
  assert.equal(expectedCancellation('Canceled: Canceled\n    at file:///app.js:1'), false);
  assert.equal(expectedCancellation('TypeError: failure\n    at editorWorkerHost-abc.js:1'), false);
});

test('Проверка конфигурации не подменяет наблюдение физической инерции', () => {
  const row = configuration('inertialScroll', [false, true]);
  assert.equal(configurationOnly(row), true);
  assert.equal(acceptedSetting(row), true);
  assert.deepEqual(verify(row), []);
  row.cases[1].diagnostic.parent = false;
  assert.equal(configurationOnly(row), false);
  assert.equal(acceptedSetting(row), false);
  assert.ok(verify(row).length > 0);
});

test('Все три действия средней кнопки проверяются без разрешения popup', () => {
  const row = configuration('mouseMiddleClickAction', ['default', 'openLink', 'ctrlLeftClick']);
  assert.equal(acceptedSetting(row), true);
  assert.deepEqual(verify(row), []);
  row.cases[1].observed.urls.push('http://127.0.0.1/');
  assert.equal(acceptedSetting(row), false);
  assert.ok(verify(row).length > 0);
});

test('Произвольный непроверенный случай нельзя объявить успешным', () => {
  const row = configuration('anotherSetting', [false, true]);
  assert.equal(acceptedSetting(row), false);
  const inertia = configuration('inertialScroll', [false, true]);
  delete inertia.limitation;
  assert.equal(acceptedSetting(inertia), false);
  inertia.limitation = 'Не проверено'; inertia.errors.push('Ошибка исполнения');
  assert.equal(acceptedSetting(inertia), false);
});

test('Итог учитывает ошибки страницы и сохраняет внешние ограничения', () => {
  const results = [
    { id: 'clean', passed: 2, failures: [], crashed: false, pageErrors: [] },
    { id: 'limited', passed: 1, failures: [], crashed: false, pageErrors: [], limitations: [{ scope: 'automatic-foreground', status: 'not-verified' }] },
    { id: 'error', passed: 1, failures: [], crashed: false, pageErrors: ['TypeError: boom'] },
  ];
  const summary = summarize(results);
  assert.equal(summary.executed, 3); assert.equal(summary.failed, 1); assert.equal(summary.passedChecks, 4);
  assert.deepEqual(summary.limitations, [{ id: 'limited', scope: 'automatic-foreground', status: 'not-verified' }]);
});
