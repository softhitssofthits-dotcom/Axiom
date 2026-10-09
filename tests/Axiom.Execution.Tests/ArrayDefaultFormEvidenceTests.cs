using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArrayDefaultFormEvidenceTests
{
    private const string Setup = """
DIM a(5 TO 7) AS INTEGER
DIM result() AS INTEGER
a(5) = 30
a(6) = 10
a(7) = 20
""";

    public static IEnumerable<object[]> Forms()
    {
        yield return new object[] { "ArrayToSorted", "result = ARRAYTOSORTED(a)", "array", "result", new[] { "10", "20", "30", "30" } };
        yield return new object[] { "ArrayToReversed", "result = ARRAYTOREVERSED(a)", "array", "result", new[] { "20", "10", "30", "30" } };
        yield return new object[] { "ArraySort", "ARRAYSORT(a)", "void", "a", new[] { "10", "20", "30", "10" } };
        yield return new object[] { "ArrayReverse", "ARRAYREVERSE(a)", "void", "a", new[] { "20", "10", "30", "20" } };
        yield return new object[] { "ArrayPush", "ARRAYPUSH(a, 40)", "void", "a", new[] { "30", "10", "20", "40", "30" } };
        yield return new object[] { "ArrayInsert", "ARRAYINSERT(a, 6, 40)", "void", "a", new[] { "30", "40", "10", "20", "30" } };
        yield return new object[] { "ArrayPop", "PRINT ARRAYPOP(a)", "scalar", "a", new[] { "20", "30", "10", "30" } };
        yield return new object[] { "ArrayRemove", "PRINT ARRAYREMOVE(a, 6)", "scalar", "a", new[] { "10", "30", "20", "30" } };
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void DefaultFormBindsToExpectedBuiltinAndProducesExpectedValues(string id, string statement, string kind, string target, string[] expected)
    {
        var syntax = Parser.Parse(Setup + "\n" + statement, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var final = program.GlobalStatements.Last();
        var expression = kind == "array"
            ? Assert.IsType<BoundAssignmentStatement>(final).Expression
            : kind == "scalar"
                ? Assert.Single(Assert.IsType<BoundPrintStatement>(final).Items).Expression
                : Assert.IsType<BoundExpressionStatement>(final).Expression;
        var call = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        if (kind == "array")
        {
            var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
            Assert.Equal(1, type.Rank);
            Assert.Equal(TypeSymbol.Integer, type.ElementType);
        }
        else if (kind == "scalar")
            Assert.Equal(TypeSymbol.Integer, call.Type);
        else
            Assert.Equal("VOID", call.Type.Name.ToUpperInvariant());
        var source = Setup + "\n" + statement + $"\nFOR i = LBOUND({target}) TO UBOUND({target})\nPRINT {target}(i)\nNEXT i\nPRINT a(5)";
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.True(run.Succeeded, mode + "\n" + string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.False(run.UsedInterpreterFallback);
            Assert.Equal(expected, run.Output);
        }
    }
}
