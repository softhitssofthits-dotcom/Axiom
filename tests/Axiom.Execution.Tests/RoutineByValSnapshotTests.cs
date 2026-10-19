namespace Axiom.Execution.Tests;

public sealed class RoutineByValSnapshotTests
{
    [Fact]
    public void ByValRecordIsSnapshottedWhenItsArgumentIsEvaluated()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Box
    item AS INTEGER
END TYPE
DIM shared AS Box
shared.item = 1
FUNCTION Touch() AS INTEGER
    shared.item = 2
    RETURN 0
END FUNCTION
FUNCTION Observe(copy AS Box, ignored AS INTEGER) AS INTEGER
    RETURN copy.item
END FUNCTION
PRINT Observe(shared, Touch())
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1"], result.Output);
    }

    [Fact]
    public void LaterArgumentSeesChangesMadeByEarlierArgument()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Box
    item AS INTEGER
END TYPE
DIM shared AS Box
shared.item = 1
FUNCTION Touch() AS INTEGER
    shared.item = 2
    RETURN 0
END FUNCTION
FUNCTION Observe(ignored AS INTEGER, copy AS Box) AS INTEGER
    RETURN copy.item
END FUNCTION
PRINT Observe(Touch(), shared)
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["2"], result.Output);
    }

    [Fact]
    public void ByValRecordParameterStaysIndependentFromCaller()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Box
    item AS INTEGER
END TYPE
SUB Change(copy AS Box)
    copy.item = 99
END SUB
DIM shared AS Box
shared.item = 1
Change(shared)
PRINT shared.item
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1"], result.Output);
    }

    [Fact]
    public void IndirectCallAlsoSnapshotsRecordArgument()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Box
    item AS INTEGER
END TYPE
DIM shared AS Box
shared.item = 1
FUNCTION Touch() AS INTEGER
    shared.item = 2
    RETURN 0
END FUNCTION
FUNCTION Observe(copy AS Box, ignored AS INTEGER) AS INTEGER
    RETURN copy.item
END FUNCTION
DIM action AS FUNCTION(Box, INTEGER) AS INTEGER
action = Observe
PRINT action(shared, Touch())
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1"], result.Output);
    }

    [Fact]
    public void ArrayCallbackStillReceivesRecordCopy()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Box
    item AS INTEGER
END TYPE
DIM items(1 TO 1) AS Box
items(1).item = 5
FUNCTION Peek(copy AS Box) AS BOOLEAN
    copy.item = 99
    RETURN copy.item = 99
END FUNCTION
PRINT ARRAYEVERY(items, Peek)
PRINT items(1).item
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["TRUE", "5"], result.Output);
    }
}
