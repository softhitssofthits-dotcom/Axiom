const fs = require('node:fs');
const path = require('node:path');
const { testWorkspace, writeAxm } = require('../helpers');
const { probe } = require('../native-canvas');
const OUT = path.resolve(__dirname, '../../../.artifacts/graphics-repair/native');
fs.mkdirSync(OUT, { recursive: true });
function pixel(frame, x, y, w=320, h=200) {
  const data=Buffer.from(frame.bgra,'base64');
  const s=Math.min(frame.width/w,frame.height/h);
  const fw=Math.floor(w*s),fh=Math.floor(h*s);
  const px=Math.floor((frame.width-fw)/2)+Math.floor((x+.5)*fw/w);
  const py=Math.floor((frame.height-fh)/2)+Math.floor((y+.5)*fh/h);
  const i=(py*frame.width+px)*4;
  return [data[i+2],data[i+1],data[i]].join(',');
}
function observe(name,data) { fs.appendFileSync(path.join(OUT,'native-observations.jsonl'),JSON.stringify({name,data})+'\n'); }
module.exports = [
  {id:'graphics-repair-all-screen-modes',name:'Проверка пикселей всех графических режимов SCREEN 1..10',async fn({S,C}) {
    await S.launch();const dir=testWorkspace('graphics-repair-screen-modes');
    const file=writeAxm(dir,'modes.axm','DIM answer AS STRING\nFOR mode=1 TO 10\nSCREEN mode\nLINE (10,60)-(30,80),12,BF\nINPUT answer\nNEXT\nSLEEP\n');
    await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
    let previous=null;
    for(let mode=1;mode<=10;mode++) {
      await S.page.waitForFunction(old=>window.__ide.execution.inputRequestId!==null&&window.__ide.execution.inputRequestId!==old&&window.__ide.execution.surface?.windowVisible===true,previous);
      const current=await S.page.evaluate(()=>({id:window.__ide.execution.inputRequestId,surface:window.__ide.execution.surface}));previous=current.id;
      const frame=probe(current.surface.windowHandle,'capture',{OutputPath:path.join(OUT,`screen-mode-${mode}.png`)});
      const color=pixel(frame,20,70,current.surface.logicalWidth,current.surface.logicalHeight);
      observe('all-screen-modes',{mode,color,surface:current.surface});C.eq(color,'255,85,85',`Пиксель SCREEN ${mode}`);
      probe(current.surface.windowHandle,'char',{Key:13});
    }
    await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
  }},
  {id:'graphics-repair-screen-zero-to-graphics',name:'Проверка исправления перехода SCREEN 0 в SCREEN 1',async fn({S,C}) {
    await S.launch();
    const dir=testWorkspace('graphics-repair-screen-transition');
    for(const withText of [false,true]) {
      const file=writeAxm(dir,`transition-${withText}.axm`,(withText?'SCREEN 0\nPRINT "BEFORE"\nSLEEP 100\n':'')+'SCREEN 1\nLINE (10,60)-(30,80),12,BF\nDIM s AS STRING\nINPUT s\n');
      await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
      await S.page.waitForFunction(()=>window.__ide.execution.inputRequestId!==null&&window.__ide.execution.surface?.windowVisible===true);
      const surface=await S.page.evaluate(()=>window.__ide.execution.surface);
      await S.page.waitForTimeout(200);
      const frame=probe(surface.windowHandle,'capture',{OutputPath:path.join(OUT,`transition-${withText}.png`)});
      const color=pixel(frame,20,70);
      observe('screen-zero-to-graphics',{withText,surface,color});
      C.eq(color,'255,85,85',withText?'Красный пиксель после SCREEN 0 -> SCREEN 1':'Контрольный красный пиксель SCREEN 1');
      await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
    }
  }},
  {id:'graphics-repair-paint-checker-black',name:'Проверка исправления узорной PAINT на чёрном фоне',async fn({S,C}) {
    await S.launch();const dir=testWorkspace('graphics-repair-paint-black');
    const file=writeAxm(dir,'checker.axm','SCREEN 1\nCOLOR 6,0\nLINE (20,60)-(29,69),9,B\nPAINT (22,62),CHR(170) & CHR(85),9\nDIM s AS STRING\nINPUT s\n');
    await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
    await S.page.waitForFunction(()=>window.__ide.execution.inputRequestId!==null&&window.__ide.execution.surface?.windowVisible===true);
    const surface=await S.page.evaluate(()=>window.__ide.execution.surface);await S.page.waitForTimeout(200);
    const frame=probe(surface.windowHandle,'capture',{OutputPath:path.join(OUT,'paint-checker-black.png')});
    let colored=0;for(let y=61;y<=68;y++)for(let x=21;x<=28;x++)if(pixel(frame,x,y)==='170,85,0')colored++;
    observe('paint-checker-black',{colored,expected:32});C.eq(colored,32,'Шахматный узор заполняет 32 из 64 внутренних пикселей');
    await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
  }},
  {id:'graphics-repair-screen-input-surrogate-backspace',name:'Проверка исправления Backspace для emoji в SCREEN INPUT',async fn({S,C}) {
    await S.launch();const dir=testWorkspace('graphics-repair-native-unicode');const output=path.join(dir,'input.txt');
    const file=writeAxm(dir,'input.axm',`SCREEN 1\nDIM s AS STRING\nINPUT s\nWRITEFILE("${output.replaceAll('\\','/')}",s)\nSLEEP\n`);
    await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
    await S.page.waitForFunction(()=>window.__ide.execution.inputRequestId!==null&&window.__ide.execution.surface?.windowVisible===true);
    const hwnd=await S.page.evaluate(()=>window.__ide.execution.surface.windowHandle);
    probe(hwnd,'char',{Key:0xd83d});probe(hwnd,'char',{Key:0xde00});probe(hwnd,'char',{Key:8});
    probe(hwnd,'down',{Key:13});probe(hwnd,'char',{Key:13});probe(hwnd,'up',{Key:13});
    for(let i=0;i<50&&!fs.existsSync(output);i++)await S.page.waitForTimeout(100);
    C.ok(fs.existsSync(output),'Ввод принят и результат записан');
    if(fs.existsSync(output)) {const bytes=fs.readFileSync(output);observe('native-surrogate-backspace',{hex:bytes.toString('hex'),text:bytes.toString('utf8')});C.eq(bytes.length,0,'Одно нажатие Backspace удаляет введённый emoji целиком');}
    await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
  }},
  {id:'graphics-repair-bulk-put',name:'Пакетная запись PUT на настоящем CANVAS',async fn({S,C}) {
    await S.launch(); const dir=testWorkspace('graphics-repair-bulk-put');
    const file=writeAxm(dir,'put.axm','CANVAS 128,128\nDIM b[0 TO 127,0 TO 127] AS INTEGER\nFOR x=0 TO 127\nFOR y=0 TO 127\nb[x,y]=12\nNEXT\nNEXT\nBEGINFRAME\nPUT (0,0),b[],PSET\nPRESENT\nDIM answer AS STRING\nINPUT answer\n');
    await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
    await S.page.waitForFunction(()=>window.__ide.execution.inputRequestId!==null&&window.__ide.execution.surface?.windowVisible===true,null,{timeout:15000});
    const surface=await S.page.evaluate(()=>window.__ide.execution.surface);
    const frame=probe(surface.windowHandle,'capture',{OutputPath:path.join(OUT,'bulk-put.png')});
    C.eq(pixel(frame,64,64,128,128),'255,85,85','Пакет PUT показал красный центр кадра');
    await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
  }},
  {id:'graphics-repair-stop-paint',name:'Остановка большой заливки без зависания DesktopHost',async fn({S,C}) {
    await S.launch(); const dir=testWorkspace('graphics-repair-stop-paint'); const marker=path.join(dir,'started.txt');
    const file=writeAxm(dir,'paint.axm',`CANVAS 2048,2048\nWRITEFILE("${marker.replaceAll('\\','/')}","started")\nPAINT (0,0),6,9\nSLEEP\n`);
    await S.page.evaluate(async p=>{await window.__ide.openFile(p);await window.__ide.run();},file);
    for(let i=0;i<100&&!fs.existsSync(marker);i++)await S.page.waitForTimeout(50);
    C.ok(fs.existsSync(marker),'Поверхность создана, программа дошла до заливки');
    await S.page.waitForTimeout(100);
    const started=Date.now();await S.page.evaluate(()=>window.__ide.stop());await S.waitIdle();
    C.ok(Date.now()-started<8000,'Команда остановки принята во время тяжёлой графической операции');
    C.ok(await S.page.evaluate(()=>!window.__ide.execution.surface?.windowVisible),'Графическое окно закрыто после остановки');
  }},];
