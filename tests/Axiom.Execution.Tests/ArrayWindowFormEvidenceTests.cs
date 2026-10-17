using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArrayWindowFormEvidenceTests
{
    private const string Setup = """
DIM a(5 TO 7) AS INTEGER
DIM m(-2 TO -1, 4 TO 6) AS INTEGER
DIM result() AS INTEGER
DIM matrix(,) AS INTEGER
a(5) = 30
a(6) = 10
a(7) = 20
m(-2, 4) = 1
m(-2, 5) = 2
m(-2, 6) = 3
m(-1, 4) = 4
m(-1, 5) = 5
m(-1, 6) = 6
""";
    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "LBound", "LBOUND(m)", "INTEGER", new[] { "-2" } };
        yield return new object[] { "LBound", "LBOUND(m, 2)", "INTEGER", new[] { "4" } };
        yield return new object[] { "UBound", "UBOUND(m)", "INTEGER", new[] { "-1" } };
        yield return new object[] { "UBound", "UBOUND(m, 2)", "INTEGER", new[] { "6" } };
        yield return new object[] { "ArrayLength", "ARRAYLENGTH(m)", "INTEGER", new[] { "6" } };
        yield return new object[] { "ArrayIsAllocated", "ARRAYISALLOCATED(m)", "BOOLEAN", new[] { "TRUE" } };
        yield return new object[] { "ArrayRank", "ARRAYRANK(m)", "INTEGER", new[] { "2" } };
        yield return new object[] { "ArrayBounds", "ARRAYBOUNDS(m)", "array2", new[] { "-2", "-1", "4", "6" } };
        yield return new object[] { "ArrayClone", "ARRAYCLONE(a)", "array1", new[] { "30", "10", "20" } };
        yield return new object[] { "ArraySlice", "ARRAYSLICE(a, 6, 2)", "array1", new[] { "10", "20" } };
        yield return new object[] { "ArraySlice", "ARRAYSLICE(m, 5, 1, 2)", "array2", new[] { "2", "5" } };
        yield return new object[] { "ArrayTake", "ARRAYTAKE(a, 2)", "array1", new[] { "30", "10" } };
        yield return new object[] { "ArrayTake", "ARRAYTAKE(m, 1, 2)", "array2", new[] { "1", "4" } };
        yield return new object[] { "ArraySkip", "ARRAYSKIP(a, 1)", "array1", new[] { "10", "20" } };
        yield return new object[] { "ArraySkip", "ARRAYSKIP(m, 1, 2)", "array2", new[] { "2", "3", "5", "6" } };
    }

    public static IEnumerable<object[]> InvalidOneDimensionalAxes()
    {
        foreach (var dimension in new[] { "0", "2", "2147483647", "(-2147483647 - 1)" })
            foreach (var call in new[] { $"ARRAYSLICE(a, 5, 1, {dimension})", $"ARRAYTAKE(a, 1, {dimension})", $"ARRAYSKIP(a, 1, {dimension})" })
                yield return new object[] { call };
    }

    public static IEnumerable<object[]> WrongArguments()
    {
        yield return new object[] { "LBOUND(TRUE)" };
        yield return new object[] { "LBOUND(TRUE, 2)" };
        yield return new object[] { "LBOUND(m, TRUE)" };
        yield return new object[] { "UBOUND(TRUE)" };
        yield return new object[] { "UBOUND(TRUE, 2)" };
        yield return new object[] { "UBOUND(m, TRUE)" };
        yield return new object[] { "ARRAYLENGTH(TRUE)" };
        yield return new object[] { "ARRAYISALLOCATED(TRUE)" };
        yield return new object[] { "ARRAYRANK(TRUE)" };
        yield return new object[] { "ARRAYBOUNDS(TRUE)" };
        yield return new object[] { "ARRAYCLONE(TRUE)" };
        yield return new object[] { "ARRAYSLICE(TRUE, 6, 2)" };
        yield return new object[] { "ARRAYSLICE(a, TRUE, 2)" };
        yield return new object[] { "ARRAYSLICE(a, 6, TRUE)" };
        yield return new object[] { "ARRAYSLICE(TRUE, 5, 1, 2)" };
        yield return new object[] { "ARRAYSLICE(m, TRUE, 1, 2)" };
        yield return new object[] { "ARRAYSLICE(m, 5, TRUE, 2)" };
        yield return new object[] { "ARRAYSLICE(m, 5, 1, TRUE)" };
        yield return new object[] { "ARRAYTAKE(TRUE, 2)" };
        yield return new object[] { "ARRAYTAKE(a, TRUE)" };
        yield return new object[] { "ARRAYTAKE(TRUE, 1, 2)" };
        yield return new object[] { "ARRAYTAKE(m, TRUE, 2)" };
        yield return new object[] { "ARRAYTAKE(m, 1, TRUE)" };
        yield return new object[] { "ARRAYSKIP(TRUE, 1)" };
        yield return new object[] { "ARRAYSKIP(a, TRUE)" };
        yield return new object[] { "ARRAYSKIP(TRUE, 1, 2)" };
        yield return new object[] { "ARRAYSKIP(m, TRUE, 2)" };
        yield return new object[] { "ARRAYSKIP(m, 1, TRUE)" };
    }

    [Theory]
    [MemberData(nameof(WrongArguments))]
    public void WrongTypesMatchOnBytecodeVm(string call)
    {
        var source = Setup + "\nPRINT " + call;
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.NotEmpty(second.Diagnostics);
    }

    [Fact]
    public void CopiesAndEmptyResultsHaveIndependentStorageAndVisibleBounds()
    {
        const string extra = """
result = ARRAYCLONE(a)
result(5) = 99
PRINT a(5)
PRINT LBOUND(result)
PRINT UBOUND(result)
result = ARRAYSLICE(a, 6, 2, 1)
PRINT LBOUND(result)
PRINT UBOUND(result)
PRINT result(1)
result = ARRAYTAKE(a, 0, 1)
PRINT ARRAYISALLOCATED(result)
PRINT ARRAYLENGTH(result)
PRINT LBOUND(result)
PRINT UBOUND(result)
result = ARRAYSKIP(a, 2147483647, 1)
PRINT ARRAYLENGTH(result)
matrix = ARRAYTAKE(m, 0, 2)
PRINT ARRAYRANK(matrix)
PRINT ARRAYLENGTH(matrix)
PRINT LBOUND(matrix, 1)
PRINT UBOUND(matrix, 1)
PRINT LBOUND(matrix, 2)
PRINT UBOUND(matrix, 2)
DIM missing() AS INTEGER
result = ARRAYCLONE(missing)
PRINT ARRAYISALLOCATED(result)
PRINT ARRAYLENGTH(result)
PRINT ARRAYRANK(result)
""";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\n" + extra));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Equal(new[] { "30", "5", "7", "1", "2", "10", "TRUE", "0", "1", "0", "0", "2", "0", "-2", "-1", "1", "0", "FALSE", "0", "1" }, run.Output);
    }

    [Theory]
    [MemberData(nameof(InvalidOneDimensionalAxes))]
    public void ExplicitAxisIsValidatedEvenForOneDimensionalArrays(string call)
    {
        var source = Setup + "\nresult = " + call;
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.Equal("MB3007", Assert.Single(second.Diagnostics).Code);

        {
            var caught = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\nTRY\nresult = " + call + "\nCATCH err\nPRINT err.Code\nEND TRY"));
            Assert.True(caught.Succeeded);
            Assert.Equal(new[] { "MB3007" }, caught.Output);
        }
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void RegisteredFormProducesIndependentExpectedValues(string id, string call, string kind, string[] expected)
    {
        var isArray = kind.StartsWith("array", StringComparison.Ordinal);
        var target = kind == "array2" ? "matrix" : "result";
        var statement = isArray ? target + " = " + call : "PRINT " + call;
        var syntax = Parser.Parse(Setup + "\n" + statement, out var parsingDiagnostics);
        Assert.Empty(parsingDiagnostics);
        var program = Binder.Bind(syntax, out var bindingDiagnostics);
        Assert.Empty(bindingDiagnostics);
        var expression = isArray
            ? Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements.Last()).Expression
            : Assert.Single(Assert.IsType<BoundPrintStatement>(program.GlobalStatements.Last()).Items).Expression;
        var boundCall = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(boundCall.Target).Function.BuiltinId?.ToString());
        if (isArray)
        {
            var type = Assert.IsType<ArrayTypeSymbol>(boundCall.Type);
            Assert.Equal(kind == "array2" ? 2 : 1, type.Rank);
            Assert.Equal(TypeSymbol.Integer, type.ElementType);
        }
        else
            Assert.Equal(kind, boundCall.Type.Name.ToUpperInvariant());
        var output = !isArray ? "" : kind == "array2"
            ? "\nFOR i = LBOUND(matrix, 1) TO UBOUND(matrix, 1)\nFOR j = LBOUND(matrix, 2) TO UBOUND(matrix, 2)\nPRINT matrix(i, j)\nNEXT j\nNEXT i"
            : "\nFOR i = LBOUND(result) TO UBOUND(result)\nPRINT result(i)\nNEXT i";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\n" + statement + output));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
}
