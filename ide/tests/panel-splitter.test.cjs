const {test}=require('node:test');const assert=require('node:assert/strict');const {bounds,clamp}=require('../src/renderer/panel-splitter');
test('Границы разделителя оставляют место редактору даже в маленьком окне',()=>{
 assert.deepEqual(bounds(900,160,160,9,480),{min:160,max:480});assert.deepEqual(bounds(200,160,160,9,480),{min:31,max:31});assert.deepEqual(bounds(100,64,140,9),{min:0,max:0});
 assert.equal(clamp(900,bounds(400,64,140,9)),251);assert.equal(clamp(-10,bounds(400,64,140,9)),64);assert.equal(clamp(NaN,bounds(400,64,140,9)),64);
});
