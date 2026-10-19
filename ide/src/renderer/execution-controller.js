class ExecutionController {
  constructor(ide, api) {
    this.inputSubmission = null; this.outputLineCount = 0; this.clearedOutputLineCount = 0;
    this.ide = ide; this.api = api; this.sessionId = null; this.stopId = null;
    this.inputRequestId = null; this.inputTarget = null; this.inputRevision = 0; this.inputText = ''; this.inputPrompt = ''; this.outputText = ''; this.streamedOutputText = ''; this.pendingEvents = [];
    this.disposers = [api.onBackendEvent(message => this.event(message)),
      api.onBackendFault(fault => this.fault(fault.message))];
  }
  async start(file, launchKind = 'run', breakpoints = [], stopFirst = true) {
    this.inputSubmission = null;
    const ide = this.ide;
    const request = this.startRequest = {};
    ide.running = true; this.starting = true; this.pendingStop = false; this.pendingEvents = []; this.clearedEmptyHistory = false; this.outputLineCount = 0; this.clearedOutputLineCount = 0; this.outputText = ''; this.streamedOutputText = ''; this.clearedOutputPrefix = ''; this.inputPrompt = ''; this.inputText = ''; this.inputRequestId = null; this.inputTarget = null; this.stopId = null;
    this.canStepBack=false;this.callDepth=0;this.line=null;this.debugBusy=false;this.debugRequest=null;this.stopRequest=null;this.debugError=null;ide.trace?.clearMarker();ide.trace?.update();
    ide._stopRequested = false; ide.clearDiagnostics(); ide.showPanel(); ide.switchPanel('console');
    ide.setStatus(I18N.t('trace.preparing'));
    if (!ide.ensureConsole()) { ide.running = false; this.starting = false; this.startRequest = null; ide.trace?.finished(); return; }
    ide.term.reset(); this.startedAt = performance.now();
    const document = ide.openFiles.get(file);
    const snapshot = this.sourceSnapshot = { documentId: document.documentId, sourceText: document.model.getValue(), file };
    let response;
    try {
      response = await this.api.startExecution({ documentId: document.documentId,
        documentRevision: document.revision, sourceText: snapshot.sourceText, sourceFilePath: file,
        launchKind, breakpointLines: breakpoints, stopBeforeFirstInstruction: launchKind === 'debug' && stopFirst });
    } catch (error) {
      if (this.startRequest === request) this.fault(error?.message || 'Не удалось начать выполнение.');
      return;
    }
    if (this.startRequest !== request) return;
    this.startRequest = null;
    this.starting = false;
    if (!response.ok) { this.fault(response.error?.message || 'Не удалось начать выполнение.'); return; }
    this.sessionId = response.payload.sessionId;
    for (const event of this.pendingEvents) this.event(event);
    this.pendingEvents = [];
    if (this.pendingStop && this.sessionId) await this.stop();
    if(this.stopId) ide.editor.focus(); else if (this.inputTarget !== 'screen') ide.term.focus();
  }
  event(message) {
    if (this.starting) { this.pendingEvents.push(message); return; }
    if (!this.sessionId || message.sessionId !== this.sessionId) return;
    const payload = message.payload;
    switch (message.event) {
      case 'execution.state':
        this.ide.setStatus(I18N.t(({Compiling:'trace.compiling',Running:'status.running',WaitingForInput:'trace.waiting',Paused:'trace.running'})[payload.state] || 'trace.running'));
        break;
      case 'output.append': case 'output.replace':
        if (message.event === 'output.replace') this.clearedOutputPrefix = '';
        // Снимок INPUT может опережать таймер вывода. append относится только
        // к опубликованному потоку, а не к уже полученному полному снимку.
        this.streamedOutputText = message.event === 'output.append' ? this.streamedOutputText + payload.text : payload.text;
        if (message.event === 'output.replace' || !this.outputText.startsWith(this.streamedOutputText)) this.outputText = this.streamedOutputText;
        this.renderOutput(); this.api.acknowledgeOutput(this.sessionId, payload.outputVersion); break;
      case 'input.request':
        this.inputSubmission = null;
        this.inputRevision++;
        this.inputRequestId = payload.inputRequestId; this.inputText = ''; this.inputPrompt = payload.prompt;
        this.inputTarget = payload.target || 'console';
        this.outputText = payload.outputText; this.outputLineCount = payload.outputLineCount ?? (payload.outputText ? payload.outputText.split(/\r?\n/).length : 0); this.renderOutput();
        if (this.inputTarget !== 'screen') this.ide.term.focus();
        break;
      case 'input.completed': {
        const active = payload.inputRequestId === this.inputRequestId;
        const submitted = this.inputSubmission?.id === payload.inputRequestId && this.inputSubmission.sessionId === this.sessionId;
        if (!active && !submitted) break;
        this.inputSubmission = null;
        this.inputRequestId = null; this.inputPrompt = ''; this.inputText = ''; this.inputTarget = null;
        // Отправленная строка уже показана в консоли; её заменит снимок службы.
        if (active) this.renderOutput();
        this.ide.setStatus(I18N.t('status.running'));
        break;
      }
      case 'debug.paused':
        this.debugRequest=null;this.debugBusy=false;this.debugError=null;
        this.callDepth=payload.callDepth;this.line=payload.line;this.ide.trace?.mark(payload);
        this.stopId = payload.stopId; this.canStepBack = payload.canStepBack;
        this.outputText = payload.outputText; this.outputLineCount = payload.outputLineCount ?? (payload.outputText ? payload.outputText.split(/\r?\n/).length : 0); this.renderOutput();
        this.ide.setStatus(I18N.t('trace.paused',payload.line ?? '—'));
        if (payload.line) { this.ide.editor.revealLineInCenter(payload.line); this.ide.editor.setPosition({ lineNumber: payload.line, column: payload.column || 1 }); }
        this.ide.editor.focus(); break;
      case 'graphics.surface':
        this.surface = payload;
        if (payload.kind === 'screen' && payload.state !== 'closed') this.ide.setStatus('Программа в окне SCREEN');
        break;
      case 'graphics.error': this.fault(payload.message); break;
      case 'execution.finished':
        this.inputSubmission = null;
        this.debugRequest=null;this.debugBusy=false;this.debugError=null;
        this.inputRequestId = null; this.inputTarget = null; this.inputPrompt = ''; this.inputText = '';
        this.outputText = payload.outputText; this.outputLineCount = payload.outputLineCount ?? (payload.outputText ? payload.outputText.split(/\r?\n/).length : 0); this.renderOutput();
        this.ide.running = false; this.inputRequestId = null; this.stopId = null; this.sessionId = null;
        this.ide.setStatus(I18N.t(({Succeeded:'status.finished',Cancelled:'status.stopped',CompilationFailed:'trace.compileFailed',RuntimeFailed:'trace.runtimeFailed'})[payload.status] || 'trace.failed'));
        if (payload.diagnostics?.length) {
          const snapshot = this.sourceSnapshot;
          const entry = snapshot && [...this.ide.openFiles.values()].find(entry => entry.documentId === snapshot.documentId);
          const file = entry?.path || snapshot?.file || this.ide._runFile;
          this.ide.applyDiagnostics(payload.diagnostics.map(d => ({ ...d, file, executionSnapshot: snapshot, col: d.column, severity: 'error' })), false, file);
          this.ide.switchPanel('diagnostics');
        }
        I18N.setText(document.getElementById('status-execution-time'),'trace.elapsed',Math.round(performance.now() - this.startedAt));
        this.ide.focusEditor(); break;
    }
    if(message.event==='execution.finished') this.ide.trace?.finished();
    else this.ide.trace?.update();
  }
  clearConsole() {
    this.clearedOutputPrefix = this.outputText || ''; this.clearedOutputLineCount = this.outputLineCount;
    this.clearedEmptyHistory = !this.outputText && this.outputLineCount > 0;
    this.renderOutput();
  }
  renderOutput() {
    if (!this.ide.term) return;
    // Снимок службы консоли разделяет завершённые строки, но не содержит
    // завершающего перевода строки. Следующий INPUT начинается новой строкой.
    const fullHistory = this.outputText || '';
    const prefix = this.clearedOutputPrefix || '';
    if (prefix && !fullHistory.startsWith(prefix)) this.clearedOutputPrefix = '';
    let history = prefix && fullHistory.startsWith(prefix)
      ? fullHistory.slice(prefix.length).replace(/^\r?\n/, '') : fullHistory;
    if (this.clearedEmptyHistory) history = history.replace(/^\r?\n/, '');
    const hasHistory = history.length > 0 || this.outputLineCount > (this.clearedOutputLineCount || 0);
    const separator = this.inputRequestId && hasHistory ? '\n' : '';
    const text = history + separator + (this.inputRequestId ? this.inputPrompt + this.inputText : '');
    // write() асинхронен: синхронный reset() не удаляет ожидающие записи.
    // RIS сбрасывает терминал внутри той же очереди перед новым снимком.
    this.ide.term.write('\x1bc' + text.replace(/\r?\n/g, '\r\n'));
  }
  async input(data) {
    if (data === '\u0003') { await this.stop(); return; }
    if (!this.inputRequestId) return;
    for (const character of data) {
      if (character === '\r' || character === '\n') {
        const id = this.inputRequestId; const text = this.inputText;
        const sessionId = this.sessionId, revision = this.inputRevision;
        const submission = this.inputSubmission = { sessionId, id, text, prompt: this.inputPrompt, target: this.inputTarget };
        this.inputRequestId = null; this.inputText = ''; this.inputPrompt = ''; this.inputTarget = null; this.ide.term.write('\r\n');
        this.ide.setStatus(I18N.t('trace.waiting'));
        let result;
        try { result = await this.api.submitInput(sessionId, id, text); }
        catch (error) { result = { ok: false, error: { message: error?.message || 'Не удалось отправить ввод.' } }; }
        if (this.inputSubmission !== submission || this.sessionId !== sessionId || this.inputRevision !== revision || (this.inputRequestId && this.inputRequestId !== id)) return;
        this.inputSubmission = null;
        if (!result.ok) {
          this.inputRequestId = id; this.inputText = text; this.inputPrompt = submission.prompt; this.inputTarget = submission.target;
          this.renderOutput();
          if (this.inputTarget !== 'screen') this.ide.term.focus();
          this.ide.setStatus(result.error?.message || 'Не удалось отправить ввод.');
        }
        break;
      } else if (character === '\x7f' || character === '\b') {
        if (this.inputText.length) {
          this.inputText = [...this.inputText].slice(0, -1).join('');
          // Один символ может занимать две ячейки, объединяться с предыдущим
          // или находиться за переносом строки. Восстанавливаем точный снимок.
          this.renderOutput();
        }
      } else if (!/[\u0000-\u001f]/.test(character)) { this.inputText += character; this.ide.term.write(character); }
    }
  }
  async stop() {
    if (this.starting) { this.pendingStop = true; return true; }
    if (!this.sessionId) return !this.ide.running;
    const sessionId = this.sessionId, snapshot = this.sourceSnapshot, request = {};
    this.stopRequest=request;this.debugError=null;
    let result;
    try { result = await this.api.stopExecution(sessionId); }
    catch (error) { result = { ok: false, error: { message: error?.message || 'Не удалось остановить выполнение.' } }; }
    if (this.stopRequest !== request) return false;
    this.stopRequest=null;
    if (this.sessionId !== sessionId) return !this.ide.running && this.sourceSnapshot===snapshot;
    if (!result.ok) {
      this.ide._stopRequested=false;
      this.debugError=result.error.message;
      this.ide.trace?.update();this.ide.setStatus(this.debugError);
      return false;
    }
    return true;
  }
  async debug(command) {
    if (!this.stopId || !this.sessionId) return;
    const stopId = this.stopId, line = this.line;
    const sessionId = this.sessionId, request = {};
    this.debugRequest = request;this.debugError=null;
    this.stopId = null;
    this.debugBusy=true;this.ide.trace?.clearMarker();this.ide.trace?.update();
    this.ide.setStatus(I18N.t('trace.running'));
    let result;
    try { result = await this.api.debugCommand(sessionId, command, stopId); }
    catch (error) { result = { ok: false, error: { message: error?.message || 'Не удалось выполнить команду отладки.' } }; }
    if (this.sessionId !== sessionId || this.debugRequest !== request) return;
    this.debugRequest=null;
    if (!result.ok) {
      this.stopId = stopId;this.debugError=result.error.message;
      this.ide.trace?.mark({line});
      this.ide.setStatus(this.debugError);
    }
    this.debugBusy=false;this.ide.trace?.update();
  }
  fault(message) {
    this.inputSubmission = null;
    this.debugRequest=null;this.stopRequest=null;this.debugBusy=false;this.debugError=null;
    this.startRequest = null; this.starting = false; this.pendingStop = false; this.pendingEvents = [];
    this.ide.running = false; this.sessionId = null; this.stopId = null; this.inputRequestId = null;
    this.ide.showIdeProblem('status.runFailed', message); this.ide.setStatus(I18N.t('trace.failed'));
    this.ide.trace?.finished();
  }
}
window.ExecutionController = ExecutionController;
