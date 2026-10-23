namespace Axiom.Execution.Tests;

public sealed class RoutineReceiverContractTests
{
    [Theory]
    [InlineData("cells[1].Edit()", new[] { "1", "1" })]
    [InlineData("GetCells()[1].Edit()", new[] { "1", "1" })]
    public void MethodOnArrayElementObservesLiveSelf(string receiver, string[] expected)
    {
        var result = Run(receiver, failing: false);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }

    [Theory]
    [InlineData("cells[1].Edit()", new[] { "1", "EXPECTED", "1" })]
    [InlineData("GetCells()[1].Edit()", new[] { "1", "EXPECTED", "1" })]
    public void MethodOnArrayElementKeepsChangesWhenItFails(string receiver, string[] expected)
    {
        var result = Run(receiver, failing: true);

        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }

    [Fact]
    public void ReceiverExpressionAndIndicesAreEvaluatedOnce()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("TYPE Cell\r\n    item AS INTEGER\r\n    SUB Edit()\r\n        SELF.item = SELF.item + 1\r\n    END SUB\r\nEND TYPE\r\nDIM cells[1 TO 3] AS Cell\r\nDIM receiverCalls AS INTEGER = 0\r\nDIM indexCalls AS INTEGER = 0\r\nFUNCTION GetCells() AS Cell[]\r\n    receiverCalls = receiverCalls + 1\r\n    RETURN cells[]\r\nEND FUNCTION\r\nFUNCTION NextIndex() AS INTEGER\r\n    indexCalls = indexCalls + 1\r\n    RETURN 2\r\nEND FUNCTION\r\nGetCells()[NextIndex()].Edit()\r\nPRINT cells[2].item\r\nPRINT receiverCalls\r\nPRINT indexCalls"));

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
        PRINT cells[1].item
        {{failure}}
    END SUB
END TYPE
DIM cells[1 TO 1] AS Cell
FUNCTION GetCells() AS Cell[]
    RETURN cells[]
END FUNCTION
TRY
    {{receiver}}
CATCH e
    PRINT e.Code
END TRY
PRINT cells[1].item
"""));
    }
}
