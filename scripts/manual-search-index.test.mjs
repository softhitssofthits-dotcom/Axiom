import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {mergeSearchDestinations} from './manual-search-index.mjs';

test('Одна статья находится по всем именам без повторения результата', () => {
  const entries=mergeSearchDestinations([
    {title:'Оператор INPUT',href:'input.html',kind:'Тема',description:'Получает значения пользователя.',keywords:['ввод']},
    {title:'INPUT',href:'input.html',kind:'Оператор',description:'Приглашение и параметры ввода.',keywords:['INPUT']},
    {title:'GET',href:'get-put.html',kind:'Оператор',description:'Сохраняет прямоугольник.',keywords:['GET']},
    {title:'PUT',href:'get-put.html',kind:'Оператор',description:'Рисует сохранённый прямоугольник.',keywords:['PUT']},
    {title:'VIEW PRINT',href:'view.html#view-print',kind:'Оператор',description:'Область вывода.',keywords:[]},
    {title:'VIEW',href:'view.html',kind:'Оператор',description:'Область рисования.',keywords:[]}
  ]);
  assert.equal(entries.length,4);
  assert.deepEqual(entries[0].aliases,['Оператор INPUT','INPUT']);
  assert.equal(entries[0].description,'Получает значения пользователя.');
  assert.ok(entries[0].keywords.includes('Приглашение и параметры ввода.'));
  assert.deepEqual(entries[1].aliases,['GET','PUT']);
  assert.ok(entries[1].keywords.includes('PUT'));
  assert.equal(entries[2].href,'view.html#view-print');
  assert.equal(entries[3].href,'view.html');
});


test('Руководство перехода массивов доступно в общем оглавлении и поиске',()=>{
 const context={window:{}};
 for(const file of ['chapters.js','search-index.js'])vm.runInNewContext(fs.readFileSync(new URL('../docs/manual/scripts/'+file,import.meta.url),'utf8'),context);
 const chapter=context.window.AxiomManualChapters.find(c=>c.number===10);
 assert.equal(chapter.items.filter(item=>item.href==='arrays-migration.html').length,1);
 const entries=context.window.AxiomSearchIndex.filter(item=>item.href==='arrays-migration.html');
 assert.equal(entries.length,1);
 assert.ok(entries[0].keywords.includes('квадратные скобки'));
});
