import test from 'node:test';import assert from 'node:assert/strict';import fs from 'node:fs';
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
 assert.ok(fn.includes('REGEXMATCHES(&lt;текст&gt;, &lt;шаблон&gt;[, &lt;режимы&gt;])'));
 assert.ok(!fn.includes('results ='));
});
