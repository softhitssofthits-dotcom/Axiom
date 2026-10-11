const { testWorkspace, writeAxm } = require('../helpers');
module.exports = [{
 id: 'interpolation-print-typo-location', name: 'Опечатка PRIN перед интерполяцией: русская ошибка на строке 9',
 async fn({ S, C }) {
  const source = 'DIM name AS STRING, age AS INTEGER\nDIM BirthDay AS INTEGER\n\nINPUT "Введите ваше имя: ", name\n\nINPUT "Введите ваш возраст"; age\n\nBirthDay = 2026 - age\nPRIN $"привет {name}, ты родился в {BirthDay} году."\n';
  const file = writeAxm(testWorkspace('interpolation-typo'), 'name-age-typo.axm', source);
  await S.launch({ settings: { language: 'ru', checkOnChange: true } });
  await S.page.evaluate(async p => { await window.__ide.openFile(p); await window.__ide.runLiveCheck(); }, file);
  const markers = await S.markers();
  C.ok(markers.length > 0, 'Опечатка имеет обычную диагностику');
  C.ok(markers.every(m => m.line === 9), 'Все маркеры относятся к строке 9', JSON.stringify(markers));
  C.ok(markers.every(m => !m.message.includes('IDE0001')), 'Нет внутреннего сбоя');
  C.ok(markers.every(m => /[А-Яа-яЁё]/.test(m.message)), 'Описания ошибок русские');
  await S.page.evaluate(() => window.__ide.run('debug'));
  await S.waitIdle();
  const diagnostics = await S.page.evaluate(() => window.__ide._lastDiagnostics);
  C.ok(diagnostics.every(d => d.code !== 'IDE0001' && d.line === 9), 'Запуск трассировки сохраняет строку обычной ошибки');
  await S.screenshot('interpolation-print-typo-fixed');
  await S.page.evaluate(() => {
   const i = window.__ide;
   const marker = i.monaco.editor.getModelMarkers({ owner: 'axiom', resource: i.editor.getModel().uri })[0];
   i.showErrorDetailsForMarker(marker);
  });
  await S.page.locator('.marker-widget').waitFor({ state: 'visible' });
  C.ok(true, 'Карточка ошибки открыта перед исправлением');
  await S.page.evaluate(() => { const i = window.__ide; i.editor.setPosition({ lineNumber: 9, column: 5 }); i.editor.focus(); });
  await S.page.keyboard.type('T');
  await S.page.keyboard.press('ArrowRight');
  C.eq(await S.page.locator('.marker-widget:visible').count(), 0, 'Карточка сразу закрывается при исправлении текста');
  await S.page.waitForFunction(() => {
   const i = window.__ide;
   return i._lastDiagnostics.length === 0 && i.monaco.editor.getModelMarkers({ owner: 'axiom', resource: i.editor.getModel().uri }).length === 0;
  });
  C.eq((await S.markers()).filter(m => m.severity === 8).length, 0, 'После исправления PRINT ошибок нет');
  C.eq(await S.page.locator('.marker-widget:visible').count(), 0, 'Автоматическая проверка не оставляет старую карточку');
  await S.screenshot('diagnostic-peek-dismissed');
 }
}];
