(function () {
  const P=window.EditorPreferences;
  const tabs={appearance:['Редактор','Editor'],font:['Шрифт и тема','Font & Theme'],typing:['Ввод и отступы','Typing & Indentation'],
    suggestions:['Подсказки','Suggestions'],scroll:['Прокрутка и обзор','Scrolling & Overview'],selection:['Курсор и выделение','Cursor & Selection'],
    structure:['Структура','Structure'],advanced:['Дополнительные','Advanced']};
  const strings={
    ru:{title:'Параметры Axiom IDE',search:'Поиск по всем настройкам',reset:'По умолчанию',ok:'ОК',apply:'Применить',cancel:'Отмена',
      help:'Наведите указатель на настройку или перейдите к ней клавишей Tab, чтобы прочитать пояснение.',none:'Настройки не найдены.',
      error:'Проверьте значение: ',saveError:'Не удалось сохранить настройки. Проверьте доступность папки профиля и повторите.',
      theme:'Тема оформления',language:'Язык интерфейса',preview:'Предпросмотр',saved:'Настройки сохранены.',
      themeHelp:'Выбирает оформление всей IDE. Образец кода показывает выбранную тему до применения.',languageHelp:'Меняет подписи, кнопки и пояснения IDE на русский или английский язык после применения.',
      provider:' Для отображения результата нужен соответствующий языковой провайдер.',numbers:' Допустимый диапазон: ',json:' Ввод в формате JSON.'},
    en:{title:'Axiom IDE Settings',search:'Search all settings',reset:'Restore Defaults',ok:'OK',apply:'Apply',cancel:'Cancel',
      help:'Hover over a setting or focus it with Tab to read its description.',none:'No settings found.',error:'Check the value: ',
      saveError:'Could not save settings. Check that the profile folder is writable and try again.',theme:'Color theme',language:'Interface language',
      preview:'Preview',saved:'Settings saved.',provider:' This feature requires a corresponding language provider.',numbers:' Allowed range: ',json:' Enter JSON.',
      themeHelp:'Choose the appearance of the whole IDE. The code sample previews the selected theme before applying.',languageHelp:'Change IDE labels, buttons and descriptions to Russian or English after applying.'}
  };
  const valueLabels={
    true:['Включено','Enabled'],false:['Выключено','Disabled'],on:['Включено','On'],off:['Выключено','Off'],auto:['Автоматически','Automatic'],
    none:['Нет','None'],always:['Всегда','Always'],never:['Никогда','Never'],smart:['По контексту','Smart'],
    languageDefined:['По правилам языка','Language rules'],beforeWhitespace:['Перед пробелом','Before whitespace'],
    quotes:['Кавычки','Quotes'],brackets:['Скобки','Brackets'],keep:['Сохранять','Keep'],advanced:['Расширенный','Advanced'],full:['Полный','Full'],
    onKeyboardModifier:['С клавишей-модификатором','With modifier key'],onCode:['Только на коде','On code only'],
    viewport:['По ширине редактора','Editor width'],wordWrapColumn:['По заданной колонке','Wrap column'],bounded:['Меньшая из двух ширин','Smaller of both widths'],
    left:['Слева','Left'],right:['Справа','Right'],proportional:['Пропорционально','Proportional'],fill:['Заполнить высоту','Fill height'],fit:['Вместить документ','Fit document'],
    visible:['Всегда видна','Visible'],hidden:['Скрыта','Hidden'],mouseover:['При наведении','On hover'],scroll:['При прокрутке','On scroll'],
    outlineModel:['Структура языка','Language outline'],foldingProviderModel:['Области сворачивания','Folding regions'],indentationModel:['Уровни отступов','Indentation levels'],
    indentation:['По отступам','Indentation'],same:['Как у исходной строки','Same as original line'],indent:['Дополнительный отступ','One extra indent'],deepIndent:['Два дополнительных отступа','Two extra indents'],
    line:['Строка / линия','Line'],gutter:['Поле номера','Gutter'],all:['Все','All'],relative:['Относительные','Relative'],interval:['Через интервал','At intervals'],
    boundary:['На границах','At boundaries'],selection:['В выделении','In selection'],trailing:['В конце строки','Trailing'],dimmed:['Приглушённо','Dimmed'],
    block:['Блок','Block'],underline:['Подчёркивание','Underline'],'line-thin':['Тонкая линия','Thin line'],'block-outline':['Контур блока','Block outline'],
    'underline-thin':['Тонкое подчёркивание','Thin underline'],blink:['Обычное мигание','Blink'],smooth:['Плавное мигание','Smooth'],phase:['Плавное с паузой','Phase'],
    expand:['Сжатие и расширение','Expand'],solid:['Без мигания','Solid'],explicit:['При явных перемещениях','Explicit movements'],
    altKey:['Alt','Alt'],ctrlKey:['Ctrl','Ctrl'],metaKey:['Meta','Meta'],spread:['Разнести строки по курсорам','Spread lines across cursors'],
    full:['Полный','Full'],top:['В начале','Top'],bottom:['В конце','Bottom'],inline:['В общем списке / строке','Inline'],
    first:['Первый вариант','First'],recentlyUsed:['Последний использованный','Recently used'],recentlyUsedByPrefix:['Последний для этого префикса','Recently used by prefix'],
    insert:['Вставка','Insert'],replace:['Замещение','Replace'],prefix:['Префикс','Prefix'],subword:['Часть слова','Subword'],subwordSmart:['По частям с учётом позиции','Smart subword'],
    whenQuickSuggestion:['При автоматических подсказках','When quick suggestions'],whenTriggerCharacter:['После специального символа','When trigger character'],
    offUnlessPressed:['Только с нажатой клавишей','Only while pressed'],onUnlessPressed:['Кроме удержания клавиши','Except while pressed'],
    inUntrustedWorkspace:['В недоверенных документах','In untrusted workspace'],configuredByTheme:['По теме оформления','Configured by theme'],
    currentDocument:['Текущий документ','Current document'],matchingDocuments:['Документы того же языка','Matching documents'],allDocuments:['Все документы','All documents'],
    offWithInlineSuggestions:['Отключать при продолжениях в строке','Off with inline suggestions'],
    offWhenInlineCompletions:['Скрыть при продолжениях в строке','Off with inline completions'],fast:['Быстрый','Fast'],simple:['Быстрый','Simple'],
    advanced:['Расширенный','Advanced'],normal:['Обычный','Normal'],keepAll:['Не разбивать слова','Keep words'],text:['Текстовый','Text'],copy:['Копирование','Copy'],
    default:['По умолчанию','Default'],openLink:['Открыть ссылку','Open link'],ctrlLeftClick:['Как Ctrl и левая кнопка','Like Ctrl + left click'],
    afterDrop:['После перетаскивания','After drop'],afterPaste:['После вставки','After paste'],always:['Всегда','Always'],
    ctrlCmd:['Ctrl (Command в macOS)','Ctrl (Command on macOS)'],alt:['Alt','Alt'],onlySnippets:['Только заготовки','Snippets only'],
    singleFile:['В текущем документе','Current document'],multiFile:['В нескольких документах','Multiple documents'],
    near:['Рядом с курсором','Near cursor'],active:['Только текущие','Active only'],clickAndHover:['При нажатии и наведении','Click and hover'],
    onHover:['При наведении','On hover'],peek:['Карточка внутри редактора','Peek'],goto:['Прямой переход','Go to'],gotoAndPeek:['Переход и карточка','Go to and peek'],
    remove:['Удалять','Remove'],prompt:['Спрашивать','Ask'],ignore:['Игнорировать','Ignore'],both:['Оба способа','Both'],click:['При нажатии','On click'],
    hover:['При наведении','On hover'],readOnly:['В режиме чтения','Read only'],editable:['При редактировании','Editable'],tree:['Дерево результатов','Result tree'],editor:['Редактор','Editor'],multiline:['Для нескольких строк','Multiline']
  };
  class EditorSettingsDialog {
    constructor(ide) {this.ide=ide;this.group='appearance';this.query='';this.busy=false;}
    text(key) {return strings[this.ide.settings.language==='en'?'en':'ru'][key];}
    pair(pair) {return pair[this.ide.settings.language==='en'?1:0];}
    open() {
      if(this.overlay) {this.search.focus();return;}
      this.defaults=P.defaults(this.ide.monaco);
      this.draft={...P.clone(this.defaults),...P.normalize(this.ide.settings.editorPreferences)};
      this.invalid=new Map();
      this.theme=this.ide.settings.theme;this.language=this.ide.settings.language;this.query='';
      this.previousFocus=document.activeElement;
      this.overlay=document.createElement('div');this.overlay.className='modal-overlay editor-settings-overlay';
      this.dialog=document.createElement('section');this.dialog.className='editor-settings-dialog';
      this.dialog.setAttribute('role','dialog');this.dialog.setAttribute('aria-modal','true');this.dialog.setAttribute('aria-labelledby','editor-settings-title');
      this.overlay.append(this.dialog);document.body.append(this.overlay);
      this.app=document.getElementById('app');this.wasInert=this.app.inert;this.app.inert=true;
      this.overlay.addEventListener('keydown',e=>this.keydown(e));
      this.render();this.search.focus();
    }
    element(tag,className,text) {const el=document.createElement(tag);if(className)el.className=className;if(text!==undefined)el.textContent=text;return el;}
    button(text,action,className='') {const b=this.element('button',className,text);b.type='button';b.onclick=action;return b;}
    decorateButtonHotkey(button,index,defaultText=false) {
      const chars=Array.from(button.textContent||'');if(!chars.length)return;
      const hotkeyIndex=Math.max(0,Math.min(index,chars.length-1));
      const fragment=document.createDocumentFragment();
      if(defaultText&&hotkeyIndex>0)fragment.append(this.element('span','settings-button-default-text',chars.slice(0,hotkeyIndex).join('')));
      else if(hotkeyIndex>0)fragment.append(document.createTextNode(chars.slice(0,hotkeyIndex).join('')));
      fragment.append(this.element('span','settings-button-hotkey',chars[hotkeyIndex]));
      if(hotkeyIndex+1<chars.length)fragment.append(document.createTextNode(chars.slice(hotkeyIndex+1).join('')));
      button.replaceChildren(fragment);
    }
    render() {
      this.dialog.replaceChildren();
      const head=this.element('header','settings-header'); const title=this.element('h2','',this.text('title'));title.id='editor-settings-title';
      const close=this.button('×',()=>this.close(),'settings-close');close.title=this.text('cancel');UIControls.setLabel(close,this.text('cancel'));head.append(title,close);
      this.search=this.element('input','settings-search');this.search.type='search';this.search.placeholder=this.text('search');UIControls.setLabel(this.search,this.text('search'),{tooltip:false});this.search.value=this.query;
      this.search.oninput=()=>{this.query=this.search.value;this.renderContent();};
      this.tabbar=this.element('div','settings-tabs');this.tabbar.setAttribute('role','tablist');
      for(const [id,label] of Object.entries(tabs)) {
        const tab=this.button(this.pair(label),()=>this.selectTab(id),'settings-tab');tab.id='settings-tab-'+id;tab.dataset.group=id;
        tab.setAttribute('role','tab');tab.setAttribute('aria-controls','settings-panel');
        tab.onkeydown=e=>{if(['ArrowLeft','ArrowRight','Home','End'].includes(e.key)){e.preventDefault();const ids=Object.keys(tabs);let n=ids.indexOf(id);n=e.key==='Home'?0:e.key==='End'?ids.length-1:(n+(e.key==='ArrowRight'?1:-1)+ids.length)%ids.length;this.selectTab(ids[n]);this.tabbar.children[n].focus();}};
        this.tabbar.append(tab);
      }
      this.content=this.element('div','settings-content');this.content.id='settings-panel';
      this.help=this.element('div','settings-help',this.text('help'));this.help.id='settings-help';
      this.status=this.element('div','settings-status');this.status.setAttribute('role','status');
      const footer=this.element('footer','settings-footer');this.resetButton=this.button(this.text('reset'),()=>this.reset());footer.append(this.resetButton);
      const spacer=this.element('span','settings-spacer');footer.append(spacer);
      this.okButton=this.button(this.text('ok'),()=>this.apply(true),'settings-primary');this.okButton.dataset.settingsAction='ok';
      this.applyButton=this.button(this.text('apply'),()=>this.apply(false));this.applyButton.dataset.settingsAction='apply';
      this.cancelButton=this.button(this.text('cancel'),()=>this.close());this.cancelButton.dataset.settingsAction='cancel';footer.append(this.okButton,this.applyButton,this.cancelButton);
      this.decorateButtonHotkey(this.resetButton,this.ide.settings.language==='en'?0:3);
      this.decorateButtonHotkey(this.okButton,1,true);
      this.decorateButtonHotkey(this.applyButton,0);
      this.decorateButtonHotkey(this.cancelButton,0);
      this.dialog.append(head,this.search,this.tabbar,this.content,this.help,this.status,footer);this.renderContent();
    }
    selectTab(group) {this.group=group;this.query='';this.search.value='';this.renderContent();this.content.scrollTop=0;}
    renderContent() {
      const query=this.query.trim().toLocaleLowerCase();this.content.replaceChildren();
      this.tabbar.querySelectorAll('button').forEach(b=>{const active=b.dataset.group===this.group&&!query;b.setAttribute('aria-selected',String(active));b.tabIndex=active?0:-1;});
      this.content.setAttribute('role',query?'region':'tabpanel');UIControls.setLabel(this.content,query?this.text('search'):this.pair(tabs[this.group]),{tooltip:false});
      if(!query)this.content.setAttribute('aria-labelledby','settings-tab-'+this.group);else this.content.removeAttribute('aria-labelledby');
      this.controls=new Map();
      if(!query&&(this.group==='appearance'||this.group==='font')) {
        const field=this.group==='font'?'theme':'language';const choices=field==='theme'?['dark','light','basic','borland']:['ru','en'];
        const labels=field==='theme'?[['Тёмная','Dark'],['Светлая','Light'],['QuickBASIC','QuickBASIC'],['Borland Pascal','Borland Pascal']]:[['Русский','Russian'],['Английский','English']];
        const row=this.element('div','settings-row');const label=this.element('label','',this.text(field));label.htmlFor='settings-'+field;
        const select=this.element('select');select.id='settings-'+field;
        const help=this.text(field+'Help');row.title=help;label.title=help;select.title=help;select.setAttribute('aria-describedby','settings-help');
        select.onfocus=()=>{this.help.textContent=help;};row.onmouseenter=()=>{this.help.textContent=help;};
        choices.forEach((value,n)=>{const opt=this.element('option','',this.pair(labels[n]));opt.value=value;select.append(opt);});select.value=this[field];
        select.onchange=()=>{this[field]=select.value;this.updatePreview();};row.append(label,select);this.content.append(row);
      }
      const matches=P.entries.filter(e=>query?[e.ru,e.en,e.helpRu,e.helpEn,e.key].join(' ').toLocaleLowerCase().includes(query):e.group===this.group);
      let subgroup='';
      for(const entry of matches) {
        const prefix=query?entry.group:entry.key.split('.')[0];
        if(prefix!==subgroup){subgroup=prefix;const heading=this.element('h3','settings-subheading',query?this.pair(tabs[entry.group]):this.sectionName(prefix));this.content.append(heading);}
        this.addControl(entry);
      }
      if(!matches.length)this.content.append(this.element('p','',this.text('none')));
      if(!query&&this.group==='font')this.createPreview();
      this.syncDependencies();
      UIControls.setEnabled(this.resetButton,!query);
    }
    sectionName(prefix) {
      const named={suggest:['Список дополнений','Completion list'],quickSuggestions:['Автоматические подсказки','Quick suggestions'],hover:['Подсказки при наведении','Hover'],
        parameterHints:['Параметры вызовов','Parameter hints'],stickyScroll:['Закреплённые строки','Sticky Scroll'],minimap:['Миникарта','Minimap'],scrollbar:['Полосы прокрутки','Scrollbars'],
        guides:['Направляющие','Guides'],bracketPairColorization:['Цвета скобок','Bracket colors'],find:['Поиск','Find'],inlayHints:['Встроенные пояснения','Inlay hints'],
        inlineSuggest:['Продолжения внутри строки','Inline suggestions'],unicodeHighlight:['Предупреждения о символах','Unicode highlighting'],comments:['Комментарии','Comments'],padding:['Поля','Padding'],
        dropIntoEditor:['Перетаскивание','Drop into editor'],pasteAs:['Способы вставки','Paste options'],smartSelect:['Умное выделение','Smart selection']};
      return named[prefix]?this.pair(named[prefix]):'';
    }
    addControl(entry) {
      const row=this.element('div','settings-row');row.dataset.setting=entry.key;
      if(entry.available===false)row.dataset.unavailable='true';
      const label=this.element('label','',this.ide.settings.language==='en'?entry.en:entry.ru);
      const id='setting-'+entry.key.replace(/\./g,'-');label.htmlFor=id;
      let control,compound;
      const openType=entry.key==='fontLigatures'||entry.key==='fontVariations';
      if(openType) {
        compound=this.element('div','settings-font-option');control=this.element('select');
        for(const [value,label] of [['false',['Выключено','Disabled']],['true',['Включено','Enabled']],['custom',['Свои параметры…','Custom settings…']]]){const opt=this.element('option','',this.pair(label));opt.value=value;control.append(opt);}
        control.value=typeof this.draft[entry.key]==='string'?'custom':JSON.stringify(this.draft[entry.key]);
        control.detail=this.element('input');control.detail.type='text';control.detail.maxLength=2000;control.detail.value=typeof this.draft[entry.key]==='string'?this.draft[entry.key]:'';
        UIControls.setEnabled(control.detail,control.value==='custom');UIControls.setLabel(control.detail,(this.ide.settings.language==='en'?entry.en:entry.ru)+' — OpenType',{tooltip:false});
        control.detail.oninput=()=>control.oninput();compound.append(control,control.detail);
      } else if(entry.kind==='select') {
        control=this.element('select');for(const value of entry.choices){const opt=this.element('option','',this.valueText(value,entry));opt.value=JSON.stringify(value);control.append(opt);}control.value=JSON.stringify(this.draft[entry.key]);
      } else {
        control=this.element(entry.kind==='json'?'textarea':'input');
        if(entry.kind==='boolean'){control.type='checkbox';control.checked=this.draft[entry.key];}
        else if(entry.kind==='number'){control.type='number';const bounds=P.limits[entry.key]||[0,1000000];control.min=bounds[0];control.max=bounds[1];control.step=P.fractional.has(entry.key)?'0.1':'1';control.value=this.draft[entry.key];}
        else {if(entry.kind==='text')control.type='text';control.value=entry.kind==='json'?JSON.stringify(this.draft[entry.key]):this.draft[entry.key];}
      }
      if(entry.key==='fontFamily'||entry.key==='fontWeight'){
        compound=this.element('div','settings-font-choice');control=this.element('select');control.fontChoice=true;
        const choices=entry.key==='fontFamily'
          ? [[this.defaults.fontFamily,this.pair(['По умолчанию','Default'])],...['Cascadia Code','Consolas','Courier New','Lucida Console','monospace'].map(font=>[font,font])]
          : [['normal',this.pair(['Обычный','Regular'])],['bold',this.pair(['Полужирный','Bold'])],...['100','200','300','400','500','600','700','800','900'].map(weight=>[weight,weight])];
        for(const [value,label] of choices){const opt=this.element('option','',label);opt.value=value;control.append(opt);}
        const custom=this.element('option','',this.pair(['Свой вариант…','Custom…']));custom.value='custom';control.append(custom);
        const known=choices.some(([value])=>value===this.draft[entry.key]);
        control.value=known?this.draft[entry.key]:'custom';
        control.detail=this.element('input');control.detail.type='text';control.detail.maxLength=2000;control.detail.value=this.draft[entry.key];
        UIControls.setLabel(control.detail,this.pair(entry.key==='fontFamily'?['Свой шрифт или список запасных шрифтов','Custom font or fallback list']:['Своя толщина шрифта','Custom font weight']));
        control.detail.oninput=()=>control.oninput();compound.append(control,control.detail);
      }
      control.id=id;control.dataset.settingInput=entry.key;control.setAttribute('aria-describedby','settings-help');
      const help=this.description(entry);row.title=help;label.title=help;control.title=help;
      if(control.detail){control.detail.title=help;control.detail.setAttribute('aria-describedby','settings-help');control.detail.onfocus=()=>{this.help.textContent=help;};}
      control.onfocus=()=>{this.help.textContent=help;};row.onmouseenter=()=>{this.help.textContent=help;};
      if(this.invalid.has(entry.key)){if(control.fontChoice){control.value='custom';control.detail.value=this.invalid.get(entry.key);}else control.value=this.invalid.get(entry.key);control.setAttribute('aria-invalid','true');}
      control.oninput=()=>{this.readControl(entry,control);this.updatePreview();this.syncDependencies();};control.onchange=control.oninput;
      row.append(label,compound||control);this.controls.set(entry.key,{entry,control});this.content.append(row);
    }
    description(entry) {
      let text=this.ide.settings.language==='en'?entry.helpEn:entry.helpRu;
      if(entry.available===false)text+=' '+(this.ide.settings.language==='en'?entry.unavailableEn:entry.unavailableRu);
      if(entry.available!==false&&/^(codeLens|lightbulb|inlayHints|inlineSuggest|linkedEditing|renameOnType|formatOn|colorDecorators|occurrencesHighlight)/.test(entry.key))text+=this.text('provider');
      if(entry.kind==='number')text+=this.text('numbers')+(P.limits[entry.key]||[0,1000000]).join(' … ')+'.';
      if(entry.kind==='json'&&!['fontLigatures','fontVariations'].includes(entry.key))text+=this.text('json');return text;
    }
    valueText(value,entry) {
      const overrides={wordWrap:{on:['По ширине редактора','Editor width'],off:['Без переноса','No wrapping']},lineNumbers:{on:['Обычные','Absolute'],off:['Не показывать','Hidden']},cursorStyle:{line:['Вертикальная линия','Vertical line']},renderLineHighlight:{line:['Только строка','Line only']}};
      const pair=overrides[entry?.key]?.[String(value)]||valueLabels[String(value)];return pair?this.pair(pair):String(value);
    }
    readControl(entry,control) {
      if(entry.available===false)return true;
      let value;
      try {value=control.detail?(control.value==='custom'?control.detail.value:control.fontChoice?control.value:JSON.parse(control.value)):entry.kind==='boolean'?control.checked:entry.kind==='select'?JSON.parse(control.value):entry.kind==='number'?(control.value.trim()===''?NaN:Number(control.value)):entry.kind==='json'?JSON.parse(control.value):control.value;
        if(!P.valid(entry,value))throw new Error();
        this.draft[entry.key]=value;this.invalid.delete(entry.key);control.setAttribute('aria-invalid','false');return true;
      }catch{this.invalid.set(entry.key,control.detail&&control.value==='custom'?control.detail.value:control.value);control.setAttribute('aria-invalid','true');return false;}
    }
    syncDependencies() {
      const parents={'minimap':'minimap.enabled','stickyScroll':'stickyScroll.enabled','parameterHints':'parameterHints.enabled','inlineSuggest':'inlineSuggest.enabled','inlayHints':'inlayHints.enabled','hover':'hover.enabled','bracketPairColorization':'bracketPairColorization.enabled'};
      for(const {entry,control} of this.controls.values()) {
        const parent=parents[entry.key.split('.')[0]];
        UIControls.setEnabled(control,!(entry.available===false||!!parent&&entry.key!==parent&&(this.draft[parent]===false||this.draft[parent]==='off')));
        if(control.detail){UIControls.setEnabled(control.detail,!control.disabled&&control.value==='custom');if(control.fontChoice)control.detail.hidden=control.value!=='custom';}
      }
    }
    createPreview() {
      this.content.append(this.element('h3','settings-subheading',this.text('preview')));
      this.preview=this.element('pre','settings-preview');UIControls.setLabel(this.preview,this.text('preview'),{tooltip:false});
      this.preview.innerHTML='<span class="preview-comment">\' Axiom</span>\n<span class="preview-keyword">Dim</span> score <span class="preview-keyword">As</span> <span class="preview-type">Integer</span> = <span class="preview-number">42</span>\n<span class="preview-keyword">If</span> score &gt; <span class="preview-number">0</span> <span class="preview-keyword">Then</span>\n    <span class="preview-keyword">Print</span> <span class="preview-string">"Очки / Score: "</span>; score\n<span class="preview-keyword">End If</span>';
      this.content.append(this.preview);this.updatePreview();
    }
    updatePreview() {if(!this.preview?.isConnected)return;this.preview.dataset.previewTheme=this.theme;this.preview.style.fontFamily=this.draft.fontFamily;this.preview.style.fontSize=this.draft.fontSize+'px';this.preview.style.fontWeight=this.draft.fontWeight;this.preview.style.lineHeight=this.draft.lineHeight?(this.draft.lineHeight<8?String(this.draft.lineHeight):this.draft.lineHeight+'px'):'1.5';this.preview.style.letterSpacing=this.draft.letterSpacing+'px';this.preview.style.fontFeatureSettings=typeof this.draft.fontLigatures==='string'?this.draft.fontLigatures:this.draft.fontLigatures?'normal':'"liga" off, "calt" off';this.preview.style.fontVariationSettings=typeof this.draft.fontVariations==='string'?this.draft.fontVariations:'normal';}
    reset() {for(const entry of P.entries.filter(e=>e.group===this.group)){this.draft[entry.key]=P.clone(this.defaults[entry.key]);this.invalid.delete(entry.key);}if(this.group==='font')this.theme='dark';if(this.group==='appearance')this.language='ru';this.renderContent();this.status.textContent='';}
    async apply(close) {
      if(this.busy)return;
      for(const {entry,control} of this.controls.values())this.readControl(entry,control);
      if(this.invalid.size){const key=this.invalid.keys().next().value;const entry=P.entries.find(e=>e.key===key);this.selectTab(entry.group);this.status.textContent=this.text('error')+(this.ide.settings.language==='en'?entry.en:entry.ru);this.controls.get(key).control.focus();return;}
      this.busy=true;this.dialog.querySelectorAll('button,input,select,textarea').forEach(el=>UIControls.setEnabled(el,false));
      try {
        await this.ide.applyEditorPreferences(P.overrides(this.draft,this.defaults),this.theme,this.language);
        if(close){this.busy=false;this.close();return;}
        this.busy=false;this.render();this.status.textContent=this.text('saved');this.applyButton.focus();
      }catch(error){this.busy=false;this.dialog.querySelectorAll('button,input,select,textarea').forEach(el=>UIControls.setEnabled(el,true));this.syncDependencies();UIControls.setEnabled(this.resetButton,!this.query);this.status.textContent=this.text('saveError');this.ide.log('settings.save.failed',{message:String(error.message)},'error');}
    }
    keydown(e) {
      if(e.key==='Escape'){e.preventDefault();e.stopPropagation();this.close();return;}
      if(e.key==='Tab'){
        const focusable=[...this.dialog.querySelectorAll('button,input,select,textarea')].filter(el=>!el.disabled&&el.tabIndex>=0&&el.getClientRects().length);
        const first=focusable[0],last=focusable.at(-1);if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus();}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus();}
      }
    }
    close() {if(this.busy||!this.overlay)return;this.overlay.remove();this.overlay=null;this.app.inert=this.wasInert;if(this.previousFocus?.isConnected&&this.previousFocus!==document.body)this.previousFocus.focus();else this.ide.focusEditor();}
  }
  window.EditorSettingsDialog=EditorSettingsDialog;
  EditorSettingsDialog.valueLabels=valueLabels;
})();
