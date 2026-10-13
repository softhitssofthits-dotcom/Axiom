const { testWorkspace, writeAxm } = require('../helpers');
const { nativeCanvas } = require('../native-canvas');
module.exports = [{ id:'canvas-mouse-world-click', name:'Настоящий щелчок CANVAS передаёт кнопку и мировые координаты в ON MOUSE', async fn({S,C}) {
  const source = 'CANVAS 200, 100\nVIEWPORT (20, 10)-(180, 90)\nCAMERA 100, 50\nDIM handled AS BOOLEAN = FALSE\nON MOUSE(1), ClickMark\nBEGINFRAME\nCLS RGB(10, 20, 30)\nPRESENT\nDO WHILE NOT handled\nLOOP\nSUB ClickMark(button AS INTEGER, x AS LONG, y AS LONG)\nPRINT button\nPRINT x\nPRINT y\nPRINT MOUSEBUTTON(1)\nPRINT MOUSEX() = x\nPRINT MOUSEY() = y\nhandled = TRUE\nEND SUB\n';
  const file = writeAxm(testWorkspace('canvas-mouse'),'mouse.axm',source);
  await S.launch(); await S.page.evaluate(p=>window.__ide.openFile(p),file); await S.page.evaluate(()=>window.__ide.run());
  const page = await nativeCanvas(S);
  C.eq(page.width, 200, 'Нативный CANVAS сохраняет логическую ширину');
  page.pointer(0.3, 0.4);
  page.button('press');
  await S.waitIdle();
  const text=await S.consoleText();
  C.contains(text,'140','X учитывает начало VIEWPORT и CAMERA');
  C.contains(text,'80','Y учитывает начало VIEWPORT и CAMERA');
  C.contains(text,'TRUE','Состояние кнопки и чтение координат согласованы с обработчиком');
  page.button('release');
  C.ok(!(await S.diagnosticItems()).some(x=>x.text.includes('MB')), 'Щелчок не вызывает диагностику');
} }];
