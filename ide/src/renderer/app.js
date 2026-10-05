const API = window.axiom;

const DEFAULT_TEMPLATE = [
  "' Новая программа на Axiom.",
  'PRINT "Hello, world"',
  '',
].join('\n');

class AxiomIDE {
  constructor() {
    this.monaco = null;
    this.langMeta = null; // language registry from `axiom metadata`; null until loaded
    this.editor = null;
    this.openFiles = new Map(); // path -> { model, viewState }
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
    // Autoformat trigger defaults — blur on, save off; each is a Правка-menu toggle.
    // The program always runs in the integrated ConPTY terminal — the console is the only run mode.
    this.settings = { theme: 'dark', language: 'ru', recentFiles: [], formatOnBlur: true, formatOnSave: false, checkOnChange: true, projectExplorer: true };
    this._autoFormatting = false; // true while an auto-format edit is applied (suppresses dirty-marking)
    // One coordination state for all background editor services. Requests are cheap to
    // abandon, but their results must never win after a newer edit or a focus transition.
    this._serviceSeq = 0;
    this._formatBusy = 0;
    this._liveCheckSeq = 0;
    this._outlineSeq = 0;
    this._lastEditorFocus = 0;
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
      menu: !!active?.closest?.('.monaco-menu, .context-view, .monaco-hover, .suggest-widget, .parameter-hints-widget, .peekview-widget'),
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
      document.documentElement.setAttribute('data-theme', this.settings.theme === 'light' ? 'light' : 'dark');
      I18N.setLocale(this.settings.language);
      await this.loadMonaco();
      await this.loadLanguageMetadata();
      this.setupLanguage();
      this.createEditor();
      this.setupEditorContextMenu();
      this.setupMenubar();
      this.setupToolbar();
      this.setupProjectExplorer();
      this.setupPanelTabs();
      this.setupPanelResizer();
      this.updateOutputMenuCheck();
      this.setupKeyboardShortcuts();
      this.setupConsoleEvents();
      await this.restoreTabs();
      await this.restoreProject();
      if (!this.openFiles.size) this.newFile();
      this.renderRecentFiles();
      this.applyLocale();
      API.onFileChanged((ev) => this.handleExternalChange(ev));
      document.getElementById('app')?.classList.add('ready');
      this.focusEditor();
      this.log('init.ready', { activeFile: this.activeFile });
    } catch (e) {
      this.log('init.failed', { message: e?.message || String(e) }, 'error');
      console.error('init failed', e);
      document.body.innerHTML = '<div style="padding:24px;font-family:sans-serif;color:#ccc">Failed to load Monaco. Run `npm install` and `npm run prepare` (see run-ide.bat).</div>';
    }
  }

  async loadSettings() {
    try {
      const s = await API.getSettings();
      if (s) Object.assign(this.settings, s);
    } catch {}
    // A missing or corrupt trigger setting falls back to its default.
    for (const [k, d] of [['formatOnBlur', true], ['formatOnSave', false], ['checkOnChange', true]]) {
      if (typeof this.settings[k] !== 'boolean') this.settings[k] = d;
    }
    this.updateFormatMenuChecks();
  }

  saveSettings(patch) {
    Object.assign(this.settings, patch);
    API.saveSettings(patch).catch(() => {});
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
    keywords: ['AND','APPEND','AS','BEGINFRAME','BYREF','BYVAL','CAMERA','CANVAS','CASE','CATCH','CIRCLE','CLOSE','CLS','COLOR','CONST','CONTINUE','DIM','DO','DRAW','DRAWCIRCLE','DRAWIMAGE','DRAWLINE','DRAWRECT','DRAWSPRITE','DRAWTILE','DRAWTILELAYER','DRAWTILEMAP','DRAWTEXT','ELSE','ELSEIF','END','ERROR','EQV','ERASE','EXIT','FALSE','TRUE','FILLCIRCLE','FILLRECT','FINALLY','FOR','FUNCTION','GET','IF','IMP','INPUT','IS','KEY','LINE','LOCATE','LOOP','MOD','MOUSE','NEXT','NOT','OFF','ON','OPEN','OR','OUTPUT','PAINT','PALETTE','PCOPY','PRESERVE','PRESENT','PRESET','PRINT','PSET','PUT','REDIM','RESETCAMERA','RETURN','SCREEN','SELECT','SELF','SETMAPTILE','SLEEP','STEP','STOP','SUB','THEN','TO','TRANSPARENT','TRY','TYPE','UNTIL','USING','VIEW','VIEWPORT','WHILE','WINDOW','XOR'],
    typeNames: ['BOOLEAN','DATE','DATETIME','DECIMAL','DOUBLE','FILE','IMAGE','INTEGER','LONG','SPRITE','STRING','TILEMAP','TILESET','TIME'],
    builtins: ['ABS','ASC','CHR','DATE','DATEADD','DATEDIFF','DAY','EOF','EXP','FILEEXISTS','FLOOR','INSTR','INT','JOIN','LCASE','LEFT','LEN','LOG','LTRIM','MID','MIN','MAX','MONTH','NOW','POW','READFILE','REPLACE','RGB','RIGHT','RND','ROUND','SGN','SIN','SQR','STR','SUBSTRCOUNT','TAN','TIME','TIMER','TRIM','TYPE','UCASE','VAL','WEEKDAY','WRITEFILE','YEAR'],
  };

  async loadLanguageMetadata() {
    this.langMeta = null;
    try {
      const r = await Promise.race([
        API.getLanguageMetadata(),
        new Promise((resolve) => setTimeout(() => resolve({ ok: false, reason: 'timeout' }), 3000)),
      ]);
      if (r && r.ok && r.metadata && typeof r.metadata === 'object') this.langMeta = r.metadata;
    } catch {
      this.langMeta = null;
    }
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
    // {expression} holes; &H hex literals; decimal numbers with type suffixes.
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
          [/\$"/, { token: 'string.quote', bracket: '@open', next: '@interpolatedString' }],
          [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],
          [/&H[0-9a-fA-F]+&?/, 'number.hex'],
          [/\d+\.\d*(#[DRFdrf]?|\d*)/, 'number'],
          [/\d+[%&#DRFSILdrfsil]?/, 'number'],
          [/<>|<=|>=|=|<|>/, 'operator'],
          [/\+|-|\*|\/|\\|\^|&/, 'operator'],
          [/[(),;:]/, '@brackets'],
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
          [/&H[0-9a-fA-F]+&?/, 'number.hex'],
          [/\d+\.\d*(#[DRFdrf]?)?/, 'number'],
          [/\d+%?[DL]?/, 'number'],
          [/<>|<=|>=|=|<|>/, 'operator'],
          [/\+|-|\*|\/|\\|\^|&/, 'operator'],
          [/[(),;:]/, '@brackets'],
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

    // No bracketed blocks in BASIC: only quotes auto-close.
    monaco.languages.setLanguageConfiguration('axiom', {
      comments: { lineComment: "'" },
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
      base: 'vs', inherit: true,
      rules: [
        { token: 'comment', foreground: '6a9955', fontStyle: 'italic' },
        { token: 'string', foreground: 'a31515' },
        { token: 'string.escape', foreground: 'ee0000' },
        { token: 'delimiter.interpolation', foreground: 'ee0000' },
        { token: 'number', foreground: '098658' },
        { token: 'number.hex', foreground: '098658' },
        { token: 'keyword', foreground: '0000ff' },
        { token: 'type', foreground: '267f99' },
        { token: 'support.function', foreground: '795e26' },
        { token: 'operator', foreground: '000000' },
      ],
      colors: {
        'editor.background': '#ffffff',
        'editor.foreground': '#182231',
        'editor.lineHighlightBackground': '#f3f7fc',
        'editor.selectionBackground': '#b9d7fb',
        'editorCursor.foreground': '#0969da',
        'editorLineNumber.foreground': '#8a98ab',
        'editorLineNumber.activeForeground': '#536174',
        'editorIndentGuide.background1': '#e6ebf2',
        'editorIndentGuide.activeBackground1': '#c4d1e1',
      },
    });

    monaco.editor.defineTheme('axiom-dark', {
      base: 'vs-dark', inherit: true,
      rules: [
        { token: 'comment', foreground: '6a9955', fontStyle: 'italic' },
        { token: 'string', foreground: 'ce9178' },
        { token: 'string.escape', foreground: 'd7ba7d' },
        { token: 'delimiter.interpolation', foreground: 'd7ba7d' },
        { token: 'number', foreground: 'b5cea8' },
        { token: 'number.hex', foreground: 'b5cea8' },
        { token: 'keyword', foreground: 'c586c0' },
        { token: 'type', foreground: '4ec9b0' },
        { token: 'support.function', foreground: 'dcdcaa' },
        { token: 'operator', foreground: 'd4d4d4' },
      ],
      colors: {
        'editor.background': '#1a1e27',
        'editor.foreground': '#dce3ec',
        'editor.lineHighlightBackground': '#202633',
        'editor.selectionBackground': '#2d568b',
        'editorCursor.foreground': '#62a8ff',
        'editorLineNumber.foreground': '#5f6c80',
        'editorLineNumber.activeForeground': '#a5afbf',
        'editorIndentGuide.background1': '#2a3240',
        'editorIndentGuide.activeBackground1': '#465064',
      },
    });

    // Completion + signature help + hover — builtins from the language registry
    // (ide.langMeta, loaded from `axiom metadata`), document locals from a light model scan.
    if (window.LangFeatures) LangFeatures.register(monaco, this);
  }

  createEditor() {
    this.editor = this.monaco.editor.create(document.getElementById('editor'), {
      value: '',
      language: 'axiom',
      theme: this.settings.theme === 'light' ? 'axiom-light' : 'axiom-dark',
      automaticLayout: true,
      fontSize: 14,
      fontFamily: "'Cascadia Code', Consolas, 'Courier New', monospace",
      minimap: { enabled: false },
      // Tooltip below the hovered line: with the default (above) the tooltip extends
      // above the editor's top edge when the code is at the top and gets hidden behind
      // the tab strip/toolbar.
      hover: { above: false },
      scrollBeyondLastLine: false,
      tabSize: 4,
      insertSpaces: true,
    });

    this.editor.onDidChangeCursorPosition((e) => {
      document.getElementById('status-line-col').textContent = `Ln ${e.position.lineNumber}, Col ${e.position.column}`;
      this.log('editor.cursor', { line: e.position.lineNumber, col: e.position.column, reason: e.reason });
    });
    this.editor.onDidFocusEditorText(() => {
      this._lastEditorFocus = performance.now();
      clearTimeout(this._blurTimer);
      this._blurTimer = null;
      this.log('editor.focus', this.focusState());
    });
    this.editor.onDidChangeModelContent(() => {
      // Auto-format edits are non-semantic normalization — they must not re-dirty a
      // just-saved buffer.
      if (this.activeFile && !this._autoFormatting) this.markDirty(this.activeFile, true);
      this.scheduleLiveCheck();
      this.scheduleOutlineRebuild();
      this.log('editor.change', { length: this.editor.getValue().length, activeFile: this.activeFile });
    });
    // Autoformat trigger: blur normalizes the whole buffer (no save required).
    // The manual Format command keeps its save-first semantics.
    this.editor.onDidBlurEditorText(() => {
      // Debounce: format only when the focus has really left the editor. Quick menu
      // round-trips (undo/copy/paste re-focus the editor) cancel the pending format,
      // so navigating menus never mutates the buffer mid-action.
      clearTimeout(this._blurTimer);
      this._blurTimer = setTimeout(() => {
        this._blurTimer = null;
        if (!this.settings.formatOnBlur) return;
        if (this.editor.hasTextFocus()) return; // refocused before the timer fired
        if (this.isTransientUiFocus()) {
          this.log('format.blur.deferred', this.focusState());
          return;
        }
        this.formatWholeDocument('ax-format-on-blur').catch(() => {});
      }, 300);
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
      { id: 'change-all', label: 'Change All Occurrences', key: 'Ctrl+F2', icon: 'codicon-edit' },
      { separator: true },
      { id: 'cut', label: 'Cut', icon: 'codicon-screen-cut' },
      { id: 'copy', label: 'Copy', icon: 'codicon-copy' },
      { id: 'paste', label: 'Paste', icon: 'codicon-files' },
      { separator: true },
      { id: 'command-palette', label: 'Command Palette', key: 'F1', icon: 'codicon-symbol-misc' },
    ];
    const close = () => { menu.hidden = true; menu.replaceChildren(); };
    const run = (id) => {
      close();
      // The menu button temporarily owns focus. Restore Monaco first.
      this.editor?.focus();
      if (id === 'change-all') this.editor?.trigger('ax-context', 'editor.action.changeAllOccurrences');
      else if (id === 'command-palette') this.editor?.trigger('ax-context', 'editor.action.quickCommand');
      else this.handleAction(id);
    };
    document.addEventListener('contextmenu', (event) => {
      if (!event.target.closest?.('.monaco-editor')) return;
      event.preventDefault();
      event.stopPropagation();
      menu.replaceChildren();
      items.forEach((item) => {
        if (item.separator) {
          const separator = document.createElement('div');
          separator.className = 'xs-context-separator';
          menu.appendChild(separator);
          return;
        }
        const row = document.createElement('button');
        row.type = 'button';
        row.className = 'xs-context-item';
        row.dataset.action = item.id;
        row.setAttribute('role', 'menuitem');
        row.innerHTML = `<span class="xs-context-item-icon codicon ${item.icon}"></span><span class="xs-context-item-label">${item.label}</span>${item.key ? `<span class="xs-context-item-key">${item.key}</span>` : ''}`;
        menu.appendChild(row);
      });
      menu.hidden = false;
      const margin = 4;
      const rect = menu.getBoundingClientRect();
      menu.style.left = `${Math.min(event.clientX, window.innerWidth - rect.width - margin)}px`;
      menu.style.top = `${Math.min(event.clientY, window.innerHeight - rect.height - margin)}px`;
    }, true);
    menu.addEventListener('click', (event) => {
      const item = event.target.closest?.('[data-action]');
      if (item) run(item.dataset.action);
    });
    document.addEventListener('mousedown', (event) => {
      if (!menu.hidden && !menu.contains(event.target)) close();
    }, true);
    document.addEventListener('keydown', (event) => {
      if (event.key === 'Escape' && !menu.hidden) { event.preventDefault(); close(); }
    });
  }

  // ================= UI binding =================

  setupMenubar() {
    let openMenu = null;
    let lastOpenMenu = null; // remembers the menu that was open a moment ago (hover-switch)

    const closeAll = () => {
      document.querySelectorAll('.menu-item.open').forEach((el) => el.classList.remove('open'));
      openMenu = null;
    };

    const openMenuFor = (menuItem) => {
      closeAll();
      menuItem.classList.add('open');
      openMenu = menuItem;
      lastOpenMenu = menuItem;
    };

    document.querySelectorAll('.menu-item').forEach((menuItem) => {
      const label = menuItem.querySelector('.menu-label');
      if (!label) return;

      label.addEventListener('mousedown', (e) => {
        e.preventDefault();
        if (menuItem.classList.contains('open')) closeAll();
        else openMenuFor(menuItem);
      });

      label.addEventListener('mouseenter', () => {
        if (openMenu === menuItem) return;
        if (openMenu) { openMenuFor(menuItem); return; }
        // When the pointer moves from an open menu to a sibling label, the old menu's
        // mouseleave fires first and closes everything; without this branch the hovered
        // menu would never open on hover.
        if (lastOpenMenu && lastOpenMenu !== menuItem) openMenuFor(menuItem);
      });

      // Close when the pointer leaves the whole menu (label + dropdown), so a
      // lingering open dropdown can't swallow clicks on the tab bar below it.
      menuItem.addEventListener('mouseleave', () => {
        if (openMenu === menuItem) {
          closeAll();
          setTimeout(() => { if (!openMenu) lastOpenMenu = null; }, 0);
        }
      });
    });

    document.querySelectorAll('.menu-dropdown-item').forEach((item) => {
      item.addEventListener('click', () => {
        const action = item.dataset.action;
        closeAll();
        lastOpenMenu = null; // a real action closes menus for good
        if (action) this.handleAction(action);
      });
    });

    document.addEventListener('mousedown', (e) => {
      if (openMenu && !openMenu.contains(e.target)) { closeAll(); lastOpenMenu = null; }
    });

    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') { closeAll(); lastOpenMenu = null; } });
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
  }

  setupPanelTabs() {
    document.querySelectorAll('.panel-tab').forEach((tab) => {
      tab.addEventListener('click', (e) => {
        const name = (e.target).dataset.tab;
        if (name) this.switchPanel(name);
      });
    });
  }

  // ================= panel resizer =================

  // Splitter по образцу GridSplitter из WPF Axiom IDE: во время drag двигается тонкая
  // линия-предпросмотр, высота панели применяется ОДИН раз при отпускании.
  setupPanelResizer() {
    const resizer = document.getElementById('panel-resizer');
    const panel = document.getElementById('bottom-panel');
    const main = document.getElementById('main-content');
    const preview = document.getElementById('panel-resize-preview');
    const MIN_HEIGHT = 64;      // tabs + a couple of terminal rows
    // Guarantees #editor-container keeps at least this much space. The same invariant is
    // ALSO enforced in CSS by #bottom-panel { max-height: calc(100% - 149px) }, which
    // re-clamps the panel when the window is resized after a drag.
    const EDITOR_MIN = 140;
    const RESIZER_H = 9;        // height of #panel-resizer in styles.css

    const clamp = (h) => Math.max(MIN_HEIGHT, Math.min(h, main.clientHeight - EDITOR_MIN - RESIZER_H));

    // Manual double-tap detection: preventDefault() on pointerdown (needed to keep the
    // drag from selecting text) suppresses the compatibility mouse sequence, so a
    // 'dblclick' listener on the strip never fires. Two taps within 500ms and ~6px of
    // each other = reset.
    let lastTapAt = 0;
    let lastTapX = 0;

    resizer.addEventListener('pointerdown', (e) => {
      if (e.button !== 0) return;
      e.preventDefault();

      const now = performance.now();
      if (now - lastTapAt < 500 && Math.abs(e.clientX - lastTapX) < 6) {
        lastTapAt = 0;
        panel.style.height = ''; // back to the CSS default
        this.saveSettings({ consoleHeight: null });
        this.focusEditor();
        return; // reset consumed the tap — don't start a drag
      }
      lastTapAt = now;
      lastTapX = e.clientX;

      document.body.classList.add('panel-resizing');
      try { resizer.setPointerCapture(e.pointerId); } catch {}

      const showPreview = (h) => {
        preview.style.display = 'block';
        preview.style.top = Math.round(main.getBoundingClientRect().bottom - h) + 'px';
      };
      // live rect, not the pointerdown cache: a window resize mid-drag must not skew the
      // committed height; the preview line then sticks to the cursor
      const heightAt = (clientY) => clamp(main.getBoundingClientRect().bottom - clientY);
      const onMove = (ev) => showPreview(heightAt(ev.clientY));
      const onUp = (ev) => {
        resizer.removeEventListener('pointermove', onMove);
        resizer.removeEventListener('pointerup', onUp);
        resizer.removeEventListener('pointercancel', onUp);
        document.body.classList.remove('panel-resizing');
        preview.style.display = 'none';
        const h = heightAt(ev.clientY);
        panel.style.height = Math.round(h) + 'px';
        this.saveSettings({ consoleHeight: h });
        this.focusEditor();
      };
      resizer.addEventListener('pointermove', onMove);
      resizer.addEventListener('pointerup', onUp);
      resizer.addEventListener('pointercancel', onUp);
      showPreview(clamp(panel.getBoundingClientRect().height));
    });

    const saved = this.settings.consoleHeight;
    if (typeof saved === 'number' && isFinite(saved) && saved > 0) {
      panel.style.height = Math.round(clamp(saved)) + 'px';
    }
  }

  setupKeyboardShortcuts() {
    // Capture phase: the integrated terminal's keydown handling stops the event before
    // bubble-phase listeners once xterm has focus, so F5 / Ctrl chords would never reach
    // the IDE while the console is focused.
    window.addEventListener('keydown', (e) => {
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
      if (e.key === 'F5') { consume(); e.shiftKey ? this.handleAction('stop') : this.handleAction('run'); return; }
    }, true);

    window.addEventListener('beforeunload', () => this.saveOpenTabs(true));

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
    const editor = this.editor;
    switch (action) {
      case 'new': this.newFile(); break;
      case 'new-project-file': this.newProjectFile(); break;
      case 'open': this.openDialog(); break;
      case 'open-project': this.openProjectDialog(); break;
      case 'close-project': this.closeProject(); break;
      case 'save': this.save(); break;
      case 'save-as': this.saveAs(); break;
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
      case 'toggle-format-on-blur': this.toggleSetting('formatOnBlur'); break;
      case 'toggle-format-on-save': this.toggleSetting('formatOnSave'); break;
      case 'toggle-check-on-change': this.toggleSetting('checkOnChange'); break;
      case 'run': this.run(); break;
      case 'check': this.check(true); break;
      case 'stop': this.stop(); break;
      case 'toggle-output': this.togglePanel(); break;
      case 'toggle-project-explorer': this.toggleProjectExplorer(); break;
      case 'theme-light': this.setTheme('light'); break;
      case 'theme-dark': this.setTheme('dark'); break;
      case 'lang-ru': this.setLanguage('ru'); break;
      case 'lang-en': this.setLanguage('en'); break;
      case 'open-log-folder': this.openLogFolder(); break;
      case 'about': this.showAbout(); break;
    }
  }

  // ================= files / tabs =================

  newFile() {
    const model = this.monaco.editor.createModel(DEFAULT_TEMPLATE, 'axiom');
    let p;
    do { this.untitled++; p = `untitled${this.untitled}.axm`; } while (this.openFiles.has(p));
    this.openFiles.set(p, { model, viewState: null });
    this.dirty.add(p);
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
    const defaultPath = this.projectDir + '\\program.axm';
    const selectedPath = await API.saveFileDialog(defaultPath);
    if (!selectedPath) return;
    const written = await API.writeFile(selectedPath, "' Новая программа на Axiom.\n");
    if (!written) { alert(I18N.t('dialog.openFailed', selectedPath)); return; }
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

  closeProject() {
    if (!this.projectDir) return;
    // Take the membership snapshot before clearing projectDir: after closure the remaining
    // documents are no longer classified as project files. Confirm every dirty project
    // document first, so cancelling one confirmation leaves the project and all its tabs intact.
    const projectFiles = [...this.openFiles.keys()].filter((file) => this.isProjectFile(file));
    for (const file of projectFiles) {
      if (!this.dirty.has(file)) continue;
      const name = file.split(/[/\\]/).pop();
      if (!confirm(I18N.t('dialog.unsaved', name))) return;
    }
    this.projectDir = null;
    this.saveSettings({ lastProjectDir: null });
    const title = document.getElementById('project-title');
    title.textContent = I18N.t('project.open');
    title.classList.add('project-open');
    document.getElementById('project-files').innerHTML = '';
    // A project owns its open documents. Closing it releases their editor models and tabs,
    // while files opened from elsewhere deliberately remain available as independent files.
    for (const file of projectFiles) {
      this.dirty.delete(file); // confirmation above authorizes discarding unsaved project edits
      this.closeFile(file);
    }
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
    if (check) check.classList.toggle('on', visible);
    setTimeout(() => this.editor?.layout(), 0);
  }

  async openFile(p) {
    if (this.openFiles.has(p)) {
      this.switchToFile(p);
      // Re-opening an already-open file still bumps recency.
      if (!p.startsWith('untitled')) {
        API.addRecentFile(p).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
      }
      return;
    }
    let content;
    try { content = await API.readFileContent(p); }
    catch { alert(I18N.t('dialog.openFailed', p)); return; }
    const model = this.monaco.editor.createModel(content, 'axiom', this.monaco.Uri.file(p));
    this.openFiles.set(p, { model, viewState: null });
    this.createTab(p);
    this.switchToFile(p);
    // A freshly opened document is diagnosed right away (same debounced live check as
    // typing) — its squiggles must not wait for the first keystroke.
    this.scheduleLiveCheck();
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
    tab.innerHTML = `<span class="tab-name">${name}</span><span class="dirty-indicator" style="display:none">*</span><button class="tab-close" aria-label="Close tab"><span class="codicon codicon-close"></span></button>`;
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

  switchToFile(p) {
    const f = this.openFiles.get(p);
    if (!f) return;
    if (this.activeFile) {
      const cur = this.openFiles.get(this.activeFile);
      if (cur) cur.viewState = this.editor.saveViewState();
    }
    this.editor.setModel(f.model);
    if (f.viewState) this.editor.restoreViewState(f.viewState);
    this.activeFile = p;
    document.getElementById('status-file-path').textContent = p;
    document.querySelectorAll('.editor-tab').forEach((t) => t.classList.toggle('active', t.dataset.path === p));
    document.querySelectorAll('.project-file').forEach((t) => t.classList.toggle('active', t.dataset.path === p || t.title === p));
    this.renderMiscellaneousFiles();
    this.updateSaveButton();
    this.editor.focus();
    this.rebuildOutline();
  }

  closeFile(p) {
    const f = this.openFiles.get(p);
    if (!f) return;
    if (this.dirty.has(p)) {
      const name = p.split(/[/\\]/).pop();
      if (!confirm(I18N.t('dialog.unsaved', name))) return;
    }
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
    document.querySelectorAll('.editor-tab').forEach((t) => { if (t.dataset.path === p) t.remove(); });
    if (this.activeFile === p) {
      const rest = [...this.openFiles.keys()];
      if (rest.length) this.switchToFile(rest[rest.length - 1]);
      else { this.activeFile = null; this.editor.setModel(null); document.getElementById('status-file-path').textContent = ''; }
    }
    if (this.projectDir) this.refreshProjectFiles();
    this.renderMiscellaneousFiles();
  }

  async refreshProjectFiles() {
    if (!this.projectDir) return;
    const files = await API.listProjectFiles(this.projectDir);
    this.renderProjectFiles(files);
  }

  markDirty(p, d) {
    if (d) this.dirty.add(p); else this.dirty.delete(p);
    const tab = [...document.querySelectorAll('.editor-tab')].find((t) => t.dataset.path === p);
    if (tab) tab.querySelector('.dirty-indicator').style.display = d ? 'inline' : 'none';
  }

  async save() {
    if (!this.activeFile) return;
    if (this.settings.formatOnSave) await this.formatWholeDocument('ax-format-on-save');
    const content = this.editor.getValue();
    if (this.activeFile.startsWith('untitled')) {
      const p = await API.saveFileDialog(this.activeFile);
      if (!p) return;
      if (this.openFiles.has(p)) {
        alert(I18N.t('dialog.alreadyOpen', p.split(/[/\\]/).pop()));
        this.focusEditor();
        return;
      }
      const ok = await API.writeFile(p, content);
      if (!ok) { this.saveFailed(p); return; } // no false success
      this.renameActive(p);
    } else {
      const ok = await API.writeFile(this.activeFile, content);
      if (!ok) { this.saveFailed(this.activeFile); return; }
      this.markDirty(this.activeFile, false);
    }
    this.setStatus(I18N.t('status.saved'));
    this.focusEditor();
  }

  saveFailed(p) {
    // A failed disk write must be visible, keep the tab dirty and never rename it to
    // the unwritable path.
    this.setStatus(I18N.t('status.saveFailed'));
    alert(I18N.t('dialog.saveFailed', p));
    this.focusEditor();
  }

  async saveAs() {
    if (!this.activeFile) return;
    const p = await API.saveFileDialog(this.activeFile.startsWith('untitled') ? undefined : this.activeFile);
    if (!p) return;
    if (this.openFiles.has(p) && p !== this.activeFile) {
      // Overwriting another open tab's path would corrupt its buffer/tab map.
      alert(I18N.t('dialog.alreadyOpen', p.split(/[/\\]/).pop()));
      this.focusEditor();
      return;
    }
    if (this.settings.formatOnSave) await this.formatWholeDocument('ax-format-on-save');
    const ok = await API.writeFile(p, this.editor.getValue());
    if (!ok) { this.saveFailed(p); return; }
    this.renameActive(p);
    this.setStatus(I18N.t('status.saved'));
    this.focusEditor();
  }

  renameActive(newPath) {
    const old = this.activeFile;
    const f = this.openFiles.get(old);
    if (!f) return;
    this.openFiles.delete(old);
    this.openFiles.set(newPath, { model: f.model, viewState: null });
    this.dirty.delete(old);
    this.dirty.delete(newPath);
    this.activeFile = newPath;
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
    this.markDirty(newPath, false);
    document.getElementById('status-file-path').textContent = newPath;
    document.querySelectorAll('.editor-tab').forEach((t) => t.classList.toggle('active', t.dataset.path === newPath));
    this.renderMiscellaneousFiles();
    this.updateSaveButton();
    API.addRecentFile(newPath).then((recent) => { this.settings.recentFiles = recent; this.renderRecentFiles(); }).catch(() => {});
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

  async formatWholeDocument(owner) {
    const model = this.editor.getModel();
    if (!model) return;
    if (this._formatBusy) {
      this.log('format.document.skipped-busy', { owner, busy: this._formatBusy });
      return;
    }
    const sel = this.editor.getSelection();
    const text = model.getValue();
    const request = ++this._serviceSeq;
    clearTimeout(this._liveCheckTimer);
    this._liveCheckTimer = null;
    this._formatBusy++;
    this.log('format.document.start', { owner, length: text.length, request });
    try {
      const r = await API.formatBuffer(text);
      if (!r || !r.ok || typeof r.text !== 'string') { this.log('format.document.failed', { owner, request, reason: r?.reason }); return; }
      const m = this.editor.getModel();
      if (request !== this._serviceSeq || !m || m !== model) { this.log('format.document.stale', { owner, request }); return; }
      if (m.getValue() !== text) { this.log('format.document.stale-edit', { owner, request }); return; } // the buffer changed while the request was in flight — stale, skip
      const edits = this.lineDiffEdits(m, r.text);
      if (!edits.length) { this.log('format.document.noop', { owner, request }); return; }
      this.editor.pushUndoStop();
      this._autoFormatting = true;
      try {
        this.editor.executeEdits(owner, edits);
      } finally {
        this._autoFormatting = false;
      }
      this.editor.pushUndoStop(); // the whole autoformat is a single undo step
      if (sel) this.editor.setSelection(sel);
      this.log('format.document.applied', { owner, request, edits: edits.length });
    } catch (e) {
      this.log('format.document.exception', { owner, request, message: e?.message || String(e) }, 'error');
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

  toggleSetting(key) {
    this.saveSettings({ [key]: !this.settings[key] });
    if (key === 'checkOnChange' && !this.settings.checkOnChange) {
      clearTimeout(this._liveCheckTimer);
      this._liveCheckTimer = null;
      this.log('check.live.disabled');
    }
    this.updateFormatMenuChecks();
    const labels = {
      formatOnBlur: 'menu.edit.fmtBlur',
      formatOnSave: 'menu.edit.fmtSave',
      checkOnChange: 'menu.edit.checkLive',
    };
    const labelKey = labels[key] || key;
    this.setStatus(I18N.t(labelKey) + ': ' + I18N.t(this.settings[key] ? 'fmt.on' : 'fmt.off'));
    this.focusEditor();
  }

  updateFormatMenuChecks() {
    for (const key of ['formatOnBlur', 'formatOnSave', 'checkOnChange']) {
      const el = document.querySelector(`.menu-check[data-check="${key}"]`);
      if (el) el.classList.toggle('on', !!this.settings[key]);
    }
  }

  // ================= live diagnostics + outline =================

  // The buffer is checked without saving (`axiom check --stdin --json`, debounced).
  // Its file path accompanies the buffer so the main process can run the CLI from the
  // active file's directory.
  scheduleLiveCheck() {
    this._liveCheckSeq++;
    if (!this.settings.checkOnChange) return;
    clearTimeout(this._liveCheckTimer);
    const seq = this._liveCheckSeq;
    this._liveCheckTimer = setTimeout(() => this.runLiveCheck(seq).catch((e) => this.log('check.live.exception', { message: e?.message || String(e) }, 'error')), 600);
    this.log('check.live.scheduled', { seq });
  }

  async runLiveCheck(seq = this._liveCheckSeq) {
    const model = this.editor.getModel();
    if (!model || !this.settings.checkOnChange || seq !== this._liveCheckSeq) return;
    if (this._formatBusy) { this.log('check.live.skipped-format', { seq, busy: this._formatBusy }); return; }
    const text = model.getValue();
    const filePath = this.activeFile; // keep diagnostics tied to their file
    this.log('check.live.start', { seq, length: text.length, filePath });
    const r = await API.checkBuffer(text, filePath);
    if (!r || !Array.isArray(r.diagnostics)) { this.log('check.live.failed', { seq, reason: r?.reason }); return; }
    const m = this.editor.getModel();
    if (seq !== this._liveCheckSeq || this._formatBusy || !m || m !== model) { this.log('check.live.stale', { seq }); return; }
    if (m.getValue() !== text) { this.log('check.live.stale-edit', { seq }); return; } // stale
    r.diagnostics.forEach((d) => { d.file = filePath; });
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
    if (!this.activeFile || this.activeFile.startsWith('untitled')) return;
    // Classic save semantics: the file on disk changes only on explicit save actions.
    // Check/Run/Format analyze the saved file, so a dirty document must be saved first.
    const target = this.runTarget();
    if (!target || target.startsWith('untitled')) return;
    if (this.dirty.has(target)) {
      alert(I18N.t('dialog.saveBeforeCheck'));
      this.focusEditor();
      return;
    }
    const startedAt = performance.now();
    try {
      const r = await API.checkFile(target);
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

  async run() {
    if (this.running) return;
    const target = this.runTarget();
    if (!target || target.startsWith('untitled')) { alert(I18N.t('dialog.saveBeforeRun')); return; }
    if (this.dirty.has(target)) { alert(I18N.t('dialog.saveBeforeRun')); return; }
    this._runFile = target;
    // The integrated ConPTY console is the only run path: output, stderr and interactive
    // input all live in the terminal.
    await this.runInConsole();
  }

  stop() {
    // Stop is silent when nothing is running — no misleading status.
    if (!this.running) return;
    this._stopRequested = true; // the pty exit event must not overwrite "Остановлено"
    API.stopRun(); // kills the ConPTY session running the program
    this.setStatus(I18N.t('status.stopped'));
  }

  // ================= integrated console =================

  // xterm.js follows the IDE theme. Keeping a terminal dark in a dark workbench preserves
  // its familiar contrast; in the light theme, a light terminal avoids making the bottom
  // panel look like a separate embedded application.
  static CONSOLE_THEMES = {
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
      theme: AxiomIDE.CONSOLE_THEMES[this.settings.theme === 'light' ? 'light' : 'dark'],
    });
    this.fitAddon = new window.FitAddon.FitAddon();
    this.term.loadAddon(this.fitAddon);
    this.term.open(document.getElementById('terminal'));
    this.log('terminal.created');
    this.fitConsole();
    this.term.onData((d) => this.consoleInput(d)); // keystrokes go through the type-ahead hold
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
    if (clean) { API.ptyWrite(clean); return; }
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
    if (!this.activeFile || this.activeFile.startsWith('untitled')) return;
    if (this.dirty.has(this.activeFile)) {
      alert(I18N.t('dialog.saveBeforeFormat'));
      this.focusEditor();
      return;
    }
    try {
      const r = await API.formatFile(this.activeFile);
      if (r.ok && r.formatted != null) {
        const f = this.openFiles.get(this.activeFile);
        if (f) {
          const pos = this.editor.getPosition();
          f.model.setValue(r.formatted);
          this.markDirty(this.activeFile, true);
          this.editor.setPosition(pos);
          // The formatted text lives only in the editor buffer (the user must still save).
          this.setStatus(I18N.t('status.formatted'));
        }
      } else if (r.diagnostics && r.diagnostics.length) {
        this.applyDiagnostics(r.diagnostics, true);
        this.switchPanel('diagnostics');
      }
    } catch (e) {
      this.showIdeProblem('status.fmtFailed', e && e.message ? e.message : String(e));
      this.setStatus(I18N.t('status.error'));
    }
    this.focusEditor();
  }

  // ================= external file changes (watcher) =================

  handleExternalChange(ev) {
    const p = ev && ev.path;
    if (!p || !this.openFiles.has(p) || p.startsWith('untitled')) return;
    if (this.fileChangePrompts.has(p)) return;
    this.fileChangePrompts.add(p);
    const name = p.split(/[/\\]/).pop();
    const message = ev.exists ? I18N.t('dialog.fileChangedReload', name) : I18N.t('dialog.fileDeleted', name);
    const reload = confirm(message);
    this.fileChangePrompts.delete(p);
    if (!reload) return; // keep the in-editor version; the next external change re-prompts
    API.readFileContent(p)
      .then((content) => {
        const f = this.openFiles.get(p);
        if (!f) return;
        f.model.setValue(content);
        this.markDirty(p, false);
        this.setStatus(I18N.t('status.reloaded'));
        if (this.activeFile === p) this.editor.focus();
      })
      .catch(() => {
        alert(I18N.t('dialog.openFailed', p));
      });
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
  }

  applyDiagnostics(diags, onlyActive = false) {
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
      const empty = document.createElement('div');
      empty.className = 'recent-empty';
      empty.textContent = I18N.t('problems.empty');
      el.appendChild(empty);
      for (const model of byModel.keys()) monaco.editor.setModelMarkers(model, 'axiom', []);
      this.log('diagnostics.apply.end', { count: 0, focus: this.focusState() });
      return;
    }

    for (const d of diags) {
      const sev = d.severity === 'error' ? monaco.MarkerSeverity.Error : monaco.MarkerSeverity.Warning;
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
        });
      }

      const item = document.createElement('div');
      item.className = 'diagnostic-item';
      const icon = document.createElement('span');
      icon.className = 'diagnostic-icon ' + d.severity;
      icon.textContent = d.severity === 'error' ? '\u2716' : '\u26A0\uFE0E';
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

  // Диагностики Axiom изначально на русском — слой перевода из X#-IDE не нужен.
  localizedDiagText(d) {
    return `${d.code}: ${d.message}`;
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

  togglePanel() {
    const panel = document.getElementById('bottom-panel');
    panel.style.display = panel.style.display === 'none' ? 'flex' : 'none';
    this.updateOutputMenuCheck();
  }

  showPanel() {
    document.getElementById('bottom-panel').style.display = 'flex';
    this.updateOutputMenuCheck();
  }

  // View menu affordance: "Нижняя панель" shows its ✓ exactly when the panel is visible.
  updateOutputMenuCheck() {
    const check = document.querySelector('.menu-check[data-check="outputPanel"]');
    if (check) check.classList.toggle('on',
      document.getElementById('bottom-panel').style.display !== 'none');
  }

  // ================= theme / language =================

  setTheme(theme) {
    this.saveSettings({ theme });
    document.documentElement.setAttribute('data-theme', theme);
    this.monaco?.editor.setTheme(theme === 'dark' ? 'axiom-dark' : 'axiom-light');
    if (this.term) this.term.options.theme = AxiomIDE.CONSOLE_THEMES[theme === 'light' ? 'light' : 'dark'];
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
    this.setProjectExplorerVisible(this.settings.projectExplorer !== false);
    this.renderMiscellaneousFiles();
  }

  setStatus(t) { document.getElementById('status-text').textContent = t; }

  focusEditor() {
    this.editor?.focus();
  }

  // ================= recent files / about =================

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
  }

  async showAbout() {
    let info = {};
    try { info = await API.getVersion(); } catch { /* show what we know */ }
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.innerHTML = `
      <div class="modal">
        <div class="about-head">
          <img class="brand-logo-lg logo-dark" src="assets/logo-dark.png" alt="Axiom" draggable="false">
          <img class="brand-logo-lg logo-light" src="assets/logo-light.png" alt="Axiom" draggable="false">
          <h3>${I18N.t('about.title')}</h3>
        </div>
        <div class="about-row"><span>${I18N.t('about.ide')}</span><code>${info.ide || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.cli')}</span><code>${info.axiom || I18N.t('about.notFound')}</code></div>
        <div class="about-row"><span>${I18N.t('about.electron')}</span><code>${info.electron}</code></div>
        <div class="about-row"><span>${I18N.t('about.node')}</span><code>${info.node}</code></div>
        <button id="about-close">${I18N.t('about.ok')}</button>
      </div>`;
    document.body.appendChild(overlay);
    overlay.querySelector('#about-close').onclick = () => overlay.remove();
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

  saveOpenTabs(useSync = false) {
    const tabs = [];
    for (const [p, f] of this.openFiles) {
      const isDirty = this.dirty.has(p);
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
    const tabs = this.settings.openTabs;
    if (Array.isArray(tabs) && tabs.length) {
      for (const tab of tabs) {
        if (tab.path && !tab.path.startsWith('untitled')) {
          try {
            const disk = await API.readFileContent(tab.path);
            const content = typeof tab.content === 'string' && tab.content !== disk ? tab.content : disk;
            const model = this.monaco.editor.createModel(content, 'axiom', this.monaco.Uri.file(tab.path));
            this.openFiles.set(tab.path, { model, viewState: null });
            this.createTab(tab.path);
            if (typeof tab.content === 'string' && tab.content !== disk) this.markDirty(tab.path, true);
            if (tab.isActive) this.switchToFile(tab.path);
          } catch { /* file gone */ }
        } else if (tab.path && typeof tab.content === 'string') {
          const model = this.monaco.editor.createModel(tab.content, 'axiom');
          this.openFiles.set(tab.path, { model, viewState: null });
          this.createTab(tab.path);
          this.markDirty(tab.path, true);
          if (tab.isActive) this.switchToFile(tab.path);
          const m = tab.path.match(/^untitled(\d+)\.axm$/);
          if (m) this.untitled = Math.max(this.untitled, parseInt(m[1], 10));
        }
      }
      if (!this.activeFile && this.openFiles.size) this.switchToFile([...this.openFiles.keys()][0]);
      // The restored buffer must be re-checked so the diagnostics (markers + Problems)
      // reappear right away.
      if (this.activeFile) this.scheduleLiveCheck();
      if (this.openFiles.size) return;
    }
  }
}

window.__ide = new AxiomIDE();
