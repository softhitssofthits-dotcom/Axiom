using Axiom.Bytecode;
using Axiom.Parsing;
using Axiom.Semantics;
using System.Linq;

namespace Axiom.Runtime.Tests;

public sealed class BytecodeVmTests
{
    [Fact]
    public void Execute_CompiledScalarProgramWithRoutineAndBuiltin()
    {
        const string text = """
FUNCTION Increment(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT Increment(41)
PRINT LEN("abcd")
PRINT "A", "B"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "4", "A B"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPrintWithTrailingCommaKeepsLoopOutputOnOneLine()
    {
        const string text = """
FOR i = 1 TO 10
    PRINT i,
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1 2 3 4 5 6 7 8 9 10 "], host.Output);
    }

    [Fact]
    public void Execute_StepOverAfterTrailingCommaExposesPendingOutputWithoutFlushing()
    {
        const string text = """
FOR i = 1 TO 2
    PRINT i,
NEXT
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Stopped);
        Assert.Empty(host.Output);
        Assert.Equal(string.Empty, result.PendingOutput);

        var stepped = result.StopState!.StepOver();

        Assert.True(stepped.Stopped);
        Assert.Empty(host.Output);
        Assert.Equal("1 ", stepped.PendingOutput);
    }

    [Fact]
    public void Execute_CompiledPrintWithTrailingSemicolonKeepsLoopOutputOnOneLine()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i;
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["123"], host.Output);
    }

    [Fact]
    public void Execute_CompiledControlFlowProgramUsesGlobalsAndJumps()
    {
        const string text = """
DIM counter AS INTEGER = 1

IF counter < 2 THEN
    counter = counter + 10
ELSE
    counter = 0
END IF

PRINT counter
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["11"], host.Output);
    }

    [Fact]
    public void Execute_CompiledProgramReportsDivisionByZeroWithSourceLocation()
    {
        const string text = "PRINT 1 / 0";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3001", diagnostic.Code);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_ReportsInstructionSnapshotsWithSourceLocations()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();

        var (_, host, result) = ExecuteProgram(text, instructionObserver: snapshots.Add);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2"], host.Output);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.RoutineId == 0
            && snapshot.RoutineName == "<global>"
            && snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 1
            && snapshot.CallDepth == 1);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.RoutineId == 0
            && snapshot.RoutineName == "<global>"
            && snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2
            && snapshot.CallDepth == 1);
    }

    [Fact]
    public void Execute_StopsBeforeRequestedInstruction()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();

        var (_, host, result) = ExecuteProgram(
            text,
            instructionObserver: snapshots.Add,
            shouldStopBeforeInstruction: static snapshot =>
                snapshot.Opcode == Opcode.PushConst
                && snapshot.SourceLocation.Line == 2);

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(Opcode.PushConst, result.StoppedAt!.Opcode);
        Assert.Equal(2, result.StoppedAt.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);
        Assert.Contains(snapshots, static snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);
    }

    [Fact]
    public void Execute_StopsAtSourceLineBreakpoint()
    {
        const string text = """
PRINT 1
PRINT 2
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_StopsAtSourcePositionBreakpoint()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var target = snapshots.Single(snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromPositions((target.SourceLocation.Line, target.SourceLocation.Column)));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.NotNull(result.StoppedAt);
        Assert.Same(result.StopState.Snapshot, result.StoppedAt);
        Assert.Equal(target.SourceLocation.Line, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(target.SourceLocation.Column, result.StoppedAt.SourceLocation.Column);
        Assert.Equal(["1"], host.Output);
    }

    [Fact]
    public void Execute_IgnoresSourcePositionBreakpointWithDifferentColumn()
    {
        const string text = """
PRINT 1
PRINT 2
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var target = snapshots.Single(snapshot =>
            snapshot.Opcode == Opcode.PushConst
            && snapshot.SourceLocation.Line == 2);

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromPositions((target.SourceLocation.Line, target.SourceLocation.Column + 1)));

        Assert.True(result.Succeeded);
        Assert.False(result.Stopped);
        Assert.Null(result.StopState);
        Assert.Null(result.StoppedAt);
        Assert.Equal(["1", "2"], host.Output);
    }

    [Fact]
    public void Execute_ResumesStoppedRunFromSameInstruction()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            shouldStopBeforeInstruction: static snapshot =>
                snapshot.Opcode == Opcode.PushConst
                && snapshot.SourceLocation.Line == 2);

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(["1"], host.Output);

        var resumed = result.StopState.Resume();

        Assert.True(resumed.Succeeded);
        Assert.False(resumed.Stopped);
        Assert.Null(resumed.StopState);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_StepOverFromSourceLineBreakpointStopsAtNextSourceLine()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var stepped = result.StopState!.StepOver();

        Assert.True(stepped.Succeeded);
        Assert.True(stepped.Stopped);
        Assert.NotNull(stepped.StopState);
        Assert.Equal(3, stepped.StoppedAt!.SourceLocation.Line);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2"], host.Output);
    }

    [Fact]
    public void Execute_StepIntoFromCallStopsInsideRoutine()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(5));

        Assert.True(result.Stopped);
        Assert.Equal(5, result.StoppedAt!.SourceLocation.Line);

        var stepped = result.StopState!.StepInto();

        Assert.True(stepped.Stopped);
        Assert.Equal(2, stepped.StoppedAt!.SourceLocation.Line);
        Assert.Equal(2, stepped.StoppedAt.CallDepth);
        Assert.True(stepped.StopState!.CanStepBack);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackAfterStepIntoRoutineReturnsToCallSite()
    {
        const string text = """
SUB PrintBanner(title AS STRING)
    PRINT "sub: begin"
    PRINT title
END SUB

PrintBanner("debug sample")
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(6));

        var insideRoutine = result.StopState!.StepInto();

        Assert.True(insideRoutine.Stopped);
        Assert.Equal(2, insideRoutine.StoppedAt!.SourceLocation.Line);
        Assert.True(insideRoutine.StopState!.CanStepBack);

        var backed = insideRoutine.StopState.StepBack();

        Assert.True(backed.Stopped);
        Assert.Equal(6, backed.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackInsideRoutineAfterPrintedOutputRestoresOutputCheckpoint()
    {
        const string text = """
SUB PrintBanner(title AS STRING)
    PRINT "sub: begin"
    PRINT title
    PRINT "sub: end"
END SUB

PrintBanner("debug sample")
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(7));

        var insideRoutine = result.StopState!.StepInto();
        var afterFirstPrint = insideRoutine.StopState!.StepOver();
        var afterSecondPrint = afterFirstPrint.StopState!.StepOver();

        Assert.True(afterSecondPrint.Stopped);
        Assert.Equal(4, afterSecondPrint.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["sub: begin", "debug sample"], host.Output);
        Assert.True(afterSecondPrint.StopState!.CanStepBack);

        var backed = afterSecondPrint.StopState.StepBack();

        Assert.True(backed.Stopped);
        Assert.Equal(3, backed.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["sub: begin"], host.Output);
    }

    [Fact]
    public void Execute_StepOutFromRoutineStopsAfterReturningToCaller()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

PRINT AddOne(4)
PRINT "done"
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(5));
        var insideRoutine = result.StopState!.StepInto();

        Assert.True(insideRoutine.Stopped);
        Assert.Equal(2, insideRoutine.StoppedAt!.CallDepth);

        var steppedOut = insideRoutine.StopState!.StepOut();

        Assert.True(steppedOut.Stopped);
        Assert.Equal(1, steppedOut.StoppedAt!.CallDepth);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackFromBreakpointRestoresPreviousInstruction()
    {
        const string text = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanStepBack);
        Assert.Equal(3, result.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var backed = result.StopState.StepBack();

        Assert.True(backed.Succeeded);
        Assert.True(backed.Stopped);
        Assert.NotNull(backed.StopState);
        Assert.Equal(2, backed.StoppedAt!.SourceLocation.Line);
        Assert.False(result.StopState.CanResume);
        Assert.False(result.StopState.CanStepBack);
        Assert.Empty(host.Output);

        var stoppedAgain = backed.StopState!.Resume();

        Assert.True(stoppedAgain.Succeeded);
        Assert.True(stoppedAgain.Stopped);
        Assert.Equal(3, stoppedAgain.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var completed = stoppedAgain.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_StepBackKeepsEarlierSnapshotAvailable()
    {
        const string text = """
DIM value AS INTEGER = 1
value = value + 1
PRINT value
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        var firstBack = result.StopState!.StepBack();
        var secondBack = firstBack.StopState!.StepBack();

        Assert.True(secondBack.Succeeded);
        Assert.True(secondBack.Stopped);
        Assert.NotNull(secondBack.StopState);
        Assert.Equal(2, secondBack.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_StepBackUnavailableAfterInput()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
PRINT value
""";
        var host = new TestRuntimeHost(inputs: ["7"]);

        var (_, _, result) = ExecuteProgram(
            text,
            host,
            breakpoints: BytecodeBreakpointSet.FromLines(3));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.False(result.StopState!.CanStepBack);
        Assert.Equal(3, result.StoppedAt!.SourceLocation.Line);
        var request = Assert.Single(host.InputRequests);
        Assert.Null(request.PromptText);
        Assert.Equal(1, request.ValueCount);
        Assert.Empty(host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourceLineBreakpointSkipsRestOfSameLine()
    {
        const string text = """
PRINT 1
PRINT 2
PRINT 3
""";

        var (_, host, result) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(result.Succeeded);
        Assert.True(result.Stopped);
        Assert.NotNull(result.StopState);
        Assert.True(result.StopState!.CanResume);
        Assert.Equal(2, result.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var resumed = result.StopState.Resume();

        Assert.True(resumed.Succeeded);
        Assert.False(resumed.Stopped);
        Assert.Null(resumed.StopState);
        Assert.False(result.StopState.CanResume);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourceLineBreakpointStopsAgainOnNextLoopIteration()
    {
        const string text = """
FOR i = 1 TO 2
    PRINT i
NEXT
PRINT 99
""";

        var (_, host, firstStop) = ExecuteProgram(
            text,
            breakpoints: BytecodeBreakpointSet.FromLines(2));

        Assert.True(firstStop.Succeeded);
        Assert.True(firstStop.Stopped);
        Assert.NotNull(firstStop.StopState);
        Assert.Equal(2, firstStop.StoppedAt!.SourceLocation.Line);
        Assert.Empty(host.Output);

        var secondStop = firstStop.StopState!.Resume();

        Assert.True(secondStop.Succeeded);
        Assert.True(secondStop.Stopped);
        Assert.NotNull(secondStop.StopState);
        Assert.False(firstStop.StopState.CanResume);
        Assert.Equal(2, secondStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(["1"], host.Output);

        var completed = secondStop.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Null(completed.StopState);
        Assert.False(secondStop.StopState.CanResume);
        Assert.Equal(["1", "2", "99"], host.Output);
    }

    [Fact]
    public void Execute_ResumeFromSourcePositionBreakpointCanStopAgainOnSameLine()
    {
        const string text = """
PRINT 0
PRINT 2 + 3
PRINT 9
""";
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        ExecuteProgram(text, instructionObserver: snapshots.Add);

        var targets = snapshots
            .Where(static snapshot => snapshot.SourceLocation.Line == 2)
            .GroupBy(static snapshot => (snapshot.SourceLocation.Line, snapshot.SourceLocation.Column))
            .Select(static group => group.First())
            .Take(2)
            .ToArray();

        Assert.True(
            targets.Length >= 2,
            string.Join(", ", snapshots
                .Where(static snapshot => snapshot.SourceLocation.Line == 2)
                .Select(static snapshot => $"{snapshot.Opcode}@{snapshot.SourceLocation.Line}:{snapshot.SourceLocation.Column}")));

        var breakpoints = BytecodeBreakpointSet.FromPositions(targets
            .Select(static snapshot => (snapshot.SourceLocation.Line, snapshot.SourceLocation.Column))
            .ToArray());

        var (_, host, firstStop) = ExecuteProgram(text, breakpoints: breakpoints);

        Assert.True(firstStop.Succeeded);
        Assert.True(firstStop.Stopped);
        Assert.NotNull(firstStop.StopState);
        Assert.Equal(targets[0].SourceLocation.Line, firstStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(targets[0].SourceLocation.Column, firstStop.StoppedAt.SourceLocation.Column);
        Assert.Equal(["0"], host.Output);

        var secondStop = firstStop.StopState!.Resume();

        Assert.True(secondStop.Succeeded);
        Assert.True(secondStop.Stopped);
        Assert.NotNull(secondStop.StopState);
        Assert.False(firstStop.StopState.CanResume);
        Assert.Equal(targets[1].SourceLocation.Line, secondStop.StoppedAt!.SourceLocation.Line);
        Assert.Equal(targets[1].SourceLocation.Column, secondStop.StoppedAt.SourceLocation.Column);
        Assert.Equal(["0"], host.Output);

        var completed = secondStop.StopState!.Resume();

        Assert.True(completed.Succeeded);
        Assert.False(completed.Stopped);
        Assert.Null(completed.StopState);
        Assert.False(secondStop.StopState.CanResume);
        Assert.Equal(["0", "5", "9"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRemainingArithmeticOperators()
    {
        const string text = """
DIM mask AS INTEGER = 16
DIM wide AS LONG = 64
PRINT 7 \ 2
PRINT 2 ^ 3
PRINT 1 << 3
PRINT mask >> 2
PRINT wide >> 3
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["3", "8", "8", "4", "8"], host.Output);
    }

    [Fact]
    public void Execute_CompiledIndirectFunctionValueCall()
    {
        const string text = """
FUNCTION AddOne(value AS INTEGER) AS INTEGER
    RETURN value + 1
END FUNCTION

DIM op AS FUNCTION(INTEGER) AS INTEGER
op = AddOne
PRINT op(4)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledIntegralDivisionReportsDivisionByZero()
    {
        const string text = "PRINT 7 \\ 0";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3001", diagnostic.Code);
        Assert.Equal(1, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_CompiledForLoopProgram()
    {
        const string text = """
FOR i = 1 TO 3
    PRINT i
NEXT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "2", "3"], host.Output);
    }

    [Fact]
    public void Execute_CompiledForLoopReportsStepZero()
    {
        const string text = """
FOR i = 1 TO 3 STEP 0
    PRINT i
NEXT
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3004", diagnostic.Code);
        Assert.Contains("STEP 0", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, diagnostic.Location.Line);
        Assert.Equal(21, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_CompiledLocalFunctionWithoutCaptureProgram()
    {
        const string text = """
PRINT Outer(10)

FUNCTION Outer(base AS INTEGER) AS INTEGER
    RETURN AddTwo(base)

    FUNCTION AddTwo(value AS INTEGER) AS INTEGER
        RETURN value + 2
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["12"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLocalSubWithoutCaptureProgram()
    {
        const string text = """
DIM total AS INTEGER = 0

Demo()
PRINT total

SUB Demo()
    AddOne()
    AddOne()

    SUB AddOne()
        total = total + 1
    END SUB
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFixedStringProgram()
    {
        const string text = """
DIM code AS STRING * 4

PRINT "[" + code + "]"

code = "AB"
PRINT "[" + code + "]"

code = "Alphabet"
PRINT "[" + code + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[    ]", "[AB  ]", "[Alph]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexArrayBuiltins()
    {
        const string text = """
DIM matches() AS STRING
DIM extracted() AS STRING
DIM parts() AS STRING
DIM emptyParts() AS STRING

matches = REGEXMATCHES("A12 b34 C56", "[a-z]\d+", "i")
extracted = REGEXEXTRACTALL("A12 b34 C56", "([a-z])(\d+)", 2, "i")
parts = REGEXSPLIT("A12,b34;C56", "[,;]")
emptyParts = REGEXSPLIT("", ",")

PRINT ARRAYLENGTH(matches)
PRINT matches(1)
PRINT matches(3)
PRINT ARRAYLENGTH(extracted)
PRINT extracted(2)
PRINT ARRAYLENGTH(parts)
PRINT parts(2)
PRINT ARRAYLENGTH(emptyParts)
PRINT LBOUND(emptyParts)
PRINT UBOUND(emptyParts)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["3", "A12", "C56", "3", "34", "3", "b34", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexArrayBuiltinReportsInvalidMode()
    {
        const string text = """
DIM matches() AS STRING
matches = REGEXMATCHES("abc", "a", "q")
PRINT ARRAYLENGTH(matches)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXMATCHES", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("modes", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledRegexScalarBuiltins()
    {
        const string text = """
PRINT REGEXISMATCH("A12", "\d+")
PRINT REGEXISMATCH("A12", "^a\d+", "i")
PRINT REGEXMATCH("Order 42 and 100", "\d+")
PRINT "[" + REGEXMATCH("Order", "\d+") + "]"
PRINT REGEXEXTRACT("A12", "([A-Z])(\d+)", 2)
PRINT "[" + REGEXEXTRACT("A12", "([A-Z])(\d+)", 3) + "]"
PRINT REGEXREPLACE("one 12 two 34", "\d+", "#", 1)
PRINT REGEXREPLACE("a12 b34", "[a-z]\d+", "#", "i", 1)
PRINT REGEXCOUNT("one 12 two 34", "\d+")
PRINT REGEXCOUNT("A12 b34", "[a-z]\d+", "i")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["TRUE", "TRUE", "42", "[]", "12", "[]", "one # two 34", "# b34", "2", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledRegexScalarBuiltinReportsInvalidArgument()
    {
        const string text = """
PRINT REGEXREPLACE("abc", "a", "x", -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REGEXREPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledStringArrayBuiltins()
    {
        const string text = """
DIM parts() AS STRING
DIM emptyParts() AS STRING
DIM missing() AS STRING

parts = SPLIT("red,green,,blue", ",")
emptyParts = SPLIT("", ",")

PRINT ARRAYISALLOCATED(parts)
PRINT LBOUND(parts)
PRINT UBOUND(parts)
PRINT ARRAYLENGTH(parts)
PRINT parts(1)
PRINT parts(3)
PRINT parts(4)
PRINT JOIN(parts, "/")
PRINT ARRAYLENGTH(emptyParts)
PRINT LBOUND(emptyParts)
PRINT UBOUND(emptyParts)
PRINT "[" + JOIN(emptyParts) + "]"
PRINT "[" + JOIN(missing) + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["TRUE", "1", "4", "4", "red", "", "blue", "red/green//blue", "0", "1", "0", "[]", "[]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringArrayBuiltinReportsEmptySeparator()
    {
        const string text = """
DIM parts() AS STRING
parts = SPLIT("abc", "")
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("SPLIT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("separator", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPrintUsingProgram()
    {
        const string text = """
PRINT USING "##.##"; 3.14159
PRINT USING "! & \    \"; "Hello"; "World"; "xy"
PRINT USING "##"; 12;
PRINT "tail"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal([" 3.14", "H World xy    ", "12tail"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPrintUsingReportsFieldTypeMismatch()
    {
        const string text = """
PRINT USING "!"; 1
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("PRINT USING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("STRING", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledInputProgram()
    {
        const string text = """
DIM firstName AS STRING
DIM age AS INTEGER
INPUT "Pair"; firstName, age
PRINT firstName; age + 1
""";

        var (_, host, result) = ExecuteProgram(text, new TestRuntimeHost(inputs: ["Ada, 41"]));

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["Ada42"], host.Output);
        var request = Assert.Single(host.InputRequests);
        Assert.Equal("Pair", request.PromptText);
        Assert.Equal(2, request.ValueCount);
    }

    [Fact]
    public void Execute_CompiledInputReportsConversionFailure()
    {
        const string text = """
DIM value AS INTEGER
INPUT value
""";

        var (_, _, result) = ExecuteProgram(text, new TestRuntimeHost(inputs: ["oops"]));

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3003", diagnostic.Code);
        Assert.Contains("INTEGER", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
    }

    [Fact]
    public void Execute_CompiledArrayProgram()
    {
        const string text = """
DIM values(1 TO 2) AS INTEGER
values(1) = 10
values(2) = 20

PRINT values(1)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)
PRINT LBOUND(values)
PRINT UBOUND(values)

REDIM PRESERVE values(2 TO 3)
PRINT values(2)
PRINT values(3)

ERASE values
PRINT ARRAYISALLOCATED(values)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "TRUE", "2", "1", "2", "20", "0", "FALSE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltins()
    {
        const string text = """
DIM source(5 TO 7) AS INTEGER
DIM clone() AS INTEGER
DIM slice() AS INTEGER
DIM merged() AS INTEGER
DIM reversed() AS INTEGER
DIM taken() AS INTEGER
DIM skipped() AS INTEGER
DIM uniqueValues() AS INTEGER
DIM sortedValues() AS INTEGER

source(5) = 30
source(6) = 10
source(7) = 20

clone = ARRAYCLONE(source)
clone(5) = 99
slice = ARRAYSLICE(source, 6, 2)
merged = ARRAYCONCAT(slice, slice)
reversed = ARRAYTOREVERSED(source)
taken = ARRAYTAKE(source, 2)
skipped = ARRAYSKIP(source, 1)
uniqueValues = ARRAYDISTINCT(merged)
sortedValues = ARRAYTOSORTED(source)

PRINT LBOUND(clone)
PRINT UBOUND(clone)
PRINT source(5)
PRINT clone(5)
PRINT slice(1)
PRINT slice(2)
PRINT ARRAYLENGTH(merged)
PRINT merged(3)
PRINT reversed(1)
PRINT taken(2)
PRINT skipped(1)
PRINT ARRAYLENGTH(uniqueValues)
PRINT uniqueValues(1)
PRINT uniqueValues(2)
PRINT LBOUND(sortedValues)
PRINT UBOUND(sortedValues)
PRINT sortedValues(5)
PRINT sortedValues(7)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["5", "7", "30", "99", "10", "20", "4", "10", "20", "10", "10", "2", "10", "20", "5", "7", "10", "30"],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayClonePreservesUnallocatedAndEmptyStates()
    {
        const string text = """
DIM pending() AS STRING
DIM copy() AS STRING

copy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(copy)

REDIM pending(1 TO 0)
copy = ARRAYCLONE(pending)
PRINT ARRAYISALLOCATED(copy)
PRINT ARRAYLENGTH(copy)
PRINT LBOUND(copy)
PRINT UBOUND(copy)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "TRUE", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltinsReportArgumentErrors()
    {
        const string text = """
DIM source(1 TO 1) AS INTEGER
PRINT ARRAYTAKE(source, -1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYTAKE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledPureArrayBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM source() AS INTEGER
DIM part() AS INTEGER
part = ARRAYSLICE(source, 1, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltins()
    {
        const string text = """
DIM values(5 TO 6) AS INTEGER
DIM removed AS INTEGER
DIM popped AS INTEGER

ARRAYFILL(values, 42)
PRINT values(5)
PRINT values(6)

values(5) = 3
values(6) = 1
ARRAYPUSH(values, 2)
ARRAYINSERT(values, 6, 4)
removed = ARRAYREMOVE(values, 7)
popped = ARRAYPOP(values)

PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT removed
PRINT popped
PRINT values(5)
PRINT values(6)

ARRAYREVERSE(values)
PRINT values(5)
PRINT values(6)

ARRAYSORT(values)
PRINT values(5)
PRINT values(6)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42", "42", "5", "6", "1", "2", "3", "4", "4", "3", "3", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsAllowCanonicalEmptyArray()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM popped AS INTEGER

ARRAYFILL(values, 11)
ARRAYREVERSE(values)
ARRAYSORT(values)
PRINT ARRAYISALLOCATED(values)
PRINT ARRAYLENGTH(values)

ARRAYPUSH(values, 77)
PRINT LBOUND(values)
PRINT UBOUND(values)
PRINT values(1)

popped = ARRAYPOP(values)
PRINT popped
PRINT ARRAYLENGTH(values)

ARRAYINSERT(values, 1, 55)
PRINT values(1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "0", "1", "1", "77", "77", "0", "55"], host.Output);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
ARRAYFILL(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("не выделен", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportEmptyPop()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
PRINT ARRAYPOP(values)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYPOP", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("array", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledMutatingArrayBuiltinsReportInvalidInsertIndex()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
ARRAYINSERT(values, 2, 10)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("ARRAYINSERT", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("index", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltins()
    {
        const string text = """
DIM values(5 TO 9) AS INTEGER
DIM matches() AS INTEGER
DIM sorted(5 TO 9) AS INTEGER

values(5) = 10
values(6) = 20
values(7) = 10
values(8) = 30
values(9) = 20

sorted(5) = 10
sorted(6) = 20
sorted(7) = 20
sorted(8) = 30
sorted(9) = 40

matches = ARRAYFINDALL(values, 20)

PRINT ARRAYCONTAINS(values, 20)
PRINT ARRAYINDEXOF(values, 10)
PRINT ARRAYLASTINDEXOF(values, 20)
PRINT ARRAYCOUNT(values, 20)
PRINT ARRAYINDEXOF(values, 99)
PRINT LBOUND(matches)
PRINT UBOUND(matches)
PRINT matches(1)
PRINT matches(2)
PRINT ARRAYBINARYSEARCH(sorted, 20)
PRINT ARRAYBINARYSEARCH(sorted, 25)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "5", "9", "2", "4", "1", "2", "6", "9", "6", "4"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltinsTreatCanonicalEmptyArrayAsNotFound()
    {
        const string text = """
DIM values(1 TO 0) AS INTEGER
DIM matches() AS INTEGER

matches = ARRAYFINDALL(values, 1)

PRINT ARRAYCONTAINS(values, 1)
PRINT ARRAYCOUNT(values, 1)
PRINT ARRAYINDEXOF(values, 1)
PRINT ARRAYLASTINDEXOF(values, 1)
PRINT ARRAYBINARYSEARCH(values, 1)
PRINT ARRAYLENGTH(matches)
PRINT LBOUND(matches)
PRINT UBOUND(matches)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["FALSE", "0", "0", "0", "0", "0", "1", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArraySearchBuiltinsReportUnallocatedSource()
    {
        const string text = """
DIM values() AS INTEGER
PRINT ARRAYINDEXOF(values, 1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsUseFunctionValuesAndClosures()
    {
        const string text = """
SUB RunCallbacks()
DIM values(1 TO 4) AS INTEGER
DIM filtered() AS INTEGER
DIM mapped() AS STRING
DIM threshold AS INTEGER = 2
DIM prefix AS STRING = "N"
DIM bonus AS INTEGER = 1

values(1) = 1
values(2) = 2
values(3) = 3
values(4) = 4

PRINT ARRAYEVERY(values, IsPositive)
PRINT ARRAYSOME(values, IsLarge)

filtered = ARRAYFILTER(values, IsLarge)
mapped = ARRAYMAP(filtered, FormatValue)

PRINT ARRAYLENGTH(filtered)
PRINT mapped(1)
PRINT mapped(2)
PRINT ARRAYREDUCE(values, 10, AddWithBonus)

FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    RETURN value > 0
END FUNCTION

FUNCTION IsLarge(value AS INTEGER) AS BOOLEAN
    RETURN value > threshold
END FUNCTION

FUNCTION FormatValue(value AS INTEGER) AS STRING
    RETURN prefix + STR(value)
END FUNCTION

FUNCTION AddWithBonus(accumulator AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN accumulator + value + bonus
END FUNCTION
END SUB

RunCallbacks()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "2", "N3", "N4", "24"], host.Output);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsReportEmptyFunctionValue()
    {
        const string text = """
DIM values(1 TO 1) AS INTEGER
DIM predicate AS FUNCTION(INTEGER) AS BOOLEAN
PRINT ARRAYEVERY(values, predicate)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3009", diagnostic.Code);
    }

    [Fact]
    public void Execute_CompiledArrayCallbackBuiltinsUseBuiltinFunctionValues()
    {
        const string text = """
DIM names(1 TO 3) AS STRING
DIM upper() AS STRING
DIM lengths() AS INTEGER
DIM dateTexts(1 TO 3) AS STRING
DIM validDates() AS STRING

names(1) = "ada"
names(2) = "Grace"
names(3) = "linus"

dateTexts(1) = "2026-04-24"
dateTexts(2) = "not-a-date"
dateTexts(3) = "2026-05-01"

upper = ARRAYMAP(names, UCASE)
lengths = ARRAYMAP(names, LEN)
validDates = ARRAYFILTER(dateTexts, ISDATE)

PRINT upper(1)
PRINT upper(3)
PRINT lengths(2)
PRINT ARRAYLENGTH(validDates)
PRINT validDates(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["ADA", "LINUS", "5", "2", "2026-05-01"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeFieldsNestedRecordsAndFixedStrings()
    {
        const string text = """
TYPE Address
    City AS STRING
END TYPE

TYPE Employee
    Name AS STRING * 5
    Age AS INTEGER
    Home AS Address
END TYPE

DIM first AS Employee
DIM second AS Employee

PRINT "[" + first.Name + "]"
PRINT first.Age
first.Name = "Ada"
first.Age = 36
first.Home.City = "London"
second = first
first.Name = "Grace"
PRINT "[" + second.Name + "]"
PRINT second.Age
PRINT second.Home.City
PRINT "[" + first.Name + "]"
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[     ]", "0", "[Ada  ]", "36", "London", "[Grace]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeArrayAndArrayFields()
    {
        const string text = """
TYPE Person
    Name AS STRING
    Scores() AS INTEGER
    Flags(1 TO 2) AS INTEGER
END TYPE

DIM people(1 TO 1) AS Person
DIM copy AS Person

people(1).Name = "Ada"
REDIM people(1).Scores(1 TO 2)
people(1).Scores(1) = 10
people(1).Scores(2) = 20
people(1).Flags(2) = 7
copy = people(1)
people(1).Name = "Grace"
people(1).Scores(1) = 99
people(1).Flags(2) = 42
PRINT copy.Name
PRINT copy.Scores(1)
PRINT copy.Scores(2)
PRINT copy.Flags(2)
PRINT people(1).Name
PRINT people(1).Scores(1)
PRINT people(1).Flags(2)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Ada", "99", "20", "7", "Grace", "99", "42"], host.Output);
    }

    [Fact]
    public void Execute_CompiledTypeLocalDefaultCanBeCaptured()
    {
        const string text = """
TYPE Person
    Name AS STRING
END TYPE

SUB ShowDefault()
    DIM user AS Person
    PRINT "[" + ReadName() + "]"

    FUNCTION ReadName() AS STRING
        RETURN user.Name
    END FUNCTION
END SUB

ShowDefault()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["[]"], host.Output);
    }

    [Fact]
    public void Execute_CompiledUnallocatedArrayAccessReportsRuntimeState()
    {
        const string text = """
DIM values() AS INTEGER
PRINT values(1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3005", diagnostic.Code);
        Assert.Contains("values", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, diagnostic.Location.Line);
    }

    [Fact]
    public void Execute_CompiledStringSliceAndTrimBuiltins()
    {
        const string text = """
PRINT LEFT("Hello", 3)
PRINT RIGHT("Hello", 3)
PRINT MID("Hello", 2, 4)
PRINT TRIM("  hello  ")
PRINT LTRIM("xyxyhello", "xy")
PRINT RTRIM("helloxyxy", "xy")
PRINT REVERSE("abcd")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["Hel", "llo", "ello", "hello", "hello", "hello", "dcba"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringSearchBuiltins()
    {
        const string text = """
PRINT CONTAINS("Hello", "ell")
PRINT STARTSWITH("Hello", "He")
PRINT ENDSWITH("Hello", "lo")
PRINT REPLACE("one two two", "two", "three")
PRINT INSTR("banana", "na")
PRINT INSTR(4, "banana", "na")
PRINT INSTRREV("banana", "na")
PRINT INSTRREV(4, "banana", "na")
PRINT SUBSTRCOUNT("banana", "na")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE", "TRUE", "one three three", "3", "5", "5", "3", "2"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringFillAndEditBuiltins()
    {
        const string text = """
PRINT REPEAT("ha", 3)
PRINT "[" + SPACE(4) + "]"
PRINT STRING(5, "ab")
PRINT "[" + PADLEFT("7", 3) + "]"
PRINT PADRIGHT("7", 3, ".")
PRINT "[" + CENTER("A", 4, ".") + "]"
PRINT INSERT("abcd", 3, "-")
PRINT REMOVE("abcdef", 2, 3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["hahaha", "[    ]", "aaaaa", "[  7]", "7..", "[.A..]", "ab-cd", "aef"], host.Output);
    }

    [Fact]
    public void Execute_CompiledStringCharBuiltins()
    {
        const string text = """
PRINT ASC("A")
PRINT CHR(65)
PRINT ASC(CHR(1071))
PRINT ASC(CHR(128512))
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["65", "A", "1071", "128512"], host.Output);
    }

    [Fact]
    public void Execute_CompiledNumericConversionAndRoundingBuiltins()
    {
        const string text = """
PRINT INT(3.8)
PRINT INT(-3.2)
PRINT FLOOR(3.8)
PRINT CEIL(3.2)
PRINT ROUND(2.5)
PRINT ROUND(-2.5)
PRINT FIX(-3.8)
PRINT SETSIGN(5, -1)
PRINT SETSIGN(-5, 1)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["3", "-4", "3", "4", "3", "-3", "-3", "-5", "5"], host.Output);
    }

    [Fact]
    public void Execute_CompiledAutomaticNumericConversions()
    {
        const string text = """
DIM seed AS INTEGER = 1
DIM amount AS DECIMAL = seed
DIM half AS DECIMAL = 0.5
DIM wide AS LONG = seed
DIM angle AS INTEGER = 0
PRINT amount + half
PRINT wide + 7
PRINT SIN(angle)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1.5", "8", "0"], host.Output);
    }

    [Fact]
    public void Execute_CompiledNumericMathBuiltins()
    {
        const string text = """
PRINT SIN(0)
PRINT COS(0)
PRINT TAN(0)
PRINT ATN(0)
PRINT ATN2(0, 1)
PRINT SQR(9)
PRINT CBRT(27)
PRINT EXP(0)
PRINT LOG(1)
PRINT LOG10(100)
PRINT LOG2(8)
PRINT POW(2, 3)
PRINT ASIN(0)
PRINT ACOS(1)
PRINT ASINH(0)
PRINT ACOSH(1)
PRINT ATANH(0)
PRINT SINH(0)
PRINT COSH(0)
PRINT TANH(0)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            ["0", "1", "0", "0", "0", "3", "3", "1", "0", "2", "3", "8", "0", "0", "0", "0", "0", "0", "1", "0"],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledRndReturnsValueInHalfOpenRange()
    {
        const string text = """
DIM value AS DOUBLE = RND()
PRINT value >= 0
PRINT value < 1
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["TRUE", "TRUE"], host.Output);
    }

    [Fact]
    public void Execute_CompiledCurrentDateTimeBuiltinsUseHostClock()
    {
        const string text = """
PRINT DATE()
PRINT TIME()
PRINT NOW()
PRINT UTCDATE()
PRINT UTCTIME()
PRINT UTCNOW()
PRINT TIMER()
PRINT DATE("UTC")
PRINT TIME("UTC")
PRINT NOW("UTC")
PRINT UTCOFFSET("2026-04-20 14:05:06")
PRINT TOUTC("2026-04-20 14:05:06")
PRINT FROMUTC("2026-04-20 11:05:06")
""";

        var host = new TestRuntimeHost(
            new DateTimeOffset(2026, 04, 20, 14, 05, 06, TimeSpan.FromHours(3)),
            CreateTestFixedTimeZone("Axiom Runtime Tests East", 3));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026-04-20 14:05:06",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "50706",
                "2026-04-20",
                "11:05:06",
                "2026-04-20 11:05:06",
                "180",
                "2026-04-20 11:05:06",
                "2026-04-20 14:05:06"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeTransformBuiltins()
    {
        const string text = """
PRINT DATESERIAL(2026, 4, 20)
PRINT TIMESERIAL(14, 5, 6)
PRINT YEAR("2026-04-20")
PRINT MONTH("2026-04-20 14:05:06")
PRINT DAY("2026-04-20")
PRINT HOUR("14:05:06")
PRINT MINUTE("2026-04-20 14:05:06")
PRINT SECOND("14:05:06")
PRINT WEEKDAY("2026-04-20")
PRINT MONTHNAME(4)
PRINT WEEKDAYNAME(7)
PRINT DATEADD("m", 1, "2026-01-31")
PRINT DATEADD("s", 10, "23:59:55")
PRINT DATEDIFF("d", "2026-04-20 23:59:59", "2026-04-21 00:00:00")
PRINT DATEPART("y", "2026-04-21")
PRINT DATEVALUE("2026-04-20 14:05:06")
PRINT TIMEVALUE("2026-04-20 14:05:06")
PRINT ISDATE("2026-04-20 14:05:06")
PRINT ISDATE("20.04.2026")
PRINT ISLEAPYEAR(2024)
PRINT DAYSINMONTH(2024, 2)
PRINT STARTOFMONTH("2026-04-20 14:05:06")
PRINT ENDOFMONTH("2024-02-11")
PRINT STARTOFYEAR("2026-04-20")
PRINT ENDOFYEAR("2026-04-20 14:05:06")
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-20",
                "14:05:06",
                "2026",
                "4",
                "20",
                "14",
                "5",
                "6",
                "1",
                "April",
                "Sunday",
                "2026-02-28",
                "00:00:05",
                "1",
                "111",
                "2026-04-20",
                "14:05:06",
                "TRUE",
                "FALSE",
                "TRUE",
                "29",
                "2026-04-01",
                "2024-02-29",
                "2026-01-01",
                "2026-12-31"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeNamedZoneBuiltinsUseRequestedZone()
    {
        const string text = """
PRINT DATE("Axiom Runtime Tests West")
PRINT TIME("Axiom Runtime Tests West")
PRINT NOW("Axiom Runtime Tests West")
PRINT UTCOFFSET(NOW("Axiom Runtime Tests West"), "Axiom Runtime Tests West")
PRINT TOUTC("2026-04-19 16:30:45", "Axiom Runtime Tests West")
PRINT FROMUTC("2026-04-19 21:30:45", "Axiom Runtime Tests West")
""";

        var westTimeZone = CreateTestFixedTimeZone("Axiom Runtime Tests West", -5);
        var host = new TestRuntimeHost(
            new DateTimeOffset(2026, 04, 20, 00, 30, 45, TimeSpan.FromHours(3)),
            CreateTestFixedTimeZone("Axiom Runtime Tests East", 3),
            CreateNamedTimeZones(westTimeZone));

        var (_, _, result) = ExecuteProgram(text, host);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "2026-04-19",
                "16:30:45",
                "2026-04-19 16:30:45",
                "-300",
                "2026-04-19 21:30:45",
                "2026-04-19 16:30:45"
            ],
            host.Output);
    }

    [Fact]
    public void Execute_CompiledDateTimeBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT DATESERIAL(2025, 2, 29)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("DATESERIAL", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("day", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledNumericBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT SQR(-1)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("SQR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("value", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledStringBuiltinsReportArgumentErrors()
    {
        const string text = """
PRINT REPLACE("abc", "", "x")
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("REPLACE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("oldText", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledChrReportsInvalidCode()
    {
        const string text = """
PRINT CHR(55296)
""";

        var (_, _, result) = ExecuteProgram(text);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3007", diagnostic.Code);
        Assert.Contains("CHR", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("code", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_CompiledNestedBlockLocalProgram()
    {
        const string text = """
IF TRUE THEN
    DIM message AS STRING = "yes"
    PRINT message
ELSE
    DIM message AS STRING = "no"
    PRINT message
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["yes"], host.Output);
    }

    [Fact]
    public void Execute_CompiledElseIfBlockLocalProgram()
    {
        const string text = """
DIM mode AS INTEGER = 2
IF mode = 1 THEN
    DIM branchText AS STRING = "if-mode-1"
    PRINT branchText
ELSEIF mode = 2 THEN
    DIM branchText AS STRING = "elseif-branch"
    PRINT branchText
ELSE
    DIM branchText AS STRING = "if-mode-else"
    PRINT branchText
END IF

IF FALSE THEN
    DIM branchText AS STRING = "never"
    PRINT branchText
ELSE
    DIM branchText AS STRING = "else-branch"
    PRINT branchText
END IF
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["elseif-branch", "else-branch"], host.Output);
    }

    [Fact]
    public void Execute_CompiledSelectCaseBlockLocalProgram()
    {
        const string text = """
DIM code AS INTEGER = 2
SELECT CASE code
    CASE 1
        DIM caseText AS STRING = "case-one"
        PRINT caseText
    CASE 2
        DIM caseText AS STRING = "case-two"
        PRINT caseText
    CASE ELSE
        DIM caseText AS STRING = "case-other"
        PRINT caseText
END SELECT

DIM fallbackCode AS INTEGER = 9
SELECT CASE fallbackCode
    CASE 1
        DIM caseText AS STRING = "fallback-one"
        PRINT caseText
    CASE ELSE
        DIM caseText AS STRING = "case-else"
        PRINT caseText
END SELECT
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(
            result.Succeeded,
            string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
        Assert.Equal(["case-two", "case-else"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLoopBlockLocalProgram()
    {
        const string text = """
FOR i = 1 TO 2
    DIM loopText AS STRING = "for-" + STR(i)
    PRINT loopText
NEXT

DIM counter AS INTEGER = 0
WHILE counter < 2
    DIM whileText AS STRING = "while-" + STR(counter)
    PRINT whileText
    counter = counter + 1
END WHILE

DIM probe AS INTEGER = 0
DO
    DIM doText AS STRING = "do-" + STR(probe)
    PRINT doText
    probe = probe + 1
LOOP UNTIL probe = 2
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["for-1", "for-2", "while-0", "while-1", "do-0", "do-1"], host.Output);
    }

    [Fact]
    public void Execute_CompiledSubBlockLocalProgram()
    {
        const string text = """
PrintScopeRoutine()

SUB PrintScopeRoutine()
    PRINT "sub-start"

    FOR localIndex = 1 TO 2
        DIM subLoopText AS STRING = "sub-for-" + STR(localIndex)
        PRINT subLoopText
    NEXT

    IF TRUE THEN
        DIM subBranchText AS STRING = "sub-if"
        PRINT subBranchText
    END IF

    PRINT "sub-end"
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["sub-start", "sub-for-1", "sub-for-2", "sub-if", "sub-end"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFunctionBlockLocalProgram()
    {
        const string text = """
PRINT "fn=" + STR(ComputeScopedValue(3))

FUNCTION ComputeScopedValue(value AS INTEGER) AS INTEGER
    IF value > 2 THEN
        DIM bonus AS INTEGER = 4
        RETURN value + bonus
    ELSE
        DIM bonus AS INTEGER = 1
        RETURN value + bonus
    END IF
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["fn=7"], host.Output);
    }

    [Fact]
    public void Execute_CompiledFileSystemBuiltinsProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var sourcePath = Path.Combine(workspace, "source.txt");
            var copyPath = Path.Combine(workspace, "copy.txt");
            var renamedPath = Path.Combine(workspace, "renamed.txt");
            var linesPath = Path.Combine(workspace, "lines.txt");
            var nestedDirectory = Path.Combine(workspace, "nested");

            File.WriteAllText(linesPath, "one\ntwo\n");

            var text = $$"""
DIM source AS STRING = "{{EscapePath(sourcePath)}}"
DIM copyPath AS STRING = "{{EscapePath(copyPath)}}"
DIM renamedPath AS STRING = "{{EscapePath(renamedPath)}}"
DIM linesPath AS STRING = "{{EscapePath(linesPath)}}"
DIM nestedDirectory AS STRING = "{{EscapePath(nestedDirectory)}}"

PRINT FILEEXISTS(source)
PRINT WRITEFILE(source, "alpha")
PRINT APPENDFILE(source, "-beta")
PRINT READFILE(source)
PRINT FILESIZE(source)
PRINT COPYFILE(source, copyPath)
PRINT FILEEXISTS(copyPath)
PRINT RENAMEFILE(copyPath, "renamed.txt")
PRINT FILENAME(renamedPath)
PRINT EXTNAME(renamedPath)
PRINT MAKEDIR(nestedDirectory)
PRINT DIREXISTS(nestedDirectory)
DIM lines() AS STRING
lines = READALLLINES(linesPath)
PRINT ARRAYLENGTH(lines)
PRINT lines(2)
DIM files() AS STRING
files = GETFILES("{{EscapePath(workspace)}}", "*.txt")
PRINT ARRAYCONTAINS(files, renamedPath)
PRINT DELETEFILE(renamedPath)
PRINT DELETEDIR(nestedDirectory)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(
                [
                    "FALSE",
                    "TRUE",
                    "TRUE",
                    "alpha-beta",
                    "10",
                    "TRUE",
                    "TRUE",
                    "TRUE",
                    "renamed.txt",
                    ".txt",
                    "TRUE",
                    "TRUE",
                    "2",
                    "two",
                    "TRUE",
                    "TRUE",
                    "TRUE"
                ],
                host.Output);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledFileSystemBuiltinReportsMissingFile()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var missingPath = Path.Combine(workspace, "missing.txt");
            var text = $$"""
PRINT FILESIZE("{{EscapePath(missingPath)}}")
""";

            var (_, _, result) = ExecuteProgram(text);

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("FILESIZE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("не существует", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenInputFileProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var inputPath = Path.Combine(workspace, "input.txt");
            File.WriteAllText(inputPath, string.Join(Environment.NewLine, ["one", "two", "three"]));

            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(inputPath)}}" FOR INPUT AS f

PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT READLINE(f)
PRINT LINENUMBER(f)
DIM rest() AS STRING
rest = READLINES(f, 2)
PRINT ARRAYLENGTH(rest)
PRINT rest(2)
PRINT EOF(f)
CLOSE f
PRINT ISOPEN(f)
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(["TRUE", "INPUT", "one", "2", "2", "three", "TRUE", "FALSE"], host.Output);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenOutputAndAppendFileProgram()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var outputPath = Path.Combine(workspace, "output.txt");
            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(outputPath)}}" FOR OUTPUT AS f

PRINT ISOPEN(f)
PRINT FILEMODE(f)
PRINT WRITELINE(f, "alpha")
PRINT WRITE(f, "beta")
CLOSE f
PRINT ISOPEN(f)

OPEN "{{EscapePath(outputPath)}}" FOR APPEND AS f
PRINT WRITELINE(f, "-gamma")
CLOSE f
""";

            var (_, host, result) = ExecuteProgram(text);

            Assert.True(
                result.Succeeded,
                string.Join(" | ", result.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));
            Assert.Equal(["TRUE", "OUTPUT", "TRUE", "TRUE", "FALSE", "TRUE"], host.Output);
            Assert.Equal($"alpha{Environment.NewLine}beta-gamma{Environment.NewLine}", File.ReadAllText(outputPath));
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledOpenFileReportsClosedFileRead()
    {
        var workspace = CreateTemporaryWorkspace();
        try
        {
            var inputPath = Path.Combine(workspace, "input.txt");
            File.WriteAllText(inputPath, "one");

            var text = $$"""
DIM f AS FILE
OPEN "{{EscapePath(inputPath)}}" FOR INPUT AS f
CLOSE f
PRINT READLINE(f)
""";

            var (_, _, result) = ExecuteProgram(text);

            Assert.False(result.Succeeded);
            var diagnostic = Assert.Single(result.Diagnostics, static diagnostic => diagnostic.Code == "MB3007");
            Assert.Contains("READLINE", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteTemporaryWorkspace(workspace);
        }
    }

    [Fact]
    public void Execute_CompiledLocalFunctionCaptureProgram()
    {
        const string text = """
PRINT Outer(5)

FUNCTION Outer(seed AS INTEGER) AS INTEGER
    DIM offset AS INTEGER = 3
    RETURN Inner(4)

    FUNCTION Inner(multiplier AS INTEGER) AS INTEGER
        RETURN seed * multiplier + offset
    END FUNCTION
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["23"], host.Output);
    }

    [Fact]
    public void Execute_CompiledLocalSubCaptureProgram()
    {
        const string text = """
SUB Outer(stepValue AS INTEGER)
    DIM total AS INTEGER = 2
    Increase()
    Increase()
    PRINT total

    SUB Increase()
        total = total + stepValue
    END SUB
END SUB

Outer(3)
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["8"], host.Output);
    }

    private static (BytecodeModule module, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(
        string text,
        TestRuntimeHost? host = null,
        Action<BytecodeVmExecutionSnapshot>? instructionObserver = null,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction = null,
        BytecodeBreakpointSet? breakpoints = null)
    {
        var syntax = Parser.Parse(text, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.Empty(binderDiagnostics);

        var loweringResult = new BoundProgramLowerer().Lower(program);
        Assert.True(
            loweringResult.Succeeded,
            string.Join(" | ", loweringResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));

        var compilationResult = new BytecodeCompiler().Compile(loweringResult.Program!);
        Assert.True(
            compilationResult.Succeeded,
            string.Join(" | ", compilationResult.Diagnostics.Select(static diagnostic => $"{diagnostic.Code}:{diagnostic.Message}@{diagnostic.Location.Line}:{diagnostic.Location.Column}")));

        host ??= new TestRuntimeHost();
        var result = BytecodeVm.Execute(compilationResult.Module!, host, instructionObserver, shouldStopBeforeInstruction, breakpoints);
        return (compilationResult.Module!, host, result);
    }

    private static string CreateTemporaryWorkspace()
    {
        var path = Path.Combine(Path.GetTempPath(), "axiom-vm-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTemporaryWorkspace(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string EscapePath(string path)
    {
        return path.Replace("\"", "\"\"", StringComparison.Ordinal);
    }

    private static TimeZoneInfo CreateTestFixedTimeZone(string id, int offsetHours)
    {
        return TimeZoneInfo.CreateCustomTimeZone(
            id,
            TimeSpan.FromHours(offsetHours),
            id,
            id);
    }

    private static IReadOnlyDictionary<string, TimeZoneInfo> CreateNamedTimeZones(params TimeZoneInfo[] timeZones)
    {
        return timeZones.ToDictionary(static timeZone => timeZone.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class TestRuntimeHost : IRuntimeHost, IReversibleOutputWriter
    {
        private readonly Queue<string> _inputs;
        private readonly DateTimeOffset _localNow;
        private readonly TimeZoneInfo _localTimeZone;
        private readonly IReadOnlyDictionary<string, TimeZoneInfo> _namedTimeZones;

        public TestRuntimeHost(
            DateTimeOffset? localNow = null,
            TimeZoneInfo? localTimeZone = null,
            IReadOnlyDictionary<string, TimeZoneInfo>? namedTimeZones = null,
            IEnumerable<string>? inputs = null)
        {
            _inputs = new Queue<string>(inputs ?? []);
            _localNow = localNow ?? DateTimeOffset.Now;
            _localTimeZone = localTimeZone ?? TimeZoneInfo.Local;
            _namedTimeZones = namedTimeZones ?? new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
        }

        public List<string> Output { get; } = [];

        public int OutputCheckpoint => Output.Count;

        public List<InputRequest> InputRequests { get; } = [];

        public string ReadInput(InputRequest request)
        {
            InputRequests.Add(request);
            return _inputs.Count > 0 ? _inputs.Dequeue() : string.Empty;
        }

        public void WriteOutput(string text)
        {
            Output.Add(text);
        }

        public void RestoreOutputCheckpoint(int checkpoint)
        {
            if (checkpoint < 0 || checkpoint > Output.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
            }

            Output.RemoveRange(checkpoint, Output.Count - checkpoint);
        }

        public DateTimeOffset GetLocalNow() => _localNow;

        public TimeZoneInfo GetLocalTimeZone() => _localTimeZone;

        public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
        {
            if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = _localTimeZone;
                return true;
            }

            if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
            {
                timeZone = TimeZoneInfo.Utc;
                return true;
            }

            return _namedTimeZones.TryGetValue(id, out timeZone!);
        }
    }
}
