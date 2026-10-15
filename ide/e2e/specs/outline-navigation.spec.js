const path = require('path');
const fs = require('fs');
const {testWorkspace, writeAxm} = require('../helpers');

module.exports = [{
  id: 'outline-maze-navigation',
  name: 'Навигатор Maze Chase: поиск, клавиатура, фильтры, закрепления, текущая процедура и профиль',
  async fn({S,C}) {
    const maze = path.resolve(__dirname, '../../../samples/graphics/game/maze_chase.axm');
    const source = fs.readFileSync(maze, 'utf8');
    const expected = (source.match(/^\s*(SUB|FUNCTION|TYPE)\s+\w+/gm) || []).length;
    await S.launch({settings:{smartIdentifierEnabled:false,sidebarVisible:true,outlineFollow:false,outlineGrouped:false}});
    await S.page.evaluate(p => window.__ide.openFile(p), maze);
    C.eq(await S.page.locator('.outline-item').count(), expected, 'Все объявления реального Maze Chase');
    const search = () => S.page.locator('#outline-search');
    await search().fill('pElLeT');
    const names = await S.page.locator('.outline-name').allTextContents();
    C.ok(names.length > 3 && names.every(n => /pellet/i.test(n)), 'Поиск без учёта регистра');
    C.eq(await S.page.locator('.outline-name mark').count(), names.length, 'Совпадения выделены');
    C.contains(await S.page.locator('#outline-count').innerText(), String(expected), 'Счётчик сохраняет общий объём');
    await search().fill('StepWalker');
    const step = S.page.locator('.outline-item').filter({hasText:'StepWalker'});
    C.contains(await step.locator('.outline-jump').getAttribute('title'), 'BYREF walker AS Walker', 'Подсказка сохраняет BYREF');
    C.contains(await step.locator('.outline-jump').getAttribute('title'), 'Общая процедура', 'Пояснение из исходника');
    await search().press('ArrowDown');
    C.ok(await step.locator('.outline-jump').evaluate(e => e === document.activeElement), 'Стрелка переводит фокус в список');
    await S.page.keyboard.press('Enter');
    const stepLine = source.split(/\r?\n/).findIndex(l => /^SUB StepWalker\(/i.test(l))+1;
    C.eq(await S.page.evaluate(() => window.__ide.editor.getPosition().lineNumber), stepLine, 'Enter переходит к объявлению');
    await S.page.evaluate(line => window.__ide.editor.setPosition({lineNumber:line+2,column:1}),stepLine);
    C.eq(await step.locator('.outline-jump').getAttribute('aria-current'), 'location', 'Курсор внутри тела выделяет процедуру');
    await step.locator('.outline-pin').click();
    await search().focus(); await search().press('Escape');
    C.eq(await S.page.locator('.outline-name').first().innerText(), 'StepWalker', 'Закрепление наверху');
    await S.page.locator('#outline-filter').selectOption('function');
    C.eq(await S.page.locator('.outline-item').count(), (source.match(/^FUNCTION\s/gm)||[]).length, 'Фильтр функций');
    await S.page.locator('#outline-sort').selectOption('name');
    const sorted = await S.page.locator('.outline-name').allTextContents();
    C.eq(sorted.join(','), [...sorted].sort((a,b)=>a.localeCompare(b,undefined,{sensitivity:'base'})).join(','), 'Алфавитный порядок');
    await search().fill('no_such_routine');
    C.contains(await S.page.locator('#outline-list').innerText(), 'Совпадений нет', 'Пустой результат поиска');
    await search().press('Escape');
    await S.page.locator('#outline-filter').selectOption('all');
    await S.page.locator('#outline-follow').click();
    const wrapLine = source.split(/\r?\n/).findIndex(l=>/^FUNCTION WrapTunnelColumn\(/i.test(l))+1;
    await S.page.evaluate(line=>window.__ide.editor.setPosition({lineNumber:line+1,column:1}),wrapLine);
    C.contains(await S.page.locator('.outline-current').innerText(),'WrapTunnelColumn','Следование находит последнюю функцию');
    C.ok(await S.page.locator('.outline-current').evaluate(e=>{const p=e.closest('#outline-list').getBoundingClientRect(),r=e.getBoundingClientRect();return r.top>=p.top-1&&r.bottom<=p.bottom+1;}),'Текущая функция прокручена в видимую область');
    await S.page.locator('#outline-follow').click();
    await S.page.evaluate(()=>{document.getElementById('outline-list').scrollTop=200;});
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.state.scroll>0);
    const before=await S.page.locator('#outline-list').evaluate(e=>e.scrollTop);
    await S.page.evaluate(()=>window.__ide.editor.executeEdits('outline-check',[{range:new window.monaco.Range(1,1,1,1),text:"' Навигация\n"}]));
    await S.page.waitForFunction(line=>window.__ide.outlinePanel.items.find(i=>i.name==='StepWalker').lineNo===line+1,stepLine);
    C.eq(await S.page.locator('#outline-list').evaluate(e=>e.scrollTop),before,'Правка сохраняет прокрутку');
    await S.page.evaluate(()=>window.__ide.editor.getModel().undo());
    const other=writeAxm(testWorkspace('outline-other'),'другой.axm','SUB StepWalker()\nEND SUB\n');
    await S.page.evaluate(p=>window.__ide.openFile(p),other);
    C.eq(await S.page.locator('.outline-pin').getAttribute('aria-pressed'),'false','Закрепления принадлежат файлу');
    await S.page.evaluate(p=>window.__ide.switchToFile(p),maze);
    C.eq(await S.page.locator('.outline-pin').first().getAttribute('aria-pressed'),'true','Закрепление восстановлено при смене вкладки');
    await S.page.evaluate(()=>window.__ide.setLanguage('en'));
    C.eq(await search().getAttribute('placeholder'),'Find a routine…','Элементы локализованы');
    C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
    await S.close(); await S.launch();
    await S.page.evaluate(p=>window.__ide.openFile(p),maze);
    C.eq(await S.page.locator('.outline-name').first().innerText(),'StepWalker','Закрепление пережило перезапуск');
    C.eq(await S.page.locator('#outline-sort').inputValue(),'name','Порядок сохранён в профиле');
    C.contains(await S.page.locator('#outline-follow').getAttribute('title'),'Automatically scroll','Подсказка объясняет действие кнопки');
    for (const theme of ['dark','light','basic','borland']) {
      await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
      const width=await search().evaluate(e=>{const r=e.getBoundingClientRect(),p=e.closest('#shell-sidebar').getBoundingClientRect();return r.width>0&&r.right<=p.right;});
      C.ok(width,'Поиск помещается в панели: '+theme);
      for (const id of ['outline-filter','outline-sort']) {
        C.ok(await S.page.locator('#'+id).evaluate(e=>{const s=getComputedStyle(e);return parseFloat(s.paddingLeft)>=7&&parseFloat(s.paddingRight)>=8;}),'Отступы текста и стрелки списка: '+id+', '+theme);
      }
    }
    await S.page.evaluate(()=>window.__ide.setLanguage('ru'));
    await S.screenshot('outline-maze-borland');
    C.eq(S.pageErrors.length,0,'Перезапуск без ошибок страницы');
  },
}, {
  id: 'outline-document-state-and-edits',
  name: 'Поиск и прокрутка принадлежат вкладке; текущая процедура учитывает границы и изменения',
  async fn({S,C}) {
    const dir=testWorkspace('outline-state');
    const a=writeAxm(dir,'первый.axm','SUB Alpha()\nPRINT 1\nEND SUB\n\nSUB Beta()\nEND SUB\n');
    const b=writeAxm(dir,'второй.axm','FUNCTION Gamma() AS INTEGER\nRETURN 3\nEND FUNCTION\n');
    await S.launch({settings:{smartIdentifierEnabled:false,outlineFollow:false,outlineGrouped:false}});
    await S.page.evaluate(p=>window.__ide.openFile(p),a);
    await S.page.locator('#outline-search').fill('Alpha');
    await S.page.evaluate(p=>window.__ide.openFile(p),b);
    C.eq(await S.page.locator('#outline-search').inputValue(),'','Другая вкладка имеет отдельный поиск');
    C.eq(await S.page.locator('.outline-name').innerText(),'Gamma','Поиск не скрывает объявления другой вкладки');
    await S.page.evaluate(p=>window.__ide.switchToFile(p),a);
    C.eq(await S.page.locator('#outline-search').inputValue(),'Alpha','Запрос первой вкладки восстановлен');
    await S.page.locator('#outline-search').press('Escape');
    await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:4,column:1}));
    C.eq(await S.page.locator('.outline-current').count(),0,'Между подпрограммами нет ложного выделения');
    await S.page.evaluate(()=>window.__ide.editor.executeEdits('outline-check',[{range:new window.monaco.Range(7,1,7,1),text:'SUB Delta()\nEND SUB\n'}]));
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.items.some(i=>i.name==='Delta'));
    C.eq(await S.page.locator('.outline-item').count(),3,'Новое объявление появилось без перезагрузки');
    await S.page.evaluate(()=>window.__ide.editor.getModel().undo());
    await S.page.waitForFunction(()=>!window.__ide.outlinePanel.items.some(i=>i.name==='Delta'));
    C.eq(await S.page.locator('.outline-item').count(),2,'Отмена удаляет объявление из списка');
    await S.page.locator('.outline-jump').first().focus();
    await S.page.keyboard.press('End');
    C.contains(await S.page.locator(':focus').innerText(),'Beta','End выбирает последний элемент');
    await S.page.keyboard.press('Home');
    C.contains(await S.page.locator(':focus').innerText(),'Alpha','Home выбирает первый элемент');
    await S.page.keyboard.press('Escape');
    C.eq(await S.page.locator(':focus').getAttribute('id'),'outline-search','Escape возвращает фокус в поиск');
    C.eq(S.pageErrors.length,0,'Изменения и смена вкладки без ошибок страницы');
  },
}, {
  id: 'outline-regions-state-search-folding',
  name: 'Вложенные разделы: поиск, состояние групп, плоский список, клавиатура и сворачивание редактора',
  async fn({S,C}) {
    const source="' #region Игра\n' #region Движение\nSUB Move()\nPRINT 1\nEND SUB\n' #endregion\nFUNCTION Read() AS INTEGER\nRETURN 2\nEND FUNCTION\n' #endregion\nSUB Tail()\nEND SUB\n";
    const file=writeAxm(testWorkspace('outline-regions'),'разделы.axm',source);
    await S.launch({settings:{smartIdentifierEnabled:false,outlineGrouped:true,outlineFollow:false}});
    await S.page.evaluate(p=>window.__ide.openFile(p),file);
    C.eq(await S.page.locator('.outline-group-toggle').count(),2,'Два вложенных раздела');
    C.eq(await S.page.locator('.outline-name').count(),3,'Видны все объявления, включая внешнее');
    C.eq(await S.page.locator('#outline-region-warnings').isVisible(),false,'Правильные разделы без предупреждений');
    C.eq(await S.page.locator('#outline-group-all').getAttribute('title'),'Свернуть все группы','Все группы открыты: предлагается сворачивание');
    C.eq(await S.page.locator('#outline-group-all .codicon-fold').count(),1,'Значок сворачивания');
    const game=()=>S.page.locator('.outline-group-toggle').filter({hasText:'Игра'});
    await game().focus();await S.page.keyboard.press('ArrowLeft');
    C.eq(await S.page.locator('.outline-name').allTextContents().then(a=>a.join(',')),'Tail','Клавиатура сворачивает группу');
    await S.page.locator('#outline-search').fill('move');
    C.eq(await S.page.locator('.outline-name').innerText(),'Move','Поиск раскрывает вложенный результат');
    C.eq(await S.page.locator('.outline-group-toggle').count(),2,'Поиск сохраняет путь к результату');
    await S.page.locator('#outline-search').press('Escape');
    C.eq(await game().getAttribute('aria-expanded'),'false','Очистка поиска восстанавливает свёрнутость');
    await S.page.locator('#outline-search').fill('Движение');
    C.eq(await S.page.locator('.outline-name').innerText(),'Move','Поиск по названию раздела');
    await S.page.locator('#outline-search').press('Escape');
    await S.page.locator('#outline-groups').click();
    C.eq(await S.page.locator('.outline-group-toggle').count(),0,'Плоский список без заголовков');
    C.eq(await S.page.locator('.outline-name').count(),3,'Объявления не теряются');
    await S.page.locator('#outline-groups').click();
    C.eq(await game().getAttribute('aria-expanded'),'false','Переключение вида сохраняет свёрнутость');
    await S.page.locator('#outline-follow').click();
    await S.page.evaluate(()=>window.__ide.editor.setPosition({lineNumber:4,column:1}));
    C.eq(await game().getAttribute('aria-expanded'),'true','Следование раскрывает предков');
    C.contains(await S.page.locator('.outline-current').innerText(),'Move','Текущая процедура в группе');
    await S.page.locator('#outline-follow').click();
    await S.page.locator('.outline-group-toggle').filter({hasText:'Движение'}).click();
    C.eq(await S.page.locator('#outline-group-all').getAttribute('title'),'Развернуть все группы','Частично свёрнутое дерево: предлагается раскрытие');
    C.eq(await S.page.locator('#outline-group-all .codicon-unfold').count(),1,'Значок раскрытия');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('#outline-group-all').getAttribute('title'),'Свернуть все группы','После раскрытия команда меняется обратно');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('.outline-name').count(),1,'Свернуть все сохраняет внешние объявления');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('.outline-name').count(),3,'Развернуть все возвращает объявления');
    const pin=S.page.locator('.outline-item').filter({hasText:'Move'}).locator('.outline-pin');
    await pin.click();
    C.eq(await S.page.locator('#outline-list > .outline-item .outline-name').first().innerText(),'Move','Закреплённая процедура вынесена наверх');
    C.eq(await S.page.locator('.outline-name').count(),3,'Закрепление без дублирования');
    await S.page.locator('.outline-item').filter({hasText:'Move'}).locator('.outline-pin').click();
    await S.page.evaluate(()=>window.__ide.editor.getAction('editor.foldAllMarkerRegions').run());
    await S.page.waitForFunction(()=>!window.__ide.editor.getVisibleRanges().some(r=>r.startLineNumber<=4&&r.endLineNumber>=4));
    C.ok(true,'Реальное сворачивание #region в Monaco скрывает тело');
    await S.page.evaluate(()=>window.__ide.editor.getAction('editor.unfoldAllMarkerRegions').run());
    await S.page.waitForFunction(()=>window.__ide.editor.getVisibleRanges().some(r=>r.startLineNumber<=4&&r.endLineNumber>=4));
    C.ok(true,'Разворачивание возвращает код');
    C.ok(await S.page.evaluate(()=>window.monaco.editor.tokenize("' #region Игра",'axiom')[0][0].type.startsWith('comment')),'Метка остаётся комментарием в подсветке');
    await S.page.evaluate(()=>window.__ide.formatWholeDocument('region-check'));
    C.eq((await S.editorValue()).split(/\r?\n/).filter(l=>/#(?:end)?region/i.test(l)).join('\n'),source.split('\n').filter(l=>/#(?:end)?region/i.test(l)).join('\n'),'Форматирование сохраняет метки');
    await S.page.locator('#outline-group-all').click();
    await S.close();await S.launch();await S.page.evaluate(p=>window.__ide.openFile(p),file);
    C.eq(await game().getAttribute('aria-expanded'),'false','Свёрнутость сохраняется после перезапуска');
    await S.page.locator('#outline-group-all').click();
    await S.page.evaluate(()=>window.__ide.editor.executeEdits('region-check',[{range:new window.monaco.Range(1,1,1,1),text:"' Дополнительная строка\n"}]));
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.regions[0].lineNo===2);
    C.eq(await S.page.locator('.outline-name').count(),3,'Сдвиг строк не теряет содержимое');
    await S.page.evaluate(()=>window.__ide.editor.getModel().setValue("' #endregion\n' #region Без конца\nSUB Work()\nEND SUB\n"));
    await S.page.waitForFunction(()=>window.__ide.outlinePanel.regionWarnings.length===2);
    C.contains(await S.page.locator('#outline-region-warnings').innerText(),'не закрыт','Показано незакрытое начало');
    C.eq(await S.page.locator('.outline-name').innerText(),'Work','Повреждённые метки не скрывают код');
    await S.page.evaluate(()=>window.__ide.setLanguage('en'));
    C.contains(await S.page.locator('#outline-region-warnings').innerText(),'unclosed region','Предупреждение переводится');
    C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
  },
}, {
  id: 'outline-maze-regions',
  name: 'Разделы реального Maze Chase: группировка, фильтр и поиск калибровки',
  async fn({S,C}) {
    const file=path.resolve(__dirname,'../../../samples/graphics/game/maze_chase.axm');
    await S.launch({settings:{smartIdentifierEnabled:false,outlineGrouped:true}});
    await S.page.evaluate(p=>window.__ide.openFile(p),file);
    C.ok(await S.page.locator('.outline-group-toggle').count()>=10,'Тематические разделы Maze Chase');
    C.eq(await S.page.locator('.outline-item').count(),62,'Все 61 подпрограмма и тип доступны');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('.outline-item').count(),1,'Свёрнута логика, тип Walker остаётся снаружи');
    await S.page.locator('#outline-search').fill('Калибровка');
    C.ok(await S.page.locator('.outline-item').count()>10,'Поиск раскрывает подпрограммы калибровки');
    await S.page.locator('#outline-filter').selectOption('function');
    C.ok((await S.page.locator('.outline-icon').allTextContents()).every(t=>t==='ƒ'),'Фильтр функций действует внутри групп');
    await S.page.locator('#outline-search').press('Escape');
    await S.page.locator('#outline-filter').selectOption('all');
    await S.screenshot('outline-maze-regions');
    C.eq(S.pageErrors.length,0,'Maze Chase без ошибок страницы');
  },
}, {
  id: 'outline-sidebar-theme-and-icons',
  name: 'Компактные общие значки навигатора и спокойная палитра групп в Pascal',
  async fn({S,C}) {
    const file=path.resolve(__dirname,'../../../samples/graphics/game/maze_chase.axm');
    await S.launch({settings:{smartIdentifierEnabled:false,outlineGrouped:true,sidebarWidth:320}});
    await S.page.evaluate(p=>window.__ide.openFile(p),file);
    await S.page.locator('#outline-group-all').click();
    for(const [id,icon] of [['outline-groups','list-tree'],['outline-follow','target'],['outline-group-all','unfold']]) {
      C.eq(await S.page.locator('#'+id+' .codicon-'+icon).count(),1,'Значок из общего набора: '+id);
      C.ok(await S.page.locator('#'+id).evaluate((e,key)=>e.innerHTML===window.ShellIcons.markup(key),icon),'Общий источник значков: '+id);
    }
    for(const theme of ['dark','light','basic','borland']) {
      await S.page.evaluate(t=>window.__ide.setTheme(t),theme);
      await S.page.mouse.move(750,450);
      await S.sleep(180);
      const state=await S.page.evaluate(()=>({
        buttons:[...document.querySelectorAll('.outline-group-tools .toolbar-btn')].map(e=>({width:e.getBoundingClientRect().width,bg:getComputedStyle(e).backgroundColor,label:e.getAttribute('aria-label')})),
        groupBg:getComputedStyle(document.querySelector('.outline-group-toggle')).backgroundColor,
      }));
      C.ok(state.buttons.length===3&&state.buttons.every(b=>b.width<=28&&b.width>=24),'Три компактные кнопки: '+theme);
      C.ok(await S.page.locator('.outline-group-tools').evaluate(e=>Math.abs(e.getBoundingClientRect().right-e.lastElementChild.getBoundingClientRect().right)<1),'Значки выровнены справа: '+theme);
      C.ok(state.buttons.every(b=>b.bg==='rgba(0, 0, 0, 0)'&&b.label),'Прозрачные кнопки с подписями: '+theme);
      if(theme==='borland')C.eq(state.groupBg,'rgba(0, 0, 0, 0)','Группы Pascal без серых плашек');
      await S.screenshot('outline-sidebar-'+theme);
    }
    await S.page.locator('#outline-follow').click();
    C.eq(await S.page.locator('#outline-follow').getAttribute('aria-pressed'),'true','Прицел включает следование');
    await S.page.mouse.move(750,450);
    await S.sleep(180);
    const active=await S.page.locator('#outline-follow').evaluate(e=>({bg:getComputedStyle(e).backgroundColor,color:getComputedStyle(e).color}));
    C.eq(active.bg,'rgba(0, 0, 0, 0)','Включённое следование без серого фона');
    C.eq(active.color,'rgb(255, 255, 85)','Включённое следование выделено жёлтым значком Pascal');
    await S.page.locator('.outline-group-toggle').first().hover();
    await S.sleep(180);
    C.eq(await S.page.locator('.outline-group-toggle').first().evaluate(e=>getComputedStyle(e).backgroundColor),'rgb(0, 0, 170)','Ненавязчивое синее наведение в Pascal');
    await S.page.locator('#outline-groups').click();
    C.eq(await S.page.locator('.outline-group-toggle').count(),0,'Дерево переключает плоский список');
    await S.page.locator('#outline-groups').click();
    C.ok(await S.page.locator('.outline-group-toggle').count()>0,'Дерево возвращает группы');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('.outline-item').count(),62,'Новый значок разворачивает все');
    await S.page.locator('#outline-group-all').click();
    C.eq(await S.page.locator('.outline-item').count(),1,'Новый значок сворачивает все');
    await S.page.evaluate(()=>window.__ide.setLanguage('en'));
    C.contains(await S.page.locator('#outline-groups').getAttribute('aria-label'),'Show sections','Подпись значка переводится');
    C.eq(S.pageErrors.length,0,'Нет ошибок страницы');
  },
}];
