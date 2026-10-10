const { testWorkspace, writeAxm } = require('../helpers');
module.exports = [{ id:'canvas-mouse-world-click', name:'Настоящий щелчок CANVAS передаёт кнопку и мировые координаты в ON MOUSE', async fn({S,C}) {
  const source = 'CANVAS 200, 100\nVIEWPORT (20, 10)-(180, 90)\nCAMERA 100, 50\nDIM handled AS BOOLEAN = FALSE\nON MOUSE(1), ClickMark\nBEGINFRAME\nCLS RGB(10, 20, 30)\nPRESENT\nDO WHILE NOT handled\nLOOP\nSUB ClickMark(button AS INTEGER, x AS LONG, y AS LONG)\nPRINT button\nPRINT x\nPRINT y\nPRINT MOUSEBUTTON(1)\nPRINT MOUSEX() = x\nPRINT MOUSEY() = y\nhandled = TRUE\nEND SUB\n';
  const file = writeAxm(testWorkspace('canvas-mouse'),'mouse.axm',source);
  await S.launch(); await S.page.evaluate(p=>window.__ide.openFile(p),file); await S.page.evaluate(()=>window.__ide.run());
  let page;
  for(let i=0;i<100;i++){page=(await S.electronApp.windows()).find(p=>p.url().endsWith('/canvas.html'));if(page)break;await S.page.waitForTimeout(50);}
  if(!page)throw new Error('Нет окна CANVAS');
  await page.waitForFunction(()=>document.getElementById('program-canvas').width===200);
  const bounds=await page.locator('#program-canvas').boundingBox();
  await page.mouse.move(bounds.x+bounds.width*0.3,bounds.y+bounds.height*0.4);
  await page.mouse.down();
  await S.waitIdle();
  const text=await S.consoleText();
  C.contains(text,'140','X учитывает начало VIEWPORT и CAMERA');
  C.contains(text,'80','Y учитывает начало VIEWPORT и CAMERA');
  C.contains(text,'TRUE','Состояние кнопки и чтение координат согласованы с обработчиком');
  await page.mouse.up();
  C.ok(!(await S.diagnosticItems()).some(x=>x.text.includes('MB')), 'Щелчок не вызывает диагностику');
} }];
