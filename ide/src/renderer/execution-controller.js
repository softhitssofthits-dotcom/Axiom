class ExecutionController {
  constructor(ide, api) {
    this.ide = ide; this.api = api; this.sessionId = null; this.stopId = null;
    this.inputRequestId = null; this.inputTarget = null; this.inputText = ''; this.inputPrompt = ''; this.outputText = ''; this.streamedOutputText = ''; this.pendingEvents = [];
    this.disposers = [api.onBackendEvent(message => this.event(message)),
      api.onBackendFault(fault => this.fault(fault.message))];
  }
  async start(file, launchKind = 'run', breakpoints = [], stopFirst = true) {
    const ide = this.ide;
    ide.running = true; this.starting = true; this.pendingStop = false; this.pendingEvents = []; this.outputText = ''; this.streamedOutputText = ''; this.clearedOutputPrefix = ''; this.inputPrompt = ''; this.inputText = ''; this.inputRequestId = null; this.inputTarget = null; this.stopId = null;
    this.canStepBack=false;this.callDepth=0;this.line=null;this.debugBusy=false;ide.trace?.clearMarker();ide.trace?.update();
    ide._stopRequested = false; ide.clearDiagnostics(); ide.showPanel(); ide.switchPanel('console');
    ide.setStatus(I18N.t('trace.preparing'));
    if (!ide.ensureConsole()) { ide.running = false; this.starting = false; ide.trace?.finished(); return; }
    ide.term.reset(); this.startedAt = performance.now();
    const document = ide.openFiles.get(file);
    const response = await this.api.startExecution({ documentId: document.documentId,
      documentRevision: document.revision, sourceText: document.model.getValue(), sourceFilePath: file,
      launchKind, breakpointLines: breakpoints, stopBeforeFirstInstruction: launchKind === 'debug' && stopFirst });
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
        this.inputRequestId = payload.inputRequestId; this.inputText = ''; this.inputPrompt = payload.prompt;
        this.inputTarget = payload.target || 'console';
        this.outputText = payload.outputText; this.renderOutput();
        if (this.inputTarget !== 'screen') this.ide.term.focus();
        break;
      case 'input.completed':
        if (payload.inputRequestId !== this.inputRequestId) break;
        this.inputRequestId = null; this.inputPrompt = ''; this.inputText = ''; this.inputTarget = null;
        this.renderOutput(); this.ide.setStatus(I18N.t('status.running'));
        break;
      case 'debug.paused':
        this.callDepth=payload.callDepth;this.line=payload.line;this.ide.trace?.mark(payload);
        this.stopId = payload.stopId; this.canStepBack = payload.canStepBack;
        this.outputText = payload.outputText; this.renderOutput();
        this.ide.setStatus(I18N.t('trace.paused',payload.line ?? '—'));
        if (payload.line) { this.ide.editor.revealLineInCenter(payload.line); this.ide.editor.setPosition({ lineNumber: payload.line, column: payload.column || 1 }); }
        this.ide.editor.focus(); break;
      case 'graphics.surface':
        this.surface = payload;
        if (payload.kind === 'screen' && payload.state !== 'closed') this.ide.setStatus('Программа в окне SCREEN');
        break;
      case 'graphics.error': this.fault(payload.message); break;
      case 'execution.finished':
        this.inputRequestId = null; this.inputTarget = null; this.inputPrompt = ''; this.inputText = '';
        this.outputText = payload.outputText; this.renderOutput();
        this.ide.running = false; this.inputRequestId = null; this.stopId = null; this.sessionId = null;
        this.ide.setStatus(I18N.t(({Succeeded:'status.finished',Cancelled:'status.stopped',CompilationFailed:'trace.compileFailed',RuntimeFailed:'trace.runtimeFailed'})[payload.status] || 'trace.failed'));
        if (payload.diagnostics?.length) {
          this.ide.applyDiagnostics(payload.diagnostics.map(d => ({ ...d, file: this.ide._runFile, col: d.column, severity: 'error' })), true);
          this.ide.switchPanel('diagnostics');
        }
        document.getElementById('status-execution-time').textContent = I18N.t('trace.elapsed',Math.round(performance.now() - this.startedAt));
        this.ide.focusEditor(); break;
    }
    if(message.event==='execution.finished') this.ide.trace?.finished();
    else this.ide.trace?.update();
  }
  clearConsole() {
    this.clearedOutputPrefix = this.outputText || '';
    this.renderOutput();
  }
  renderOutput() {
    if (!this.ide.term) return;
    // Снимок службы консоли разделяет завершённые строки, но не содержит
    // завершающего перевода строки. Следующий INPUT начинается новой строкой.
    const fullHistory = this.outputText || '';
    const prefix = this.clearedOutputPrefix || '';
    if (prefix && !fullHistory.startsWith(prefix)) this.clearedOutputPrefix = '';
    const history = prefix && fullHistory.startsWith(prefix)
      ? fullHistory.slice(prefix.length).replace(/^\r?\n/, '') : fullHistory;
    const separator = this.inputRequestId && history && !history.endsWith('\n') ? '\n' : '';
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
        this.inputRequestId = null; this.inputText = ''; this.inputPrompt = ''; this.ide.term.write('\r\n');
        const result = await this.api.submitInput(this.sessionId, id, text);
        if (!result.ok) this.fault(result.error.message);
        break;
      } else if (character === '\x7f' || character === '\b') {
        if (this.inputText.length) { this.inputText = [...this.inputText].slice(0, -1).join(''); this.ide.term.write('\b \b'); }
      } else if (!/[\u0000-\u001f]/.test(character)) { this.inputText += character; this.ide.term.write(character); }
    }
  }
  async stop() {
    if (this.starting) { this.pendingStop = true; return; }
    if (!this.sessionId) return;
    const result = await this.api.stopExecution(this.sessionId);
    if (!result.ok) this.fault(result.error.message);
  }
  async debug(command) {
    if (!this.stopId || !this.sessionId) return;
    const stopId = this.stopId;
    this.stopId = null;
    this.debugBusy=true;this.ide.trace?.clearMarker();this.ide.trace?.update();
    this.ide.setStatus(I18N.t('trace.running'));
    const result = await this.api.debugCommand(this.sessionId, command, stopId);
    if (!result.ok) {
      if (this.sessionId && !this.stopId) this.stopId = stopId;
      this.ide.setStatus(result.error.message);
    }
    this.debugBusy=false;this.ide.trace?.update();
  }
  fault(message) {
    this.ide.running = false; this.sessionId = null; this.stopId = null; this.inputRequestId = null;
    this.ide.showIdeProblem('status.runFailed', message); this.ide.setStatus(I18N.t('trace.failed'));
    this.ide.trace?.finished();
  }
}
window.ExecutionController = ExecutionController;
