using System.Diagnostics;
using Axiom.Execution;
using Axiom.Ide.Documents;
using Axiom.Ide.Models;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Execution;

namespace Axiom.Ide.ViewModels;

public sealed partial class MainWindowViewModel
{
    private Task RunProgramWithDebuggingAsync()
    {
        return RunProgramAsync(useSourceBreakpoints: true);
    }

    private async Task RunProgramAsync(bool useSourceBreakpoints)
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoActiveDocument", "No active document to run.");
            return;
        }

        if (!TryPrepareRunDocument(Documents.ActiveDocument, out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString("Run.StatusRunning", "Running...");
        StatusBar.ClearTimingText();
        SetStoppedRunState(null, null);
        RaiseShellCommandCanExecuteChanged();

        // Auto-switch to output
        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();
        BottomPane.ClearOutput();

        _traceService.Write(
            "run.request",
            BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
        var runDocument = Documents.ActiveDocument;
        var sourceBreakpointLines = useSourceBreakpoints
            ? SourceBreakpointLines
            : Array.Empty<int>();
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: useSourceBreakpoints ? "RunDebug" : "Run",
            document: Documents.ActiveDocument,
            textPreview: documentContent.SourceText,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = SelectedExecutionMode.ToString(),
                ["useSourceBreakpoints"] = useSourceBreakpoints.ToString(),
                ["breakpoints"] = sourceBreakpointLines.Count().ToString(System.Globalization.CultureInfo.InvariantCulture)
            });

        var result = await ExecuteRunOperationAsync(
            () =>
            {
                var cancellation = new ManualCancellationProvider();
                _runCancellationProvider = cancellation;
                try
                {
                    return _programRunService.Run(
                        documentContent.SourceText,
                        SelectedExecutionMode,
                        sourceBreakpointLines,
                        sourceFilePath: runDocument?.FilePath,
                        cancellationProvider: cancellation);
                }
                finally
                {
                    _runCancellationProvider = null;
                }
            });

        ApplyRunOutputFromResult(result, "run.output-sync");

        BottomPane.ReplaceDiagnostics(
            result.Diagnostics.Select(d => d));

        _traceService.Write(
            "run.result",
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            result.Stopped ? "debugger.stopped" : "debugger.run-finished",
            commandName: useSourceBreakpoints ? "RunDebug" : "Run",
            document: runDocument,
            line: result.StoppedAt?.Line,
            column: result.StoppedAt?.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["status"] = result.Status.ToString(),
                ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["canStepBack"] = (result.CanStepBack).ToString()
            });

        SetStoppedRunState(useSourceBreakpoints ? result.DebuggerState : null, useSourceBreakpoints ? runDocument : null);
        UpdateRunTiming(result);

        if (result.Cancelled)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusCancelled", "Run cancelled.");
        }
        else if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void CancelRunningProgram()
    {
        _runCancellationProvider?.Cancel();
        StatusBar.StatusText = GetLocalizedString("Run.StatusCancelling", "Cancelling...");
    }

    private bool CanCancelRunningProgram() => StatusBar.IsRunning && _runCancellationProvider is not null;

    private async Task RunOrContinueProgramAsync()
    {
        if (HasStoppedRun)
        {
            await ContinueProgramAsync();
            return;
        }

        await RunProgramWithDebuggingAsync();
    }

    private async Task ContinueProgramAsync()
    {
        await ResumeStoppedProgramAsync(
            _programRunService.Continue,
            "Run.StatusContinuing",
            "Continuing...",
            "run.continue.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "Continue", document: Documents.ActiveDocument);
    }

    private async Task StepOverProgramAsync()
    {
        if (!HasStoppedRun)
        {
            await StartSteppingProgramAsync("Run.StatusSteppingOver", "Stepping over...", "run.step-over.start");
            LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOver.Start", document: Documents.ActiveDocument);
            return;
        }

        await ResumeStoppedProgramAsync(
            _programRunService.StepOver,
            "Run.StatusSteppingOver",
            "Stepping over...",
            "run.step-over.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOver", document: Documents.ActiveDocument);
    }

    private async Task StepIntoProgramAsync()
    {
        if (!HasStoppedRun)
        {
            await StartSteppingProgramAsync("Run.StatusSteppingInto", "Stepping into...", "run.step-into.start");
            LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepInto.Start", document: Documents.ActiveDocument);
            return;
        }

        await ResumeStoppedProgramAsync(
            _programRunService.StepInto,
            "Run.StatusSteppingInto",
            "Stepping into...",
            "run.step-into.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepInto", document: Documents.ActiveDocument);
    }

    private async Task StepOutProgramAsync()
    {
        await ResumeStoppedProgramAsync(
            _programRunService.StepOut,
            "Run.StatusSteppingOut",
            "Stepping out...",
            "run.step-out.result");
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepOut", document: Documents.ActiveDocument);
    }

    private async Task StepBackProgramAsync()
    {
        await ResumeStoppedProgramAsync(
            _programRunService.StepBack,
            "Run.StatusSteppingBack",
            "Stepping back...",
            "run.step-back.result",
            static state => state.CanStepBack);
        LogIdeEvent(IdeEventLevel.Info, "commands", "command.executed", commandName: "StepBack", document: Documents.ActiveDocument);
    }

    private async Task StartSteppingProgramAsync(
        string runningStatusKey,
        string runningStatusFallback,
        string resultTraceName)
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoActiveDocument", "No active document to run.");
            return;
        }

        if (!TryPrepareRunDocument(Documents.ActiveDocument, out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString(runningStatusKey, runningStatusFallback);
        StatusBar.ClearTimingText();
        SetStoppedRunState(null, null);
        RaiseShellCommandCanExecuteChanged();

        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();
        BottomPane.ClearOutput();

        _traceService.Write(
            "run.step-start.request",
            BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
        LogIdeEvent(
            IdeEventLevel.Info,
            "commands",
            "command.executed",
            commandName: "StepStart",
            document: Documents.ActiveDocument,
            textPreview: documentContent.SourceText,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["executionMode"] = ExecutionMode.Bytecode.ToString()
            });

        var runDocument = Documents.ActiveDocument;
        var result = await ExecuteRunOperationAsync(
            () => _programRunService.Run(
                documentContent.SourceText,
                ExecutionMode.Bytecode,
                SourceBreakpointLines,
                stopBeforeFirstInstruction: true,
                sourceFilePath: runDocument?.FilePath));

        ApplyRunOutputFromResult(result, "run.output-sync");

        BottomPane.ReplaceDiagnostics(
            result.Diagnostics.Select(d => d));

        _traceService.Write(
            resultTraceName,
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            "debugger.step-start-result",
            commandName: "StepStart",
            document: runDocument,
            line: result.StoppedAt?.Line,
            column: result.StoppedAt?.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["trace"] = resultTraceName,
                ["status"] = result.Status.ToString(),
                ["stopped"] = result.Stopped.ToString()
            });

        SetStoppedRunState(result.DebuggerState, runDocument);
        UpdateRunTiming(result);

        if (result.Cancelled)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusCancelled", "Run cancelled.");
        }
        else if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private async Task ResumeStoppedProgramAsync(
        Func<IProgramDebuggerState, ProgramRunResult> resume,
        string runningStatusKey,
        string runningStatusFallback,
        string resultTraceName,
        Func<IProgramDebuggerState, bool>? canResume = null)
    {
        canResume ??= static state => state.CanResume;

        if (!HasStoppedRun || _stoppedRunState is null || !canResume(_stoppedRunState))
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRun", "No stopped program to continue.");
            return;
        }

        var stoppedRunDocument = _stoppedRunDocument;
        StatusBar.IsRunning = true;
        StatusBar.StatusText = GetLocalizedString(runningStatusKey, runningStatusFallback);
        StatusBar.ClearTimingText();
        RaiseShellCommandCanExecuteChanged();

        BottomPane.ShowOutput();
        BottomPane.RequestOutputFocus();

        var result = await ExecuteRunOperationAsync(() => resume(_stoppedRunState));

        ApplyRunOutputFromResult(result, "run.continue.output-sync");

        BottomPane.ReplaceDiagnostics(result.Diagnostics.Select(d => d));

        _traceService.Write(
            resultTraceName,
            BuildRunResultTrace(result));
        LogIdeEvent(
            result.Succeeded ? IdeEventLevel.Info : IdeEventLevel.Warning,
            "debugger",
            "debugger.resume-result",
            commandName: resultTraceName,
            document: stoppedRunDocument,
            line: result.StoppedAt?.Line,
            column: result.StoppedAt?.Column,
            outputLength: result.OutputText.Length,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["status"] = result.Status.ToString(),
                ["stopped"] = result.Stopped.ToString(),
                ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["canStepBack"] = result.CanStepBack.ToString()
            });

        SetStoppedRunState(result.DebuggerState, stoppedRunDocument);
        UpdateRunTiming(result);

        if (result.Cancelled)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusCancelled", "Run cancelled.");
        }
        else if (!result.Succeeded || result.Diagnostics.Count > 0)
        {
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = GetLocalizedFormattedString("Run.StatusStopped", "Run stopped: {0} diagnostics.", result.Diagnostics.Count);
        }
        else if (result.Stopped)
        {
            StatusBar.StatusText = FormatStoppedStatus(result);
        }
        else
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusSucceeded", "Run succeeded.");
        }

        StatusBar.IsRunning = false;
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void StopDebugging()
    {
        if (!HasStoppedRun)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRunToStop", "No stopped program to stop.");
            return;
        }

        SetStoppedRunState(null, null);
        StatusBar.IsRunning = false;
        StatusBar.StatusText = GetLocalizedString("Run.StatusDebuggingStopped", "Debugging stopped.");
        LogIdeEvent(IdeEventLevel.Info, "debugger", "debugger.stopped-by-user", commandName: "StopDebugging", document: Documents.ActiveDocument);
        RaiseShellCommandCanExecuteChanged();
        UpdateTitle();
    }

    private void RestartDebugging()
    {
        if (!HasStoppedRun)
        {
            StatusBar.StatusText = GetLocalizedString("Run.StatusNoStoppedRunToRestart", "No stopped program to restart.");
            return;
        }

        SetStoppedRunState(null, null);
        StatusBar.StatusText = GetLocalizedString("Run.StatusRestartingDebugging", "Restarting debugging...");
        LogIdeEvent(IdeEventLevel.Info, "debugger", "debugger.restarting", commandName: "RestartDebugging", document: Documents.ActiveDocument);
        _ = RunProgramWithDebuggingAsync();
    }

    private async Task CompileProgramAsync()
    {
        if (Documents.ActiveDocument is null)
        {
            StatusBar.StatusText = GetLocalizedString("Compile.StatusNoActiveDocument", "No active document to compile.");
            return;
        }

        if (!TryPrepareExecutionDocument(
                Documents.ActiveDocument,
                "Compile",
                "Compile",
                "The document must be saved before compilation. Save it now?",
                "The document has unsaved changes. Save before compilation?",
                "Compilation cancelled. Save the document before compilation.",
                "Compilation cancelled.",
                "Compilation failed: {0}",
                out var documentContent))
        {
            UpdateTitle();
            return;
        }

        StatusBar.IsRunning = true;
        StatusBar.ShowCompilationProgress();
        StatusBar.ClearTimingText();
        RaiseShellCommandCanExecuteChanged();
        await Task.Yield();

        var compilationStopwatch = Stopwatch.StartNew();
        var progress = new Progress<int>(percent => StatusBar.CompilationProgressPercent = percent);
        var compilationTask = Task.Run(() => _programRunService.Compile(documentContent.SourceText, progress));

        try
        {
            _traceService.Write(
                "compile.request",
                BuildRunRequestTrace(Documents.ActiveDocument, documentContent.SourceText));
            LogIdeEvent(
                IdeEventLevel.Info,
                "commands",
                "command.executed",
                commandName: "Compile",
                document: Documents.ActiveDocument,
                textPreview: documentContent.SourceText,
                properties: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["executionMode"] = SelectedExecutionMode.ToString()
                });

            while (!compilationTask.IsCompleted)
            {
                StatusBar.ShowCompilationTiming(BuildCompilationTimingText(compilationStopwatch.Elapsed));
                await Task.Delay(50);
            }

            var result = await compilationTask;
            compilationStopwatch.Stop();
            StatusBar.ShowCompilationTiming(BuildCompilationTimingText(compilationStopwatch.Elapsed));
            BottomPane.ReplaceDiagnostics(result.Diagnostics);

            _traceService.Write(
                "compile.result",
                $"status={result.Status} | diagnostics={result.Diagnostics.Count}");
            LogIdeEvent(
                !result.Succeeded || result.Diagnostics.Count > 0 ? IdeEventLevel.Warning : IdeEventLevel.Info,
                "commands",
                "compile.result",
                commandName: "Compile",
                document: Documents.ActiveDocument,
                properties: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["status"] = result.Status.ToString(),
                    ["diagnostics"] = result.Diagnostics.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
                });

            if (!result.Succeeded || result.Diagnostics.Count > 0)
            {
                BottomPane.ShowDiagnostics();
                BottomPane.RequestDiagnosticsFocus();
                StatusBar.StatusText = GetLocalizedFormattedString("Compile.StatusStopped", "Compilation stopped: {0} diagnostics.", result.Diagnostics.Count);
            }
            else
            {
                StatusBar.StatusText = GetLocalizedString("Compile.StatusSucceeded", "Compilation succeeded.");
            }
        }
        finally
        {
            StatusBar.HideCompilationProgress();
            StatusBar.IsRunning = false;
            RaiseShellCommandCanExecuteChanged();
            UpdateTitle();
        }
    }

    private bool TryPrepareRunDocument(DocumentTabViewModel document, out DocumentFileContent documentContent)
    {
        return TryPrepareExecutionDocument(
            document,
            "Run",
            "Run",
            "The document must be saved before running. Save it now?",
            "The document has unsaved changes. Save before running?",
            "Run cancelled. Save the document before running.",
            "Run cancelled.",
            "Run failed: {0}",
            out documentContent);
    }

    public void SetSourceBreakpointLines(IEnumerable<int> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (Documents.ActiveDocument is not { } document)
        {
            return;
        }

        document.SetSourceBreakpointLines(lines);
    }

    public void ClearSourceBreakpointLines()
    {
        SetSourceBreakpointLines([]);
    }

    private void ClearAllSourceBreakpoints()
    {
        if (Documents.ActiveDocument is null)
        {
            return;
        }

        var count = SourceBreakpointLines.Count;
        var hadStoppedRun = HasStoppedRun;
        ClearSourceBreakpointLines();

        if (hadStoppedRun)
        {
            SetStoppedRunState(null, null);
            StatusBar.IsRunning = false;
        }

        if (hadStoppedRun && count > 0)
        {
            StatusBar.StatusText = GetLocalizedFormattedString(
                "Run.Breakpoints.ClearedAllAndStoppedDebugging",
                "Cleared {0} breakpoints. Debugging stopped.",
                count);
        }
        else
        {
            StatusBar.StatusText = count > 0
                ? GetLocalizedFormattedString("Run.Breakpoints.ClearedAll", "Cleared {0} breakpoints.", count)
                : GetLocalizedString("Run.Breakpoints.NoBreakpointsToClear", "No breakpoints to clear.");
        }
    }

    public void ToggleSourceBreakpointLine(int line)
    {
        if (line <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(line), line, "Source breakpoint line must be greater than zero.");
        }

        if (Documents.ActiveDocument is null)
        {
            return;
        }

        Documents.ActiveDocument.ToggleSourceBreakpointLine(line);
    }

    private void ToggleCurrentSourceBreakpoint(object? parameter)
    {
        if (!TryGetPositiveLine(parameter, out var line))
        {
            StatusBar.StatusText = GetLocalizedString("Run.Breakpoints.InvalidLine", "Cannot toggle breakpoint for the current line.");
            return;
        }

        var hadBreakpoint = SourceBreakpointLines.Contains(line);
        ToggleSourceBreakpointLine(line);

        StatusBar.StatusText = hadBreakpoint
            ? GetLocalizedFormattedString("Run.Breakpoints.Removed", "Breakpoint removed from line {0}.", line)
            : GetLocalizedFormattedString("Run.Breakpoints.Added", "Breakpoint added to line {0}.", line);
    }

    private bool CanToggleCurrentSourceBreakpoint(object? parameter) =>
        !IsShellInteractionLocked() && TryGetPositiveLine(parameter, out _);

    private bool CanClearAllSourceBreakpoints() =>
        Documents.ActiveDocument is not null
        && SourceBreakpointLines.Count > 0
        && !IsShellInteractionLocked();

    private bool IsStoppedRunForActiveDocument =>
        _stoppedRunDocument is not null
        && ReferenceEquals(_stoppedRunDocument, Documents.ActiveDocument);

    private void SetStoppedRunState(
        IProgramDebuggerState? stopState,
        DocumentTabViewModel? document)
    {
        var normalizedDocument = stopState is null ? null : document;
        if (ReferenceEquals(_stoppedRunState, stopState)
            && ReferenceEquals(_stoppedRunDocument, normalizedDocument))
        {
            return;
        }

        _stoppedRunState = stopState;
        _stoppedRunDocument = normalizedDocument;
        SetStoppedRunLine(stopState?.StopInfo.Line);
        TraceStoppedRunState(stopState, normalizedDocument);
        NotifyStoppedRunPresentationChanged();
    }

    private void ApplyRunOutputFromResult(ProgramRunResult result, string traceName)
    {
        if (BottomPane.IsWaitingForInput
            || string.Equals(BottomPane.OutputText, result.OutputText, StringComparison.Ordinal))
        {
            return;
        }

        BottomPane.OutputText = result.OutputText;
        _traceService.Write(
            traceName,
            $"Applied output from ProgramRunResult. outputChars={result.OutputText.Length} | output=\"{BuildSourcePreview(result.OutputText)}\"");
    }

    private void TraceStoppedRunState(
        IProgramDebuggerState? stopState,
        DocumentTabViewModel? document)
    {
        if (stopState is null)
        {
            _traceService.Write(
                "run.stop-state",
                $"stopped=false | document={document?.DisplayName ?? "<none>"} | outputChars={BottomPane.OutputText.Length} | output=\"{BuildSourcePreview(BottomPane.OutputText)}\"");
            LogIdeEvent(
                IdeEventLevel.Info,
                "debugger",
                "debugger.stop-state-cleared",
                commandName: "StopState",
                document: document,
                outputLength: BottomPane.OutputText.Length,
                textPreview: BottomPane.OutputText);
            return;
        }

        var info = stopState.StopInfo;
        _traceService.Write(
            "run.stop-state",
            $"stopped=true | line={info.Line} | column={info.Column} | opcode={info.Opcode} | callDepth={info.CallDepth} | canResume={stopState.CanResume} | canStepBack={info.CanStepBack} | document={document?.DisplayName ?? "<none>"} | outputChars={BottomPane.OutputText.Length} | output=\"{BuildSourcePreview(BottomPane.OutputText)}\"");
        _eventLogService.Write(new IdeEventLogEntry
        {
            Level = IdeEventLevel.Info,
            Category = "debugger",
            EventName = "debugger.stop-state-changed",
            Source = nameof(MainWindowViewModel),
            CommandName = "StopState",
            DocumentPath = document?.FilePath,
            DocumentName = document?.DisplayName,
            Line = info.Line,
            Column = info.Column,
            DebugLine = info.Line,
            Opcode = info.Opcode,
            CallDepth = info.CallDepth,
            CanStepBack = info.CanStepBack,
            OutputLength = BottomPane.OutputText.Length,
            TextLength = BottomPane.OutputText.Length,
            TextPreview = BottomPane.OutputText,
            StatusBar = BuildStatusBarSnapshot(),
            Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["canResume"] = stopState.CanResume.ToString()
            }
        });
    }

    private void SetStoppedRunLine(int? line)
    {
        var normalizedLine = line is > 0 ? line : null;
        if (_stoppedRunLine == normalizedLine)
        {
            return;
        }

        _stoppedRunLine = normalizedLine;
        OnPropertyChanged(nameof(StoppedRunLine));
    }

    private void NotifyStoppedRunPresentationChanged()
    {
        OnPropertyChanged(nameof(StoppedRunState));
        OnPropertyChanged(nameof(HasStoppedRun));
        OnPropertyChanged(nameof(StoppedRunLine));
        ContinueRunCommand.RaiseCanExecuteChanged();
        StepOverCommand.RaiseCanExecuteChanged();
        StepIntoCommand.RaiseCanExecuteChanged();
        StepOutCommand.RaiseCanExecuteChanged();
        StepBackCommand.RaiseCanExecuteChanged();
        RunOrContinueCommand.RaiseCanExecuteChanged();
        StopDebuggingCommand.RaiseCanExecuteChanged();
        RestartDebuggingCommand.RaiseCanExecuteChanged();
        ShowIdeEventLogViewerCommand.RaiseCanExecuteChanged();
        OpenIdeEventLogFileCommand.RaiseCanExecuteChanged();
        OpenIdeEventLogFolderCommand.RaiseCanExecuteChanged();
        ClearIdeEventLogsCommand.RaiseCanExecuteChanged();
        ClearAllSourceBreakpointsCommand.RaiseCanExecuteChanged();
    }

    private static bool TryGetPositiveLine(object? value, out int line)
    {
        switch (value)
        {
            case int intValue:
                line = intValue;
                return line > 0;
            case string text when int.TryParse(text, out var parsedValue):
                line = parsedValue;
                return line > 0;
            default:
                line = 0;
                return false;
        }
    }

    private bool TryPrepareExecutionDocument(
        DocumentTabViewModel document,
        string actionKeyPrefix,
        string actionTitleFallback,
        string savePromptUntitledFallback,
        string savePromptDirtyFallback,
        string cancelledNeedSaveFallback,
        string cancelledFallback,
        string loadFailedFallback,
        out DocumentFileContent documentContent)
    {
        ArgumentNullException.ThrowIfNull(document);

        documentContent = default!;

        if (document.IsUntitled || document.IsDirty)
        {
            var promptMessage = document.IsUntitled
                ? GetLocalizedString($"{actionKeyPrefix}.SavePromptUntitled", savePromptUntitledFallback)
                : GetLocalizedString($"{actionKeyPrefix}.SavePromptDirty", savePromptDirtyFallback);

            if (!_promptService.ShowConfirmation(promptMessage, GetLocalizedString($"{actionKeyPrefix}.Title", actionTitleFallback)))
            {
                StatusBar.StatusText = GetLocalizedString($"{actionKeyPrefix}.StatusCancelledNeedSave", cancelledNeedSaveFallback);
                _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"User declined save-before-{actionKeyPrefix.ToLowerInvariant()} prompt for '{document.DisplayName}'.");
                return false;
            }

            if (!Documents.SaveDocument(document, out var saveStatus))
            {
                StatusBar.StatusText = saveStatus ?? GetLocalizedString($"{actionKeyPrefix}.StatusCancelled", cancelledFallback);
                _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"Save-before-{actionKeyPrefix.ToLowerInvariant()} failed for '{document.DisplayName}': {saveStatus ?? "<none>"}");
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(document.FilePath))
        {
            StatusBar.StatusText = GetLocalizedString($"{actionKeyPrefix}.StatusCancelledNeedSave", cancelledNeedSaveFallback);
            _traceService.Write($"{actionKeyPrefix.ToLowerInvariant()}.blocked", $"Document '{document.DisplayName}' has no file path for {actionKeyPrefix.ToLowerInvariant()}.");
            return false;
        }

        try
        {
            documentContent = _documentFileService.LoadDocument(document.FilePath);
            return true;
        }
        catch (Exception exception)
        {
            var message = GetLocalizedFormattedString($"{actionKeyPrefix}.LoadFailed", loadFailedFallback, exception.Message);
            BottomPane.ReplaceDiagnostics(
                [ProgramDiagnosticItem.CreateInfrastructure("IDE0002", message)]);
            BottomPane.ShowDiagnostics();
            BottomPane.RequestDiagnosticsFocus();
            StatusBar.StatusText = message;
            _traceService.WriteException($"{actionKeyPrefix.ToLowerInvariant()}.load-failed", exception, $"Could not load '{document.FilePath}' for {actionKeyPrefix.ToLowerInvariant()}.");
            return false;
        }
    }

    private void SubmitInput()
    {
        if (_programConsoleService.IsScreenOutputActive)
        {
            return;
        }

        _programConsoleService.SubmitInput();
    }
}
