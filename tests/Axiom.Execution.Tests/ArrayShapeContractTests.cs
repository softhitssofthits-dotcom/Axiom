using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArrayShapeContractTests
{
    private const string Setup = """
DIM m(-2 TO -1, 4 TO 6) AS INTEGER
DIM flat() AS INTEGER
DIM shaped(,) AS INTEGER
DIM template(9 TO 11, -4 TO -3) AS INTEGER
m(-2, 4) = 11
m(-2, 5) = 12
m(-2, 6) = 13
m(-1, 4) = 21
m(-1, 5) = 22
m(-1, 6) = 23
""";

    public static IEnumerable<object[]> ValidCases()
    {
        yield return new object[] { "flat = ARRAYFLATTEN(m)\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nFOR i = 1 TO 6\nPRINT flat(i)\nNEXT i\nflat(1) = 99\nPRINT m(-2, 4)", new[] { "1", "6", "11", "12", "13", "21", "22", "23", "11" } };
        yield return new object[] { "flat = ARRAYFLATTEN(m(-1))\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nPRINT flat(1)\nPRINT flat(3)\nflat(1) = 99\nPRINT m(-1, 4)", new[] { "1", "3", "21", "23", "21" } };
        yield return new object[] { "shaped = ARRAYRESHAPE(m, template)\nPRINT LBOUND(shaped, 1)\nPRINT UBOUND(shaped, 1)\nPRINT LBOUND(shaped, 2)\nPRINT UBOUND(shaped, 2)\nFOR i = 9 TO 11\nFOR j = -4 TO -3\nPRINT shaped(i, j)\nNEXT j\nNEXT i\nshaped(9, -4) = 99\nPRINT m(-2, 4)\nPRINT template(9, -4)", new[] { "9", "11", "-4", "-3", "11", "12", "13", "21", "22", "23", "11", "0" } };
        yield return new object[] { "flat = ARRAYRESHAPE(m(-1), m(-2))\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nPRINT flat(4)\nPRINT flat(6)\nflat(4) = 99\nPRINT m(-1, 4)\nPRINT m(-2, 4)", new[] { "4", "6", "21", "23", "21", "11" } };
        yield return new object[] { "shaped = ARRAYCONCAT(m, m, 1)\nPRINT LBOUND(shaped, 1)\nPRINT UBOUND(shaped, 1)\nPRINT LBOUND(shaped, 2)\nPRINT UBOUND(shaped, 2)\nPRINT shaped(-2, 4)\nPRINT shaped(-1, 6)\nPRINT shaped(0, 4)\nPRINT shaped(1, 6)\nshaped(-2, 4) = 99\nPRINT m(-2, 4)", new[] { "-2", "1", "4", "6", "11", "23", "11", "23", "11" } };
        yield return new object[] { "shaped = ARRAYCONCAT(m, m, 2)\nPRINT LBOUND(shaped, 1)\nPRINT UBOUND(shaped, 1)\nPRINT LBOUND(shaped, 2)\nPRINT UBOUND(shaped, 2)\nPRINT shaped(-2, 4)\nPRINT shaped(-2, 7)\nPRINT shaped(-1, 9)", new[] { "-2", "-1", "4", "9", "11", "11", "23" } };
        yield return new object[] { "flat = ARRAYCONCAT(m(-2), m(-1))\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nPRINT flat(1)\nPRINT flat(6)", new[] { "1", "6", "11", "23" } };
        yield return new object[] { "DIM empty(,) AS INTEGER\nempty = ARRAYTAKE(m, 0, 1)\nflat = ARRAYFLATTEN(empty)\nshaped = ARRAYRESHAPE(flat, empty)\nPRINT ARRAYISALLOCATED(flat)\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nPRINT ARRAYLENGTH(shaped)\nPRINT ARRAYRANK(shaped)\nPRINT LBOUND(shaped, 1)\nPRINT UBOUND(shaped, 1)\nPRINT LBOUND(shaped, 2)\nPRINT UBOUND(shaped, 2)", new[] { "TRUE", "1", "0", "0", "2", "-2", "-3", "4", "6" } };
        yield return new object[] { "DIM empty(,) AS INTEGER\nempty = ARRAYTAKE(m, 0, 1)\nshaped = ARRAYCONCAT(empty, m, 1)\nPRINT ARRAYLENGTH(shaped)\nPRINT LBOUND(shaped, 1)\nPRINT UBOUND(shaped, 1)\nPRINT shaped(-1, 6)", new[] { "6", "-2", "-1", "23" } };
        yield return new object[] { "DIM empty() AS INTEGER\nempty = ARRAYTAKE(m(-2), 0)\nflat = ARRAYCONCAT(empty, m(-1))\nPRINT LBOUND(flat)\nPRINT UBOUND(flat)\nPRINT flat(4)\nPRINT flat(6)", new[] { "4", "6", "21", "23" } };
    }

    [Theory]
    [MemberData(nameof(ValidCases))]
    public void ShapeOperationsProduceIndependentExpectedResults(string body, string[] expected)
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\n" + body, mode: mode));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(expected, run.Output);
        }
    }

    [Theory]
    [InlineData("shaped = ARRAYRESHAPE(m(-2), template)")]
    [InlineData("shaped = ARRAYCONCAT(m, template, 1)")]
    [InlineData("shaped = ARRAYCONCAT(m, template, 2)")]
    public void IncompatibleSizesAreCatchableAndPreserveInputs(string statement)
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(Setup + "\nTRY\n" + statement + "\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT m(-2, 4)\nPRINT ARRAYLENGTH(m)\nPRINT template(9, -4)", mode: mode));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(new[] { "MB3007", "11", "6", "0" }, run.Output);
        }
    }
    [Theory]
    [InlineData("flat = ARRAYFLATTEN(pending)")]
    [InlineData("shaped = ARRAYRESHAPE(pending, template)")]
    [InlineData("shaped = ARRAYRESHAPE(m, pendingMatrix)")]
    [InlineData("flat = ARRAYCONCAT(pending, m(-2))")]
    [InlineData("flat = ARRAYCONCAT(m(-2), pending)")]
    public void UnallocatedArgumentsReportMatchingLocations(string statement)
    {
        var source = Setup + "\nDIM pending() AS INTEGER\nDIM pendingMatrix(,) AS INTEGER\n" + statement;
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.False(second.UsedInterpreterFallback);
        Assert.Equal("MB3005", Assert.Single(first.Diagnostics).Code);
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }

    [Theory]
    [InlineData("PRINT ARRAYFLATTEN(TRUE)")]
    [InlineData("PRINT ARRAYRESHAPE(TRUE, m)")]
    [InlineData("PRINT ARRAYRESHAPE(m, TRUE)")]
    [InlineData("PRINT ARRAYCONCAT(TRUE, m)")]
    [InlineData("PRINT ARRAYCONCAT(m, TRUE)")]
    [InlineData("PRINT ARRAYCONCAT(m, m, TRUE)")]
    [InlineData("DIM strings(1 TO 6) AS STRING\nshaped = ARRAYRESHAPE(strings, template)")]
    public void WrongTypesAreRejectedBeforeExecution(string statement)
    {
        var source = Setup + "\n" + statement;
        var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
        Assert.False(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.False(second.UsedInterpreterFallback);
        Assert.NotEmpty(first.Diagnostics);
        Assert.All(first.Diagnostics, d => Assert.StartsWith("MB2", d.Code));
        Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)), second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
    }

    [Theory]
    [InlineData("ArrayFlatten", "flat = ARRAYFLATTEN(m)", 1)]
    [InlineData("ArrayReshape", "shaped = ARRAYRESHAPE(m, template)", 2)]
    [InlineData("ArrayConcat", "flat = ARRAYCONCAT(m(-2), m(-1))", 1)]
    [InlineData("ArrayConcat", "flat = ARRAYCONCAT(m(-2), m(-1), 1)", 1)]
    [InlineData("ArrayConcat", "shaped = ARRAYCONCAT(m, m, 2)", 2)]
    public void CallableFormsResolveExactBuiltinAndResultRank(string id, string statement, int rank)
    {
        var syntax = Parser.Parse(Setup + "\n" + statement, out var parsingDiagnostics);
        Assert.Empty(parsingDiagnostics);
        var program = Binder.Bind(syntax, out var bindingDiagnostics);
        Assert.Empty(bindingDiagnostics);
        var expression = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements.Last()).Expression;
        var call = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
        Assert.Equal(rank, type.Rank);
        Assert.Equal(TypeSymbol.Integer, type.ElementType);
    }

}
