using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Diagnostics;
using Axiom.Ide.Services.EventLog;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
using Axiom.Runtime;

namespace Axiom.Ide.Tests;

public sealed class ProgramRunServiceTests
{
    [Fact]
    public void Compile_ReturnsSuccessWithoutDiagnostics()
    {
        var service = CreateService();

        var result = service.Compile("PRINT 1 + 2");

        Assert.Equal(ProgramCompilationStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Compile_MapsSemanticDiagnosticsToUiModel()
    {
        var service = CreateService();

        var result = service.Compile("PRINT missingValue");

        Assert.Equal(ProgramCompilationStatus.SemanticFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("Semantic", diagnostic.Stage);
        Assert.Equal("MB2001", diagnostic.Code);
    }

    [Fact]
    public void Run_ReturnsSuccessWithJoinedOutput()
    {
        var service = CreateService();

        const string sourceText = """
PRINT "hello"
PRINT 1 + 2
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal($"hello{Environment.NewLine}3", result.OutputText);
    }

    [Fact]
    public void Run_WhenProgramCompletes_EndsConsoleSession()
    {
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService());
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        var result = service.Run("PRINT 1");

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.True(consoleService.SessionStarted);
        Assert.True(consoleService.SessionEnded);
    }

    [Fact]
    public void Run_AfterCompile_UsesCompiledCache()
    {
        var traceService = new FakeIdeTraceService();
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService());
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService, traceService);

        var compileResult = service.Compile("PRINT 1 + 2");
        var runResult = service.Run("PRINT 1 + 2");

        Assert.Equal(ProgramCompilationStatus.Success, compileResult.Status);
        Assert.Equal(ProgramRunStatus.Success, runResult.Status);
        Assert.True(runResult.UsedCompiledCache);
        Assert.Equal(ExecutionMode.Bytecode, runResult.RequestedExecutionMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, runResult.ActualEngine);
        Assert.Equal(TimeSpan.Zero, runResult.PreparationDuration);
        Assert.True(runResult.ExecutionDuration >= TimeSpan.Zero);
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "run.pipeline"
                && entry.Message.Contains("compiledCache=hit", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_WithBytecodeMode_MapsIndirectFunctionValueCallToSuccess()
    {
        var service = CreateService();

        const string sourceText = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

DIM op AS FUNCTION(INTEGER) AS INTEGER
op = AddOne
PRINT op(4)
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedExecutionMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("5", result.OutputText);
    }

    [Fact]
    public void Run_WithBytecodeMode_RunsTryBlockOnBytecodeVm()
    {
        var service = CreateService();

        const string sourceText = """
TRY
    PRINT "before"
    PRINT 10 \ 0
CATCH err
    PRINT "caught"
    PRINT err.Code
FINALLY
    PRINT "cleanup"
END TRY
PRINT "after"
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedExecutionMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(
            string.Join(Environment.NewLine, "before", "caught", "MB3001", "cleanup", "after"),
            result.OutputText);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Run_WithSourceBreakpointLines_ReturnsStoppedResult()
    {
        var service = CreateService();

        const string sourceText = """
PRINT 1
PRINT 2
PRINT 3
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal("1", result.OutputText);
    }

    [Fact]
    public void Run_WithStopBeforeFirstInstruction_ReturnsStoppedResultAtFirstExecutableLine()
    {
        var service = CreateService();

        const string sourceText = """
' Comments and routine declarations are skipped before the first executable line.
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
PRINT "done"
""";

        var result = service.Run(
            sourceText,
            ExecutionMode.Bytecode,
            stopBeforeFirstInstruction: true);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.Equal(6, result.StoppedAt!.SourceLocation.Line);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(string.Empty, result.OutputText);
    }

    [Fact]
    public void Continue_AfterSourceBreakpoint_ResumesStoppedBytecodeVm()
    {
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService());
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
PRINT 1
PRINT 2
PRINT 3
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);
        var continuedResult = service.Continue(stoppedResult.StopState!);

        Assert.True(stoppedResult.Stopped);
        Assert.Equal(ProgramRunStatus.Success, continuedResult.Status);
        Assert.False(continuedResult.Stopped);
        Assert.False(stoppedResult.StopState!.CanResume);
        Assert.Equal(["1", "2", "3"], consoleService.WrittenOutput);
    }

    [Fact]
    public void StepOver_AfterSourceBreakpoint_StopsAtNextSourceLine()
    {
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService());
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
PRINT 1
PRINT 2
PRINT 3
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);
        var steppedResult = service.StepOver(stoppedResult.StopState!);

        Assert.True(stoppedResult.Stopped);
        Assert.Equal(ProgramRunStatus.Success, steppedResult.Status);
        Assert.True(steppedResult.Stopped);
        Assert.Equal(3, steppedResult.StoppedAt!.SourceLocation.Line);
        Assert.False(stoppedResult.StopState!.CanResume);
        Assert.Equal(["1", "2"], consoleService.WrittenOutput);
    }

    [Fact]
    public void StepOver_AfterTrailingCommaPrint_ReturnsPendingOutputPreview()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
FOR i = 1 TO 2
    PRINT i,
NEXT
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);
        var steppedResult = service.StepOver(stoppedResult.StopState!);

        Assert.True(steppedResult.Stopped);
        Assert.Equal("1 ", steppedResult.OutputText);
        Assert.Equal(string.Empty, consoleService.OutputText);
    }

    [Fact]
    public void StepInto_AfterCallBreakpoint_StopsInsideRoutine()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [5]);
        var steppedResult = service.StepInto(stoppedResult.StopState!);

        Assert.True(steppedResult.Stopped);
        Assert.Equal(2, steppedResult.StoppedAt!.SourceLocation.Line);
        Assert.Equal(2, steppedResult.StoppedAt.CallDepth);
        Assert.Equal(string.Empty, steppedResult.OutputText);
    }

    [Fact]
    public void StepOut_FromRoutine_StopsAfterReturningToCaller()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
PRINT "done"
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [5]);
        var insideRoutine = service.StepInto(stoppedResult.StopState!);
        var steppedOut = service.StepOut(insideRoutine.StopState!);

        Assert.True(steppedOut.Stopped);
        Assert.Equal(1, steppedOut.StoppedAt!.CallDepth);
        Assert.Equal(string.Empty, steppedOut.OutputText);
    }

    [Fact]
    public void StepBack_AfterReversibleBreakpoint_ReturnsPreviousStoppedLine()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [3]);
        Assert.True(stoppedResult.CanStepBack);

        var steppedBack = service.StepBack(stoppedResult.StopState!);

        Assert.Equal(ProgramRunStatus.Success, steppedBack.Status);
        Assert.True(steppedBack.Stopped);
        Assert.Equal(2, steppedBack.StoppedAt!.SourceLocation.Line);
        Assert.True(steppedBack.CanStepBack);
        Assert.False(stoppedResult.StopState!.CanResume);
        Assert.Equal(string.Empty, steppedBack.OutputText);
    }

    [Fact]
    public void StepBack_InsideRoutineAfterPrintedOutput_RestoresConsoleOutput()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
SUB PrintBanner(title AS STRING)
    PRINT "sub: begin"
    PRINT title
    PRINT "sub: end"
END SUB

PrintBanner("debug sample")
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [7]);
        var insideRoutine = service.StepInto(stoppedResult.StopState!);
        var afterFirstPrint = service.StepOver(insideRoutine.StopState!);
        var afterSecondPrint = service.StepOver(afterFirstPrint.StopState!);

        Assert.True(afterSecondPrint.Stopped);
        Assert.True(afterSecondPrint.CanStepBack);
        Assert.Equal(4, afterSecondPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal("sub: begin" + Environment.NewLine + "debug sample", consoleService.OutputText);

        var backed = service.StepBack(afterSecondPrint.StopState!);

        Assert.True(backed.Stopped);
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        Assert.Equal("sub: begin", consoleService.OutputText);
        Assert.Equal("sub: begin" + Environment.NewLine + "debug sample", backed.OutputText);
    }

    [Fact]
    public void StepBack_WithArrayState_RemainsAvailable()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20
PRINT values(1)
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [4]);

        Assert.True(stoppedResult.Stopped);
        Assert.True(stoppedResult.CanStepBack);
        Assert.Equal(4, stoppedResult.StoppedAt!.SourceLocation.Line);

        var steppedBack = service.StepBack(stoppedResult.StopState!);

        Assert.Equal(ProgramRunStatus.Success, steppedBack.Status);
        Assert.True(steppedBack.Stopped);
        Assert.Equal(3, steppedBack.StoppedAt!.SourceLocation.Line);
        Assert.True(steppedBack.CanStepBack);
        Assert.Equal(string.Empty, consoleService.OutputText);
    }

    [Fact]
    public void StepBack_FromArrayElementPrint_SkipsInternalExpressionStops()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
PRINT values(1)
PRINT 99
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [4]);

        Assert.True(stoppedResult.Stopped);
        Assert.Equal(4, stoppedResult.StoppedAt!.SourceLocation.Line);
        Assert.Equal("10", consoleService.OutputText);

        var steppedBack = service.StepBack(stoppedResult.StopState!);

        Assert.Equal(ProgramRunStatus.Success, steppedBack.Status);
        Assert.True(steppedBack.Stopped);
        Assert.Equal(3, steppedBack.StoppedAt!.SourceLocation.Line);
        Assert.True(steppedBack.CanStepBack);
        Assert.Equal(string.Empty, consoleService.OutputText);
    }

    [Fact]
    public void StepBack_AcrossArraySort_ReturnsToArraySortAndAssignments()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER

board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5

ARRAYSORT(board, COLUMN)

PRINT board(1, 1)
PRINT board(1, 2)
""";

        var stoppedResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [13]);

        Assert.True(stoppedResult.Stopped);
        Assert.Equal(13, stoppedResult.StoppedAt!.SourceLocation.Line);
        Assert.Equal("2", consoleService.OutputText);
        Assert.True(stoppedResult.CanStepBack);

        var toArraySort = service.StepBack(stoppedResult.StopState!);

        Assert.Equal(ProgramRunStatus.Success, toArraySort.Status);
        Assert.True(toArraySort.Stopped);
        Assert.Equal(12, toArraySort.StoppedAt!.SourceLocation.Line);
        Assert.Equal(string.Empty, consoleService.OutputText);
        Assert.True(toArraySort.CanStepBack);

        var toLastAssignment = service.StepBack(toArraySort.StopState!);

        Assert.Equal(ProgramRunStatus.Success, toLastAssignment.Status);
        Assert.True(toLastAssignment.Stopped);
        Assert.Equal(10, toLastAssignment.StoppedAt!.SourceLocation.Line);
        Assert.Equal(string.Empty, consoleService.OutputText);
    }

    [Fact]
    public void StepBack_ThenContinue_PreservesArraySortResults()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
DIM board(1 TO 2, 1 TO 3) AS INTEGER
board(1, 1) = 4
board(1, 2) = 2
board(1, 3) = 6
board(2, 1) = 3
board(2, 2) = 1
board(2, 3) = 5
ARRAYSORT(board, COLUMN)
PRINT board(1, 1)
PRINT board(1, 2)
PRINT board(1, 3)
PRINT board(2, 1)
PRINT board(2, 2)
PRINT board(2, 3)
""";

        var firstPrint = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [10]);

        Assert.True(firstPrint.Stopped);
        Assert.Equal(10, firstPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal("2", consoleService.OutputText);

        var atFirstPrint = service.StepBack(firstPrint.StopState!);
        Assert.True(atFirstPrint.Stopped);
        Assert.Equal(9, atFirstPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal(string.Empty, consoleService.OutputText);

        var atArraySort = service.StepBack(atFirstPrint.StopState!);
        Assert.True(atArraySort.Stopped);
        Assert.Equal(8, atArraySort.StoppedAt!.SourceLocation.Line);
        Assert.Equal(string.Empty, consoleService.OutputText);

        var rerunPrint = service.Continue(atArraySort.StopState!);
        Assert.True(rerunPrint.Stopped);
        Assert.Equal(10, rerunPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal("2", consoleService.OutputText);

        var completed = service.Continue(rerunPrint.StopState!);
        Assert.False(completed.Stopped);
        Assert.Equal(
            string.Join(Environment.NewLine, ["2", "4", "6", "1", "3", "5"]),
            consoleService.OutputText);
    }

    [Fact]
    public void Run_WithSourceBreakpointLines_UsesCompiledCache()
    {
        var service = CreateService();

        const string sourceText = """
PRINT 1
PRINT 2
""";

        var compileResult = service.Compile(sourceText);
        var runResult = service.Run(sourceText, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);

        Assert.Equal(ProgramCompilationStatus.Success, compileResult.Status);
        Assert.True(runResult.UsedCompiledCache);
        Assert.True(runResult.Stopped);
        Assert.Equal(2, runResult.StoppedAt!.SourceLocation.Line);
    }

    [Fact]
    public void Run_GraphicsStepSample_CompletesOnBytecodeVmWithoutInterpreterFallback()
    {
        var consoleService = new ProgramConsoleService();
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
SCREEN 1
CLS
COLOR 6, 2
PSET (10, 20)
PRINT "point0="; POINT(0); " point1="; POINT(1)
PSET STEP (5, 4)
PRINT "after pset step="; POINT(0); ","; POINT(1)
LINE -(30, 40)
PRINT "after line from current="; POINT(0); ","; POINT(1)
LINE STEP (5, 5)-STEP (10, 15)
PRINT "after line step-step="; POINT(0); ","; POINT(1)
PRINT "pixel 10,20="; POINT(10, 20)
PRINT "pixel 15,24="; POINT(15, 24)
SCREEN 0
PRINT "done"
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionMode.Bytecode, result.RequestedExecutionMode);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(string.Empty, result.OutputText);
        Assert.False(consoleService.IsScreenActive);
    }

    [Fact]
    public void Run_GetAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm()
    {
        var traceService = new FakeIdeTraceService();
        var consoleService = new ProgramConsoleService(traceService);
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
SCREEN 1
VIEW SCREEN (10, 10)-(30, 30)
WINDOW SCREEN (100, 200)-(120, 220)

DIM buffer(1 TO 2, 5 TO 6) AS INTEGER

PSET (104, 205), 6
PSET (105, 205), 9
PSET (104, 206), 12
PSET (105, 206), 15

GET (104, 205)-(105, 206), buffer

PRINT buffer(1, 5)
PRINT buffer(2, 5)
PRINT buffer(1, 6)
PRINT buffer(2, 6)
PRINT "GET sample finished"
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);
        var graphicsOutput = traceService.Entries
            .Where(static entry => entry.Category == "screen.output")
            .Select(static entry => entry.Message)
            .ToArray();

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["6", "9", "12", "15", "GET sample finished"], graphicsOutput);
    }

    [Fact]
    public void Run_PutAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm()
    {
        var traceService = new FakeIdeTraceService();
        var consoleService = new ProgramConsoleService(traceService);
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
SCREEN 1

DIM buffer(1 TO 2, 1 TO 1) AS DOUBLE
buffer(1, 1) = 1.9
buffer(2, 1) = -2.2

PUT (10, 10), buffer
DIM xorLeft AS INTEGER = POINT(10, 10)
DIM xorRight AS INTEGER = POINT(11, 10)

LINE (10, 10)-(11, 10), 0, BF
PUT (10, 10), buffer, PRESET
DIM presetLeft AS INTEGER = POINT(10, 10)
DIM presetRight AS INTEGER = POINT(11, 10)

PRINT xorLeft
PRINT xorRight
PRINT presetLeft
PRINT presetRight
PRINT "PUT sample finished"
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);
        var graphicsOutput = traceService.Entries
            .Where(static entry => entry.Category == "screen.output")
            .Select(static entry => entry.Message)
            .ToArray();

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(["1", "2", "254", "253", "PUT sample finished"], graphicsOutput);
    }

    [Fact]
    public void Run_Screen10BestFitAcceptanceProgram_WritesExpectedGraphicsOutputOnBytecodeVm()
    {
        var traceService = new FakeIdeTraceService();
        var consoleService = new ProgramConsoleService(
            traceService: traceService,
            screenModeSizeProvider: new FixedScreenModeSizeProvider((1918, 1079)));
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        const string sourceText = """
SCREEN 10
DIM wasActive AS BOOLEAN = SCREENISACTIVE()
DIM mode AS INTEGER = SCREENMODE()
DIM width AS INTEGER = SCREENWIDTH()
DIM height AS INTEGER = SCREENHEIGHT()
DIM cols AS INTEGER = width \ 8
DIM rows AS INTEGER = height \ 12

LOCATE rows, cols
PRINT "#"

SCREEN 0
PRINT "mode10 active="; wasActive
PRINT "mode10 mode="; mode
PRINT "mode10 width="; width
PRINT "mode10 height="; height
PRINT "mode10 cols="; cols
PRINT "mode10 rows="; rows
PRINT "Проверка SCREEN 10 завершена"
""";

        var result = service.Run(sourceText, ExecutionMode.Bytecode);
        var graphicsOutput = traceService.Entries
            .Where(static entry => entry.Category == "screen.output")
            .Select(static entry => entry.Message)
            .ToArray();

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(
            [
                "#",
                "mode10 active=TRUE",
                "mode10 mode=10",
                "mode10 width=1918",
                "mode10 height=1079",
                "mode10 cols=239",
                "mode10 rows=89",
                "Проверка SCREEN 10 завершена"
            ],
            graphicsOutput);
    }

    [Fact]
    public void Run_MapsSemanticDiagnosticsToUiModel()
    {
        var service = CreateService();
        var result = service.Run("PRINT missingValue");

        Assert.Equal(ProgramRunStatus.SemanticFailure, result.Status);
        Assert.Single(result.Diagnostics);

        var diagnostic = result.Diagnostics[0];
        Assert.Equal("Semantic", diagnostic.Stage);
        Assert.Equal("MB2001", diagnostic.Code);
        Assert.NotNull(diagnostic.Line);
        Assert.NotNull(diagnostic.Column);
    }

    [Fact]
    public void Run_MapsRuntimeDiagnosticsToUiModel()
    {
        var service = CreateService();
        var result = service.Run("PRINT 1 / 0");

        Assert.Equal(ProgramRunStatus.RuntimeFailure, result.Status);
        Assert.Single(result.Diagnostics);
        Assert.Equal("Runtime", result.Diagnostics[0].Stage);
        Assert.Equal("MB3001", result.Diagnostics[0].Code);
        Assert.Equal(1, result.Diagnostics[0].Line);
        Assert.Equal(9, result.Diagnostics[0].Column);
        Assert.Contains("ноль", result.Diagnostics[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, result.OutputText);
    }

    [Theory]
    [InlineData(ExecutionMode.Bytecode)]
    [InlineData(ExecutionMode.Interpreter)]
    public void Run_WhenHostCancellationIsRequested_ReturnsCancelled(ExecutionMode mode)
    {
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService())
        {
            IsCancellationRequested = true
        };
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        var result = service.Run(
            """
            DO WHILE TRUE
            LOOP
            PRINT "after"
            """,
            mode);

        Assert.Equal(ProgramRunStatus.Cancelled, result.Status);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.DoesNotContain("after", result.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_MapsRuntimeDiagnosticLocationsToUiModel()
    {
        var service = CreateService("oops");

        const string sourceText = """
DIM value AS INTEGER
INPUT value
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("Runtime", diagnostic.Stage);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Equal(2, diagnostic.Line);
        Assert.Equal(7, diagnostic.Column);
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_UsesPromptServiceForSingleStringInput()
    {
        var promptService = new FakeProgramInputPromptService("Ada");
        var service = CreateService(promptService);

        const string sourceText = """
DIM name AS STRING
INPUT name
PRINT name
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal("Ada", result.OutputText);
        var request = Assert.Single(promptService.Requests);
        Assert.Equal(1, request.RequestNumber);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void Run_UsesPromptServiceForMultipleInputValues()
    {
        var promptService = new FakeProgramInputPromptService("Ada, 42");
        var service = CreateService(promptService);

        const string sourceText = """
DIM name AS STRING
DIM age AS INTEGER
INPUT name, age
PRINT name; age
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal("Ada42", result.OutputText);
        var request = Assert.Single(promptService.Requests);
        Assert.Equal(1, request.RequestNumber);
        Assert.Equal(2, request.ValueCount);
    }

    [Fact]
    public void Run_UsesPromptServiceForPromptWithSemicolon()
    {
        var promptService = new FakeProgramInputPromptService("Ada");
        var service = CreateService(promptService);

        const string sourceText = """
DIM name AS STRING
INPUT "Name"; name
PRINT name
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal("Ada", result.OutputText);
        Assert.Equal("Name", Assert.Single(promptService.Requests).PromptText);
    }

    [Fact]
    public void Run_UsesPromptServiceForPromptWithComma()
    {
        var promptService = new FakeProgramInputPromptService("Ada, 42");
        var service = CreateService(promptService);

        const string sourceText = """
DIM name AS STRING
DIM age AS INTEGER
INPUT "Pair", name, age
PRINT name; age
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal("Ada42", result.OutputText);
        var request = Assert.Single(promptService.Requests);
        Assert.Equal("Pair", request.PromptText);
        Assert.Equal(2, request.ValueCount);
    }

    [Fact]
    public void Run_MapsInvalidMultiInputHandling()
    {
        var promptService = new FakeProgramInputPromptService("Ada");
        var service = CreateService(promptService);

        const string sourceText = """
DIM name AS STRING
DIM age AS INTEGER
INPUT name, age
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.RuntimeFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Contains("2", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Run_TreatsEmptyPromptResultAsEmptyStringInput()
    {
        var promptService = new FakeProgramInputPromptService(string.Empty);
        var service = CreateService(promptService);

        const string sourceText = """
DIM text AS STRING
INPUT text
PRINT LEN(text)
""";

        var result = service.Run(sourceText);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal("0", result.OutputText);
        Assert.Equal(1, Assert.Single(promptService.Requests).RequestNumber);
    }

    [Fact]
    public void Run_WritesPipelineSummaryToTrace()
    {
        var traceService = new FakeIdeTraceService();
        var consoleService = new FakeProgramConsoleService(new FakeProgramInputPromptService());
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService, traceService);

        var result = service.Run("""
PRINT "hello"
PRINT 1 + 2
""");

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Contains(
            traceService.Entries,
            static entry => entry.Category == "run.pipeline"
                && entry.Message.Contains("status=Success", StringComparison.Ordinal)
                && entry.Message.Contains("outputLines=2", StringComparison.Ordinal));
    }

    [Fact]
    public void Run_WritesStructuredDebugEvent()
    {
        var eventLogService = new FakeIdeEventLogService();
        var service = new ProgramRunService(
            new AxiomExecutionPipeline(),
            new FakeProgramConsoleService(new FakeProgramInputPromptService()),
            eventLogService: eventLogService);

        var result = service.Run("""
PRINT "hello"
PRINT 1 + 2
""");

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Contains(
            eventLogService.Entries,
            static entry => entry.Category == "debugger"
                && entry.EventName == "run.completed");
    }

    [Fact]
    public void Run_WhenInfrastructureThrows_ReturnsLocalizedInfrastructureDiagnostic()
    {
        var consoleService = new ThrowingProgramConsoleService(new InvalidOperationException("boom"));
        var service = new ProgramRunService(new AxiomExecutionPipeline(), consoleService);

        var result = service.Run("PRINT 1");

        Assert.Equal(ProgramRunStatus.InfrastructureFailure, result.Status);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("IDE0001", diagnostic.Code);
        Assert.Equal("Unexpected execution error: boom", diagnostic.Message);
    }

    private static ProgramRunService CreateService(params string[] responses)
    {
        return CreateService(new FakeProgramInputPromptService(responses));
    }

    private static ProgramRunService CreateService(IProgramInputPromptService promptService)
    {
        var consoleService = promptService as IProgramConsoleService
            ?? new FakeProgramConsoleService(promptService);
        return new ProgramRunService(new AxiomExecutionPipeline(), consoleService);
    }

    private sealed class FakeProgramInputPromptService : IProgramInputPromptService
    {
        private readonly Queue<string> _responses;

        public FakeProgramInputPromptService(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public List<ProgramInputRequest> Requests { get; } = [];

        public string RequestInput(ProgramInputRequest request)
        {
            Requests.Add(request);
            return _responses.Count > 0 ? _responses.Dequeue() : string.Empty;
        }
    }

    private sealed class FakeProgramConsoleService : IProgramConsoleService, IRuntimeCancellationProvider
    {
        private readonly IProgramInputPromptService _promptService;

        public FakeProgramConsoleService(IProgramInputPromptService promptService)
        {
            _promptService = promptService;
        }

        public List<string> WrittenOutput { get; } = [];

        public int OutputCheckpoint => WrittenOutput.Count;

        public bool SessionStarted { get; private set; }

        public bool SessionEnded { get; private set; }

        public bool IsCancellationRequested { get; init; }

        public void BeginSession()
        {
            SessionStarted = true;
            SessionEnded = false;
            WrittenOutput.Clear();
        }

        public void EndSession()
        {
            SessionEnded = true;
        }

        public void Clear()
        {
            WrittenOutput.Clear();
        }

        public void ClearOutput()
        {
            WrittenOutput.Clear();
        }

        public void EnqueueKey(string keyText)
        {
        }

        public void SetKeyDown(string keyText)
        {
        }

        public void SetKeyUp(string keyText)
        {
        }

        public void ClearKeyState()
        {
        }

        public void ClearMouseState()
        {
        }

        public string ReadKey() => string.Empty;

        public bool IsKeyDown(string keyText) => false;

        public long GetMouseX() => -1;

        public long GetMouseY() => -1;

        public bool IsMouseButtonDown(int buttonNumber) => false;

        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent() => default;

        public string RequestInput(ProgramInputRequest request) => _promptService.RequestInput(request);

        public void WriteOutput(string text)
        {
            WrittenOutput.Add(text);
        }

        public void SetMousePosition(long x, long y)
        {
        }

        public void SetMouseButtonDown(int buttonNumber)
        {
        }

        public void SetMouseButtonUp(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonPress(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonRelease(int buttonNumber)
        {
        }

        public void EnqueueMouseMove()
        {
        }

        public void EnqueueMouseWheel(int delta)
        {
        }

        public void RestoreOutputCheckpoint(int checkpoint)
        {
            if (checkpoint < 0 || checkpoint > WrittenOutput.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
            }

            WrittenOutput.RemoveRange(checkpoint, WrittenOutput.Count - checkpoint);
        }
    }

    private sealed class FakeIdeTraceService : IIdeTraceService
    {
        public List<(string Category, string Message)> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public void BeginSession(string sessionName)
        {
        }

        public void Write(string category, string message)
        {
            Entries.Add((category, message));
        }

        public void WriteException(string category, Exception exception, string? note = null)
        {
            Entries.Add((category, note ?? exception.Message));
        }
    }

    private sealed class FakeIdeEventLogService : IIdeEventLogService
    {
        public List<IdeEventLogEntry> Entries { get; } = [];

        public string LogsDirectoryPath => string.Empty;

        public string CurrentSessionLogPath => string.Empty;

        public string SessionId => "test";

        public void BeginSession(string sessionName)
        {
        }

        public void Write(IdeEventLogEntry entry)
        {
            Entries.Add(entry);
        }

        public void Flush()
        {
        }

        public int DeleteInactiveLogs() => 0;

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingProgramConsoleService : IProgramConsoleService
    {
        private readonly Exception _exception;

        public ThrowingProgramConsoleService(Exception exception)
        {
            _exception = exception;
        }

        public void BeginSession()
        {
            throw _exception;
        }

        public void EndSession()
        {
        }

        public void Clear()
        {
        }

        public void ClearOutput()
        {
        }

        public void EnqueueKey(string keyText)
        {
        }

        public void SetKeyDown(string keyText)
        {
        }

        public void SetKeyUp(string keyText)
        {
        }

        public void ClearKeyState()
        {
        }

        public void ClearMouseState()
        {
        }

        public string ReadKey() => string.Empty;

        public bool IsKeyDown(string keyText) => false;

        public long GetMouseX() => -1;

        public long GetMouseY() => -1;

        public bool IsMouseButtonDown(int buttonNumber) => false;

        public int ReadMouseButtonPress() => 0;

        public MouseInputEvent ReadMouseEvent() => default;

        public int OutputCheckpoint => 0;

        public string RequestInput(ProgramInputRequest request) => string.Empty;

        public void WriteOutput(string text)
        {
        }

        public void SetMousePosition(long x, long y)
        {
        }

        public void SetMouseButtonDown(int buttonNumber)
        {
        }

        public void SetMouseButtonUp(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonPress(int buttonNumber)
        {
        }

        public void EnqueueMouseButtonRelease(int buttonNumber)
        {
        }

        public void EnqueueMouseMove()
        {
        }

        public void EnqueueMouseWheel(int delta)
        {
        }

        public void RestoreOutputCheckpoint(int checkpoint)
        {
            if (checkpoint != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint must be 0.");
            }
        }
    }

    private sealed class FixedScreenModeSizeProvider : IScreenModeSizeProvider
    {
        private readonly (int Width, int Height) _screen10Size;

        public FixedScreenModeSizeProvider((int Width, int Height) screen10Size)
        {
            _screen10Size = screen10Size;
        }

        public (int Width, int Height) GetSize(int mode) => mode switch
        {
            0 => (640, 300),
            1 => (320, 200),
            2 => (640, 480),
            3 => (800, 600),
            4 => (1024, 768),
            5 => (1280, 720),
            6 => (1366, 768),
            7 => (1600, 900),
            8 => (1920, 1080),
            9 => (2560, 1440),
            10 => _screen10Size,
            _ => (0, 0)
        };
    }
}

