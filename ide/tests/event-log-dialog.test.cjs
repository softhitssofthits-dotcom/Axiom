const { test } = require('node:test');
const assert = require('node:assert/strict');
const { EventLogDialog } = require('../src/renderer/event-log-dialog');
function setup() {
  const pending = [], rendered = [], errors = [], busy = [];
  const dialog = new EventLogDialog({}, { readEventLog: () => new Promise((resolve, reject) => pending.push({ resolve, reject })) });
  Object.assign(dialog, { overlay: {}, filters: () => ({}), render: result => rendered.push(result),
    showError: error => errors.push(error), setBusy: value => busy.push(value) });
  return { dialog, pending, rendered, errors, busy };
}
test('Поздний результат фильтра не заменяет новый просмотр журнала', async () => {
  const { dialog, pending, rendered } = setup();
  const first = dialog.refresh(), second = dialog.refresh();
  pending[1].resolve({ ok: true, entries: ['new'] }); await second;
  pending[0].resolve({ ok: true, entries: ['old'] }); await first;
  assert.deepEqual(rendered.map(result => result.entries), [['new']]);
});
test('Поздний отказ и закрытие не меняют новый просмотр', async () => {
  for (const mode of ['newer', 'close']) {
    const { dialog, pending, errors } = setup(); const first = dialog.refresh();
    if (mode === 'newer') { const next = dialog.refresh(); pending[1].resolve({ ok: true }); await next; }
    else dialog.overlay = null;
    pending[0].reject(new Error('Старый отказ')); await first;
    assert.deepEqual(errors, []);
  }
});
test('Текущий отказ показан и не блокирует повтор', async () => {
  const { dialog, pending, errors, busy, rendered } = setup();
  let operation = dialog.refresh(); pending[0].resolve({ ok: false, error: 'Нет доступа' }); await operation;
  assert.equal(errors.length, 1); assert.match(errors[0], /Нет доступа/); assert.equal(busy.at(-1), false);
  operation = dialog.refresh(); pending[1].resolve({ ok: true, entries: [] }); await operation;
  assert.equal(rendered.length, 1);
});
