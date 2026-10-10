class ExecutionController {
  constructor(ide, api) {
    this.ide = ide; this.api = api; this.sessionId = null; this.stopId = null;
    this.inputRequestId = null; this.inputText = ''; this.outputText = ''; this.pendingEvents = [];
    this.disposers = [api.onBackendEvent(message => this.event(message)),
      api.onBackendFault(fault => this.fault(fault.message))];
  }
  async start(file, launchKind = 'run', breakpoints = []) {
    const ide = this.ide;
    ide.running = true; this.starting = true; this.pendingStop = false; this.pendingEvents = []; this.outputText = ''; this.inputRequestId = null; this.stopId = null;
    ide._stopRequested = false; ide.clearDiagnostics(); ide.showPanel(); ide.switchPanel('console');
    ide.setStatus('Подготовка программы…');
    if (!ide.ensureConsole()) { ide.running = false; this.starting = false; return; }
    ide.term.reset(); this.startedAt = performance.now();
    const document = ide.openFiles.get(file);
    const response = await this.api.startExecution({ documentId: document.documentId,
      documentRevision: document.revision, sourceText: document.model.getValue(), sourceFilePath: file,
      launchKind, breakpointLines: breakpoints, stopBeforeFirstInstruction: launchKind === 'debug' });
    this.starting = false;
    if (!response.ok) { this.fault(response.error?.message || 'Не удалось начать выполнение.'); return; }
    this.sessionId = response.payload.sessionId;
    for (const event of this.pendingEvents) this.event(event);
    this.pendingEvents = [];
    if (this.pendingStop && this.sessionId) await this.stop();
    ide.term.focus();
  }
  event(message) {
    if (this.starting) { this.pendingEvents.push(message); return; }
    if (!this.sessionId || message.sessionId !== this.sessionId) return;
    const payload = message.payload;
    switch (message.event) {
      case 'execution.state':
        this.ide.setStatus(({ Compiling: 'Компиляция…', Running: 'Выполняется', WaitingForInput: 'Ожидание ввода', Paused: 'Пауза отладки' })[payload.state] || payload.state);
        break;
      case 'output.append': case 'output.replace':
        this.outputText = message.event === 'output.append' ? this.outputText + payload.text : payload.text;
        this.renderOutput(); this.api.acknowledgeOutput(this.sessionId, payload.outputVersion); break;
      case 'input.request':
        this.inputRequestId = payload.inputRequestId; this.inputText = '';
        this.outputText = payload.outputText; this.renderOutput();
        this.ide.term.write(payload.prompt); this.ide.term.focus(); break;
      case 'debug.paused':
        this.stopId = payload.stopId; this.canStepBack = payload.canStepBack;
        this.outputText = payload.outputText; this.renderOutput();
        this.ide.setStatus(`Пауза отладки: строка ${payload.line ?? '—'}`);
        if (payload.line) { this.ide.editor.revealLineInCenter(payload.line); this.ide.editor.setPosition({ lineNumber: payload.line, column: payload.column || 1 }); }
        this.ide.editor.focus(); break;
      case 'graphics.surface':
        this.surface = payload;
        if (payload.kind === 'screen' && payload.state !== 'closed') this.ide.setStatus('Программа в окне SCREEN');
        break;
      case 'graphics.error': this.fault(payload.message); break;
      case 'execution.finished':
        this.outputText = payload.outputText; this.renderOutput();
        this.ide.running = false; this.inputRequestId = null; this.stopId = null; this.sessionId = null;
        this.ide.setStatus(({ Succeeded: 'Завершено', Cancelled: 'Остановлено', CompilationFailed: 'Ошибка компиляции', RuntimeFailed: 'Ошибка выполнения' })[payload.status] || 'Ошибка служебного процесса');
        if (payload.diagnostics?.length) {
          this.ide.applyDiagnostics(payload.diagnostics.map(d => ({ ...d, file: this.ide._runFile, col: d.column, severity: 'error' })), true);
          this.ide.switchPanel('diagnostics');
        }
        document.getElementById('status-execution-time').textContent = `Время: ${Math.round(performance.now() - this.startedAt)} мс`;
        this.ide.focusEditor(); break;
    }
  }
  renderOutput() {
    if (!this.ide.term) return;
    this.ide.term.reset(); this.ide.term.write((this.outputText || '').replace(/\r?\n/g, '\r\n'));
  }
  async input(data) {
    if (data === '\u0003') { await this.stop(); return; }
    if (!this.inputRequestId) return;
    for (const character of data) {
      if (character === '\r' || character === '\n') {
        const id = this.inputRequestId; const text = this.inputText;
        this.inputRequestId = null; this.inputText = ''; this.ide.term.write('\r\n');
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
    const result = await this.api.debugCommand(this.sessionId, command, stopId);
    if (!result.ok) {
      if (this.sessionId && !this.stopId) this.stopId = stopId;
      this.ide.setStatus(result.error.message);
    }
  }
  fault(message) {
    this.ide.running = false; this.sessionId = null; this.stopId = null; this.inputRequestId = null;
    this.ide.showIdeProblem('status.runFailed', message); this.ide.setStatus('Ошибка служебного процесса');
  }
}
window.ExecutionController = ExecutionController;
