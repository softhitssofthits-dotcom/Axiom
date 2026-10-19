namespace Axiom.Execution.Tests;

public sealed class RoutineReceiverContractTests
{
    [Theory]
    [InlineData("cells(1).Edit()", new[] { "1", "1" })]
    [InlineData("GetCells()(1).Edit()", new[] { "1", "1" })]
    public void MethodOnArrayElementObservesLiveSelf(string receiver, string[] expected)
    {
        var result = Run(receiver, failing: false);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }

    [Theory]
    [InlineData("cells(1).Edit()", new[] { "1", "EXPECTED", "1" })]
    [InlineData("GetCells()(1).Edit()", new[] { "1", "EXPECTED", "1" })]
    public void MethodOnArrayElementKeepsChangesWhenItFails(string receiver, string[] expected)
    {
        var result = Run(receiver, failing: true);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }

    [Fact]
    public void ReceiverExpressionAndIndicesAreEvaluatedOnce()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
TYPE Cell
    item AS INTEGER
    SUB Edit()
        SELF.item = SELF.item + 1
    END SUB
END TYPE
DIM cells(1 TO 3) AS Cell
DIM receiverCalls AS INTEGER = 0
DIM indexCalls AS INTEGER = 0
FUNCTION GetCells() AS Cell ()
    receiverCalls = receiverCalls + 1
    RETURN cells
END FUNCTION
FUNCTION NextIndex() AS INTEGER
    indexCalls = indexCalls + 1
    RETURN 2
END FUNCTION
GetCells()(NextIndex()).Edit()
PRINT cells(2).item
PRINT receiverCalls
PRINT indexCalls
"""));

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(["1", "1", "1"], result.Output);
    }

    private static ExecutionResult Run(string receiver, bool failing)
    {
        var failure = failing ? "ERROR \"EXPECTED\", \"failure\"" : string.Empty;

        return new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
TYPE Cell
    item AS INTEGER
    SUB Edit()
        SELF.item = SELF.item + 1
        PRINT cells(1).item
        {{failure}}
    END SUB
END TYPE
DIM cells(1 TO 1) AS Cell
FUNCTION GetCells() AS Cell ()
    RETURN cells
END FUNCTION
TRY
    {{receiver}}
CATCH e
    PRINT e.Code
END TRY
PRINT cells(1).item
"""));
    }
}
