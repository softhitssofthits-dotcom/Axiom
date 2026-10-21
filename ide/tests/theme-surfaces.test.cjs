const {test}=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const directory=path.join(__dirname,'../src/renderer');
for(const file of fs.readdirSync(directory).filter(file=>file.endsWith('.css')&&file!=='design-system.css'))test(file+': компоненты не выбирают тему по имени',()=>{
 const css=fs.readFileSync(path.join(__dirname,'../src/renderer',file),'utf8');
 assert.doesNotMatch(css,/\[(?:data-theme|data-chrome)[^\]]*\]/,'Выбор темы допустим только в центральной палитре');
});


test('Все обязательные CSS-переменные оболочки объявлены',()=>{
 const files=fs.readdirSync(directory).filter(file=>file.endsWith('.css'));
 const source=files.map(file=>fs.readFileSync(path.join(directory,file),'utf8')).join('\n');
 const defined=new Set([...source.matchAll(/(--[\w-]+)\s*:/g)].map(match=>match[1]));
 for(const match of source.matchAll(/var\((--[\w-]+)\s*\)/g))assert.ok(defined.has(match[1]),match[1]);
});
test('Палитры содержат только определения ролей без повторов',()=>{
 const css=fs.readFileSync(path.join(directory,'design-system.css'),'utf8').replace(/\/\*[\s\S]*?\*\//g,'');
 const blocks=[...css.matchAll(/(?:^|\n)(:root|[^{}]*\[data-theme[^{}]+)\s*\{([^{}]*)\}/g)];
 assert.equal(blocks.length,4);
 for(const [,selector,body] of blocks){
  const names=new Set();
  for(const declaration of body.split(';').map(value=>value.trim()).filter(Boolean)){
   const match=/^(--[\w-]+)\s*:/.exec(declaration);assert.ok(match,selector+': '+declaration);
   assert.ok(!names.has(match[1]),selector+': повтор '+match[1]);names.add(match[1]);
  }
 }
});
