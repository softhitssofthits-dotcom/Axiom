// Явные ожидаемые результаты ключевых действий; остальные пункты сохраняют своё наблюдение в плане.
function verify(row){
 const failures=[],check=(ok,message)=>{if(!ok)failures.push(message);},cases=row.cases||[],get=value=>cases.find(c=>c.value===value)?.observed;
 if(row.status==='unsupported'){check(row.locales?.length===2,'Две локали недоступного элемента');return failures;}
 check(cases.length>=2,'Не менее двух вариантов действия');check(!row.errors.length,'Нет ошибок сценария');
 const off=get(false),on=get(true),key=row.key;
 if(key==='bracketPairColorization.enabled'){check(off?.length===0,'Выключенная раскраска отсутствует');check(new Set(on?.map(x=>x.color)).size>=2,'Вложенные скобки имеют разные цвета');}
 if(key==='trimWhitespaceOnDelete'){check(off?.text==='Print 1    Print 2','Delete сохраняет отступ следующей строки');check(on?.text==='Print 1Print 2','Delete убирает отступ следующей строки');}
 if(key==='pasteAs.enabled'){check(off?.pasteMetadata===false&&on?.pasteMetadata===true,'Признак обработки специальной вставки в системном буфере меняется');}
 if(key==='dropIntoEditor.enabled'){
  check(off?.text==='','Выключенное перетаскивание не вставляет текст');check(on?.text?.endsWith('dropped.axm'),'Вставлен настоящий путь');check(!on?.text?.includes('$0')&&!on?.text?.includes('\\\\'),'Нет служебной позиции заготовки и удвоенных слешей');
  check(on?.undo===''&&on?.redo===on?.text,'Перетаскивание отменяется и повторяется');
  check(on?.events.some(e=>e.type==='drop'&&e.trusted&&e.types.includes('text/uri-list')),'Настоящее системное событие Drop');
 }
 if(key==='suggest.snippetsPreventQuickSuggestions'){check(off?.snippet&&on?.snippet,'Оба действия происходят внутри заготовки');check(off?.widget.length>0&&on?.widget.length===0,'Подсказки видимы только при разрешении');}
 if(key==='inlineSuggest.suppressInSnippetMode'){check(off?.snippet&&on?.snippet,'Оба действия происходят внутри заготовки');check(off?.ghost.length>0&&on?.ghost.length===0,'Продолжение скрывается в заготовке');}
 if(key==='suggest.localityBonus'){check(off?.items[0].label==='appleFar'&&on?.items[0].label==='apricotNear','Ближайшее имя становится первым');}
 if(key==='inertialScroll'){check(new Set(off?.trace).size===1,'Без инерции движение прекращается');check(on?.trace.at(-1)>on?.trace[0],'С инерцией движение продолжается после ввода');}
 if(key==='scrollbar.alwaysConsumeMouseWheel'){check(off?.wheel.some(e=>!e.prevented)&&on?.wheel.some(e=>e.prevented),'Событие поглощается только при включении');}
 if(key==='find.autoFindInSelection'){check(get('never')?.matches===3,'Поиск во всём файле');check(get('always')?.matches===2&&get('multiline')?.matches===2,'Поиск в двух выделенных строках');}
 if(key==='mouseMiddleClickAction'){check(get('default')?.urls.length===0,'Обычная средняя кнопка не открывает ссылку');check(get('openLink')?.urls.length===1&&get('ctrlLeftClick')?.urls.length===1,'Открытие настоящей локальной страницы');}
 if(key==='autoDetectHighContrast'){check(off?.after.forced&&on?.after.forced,'Windows действительно переключилась');check(!off?.after.theme.includes('hc-black')&&on?.after.theme.includes('hc-black'),'Тема следует системе только при включении');}
 for(const c of cases){
  if(key==='fontFamily')check(c.observed?.font.replaceAll('\"','').replaceAll("'",'').includes(c.value.replaceAll('\"','').replaceAll("'",'')),'Название выбранного шрифта');
  if(key==='fontSize')check(c.observed?.size===c.value+'px','Размер шрифта');
  if(key==='fontWeight')check(c.observed?.weight===(c.value==='bold'?'700':c.value==='normal'?'400':c.value),'Толщина шрифта');
  if(key==='lineHeight')check(c.observed?.height===c.value+'px','Высота строки');
  if(key==='letterSpacing')check(c.observed?.spacing===(c.value?c.value+'px':'normal'),'Интервал букв');
 }
 return failures;
}
module.exports={verify};
