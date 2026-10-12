const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { portableRuntime } = require('../src/main/portable-runtime');
const { resolveHostPath, ensureFreshHost } = require('../src/main/host-ensure');

test('Переносимая поставка выбирает собственный Host без исходников и пересборки', context => {
  const base = path.resolve('.artifacts/portable-runtime-tests'); fs.mkdirSync(base,{recursive:true});
  const root = fs.mkdtempSync(path.join(base,'layout-'));
  context.after(() => fs.rmSync(root,{recursive:true,force:true}));
  assert.equal(portableRuntime(root),null);
  fs.writeFileSync(path.join(root,'portable-runtime.json'),JSON.stringify({version:1,sourceFingerprint:'a'.repeat(64)}));
  const layout = portableRuntime(root);
  assert.equal(layout.cli,path.join(root,'runtime/cli/axiom.exe'));
  assert.equal(resolveHostPath({},root).exe,layout.host);
  let builds = 0;
  assert.equal(ensureFreshHost(root,{env:{},build:() => { builds++; }}).rebuilt,false);
  assert.equal(builds,0);
});
test('Повреждённый состав поставки не заменяется случайной сборкой', context => {
  const base = path.resolve('.artifacts/portable-runtime-tests'); fs.mkdirSync(base,{recursive:true});
  const root = fs.mkdtempSync(path.join(base,'invalid-'));
  context.after(() => fs.rmSync(root,{recursive:true,force:true}));
  fs.writeFileSync(path.join(root,'portable-runtime.json'),JSON.stringify({version:2,sourceFingerprint:'invalid'}));
  assert.throws(() => portableRuntime(root),/Повреждён/);
});
