using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArrayRemainingFormEvidenceTests
{
    private const string Setup = """
DIM a(-2 TO -1, 7 TO 8) AS INTEGER
DIM template(9 TO 10, 4 TO 5) AS INTEGER
DIM matrix(,) AS INTEGER
DIM flat() AS INTEGER
DIM values(7 TO 8) AS INTEGER
a(-2, 7) = 1
a(-2, 8) = 2
a(-1, 7) = 0
a(-1, 8) = 4
""";

    [Theory]
    [InlineData("ArrayToSorted", "matrix = ARRAYTOSORTED(a, 1)", "INTEGER", 2)]
    [InlineData("ArrayToReversed", "matrix = ARRAYTOREVERSED(a, 1)", "INTEGER", 2)]
    [InlineData("ArrayFlatten", "flat = ARRAYFLATTEN(a)", "INTEGER", 1)]
    [InlineData("ArrayReshape", "matrix = ARRAYRESHAPE(a, template)", "INTEGER", 2)]
    [InlineData("ArrayFill", "ARRAYFILL(a, 42)", "VOID", 0)]
    [InlineData("ArrayReverse", "ARRAYREVERSE(a, 1)", "VOID", 0)]
    [InlineData("ArraySort", "ARRAYSORT(a, 1)", "VOID", 0)]
    [InlineData("ArrayPush", "ARRAYPUSH(a, values, 1)", "VOID", 0)]
    [InlineData("ArrayPop", "flat = ARRAYPOP(a, 1)", "INTEGER", 1)]
    [InlineData("ArrayInsert", "ARRAYINSERT(a, -1, values, 1)", "VOID", 0)]
    [InlineData("ArrayRemove", "flat = ARRAYREMOVE(a, -2, 1)", "INTEGER", 1)]
    [InlineData("ArrayConcat", "flat = ARRAYCONCAT(a(-2), a(-1))", "INTEGER", 1)]
    [InlineData("ArrayConcat", "matrix = ARRAYCONCAT(a, a, 1)", "INTEGER", 2)]
    [InlineData("ArraySum", "PRINT ARRAYSUM(a)", "INTEGER", 0)]
    [InlineData("ArrayMin", "PRINT ARRAYMIN(a)", "INTEGER", 0)]
    [InlineData("ArrayMax", "PRINT ARRAYMAX(a)", "INTEGER", 0)]
    [InlineData("ArrayAverage", "PRINT ARRAYAVERAGE(a)", "DOUBLE", 0)]
    public void RemainingFormBindsExactBuiltinAndResultType(string id, string statement, string typeName, int rank)
    {
        var syntax = Parser.Parse(Setup + "\n" + statement, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var final = program.GlobalStatements.Last();
        BoundExpression expression = final switch
        {
            BoundAssignmentStatement assignment => assignment.Expression,
            BoundPrintStatement print => Assert.Single(print.Items).Expression,
            BoundExpressionStatement standalone => standalone.Expression,
            _ => throw new InvalidOperationException(final.Kind.ToString())
        };
        var call = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        if (rank == 0) Assert.Equal(typeName, call.Type.Name.ToUpperInvariant());
        else
        {
            var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
            Assert.Equal(rank, type.Rank);
            Assert.Equal(typeName, type.ElementType.Name.ToUpperInvariant());
        }
    }

    [Fact]
    public void IntegerMatrixAggregatesReturnIndependentExpectedValues()
    {
        var source = Setup + "\nPRINT ARRAYSUM(a)\nPRINT ARRAYMIN(a)\nPRINT ARRAYMAX(a)\nPRINT ARRAYAVERAGE(a)\nPRINT a(-2, 7)\nPRINT a(-1, 8)";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "7", "0", "4", "1.75", "1", "4" }, run.Output);
    }
}
