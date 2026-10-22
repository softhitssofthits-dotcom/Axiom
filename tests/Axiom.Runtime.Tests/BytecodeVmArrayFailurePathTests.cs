using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

public sealed class BytecodeVmArrayFailurePathTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnhandledAllocationLimitHasExactLocation(bool observed)
    {
        const string source = """
DIM values(1 TO 1) AS INTEGER
PRINT "before"
REDIM values(1 TO 100000000)
PRINT "unreachable"
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3013", diagnostic.Code);
        Assert.Equal(3, diagnostic.Location.Line);
        Assert.Equal(1, diagnostic.Location.Column);
        Assert.Equal(["before"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AllocationFailureRunsCatchAndFinallyAndKeepsArray(bool observed)
    {
        const string source = """
DIM values(1 TO 1) AS INTEGER
values(1) = 7
TRY
    REDIM values(1 TO 100000000)
    PRINT "unreachable"
CATCH failure
    PRINT failure.Code
    PRINT failure.Line
    PRINT failure.Column
FINALLY
    PRINT "finally"
END TRY
PRINT values(1)
values(1) = 9
PRINT values(1)
PRINT "after"
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3013", "4", "5", "finally", "7", "9", "after"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    [Theory]
    [InlineData(false, "REDIM grid(1 TO 3, 1 TO 3)")]
    [InlineData(true, "REDIM grid(1 TO 3, 1 TO 3)")]
    [InlineData(false, "REDIM PRESERVE grid(1 TO 2, 1 TO 3)")]
    [InlineData(true, "REDIM PRESERVE grid(1 TO 2, 1 TO 3)")]
    [InlineData(false, "ERASE grid")]
    [InlineData(true, "ERASE grid")]
    public void UnhandledExpiredViewHasExactLocation(bool observed, string invalidate)
    {
        var source = $$"""
DIM grid(1 TO 2, 1 TO 2) AS INTEGER
DIM rowView() AS INTEGER
grid(1, 1) = 7
rowView = grid(1)
{{invalidate}}
PRINT "before"
PRINT rowView(1)
PRINT "unreachable"
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.False(result.Succeeded);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("MB3014", diagnostic.Code);
        Assert.Equal(7, diagnostic.Location.Line);
        Assert.Equal(7, diagnostic.Location.Column);
        Assert.Equal(["before"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
    }

    [Theory]
    [InlineData(false, "REDIM grid(1 TO 3, 1 TO 3)")]
    [InlineData(true, "REDIM grid(1 TO 3, 1 TO 3)")]
    [InlineData(false, "REDIM PRESERVE grid(1 TO 2, 1 TO 3)")]
    [InlineData(true, "REDIM PRESERVE grid(1 TO 2, 1 TO 3)")]
    [InlineData(false, "ERASE grid")]
    [InlineData(true, "ERASE grid")]
    public void ExpiredViewRunsCatchAndFinallyAndRootRemainsUsable(bool observed, string invalidate)
    {
        var source = $$"""
DIM grid(1 TO 2, 1 TO 2) AS INTEGER
DIM rowView() AS INTEGER
grid(1, 1) = 7
rowView = grid(1)
{{invalidate}}
TRY
    PRINT rowView(1)
    PRINT "unreachable"
CATCH failure
    PRINT failure.Code
    PRINT failure.Line
    PRINT failure.Column
FINALLY
    PRINT "finally"
END TRY
REDIM grid(1 TO 2, 1 TO 2)
grid(1, 1) = 9
PRINT grid(1, 1)
PRINT "after"
""";

        var (host, result, snapshots) = Execute(source, observed);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["MB3014", "7", "11", "finally", "9", "after"], host.Output);
        Assert.Equal(observed, snapshots.Count > 0);
        if (observed)
        {
            Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 7 && snapshot.SourceLocation.Column == 11);
            Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 19);
        }
    }

    [Fact]
    public void ObservedAllocationFailureKeepsIndependentStateSnapshotAndResumes()
    {
        const string source = """
DIM values(1 TO 1) AS INTEGER
values(1) = 7
TryResize()
values(1) = 9
PRINT values(1)
values(1) = 10
PRINT values(1)

SUB TryResize()
    TRY
        REDIM values(1 TO 100000000)
    CATCH failure
        PRINT failure.Code
        PRINT failure.Line
        PRINT failure.Column
    FINALLY
        PRINT "finally"
    END TRY
END SUB
""";
        var stoppedOnce = false;
        var (host, stopped, snapshots) = Execute(source, observed: true, snapshot =>
        {
            if (stoppedOnce || snapshot.SourceLocation.Line != 5 || snapshot.Opcode != Opcode.PrintValue)
            {
                return false;
            }

            stoppedOnce = true;
            return true;
        });

        Assert.True(stopped.Succeeded);
        Assert.True(stopped.Stopped);
        Assert.Empty(stopped.Diagnostics);
        Assert.Equal(["MB3013", "11", "9", "finally"], host.Output);
        var previous = Assert.IsType<BytecodeVmStateSnapshot>(stopped.StopState!.PreviousStateSnapshot);
        var arraySnapshot = Assert.Single(previous.Globals.Values, value => value.Type is ArrayTypeSymbol).AsArray();
        Assert.Equal(9, arraySnapshot.GetValue(1).AsInteger());
        Assert.Contains(snapshots, snapshot => snapshot.SourceLocation.Line == 11 && snapshot.SourceLocation.Column == 9 && snapshot.CallDepth == 2);

        var resumed = stopped.StopState.Resume();

        Assert.True(resumed.Succeeded);
        Assert.False(resumed.Stopped);
        Assert.Empty(resumed.Diagnostics);
        Assert.Equal(["MB3013", "11", "9", "finally", "9", "10"], host.Output);
        Assert.Equal(9, arraySnapshot.GetValue(1).AsInteger());
    }

    private static (TestRuntimeHost Host, RuntimeResult Result, List<BytecodeVmExecutionSnapshot> Snapshots) Execute(
        string source,
        bool observed,
        Func<BytecodeVmExecutionSnapshot, bool>? shouldStopBeforeInstruction = null)
    {
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);
        var program = Binder.Bind(syntax, out var binderDiagnostics);
        Assert.DoesNotContain(binderDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var lowering = new BoundProgramLowerer().Lower(program);
        Assert.True(lowering.Succeeded);
        var compilation = new BytecodeCompiler().Compile(lowering.Program!);
        Assert.True(compilation.Succeeded, string.Join(" | ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var host = new TestRuntimeHost([]);
        var snapshots = new List<BytecodeVmExecutionSnapshot>();
        var result = BytecodeVm.Execute(compilation.Module!, host, observed ? snapshots.Add : null, shouldStopBeforeInstruction);
        return (host, result, snapshots);
    }
}
