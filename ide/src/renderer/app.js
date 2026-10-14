const API = window.axiom;

class AxiomIDE {
  constructor() {
    this.monaco = null;
    this.langMeta = null; // language registry from `axiom metadata`; null until loaded
    this.editor = null;
    this._recoveryEnabled = false;
    this.openFiles = new window.DocumentStore(() => { this.refreshDirtyIndicators(); this.persistRecovery(); });
    this.editedLines = new window.EditedLines();
    this.documentServices = new window.DocumentServices(this.openFiles, API);
    this.documentSaver = new window.DocumentSaver(this.openFiles, API);
    this.dirty = new Set();
    this.activeFile = null;
    this.projectDir = null;
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
      await this.loadSettings();
      if (!['dark', 'light', 'basic', 'borland'].includes(this.settings.theme)) this.settings.theme = 'dark';
      document.documentElement.setAttribute('data-theme', this.settings.theme);
      I18N.setLocale(this.settings.language);
      await this.loadMonaco();
      await this.loadLanguageMetadata();
      // Подключаем службы до регистрации языка: первый вызов Monaco фиксирует их.
      this.createEditor();
      this.setupLanguage();
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
      if (!this.openFiles.size) this.newFile();
      this._recoveryEnabled = true;
      this.persistRecovery();
      this.renderRecentFiles();
      this.applyLocale();
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
    document.documentElement.setAttribute('data-theme',theme);
    this.monaco.editor.setTheme('axiom-'+theme);
    if(this.term) this.term.options.theme=this.consoleTheme();
    this.updateThemeMenuChecks();
    I18N.setLocale(language);this.applyLocale();
    if(changedLanguage) { if(this._lastDiagnostics)this.applyDiagnostics(this._lastDiagnostics,this._lastDiagnosticsOnlyActive);this.scheduleLiveCheck(); }
    this.log('settings.editor.applied',{count:Object.keys(normalized).length,theme,language});
  }

  loadMonaco() {
    return new Promise((resolve, reject) => {
      let settled = false;
      const done = (ok) => { if (!settled) { settled = true; ok ? resolve() : reject(new Error('monaco load')); } };
      const timer = setTimeout(() => done(false), 15000);
      // Monaco's own UI strings intentionally stay English in both locales:
      // only compiler diagnostics and the IDE's own UI are localized.
      require.config({ paths: { vs: 'vs' } });
      require(['vs/editor/editor.main'], (monaco) => { clearTimeout(timer); this.monaco = monaco; done(true); }, () => done(false));
    });
  }

  // ================= language (highlighting) =================

  // Monarch dictionaries come from `axiom metadata` (the language registry of the CLI).
  // Embedded lists below are only a fallback when axiom.exe is missing or broken.
  static FALLBACK_DICTS = {
    keywords: ['AND','APPEND','AS','BEGINFRAME','BYREF','BYVAL','CAMERA','CANVAS','CASE','CATCH','CIRCLE','CLOSE','CLS','COLOR','CONST','CONTINUE','DIM','DO','DRAW','DRAWCIRCLE','DRAWIMAGE','DRAWLINE','DRAWRECT','DRAWSPRITE','DRAWTILE','DRAWTILELAYER','DRAWTILEMAP','DRAWTEXT','ELSE','ELSEIF','END','ERROR','EQV','ERASE','EXIT','FALSE','TRUE','FILLCIRCLE','FILLRECT','FINALLY','FOR','FUNCTION','GET','IF','IMP','INPUT','IS','KEY','LINE','LOCATE','LOOP','MOD','MOUSE','NEXT','NOT','OFF','ON','OPEN','OR','OUTPUT','PAINT','PALETTE','PCOPY','PRESERVE','PRESENT','PRESET','PRINT','PSET','PUT','REDIM','RESETCAMERA','RETURN','SCREEN','SELECT','SELF','SETMAPTILE','SMOOTH','SLEEP','STEP','STOP','SUB','THEN','TO','TRANSPARENT','TRY','TYPE','UNTIL','USING','VIEW','VIEWPORT','WHILE','WINDOW','XOR'],
    typeNames: ['BOOLEAN','DATE','DATETIME','DECIMAL','DOUBLE','FILE','IMAGE','INTEGER','LONG','SPRITE','STRING','TILEMAP','TILESET','TIME'],
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
          [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],
          [/&H[0-9a-fA-F]+/, 'number.hex'],
          [/&B[01]+/, 'number.binary'],
          [/\d+\.\d*(#[DRFdrf]?|\d*)/, 'number'],
          [/\d+[%&#DRFSILdrfsil]?/, 'number'],
          [/<>|<=|>=|=|<|>/, 'operator'],
          [/\+|-|\*|\/|\\|\^|&/, 'operator'],
          [/[()]/, '@brackets'],
          [/[,;:]/, 'delimiter'],
          [/[^\x00-\x7F\s]+/, 'invalid'],
          [/[a-zA-Z_][a-zA-Z0-9_]*/, {
            cases: {
              '@keywords': 'keyword',
              '@typeKeywords': 'type',
              '@builtins': 'support.function',
              '@default': 'identifier',
            },
          }],
        ],
        string: [
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          [/[^"]+/, 'string'],
        ],
        // $"...{expression}...": the hole is highlighted as code, the markers as delimiters.
        interpolatedString: [
          [/\{/, { token: 'delimiter.interpolation', bracket: '@open', next: '@interpolation' }],
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          [/[^"{]+/, 'string'],
        ],
        interpolation: [
          [/"/, { token: 'string.quote', bracket: '@open', next: '@nestedString' }],
          [/\{/, { token: '@brackets', bracket: '@open', next: '@push' }],
          [/\}/, { token: 'delimiter.interpolation', bracket: '@close', next: '@pop' }],
          [/&H[0-9a-fA-F]+/, 'number.hex'],
          [/&B[01]+/, 'number.binary'],
          [/\d+\.\d*(#[DRFdrf]?)?/, 'number'],
          [/\d+%?[DL]?/, 'number'],
          [/<>|<=|>=|=|<|>/, 'operator'],
          [/\+|-|\*|\/|\\|\^|&/, 'operator'],
          [/[()]/, '@brackets'],
          [/[,;:]/, 'delimiter'],
          [/[^\x00-\x7F\s]+/, 'invalid'],
          [/[a-zA-Z_][a-zA-Z0-9_]*/, {
            cases: {
              '@keywords': 'keyword',
              '@typeKeywords': 'type',
              '@builtins': 'support.function',
              '@default': 'identifier',
            },
          }],
          [/\s+/, 'white'],
        ],
        nestedString: [
          [/""/, 'string.escape'],
          [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }],
          [/[^"]+/, 'string'],
        ],
      },
    });

    monaco.languages.registerDocumentSemanticTokensProvider('axiom', {
      getLegend: () => window.AxiomSemanticColors.legend,
      provideDocumentSemanticTokens: model => window.AxiomSemanticColors.classify(model.getValue(), monaco.editor.tokenize(model.getValue(), 'axiom')),
      releaseDocumentSemanticTokens() {},
    });

    // Круглые скобки выражений участвуют в сопоставлении и раскраске вложенности.
    monaco.languages.setLanguageConfiguration('axiom', {
      comments: { lineComment: "'" },
      brackets: [['(', ')']],
      colorizedBracketPairs: [['(', ')']],
      indentationRules: {
        increaseIndentPattern: /^\s*(?:(?:If|ElseIf)\b.*\bThen|Else|For\b.*|While\b.*|Do(?:\s+(?:While|Until)\b.*)?|(?:Sub|Function|Type)\b.*|Select\s+Case\b.*|Case\b.*|Try|Catch\b.*|Finally)\s*(?:'.*)?$/i,
        decreaseIndentPattern: /^\s*(?:End\s+(?:If|While|Sub|Function|Type|Select|Try)\b|Next\b|Loop\b|Else(?:If)?\b|Case\b|Catch\b|Finally\b)/i,
      },
      autoClosingPairs: [
        { open: '(', close: ')' },
        { open: '"', close: '"' },
      ],
      surroundingPairs: [
        { open: '(', close: ')' },
        { open: '"', close: '"' },
      ],
    });

    monaco.editor.defineTheme('axiom-light', {
      base: 'vs', inherit: true, semanticHighlighting: true,
      semanticTokenColors: { keyword: '#0000ff' },
      rules: [
        { token: 'comment', foreground: '6a9955', fontStyle: 'italic' },
        { token: 'string', foreground: 'a31515' },
        { token: 'string.escape', foreground: 'ee0000' },
        { token: 'delimiter.interpolation', foreground: 'ee0000' },
        { token: 'number', foreground: '098658' },
        { token: 'number.hex', foreground: '098658' },
        { token: 'number.binary', foreground: '098658' },
        { token: 'keyword', foreground: '0000ff' },
        { token: 'type', foreground: '267f99' },
        { token: 'support.function', foreground: '795e26' },
        { token: 'identifier', foreground: '215E9C' },
        { token: 'invalid', foreground: 'C40000' },
        { token: 'variable', foreground: '215E9C' },
        { token: 'variable.readonly', foreground: '8F4400' },
        { token: 'function', foreground: '795E26' },
        { token: 'array', foreground: '7552A5' },
        { token: 'operator', foreground: 'AF1970' },
        { token: 'delimiter', foreground: '705A00' },
      ],
      colors: {
        ...AxiomIDE.monacoSurface('light'),
        'editorLineNumber.foreground': '#8a98ab',
        'editorLineNumber.activeForeground': '#536174',
        'editorIndentGuide.background1': '#e6ebf2',
        'editorIndentGuide.activeBackground1': '#c4d1e1',
      },
    });

    monaco.editor.defineTheme('axiom-dark', {
      base: 'vs-dark', inherit: true, semanticHighlighting: true,
      semanticTokenColors: { keyword: '#c586c0' },
      rules: [
        { token: 'comment', foreground: '6a9955', fontStyle: 'italic' },
        { token: 'string', foreground: 'ce9178' },
        { token: 'string.escape', foreground: 'd7ba7d' },
        { token: 'delimiter.interpolation', foreground: 'd7ba7d' },
        { token: 'number', foreground: 'b5cea8' },
        { token: 'number.hex', foreground: 'b5cea8' },
        { token: 'number.binary', foreground: 'b5cea8' },
        { token: 'keyword', foreground: 'c586c0' },
        { token: 'type', foreground: '4ec9b0' },
        { token: 'support.function', foreground: 'dcdcaa' },
        { token: 'identifier', foreground: '9CDCFE' },
        { token: 'invalid', foreground: 'F44747' },
        { token: 'variable', foreground: '9CDCFE' },
        { token: 'variable.readonly', foreground: 'F0B975' },
        { token: 'function', foreground: 'DCDCAA' },
        { token: 'array', foreground: 'B9A3E3' },
        { token: 'operator', foreground: 'E69BC8' },
        { token: 'delimiter', foreground: 'D7BA7D' },
      ],
      colors: {
        ...AxiomIDE.monacoSurface('dark'),
        'editorLineNumber.foreground': '#5f6c80',
        'editorLineNumber.activeForeground': '#a5afbf',
        'editorIndentGuide.background1': '#2a3240',
        'editorIndentGuide.activeBackground1': '#465064',
      },
    });

    monaco.editor.defineTheme('axiom-basic', {
      base: 'vs-dark', inherit: true, semanticHighlighting: true,
      semanticTokenColors: { keyword: '#FFFF55' },
      rules: [
        { token: 'comment', foreground: 'B8C7E8', fontStyle: 'italic' },
        { token: 'keyword', foreground: 'FFFF55' },
        { token: 'type', foreground: 'FFFFFF' },
        { token: 'support.function', foreground: '55FFFF' },
        { token: 'function', foreground: '55FFFF' },
        { token: 'identifier', foreground: 'FFFFFF' },
        { token: 'variable', foreground: 'FFFFFF' },
        { token: 'variable.readonly', foreground: 'FFFFAA' },
        { token: 'array', foreground: 'AAFFAA' },
        { token: 'string', foreground: 'AAFFAA' },
        { token: 'string.escape', foreground: '55FFFF' },
        { token: 'delimiter.interpolation', foreground: '55FFFF' },
        { token: 'number', foreground: 'FFFFAA' },
        { token: 'number.hex', foreground: 'FFFFAA' },
        { token: 'number.binary', foreground: 'FFFFAA' },
        { token: 'operator', foreground: 'FFFF55' },
        { token: 'delimiter', foreground: 'FFFFFF' },
        { token: 'invalid', foreground: 'FFAAAA' },
      ],
      colors: {
        ...AxiomIDE.monacoSurface('basic'),
        'editorLineNumber.foreground': '#B8C7E8',
        'editorLineNumber.activeForeground': '#FFFFFF', 'editorIndentGuide.background1': '#3030A0',
        'editorIndentGuide.activeBackground1': '#B8C7E8', 'editorError.foreground': '#FFAAAA', 'editorWarning.foreground': '#FFFF55',
      },
    });

    monaco.editor.defineTheme('axiom-borland', {
      base: 'vs-dark', inherit: true, semanticHighlighting: true,
      semanticTokenColors: {
        keyword: '#FFFFFF', type: '#FFFF55', function: '#FFFF55',
        variable: '#FFFF55', array: '#FFFF55', 'variable.readonly': '#FFFF55',
      },
      rules: [
        { token: 'comment', foreground: 'AAAAAA', fontStyle: 'italic' },
        { token: 'keyword', foreground: 'FFFFFF' },
        { token: 'type', foreground: 'FFFF55' },
        { token: 'support.function', foreground: 'FFFF55' },
        { token: 'function', foreground: 'FFFF55' },
        { token: 'identifier', foreground: 'FFFF55' },
        { token: 'variable', foreground: 'FFFF55' },
        { token: 'variable.readonly', foreground: 'FFFF55' },
        { token: 'array', foreground: 'FFFF55' },
        { token: 'string', foreground: 'FFFF55' },
        { token: 'string.escape', foreground: 'FFFFFF' },
        { token: 'delimiter.interpolation', foreground: 'FFFFFF' },
        { token: 'number', foreground: 'FFFF55' },
        { token: 'number.hex', foreground: 'FFFF55' },
        { token: 'number.binary', foreground: 'FFFF55' },
        { token: 'operator', foreground: 'FFFFFF' },
        { token: 'delimiter', foreground: 'FFFFFF' },
        { token: 'invalid', foreground: 'FF5555' },
      ],
      colors: {
        ...AxiomIDE.monacoSurface('borland'),
        'editor.foreground': '#FFFFFF',
        'editor.selectionForeground': '#FFFFFF',
        'editorLineNumber.foreground': '#8080FF',
        'editorLineNumber.activeForeground': '#FFFFFF',
        'editorIndentGuide.background1': '#5555FF',
        'editorIndentGuide.activeBackground1': '#C0C0C0',
        'editorError.foreground': '#FF5555',
        'editorWarning.foreground': '#FFFF55',
      },
    });

    // Completion + signature help + hover — builtins from the language registry
    // (ide.langMeta, loaded from `axiom metadata`), document locals from a light model scan.
    if (window.LangFeatures) LangFeatures.register(monaco, this);
  }

  createEditor() {
    this.editor = this.monaco.editor.create(document.getElementById('editor'), {
      value: '',
      language: 'plaintext',
      theme: this.settings.theme==='light'?'vs':'vs-dark',
      automaticLayout: true,
      'semanticHighlighting.enabled': true,
      fontSize: 14,
      fontFamily: "'Cascadia Code', Consolas, 'Courier New', monospace",
      minimap: { enabled: false },
      // Tooltip below the hovered line: with the default (above) the tooltip extends
      // above the editor's top edge when the code is at the top and gets hidden behind
      // the tab strip/toolbar.
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
      this.closeDiagnosticPeek();
      this.clearUsageAdvice();
      this.editedLines.record(this.editor.getModel(),event.changes,!this._autoFormatting);
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
      { id: 'command-palette', translation: 'menu.editor.commandPalette', key: 'F1', icon: 'codicon-symbol-misc' },
    ];
    let contextLine;
    const popup = UIControls.createContextMenu(menu, {translate: I18N.t,
      onClose: () => this.editor?.focus(), onAction: id => run(id)});
    const run = (id) => {
      // The menu button temporarily owns focus. Restore Monaco first.
      this.editor?.focus();
      if (id === 'trace-breakpoint') { if (this.trace.allowed('breakpoint')) this.trace.toggle(contextLine); }
      else if (id === 'trace-clear') this.trace.command('clear');
      else if (id === 'change-all') this.editor?.trigger('ax-context', 'editor.action.changeAllOccurrences');
      else if (id === 'command-palette') this.editor?.trigger('ax-context', 'editor.action.quickCommand');
      else this.handleAction(id);
    };
    document.addEventListener('contextmenu', (event) => {
      if (!event.target.closest?.('.monaco-editor')) return;
      event.preventDefault();
      event.stopPropagation();
      contextLine = this.editor.getTargetAtClientPoint(event.clientX, event.clientY)?.position?.lineNumber;
      popup.show(items.map(item => ({...item,
        enabled: !item.trace || (this.trace.allowed(item.trace) && (item.trace !== 'breakpoint' || !!contextLine))
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
    };
    const openMenuFor = item => { closeAll(); item.classList.add('open'); openMenu = item; };
    document.getElementById('menu-settings').addEventListener('click', () => {
      closeAll(); this.handleAction('editor-settings');
    });
    document.querySelectorAll('.menu-item').forEach(item => {
      const label = item.querySelector('.menu-label'); if (!label) return;
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
      trigger.addEventListener('keydown', e => { if (['Enter', ' ', 'ArrowRight'].includes(e.key)) { e.preventDefault(); open(); } });
    });
    document.querySelectorAll('.menu-dropdown-item').forEach(item => item.addEventListener('click', () => {
      const action = item.dataset.action; if (!action) return;
      closeAll(); this.handleAction(action);
    }));
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
        const name = (e.target).dataset.tab;
        if (name) this.switchPanel(name);
      });
    });
  }

  // Both panels use the same pointer, cancellation and size constraints.
  setupPanelResizer() {
    const {PanelSplitter} = window.PanelSplitters;
    this.bottomSplitter = new PanelSplitter({handle: document.getElementById('panel-resizer'), panel: document.getElementById('bottom-panel'), container: document.getElementById('main-content'), axis: 'y', minimum: 64, reserve: 140, defaultSize: 200, savedSize: this.settings.consoleHeight, onCommit: size => this.saveSettings({consoleHeight: size})});
    this.sidebarSplitter = new PanelSplitter({handle: document.getElementById('sidebar-resizer'), panel: document.getElementById('shell-sidebar'), container: document.getElementById('workspace'), axis: 'x', minimum: 160, reserve: 160, maximum: 480, defaultSize: 208, savedSize: this.settings.sidebarWidth, onCommit: size => this.saveSettings({sidebarWidth: size})});
    document.getElementById('sidebar-close').onclick = () => this.setSidebarVisible(false);
    document.getElementById('tb-toggle-sidebar').onclick = () => this.setSidebarVisible(this.settings.sidebarVisible === false);
    this.setSidebarVisible(this.settings.sidebarVisible !== false);
    this.setBottomPanelVisible(this.settings.bottomPanelVisible !== false);
  }

  setSidebarVisible(visible) {
    this.sidebarSplitter?.cancel();
    document.getElementById('shell-sidebar').hidden = !visible;
    document.getElementById('sidebar-resizer').hidden = !visible;
    this.saveSettings({sidebarVisible: visible});
    UIControls.setMenuChecked(document.querySelector('[data-check="sidebarVisible"]'), visible);
    UIControls.setPressed(document.getElementById('tb-toggle-sidebar'), visible);
    this.sidebarSplitter?.layout();
  }

  setToolbarVisible(visible) {
    document.getElementById('toolbar').hidden = !visible;
    this.saveSettings({toolbarVisible: visible});
    UIControls.setMenuChecked(document.querySelector('[data-check="toolbarVisible"]'), visible);
    requestAnimationFrame(() => { this.editor?.layout(); this.fitAddon?.fit(); });
  }

  setupKeyboardShortcuts() {
    // Capture phase: the integrated terminal's keydown handling stops the event before
    // bubble-phase listeners once xterm has focus, so F5 / Ctrl chords would never reach
    // the IDE while the console is focused.
    window.addEventListener('keydown', (e) => {
      if (document.querySelector('.editor-settings-overlay')) return;
      if ((e.ctrlKey || e.metaKey) && e.code === 'Comma') { e.preventDefault(); e.stopPropagation(); this.handleAction('editor-settings'); return; }
      const mod = e.ctrlKey || e.metaKey;
      const k = e.key.toLowerCase();
      // `event.key` follows the active keyboard layout, while `event.code` identifies the
      // physical key. IDE shortcuts must work independently of the layout.
      const shortcutKey = /^Key[A-Z]$/.test(e.code) ? e.code.slice(3).toLowerCase() : k;
      this.log('keyboard.down', { key: e.key, code: e.code, target: e.target?.id || e.target?.className || e.target?.tagName, mod, shift: e.shiftKey, alt: e.altKey, state: this.focusState() });

      // IDE shortcuts are consumed in the capture phase: preventDefault alone still lets
      // xterm see the key and echo its escape sequence into the running program's stdin.
      const consume = () => { e.preventDefault(); e.stopPropagation(); };

      if (mod && e.shiftKey && e.altKey && shortcutKey === 'f') { consume(); this.handleAction('format'); return; }
      if (mod && e.shiftKey && shortcutKey === 's') { consume(); this.handleAction('save-as'); return; }
      if (mod && e.shiftKey && shortcutKey === 'c') { consume(); this.handleAction('check'); return; }
      if (mod && e.shiftKey && shortcutKey === 'e') { consume(); this.handleAction('toggle-project-explorer'); return; }
      if (mod && !e.shiftKey && shortcutKey === 'b') { consume(); this.handleAction('toggle-sidebar'); return; }
      if (mod && e.shiftKey && shortcutKey === 'o') { consume(); this.handleAction('toggle-output'); return; }
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
      if (mod && shortcutKey === 's') { consume(); this.handleAction('save'); return; }
      if (mod && shortcutKey === 'n') { consume(); this.handleAction('new'); return; }
      if (mod && shortcutKey === 'o') { consume(); this.handleAction('open'); return; }
      if (mod && shortcutKey === 'w') { consume(); this.handleAction('close-tab'); return; }
      if (['F5','F9','F10','F11'].includes(e.key) && !e.altKey) {
        consume();
        if(e.key==='F5' && mod && !e.shiftKey) this.run('run');
        else this.trace.command(e.key==='F5' ? (e.shiftKey ? (mod?'restart':'stop') : 'start') :
          e.key==='F9' ? (mod&&e.shiftKey?'clear':'breakpoint') :
          e.key==='F10' ? (mod?'stepBack':'stepOver') : (e.shiftKey?'stepOut':'stepInto'));
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
      case 'open': this.openDialog(); break;
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
      case 'toggle-project-explorer': this.toggleProjectExplorer(); break;
      case 'theme-light': this.setTheme('light'); break;
      case 'theme-dark': this.setTheme('dark'); break;
      case 'theme-basic': this.setTheme('basic'); break;
      case 'theme-borland': this.setTheme('borland'); break;
      case 'lang-ru': this.setLanguage('ru'); break;
      case 'lang-en': this.setLanguage('en'); break;
      case 'open-log-folder': this.openLogFolder(); break;
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
    const dir = await API.openProjectDialog();
    if (dir) await this.openProject(dir);
  }

  // Проект в Axiom — это просто папка: конвенции main-файла нет, запускается активный
  // сохранённый файл (как и в WPF-IDE).
  async openProject(dir) {
    const files = await API.listProjectFiles(dir);
    this.projectDir = dir;
    this.saveSettings({ lastProjectDir: dir });
    this.renderProjectFiles(files);
    this.renderMiscellaneousFiles();
    if (!this.activeFile && files.length) await this.openFile(files[0]);
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
    const files = await API.listProjectFiles(this.projectDir);
    this.renderProjectFiles(files);
    await this.openFile(selectedPath);
  }

  async restoreProject() {
    const dir = this.settings.lastProjectDir;
    if (typeof dir !== 'string' || !dir) return false;
    const files = await API.listProjectFiles(dir);
    this.projectDir = dir;
    this.renderProjectFiles(files);
    // Tabs were restored before the project: the Miscellaneous Files group can only be
    // derived once the project folder is known.
    this.renderMiscellaneousFiles();
    return true;
  }

  renderProjectFiles(files) {
    const title = document.getElementById('project-title');
    const list = document.getElementById('project-files');
    title.textContent = this.projectDir.split(/[/\\]/).pop() || this.projectDir;
    title.classList.remove('project-open');
    list.innerHTML = '';
    for (const file of files) {
      const button = document.createElement('button');
      button.className = 'project-file' + (file === this.activeFile ? ' active' : '');
      button.textContent = file.slice(this.projectDir.length + 1).replace(/\\/g, '/');
      button.title = file;
      button.onclick = () => this.openFile(file);
      list.appendChild(button);
    }
  }

  // The "Прочие файлы" section under the project tree: the open saved documents that
  // live outside the current project — the ONLY surface where project membership is
  // visible. Visible only while a project is open AND at least one such document exists.
  renderMiscellaneousFiles() {
    const wrap = document.getElementById('external-files');
    const list = document.getElementById('external-files-list');
    if (!wrap || !list) return;
    list.innerHTML = '';
    const files = [...this.openFiles.keys()].filter((p) => this.isMiscellaneousFile(p));
    wrap.classList.toggle('hidden', !this.projectDir || files.length === 0);
    for (const p of files) {
      const button = document.createElement('button');
      button.className = 'project-file' + (p === this.activeFile ? ' active' : '');
      button.textContent = p.split(/[/\\]/).pop();
      button.title = p;
      button.onclick = () => this.switchToFile(p);
      list.appendChild(button);
    }
  }

  setupProjectExplorer() {
    document.getElementById('project-title').onclick = () => {
      if (!this.projectDir) this.openProjectDialog();
    };
    document.getElementById('project-explorer-close').onclick = () => this.setProjectExplorerVisible(false);
    document.getElementById('project-new-file').onclick = () => this.newProjectFile();
  }

  isProjectFile(p) {
    if (!this.projectDir || typeof p !== 'string') return false;
    const root = this.projectDir.replace(/[\\/]+$/, '').toLowerCase();
    const file = p.toLowerCase();
    return file.startsWith(root + '\\') || file.startsWith(root + '/');
  }

  // A miscellaneous file: a saved document from OUTSIDE the current project. The
  // distinction exists only while a project is open. Untitled buffers are NOT listed:
  // they are new documents without a path, not disk files that live outside the folder.
  isMiscellaneousFile(p) {
    return !!this.projectDir && !!p && !p.startsWith('untitled') && !this.isProjectFile(p);
  }

  async closeProject() {
    if (!this.projectDir) return;
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
    this.saveSettings({ lastProjectDir: null });
    const title = document.getElementById('project-title');
    title.textContent = I18N.t('project.open');
    title.classList.add('project-open');
    document.getElementById('project-files').innerHTML = '';
    // A project owns its open documents. Closing it releases their editor models and tabs,
    // while files opened from elsewhere deliberately remain available as independent files.
    for (const entry of approved.keys()) await this.closeFile(entry.path, true);
    // Without a project the distinction does not exist: the section hides itself.
    this.renderMiscellaneousFiles();
  }

  toggleProjectExplorer() {
    const visible = !document.getElementById('project-explorer').classList.contains('hidden');
    this.setProjectExplorerVisible(!visible);
  }

  setProjectExplorerVisible(visible) {
    document.getElementById('project-explorer').classList.toggle('hidden', !visible);
    this.saveSettings({ projectExplorer: visible });
    const check = document.querySelector('.menu-check[data-check="projectExplorer"]');
    UIControls.setMenuChecked(check, visible);
    setTimeout(() => this.editor?.layout(), 0);
  }

  async openFile(p) {
    if (this.openFiles.has(p)) {
      this.switchToFile(this.openFiles.get(p).path);
      // Re-opening an already-open file still bumps recency.
      if (!p.startsWith('untitled')) {
        API.addRecentFile(p).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
      }
      return;
    }
    let content, readonly;
    try { content = await API.readFileContent(p); readonly = await API.fileReadonly(p); }
    catch { alert(I18N.t('dialog.openFailed', p)); return; }
    const model = this.monaco.editor.createModel(content.replace(/^\uFEFF/, ''), /\.axm$/i.test(p) ? 'axiom' : 'plaintext', this.monaco.Uri.file(p));
    this.openFiles.set(p, { model, viewState: null, readonly });
    this.createTab(p);
    this.switchToFile(p);
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
    this.renderMiscellaneousFiles();
  }

  setupDocumentTabs() {
    const tabs = document.getElementById('editor-tabs');
    const left = document.getElementById('tabs-scroll-left');
    const right = document.getElementById('tabs-scroll-right');
    const update = () => {
      UIControls.setEnabled(left, tabs.scrollLeft > 1);
      UIControls.setEnabled(right, tabs.scrollLeft + tabs.clientWidth < tabs.scrollWidth - 1);
    };
    left.addEventListener('click', () => tabs.scrollBy({ left: -Math.max(100, tabs.clientWidth * 0.7) }));
    right.addEventListener('click', () => tabs.scrollBy({ left: Math.max(100, tabs.clientWidth * 0.7) }));
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
    document.querySelectorAll('.project-file').forEach((t) => t.classList.toggle('active', t.dataset.path === p || t.title === p));
    this.renderMiscellaneousFiles();
    this.updateSaveButton();
    this.editor.focus();
    this.rebuildOutline();
    this.persistRecovery();
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
      else { this.activeFile = null; this.editor.setModel(null); document.getElementById('status-file-path').textContent = ''; }
    }
    if (this.projectDir) this.refreshProjectFiles();
    this.renderMiscellaneousFiles();
    return true;
    } finally { f.closing = false; }
  }

  async refreshProjectFiles() {
    if (!this.projectDir) return;
    const files = await API.listProjectFiles(this.projectDir);
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

  async save(path = this.activeFile, saveAs = false) {
    const entry = this.openFiles.get(path);
    if (!entry) return false;
    path = entry.path;
    if (entry.readonly) { alert("Документ доступен только для чтения."); return false; }
    try {
      const result = await this.documentSaver.save(path, saveAs);
      if (!result) return false;
      if (result.path !== result.oldPath) await this.renameActive(result.path, result.oldPath);
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
    this.renderMiscellaneousFiles();
    this.updateSaveButton();
    API.addRecentFile(newPath).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
    if (this.projectDir && (this.isProjectFile(old) || this.isProjectFile(newPath))) await this.refreshProjectFiles();
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

  async formatWholeDocument(owner, path = this.activeFile, method = 'document.format') {
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
    const smartIdentifierEnabled = !!this.settings.smartIdentifierEnabled;
    const request = ++this._serviceSeq;
    clearTimeout(this._liveCheckTimer);
    this._liveCheckTimer = null;
    this._formatBusy++;
    this.log('format.document.start', { owner, length: text.length, request });
    try {
      const r = await this.documentServices.request(path, method, { formattingStyle: this.settings.formattingStyle || 'Classic', smartIdentifierEnabled });
      if (!r || typeof r.sourceText !== 'string') { this.log('format.document.failed', { owner, request, reason: r?.reason }); return; }
      const m = this.openFiles.get(path)?.model;
      if (this.running && path === this._runFile) return;
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
      return true;
    } catch (e) {
      this.log('format.document.exception', { owner, request, message: e?.message || String(e) }, 'error');
      this.showIdeProblem('status.fmtFailed', e.message);
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
    for (const path of [...this.openFiles.keys()]) await this.formatWholeDocument('ax-style-switch', path);
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
    clearTimeout(this._liveCheckTimer);
    const seq = this._liveCheckSeq;
    this._liveCheckTimer = setTimeout(() => this.runLiveCheck(seq).catch((e) => this.log('check.live.exception', { message: e?.message || String(e) }, 'error')), 600);
    this.log('check.live.scheduled', { seq });
  }

  async runLiveCheck(seq = this._liveCheckSeq) {
    const model = this.editor.getModel();
    if (!model || model.getLanguageId() !== 'axiom' || seq !== this._liveCheckSeq) return;
    if (this._formatBusy) { this.log('check.live.skipped-format', { seq, busy: this._formatBusy }); return; }
    const text = model.getValue();
    const filePath = this.activeFile; // keep diagnostics tied to their file
    this.log('check.live.start', { seq, length: text.length, filePath });
    const r = await this.documentServices.request(filePath, 'document.analyse');
    if (!r || !Array.isArray(r.diagnostics)) { this.log('check.live.failed', { seq, reason: r?.reason }); return; }
    const m = this.editor.getModel();
    if (seq !== this._liveCheckSeq || this._formatBusy || !m || m !== model) { this.log('check.live.stale', { seq }); return; }
    if (m.getValue() !== text) { this.log('check.live.stale-edit', { seq }); return; } // stale
    r.diagnostics.forEach((d) => { d.file = filePath; d.col = d.column; });
    this.applyDiagnostics(r.diagnostics, true);
    this.log('check.live.applied', { seq, diagnostics: r.diagnostics.length });
  }

  // Outline panel — the current file's top-level declarations (SUB / FUNCTION / TYPE)
  // using the same document scan the completion provider uses. Clicking an item jumps to
  // its line. The panel is rebuilt on content change (debounced) and when switching tabs.
  rebuildOutline() {
    if (this._formatBusy) { this.log('outline.skipped-format', { busy: this._formatBusy }); return; }
    const el = document.getElementById('outline-list');
    if (!el) return;
    el.innerHTML = '';
    const model = this.editor.getModel();
    if (!model || !window.LangFeatures) return;
    const doc = window.LangFeatures.scanDoc(model);
    const items = [];
    for (const f of doc.functions) items.push({ type: 'ƒ', name: f.name, line: f.lineNo });
    for (const s of doc.subs) items.push({ type: 'S', name: s.name, line: s.lineNo });
    for (const t of doc.types) items.push({ type: 'T', name: t.name, line: t.lineNo });
    items.sort((a, b) => a.line - b.line);
    if (!items.length) {
      const empty = document.createElement('div');
      empty.className = 'recent-empty'; empty.textContent = I18N.t('outline.empty');
      el.appendChild(empty);
      return;
    }
    for (const it of items) {
      const row = document.createElement('div');
      row.className = 'outline-item';
      const icon = document.createElement('span'); icon.className = 'outline-icon';
      icon.textContent = it.type;
      const name = document.createElement('span');
      name.className = 'outline-name'; name.textContent = it.name;
      const line = document.createElement('span');
      line.className = 'outline-line'; line.textContent = I18N.t('outline.ln') + ' ' + it.line;
      row.appendChild(icon); row.appendChild(name); row.appendChild(line);
      row.addEventListener('click', () => {
        this.editor.setPosition({ lineNumber: it.line, column: 1 });
        this.editor.revealLineInCenter(it.line);
        this.editor.focus();
      });
      el.appendChild(row);
    }
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

  // The Run/Check target: the ACTIVE SAVED file. Axiom has no main-file convention —
  // the WPF IDE runs the active document too. An open project folder only groups files.
  runTarget() {
    return this.activeFile;
  }

  async check(explicit = false) {
    const target = this.activeFile;
    if (!target) return;
    const startedAt = performance.now();
    try {
      const r = await this.documentServices.request(target, 'document.compile');
      if (!r || target !== this.activeFile) return;
      r.diagnostics.forEach(d => { d.file = target; d.col = d.column; });
      if (r.status === 'BackendFailure' && !r.diagnostics.length) throw new Error('Компилятор не подготовил байт-код.');
      this.applyDiagnostics(r.diagnostics, true);
      if (explicit) {
        const elapsed = Math.round(performance.now() - startedAt);
        const failed = (r.diagnostics || []).some((d) => String(d.severity || '').toLowerCase() === 'error');
        document.getElementById('status-execution-time').textContent = `${I18N.t('status.compileTime')}: ${elapsed}ms`;
        if (failed) this.setStatus(I18N.t('status.error'));
      }
    } catch (e) {
      this.showIdeProblem('status.checkFailed', e && e.message ? e.message : String(e));
      const elapsed = Math.round(performance.now() - startedAt);
      if (explicit) {
        document.getElementById('status-execution-time').textContent = `${I18N.t('status.compileTime')}: ${elapsed}ms`;
        this.setStatus(I18N.t('status.error'));
      }
    }
    // An explicit check always surfaces the Problems panel — both to show errors and to
    // visibly confirm "Проблем нет" on success.
    if (explicit) this.switchPanel('diagnostics');
    if (explicit) this.focusEditor();
  }

  async run(launchKind = 'run', stopFirst = true) {
    if (this.running) return;
    let target = this.runTarget();
    if (!target) return;
    if (target.startsWith('untitled') || this.dirty.has(target)) {
      if (!confirm('Перед запуском нужно сохранить документ. Сохранить и запустить?')) return;
      await this.save(); target = this.runTarget();
      if (!target || target.startsWith('untitled') || this.dirty.has(target)) return;
    }
    this._runFile = target;
    // The integrated ConPTY console is the only run path: output, stderr and interactive
    // input all live in the terminal.
    document.getElementById('program-canvas')?.setAttribute('hidden', '');
    await this.execution.start(target, launchKind, launchKind === 'debug' ? this.trace.lines(this.openFiles.get(target).model) : [], stopFirst);
  }

  stop() {
    // Stop is silent when nothing is running — no misleading status.
    if (!this.running) return;
    this._stopRequested = true; // the pty exit event must not overwrite "Остановлено"
    this.execution.stop();
    this.setStatus(I18N.t('status.stopped'));
  }

  // ================= integrated console =================

  // xterm.js follows the IDE theme. Keeping a terminal dark in a dark workbench preserves
  // its familiar contrast; in the light theme, a light terminal avoids making the bottom
  // panel look like a separate embedded application.
  static CONSOLE_THEMES = {
    borland: {
      background: '#005555', foreground: '#FFFFFF', cursor: '#FFFFFF',
      selectionBackground: '#008080', black: '#000000', red: '#AA0000', green: '#00AA00',
      yellow: '#AAAA00', blue: '#0000AA', magenta: '#AA00AA', cyan: '#00AAAA', white: '#C0C0C0',
      brightBlack: '#555555', brightRed: '#FF5555', brightGreen: '#55FF55', brightYellow: '#FFFF55',
      brightBlue: '#5555FF', brightMagenta: '#FF55FF', brightCyan: '#55FFFF', brightWhite: '#FFFFFF',
    },
    basic: {
      background: '#000080', foreground: '#FFFFFF', cursor: '#FFFF55',
      selectionBackground: '#5050B0', black: '#000000', red: '#AA0000', green: '#00AA00',
      yellow: '#AAAA00', blue: '#0000AA', magenta: '#AA00AA', cyan: '#00AAAA', white: '#AAAAAA',
      brightBlack: '#555555', brightRed: '#FF5555', brightGreen: '#55FF55', brightYellow: '#FFFF55',
      brightBlue: '#5555FF', brightMagenta: '#FF55FF', brightCyan: '#55FFFF', brightWhite: '#FFFFFF',
    },
    dark: {
      // background matches the dark console surface in design-system.css (#151820):
      // the terminal is created lazily on the first run, and a different xterm
      // background visibly recolored the console at that moment.
      background: '#151820', foreground: '#d4d4d4', cursor: '#aeafad',
      black: '#000000', red: '#cd3131', green: '#0dbc79', yellow: '#e5e510',
      blue: '#2472c8', magenta: '#bc3fbc', cyan: '#11a8cd', white: '#e5e5e5',
      brightBlack: '#666666', brightRed: '#f14c4c', brightGreen: '#23d18b',
      brightYellow: '#f5f543', brightBlue: '#3b8eea', brightMagenta: '#d670d6',
      brightCyan: '#29b8db', brightWhite: '#e5e5e5',
    },
    light: {
      background: '#ffffff', foreground: '#263241', cursor: '#0969da',
      black: '#24292f', red: '#cf222e', green: '#1a7f37', yellow: '#9a6700',
      blue: '#0969da', magenta: '#8250df', cyan: '#0550ae', white: '#f6f8fa',
      brightBlack: '#57606a', brightRed: '#a40e26', brightGreen: '#116329',
      brightYellow: '#7d4e00', brightBlue: '#0550ae', brightMagenta: '#6639ba',
      brightCyan: '#0349a3', brightWhite: '#ffffff',
    },
  };

  static monacoSurface(theme) {
    const root = document.documentElement, previous = root.dataset.theme;
    root.dataset.theme = theme;
    const styles = getComputedStyle(root), read = name => styles.getPropertyValue(name).trim();
    const editor = read('--xs-editor');
    const colors = {
      'editor.background': editor, 'editor.foreground': read('--xs-text'),
      'editor.lineHighlightBackground': editor,
      'editor.selectionBackground': read('--xs-editor-selection'),
      'editorCursor.foreground': read('--xs-editor-cursor'),
      'editorWidget.background': read('--xs-menu-bg') || read('--xs-surface-raised'),
      'editorWidget.foreground': read('--xs-menu-text') || read('--xs-text'),
      'editorWidget.border': read('--xs-menu-border') || read('--xs-border'),
      'editorSuggestWidget.background': read('--xs-menu-bg') || read('--xs-surface-raised'),
      'editorSuggestWidget.foreground': read('--xs-menu-text') || read('--xs-text'),
      'editorSuggestWidget.border': read('--xs-menu-border') || read('--xs-border'),
      'editorSuggestWidget.selectedBackground': read('--xs-menu-hover-bg') || read('--xs-surface-active'),
      'editorSuggestWidget.selectedForeground': read('--xs-menu-hover-text') || read('--xs-text'),
    };
    if (read('--xs-menu-bg')) Object.assign(colors, {
      'editorHoverWidget.background': read('--xs-menu-bg'), 'editorHoverWidget.foreground': read('--xs-menu-text'),
      'editorHoverWidget.border': read('--xs-menu-border'),
      'quickInput.background': read('--xs-menu-bg'), 'quickInput.foreground': read('--xs-menu-text'),
      'list.focusBackground': read('--xs-menu-hover-bg'), 'list.focusForeground': read('--xs-menu-hover-text'),
      'list.activeSelectionBackground': read('--xs-menu-hover-bg'), 'list.activeSelectionForeground': read('--xs-menu-hover-text'),
    });
    if (previous === undefined) delete root.dataset.theme; else root.dataset.theme = previous;
    return colors;
  }

  consoleTheme() {
    const styles = getComputedStyle(document.documentElement), read = name => styles.getPropertyValue(name).trim();
    return { ...AxiomIDE.CONSOLE_THEMES[this.settings.theme],
      background: read('--xs-console-bg'), foreground: read('--xs-text'),
      cursor: read('--xs-editor-cursor'), selectionBackground: read('--xs-editor-selection'),
    };
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
        document.getElementById('status-execution-time').textContent = `${I18N.t('status.totalTime')}: ${Math.round(performance.now() - this._consoleT0)}ms`;
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
    try {
      const result = await this.documentServices.request(this.activeFile, 'document.editTransition', {
        previousLineNumber, currentLineNumber: position.lineNumber, caretOffset,
        formattingStyle: this.settings.formattingStyle || 'Classic', smartIdentifierEnabled });
      if (smartIdentifierEnabled !== !!this.settings.smartIdentifierEnabled || !result || this.editor.getModel() !== model ||
          this.editor.getContribution('snippetController2')?.isInSnippet() ||
          (this.running && this.openFiles.get(this._runFile)?.model === model) ||
          model.getOffsetAt(this.editor.getPosition()) !== caretOffset) return;
      this.editedLines.consume(model,previousLineNumber);
      if (!result.edits.length) return;
      const edits = result.edits.map(e => {
        if (!Number.isInteger(e.start) || !Number.isInteger(e.length) || e.start < 0 || e.length < 0 ||
            e.start + e.length > model.getValueLength()) throw new Error('Некорректный диапазон правки.');
        const start=model.getPositionAt(e.start),end=model.getPositionAt(e.start+e.length);
        return {range:new this.monaco.Range(start.lineNumber,start.column,end.lineNumber,end.column),text:e.newText};
      });
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
    } catch(e) {this.showIdeProblem('status.fmtFailed',e.message);}
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

  closeDiagnosticPeek() {
    // Карточка относится к предыдущему тексту; новый результат проверки обновит маркеры.
    const editorNode = this.editor.getDomNode();
    const widget = editorNode?.querySelector('.marker-widget');
    if (!widget || widget.getClientRects().length === 0) return;
    // Команда Monaco закрывает карточку и сама переводит фокус в редактор.
    // При INPUT или работе с другим полем сохраняем выбранный пользователем фокус.
    const focused = document.activeElement;
    const restoreFocus = focused && focused !== document.body && !editorNode.contains(focused);
    this.editor.trigger('axiom.diagnostics', 'closeMarkersNavigation', {});
    if (restoreFocus && focused.isConnected) focused.focus();
  }

  clearDiagnostics() {
    this.closeDiagnosticPeek();
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
    this._lastDiagnostics = this._lastDiagnostics.filter(d => d.code !== 'MB2047' || d.file !== this.activeFile);
    document.querySelectorAll('#diagnostics-content .diagnostic-item').forEach(item => {
      if (item.dataset.code === 'MB2047' && item.dataset.file === this.activeFile) item.remove();
    });
    this.updateDiagnosticFilters();
  }

  applyDiagnostics(diags, onlyActive = false) {
    this.closeDiagnosticPeek();
    this.log('diagnostics.apply.start', { count: Array.isArray(diags) ? diags.length : 0, onlyActive, focus: this.focusState() });
    const monaco = this.monaco;
    const byModel = new Map();
    if (onlyActive) {
      const active = this.activeFile ? this.openFiles.get(this.activeFile) : null;
      if (active) byModel.set(active.model, []);
    } else {
      for (const { model } of this.openFiles.values()) byModel.set(model, []);
    }

    const el = document.getElementById('diagnostics-content');
    el.innerHTML = '';
    this._lastDiagnostics = diags;
    this._lastDiagnosticsOnlyActive = onlyActive;

    if (!diags.length) {
      for (const model of byModel.keys()) monaco.editor.setModelMarkers(model, 'axiom', []);
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
      if (model && byModel.has(model)) {
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
    return window.DiagnosticLanguage.text(d, this.settings.language);
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
  }

  togglePanel() { this.setBottomPanelVisible(document.getElementById('bottom-panel').style.display === 'none'); }

  setBottomPanelVisible(visible) {
    this.bottomSplitter?.cancel();
    document.getElementById('bottom-panel').style.display = visible ? 'flex' : 'none';
    document.getElementById('panel-resizer').hidden = !visible;
    this.saveSettings({bottomPanelVisible: visible});
    this.bottomSplitter?.layout(); this.updateOutputMenuCheck();
  }

  showPanel() { this.setBottomPanelVisible(true); }

  // View menu affordance: "Нижняя панель" shows its ✓ exactly when the panel is visible.
  updateOutputMenuCheck() {
    const visible = document.getElementById('bottom-panel').style.display !== 'none';
    UIControls.setPressed(document.getElementById('tb-toggle-output'), visible);
    const check = document.querySelector('.menu-check[data-check="outputPanel"]');
    UIControls.setMenuChecked(check, visible);
  }

  // ================= theme / language =================

  updateThemeMenuChecks() {
    for (const theme of ['light', 'dark', 'basic', 'borland']) {
      UIControls.setMenuChecked(document.querySelector(`.menu-check[data-check="theme-${theme}"]`), this.settings.theme === theme);
    }
  }

  setTheme(theme) {
    if (!['dark', 'light', 'basic', 'borland'].includes(theme)) return;
    this.saveSettings({ theme });
    document.documentElement.setAttribute('data-theme', theme);
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
    if (this._lastDiagnostics) this.applyDiagnostics(this._lastDiagnostics, this._lastDiagnosticsOnlyActive);
    this.scheduleLiveCheck();
  }

  applyLocale() {
    I18N.apply();
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
    this.setProjectExplorerVisible(this.settings.projectExplorer !== false);
    this.renderMiscellaneousFiles();
  }

  setStatus(t) { document.getElementById('status-text').textContent = t; }

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
    const tabs = [];
    for (const [p, f] of this.openFiles) {
      if (excluded?.has(f)) continue;
      const discarded = this._discardedOnExit?.has(f);
      if (discarded && p.startsWith('untitled')) continue;
      tabs.push({ path: p, isActive: p === this.activeFile, content: discarded ? f.savedText : f.model.getValue(),
        savedText: f.savedText, isDirty: !discarded && (p.startsWith('untitled') ? f.model.getValue().length > 0 : this.openFiles.isDirty(p)),
        missing: !!f.missing, readonly: !!f.readonly });
    }
    const result = API.saveRecoverySync(tabs);
    if (!result.ok) { this._recoveryFailed = true; this.setStatus('Не удалось сохранить данные восстановления: ' + result.message); }
    else this._recoveryFailed = false;
    return result.ok;
  }

  saveOpenTabs(useSync = false) {
    this.persistRecovery();
    const tabs = [];
    for (const [p, f] of this.openFiles) {
      const discarded = this._discardedOnExit?.has(f);
      if (discarded && p.startsWith("untitled")) continue;
      const isDirty = !discarded && this.dirty.has(p);
      tabs.push({ path: p, isActive: p === this.activeFile, content: isDirty ? f.model.getValue() : undefined });
    }
    // On window teardown the async invoke loses the race with the renderer being
    // destroyed; the synchronous send guarantees the final openTabs snapshot reaches
    // the main process.
    const patch = { openTabs: tabs };
    if (useSync && API.saveSettingsSync) API.saveSettingsSync(patch);
    else this.saveSettings(patch);
  }

  async restoreTabs() {
    const recovery = await API.getRecovery();
    const tabs = recovery ?? this.settings.openTabs;
    if (Array.isArray(tabs) && tabs.length) {
      for (const tab of tabs) {
        if (tab.path && !tab.path.startsWith('untitled')) {
          try {
            const disk = await API.readFileContent(tab.path);
            const restored = typeof tab.content === 'string' && (recovery ? tab.isDirty : tab.content !== disk);
            const model = this.monaco.editor.createModel(restored ? tab.content : disk.replace(/^\uFEFF/, ''), 'axiom', this.monaco.Uri.file(tab.path));
            this.openFiles.set(tab.path, { model, viewState: null, readonly: !!tab.readonly });
            this.openFiles.get(tab.path).savedText = restored ? (tab.savedText ?? disk.replace(/^\uFEFF/, '').replace(/\r\n?/g, model.getEOL())) : model.getValue();
            this.openFiles.get(tab.path).missing = !!(restored && tab.missing);
            this.createTab(tab.path);
            if (restored) this.markDirty(tab.path, true);
            if (tab.isActive) this.switchToFile(tab.path);
          } catch {
            if (typeof tab.content !== 'string') continue;
            const model = this.monaco.editor.createModel(tab.content, 'axiom', this.monaco.Uri.file(tab.path));
            this.openFiles.set(tab.path, { model, viewState: null, readonly: !!tab.readonly });
            const entry = this.openFiles.get(tab.path); entry.savedText = tab.savedText ?? tab.content; entry.missing = true;
            this.createTab(tab.path); this.markDirty(tab.path, true);
            if (tab.isActive) this.switchToFile(tab.path);
          }
          API.watchFile(tab.path).catch(() => {});
        } else if (tab.path && typeof tab.content === 'string') {
          const model = this.monaco.editor.createModel(tab.content, 'axiom');
          this.openFiles.set(tab.path, { model, viewState: null });
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
