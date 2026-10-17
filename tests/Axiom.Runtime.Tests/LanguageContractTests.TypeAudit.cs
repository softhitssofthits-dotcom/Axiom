using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Runtime.Tests;

/// <summary>
/// Постоянные регрессии аудита TYPE от 17 октября 2026 года: адресуемый SELF,
/// отложенное создание записей и независимые снимки для обратного шага.
/// </summary>
public sealed partial class LanguageContractTests
{
    [Fact]
    public void Execute_TypeMethodOnFieldObservesOwnChangeImmediately()
    {
        const string text = """
TYPE Leaf
    Value AS INTEGER
    SUB Change()
        SELF.Value = 10
        PRINT root.Child.Value
    END SUB
END TYPE

TYPE Outer
    Child AS Leaf
END TYPE

DIM root AS Outer
root.Child.Change()
PRINT root.Child.Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodOnFieldKeepsNeighbourFieldWrittenInside()
    {
        const string text = """
TYPE Leaf
    Value AS INTEGER
    Other AS INTEGER
    SUB Change()
        SELF.Value = 10
        root.Child.Other = 99
    END SUB
END TYPE

TYPE Outer
    Child AS Leaf
END TYPE

DIM root AS Outer
root.Child.Change()
PRINT root.Child.Value
PRINT root.Child.Other
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "99"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodOnArrayElementObservesOwnChangeImmediately()
    {
        const string text = """
TYPE Leaf
    Value AS INTEGER
    SUB Change()
        SELF.Value = 10
        PRINT items(0).Value
    END SUB
END TYPE

DIM items(0 TO 0) AS Leaf
items(0).Change()
PRINT items(0).Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "10"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodOnArrayElementKeepsSiblingAssignment()
    {
        const string text = """
TYPE Leaf
    Value AS INTEGER
    SUB Change()
        SELF.Value = 10
        items(1).Value = 99
    END SUB
END TYPE

DIM items(0 TO 1) AS Leaf
items(0).Change()
PRINT items(0).Value
PRINT items(1).Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["10", "99"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodKeepsSelfChangesWhenBodyRaisesError()
    {
        const string text = """
TYPE Box
    Value AS INTEGER
    SUB Change()
        SELF.Value = 10
        ERROR "E", "failure"
    END SUB
END TYPE

TYPE Outer
    Child AS Box
END TYPE

DIM x AS Outer

TRY
    x.Child.Change()
CATCH problem
    PRINT problem.Code
END TRY

PRINT x.Child.Value
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["E", "10"], host.Output);
    }

    [Fact]
    public void Execute_TypeMethodEvaluatesArrayReceiverIndexOnce()
    {
        const string text = """
TYPE Leaf
    V AS INTEGER
    SUB Change()
        SELF.V = SELF.V + 1
    END SUB
END TYPE

DIM items(0 TO 0) AS Leaf
DIM calls AS INTEGER

items(Pick()).Change()
PRINT calls
PRINT items(0).V

FUNCTION Pick() AS INTEGER
    calls = calls + 1
    RETURN 0
END FUNCTION
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["1", "1"], host.Output);
    }

    [Fact]
    public void Execute_GlobalRecordReadBeforeDeclarationSeesDefaultValue()
    {
        const string text = """
TYPE Item
    Value AS INTEGER
END TYPE

SUB Show()
    PRINT root.Value
END SUB

Show()
DIM root AS Item
root.Value = 5
Show()
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["0", "5"], host.Output);
    }

    [Fact]
    public void Execute_UncalledRoutineWithOversizedFixedFieldAllocatesNothing()
    {
        const string text = """
TYPE Big
    Data(0 TO 67108864) AS INTEGER
END TYPE

PRINT 42

SUB NeverCalled()
    DIM local AS Big
    PRINT UBOUND(local.Data)
END SUB
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal(["42"], host.Output);
    }

    [Fact]
    public void Execute_OversizedFixedFieldReportsDiagnosticInsideTry()
    {
        const string text = """
TYPE Big
    Data(0 TO 67108864) AS INTEGER
END TYPE

TRY
    DIM local AS Big
    PRINT UBOUND(local.Data)
CATCH problem
    PRINT problem.Code
END TRY

PRINT 42
""";

        var (_, host, result) = ExecuteProgram(text);

        Assert.True(result.Succeeded);
        Assert.Equal([DiagnosticCodes.ArrayAllocation, "42"], host.Output);
    }

    [Fact]
    public void Execute_StepBackIsAvailableWhenStateContainsRecord()
    {
        const string text = """
TYPE Item
    Value AS INTEGER
END TYPE

DIM item AS Item
item.Value = 1
item.Value = 2
PRINT item.Value
""";

        var (_, host, result) = ExecuteProgram(text, breakpoints: BytecodeBreakpointSet.FromLines(8));

        Assert.True(result.Stopped);
        Assert.True(result.StopState!.CanStepBack);

        var backed = result.StopState.StepBack();

        Assert.True(backed.Succeeded);
        Assert.True(backed.StopState!.CanResume);

        var completed = ResumeUntilCompleted(backed);

        Assert.True(completed.Succeeded);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_StepBackIsAvailableWhenStateContainsRecordArray()
    {
        const string text = """
TYPE Item
    Value AS INTEGER
END TYPE

DIM items(0 TO 0) AS Item
items(0).Value = 1
items(0).Value = 2
PRINT items(0).Value
""";

        var (_, host, result) = ExecuteProgram(text, breakpoints: BytecodeBreakpointSet.FromLines(8));

        Assert.True(result.Stopped);
        Assert.True(result.StopState!.CanStepBack);

        var backed = result.StopState.StepBack();
        var completed = ResumeUntilCompleted(backed);

        Assert.True(completed.Succeeded);
        Assert.Equal(["2"], host.Output);
    }

    [Fact]
    public void Execute_StepBackRestoresRecordFieldValueBeforeInstruction()
    {
        const string text = """
TYPE Item
    Value AS INTEGER
END TYPE

DIM item AS Item
item.Value = 1
item.Value = 2
PRINT item.Value
""";

        var (_, host, result) = ExecuteProgram(text, breakpoints: BytecodeBreakpointSet.FromLines(8));

        Assert.True(result.StopState!.CanStepBack);

        var backed = result.StopState.StepBack();
        Assert.Equal(7, backed.StoppedAt!.SourceLocation.Line);

        var stoppedAgain = backed.StopState!.Resume();
        Assert.Equal(8, stoppedAgain.StoppedAt!.SourceLocation.Line);

        var completed = ResumeUntilCompleted(stoppedAgain);

        Assert.True(completed.Succeeded);
        Assert.Equal(["2"], host.Output);
    }

    /// <summary>
    /// Продолжает исполнение через все оставшиеся точки останова на той же линии,
    /// пока программа не завершится.
    /// </summary>
    private static RuntimeResult ResumeUntilCompleted(RuntimeResult result)
    {
        while (result.Stopped && result.StopState?.CanResume == true)
        {
            result = result.StopState.Resume();
        }

        return result;
    }

    private static (BoundProgram program, TestRuntimeHost host, RuntimeResult result) ExecuteProgram(
        string text,
        BytecodeBreakpointSet? breakpoints)
    {
        return RuntimeVmContractRunner.Execute(text, new TestRuntimeHost([]), breakpoints);
    }
}
