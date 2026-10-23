import test from 'node:test';import assert from 'node:assert/strict';import fs from 'node:fs';import vm from 'node:vm';
test('Формальная запись отличается от программы-примера', async()=>{
 assert.ok(fs.existsSync(new URL('./normalize-manual-syntax.mjs',import.meta.url)),'Требуется единое оформление синтаксиса');
 const {normalizeManualSyntax}=await import('./normalize-manual-syntax.mjs');
 const dictionary={keywords:['GET','PUT','STEP','PRINT'],types:['STRING'],builtins:['REGEXMATCHES']};
 const html='<h2>Синтаксис</h2><pre><code class="language-axiom">GET (x1, y1)-(x2, y2), buffer\nPUT (x, y), buffer</code></pre><h2>Пример</h2><pre><code class="language-axiom">PUT (x, y), buffer</code></pre>';
 const result=normalizeManualSyntax(html,dictionary);
 assert.ok(result.includes('GET (&lt;x1&gt;, &lt;y1&gt;)-(&lt;x2&gt;, &lt;y2&gt;), &lt;буфер&gt;'));
 assert.ok(result.endsWith('<h2>Пример</h2><pre><code class="language-axiom">PUT (x, y), buffer</code></pre>'));
 assert.equal(normalizeManualSyntax(result,dictionary),result);
 const fn=normalizeManualSyntax('<h3>Синтаксис</h3><pre><code class="language-axiom">results = REGEXMATCHES(text, pattern[, modes])</code></pre>',dictionary);
 assert.ok(fn.includes('REGEXMATCHES(&lt;текст&gt;, &lt;шаблон&gt;⟦, &lt;режимы&gt;⟧)'));
 assert.ok(!fn.includes('results ='));
});


test('Необязательная метанотация не подменяет квадратные массивы', async()=>{
 const {normalizeManualSyntax,normalizeOptionalNotation}=await import('./normalize-manual-syntax.mjs');
 const dictionary={keywords:['DIM','AS','FUNCTION','PRINT','FOR','TO','STEP'],types:['STRING','INTEGER'],builtins:[]};
 const source='<h2>Синтаксис</h2><pre><code class="language-axiom">DIM &lt;имя&gt;[] AS STRING</code></pre><pre><code class="language-axiom">DIM &lt;таблица&gt;[,] AS INTEGER</code></pre><pre><code class="language-axiom">&lt;массив&gt;[&lt;индекс&gt;]</code></pre><pre><code class="language-axiom">FUNCTION &lt;имя&gt;([&lt;параметры&gt;]) AS STRING[]</code></pre>';
 const result=normalizeManualSyntax(source,dictionary);
 assert.ok(result.includes('&lt;имя&gt;[] AS STRING'));
 assert.ok(result.includes('&lt;таблица&gt;[,] AS INTEGER'));
 assert.ok(result.includes('&lt;массив&gt;[&lt;индекс&gt;]'));
 assert.ok(result.includes('FUNCTION &lt;имя&gt;(⟦&lt;параметры&gt;⟧) AS STRING[]'));
 assert.equal(normalizeManualSyntax(result,dictionary),result);
 assert.equal(normalizeOptionalNotation('values[Choose()] + grid[1, 2]'), 'values[Choose()] + grid[1, 2]');
 assert.equal(normalizeOptionalNotation('FOR <i> = 1 TO 3 [STEP <step>]'), 'FOR <i> = 1 TO 3 ⟦STEP <step>⟧');
 assert.equal(normalizeOptionalNotation('PRINT "[STEP x]"'), 'PRINT "[STEP x]"');
 assert.equal(normalizeOptionalNotation('Type(<field> := <value>[, ...])'), 'Type(<field> := <value>⟦, ...⟧)');
 assert.equal(normalizeOptionalNotation('[;|,]'), '⟦;|,⟧');
});

test('Положительные формы DIM и руководство перехода не смешивают два синтаксиса',()=>{
 const dim=fs.readFileSync(new URL('../docs/manual/dim.html',import.meta.url),'utf8');
 assert.match(dim,/DIM &lt;имя&gt;\[\] AS &lt;тип элемента&gt; =/);
 assert.match(dim,/DIM &lt;имя&gt;\[\] = &lt;выражение массива&gt;/);
 assert.doesNotMatch(dim,/DIM &lt;имя&gt; AS &lt;тип элемента&gt; \(\)/);
 assert.doesNotMatch(dim,/DIM &lt;имя&gt; = &lt;выражение массива&gt;/);
 const migration=fs.readFileSync(new URL('../docs/manual/arrays-migration.html',import.meta.url),'utf8');
 for(const form of ['DIM basket[] = fruits[]','MakeFruits()[2]','values[] AS STRING','AS STRING[]','AS INTEGER[,]','ERASE bag.Items[]']) assert.ok(migration.includes(form),form);
});

test('Обращения в справочных программах квадратные, скобки функций и координат сохранены',()=>{
 const pages={
  'arrays.html':['DIM scores[1 TO 3] AS INTEGER','LBOUND(scores[])','ARRAYCLONE(a[])'],
  'arrays-functions.html':['ARRAYPUSH(board[], rowValue[], ROW)','rowValue[] = ARRAYPOP(board[], ROW)'],
  'arrays-slices.html':['rowPart[] = board[2]','rowPart[3] = 99'],
  'get-put.html':['PSET (100, 50), 6','GET (100, 50)-(101, 51), buffer[]'],
  'string-functions-build-and-split.html':['parts[]=SPLIT("one,two")','JOIN(items[], "|")'],
  'regex-functions-arrays-and-split.html':['values[] = REGEXEXTRACTALL("A12 B34", "([A-Z])(\\d+)", 2)'],
 };
 for(const [file,forms] of Object.entries(pages)){
  const text=fs.readFileSync(new URL('../docs/manual/'+file,import.meta.url),'utf8');
  for(const form of forms)assert.ok(text.includes(form),file+': '+form);
 }
});

test('Контрольная сумма реестра включает отдельный разбор массивов',()=>{
 const script=fs.readFileSync(new URL('./build-language-reference.mjs',import.meta.url),'utf8');
 assert.ok(script.includes("'src/Axiom.Parsing/Parser.Arrays.cs'"));
 const source=script.match(/function arrayTypeNotation\(type\)\{[\s\S]*?\n\}/)?.[0];
 assert.ok(source);
 const context={};vm.runInNewContext(source,context);
 for(const [input,expected] of [['STRING()','STRING[]'],['INTEGER(,)','INTEGER[,]'],['STRING[]','STRING[]'],['FUNCTION() AS STRING()','FUNCTION() AS STRING[]'],['FUNCTION(INTEGER()) AS INTEGER','FUNCTION(INTEGER[]) AS INTEGER']])assert.equal(context.arrayTypeNotation(input),expected);
});
