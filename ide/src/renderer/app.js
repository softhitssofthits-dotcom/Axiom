const API = window.axiom;

class AxiomIDE {
  constructor() {
    this.monaco = null;
    this.langMeta = null; // language registry from `axiom metadata`; null until loaded
    this.editor = null;
    this._recoveryEnabled = false;
    this._restoringSession = true;
    this.openFiles = new window.DocumentStore(() => {
      this.refreshDirtyIndicators(); this.persistRecovery();
      if(this.editor) this.scheduleLiveCheck();
      this.assistant?.invalidate();
    });
    this.editedLines = new window.EditedLines();
    this.documentServices = new window.DocumentServices(this.openFiles, API, () => this.projectSession?.entry, path => this.projectSession?.descriptor?.sourceFiles.some(p=>p.toLowerCase()===path.toLowerCase()) ? this.projectSession.descriptor.projectFilePath : null, () => this.projectSession?.analysisContext());
    this.documentSaver = new window.DocumentSaver(this.openFiles, API);
    this.dirty = new Set();
    this.activeFile = null;
    this.projectDir = null;
    this.projectSession = new window.ProjectSession(this, API);
    this.untitled = 0;
    this.running = false;
    this.term = null; // xterm.js Terminal — created lazily by ensureConsole()
    this.fitAddon = null;
    this._stopRequested = false;
    this._consoleT0 = null;
    this.fileChangePrompts = new Set(); // paths with an open external-change prompt
    this.fileChangeEvents = new Map();
    this._diagnosticFilter = 'all';
    // The program always runs in the integrated ConPTY terminal — the console is the only run mode.
    this.settings = { theme: 'dark', language: 'ru', recentFiles: [], projectExplorer: true };
    this._autoFormatting = false; // Защищает от повторного форматирования и регистрации автоматических правок как пользовательских.
    // One coordination state for all background editor services. Requests are cheap to
    // abandon, but their results must never win after a newer edit or a focus transition.
    this._serviceSeq = 0;
    this._formatBusy = 0;
    this._liveCheckSeq = 0;
    this._outlineSeq = 0;
    this._lastEditorFocus = 0;
    this.execution = new window.ExecutionController(this, API);
    this.projectFiles = new window.ProjectFileController(this, API);
    this.init();
  }

  log(event, data, level = 'info') {
    try { API.log?.(event, data, level); } catch {}
  }

  focusState() {
    const active = document.activeElement;
    const name = active && (active.id || active.className || active.tagName);
    return {
      active: typeof name === 'string' ? name : String(name || ''),
      editor: !!this.editor?.hasTextFocus?.(),
      terminal: !!this.term && active === this.term.textarea,
      menu: !!active?.closest?.('.monaco-menu, .context-view, .monaco-hover, .suggest-widget, .parameter-hints-widget, .peekview-widget, .xs-context-menu'),
      modal: !!active?.closest?.('.modal-overlay'),
    };
  }

  // ================= boot =================

  async init() {
    this.log('init.start');
    window.addEventListener('error', (e) => this.log('window.error', { message: e.message, source: e.filename, line: e.lineno, col: e.colno }, 'error'));
    window.addEventListener('unhandledrejection', (e) => this.log('window.unhandledrejection', { reason: String(e.reason?.stack || e.reason || '') }, 'error'));
    try {
      this.fileDrop = new window.EditorFileDrop(this, API);
      this.fileDrop.setup(document);
      await this.loadSettings();
      if (!window.AxiomThemes.has(this.settings.theme)) this.settings.theme = window.AxiomThemes.defaultId;
      this.applyDocumentTheme(this.settings.theme);
      I18N.setLocale(this.settings.language);
      await this.loadMonaco();
      await this.loadLanguageMetadata();
      // Подключаем службы до регистрации языка: первый вызов Monaco фиксирует их.
      this.createEditor();
      this.setupLanguage();
      this.assistant = new window.ProgramAssistant(this);
      this.monaco.editor.setModelLanguage(this.editor.getModel(),'axiom');
      this.monaco.editor.setTheme('axiom-'+this.settings.theme);
      // При первом создании эта служба Monaco задаёт собственное значение
      // озвучивания продолжений. После её создания восстанавливаем профиль.
      this.editor.getContribution('editor.contrib.inlineCompletionsController');
      this.editor.updateOptions(window.EditorPreferences.options(this.settings.editorPreferences || {}, {}, window.EditorPreferences.defaults(this.monaco)));
      this.setupEditorContextMenu();
      this.setupMenubar();
      this.setupToolbar();
      this.setToolbarVisible(this.settings.toolbarVisible !== false);
      this.setupProjectExplorer();
      this.studio = new window.StudioLayout(this);
      this.setupPanelTabs();
      document.querySelectorAll('[data-diagnostic-filter]').forEach(button => button.addEventListener('click', () => {
        this._diagnosticFilter = button.dataset.diagnosticFilter;
        this.updateDiagnosticFilters();
      }));
      this.setupDocumentTabs();
      this.setupPanelResizer();
      this.updateOutputMenuCheck();
      this.setupKeyboardShortcuts();
      this.setupConsoleEvents();
      await this.restoreTabs();
      await this.restoreProject();
      this._restoringSession = false;
      this.scheduleLiveCheck();
      if (!this.openFiles.size) this.newFile();
      this._recoveryEnabled = true;
      this.persistRecovery();
      this.renderRecentFiles();
      this.applyLocale();
      this.switchPanel(['console','diagnostics','assistant'].includes(this.settings.bottomPanelTab)?this.settings.bottomPanelTab:'console');
      API.onFileChanged((ev) => this.handleExternalChange(ev));
      document.getElementById('app')?.classList.add('ready');
      this.focusEditor();
      this.log('init.ready', { activeFile: this.activeFile });
    } catch (e) {
      this.log('init.failed', { message: e?.message || String(e) }, 'error');
      console.error('init failed', e);
      const problem=document.createElement('div');problem.style.cssText='padding:24px;font-family:Segoe UI,sans-serif;color:var(--text-primary)';
      problem.textContent='Не удалось открыть IDE: '+(e.message||String(e));document.body.replaceChildren(problem);
    }
  }

  async loadSettings() {
    try {
      const s = await API.getSettings();
      if (s) Object.assign(this.settings, s);
    } catch {}
    // Retired settings from earlier Electron profiles must not change editor behavior.
    for (const key of ['formatOnBlur', 'formatOnSave', 'checkOnChange']) delete this.settings[key];
    this.updateFormatMenuChecks();
    this.updateThemeMenuChecks();
  }

  saveSettings(patch) {
    Object.assign(this.settings, patch);
    API.saveSettings(patch).catch(() => {});
  }

  applyModelPreferences(model) {
    if (!model) return;
    const values = {...window.EditorPreferences.defaults(this.monaco), ...window.EditorPreferences.normalize(this.settings.editorPreferences)};
    if (values.detectIndentation) model.detectIndentation(values.insertSpaces, values.tabSize);
    else model.updateOptions({tabSize:values.tabSize, insertSpaces:values.insertSpaces});
    model.updateOptions({trimAutoWhitespace:values.trimAutoWhitespace});
    // Monaco renders bracket colors from model options, independently of editor options.
    model.updateOptions({bracketColorizationOptions:{
      enabled:values['bracketPairColorization.enabled'],
      independentColorPoolPerBracketType:values['bracketPairColorization.independentColorPoolPerBracketType'],
    }});
  }

  async applyEditorPreferences(preferences, theme, language) {
    const P=window.EditorPreferences;
    const normalized=P.normalize(preferences);
    const oldPreferences=this.settings.editorPreferences || {};
    const patch={editorPreferences:normalized, theme, language};
    const saved=await API.saveSettings(patch);
    if(saved?.ok === false) throw new Error('settings-write-failed');
    const changedLanguage=this.settings.language !== language;
    Object.assign(this.settings,patch);
    const defaults=P.defaults(this.monaco);
    this.editor.updateOptions(P.options(normalized,oldPreferences,defaults));
    for(const model of this.monaco.editor.getModels()) this.applyModelPreferences(model);
    // Порог Monarch меняется, но уже рассчитанные токены сам Monaco не сбрасывает.
    if((oldPreferences.maxTokenizationLineLength??defaults.maxTokenizationLineLength)!==(normalized.maxTokenizationLineLength??defaults.maxTokenizationLineLength)){
      for(const model of this.monaco.editor.getModels())model.tokenization?.resetTokenization();
    }
    // Эти параметры Monaco считывает при создании представления, но не обновляет
    // соответствующие расчёты при updateOptions. Модель и её история сохраняются.
    const rebuildKeys=['autoIndent','stickyTabStops','wordWrapBreakBeforeCharacters','wordWrapBreakAfterCharacters','wrapOnEscapedLineFeeds','scrollBeyondLastColumn','scrollPredominantAxis','scrollbar.alwaysConsumeMouseWheel','inertialScroll'];
    if(rebuildKeys.some(key=>(oldPreferences[key]??defaults[key])!==(normalized[key]??defaults[key]))){
      const model=this.editor.getModel(),state=this.editor.saveViewState(),wasFormatting=this._autoFormatting;
      if(model){
        this._autoFormatting=true;
        try {this.editor.setModel(null);this.editor.setModel(model);if(state)this.editor.restoreViewState(state);}
        finally {this._autoFormatting=wasFormatting;}
      }
    }
    this.applyDocumentTheme(theme);
    this.monaco.editor.setTheme('axiom-'+theme);
    if(this.term) this.term.options.theme=this.consoleTheme();
    this.updateThemeMenuChecks();
    I18N.setLocale(language);this.applyLocale();
    if(changedLanguage) { if(this._lastDiagnostics)this.applyDiagnostics(this._lastDiagnostics,this._lastDiagnosticsOnlyActive,this._lastDiagnosticsTargetFile);this.scheduleLiveCheck(); }
    this.log('settings.editor.applied',{count:Object.keys(normalized).length,theme,language});
  }

  loadMonaco() {
    return new Promise((resolve, reject) => {
      let settled = false;
      const done = (ok) => { if (!settled) { settled = true; ok ? resolve() : reject(new Error('monaco load')); } };
      const timer = setTimeout(() => done(false), 15000);
      // Monaco's own UI strings intentionally stay English in both locales:
      // only compiler diagnostics and the IDE's own UI are localized.
      import('./monaco/monaco.js').then(({ monaco }) => {
        clearTimeout(timer); window.monaco = this.monaco = monaco; done(true);
      }, (error) => {
        clearTimeout(timer); this.log('monaco.load.failed', { message: String(error) }, 'error'); done(false);
      });
    });
  }

  // ================= language (highlighting) =================

  // Monarch dictionaries come from `axiom metadata` (the language registry of the CLI).
  // Embedded lists below are only a fallback when axiom.exe is missing or broken.
  static FALLBACK_DICTS = {
    keywords: ['AND','APPEND','AS','BEGINFRAME','BYREF','BYVAL','CAMERA','CANVAS','CASE','CATCH','CIRCLE','CLOSE','CLS','COLOR','CONST','CONTINUE','DIM','DO','DRAW','DRAWCIRCLE','DRAWIMAGE','DRAWLINE','DRAWRECT','DRAWSPRITE','DRAWTILE','DRAWTILELAYER','DRAWTILEMAP','DRAWTEXT','EACH','ELSE','ELSEIF','END','ERROR','EQV','ERASE','EXIT','FALSE','TRUE','FILLCIRCLE','FILLRECT','FINALLY','FOR','FUNCTION','GET','IF','IMP','IMPORT','IN','INPUT','IS','KEY','LINE','LOCATE','LOOP','MOD','MOUSE','NEXT','NOT','OFF','ON','OPEN','OR','OUTPUT','PAINT','PALETTE','PCOPY','PRESERVE','PRESENT','PRESET','PRINT','PRIVATE','PUBLIC','PSET','PUT','REDIM','RESETCAMERA','RETURN','SCREEN','SELECT','SELF','SETMAPTILE','SMOOTH','SLEEP','STEP','STOP','SUB','THEN','TO','TRANSPARENT','TRY','TYPE','UNTIL','USING','VIEW','VIEWPORT','WHILE','WINDOW','XOR'],
    typeNames: ['BOOLEAN','DATE','DATETIME','DECIMAL','DICTIONARY','DOUBLE','FILE','HASHSET','IMAGE','INTEGER','LONG','SPRITE','STRING','TILEMAP','TILESET','TIME','UTCDATETIME'],
    builtins: ['ABS','ASC','CHR','DATE','DATEADD','DATEDIFF','DAY','EOF','EXP','FILEEXISTS','FLOOR','INSTR','INT','JOIN','LCASE','LEFT','LEN','LOG','LTRIM','MID','MIN','MAX','MONTH','NOW','POW','READFILE','REPLACE','RGB','RIGHT','RND','ROUND','SGN','SIN','SQR','STR','SUBSTRCOUNT','TAN','TIME','TIMER','TRIM','TYPE','UCASE','VAL','WEEKDAY','WRITEFILE','YEAR'],
  };

  async loadLanguageMetadata() {
    const result=await API.getLanguageMetadata();
    if(!result?.ok||!result.metadata)throw new Error(result?.error?.message||'Не получен реестр языка DesktopHost.');
    this.langMeta=result.metadata;
  }

  static dictList(value, fallback) {
    return Array.isArray(value) && value.length > 0 && value.every((x) => typeof x === 'string') ? value.slice() : fallback;
  }

  setupLanguage() {
    const monaco = this.monaco;
    monaco.languages.register({ id: 'axiom' });

    const meta = this.langMeta;
    const keywords = AxiomIDE.dictList(meta && meta.keywords, AxiomIDE.FALLBACK_DICTS.keywords);
    const typeKeywords = AxiomIDE.dictList(meta && meta.typeNames, AxiomIDE.FALLBACK_DICTS.typeNames);
    const builtins = AxiomIDE.dictList(
      meta && Array.isArray(meta.builtins) ? meta.builtins.map((b) => (b && typeof b.name === 'string' ? b.name : null)).filter(Boolean) : null,
      AxiomIDE.FALLBACK_DICTS.builtins
    );

    const codeTokens = [
      [/&H[0-9a-fA-F]+(?![a-zA-Z0-9_])/, 'number.hex'],
      [/&B[01]+(?![a-zA-Z0-9_])/, 'number.binary'],
      [/&[HB][a-zA-Z0-9_]*/, 'invalid'],
      [/\d+\.\d+/, 'number'],
      [/\d+/, 'number'],
      [/<>|<=|>=|<<|>>|=|<|>/, 'operator'],
      [/\+|-|\*|\/|\\|\^|&/, 'operator'],
      [/[()[\]]/, '@brackets'],
      // После точки стоит имя члена: совпадение с именем самостоятельной
      // встроенной функции не меняет его категорию (например, Contains).
      [/(\.)(\s*)([a-zA-Z_][a-zA-Z0-9_]*)/, ['delimiter', 'white', 'identifier']],
      [/[,;:.]/, 'delimiter'],
      [/[#%$]/, 'invalid'],
      [/[^\x00-\x7F\s]+/, 'invalid'],
      [/[a-zA-Z_][a-zA-Z0-9_]*/, {
        cases: {
          '@keywords': 'keyword',
          '@typeKeywords': 'type',
          '@builtins': 'support.function',
          '@default': 'identifier',
        },
      }],
    ];

    // BASIC: case-insensitive; ' and REM comments; "..." with "" escapes; $"..." with
    // {expression} holes; &H and &B integer literals; decimal numbers.
    monaco.languages.setMonarchTokensProvider('axiom', {
      ignoreCase: true,
      defaultToken: '',
      keywords,
      typeKeywords,
      builtins,
      tokenizer: {
        root: [
          [/(rem\b).*/, 'comment'],
          [/'.*$/, 'comment'],
          [/(\$)(")/, ['delimiter.interpolation', { token: 'string.quote', bracket: '@open', next: '@interpolatedString' }]],
          // Литерал без закрывающей кавычки на этой же строке: подсветка не переносит
          // его на следующую строку, как и компилятор (MB0002).
          [/"(?:""|[^"])*$/, 'string'],
          [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],
          ...codeTokens,
        ],
        string: [
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          // Monaco разбирает каждую строку отдельно: остаток строки без закрывающей
          // кавычки завершает состояние, как это делает компилятор на конце строки.
          [/[^"]+$/, { token: 'string', next: '@pop' }],
          [/[^"]+/, 'string'],
        ],
        // $"...{expression}...": the hole is highlighted as code, the markers as delimiters.
        // Удвоенные скобки и кавычки — текст, как и в компиляторе.
        interpolatedString: [
          [/\{\{/, 'string.escape'],
          [/\}\}/, 'string.escape'],
          [/\{/, { token: 'delimiter.interpolation', bracket: '@open', next: '@interpolation' }],
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          [/[^"{}]+$/, { token: 'string', next: '@pop' }],
          [/[^"{}]+/, 'string'],
          [/\}/, 'string'],
        ],
        interpolation: [
          [/(\$)(")/, ['delimiter.interpolation', { token: 'string.quote', bracket: '@open', next: '@interpolatedString' }]],
          [/"/, { token: 'string.quote', bracket: '@open', next: '@nestedString' }],
          [/\{/, { token: '@brackets', bracket: '@open', next: '@push' }],
          [/\}/, { token: 'delimiter.interpolation', bracket: '@close', next: '@pop' }],
          ...codeTokens,
          [/\s+/, 'white'],
        ],
        nestedString: [
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          [/[^"]+$/, { token: 'string', next: '@pop' }],
          [/[^"]+/, 'string'],
        ],
      },
    });

    monaco.languages.registerDocumentSemanticTokensProvider('axiom', {
      getLegend: () => window.AxiomSemanticColors.legend,
      provideDocumentSemanticTokens: async (model, lastResultId, cancellation) => {
        const version = model.getVersionId(), source = model.getValue();
        const symbols = await window.LangFeatures?.getSemanticHovers?.(model);
        if (model.isDisposed() || model.getVersionId() !== version || cancellation?.isCancellationRequested) return null;
        return window.AxiomSemanticColors.classify(source, monaco.editor.tokenize(source, 'axiom'), symbols || []);
      },
      releaseDocumentSemanticTokens() {},
    });

    // Вызовы/группировка и массивы имеют собственные пары для сопоставления и раскраски.
    monaco.languages.setLanguageConfiguration('axiom', {
      comments: { lineComment: "'" },
      brackets: [['(', ')'], ['[', ']']],
      colorizedBracketPairs: [['(', ')'], ['[', ']']],
      folding: { markers: { start: /^\s*'\s*#region\s+\S.*$/i, end: /^\s*'\s*#endregion\s*$/i } },
      indentationRules: {
        increaseIndentPattern: /^\s*(?:(?:If|ElseIf)\b.*\bThen|Else|For\b.*|While\b.*|Do(?:\s+(?:While|Until)\b.*)?|(?:Sub|Function|Type)\b.*|Select\s+Case\b.*|Case\b.*|Try|Catch\b.*|Finally)\s*(?:'.*)?$/i,
        decreaseIndentPattern: /^\s*(?:End\s+(?:If|While|Sub|Function|Type|Select|Try)\b|Next\b|Loop\b|Else(?:If)?\b|Case\b|Catch\b|Finally\b)/i,
      },
      autoClosingPairs: [
        { open: '(', close: ')' },
        { open: '[', close: ']' },
        { open: '"', close: '"' },
      ],
      surroundingPairs: [
        { open: '(', close: ')' },
        { open: '[', close: ']' },
        { open: '"', close: '"' },
      ],
    });

    // Themes are generated from the shared registry (themes.js): one description
    // per theme instead of separate copies in Monaco, xterm and the CSS preview.
    for (const def of window.AxiomThemes.list) {
      monaco.editor.defineTheme('axiom-' + def.id, window.AxiomThemes.monacoTheme(def.id));
    }

    // Completion + signature help + hover — builtins from the language registry
    // (ide.langMeta, loaded from `axiom metadata`), document locals from a light model scan.
    if (window.LangFeatures) LangFeatures.register(monaco, this);
  }

  createEditor() {
    this.editor = this.monaco.editor.create(document.getElementById('editor'), {
      value: '',
      language: 'plaintext',
      theme: window.AxiomThemes.byId(this.settings.theme).monacoBase,
      automaticLayout: true,
      'semanticHighlighting.enabled': true,
      fontSize: 14,
      fontFamily: "'Cascadia Code', Consolas, 'Courier New', monospace",
      minimap: { enabled: false },
      // Monaco выбирает место по границам окна. Фиксированное позиционирование
      // выводит подсказки за overflow:hidden редактора, в том числе поверх консоли.
      fixedOverflowWidgets: true,
      hover: { above: false },
      scrollBeyondLastLine: false,
      renderLineHighlight: 'none',
      tabSize: 4,
      insertSpaces: true,
      acceptSuggestionOnEnter: 'off',
      ...window.EditorPreferences.options(this.settings.editorPreferences || {}, {}, window.EditorPreferences.defaults(this.monaco)),
    }, {IWorkspaceEditService: window.EditorWorkspaceEdits.create(this)});

    this.trace = new window.TraceController(this);
    this.editor.onDidChangeCursorPosition((e) => {
      document.getElementById('status-line-col').textContent = `Ln ${e.position.lineNumber}, Col ${e.position.column}`;
      this.outlinePanel?.updateCurrent();
      this.log('editor.cursor', { line: e.position.lineNumber, col: e.position.column, reason: e.reason });
      const model=this.editor.getModel(),previous=this._structuralLine;
      const sameModel=this._structuralModel===model;
      this._structuralLine=e.position.lineNumber;this._structuralModel=model;
      if(sameModel&&previous!==e.position.lineNumber&&this.editedLines.has(model,previous))this.editLineTransition(model,previous,e.position);
    });
    this.editor.onDidFocusEditorText(() => {
      this._lastEditorFocus = performance.now();
      this.log('editor.focus', this.focusState());
    });
    this.editor.onDidBlurEditorText(() => {
      if (this.settings.smartIdentifierEnabled) this.formatWholeDocument('ax-smart-blur', this.activeFile, 'document.normalizeIdentifiers');
    });
    this.editor.onDidChangeModelContent((event) => {
      this.clearUsageAdvice();
      this.refreshExecutionDiagnostics(this.editor.getModel());
      // Замена документа целиком (setValue, восстановление, откат) не является правкой строки:
      // отметки прежнего текста не должны запускать структурное завершение блока.
      this.editedLines.apply(this.editor.getModel(),event,!this._autoFormatting);
      if (this.activeFile) this.markDirty(this.activeFile, true);
      if (this.activeFile && !this.openFiles.isDirty(this.activeFile)) this.editedLines.clear(this.editor.getModel());
      this.scheduleLiveCheck();
      this.scheduleOutlineRebuild();
      this.log('editor.change', { length: this.editor.getValue().length, activeFile: this.activeFile });
    });
    // Double-click on an error squiggle opens the same details peek as Alt+F8.
    this.editor.onMouseDown((e) => {
      if (!e.target || !e.target.position) return;
      if ((e.event && e.event.detail ? e.event.detail : 1) < 2) return;
      const marker = this.findMarkerAt(e.target.position);
      if (marker) this.showErrorDetailsForMarker(marker);
    });
    // Alt+F8 (VS Code "Go to Problem"): show the details peek for the marker under the
    // caret, or the next one after it.
    this.editor.addCommand(this.monaco.KeyMod.Alt | this.monaco.KeyCode.F8, () => this.showMarkerPeekAtCaret());
    // Close Monaco context menu when clicking outside it. A real open menu has item
    // rows; the empty .context-view shell is permanently in the DOM and must never be
    // treated as an open menu (and never removed): detaching it kills Monaco's context
    // view for the whole session.
    document.addEventListener('mousedown', (e) => {
      const menu = document.querySelector('.monaco-menu');
      if (menu && menu.children.length > 0 && !menu.contains(e.target)) {
        menu.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
        setTimeout(() => {
          if (menu.isConnected && menu.children.length > 0 && !menu.contains(document.activeElement)) menu.remove();
        }, 50);
      }
    }, true);
    this.rebuildOutline();
  }

  setupEditorContextMenu() {
    const menu = document.getElementById('ax-context-menu');
    if (!menu) return;
    const items = [
      { id: 'change-all', translation: 'menu.editor.changeAll', key: 'Ctrl+F2', icon: 'codicon-edit' },
      { separator: true },
      { id: 'cut', translation: 'menu.edit.cut', icon: 'codicon-screen-cut' },
      { id: 'copy', translation: 'menu.edit.copy', icon: 'codicon-copy' },
      { id: 'paste', translation: 'menu.edit.paste', icon: 'codicon-files' },
      { separator: true },
      { id: 'trace-breakpoint', translation: 'trace.breakpoint', key: 'F9', icon: 'codicon-debug-breakpoint', trace: 'breakpoint' },
      { id: 'trace-clear', translation: 'trace.clear', key: 'Ctrl+Shift+F9', icon: 'codicon-clear-all', trace: 'clear' },
      { separator: true },
      { id: 'assistant-incoming', translation: 'assistant.incoming', icon: 'codicon-references' },
      { id: 'assistant-outgoing', translation: 'assistant.outgoing', icon: 'codicon-call-outgoing' },
      { id: 'assistant-preview', translation: 'assistant.preview', icon: 'codicon-preview' },
      { id: 'assistant-createRegion', translation: 'assistant.createRegion', icon: 'codicon-group-by-ref-type' },
      { separator: true },
      { id: 'command-palette', translation: 'menu.editor.commandPalette', key: 'F1', icon: 'codicon-symbol-misc' },
    ];
    let contextLine, contextItem, contextVersion;
    const popup = UIControls.createContextMenu(menu, {translate: I18N.t,
      onClose: () => this.editor?.focus(), onAction: id => run(id)});
    const run = (id) => {
      // The menu button temporarily owns focus. Restore Monaco first.
      this.editor?.focus();
      if (id === 'trace-breakpoint') { if (this.trace.allowed('breakpoint')) this.trace.toggle(contextLine); }
      else if (id === 'trace-clear') this.trace.command('clear');
      else if (id === 'change-all') this.editor?.trigger('ax-context', 'editor.action.changeAllOccurrences');
      else if (id === 'command-palette') this.editor?.trigger('ax-context', 'editor.action.quickCommand');
      else if (id.startsWith('assistant-')) {
        if(this.editor.getModel().getVersionId()!==contextVersion)return;
        const action=id.slice('assistant-'.length);
        if(action==='createRegion')this.assistant.regionDialog(contextItem);
        else this.assistant.show(action,contextItem);
      }
      else this.handleAction(id);
    };
    document.addEventListener('contextmenu', (event) => {
      if (!document.getElementById('editor').contains(event.target)) return;
      event.preventDefault();
      event.stopPropagation();
      contextLine = this.editor.getTargetAtClientPoint(event.clientX, event.clientY)?.position?.lineNumber;
      contextVersion = this.editor.getModel().getVersionId();
      contextItem = this.outlinePanel?.items.findLast(item=>contextLine>=item.lineNo&&contextLine<=item.endLineNo)||null;
      popup.show(items.map(item => ({...item,
        enabled: item.id==='assistant-createRegion' ? !this.editor.getRawOptions().readOnly : item.id==='assistant-preview' ? !!contextItem : !item.trace || (this.trace.allowed(item.trace) && (item.trace !== 'breakpoint' || !!contextLine))
      })), event.clientX, event.clientY);
    }, true);
  }

  // ================= UI binding =================

  setupMenubar() {
    let openMenu = null;
    const closeSubmenus = () => document.querySelectorAll('.menu-item-submenu.open').forEach(item => {
      item.classList.remove('open');
      UIControls.setExpanded(item.querySelector('.submenu-trigger'), false);
    });
    const closeAll = () => {
      closeSubmenus();
      document.querySelectorAll('.menu-item.open').forEach(item => item.classList.remove('open'));
      openMenu = null;
      document.querySelectorAll('.menu-item > .menu-label').forEach(label=>UIControls.setExpanded(label,false));
    };
    const openMenuFor = item => { closeAll(); item.classList.add('open'); openMenu = item; UIControls.setExpanded(item.querySelector('.menu-label'),true); };
    const rows=item=>[...item.querySelectorAll('.menu-dropdown-item')].filter(el=>el.offsetWidth&&el.getAttribute('aria-disabled')!=='true');
    document.getElementById('menu-settings').addEventListener('click', () => {
      closeAll(); this.handleAction('editor-settings');
    });
    document.querySelectorAll('.menu-item').forEach(item => {
      const label = item.querySelector('.menu-label'); if (!label) return;
      label.tabIndex=0;label.setAttribute('role','button');label.setAttribute('aria-haspopup','menu');UIControls.setExpanded(label,false);
      label.addEventListener('keydown',e=>{if(['Enter',' ','ArrowDown','ArrowUp'].includes(e.key)){e.preventDefault();openMenuFor(item);const items=rows(item);(e.key==='ArrowUp'?items.at(-1):items[0])?.focus();}});
      label.addEventListener('mousedown', e => { e.preventDefault(); item === openMenu ? closeAll() : openMenuFor(item); });
      label.addEventListener('mouseenter', () => { if (openMenu && openMenu !== item) openMenuFor(item); });
    });
    document.querySelectorAll('.menu-item-submenu').forEach(item => {
      const trigger = item.querySelector('.submenu-trigger'); const dropdown = item.querySelector('.submenu-dropdown');
      trigger.tabIndex = 0; trigger.setAttribute('aria-haspopup', 'menu'); UIControls.setExpanded(trigger, false);
      const open = () => {
        closeSubmenus(); item.classList.add('open'); UIControls.setExpanded(trigger, true);
        const anchor = trigger.getBoundingClientRect(); const size = dropdown.getBoundingClientRect();
        dropdown.style.position = 'fixed';
        const right = anchor.right; const left = right + size.width <= innerWidth - 8 ? right : anchor.left - size.width;
        dropdown.style.left = Math.max(8, Math.min(left, innerWidth - size.width - 8)) + 'px';
        dropdown.style.top = Math.max(8, Math.min(anchor.top, innerHeight - size.height - 8)) + 'px';
      };
      trigger.addEventListener('mouseenter', open);
      trigger.addEventListener('click', e => { e.stopPropagation(); open(); });
      trigger.addEventListener('keydown', e => { if (['Enter', ' ', 'ArrowRight'].includes(e.key)) { e.preventDefault(); open(); rows(dropdown)[0]?.focus(); } });
    });
    document.querySelectorAll('.menu-dropdown-item').forEach(item => {item.tabIndex=-1;if(!item.hasAttribute('role'))item.setAttribute('role','menuitem');item.addEventListener('click', () => {
      if(item.getAttribute('aria-disabled')==='true')return;
      const action = item.dataset.action; if (!action) return;
      closeAll(); this.handleAction(action);
    });});
    document.addEventListener('keydown',e=>{
      if(e.defaultPrevented||!openMenu||!openMenu.contains(document.activeElement))return;
      const active=document.activeElement,submenu=active.closest('.submenu-dropdown');
      if(e.key==='Escape'){e.preventDefault();const label=openMenu.querySelector('.menu-label');closeAll();label.focus();return;}
      if(e.key==='ArrowLeft'&&submenu){e.preventDefault();const parent=submenu.parentElement;parent.classList.remove('open');UIControls.setExpanded(parent.querySelector('.submenu-trigger'),false);parent.querySelector('.submenu-trigger').focus();return;}
      if(['ArrowDown','ArrowUp','Home','End'].includes(e.key)){
        e.preventDefault();const items=rows(submenu||openMenu),index=items.indexOf(active);
        const next=e.key==='Home'?0:e.key==='End'?items.length-1:(index+(e.key==='ArrowDown'?1:-1)+items.length)%items.length;
        items[next]?.focus();return;
      }
      if((e.key==='Enter'||e.key===' ')&&active.matches('.menu-dropdown-item')){e.preventDefault();active.click();if(!openMenu)this.focusEditor();}
    });
    document.addEventListener('mousedown', e => { if (openMenu && !openMenu.contains(e.target)) closeAll(); });
    document.addEventListener('click', e => { if (openMenu?.contains(e.target) && e.target.closest('.recent-item')) closeAll(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeAll(); });
    window.addEventListener('resize', closeAll);
  }

  setupToolbar() {
    document.getElementById('tb-new').onclick = () => this.handleAction('new');
    document.getElementById('tb-open').onclick = () => this.handleAction('open');
    document.getElementById('tb-save').onclick = () => this.handleAction('save');
    document.getElementById('tb-run').onclick = () => this.handleAction('run');
    document.getElementById('tb-stop').onclick = () => this.handleAction('stop');
    document.getElementById('tb-check').onclick = () => this.handleAction('check');
    document.getElementById('tb-format').onclick = () => this.handleAction('format');
    document.getElementById('tb-find').onclick = () => this.handleAction('find');
    document.getElementById('tb-toggle-output').onclick = () => this.handleAction('toggle-output');
    const refreshHistory = () => queueMicrotask(() => this.updateHistoryButtons());
    this.editor.onDidChangeModel(refreshHistory);
    this.editor.onDidChangeModelContent(refreshHistory);
    this.editor.onDidChangeConfiguration(refreshHistory);
    this.updateHistoryButtons();
  }

  updateHistoryButtons() {
    const model = this.editor.getModel();
    const editable = model && !this.editor.getOption(this.monaco.editor.EditorOption.readOnly);
    UIControls.setEnabled(document.getElementById('tb-undo'), editable && model.canUndo());
    UIControls.setEnabled(document.getElementById('tb-redo'), editable && model.canRedo());
  }

  setupPanelTabs() {
    document.querySelectorAll('.panel-tab').forEach((tab) => {
      tab.addEventListener('click', (e) => {
          const name = tab.dataset.tab;
        if (name) this.switchPanel(name);
      });
    });
  }

  // Both panels use the same pointer, cancellation and size constraints.
  setupPanelResizer() {
    const {PanelSplitter} = window.PanelSplitters;
    this.bottomSplitter = new PanelSplitter({handle: document.getElementById('panel-resizer'), panel: document.getElementById('bottom-panel'), container: document.getElementById('main-content'), axis: 'y', minimum: 64, reserve: 140, defaultSize: 200, savedSize: this.settings.consoleHeight, onCommit: size => this.saveSettings({consoleHeight: size})});
    this.sidebarSplitter = new PanelSplitter({handle: document.getElementById('sidebar-resizer'), panel: document.getElementById('shell-sidebar'), container: document.getElementById('workspace'), axis: 'x', minimum: 180, reserve: 160, maximum: 480, defaultSize: 300, savedSize: this.settings.sidebarWidth, onCommit: size => this.saveSettings({sidebarWidth: size})});
    document.getElementById('sidebar-close').onclick = () => this.setSidebarVisible(false);
    document.getElementById('tb-toggle-sidebar').onclick = () => this.setSidebarVisible(this.settings.sidebarVisible === false);
    this.setSidebarVisible(this.settings.sidebarVisible !== false);
    this.setBottomPanelVisible(this.settings.bottomPanelVisible !== false);
  }

  setSidebarVisible(visible) {
    if (!visible && document.getElementById('shell-sidebar').contains(document.activeElement)) this.focusEditor();
    this.sidebarSplitter?.cancel();
    this.projectSplitter?.cancel();
    document.getElementById('shell-sidebar').hidden = !visible;
    document.getElementById('sidebar-resizer').hidden = !visible;
    this.saveSettings({sidebarVisible: visible});
    UIControls.setMenuChecked(document.querySelector('[data-check="sidebarVisible"]'), visible);
    UIControls.setPressed(document.getElementById('tb-toggle-sidebar'), visible);
    UIControls.setPressed(document.getElementById('tb-toggle-project'), visible && this.settings.projectExplorer !== false);
    this.sidebarSplitter?.layout();
    this.studio?.render();
    requestAnimationFrame(()=>this.editor?.layout());
  }

  setToolbarVisible(visible) {
    document.getElementById('toolbar').hidden = !visible;
    this.saveSettings({toolbarVisible: visible});
    UIControls.setMenuChecked(document.querySelector('[data-check="toolbarVisible"]'), visible);
    requestAnimationFrame(() => { this.editor?.layout(); this.fitAddon?.fit(); });
  }

  static shortcutKey(event) {
    return /^Key[A-Z]$/.test(event.code) ? event.code.slice(3).toLowerCase() : event.key.toLowerCase();
  }

  static shellShortcut(event, key) {
    if (!event.ctrlKey && !event.metaKey) return null;
    if (event.code === 'Comma') return 'editor-settings';
    if (event.shiftKey && event.altKey && key === 'f') return 'format';
    if (event.shiftKey) {
      switch (key) {
        case 's': return 'save-as';
        case 'c': return 'check';
        case 'e': return 'toggle-project-explorer';
        case 'o': return 'toggle-output';
      }
    }
    if (!event.shiftKey && key === 'b') return 'toggle-sidebar';
    switch (key) {
      case 's': return 'save';
      case 'n': return 'new';
      case 'o': return 'open';
      case 'w': return 'close-tab';
      default: return null;
    }
  }

  static traceShortcut(event) {
    if (event.altKey) return null;
    const mod = event.ctrlKey || event.metaKey;
    switch (event.key) {
      case 'F5':
        if (mod && !event.shiftKey) return 'run';
        return event.shiftKey ? (mod ? 'restart' : 'stop') : 'start';
      case 'F9': return mod && event.shiftKey ? 'clear' : 'breakpoint';
      case 'F10': return mod ? 'stepBack' : 'stepOver';
      case 'F11': return event.shiftKey ? 'stepOut' : 'stepInto';
      default: return null;
    }
  }

  setupKeyboardShortcuts() {
    // Capture phase: the integrated terminal's keydown handling stops the event before
    // bubble-phase listeners once xterm has focus, so F5 / Ctrl chords would never reach
    // the IDE while the console is focused.
    window.addEventListener('keydown', (e) => {
      if (document.querySelector('.editor-settings-overlay, #project-create-dialog')) return;
      // `event.code` keeps physical shortcuts independent of the keyboard layout.
      const shortcutKey = AxiomIDE.shortcutKey(e);
      const action = AxiomIDE.shellShortcut(e, shortcutKey);
      const consume = () => { e.preventDefault(); e.stopPropagation(); };
      if (action === 'editor-settings') { consume(); this.handleAction(action); return; }
      const mod = e.ctrlKey || e.metaKey;
      this.log('keyboard.down', { key: e.key, code: e.code, target: e.target?.id || e.target?.className || e.target?.tagName, mod, shift: e.shiftKey, alt: e.altKey, state: this.focusState() });

      // IDE shortcuts are consumed in the capture phase: preventDefault alone still lets
      // xterm see the key and echo its escape sequence into the running program's stdin.
      if (action) { consume(); this.handleAction(action); return; }
      // Terminal clipboard: Ctrl+C copies selected text only; without a selection it is
      // consumed as a no-op. Ctrl+V pastes sanitized text. Both act only while the
      // terminal has focus — the editor keeps its own Monaco copy/paste.
      if (mod && !e.shiftKey && shortcutKey === 'c' && this.term && document.activeElement === this.term.textarea) {
        if (this.term.hasSelection()) {
          consume();
          const selected = this.term.getSelection();
          this.log('terminal.copy.request', { length: selected.length });
          const write = API.writeClipboardText ? API.writeClipboardText(selected) : Promise.resolve(false);
          write.then((ok) => {
            if (!ok) this.log('terminal.copy.failed', {}, 'error');
          }).catch((error) => this.log('terminal.copy.exception', { message: error?.message || String(error) }, 'error'));
        } else {
          consume();
          this.log('terminal.copy.skipped', { reason: 'no-selection' });
        }
        return; // Ctrl+C is always consumed in the integrated console
      }
      if (mod && !e.shiftKey && shortcutKey === 'v' && this.term && document.activeElement === this.term.textarea) {
        consume();
        // enqueued: the clipboard read is async, and keystrokes pressed right after
        // Ctrl+V (Enter!) must not overtake the pasted text.
        this.queueConsoleWrite(() =>
          API.readClipboardText()
            .then((t) => this.writeConsolePaste(t))
            .catch(() => {}));
        return;
      }
      const traceAction = AxiomIDE.traceShortcut(e);
      if (traceAction) {
        consume();
        if (traceAction === 'run') this.run('run');
        else this.trace.command(traceAction);
        return;
      }
    }, true);

    window.addEventListener('beforeunload', () => this.saveOpenTabs(true));
    API.onWindowCloseRequested(() => this.prepareWindowClose().catch(error => { this.setStatus(I18N.t("status.saveFailed")); alert(error.message); }));

    // When the window regains focus, put it back on the editor unless focus is
    // already on a real input.
    window.addEventListener('focus', () => {
      const state = this.focusState();
      this.log('window.focus.renderer', state);
      const a = document.activeElement;
      if (!this.isTransientUiFocus() && (!a || a === document.body || a.tagName === 'BODY')) this.focusEditor();
    });
  }

  isTransientUiFocus() {
    const active = document.activeElement;
    if (!active) return false;
    if (this.term && active === this.term.textarea) return true;
    return !!active.closest?.('.monaco-menu, .context-view, .monaco-hover, .suggest-widget, .parameter-hints-widget, .peekview-widget, .modal-overlay');
  }

  handleAction(action) {
    if (action.startsWith('trace-')) return this.trace.command(action.slice(6));
    if (action === 'smart-identifier') {
      this.saveSettings({smartIdentifierEnabled:!this.settings.smartIdentifierEnabled});
      this.updateFormatMenuChecks();return;
    }
    const editor = this.editor;
    switch (action) {
      case 'new': this.newFile(); break;
      case 'new-project-file': this.newProjectFile(); break;
      case 'add-existing-project-file': return this.addExistingProjectFile();
      case 'open': this.openDialog(); break;
      case 'create-project': (this.projectCreateDialog ||= new ProjectCreateDialog(this)).show(); break;
      case 'open-project': this.openProjectDialog(); break;
      case 'close-project': this.closeProject(); break;
      case 'save': this.save(); break;
      case 'save-as': this.saveAs(); break;
      case 'save-all': this.saveAll(); break;
      case 'close-tab': if (this.activeFile) this.closeFile(this.activeFile); break;
      case 'exit': window.close(); break;
      case 'undo': editor?.trigger('menu', 'undo'); this.focusEditor(); break;
      case 'redo': editor?.trigger('menu', 'redo'); this.focusEditor(); break;
      case 'cut': editor?.trigger('menu', 'editor.action.clipboardCutAction'); this.focusEditor(); break;
      case 'copy': editor?.trigger('menu', 'editor.action.clipboardCopyAction'); this.focusEditor(); break;
      case 'paste': editor?.trigger('menu', 'editor.action.clipboardPasteAction'); this.focusEditor(); break;
      case 'find': editor?.getAction('actions.find')?.run(); break;
      case 'replace': editor?.getAction('editor.action.startFindReplaceAction')?.run(); break;
      case 'format': this.formatDocument(); break;
      case 'format-classic': this.setFormattingStyle('Classic'); break;
      case 'format-modern': this.setFormattingStyle('Modern'); break;
      case 'run': this.run(); break;
      case 'check': this.check(true); break;
      case 'stop': this.stop(); break;
      case 'toggle-output': this.togglePanel(); break;
      case 'toggle-toolbar': this.setToolbarVisible(this.settings.toolbarVisible === false); break;
      case 'toggle-sidebar': this.setSidebarVisible(this.settings.sidebarVisible === false); break;
      case 'run-current-file': return this.run('run',true,true);
      case 'open-project-file': return this.openProjectFileDialog();
      case 'create-project-descriptor': return window.ProjectView.create(this);
      case 'project-properties': return window.ProjectView.properties(this);
      case 'toggle-project-explorer': this.toggleProjectExplorer(); break;
      case 'show-project': this.studio.select('project'); break;
      case 'show-outline': this.studio.select('outline'); break;
      case 'panel-console': this.showPanel(); this.switchPanel('console'); break;
      case 'panel-diagnostics': this.showPanel(); this.switchPanel('diagnostics'); break;
      case 'panel-assistant': this.showPanel(); this.switchPanel('assistant'); break;
      case 'reset-layout': this.studio.reset(); break;
      case 'refresh-project': if(this.projectDir)this.refreshProjectFiles(); break;
      case 'choose-project-entry': if(this.projectSession.descriptor)return window.ProjectView.properties(this); this.setToolbarVisible(true); document.getElementById('project-entry').focus(); break;
      case 'use-current-entry': if(this.projectSession.isSource(this.activeFile))Promise.resolve(this.projectSession.select(this.activeFile)).catch(error=>this.showIdeProblem('project.failed',error.message)); break;
      case 'toggle-statusbar': this.setStatusBarVisible(this.settings.statusBarVisible === false); break;
      case 'theme-light': this.setTheme('light'); break;
      case 'theme-dark': this.setTheme('dark'); break;
      case 'theme-basic': this.setTheme('basic'); break;
      case 'theme-borland': this.setTheme('borland'); break;
      case 'lang-ru': this.setLanguage('ru'); break;
      case 'lang-en': this.setLanguage('en'); break;
      case 'open-log-folder': this.openLogFolder(); break;
      case 'manual': API.openManual(); break;
      case 'event-log': this.openEventLog(); break;
      case 'about': this.showAbout(); break;
      case 'editor-settings':
        this.editorSettingsDialog ||= new window.EditorSettingsDialog(this);
        this.editorSettingsDialog.open(); break;
    }
  }

  // ================= files / tabs =================

  newFile() {
    const model = this.monaco.editor.createModel('', 'axiom');
    let p;
    do { this.untitled++; p = `untitled${this.untitled}.axm`; } while (this.openFiles.has(p));
    this.openFiles.set(p, { model, viewState: null });
    this.createTab(p);
    this.switchToFile(p);
  }

  async openDialog() {
    const p = await API.openFileDialog();
    if (p) this.openFile(p);
  }

  async openProjectDialog() {
    try {
      const dir = await API.openProjectDialog(undefined, true);
      if (dir) return await this.openProject(dir);
    } catch(error) { this.showIdeProblem('project.failed',error.message||String(error)); }
    return false;
  }

  async openProjectFileDialog() {
    try {
      const file = await API.openProjectFileDialog();
      if (file) return await this.openProject(file);
    } catch(error) { this.showIdeProblem('project.failed',error.message||String(error)); }
    return false;
  }

  // Сеанс проекта открывает документы и хранит выбранную точку входа.
  async openProject(dir) {
    if(this.running){this.setStatus(I18N.t('project.stopFirst'));return false;}
    try { return await this.projectSession.open(dir); }
    catch(error){this.showIdeProblem('project.failed',error.message||String(error));return false;}
  }

  projectFolderContains(file) {
    const key=value=>String(value||'').replace(/\\/g,'/').replace(/\/$/,'').toLowerCase();
    return !!this.projectDir&&key(file).startsWith(key(this.projectDir)+'/');
  }

  async addExistingProjectFile() {
    if(!this.projectDir)return;
    const folder=this.projectDir;
    const selected=await API.openFileDialog();if(!selected||this.projectDir!==folder)return;
    try {
      if(/^\d+\.axm$/i.test(selected.split(/[/\\]/).pop()))throw new Error(I18N.t('project.numericSourceName'));
      let target=selected;
      if(!this.projectFolderContains(selected)){
        target=folder+'/'+selected.split(/[/\\]/).pop();
        const document=this.openFiles.get(selected);
        const text=document?document.model.getValue():await API.readFileContent(selected);
        if(this.projectDir!==folder)return;
        const response=await API.createProjectFile(folder,target,text);
        if(!response.ok)throw new Error(response.message);
      }
      if(this.projectDir!==folder)return;
      await this.refreshProjectFiles();
      if(!this.projectSession.isSource(target))throw new Error(I18N.t('project.fileExcluded'));
      await this.openFile(target);
    }catch(error){this.showIdeProblem('project.addFailed',error.message);}
  }

  async newProjectFile() {
    if (!this.projectDir) {
      alert('Сначала откройте папку проекта.');
      return;
    }
    const projectRoot = this.projectDir;
    const defaultPath = this.projectDir + '\\program.axm';
    const selectedPath = await API.saveFileDialog(defaultPath);
    if (!selectedPath) return;
    if (this.projectDir !== projectRoot) return;
    if (this.openFiles.has(selectedPath)) { alert(I18N.t('dialog.alreadyOpen', selectedPath)); return; }
    const result = await API.createProjectFile(projectRoot, selectedPath, '');
    if (!result.ok) { alert(result.message); return; }
    await this.refreshProjectFiles();
    await this.openFile(selectedPath);
  }

  async restoreProject() {
    const dir = this.settings.lastProjectFile || this.settings.lastProjectDir;
    if (typeof dir !== 'string' || !dir) return false;
    try {
      // Старый сеанс папки не должен заново выбирать axproj из её содержимого.
      const opened = await this.projectSession.open(dir,{restore:true,folder:!this.settings.lastProjectFile});
      this.log('project.restore', {path:dir,opened,entry:this.projectSession.entry});
      return opened;
    }
    catch(error){
      const detail = error.message || String(error);
      this.log('project.restore.failed',{path:dir,message:detail},'error');
      this.showIdeProblem('project.failed',detail);
      alert(I18N.t('project.failed')+' '+dir+'\n'+detail);
      return false;
    }
  }

  renderProjectFiles(files) {
    this.projectSession?.refresh(files);
    const title = document.getElementById('project-title');
    const list = document.getElementById('project-files');
    title.removeAttribute('data-i18n');
    title.textContent = this.projectDir.split(/[/\\]/).pop() || this.projectDir;
    if(this.projectSession?.descriptor)title.textContent=this.projectSession.descriptor.name;
    title.classList.remove('project-open');
    this.setProjectFilesExpanded(!this.projectFilesCollapsed);
    list.innerHTML = '';
    if(this.projectSession?.descriptor){window.ProjectView.render(this,list);return;}
    for (const file of files) {
      const button = document.createElement('button');
      button.className = 'project-file' + (file === this.activeFile ? ' active' : '');
      button.dataset.path = file;
      button.textContent = file.slice(this.projectDir.length + 1).replace(/\\/g, '/');
      button.title = file;
      button.onclick = () => this.openFile(file);
      list.appendChild(button);
    }
    this.projectSession?.render();
  }

  renderProjectEntry(files,entry,loading=false) {
    const container=document.getElementById('project-entry-controls');
    const select=document.getElementById('project-entry');
    if(!container||!select)return;
    container.classList.remove('hidden');
    const toggle=document.getElementById('project-files-toggle');
    if(toggle){toggle.hidden=!this.projectDir;UIControls.setLabel(toggle,I18N.t('project.files'));}
    select.replaceChildren();
    if(!this.projectDir){
      const current=document.createElement('option');current.value=this.activeFile||'';current.textContent=I18N.t('studio.currentFile');select.append(current);
      UIControls.prepareSelect(select);UIControls.setEnabled(select,false);this.studio?.updateExecution();return;
    }
    const empty=document.createElement('option');empty.value='';empty.textContent=I18N.t(files.length?'project.chooseEntry':'project.empty');select.appendChild(empty);
    for(const file of files){
      const option=document.createElement('option');option.value=file;
      option.textContent=this.projectSession.relative(file);select.appendChild(option);
    }
    const external=!this.projectSession.descriptor&&this.activeFile&&!this.projectSession.contains(this.activeFile)?this.activeFile:null;
    if(external){const option=document.createElement('option');option.value=external;option.textContent=I18N.t('studio.currentFile')+': '+external.split(/[\\/]/).pop();select.append(option);}
    UIControls.prepareSelect(select);
    select.value=external||entry||'';UIControls.setEnabled(select,!loading&&files.length>0);
    document.querySelectorAll('#project-files .project-file').forEach(button=>{
      button.classList.toggle('project-entry-file',!!entry&&button.dataset.path.toLowerCase()===entry.toLowerCase());
    });
    this.studio?.updateExecution();
  }

  promptProjectEntry() {
    if(this.projectSession?.descriptor)return window.ProjectView.properties(this);
    this.setProjectFilesExpanded(true);
    this.setStatus(I18N.t(this.projectSession?.loading?'project.loading':this.projectSession?.files.length?'project.chooseEntry':'project.empty'));
    document.getElementById('project-entry')?.focus();
  }

  setupProjectExplorer() {
    document.getElementById('project-files-toggle').onclick = () => this.setProjectFilesExpanded(!!this.projectFilesCollapsed);
    document.getElementById('project-entry').addEventListener('change',async event=>{
      const value=event.target.value,external=this.activeFile&&!this.projectSession.contains(this.activeFile);
      try { if(await this.projectSession.select(value)&&value&&external)this.switchToFile(value); }
      catch(error) { this.showIdeProblem('project.failed',error.message); this.projectSession.render(); }
    });
    document.getElementById('project-title').onclick = () => {
      if (!this.projectDir) this.openProjectFileDialog();
      else this.setProjectFilesExpanded(!!this.projectFilesCollapsed);
    };
    const close=document.getElementById('project-explorer-close');
    close.dataset.i18nTitle='project.close';UIControls.setLabel(close,I18N.t('project.close'));
    close.onclick = async () => {if(this.projectDir)await this.closeProject();if(!this.projectDir)this.setProjectExplorerVisible(false);};
    const add=document.getElementById('project-new-file');
    add.dataset.i18nTitle='project.addExisting';UIControls.setLabel(add,I18N.t('project.addExisting'));
    add.onclick = () => this.addExistingProjectFile();
    this.projectFiles.setup();
  }

  setProjectFilesExpanded(expanded) {
    this.projectFilesCollapsed=!expanded;
    const toggle=document.getElementById('project-files-toggle');
    if(!toggle)return;
    toggle.hidden=!this.projectDir;
    UIControls.setExpanded(toggle,expanded);
    UIControls.setLabel(toggle,I18N.t('project.files'));
    toggle.innerHTML=window.ShellIcons.markup(expanded?'chevron-down':'chevron-right');
    document.getElementById('project-files').hidden=!expanded;
    document.getElementById('project-entry-controls').classList.remove('hidden');
    document.getElementById('project-explorer').classList.toggle('project-collapsed',!!this.projectDir&&!expanded);
    this.updateProjectSplitterVisibility();
  }

  updateProjectSplitterVisibility() {
    const project=document.getElementById('project-explorer');
    const separator=document.getElementById('project-resizer');
    if(!project||!separator)return;
    this.projectSplitter?.cancel();
    separator.hidden=!!this.studio||project.classList.contains('hidden')||project.classList.contains('project-collapsed');
    if(!separator.hidden)this.projectSplitter?.layout();
  }

  isProjectFile(p) {
    if(this.projectSession?.descriptor)return this.projectSession.contains(p);
    if (!this.projectDir || typeof p !== 'string') return false;
    const root = this.projectDir.replace(/[\\/]+$/, '').toLowerCase();
    const file = p.toLowerCase();
    return file.startsWith(root + '\\') || file.startsWith(root + '/');
  }

  async closeProject() {
    if (!this.projectDir) { this.projectSession?.reset(); return; }
    // Take the membership snapshot before clearing projectDir: after closure the remaining
    // documents are no longer classified as project files. Confirm every dirty project
    // document first, so cancelling one confirmation leaves the project and all its tabs intact.
    const closingProjectDir = this.projectDir;
    const projectFiles = [...this.openFiles.keys()].filter((file) => this.isProjectFile(file));
    const approved = new Map();
    for (const file of projectFiles) {
      const entry = this.openFiles.get(file);
      if (!await this.prepareClose(file)) return;
      if (entry) approved.set(entry, entry.model.getValue());
    }
    if (this.projectDir !== closingProjectDir || [...approved].some(([entry, text]) => this.openFiles.get(entry.path) !== entry || entry.model.getValue() !== text)) return;
    this.projectDir = null;
    this.projectSession?.reset();
    this.projectFiles.reset();
    this.saveSettings({ lastProjectDir: null, lastProjectFile:null });
    const title = document.getElementById('project-title');
    title.setAttribute('data-i18n', 'project.open');
    title.textContent = I18N.t('project.open');
    title.classList.add('project-open');
    document.getElementById('project-files').innerHTML = '';
    // A project owns its open documents. Closing it releases their editor models and tabs,
    // while files opened from elsewhere deliberately remain available as independent files.
    for (const entry of approved.keys()) await this.closeFile(entry.path, true);
    // Without a project the distinction does not exist: the section hides itself.
  }

  toggleProjectExplorer() {
    if(this.studio){this.studio.toggleProject();return;}
    const visible = this.settings.sidebarVisible !== false && !document.getElementById('project-explorer').classList.contains('hidden');
    if (!visible) this.setSidebarVisible(true);
    this.setProjectExplorerVisible(!visible);
  }

  setProjectExplorerVisible(visible) {
    if(this.studio){if(visible)this.studio.select('project');else if(this.studio.tab==='project')this.setSidebarVisible(false);return;}
    document.getElementById('project-explorer').classList.toggle('hidden', !visible);
    this.updateProjectSplitterVisibility();
    this.saveSettings({ projectExplorer: visible });
    UIControls.setPressed(document.getElementById('tb-toggle-project'), visible && this.settings.sidebarVisible !== false);
    const check = document.querySelector('.menu-check[data-check="projectExplorer"]');
    UIControls.setMenuChecked(check, visible);
    setTimeout(() => this.editor?.layout(), 0);
  }

  setStatusBarVisible(visible) {
    document.getElementById('status-bar').style.display = visible ? '' : 'none';
    this.saveSettings({ statusBarVisible: visible });
    const check = document.querySelector('.menu-check[data-check="statusBarVisible"]');
    UIControls.setMenuChecked(check, visible);
    setTimeout(() => this.editor?.layout(), 0);
  }

  async openFile(p, isCurrent = () => true, {activate=true} = {}) {
    if(/\.axproj$/i.test(p))return this.openProject(p);
    if (!isCurrent()) return;
    if (this.openFiles.has(p)) {
      if(activate)this.switchToFile(this.openFiles.get(p).path);
      // Re-opening an already-open file still bumps recency.
      if (!p.startsWith('untitled')) {
        API.addRecentFile(p).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
      }
      return;
    }
    let content, readonly;
    try { content = await API.readFileContent(p); readonly = await API.fileReadonly(p); }
    catch { if (isCurrent()) alert(I18N.t('dialog.openFailed', p)); return; }
    if (!isCurrent()) return;
    // Пока читался файл, другая команда могла открыть его и получить правки.
    // Сохраняем уже созданную модель, не заменяя её повторным чтением с диска.
    if(this.openFiles.has(p)) {
      if(activate)this.switchToFile(this.openFiles.get(p).path);
      return;
    }
    // После Save As старая модель сохраняет свой URI; исходный файл можно открыть отдельно.
    let uri=this.monaco.Uri.file(p);
    while(this.monaco.editor.getModel(uri))uri=uri.with({query:'axiomDocument='+String(this._modelSerial=(this._modelSerial||0)+1)});
    const model = this.monaco.editor.createModel(content.replace(/^\uFEFF/, ''), /\.axm$/i.test(p) ? 'axiom' : 'plaintext', uri);
    this.openFiles.set(p, { model, viewState: null, readonly });
    this.createTab(p);
    if(activate)this.switchToFile(p);
    // A freshly opened document is diagnosed right away (same debounced live check as
    // typing) — its squiggles must not wait for the first keystroke.
    this.scheduleLiveCheck();
    if (this.settings.smartIdentifierEnabled) await this.formatWholeDocument('ax-smart-open', p, 'document.normalizeIdentifiers');
    API.watchFile(p).catch(() => {});
    if (!p.startsWith('untitled')) {
      API.addRecentFile(p).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
    }
  }

  createTab(p) {
    const name = p.split(/[/\\]/).pop();
    const tab = document.createElement('div');
    tab.className = 'editor-tab';
    tab.dataset.path = p;
    tab.innerHTML = '<span class="tab-name"></span><span class="dirty-indicator" style="display:none">*</span><button class="tab-close" aria-label="Закрыть вкладку"><span class="codicon codicon-close"></span></button>';
    tab.querySelector('.tab-name').textContent = name;
    tab.addEventListener('click', (e) => {
      if (!e.target.classList.contains('tab-close')) this.switchToFile(tab.dataset.path);
    });
    tab.querySelector('.tab-close').addEventListener('click', (e) => {
      e.stopPropagation();
      this.closeFile(tab.dataset.path);
    });
    document.getElementById('editor-tabs').appendChild(tab);
  }

  setupDocumentTabs() {
    const tabs = document.getElementById('editor-tabs');
    const left = document.getElementById('tabs-scroll-left');
    const right = document.getElementById('tabs-scroll-right');
    const update = () => {
      UIControls.setEnabled(left, tabs.scrollLeft > 1);
      UIControls.setEnabled(right, tabs.scrollLeft + tabs.clientWidth < tabs.scrollWidth - 1);
    };
    const scrollBy = (left) => {
      tabs.scrollBy({ left });
      update();
    };
    this.updateDocumentTabControls = update;
    left.addEventListener('click', () => scrollBy(-Math.max(100, tabs.clientWidth * 0.7)));
    right.addEventListener('click', () => scrollBy(Math.max(100, tabs.clientWidth * 0.7)));
    tabs.addEventListener('scroll', update);
    this.tabResizeObserver = new ResizeObserver(update);
    this.tabResizeObserver.observe(tabs);
    this.tabMutationObserver = new MutationObserver(update);
    this.tabMutationObserver.observe(tabs, { childList: true, subtree: true, characterData: true });
    update();
  }

  switchToFile(p) {
    const f = this.openFiles.get(p);
    if (!f) return;
    if (this.activeFile) {
      const cur = this.openFiles.get(this.activeFile);
      if (cur) cur.viewState = this.editor.saveViewState();
    }
    this.editor.setModel(f.model);
    this.applyModelPreferences(f.model);
    if (f.viewState) this.editor.restoreViewState(f.viewState);
    this.activeFile = p;
    this.trace?.update();
    document.getElementById('status-file-path').textContent = p;
    document.querySelectorAll('.editor-tab').forEach((t) => t.classList.toggle('active', t.dataset.path === p));
    const tabs = document.getElementById('editor-tabs');
    const activeTab = tabs.querySelector('.editor-tab.active');
    if (activeTab) {
      const bounds = tabs.getBoundingClientRect();
      const tabBounds = activeTab.getBoundingClientRect();
      if (activeTab === tabs.firstElementChild) tabs.scrollLeft = 0;
      else if (tabBounds.left < bounds.left) tabs.scrollLeft += tabBounds.left - bounds.left;
      else if (tabBounds.right > bounds.right) tabs.scrollLeft += tabBounds.right - bounds.right;
    }
    this.updateDocumentTabControls?.();
    document.querySelectorAll('.project-file').forEach((t) => t.classList.toggle('active', t.dataset.path === p || t.title === p));
    this.updateSaveButton();
    this.editor.focus();
    this.rebuildOutline();
    this.persistRecovery();
    this.projectSession?.render();
    this.studio?.updateDocument();
  }

  async closeFile(p, approved = false) {
    if (this.running && p === this._runFile) return false;
    const f = this.openFiles.get(p);
    if (!f || f.closing) return false;
    f.closing = true;
    try {
    if (!approved && !await this.prepareClose(p)) return false;
    p = f.path;
    if (this.openFiles.get(p) !== f) return false;
    if (!this.persistRecovery(new Set([f]))) return false;
    this.monaco.editor.setModelMarkers(f.model, 'axiom', []);
    this._lastDiagnostics = (this._lastDiagnostics || []).filter(d => d.file !== p);
    f.model.dispose();
    this.openFiles.delete(p);
    this.dirty.delete(p);
    API.unwatchFile(p).catch(() => {});
    const closedProjectFile = this.isProjectFile(p);
    // Drop the closed file's rows from the Problems list so a stale click can't
    // reference a dead editor.
    document.querySelectorAll('#diagnostics-content .diagnostic-item').forEach((it) => {
      if (it.dataset.file === p) it.remove();
    });
    this.updateDiagnosticFilters();
    document.querySelectorAll('.editor-tab').forEach((t) => { if (t.dataset.path === p) t.remove(); });
    if (this.activeFile === p) {
      const rest = [...this.openFiles.keys()];
      if (rest.length) this.switchToFile(rest[rest.length - 1]);
      else {
        this.activeFile = null;
        this.editor.setModel(null);
        document.getElementById('status-file-path').textContent = '';
        // Последняя вкладка не проходит через switchToFile: очищаем её структуру явно.
        this.rebuildOutline();
      }
    }
    if (this.projectDir) this.refreshProjectFiles();
    this.projectSession?.render();
    this.studio?.updateDocument();
    return true;
    } finally { f.closing = false; }
  }

  async refreshProjectFiles() {
    if (!this.projectDir) return;
    if(this.projectSession?.descriptor)return this.projectSession.reload();
    const root=this.projectDir,request=this.projectSession?.sequence;
    const files = await API.listProjectFiles(root);
    if(this.projectDir!==root||this.projectSession?.sequence!==request)return;
    this.renderProjectFiles(files);
  }

  refreshDirtyIndicators() {
    const open = new Set();
    for (const [p, entry] of this.openFiles) {
      open.add(p);
      const changed = p.startsWith('untitled') ? entry.model.getValue().length > 0 : this.openFiles.isDirty(p);
      if (changed) this.dirty.add(p); else this.dirty.delete(p);
      const tab = [...document.querySelectorAll('.editor-tab')].find(item => item.dataset.path === p);
      if (tab) tab.querySelector('.dirty-indicator').style.display = changed ? 'inline' : 'none';
    }
    for (const p of [...this.dirty]) if (!open.has(p)) this.dirty.delete(p);
  }

  markDirty(p, d) {
    if (!d) this.openFiles.markSaved(p);
    else d = p.startsWith('untitled') ? this.openFiles.get(p)?.model.getValue().length > 0 : this.openFiles.isDirty(p);
    if (d) this.dirty.add(p); else this.dirty.delete(p);
    const tab = [...document.querySelectorAll('.editor-tab')].find((t) => t.dataset.path === p);
    if (tab) tab.querySelector('.dirty-indicator').style.display = d ? 'inline' : 'none';
    this.persistRecovery();
  }

  async save(path = this.activeFile, saveAs = false, defaultPath = null) {
    const entry = this.openFiles.get(path);
    if (!entry) return false;
    path = entry.path;
    if (entry.readonly) { alert("Документ доступен только для чтения."); return false; }
    try {
      const result = await this.documentSaver.save(path, saveAs, defaultPath, (target,original)=>{
        if(target!==original&&this.projectFolderContains(target)&&/^\d+\.axm$/i.test(target.split(/[/\\]/).pop()))throw new Error(I18N.t('project.numericSourceName'));
      });
      if (!result) return false;
      if (result.path !== result.oldPath) await this.renameActive(result.path, result.oldPath);
      else if(this.projectFolderContains(result.path)){
        try{await this.refreshProjectFiles();}catch(error){this.showIdeProblem('project.failed',error.message);}
      }
      this.markDirty(result.path, this.openFiles.isDirty(result.path));
      this.setStatus(I18N.t('status.saved'));
      return !this.openFiles.isDirty(result.path);
    } catch (error) {
      this.setStatus(I18N.t('status.saveFailed'));
      alert(error.message);
      return false;
    }
  }

  async saveAll() {
    let allSaved = true;
    for (const entry of [...this.openFiles.values()]) {
      if (entry.path.startsWith('untitled') || this.openFiles.isDirty(entry.path)) {
        if (!await this.save(entry.path)) allSaved = false;
      }
    }
    return allSaved;
  }

  async prepareClose(path, discarded = null) {
    const entry = this.openFiles.get(path);
    if (!entry) return true;
    if (path.startsWith('untitled') && entry.model.getValue().length === 0) return true;
    if (!path.startsWith('untitled') && !this.openFiles.isDirty(path)) {
      let unavailable = false;
      try { await API.readFileContent(path); }
      catch { unavailable = true; }
      if (this.openFiles.get(path) !== entry || entry.path !== path) return false;
      if (unavailable) { entry.missing = true; this.markDirty(path, true); }
      if (!this.openFiles.isDirty(path)) return true;
    }
    const text = entry.model.getValue();
    const choice = await API.confirmDocumentClose(path.split(/[/\\]/).pop());
    if (this.openFiles.get(path) !== entry || entry.model.getValue() !== text) return false;
    if (choice === 'discard') { discarded?.add(entry); return true; }
    if (choice !== 'save') return false;
    return await this.save(path);
  }

  async prepareWindowClose() {
    if (this._closingWindow) return;
    this._closingWindow = true;
    try {
      const approved = new Map();
      const discarded = new Set();
      for (const entry of [...this.openFiles.values()]) {
        if (!await this.prepareClose(entry.path, discarded)) return;
        approved.set(entry, entry.model.getValue());
      }
      if (this.openFiles.size !== approved.size || [...this.openFiles.values()].some(e => !approved.has(e) || approved.get(e) !== e.model.getValue())) return;
      this._discardedOnExit = discarded;
      this.saveOpenTabs(true);
      if (this._recoveryFailed) { this._discardedOnExit = null; return; }
      API.approveWindowClose();
    } finally { this._closingWindow = false; }
  }

  saveFailed(p) {
    // A failed disk write must be visible, keep the tab dirty and never rename it to
    // the unwritable path.
    this.setStatus(I18N.t('status.saveFailed'));
    alert(I18N.t('dialog.saveFailed', p));
    this.focusEditor();
  }

  async saveAs() { return this.save(this.activeFile, true); }

  async renameActive(newPath, old = this.activeFile) {
    const f = this.openFiles.get(old) || this.openFiles.get(newPath);
    if (!f) return;
    if (this.openFiles.has(old)) this.openFiles.rename(old, newPath);
    let projectError=null;
    try{await this.projectSession?.renamed(old,newPath);}catch(error){projectError=error;}
    if (this._runFile === old) this._runFile = newPath;
    this.dirty.delete(old);
    this.dirty.delete(newPath);
    if (this.activeFile === old) this.activeFile = newPath;
    API.unwatchFile(old).catch(() => {});
    API.watchFile(newPath).catch(() => {});
    const tab = [...document.querySelectorAll('.editor-tab')].find((t) => t.dataset.path === old);
    if (tab) {
      tab.dataset.path = newPath;
      tab.querySelector('.tab-name').textContent = newPath.split(/[/\\]/).pop();
    }
    // Rebind stale Problems rows to the new path so closeFile(newPath) prunes them too.
    document.querySelectorAll('#diagnostics-content .diagnostic-item').forEach((it) => {
      if (it.dataset.file === old) it.dataset.file = newPath;
    });
    // The file was just written to disk, so the renamed tab must not keep the dirty
    // marker visible.
    this.markDirty(newPath, this.openFiles.isDirty(newPath));
    document.getElementById('status-file-path').textContent = this.activeFile || '';
    document.querySelectorAll('.editor-tab').forEach((t) => t.classList.toggle('active', t.dataset.path === this.activeFile));
    this.updateSaveButton();
    this.rebuildOutline();
    this.assistant?.invalidate();
    this.assistant?.updateHistory();
    API.addRecentFile(newPath).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
    if (this.projectDir && (this.projectFolderContains(old) || this.projectFolderContains(newPath))){
      try{await this.refreshProjectFiles();}catch(error){projectError ||= error;}
    }
    if(projectError)this.showIdeProblem('project.failed',I18N.t('project.savedEntryFailed',projectError.message));
  }

  updateSaveButton() {
    const normal = document.getElementById('tb-save-normal');
    const saveAs = document.getElementById('tb-save-as');
    if (!normal || !saveAs) return;
    const untitled = this.activeFile?.startsWith('untitled');
    normal.style.display = untitled ? 'none' : 'block';
    saveAs.style.display = untitled ? 'block' : 'none';
  }

  // ================= autoformat triggers =================

  async formatWholeDocument(owner, path = this.activeFile, method = 'document.format', saveCleanDocument = false) {
    const entry = this.openFiles.get(path);
    // Сохраняем только документы, которые были чистыми до постановки в очередь.
    const saveSnapshot = saveCleanDocument && entry && !entry.readonly && !entry.closing &&
      !path.startsWith('untitled') && !this.openFiles.isDirty(path)
      ? { entry, savedText: entry.savedText } : null;
    const previous = this._formatDocumentPending;
    let release;
    const pending = new Promise(resolve => { release = resolve; });
    this._formatDocumentPending = pending;
    try {
      if (previous) await previous;
      return await this.performWholeDocumentFormat(owner, path, method, saveSnapshot);
    } finally {
      release();
      if (this._formatDocumentPending === pending) this._formatDocumentPending = null;
    }
  }

  async performWholeDocumentFormat(owner, path, method, saveSnapshot = null) {
    if (this.running && path === this._runFile) return false;
    const document = this.openFiles.get(path);
    const model = document?.model;
    if (!model) return;
    if (document.readonly || model.getLanguageId() !== "axiom") return true;
    if (this._formatBusy) {
      this.log('format.document.skipped-busy', { owner, busy: this._formatBusy });
      return;
    }
    const sel = this.editor.getModel() === model ? this.editor.getSelection() : null;
    const text = model.getValue();
    const saveFormatted = saveSnapshot?.entry === document && document.path === path &&
      !document.closing && !document.missing && document.savedText === saveSnapshot.savedText &&
      text === saveSnapshot.savedText;
    const formattingStyle = this.settings.formattingStyle || 'Classic';
    const smartIdentifierEnabled = !!this.settings.smartIdentifierEnabled;
    const request = ++this._serviceSeq;
    clearTimeout(this._liveCheckTimer);
    this._liveCheckTimer = null;
    this._formatBusy++;
    this.log('format.document.start', { owner, length: text.length, request });
    try {
      let r;
      try { r = await this.documentServices.request(path, method, { formattingStyle, smartIdentifierEnabled }); }
      catch (error) {
        if (request !== this._serviceSeq || this.openFiles.get(path) !== document || model.isDisposed?.() || model.getValue() !== text ||
            formattingStyle !== (this.settings.formattingStyle || 'Classic') ||
            smartIdentifierEnabled !== !!this.settings.smartIdentifierEnabled ||
            (this.running && this.openFiles.get(this._runFile)?.model === model)) return;
        throw error;
      }
      if (!r || typeof r.sourceText !== 'string') { this.log('format.document.failed', { owner, request, reason: r?.reason }); return; }
      const m = this.openFiles.get(path)?.model;
      if (this.running && path === this._runFile) return;
      if (formattingStyle !== (this.settings.formattingStyle || 'Classic')) return;
      if (smartIdentifierEnabled !== !!this.settings.smartIdentifierEnabled) return;
      if (request !== this._serviceSeq || !m || m !== model) { this.log('format.document.stale', { owner, request }); return; }
      if (m.getValue() !== text) { this.log('format.document.stale-edit', { owner, request }); return; } // the buffer changed while the request was in flight — stale, skip
      const edits = this.lineDiffEdits(m, r.sourceText);
      if (!edits.length) { this.log('format.document.noop', { owner, request }); return true; }
      model.pushStackElement();
      this._autoFormatting = true;
      try {
        model.pushEditOperations([], edits, () => null);
      } finally {
        this._autoFormatting = false;
      }
      model.pushStackElement(); // the whole autoformat is a single undo step
      if (sel && this.editor.getModel() === model) this.editor.setSelection(sel);
      this.log('format.document.applied', { owner, request, edits: edits.length });
      if (saveFormatted && this.openFiles.get(path) === document && document.path === path &&
          !document.closing && document.savedText === saveSnapshot.savedText &&
          model.getValue().replace(/\r\n?/g, '\n') === r.sourceText.replace(/\r\n?/g, '\n')) {
        // save() записывает снимок; новые правки во время записи остаются несохранёнными.
        if (!await this.save(path)) return false;
      }
      return true;
    } catch (e) {
      this.log('format.document.exception', { owner, request, filePath: path, message: e?.message || String(e) }, 'error');
      if (this.activeFile === path) this.showIdeProblem('status.fmtFailed', e?.message || String(e));
      return false;
    } finally {
      this._formatBusy = Math.max(0, this._formatBusy - 1);
      if (!this._formatBusy) { this.scheduleLiveCheck(); this.scheduleOutlineRebuild(); }
    }
  }

  // Per-line diff against the formatter result. The formatter is line-preserving (one source
  // line = one output line), so only same-line replacements plus trailing-newline normalization
  // are expected; anything unexpected falls back to one full-range replace.
  lineDiffEdits(model, formatted) {
    const monaco = this.monaco;
    const cur = model.getValue().replace(/\r\n/g, '\n').split('\n');
    const want = formatted.replace(/\r\n/g, '\n').split('\n');
    const fullReplace = [{ range: model.getFullModelRange(), text: formatted, forceMoveMarkers: true }];
    const lineEdit = (i, text) => ({ range: new monaco.Range(i + 1, 1, i + 1, cur[i].length + 1), text, forceMoveMarkers: true });
    if (cur.length !== want.length) {
      if (want.length === cur.length + 1 && want[want.length - 1] === '') {
        const edits = [];
        for (let i = 0; i < cur.length; i++) if (cur[i] !== want[i]) edits.push(lineEdit(i, want[i]));
        edits.push({ range: new monaco.Range(cur.length, cur[cur.length - 1].length + 1, cur.length, cur[cur.length - 1].length + 1), text: '\n', forceMoveMarkers: true });
        return edits;
      }
      if (want.length === cur.length - 1 && cur[cur.length - 1] === '') {
        const edits = [];
        for (let i = 0; i < want.length; i++) if (cur[i] !== want[i]) edits.push(lineEdit(i, want[i]));
        edits.push({ range: new monaco.Range(cur.length - 1, cur[cur.length - 2].length + 1, cur.length, cur[cur.length - 1].length + 1), text: '', forceMoveMarkers: true });
        return edits;
      }
      return fullReplace;
    }
    const edits = [];
    for (let i = 0; i < cur.length; i++) if (cur[i] !== want[i]) edits.push(lineEdit(i, want[i]));
    return edits;
  }

  async setFormattingStyle(style) {
    if (!['Classic', 'Modern'].includes(style)) return;
    this.saveSettings({ formattingStyle: style });
    this.updateFormatMenuChecks();
    const previous = this._styleSwitchPending;
    let release;
    const pending = new Promise(resolve => { release = resolve; });
    this._styleSwitchPending = pending;
    try {
      if (previous) await previous;
      for (const path of [...this.openFiles.keys()]) await this.formatWholeDocument('ax-style-switch', path, 'document.format', true);
    } finally {
      release();
      if (this._styleSwitchPending === pending) this._styleSwitchPending = null;
    }
  }

  updateFormatMenuChecks() {
    UIControls.setMenuChecked(document.querySelector('.menu-check[data-check="smartIdentifier"]'), this.settings.smartIdentifierEnabled);
    for (const [key, style] of [['formatClassic', 'Classic'], ['formatModern', 'Modern']]) {
      UIControls.setMenuChecked(document.querySelector('.menu-check[data-check="' + key + '"]'), (this.settings.formattingStyle || 'Classic') === style);
    }
  }

  // ================= live diagnostics + outline =================

  // The buffer is checked without saving (`axiom check --stdin --json`, debounced).
  // Its file path accompanies the buffer so the main process can run the CLI from the
  // active file's directory.
  scheduleLiveCheck() {
    this._liveCheckSeq++;
    if (this._restoringSession) return;
    clearTimeout(this._liveCheckTimer);
    const seq = this._liveCheckSeq;
    this._liveCheckTimer = setTimeout(() => this.runLiveCheck(seq).catch((e) => this.log('check.live.exception', { message: e?.message || String(e) }, 'error')), 600);
    this.log('check.live.scheduled', { seq });
  }

  async runLiveCheck(seq = this._liveCheckSeq) {
    if (this._restoringSession || this.projectSession.loading) return;
    const model = this.editor.getModel();
    if (!model || model.getLanguageId() !== 'axiom' || seq !== this._liveCheckSeq) return;
    if (this._formatBusy) { this.log('check.live.skipped-format', { seq, busy: this._formatBusy }); return; }
    const text = model.getValue();
    const filePath = this.activeFile; // keep diagnostics tied to their file
    this.log('check.live.start', { seq, length: text.length, filePath });
    let r;
    try { r = await this.documentServices.request(filePath, 'document.analyse'); }
    catch (error) {
      if (seq !== this._liveCheckSeq || this.editor.getModel() !== model || model.isDisposed?.() || model.getValue() !== text) return;
      throw error;
    }
    if (!r || !Array.isArray(r.diagnostics)) { this.log('check.live.failed', { seq, reason: r?.reason }); return; }
    const m = this.editor.getModel();
    if (seq !== this._liveCheckSeq || this._formatBusy || !m || m !== model) { this.log('check.live.stale', { seq }); return; }
    if (m.getValue() !== text) { this.log('check.live.stale-edit', { seq }); return; } // stale
    r.diagnostics.forEach((d) => { d.file = d.sourceFilePath || filePath; d.col = d.column; });
    // Ошибки исполнения относятся к тому же неизменённому снимку: фоновая
    // проверка, поставленная в очередь до запуска, не должна их стирать.
    // Действующая ошибка сохраняется независимо от файла происхождения: снимок
    // проверяется по документу самой диагностики, а не по активному файлу.
    const runtime = (this._lastDiagnostics || []).filter((d) =>
      d.executionSnapshot && this.diagnosticMatchesSnapshot(d));
    const diagnostics = runtime.length ? [...r.diagnostics, ...runtime] : r.diagnostics;
    this.applyDiagnostics(diagnostics, true);
    this.log('check.live.applied', { seq, diagnostics: diagnostics.length, runtime: runtime.length });
  }

  // Outline panel — the current file's top-level declarations (SUB / FUNCTION / TYPE)
  // using the same document scan the completion provider uses. Clicking an item jumps to
  // its line. The panel is rebuilt on content change (debounced) and when switching tabs.
  rebuildOutline() {
    if (this._formatBusy && this.editor.getModel()) { this.log('outline.skipped-format', { busy: this._formatBusy }); return; }
    if (!this.outlinePanel) this.outlinePanel = new window.OutlinePanel(this);
    this.studio?.compactOutline();
    this.outlinePanel.rebuild();
  }

  scheduleOutlineRebuild() {
    clearTimeout(this._outlineTimer);
    const seq = ++this._outlineSeq;
    this._outlineTimer = setTimeout(() => {
      if (seq !== this._outlineSeq) return;
      this.log('outline.rebuild.start', { seq });
      this.rebuildOutline();
      this.log('outline.rebuild.end', { seq });
    }, 200);
  }

  // ================= run / check / format / stop =================

  // Внутри проекта запускается выбранная точка входа; отдельный файл запускается сам.
  runTarget() {
    return this.projectSession?.target() ?? (this.projectDir ? null : this.activeFile);
  }

  async check(explicit = false) {
    if(this.projectSession?.descriptor)return window.ProjectView.check(this,explicit);
    const target = this.activeFile;
    if (!target) return;
    const startedAt = performance.now();
    try {
      const r = await this.documentServices.request(target, 'document.compile');
      if (!r || target !== this.activeFile) return;
      r.diagnostics.forEach(d => { d.file = d.sourceFilePath || target; d.col = d.column; });
      if (r.status === 'BackendFailure' && !r.diagnostics.length) throw new Error('Компилятор не подготовил байт-код.');
      this.applyDiagnostics(r.diagnostics, true);
      if (explicit) {
        const elapsed = Math.round(performance.now() - startedAt);
        const failed = (r.diagnostics || []).some((d) => String(d.severity || '').toLowerCase() === 'error');
        I18N.setText(document.getElementById('status-execution-time'), 'status.compileTimeElapsed', elapsed);
        if (failed) this.setStatus(I18N.t('status.error'));
      }
    } catch (e) {
      if (target !== this.activeFile) return;
      this.showIdeProblem('status.checkFailed', e && e.message ? e.message : String(e));
      const elapsed = Math.round(performance.now() - startedAt);
      if (explicit) {
        I18N.setText(document.getElementById('status-execution-time'), 'status.compileTimeElapsed', elapsed);
        this.setStatus(I18N.t('status.error'));
      }
    }
    // An explicit check always surfaces the Problems panel — both to show errors and to
    // visibly confirm "Проблем нет" on success.
    if (explicit) this.switchPanel('diagnostics');
    if (explicit) this.focusEditor();
  }

  async run(launchKind = 'run', stopFirst = true, currentFile = false) {
    if (this.running) return;
    if(this.projectSession?.descriptor&&!currentFile){try{await this.projectSession.reload();}catch(error){this.showIdeProblem('project.failed',error.message);return;}}
    let target = currentFile?this.activeFile:this.runTarget();
    if (!target) { this.promptProjectEntry(); return; }
    if(!this.openFiles.has(target)) {
      const origin=this.editor?.getModel();
      await this.openFile(target,()=>this.runTarget()===target&&this.editor?.getModel()===origin);
      if(this.runTarget()!==target)return;
      this.projectSession?.navigation();
    }
    const entry = this.openFiles.get(target);
    if (!entry) return;
    if (target.startsWith('untitled') || this.dirty.has(target)) {
      if (!confirm('Перед запуском нужно сохранить документ. Сохранить и запустить?')) return;
      if (!await this.save(target)) return;
      target = entry.path;
    }
    if (this.running || this.openFiles.get(target) !== entry || entry.model.isDisposed?.() ||
        target.startsWith('untitled') || this.dirty.has(target)) return;
    if (this.activeFile !== target) this.switchToFile(target);
    this._runFile = target;
    // The integrated ConPTY console is the only run path: output, stderr and interactive
    // input all live in the terminal.
    document.getElementById('program-canvas')?.setAttribute('hidden', '');
    await this.execution.start(target, launchKind, launchKind === 'debug' ? this.trace.lines(this.openFiles.get(target).model) : [], stopFirst, currentFile?null:this.projectSession?.descriptor?.projectFilePath||null);
  }

  stop() {
    this.trace?.cancelRestart();
    // Stop is silent when nothing is running — no misleading status.
    if (!this.running) return;
    this._stopRequested = true; // the pty exit event must not overwrite "Остановлено"
    return this.execution.stop();
  }

  // ================= integrated console =================

  // The xterm palette is described once in themes.js; surface colours (background,
  // text, cursor, selection) still come from the shared CSS variables.
  consoleTheme() {
    return window.AxiomThemes.consoleTheme(this.settings.theme);
  }

  // Lazy xterm.js setup: the Terminal exists only after the first console run (or tab
  // switch), so startup stays cheap.
  ensureConsole() {
    if (this.term) return true;
    if (!window.Terminal || !window.FitAddon) {
      this.showIdeProblem('console.unavailable');
      this.setStatus(I18N.t('status.error'));
      return false;
    }
    this.term = new window.Terminal({
      convertEol: false, // ConPTY supplies real CR/LF — converting would double-break
      cursorBlink: true,
      fontSize: 13,
      fontFamily: 'Consolas, "Cascadia Mono", "Courier New", monospace',
      theme: this.consoleTheme(),
    });
    this.fitAddon = new window.FitAddon.FitAddon();
    this.term.loadAddon(this.fitAddon);
    this.term.open(document.getElementById('terminal'));
    this.log('terminal.created');
    let consoleSelection = '';
    const consoleMenu = UIControls.createContextMenu(document.getElementById('ax-console-context-menu'), {
      translate: I18N.t, onClose: () => this.term.focus(), onAction: async action => {
        if (action === 'copy' && consoleSelection) await API.writeClipboardText(consoleSelection);
        if (action === 'clear') {
          if (this.execution) this.execution.clearConsole();
          else this.term.reset();
        }
      },
    });
    document.getElementById('terminal').addEventListener('contextmenu', event => {
      event.preventDefault();
      event.stopPropagation();
      consoleSelection = this.term.getSelection();
      consoleMenu.show([
        {id: 'copy', translation: 'menu.edit.copy', icon: 'codicon-copy', enabled: !!consoleSelection},
        {separator: true}, {id: 'clear', translation: 'console.clear', icon: 'codicon-clear-all'},
      ], event.clientX, event.clientY);
    });
    this.fitConsole();
    this.term.onData((d) => this.consoleInput(d)); // keystrokes go through the type-ahead hold
    // INKEY, KEYPRESS, KEYDOWN и SLEEP должны видеть клавиши консоли и отпускание модификаторов.
    const heldProgramKeys = new Map();
    const sendProgramKey = data => {
      const session = this.execution?.sessionId;
      if (!session) return;
      this._programKeyWrites = (this._programKeyWrites || Promise.resolve())
        .then(() => API.programKey(session, data)).catch(() => {});
    };
    this.term.textarea.addEventListener('keydown', event => {
      if (!this.running) return;
      const data = programKey(event);
      if (!data) return;
      heldProgramKeys.set(event.code, data.key);
      sendProgramKey({ action: 'down', ...data });
    }, true);
    window.addEventListener('keyup', event => {
      const key = heldProgramKeys.get(event.code);
      heldProgramKeys.delete(event.code);
      if (key && ![...heldProgramKeys.values()].includes(key)) sendProgramKey({ action: 'up', key });
    }, true);
    window.addEventListener('blur', () => { heldProgramKeys.clear(); sendProgramKey({ action: 'clear' }); });
    // Paste guard: any DOM paste into the terminal (Ctrl+V, context menu, Shift+Insert)
    // goes through the sanitizer — control characters in raw clipboard data once
    // terminated the program through ConPTY.
    this.term.textarea.addEventListener('paste', (e) => {
      e.preventDefault();
      e.stopPropagation();
      const text = e.clipboardData ? e.clipboardData.getData('text/plain') : '';
      this.queueConsoleWrite(() => this.writeConsolePaste(text));
    }, true);
    if (this._consoleResizeObs) this._consoleResizeObs.disconnect();
    this._consoleResizeObs = new ResizeObserver(() => this.fitConsole());
    this._consoleResizeObs.observe(document.getElementById('terminal'));
    return true;
  }

  // Console input path with silent type-ahead. While the program is starting (the dotnet
  // spawn takes a second or two) ConPTY echoes typed keys onto the empty screen, so the
  // first prompt appeared a few rows below the tabs and looked like a broken layout.
  // Input typed before the first program output is held here (no echo) and flushed when
  // that output arrives, or after 2s for programs that print nothing before reading.
  consoleInput(d) {
    if (this.execution) { this.queueConsoleWrite(() => this.execution.input(d)); return; }
    this.log('terminal.input', { length: String(d ?? '').length, control: /[\u0000-\u001f\u007f]/.test(String(d ?? '')) });
    if (this._consoleHold) {
      if (d === '\u0003') { this.queueConsoleWrite(() => API.ptyWrite(d)); return; }
      this._consoleHold.chunks.push(d);
      if (this._consoleHold.chunks.length === 1) {
        this._consoleHold.timer = setTimeout(() => this.flushConsoleHold(), 2000);
      }
      return;
    }
    this.queueConsoleWrite(() => API.ptyWrite(d));
  }

  flushConsoleHold() {
    const hold = this._consoleHold;
    if (!hold) return;
    this._consoleHold = null;
    clearTimeout(hold.timer);
    for (const chunk of hold.chunks) this.queueConsoleWrite(() => API.ptyWrite(chunk));
  }

  // All console input writes go through one promise chain. A paste resolves its clipboard
  // read asynchronously; without the chain an Enter pressed right after Ctrl+V would
  // overtake the pasted text and reach the program as an empty line first.
  queueConsoleWrite(write) {
    this._consoleWriteChain = (this._consoleWriteChain || Promise.resolve()).then(write).catch(() => {});
  }

  // Only sanitized plain text ever reaches the pty: control characters and escape
  // sequences are stripped, newlines become Enter; an image-only clipboard pastes
  // nothing and says so.
  writeConsolePaste(text) {
    if (!this.term) return;
    const clean = String(text ?? '')
      .replace(/\r\n?/g, '\r')
      .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]/g, '')
      .slice(0, 100000);
    if (clean) { return this.execution.input(clean); }
    this.setStatus(I18N.t('console.pasteNoText'));
  }

  fitConsole() {
    if (!this.term || !this.fitAddon) return;
    const panel = document.getElementById('console-content');
    if (!panel || panel.offsetParent === null) return; // hidden tab — nothing to fit yet
    try {
      this.fitAddon.fit();
      API.ptyResize(this.term.cols, this.term.rows);
      this.log('terminal.resize', { cols: this.term.cols, rows: this.term.rows });
    } catch {}
  }

  async runInConsole() {
    this.running = true;
    this._stopRequested = false;
    this.clearDiagnostics();
    this.showPanel();
    this.switchPanel('console');
    this.setStatus(I18N.t('status.running'));
    if (!this.ensureConsole()) {
      this.running = false;
      return;
    }
    this.term.reset();
    this._consoleHold = { chunks: [], timer: null }; // silent type-ahead until first output
    const t0 = performance.now();
    const r = await API.runConsole(this._runFile || this.activeFile);
    if (!r || !r.ok) {
      const why = r && r.reason === 'axiom-not-found'
        ? I18N.t('console.axiomNotFound')
        : I18N.t('console.failed') + (r && r.detail ? ` (${r.detail})` : r && r.reason ? ` (${r.reason})` : '');
      this.term.write('\x1b[31m' + why + '\x1b[0m\r\n');
      this.setStatus(I18N.t('status.error'));
      this.running = false;
      return;
    }
    this.fitConsole();
    this.term.focus();
    // The exit event (wired once in setupConsoleEvents) finalizes status/time/running.
    this._consoleT0 = t0;
  }

  // Wired once: pty data streams into the terminal; the exit event finalizes the run
  // (status, execution time) and releases the Run button.
  setupConsoleEvents() {
    API.onPtyData((d) => {
      if (!this.term || !this.running) return;
      this.flushConsoleHold(); // typed-ahead input flushes on the first program output
      this.term.write(d);
    });
    API.onPtyExit(({ exitCode, diagnostics }) => {
      if (!this.running) return;
      this.running = false;
      this._consoleHold = null; // the program is gone — held type-ahead is meaningless now
      const hasDiagnostics = Array.isArray(diagnostics) && diagnostics.length > 0;
      if (this.term && !hasDiagnostics) {
        this.term.write(`\r\n\x1b[2m[${exitCode === 0 ? I18N.t('status.finished') : I18N.t('status.exited') + ' ' + exitCode}]\x1b[0m\r\n`);
      }
      if (this._stopRequested) {
        this.setStatus(I18N.t('status.stopped'));
        this._stopRequested = false;
      } else if (exitCode === 0) {
        this.setStatus(I18N.t('status.finished'));
      } else {
        this.setStatus(`${I18N.t('status.exited')} ${exitCode}`);
      }
      if (hasDiagnostics) {
        // Diagnostics never enter xterm. Preserve program output preceding a runtime
        // diagnostic; compile failures naturally leave the console empty.
        this.applyDiagnostics(diagnostics, true);
        this.switchPanel('diagnostics');
      }
      if (this._consoleT0) {
        I18N.setText(document.getElementById('status-execution-time'), 'status.totalTimeElapsed', Math.round(performance.now() - this._consoleT0));
        this._consoleT0 = null;
      }
      this.focusEditor();
    });
  }

  async formatDocument() {
    if (!this.activeFile) return;
    if (await this.formatWholeDocument('ax-format-command')) this.setStatus(I18N.t('status.formatted'));
    this.focusEditor();
  }

  async editLineTransition(model, previousLineNumber, position) {
    if(this.editor.getContribution('snippetController2')?.isInSnippet())return;
    if (this.running && this.openFiles.get(this._runFile)?.model === model) return;
    if (this._autoFormatting || this._formatBusy || !this.activeFile || model !== this.editor.getModel()) return;
    const document = this.openFiles.get(this.activeFile);
    if (!document || document.readonly || model.getLanguageId() !== 'axiom') return;
    const caretOffset = model.getOffsetAt(position);
    const smartIdentifierEnabled = !!this.settings.smartIdentifierEnabled;
    const indentMode=this.settings.editorPreferences?.autoIndent || window.EditorPreferences.defaults(this.monaco).autoIndent;
    const nativeIndent=/^[ \t]*/.exec(model.getLineContent(position.lineNumber))[0];
    const path = this.activeFile, text = model.getValue();
    const formattingStyle = this.settings.formattingStyle || 'Classic';
    const token = {};
    this._structuralRequests ||= new WeakMap();
    this._structuralRequests.set(model, token);
    const current = () => this._structuralRequests.get(model) === token && this.activeFile === path &&
      this.openFiles.get(path) === document && !model.isDisposed?.() && this.editor.getModel() === model &&
      model.getValue() === text && smartIdentifierEnabled === !!this.settings.smartIdentifierEnabled &&
      formattingStyle === (this.settings.formattingStyle || 'Classic') &&
      indentMode === (this.settings.editorPreferences?.autoIndent || window.EditorPreferences.defaults(this.monaco).autoIndent) &&
      !this.editor.getContribution('snippetController2')?.isInSnippet() &&
      !(this.running && this.openFiles.get(this._runFile)?.model === model) &&
      model.getOffsetAt(this.editor.getPosition()) === caretOffset;
    let applying = false;
    try {
      const result = await this.documentServices.request(this.activeFile, 'document.editTransition', {
        previousLineNumber, currentLineNumber: position.lineNumber, caretOffset,
        formattingStyle, smartIdentifierEnabled });
      if (!result || !current()) return;
      this.editedLines.consume(model,previousLineNumber);
      if (!result.edits.length) return;
      const edits = result.edits.map(e => {
        if (!Number.isInteger(e.start) || !Number.isInteger(e.length) || e.start < 0 || e.length < 0 ||
            e.start + e.length > model.getValueLength()) throw new Error('Некорректный диапазон правки.');
        const start=model.getPositionAt(e.start),end=model.getPositionAt(e.start+e.length);
        return {range:new this.monaco.Range(start.lineNumber,start.column,end.lineNumber,end.column),text:e.newText};
      });
      applying = true;
      this.editor.pushUndoStop();this._autoFormatting=true;
      try {
        this.editor.executeEdits('axiom-block-edit',edits);
        let caret=model.getPositionAt(result.newCaretOffset);
        // Простые режимы сохраняют отступ, который уже выбрал Monaco.
        // Структурный алгоритм Axiom задаёт отступы только в расширенных режимах.
        if(['none','keep','brackets'].includes(indentMode)){
          const actualIndent=/^[ \t]*/.exec(model.getLineContent(caret.lineNumber))[0];
          if(actualIndent!==nativeIndent){
            this.editor.executeEdits('axiom-native-indent',[{range:new this.monaco.Range(caret.lineNumber,1,caret.lineNumber,actualIndent.length+1),text:nativeIndent}]);
            caret={lineNumber:caret.lineNumber,column:Math.max(1,caret.column-actualIndent.length+nativeIndent.length)};
          }
        }
        this.editor.setPosition(caret);
      }
      finally {this._autoFormatting=false;}
      this.editor.pushUndoStop();
    } catch(e) {
      if (applying || current()) this.showIdeProblem('status.fmtFailed', e?.message || String(e));
    }
  }

  // ================= external file changes (watcher) =================

  async handleExternalChange(ev) {
    const p = ev && ev.path;
    if (!p || !this.openFiles.has(p) || p.startsWith('untitled')) return;
    this.fileChangeEvents.set(p, ev);
    if (this.fileChangePrompts.has(p)) return;
    const entry = this.openFiles.get(p);
    const documentId = entry.documentId;
    const revision = entry.revision;
    this.fileChangePrompts.add(p);
    try {
      const name = p.split(/[/\\]/).pop();
      let approved = false;
      for (;;) {
        const event = this.fileChangeEvents.get(p);
        const f = this.openFiles.get(p);
        if (f !== entry || f.documentId !== documentId || f.revision !== revision || f.path !== p) return;
        if (!event.exists) {
          f.missing = true;
          this.markDirty(p, true);
          this.setStatus(I18N.t('dialog.fileDeleted', name));
          return;
        }
        if (!approved && !confirm(I18N.t('dialog.fileChangedReload', name))) return;
        approved = true;
        const content = await API.readFileContent(p);
        if (this.openFiles.get(p) !== entry || entry.documentId !== documentId || entry.revision !== revision || entry.path !== p) return;
        if (this.fileChangeEvents.get(p) !== event) continue;
        f.model.setValue(content);
        this.markDirty(p, false);
        this.setStatus(I18N.t('status.reloaded'));
        if (this.activeFile === p) this.editor.focus();
        return;
      }
    } catch {
      alert(I18N.t('dialog.openFailed', p));
    } finally {
      this.fileChangePrompts.delete(p);
      this.fileChangeEvents.delete(p);
    }
  }

  // ================= diagnostics =================

  findModelForFile(file) {
    if (!file) return this.activeFile ? this.openFiles.get(this.activeFile)?.model : null;
    const name = file.split(/[/\\]/).pop();
    for (const [p, { model }] of this.openFiles) {
      if (p === file || p.endsWith(file) || file.endsWith(p)) return model;
    }
    for (const [p, { model }] of this.openFiles) {
      if (p.split(/[/\\]/).pop() === name) return model;
    }
    return null;
  }

  clearDiagnostics() {
    this.log('diagnostics.clear');
    document.getElementById('diagnostics-content').innerHTML = '';
    this._lastDiagnostics = [];
    this.updateDiagnosticFilters();
  }

  diagnosticCategory(d) {
    return d.severity === 'error' ? 'error' : d.code === 'MB2047' ? 'advice' : 'warning';
  }

  updateDiagnosticFilters() {
    const diags = this._lastDiagnostics || [];
    for (const button of document.querySelectorAll('[data-diagnostic-filter]')) {
      const category = button.dataset.diagnosticFilter;
      const count = category === 'all' ? diags.length : diags.filter(d => this.diagnosticCategory(d) === category).length;
      button.textContent = I18N.t('problems.' + category) + ' (' + count + ')';
      UIControls.setPressed(button, category === this._diagnosticFilter);
    }
    const content = document.getElementById('diagnostics-content');
    content.querySelector('#diagnostic-empty')?.remove();
    const items = [...content.querySelectorAll('.diagnostic-item')];
    for (const item of items) item.hidden = this._diagnosticFilter !== 'all' && item.dataset.category !== this._diagnosticFilter;
    if (!items.some(item => !item.hidden)) {
      const empty = document.createElement('div'); empty.id = 'diagnostic-empty'; empty.className = 'recent-empty';
      empty.textContent = I18N.t(diags.length ? 'problems.filteredEmpty' : 'problems.empty');
      content.appendChild(empty);
    }
  }

  clearUsageAdvice() {
    const model = this.editor.getModel();
    if (!model || !this._lastDiagnostics?.some(d => d.code === 'MB2047' && d.file === this.activeFile)) return;
    const markers = this.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri });
    this.monaco.editor.setModelMarkers(model, 'axiom', markers.filter(m => m.code !== 'MB2047'));
    this.reconcileDiagnosticPeek(new Map([[model, markers.filter(m => m.code !== 'MB2047')]]));
    this._lastDiagnostics = this._lastDiagnostics.filter(d => d.code !== 'MB2047' || d.file !== this.activeFile);
    document.querySelectorAll('#diagnostics-content .diagnostic-item').forEach(item => {
      if (item.dataset.code === 'MB2047' && item.dataset.file === this.activeFile) item.remove();
    });
    this.updateDiagnosticFilters();
  }

  refreshExecutionDiagnostics(model) {
    if (this._lastDiagnostics?.some(d => d.executionSnapshot && this.openFiles.get(d.file)?.model === model))
      this.applyDiagnostics(this._lastDiagnostics, this._lastDiagnosticsOnlyActive, this._lastDiagnosticsTargetFile);
  }

  diagnosticMatchesSnapshot(d) {
    if (!d.executionSnapshot) return true;
    const entry = this.openFiles.get(d.file);
    return !!entry && (d.executionSnapshot.documentId==null || entry.documentId === d.executionSnapshot.documentId) && !entry.model.isDisposed?.() &&
      entry.model.getValue() === d.executionSnapshot.sourceText;
  }

  reconcileDiagnosticPeek(byModel) {
    const model=this.editor?.getModel();
    if(!model||!byModel.has(model))return;
    const controller=this.editor.getContribution?.('editor.contrib.markerController');
    if(!controller?._widget)return;
    const position=controller._widget.position;
    if(!position){controller.close(false);return;}
    const markers=byModel.get(model);
    const present=markers.some(m=>(position.lineNumber>m.startLineNumber||position.lineNumber===m.startLineNumber&&position.column>=m.startColumn)&&
      (position.lineNumber<m.endLineNumber||position.lineNumber===m.endLineNumber&&position.column<=m.endColumn));
    if(!present)controller.close(false);
  }

  applyDiagnostics(diags, onlyActive = false, targetFile = null) {
    this.log('diagnostics.apply.start', { count: Array.isArray(diags) ? diags.length : 0, onlyActive, focus: this.focusState() });
    const monaco = this.monaco;
    const byModel = new Map();
    if (targetFile) {
      const target = this.openFiles.get(targetFile);
      if (target) byModel.set(target.model, []);
    } else if (onlyActive) {
      const active = this.activeFile ? this.openFiles.get(this.activeFile) : null;
      if (active) byModel.set(active.model, []);
    } else {
      for (const { model } of this.openFiles.values()) byModel.set(model, []);
    }

    for (const diagnostic of diags) {
      const entry = this.openFiles.get(diagnostic.file);
      if (entry && !byModel.has(entry.model)) byModel.set(entry.model, []);
    }

    this.assistant?.acceptDiagnostics(byModel.keys());
    const el = document.getElementById('diagnostics-content');
    el.innerHTML = '';
    this._lastDiagnostics = diags;
    this.studio?.updateDiagnostics();
    this._lastDiagnosticsOnlyActive = onlyActive;
    this._lastDiagnosticsTargetFile = targetFile;

    if (!diags.length) {
      for (const model of byModel.keys()) monaco.editor.setModelMarkers(model, 'axiom', []);
      this.reconcileDiagnosticPeek(byModel);
      this.updateDiagnosticFilters();
      this.log('diagnostics.apply.end', { count: 0, focus: this.focusState() });
      return;
    }

    for (const d of diags) {
      const category = this.diagnosticCategory(d);
      const sev = category === 'advice' ? monaco.MarkerSeverity.Hint : d.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning;
      const line = Math.max(1, d.line || 1);
      const col = Math.max(1, d.col || 1);
      const len = Math.max(1, d.length || 1);

      const model = this.findModelForFile(d.file);
      if (model && byModel.has(model) && this.diagnosticMatchesSnapshot(d)) {
        byModel.get(model).push({
          startLineNumber: line, startColumn: col,
          endLineNumber: line, endColumn: col + len,
          message: this.localizedDiagText(d),
          severity: sev,
          code: d.code,
          ...(category === 'advice' ? { tags: [monaco.MarkerTag.Unnecessary] } : {}),
        });
      }

      const item = document.createElement('div');
      item.className = 'diagnostic-item';
      item.dataset.category = category;
      item.dataset.code = d.code;
      const icon = document.createElement('span');
      icon.className = 'diagnostic-icon ' + category + (category === 'advice' ? ' codicon codicon-lightbulb' : '');
      icon.textContent = category === 'advice' ? '' : d.severity === 'error' ? '\u2716' : '\u26A0\uFE0E';
      const message = document.createElement('span');
      message.className = 'diagnostic-message';
      message.textContent = this.localizedDiagText(d);
      const location = document.createElement('span');
      location.className = 'diagnostic-location';
      location.textContent = `${d.file ? d.file.split(/[/\\]/).pop() : ''}:${line}:${col}`;
      item.dataset.file = d.file || ''; // lets closeFile prune stale rows
      item.appendChild(icon);
      item.appendChild(message);
      item.appendChild(location);
      item.addEventListener('click', () => this.jumpToDiagnostic(d));
      el.appendChild(item);
    }

    for (const [model, markers] of byModel) monaco.editor.setModelMarkers(model, 'axiom', markers);
    this.reconcileDiagnosticPeek(byModel);
    if ([...byModel.values()].some(markers => markers.some(marker => marker.severity === monaco.MarkerSeverity.Error))) {
      // Показываем ошибки сразу, даже если панель скрыта или выбран другой фильтр.
      // Фокус редактора сохраняется, чтобы пользователь мог продолжить набор.
      this._diagnosticFilter = 'all';
      this.showPanel();
      this.switchPanel('diagnostics');
    }
    this.updateDiagnosticFilters();
    this.log('diagnostics.apply.end', { count: diags.length, focus: this.focusState() });
  }

  jumpTo(file, line, col) {
    if(this.assistant){
      const target=this.findModelForFile(file);
      const entry=[...this.openFiles].find(([,doc])=>doc.model===target);
      if(entry)this.assistant.navigate(entry[0],line,col);
      return;
    }
    const model = this.findModelForFile(file);
    if (!model) return; // the file's editor is gone — do NOT move the caret of whatever is active
    for (const [p, f] of this.openFiles) {
      if (f.model === model && p !== this.activeFile) { this.switchToFile(p); break; }
    }
    if (!this.activeFile) return;
    this.editor.setPosition({ lineNumber: line, column: col });
    this.editor.revealLineInCenter(line);
    this.editor.focus();
  }

  localizedDiagText(d) {
    const text = window.DiagnosticLanguage.text(d, this.settings.language);
    if (this.diagnosticMatchesSnapshot(d)) return text;
    return text + (this.settings.language === 'en'
      ? ' [Error in the executed version; the current document has changed or was closed.]'
      : ' [Ошибка запущенной версии; текущий документ изменён или был закрыт.]');
  }

  findMarkerAt(pos) {
    const model = this.editor.getModel();
    if (!model || !pos) return null;
    const markers = this.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri });
    return markers.find((m) => m.startLineNumber <= pos.lineNumber && pos.lineNumber <= m.endLineNumber
      && m.startColumn <= pos.column && pos.column <= m.endColumn) || null;
  }

  // Alt+F8 entry point: the marker at the caret wins, then the next marker after the caret
  // (wrapping to the first), then the first marker. No-op when the file has no problems.
  showMarkerPeekAtCaret() {
    const model = this.editor.getModel();
    if (!model) return;
    const markers = this.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model.uri });
    if (!markers.length) return;
    const sorted = markers.slice().sort((a, b) => (b.severity - a.severity)
      || (a.startLineNumber - b.startLineNumber) || (a.startColumn - b.startColumn));
    const pos = this.editor.getPosition();
    const at = pos && sorted.find((m) => m.startLineNumber <= pos.lineNumber && pos.lineNumber <= m.endLineNumber
      && m.startColumn <= pos.column && pos.column <= m.endColumn);
    const after = pos && sorted.find((m) => m.startLineNumber > pos.lineNumber
      || (m.startLineNumber === pos.lineNumber && m.startColumn > pos.column));
    this.showErrorDetailsForMarker(at || after || sorted[0]);
  }

  // Opens the same Monaco marker peek view as Alt+F8, anchored at the given marker.
  showErrorDetailsForMarker(marker) {
    const controller = this.editor.getContribution('editor.contrib.markerController');
    if (!controller || typeof controller.showAtMarker !== 'function') return;
    // Opening the error details also surfaces the Problems tab: the panel becomes
    // visible (if hidden) and "Проблемы" becomes its active tab.
    this.showPanel();
    this.switchPanel('diagnostics');
    this.editor.setPosition({ lineNumber: marker.startLineNumber, column: marker.startColumn });
    // Monaco's showAtMarker walks a severity-sorted marker list: with an error elsewhere
    // a WARNING click lands on the ERROR. Bootstrap the lazy peek widget, then show the
    // EXACT clicked marker through the widget with its own 1-based index in that same
    // sorted list — the clicked problem is always the one displayed.
    controller.showAtMarker(marker);
    const model = this.editor.getModel();
    const sorted = this.monaco.editor.getModelMarkers({ owner: 'axiom', resource: model ? model.uri : undefined })
      .slice()
      .sort((a, b) => (b.severity - a.severity) || (a.startLineNumber - b.startLineNumber) || (a.startColumn - b.startColumn) || a.message.localeCompare(b.message));
    const idx = sorted.findIndex((m) => m.startLineNumber === marker.startLineNumber && m.startColumn === marker.startColumn
      && m.endLineNumber === marker.endLineNumber && m.endColumn === marker.endColumn && m.message === marker.message);
    if (controller._widget && typeof controller._widget.showAtMarker === 'function' && idx >= 0) {
      controller._widget.showAtMarker(sorted[idx], idx + 1, sorted.length);
    }
    // The peek grabs focus asynchronously on show; make sure the editor wins the race.
    this.editor.focus();
    setTimeout(() => { if (!this.editor.hasTextFocus()) this.editor.focus(); }, 0);
  }

  // Problems panel: a click navigates AND opens the extended error details in the editor.
  jumpToDiagnostic(d) {
    if(d.file && !this.openFiles.has(d.file) && this.openFile && (!d.executionSnapshot || d.executionSnapshot.documentId==null)) {
      return this.openFile(d.file).then(()=>{
        if(this.openFiles.has(d.file)) {
          this.applyDiagnostics(this._lastDiagnostics || [d],this._lastDiagnosticsOnlyActive,this._lastDiagnosticsTargetFile);
          this.jumpToDiagnostic(d);
        }
      }).catch(error=>this.log('diagnostics.document.failed',{message:error.message}));
    }
    if (!this.diagnosticMatchesSnapshot(d)) { this.setStatus(this.localizedDiagText(d)); return; }
    const line = Math.max(1, d.line || 1);
    const col = Math.max(1, d.col || 1);
    this.jumpTo(d.file, line, col);
    const marker = this.findMarkerAt({ lineNumber: line, column: col });
    if (marker) this.showErrorDetailsForMarker(marker);
  }

  // ================= panels =================

  // IDE-level failure (an IPC invoke threw / xterm.js failed to load): surface it as a
  // synthetic problem row in Problems. line/col 0 keep the row out of the editor
  // squiggles; the text carries the explanation.
  showIdeProblem(messageKey, detail) {
    const msg = I18N.t(messageKey) + (detail ? ' ' + detail : '');
    this.applyDiagnostics([{
      file: this.activeFile || '', line: 0, col: 0, severity: 'error', code: 'AXIOM-IDE',
      message: msg,
    }], true);
    this.showPanel();
    this.switchPanel('diagnostics');
  }

  switchPanel(name) {
    if(name==='structure'&&this.studio){this.studio.select('outline');return;}
    document.getElementById('diagnostic-filters').hidden = name !== 'diagnostics';
    this.log('panel.switch', { name, focus: this.focusState() });
    document.querySelectorAll('.panel-tab').forEach((t) => {
      t.classList.toggle('active', t.dataset.tab === name);
    });
    document.querySelectorAll('.tab-content').forEach((c) => {
      c.classList.toggle('active', c.id === `${name}-content`);
    });
    // The terminal measures itself against a visible container: after the tab becomes
    // visible, refit so xterm and the pty agree on cols/rows.
    if (name === 'console') setTimeout(() => this.fitConsole(), 0);
    this.saveSettings({bottomPanelTab:name});
    this.studio?.updateBottomTabs(name);
  }

  togglePanel() { this.setBottomPanelVisible(document.getElementById('bottom-panel').style.display === 'none'); }

  setBottomPanelVisible(visible) {
    this.bottomSplitter?.cancel();
    document.getElementById('bottom-panel').style.display = visible ? 'flex' : 'none';
    document.getElementById('panel-resizer').hidden = !visible;
    this.saveSettings({bottomPanelVisible: visible});
    this.bottomSplitter?.layout(); this.updateOutputMenuCheck();
    if(!visible&&document.getElementById('bottom-panel').contains(document.activeElement))this.focusEditor();
  }

  showPanel() { this.setBottomPanelVisible(true); }

  // View menu affordance: "Нижняя панель" shows its ✓ exactly when the panel is visible.
  updateOutputMenuCheck() {
    const visible = document.getElementById('bottom-panel').style.display !== 'none';
    UIControls.setPressed(document.getElementById('tb-toggle-output'), visible);
    const check = document.querySelector('.menu-check[data-check="outputPanel"]');
    UIControls.setMenuChecked(check, visible);
    this.studio?.bottomVisible(visible);
  }

  // ================= theme / language =================

  // Applies both the CSS theme attribute and the classic/modern chrome marker so the
  // stylesheet can target a theme family instead of listing every theme id.
  applyDocumentTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-chrome', window.AxiomThemes.kind(theme));
  }

  updateThemeMenuChecks() {
    for (const def of window.AxiomThemes.list) {
      UIControls.setMenuChecked(document.querySelector(`.menu-check[data-check="theme-${def.id}"]`), this.settings.theme === def.id);
    }
  }

  setTheme(theme) {
    if (!window.AxiomThemes.has(theme)) return;
    this.saveSettings({ theme });
    this.applyDocumentTheme(theme);
    this.monaco?.editor.setTheme('axiom-' + theme);
    if (this.term) this.term.options.theme = this.consoleTheme();
    this.updateThemeMenuChecks();
  }

  setLanguage(lang) {
    this.saveSettings({ language: lang });
    I18N.setLocale(lang);
    this.applyLocale();
    // Diagnostic texts (markers, Problems, error details) are re-inked immediately:
    // re-render the last diagnostics list and re-check the active buffer so the switch
    // is visible without any restart.
    if (this._lastDiagnostics) this.applyDiagnostics(this._lastDiagnostics, this._lastDiagnosticsOnlyActive, this._lastDiagnosticsTargetFile);
    this.scheduleLiveCheck();
  }

  applyLocale() {
    I18N.apply();
    this.projectSession?.render();
    this.assistant?.localize();
    UIControls.setLabel(document.getElementById('menu-settings'), I18N.t('menu.settings'));
    this.applyMenuMnemonics();
    document.querySelectorAll('.xs-context-menu [data-translation]').forEach(row => {
      UIControls.setLabel(row, I18N.t(row.dataset.translation), { tooltip: false });
    });
    for (const direction of ['left', 'right']) {
      const button = document.getElementById('tabs-scroll-' + direction);
      const label = this.settings.language === 'en'
        ? 'Scroll tabs ' + direction
        : 'Прокрутить вкладки ' + (direction === 'left' ? 'влево' : 'вправо');
      UIControls.setLabel(button, label);
    }
    this.rebuildOutline();
    if(this.studio)this.studio.render();else this.setProjectExplorerVisible(this.settings.projectExplorer !== false);
    this.setStatusBarVisible(this.settings.statusBarVisible !== false);
  }

  setStatus(t) { document.getElementById('status-text').textContent = t; }

  setDebugCurrentElement(item) {
    const element = document.getElementById('status-current-element');
    if (!element) return;
    if (!item) {
      element.textContent = '';
      element.title = '';
      element.hidden = true;
      return;
    }
    const summary = I18N.t('trace.currentElement', item.name, item.type, item.value);
    element.textContent = summary;
    element.title = summary;
    element.hidden = false;
  }

  focusEditor() {
    this.editor?.focus();
  }

  // ================= recent files / about =================

  applyMenuMnemonics() {
    document.querySelectorAll('.menu-label, .menu-dropdown-item .menu-text, .recent-item .recent-name').forEach(element => {
      if (element.querySelector('.menu-mnemonic')) return;
      const text = element.textContent;
      if (!text) return;
      const letter = document.createElement('span');
      letter.className = 'menu-mnemonic';
      letter.textContent = Array.from(text)[0];
      element.replaceChildren(letter, document.createTextNode(text.slice(letter.textContent.length)));
    });
  }

  renderRecentFiles() {
    const container = document.getElementById('recent-files-list');
    container.innerHTML = '';
    if (!this.settings.recentFiles || !this.settings.recentFiles.length) {
      const empty = document.createElement('div');
      empty.className = 'recent-empty';
      empty.textContent = I18N.t('recent.empty');
      container.appendChild(empty);
      return;
    }
    for (const p of this.settings.recentFiles.slice(0, 12)) {
      if (typeof p !== 'string' || !p) continue; // skip corrupted entries
      const item = document.createElement('div');
      item.className = 'recent-item';
      const icon = document.createElement('span');
      icon.className = 'recent-icon codicon codicon-history';
      const name = document.createElement('span');
      name.className = 'recent-name';
      name.textContent = p.split(/[/\\]/).pop() || p;
      const dir = document.createElement('span');
      dir.className = 'recent-path';
      const parts = p.split(/[/\\]/);
      parts.pop();
      dir.textContent = parts.join('\\');
      item.appendChild(icon);
      item.appendChild(name);
      item.appendChild(dir);
      item.addEventListener('click', () => this.openFile(p));
      container.appendChild(item);
    }
    this.applyMenuMnemonics();
  }

  async showAbout() {
    let info = {};
    try { info = await API.getVersion(); } catch { /* show what we know */ }
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.innerHTML = `
      <div class="modal about-dialog">
        <div class="about-head">
          <img class="brand-logo-lg logo-dark" src="assets/logo-dark.png" alt="Axiom" draggable="false">
          <img class="brand-logo-lg logo-light" src="assets/logo-light.png" alt="Axiom" draggable="false">
          <h3>${I18N.t('about.title')}</h3>
        </div>
        <div class="about-row"><span>${I18N.t('about.ide')}</span><code>${info.ide || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.lang')}</span><code>${info.language || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.cli')}</span><code>${info.compiler || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.vm')}</span><code>BytecodeVm</code></div>
        <div class="about-row"><span>${I18N.t('about.monaco')}</span><code>${info.monaco || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.electron')}</span><code>${info.electron || I18N.t('about.notFound')}</code></div>
        <button id="about-close">${I18N.t('about.ok')}</button>
      </div>`;
    document.body.appendChild(overlay);
    const closeButton = overlay.querySelector('#about-close');
    if (document.documentElement.dataset.theme === 'borland') {
      const chars = Array.from(closeButton.textContent || '');
      if (chars.length > 1) {
        closeButton.replaceChildren(
          Object.assign(document.createElement('span'), { className: 'about-button-default-text', textContent: chars[0] }),
          Object.assign(document.createElement('span'), { className: 'about-button-hotkey', textContent: chars[1] }),
          document.createTextNode(chars.slice(2).join('')),
        );
      }
    }
    closeButton.onclick = () => overlay.remove();
    overlay.onclick = (e) => { if (e.target === overlay) overlay.remove(); };
  }

  openEventLog() {
    this.eventLogDialog ||= new window.EventLogDialog(this);
    this.eventLogDialog.show();
  }

  async openLogFolder() {
    this.log('logs.open.request');
    try {
      const result = await API.openLogFolder?.();
      if (!result || !result.ok) {
        this.log('logs.open.failed', { error: result?.error || 'unknown' }, 'error');
        this.setStatus(I18N.t('status.error'));
        return;
      }
      this.log('logs.opened', { directory: result.directory });
    } catch (e) {
      this.log('logs.open.exception', { message: e?.message || String(e) }, 'error');
      this.setStatus(I18N.t('status.error'));
    }
  }

  // ================= persistence =================

  persistRecovery(excluded = null) {
    if (!this._recoveryEnabled) return true;
    const state = [];
    for (const [p, f] of this.openFiles) {
      if (excluded?.has(f)) continue;
      const discarded = !!this._discardedOnExit?.has(f);
      if (discarded && p.startsWith('untitled')) continue;
      state.push([f, p, f.documentId, f.revision, f.savedText, !!f.missing, !!f.readonly, p === this.activeFile, discarded]);
    }
    const previous = this._recoverySnapshotState;
    if (!this._recoveryFailed && previous?.length === state.length &&
        state.every((row, index) => row.every((value, column) => value === previous[index][column]))) return true;
    const tabs = [];
    for (const [p, f] of this.openFiles) {
      if (excluded?.has(f)) continue;
      const discarded = this._discardedOnExit?.has(f);
      if (discarded && p.startsWith('untitled')) continue;
      tabs.push({ path: p, isActive: p === this.activeFile, content: discarded ? f.savedText : f.model.getValue(),
        savedText: f.savedText, isDirty: !discarded && (p.startsWith('untitled') ? f.model.getValue().length > 0 : this.openFiles.isDirty(p)),
        missing: !!f.missing, readonly: !!f.readonly });
    }
    let result;
    try { result = API.saveRecoverySync(tabs); }
    catch (error) { result = { ok: false, message: error.message }; }
    if (!result?.ok) { this._recoveryFailed = true; this.setStatus('Не удалось сохранить данные восстановления: ' + (result?.message || 'Нет ответа')); }
    else { this._recoveryFailed = false; this._recoverySnapshotState = state; }
    return !!result?.ok;
  }

  saveOpenTabs(useSync = false) {
    const active = this.openFiles.get(this.activeFile);
    if (active && this.editor.getModel() === active.model) active.viewState = this.editor.saveViewState();
    this.persistRecovery();
    const tabs = [];
    for (const [p, f] of this.openFiles) {
      const discarded = this._discardedOnExit?.has(f);
      if (discarded && p.startsWith("untitled")) continue;
      const isDirty = !discarded && this.dirty.has(p);
      tabs.push({ path: p, isActive: p === this.activeFile, content: isDirty ? f.model.getValue() : undefined, viewState: f.viewState });
    }
    // On window teardown the async invoke loses the race with the renderer being
    // destroyed; the synchronous send guarantees the final openTabs snapshot reaches
    // the main process.
    const patch = { openTabs: tabs,
      lastProjectDir: this.projectDir || this.settings.lastProjectDir || null,
      lastProjectFile: this.projectSession.descriptor?.projectFilePath || this.settings.lastProjectFile || null };
    if (useSync && API.saveSettingsSync) API.saveSettingsSync(patch);
    else this.saveSettings(patch);
  }

  async restoreTabs() {
    const recovery = await API.getRecovery();
    const tabs = recovery ?? this.settings.openTabs;
    const views = new Map((this.settings.openTabs || []).map(tab => [tab.path, tab.viewState]));
    if (Array.isArray(tabs) && tabs.length) {
      for (const tab of tabs) {
        if (tab.path && !tab.path.startsWith('untitled')) {
          try {
            const disk = await API.readFileContent(tab.path);
            const restored = typeof tab.content === 'string' && (recovery ? tab.isDirty : tab.content !== disk);
            const model = this.monaco.editor.createModel(restored ? tab.content : disk.replace(/^\uFEFF/, ''), /\.axm$/i.test(tab.path) ? 'axiom' : 'plaintext', this.monaco.Uri.file(tab.path));
            this.openFiles.set(tab.path, { model, viewState: views.get(tab.path) || null, readonly: !!tab.readonly });
            this.openFiles.get(tab.path).savedText = restored ? (tab.savedText ?? disk.replace(/^\uFEFF/, '').replace(/\r\n?/g, model.getEOL())) : model.getValue();
            this.openFiles.get(tab.path).missing = !!(restored && tab.missing);
            this.createTab(tab.path);
            if (restored) this.markDirty(tab.path, true);
            if (tab.isActive) this.switchToFile(tab.path);
          } catch {
            if (typeof tab.content !== 'string') continue;
            const model = this.monaco.editor.createModel(tab.content, /\.axm$/i.test(tab.path) ? 'axiom' : 'plaintext', this.monaco.Uri.file(tab.path));
            this.openFiles.set(tab.path, { model, viewState: views.get(tab.path) || null, readonly: !!tab.readonly });
            const entry = this.openFiles.get(tab.path); entry.savedText = tab.savedText ?? tab.content; entry.missing = true;
            this.createTab(tab.path); this.markDirty(tab.path, true);
            if (tab.isActive) this.switchToFile(tab.path);
          }
          API.watchFile(tab.path).catch(() => {});
        } else if (tab.path && typeof tab.content === 'string') {
          const model = this.monaco.editor.createModel(tab.content, 'axiom');
          this.openFiles.set(tab.path, { model, viewState: views.get(tab.path) || null });
          this.openFiles.get(tab.path).savedText = tab.savedText ?? '';
          this.createTab(tab.path);
          this.markDirty(tab.path, true);
          if (tab.isActive) this.switchToFile(tab.path);
          const m = tab.path.match(/^untitled(\d+)\.axm$/);
          if (m) this.untitled = Math.max(this.untitled, parseInt(m[1], 10));
        }
      }
      if (!this.activeFile && this.openFiles.size) this.switchToFile([...this.openFiles.keys()][0]);
      if (this.settings.smartIdentifierEnabled) {
        for (const path of this.openFiles.keys()) await this.formatWholeDocument('ax-smart-restore', path, 'document.normalizeIdentifiers');
      }
      // The restored buffer must be re-checked so the diagnostics (markers + Problems)
      // reappear right away.
      if (this.activeFile) this.scheduleLiveCheck();
      if (this.openFiles.size) return;
    }
  }
}

window.__ide = new AxiomIDE();
