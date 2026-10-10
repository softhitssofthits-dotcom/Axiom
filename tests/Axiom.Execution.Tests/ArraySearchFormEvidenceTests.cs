using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArraySearchFormEvidenceTests
{
    private const string Setup = """
DIM a(-4 TO -2) AS INTEGER
DIM m(-2 TO -1, 7 TO 8) AS INTEGER
DIM result() AS INTEGER
DIM hits(,) AS INTEGER
a(-4) = 2
a(-3) = 2
a(-2) = 4
m(-2, 7) = 1
m(-2, 8) = 2
m(-1, 7) = 2
m(-1, 8) = 4
""";
    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "ArrayContains", "ARRAYCONTAINS(a, 2)", "BOOLEAN", new[] { "TRUE" } };
        yield return new object[] { "ArrayCount", "ARRAYCOUNT(a, 2)", "INTEGER", new[] { "2" } };
        yield return new object[] { "ArrayIndexOf", "ARRAYINDEXOF(a, 2)", "INTEGER", new[] { "-4" } };
        yield return new object[] { "ArrayLastIndexOf", "ARRAYLASTINDEXOF(a, 2)", "INTEGER", new[] { "-3" } };
        yield return new object[] { "ArrayFindAll", "ARRAYFINDALL(a, 2)", "array1", new[] { "-4", "-3" } };
        yield return new object[] { "ArrayBinarySearch", "ARRAYBINARYSEARCH(a, 2)", "INTEGER", new[] { "-4" } };
        yield return new object[] { "ArrayBinarySearch", "ARRAYBINARYSEARCH(a, 4, 1)", "INTEGER", new[] { "-2" } };
        yield return new object[] { "ArrayIndexOf", "ARRAYINDEXOF(m, 2)", "array1", new[] { "-2", "8" } };
        yield return new object[] { "ArrayLastIndexOf", "ARRAYLASTINDEXOF(m, 2)", "array1", new[] { "-1", "7" } };
        yield return new object[] { "ArrayFindAll", "ARRAYFINDALL(m, 2)", "array2", new[] { "-2", "8", "-1", "7" } };
        yield return new object[] { "ArrayBinarySearch", "ARRAYBINARYSEARCH(m, 2, 1)", "array1", new[] { "-1", "7" } };
        yield return new object[] { "ArrayBinarySearch", "ARRAYBINARYSEARCH(m, 2, 2)", "array1", new[] { "-2", "8" } };
    }


    [Fact]
    public void MultidimensionalBinarySearchRequiresExplicitDimension()
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\nresult = ARRAYBINARYSEARCH(m, 2)"));
            Assert.False(run.Succeeded);
            Assert.Equal("MB2003", Assert.Single(run.Diagnostics).Code);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void SearchFormBindsExactBuiltinAndResultShape(string id, string callText, string kind, string[] expected)
    {
        var array = kind.StartsWith("array", StringComparison.Ordinal);
        var target = kind == "array2" ? "hits" : "result";
        var statement = array ? target + " = " + callText : "PRINT " + callText;
        var syntax = Parser.Parse(Setup + "\n" + statement, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var expression = array ? Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements.Last()).Expression
            : Assert.Single(Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last()).Items).Expression;
        var call = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        if (array)
        {
            var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
            Assert.Equal(kind == "array2" ? 2 : 1, type.Rank);
            Assert.Equal(TypeSymbol.Integer, type.ElementType);
        }
        else Assert.Equal(kind, call.Type.Name.ToUpperInvariant());
        var output = !array ? "" : kind == "array2"
            ? "\nFOR i = LBOUND(hits, 1) TO UBOUND(hits, 1)\nFOR j = LBOUND(hits, 2) TO UBOUND(hits, 2)\nPRINT hits(i, j)\nNEXT j\nNEXT i"
            : "\nFOR i = LBOUND(result) TO UBOUND(result)\nPRINT result(i)\nNEXT i";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\n" + statement + output));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
}
