using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackOrderContractTests
{
    private const string Setup = """
DIM a(-2 TO -1, 7 TO 8) AS INTEGER
DIM mapped(,) AS STRING
DIM filtered() AS INTEGER
DIM calls AS INTEGER
DIM trace AS STRING = ""
a(-2, 7) = 1
a(-2, 8) = 2
a(-1, 7) = 3
a(-1, 8) = 4
""";
    private const string Functions = """
FUNCTION Label(value AS INTEGER) AS STRING
    calls = calls + 1
    trace = trace + STR(value)
    RETURN "Число:" + STR(value)
END FUNCTION
FUNCTION Even(value AS INTEGER) AS BOOLEAN
    calls = calls + 1
    trace = trace + STR(value)
    RETURN value MOD 2 = 0
END FUNCTION
FUNCTION AccumulateText(acc AS STRING, value AS INTEGER) AS STRING
    calls = calls + 1
    trace = trace + STR(value)
    RETURN acc + STR(value)
END FUNCTION
FUNCTION Fault(value AS INTEGER) AS INTEGER
    calls = calls + 1
    trace = trace + STR(value)
    RETURN CINT(10 / (value - 2))
END FUNCTION
""";

    [Theory]
    [InlineData("ArrayEvery", "PRINT ARRAYEVERY(a, Even)", "BOOLEAN", 0)]
    [InlineData("ArraySome", "PRINT ARRAYSOME(a, Even)", "BOOLEAN", 0)]
    [InlineData("ArrayFilter", "filtered = ARRAYFILTER(a, Even)", "INTEGER", 1)]
    [InlineData("ArrayMap", "mapped = ARRAYMAP(a, Label)", "STRING", 2)]
    [InlineData("ArrayReduce", "PRINT ARRAYREDUCE(a, \"Начало:\", AccumulateText)", "STRING", 0)]
    [InlineData("ArrayDistinct", "filtered = ARRAYDISTINCT(a)", "INTEGER", 1)]
    public void CallableFormsBindExactBuiltinAndPolymorphicResult(string id, string statement, string elementOrScalarType, int rank)
    {
        var syntax = Parser.Parse(Setup + "\n" + statement + "\n" + Functions, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var final = program.GlobalStatements.Last();
        var expression = rank == 0
            ? Assert.Single(Assert.IsType<BoundPrintStatement>(final).Items).Expression
            : Assert.IsType<BoundAssignmentStatement>(final).Expression;
        var call = Assert.IsType<BoundCallExpression>(expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        if (rank == 0) Assert.Equal(elementOrScalarType, call.Type.Name.ToUpperInvariant());
        else
        {
            var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
            Assert.Equal(rank, type.Rank);
            Assert.Equal(elementOrScalarType, type.ElementType.Name.ToUpperInvariant());
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "PRINT ARRAYEVERY(a, Even)", new[] { "FALSE", "1", "1" } };
        yield return new object[] { "PRINT ARRAYSOME(a, Even)", new[] { "TRUE", "2", "12" } };
        yield return new object[] { "mapped = ARRAYMAP(a, Label)\nPRINT LBOUND(mapped, 1)\nPRINT UBOUND(mapped, 1)\nPRINT LBOUND(mapped, 2)\nPRINT UBOUND(mapped, 2)\nPRINT mapped(-2, 7)\nPRINT mapped(-2, 8)\nPRINT mapped(-1, 7)\nPRINT mapped(-1, 8)", new[] { "-2", "-1", "7", "8", "Число:1", "Число:2", "Число:3", "Число:4", "4", "1234" } };
        yield return new object[] { "filtered = ARRAYFILTER(a, Even)\nPRINT LBOUND(filtered)\nPRINT UBOUND(filtered)\nPRINT filtered(1)\nPRINT filtered(2)", new[] { "1", "2", "2", "4", "4", "1234" } };
        yield return new object[] { "PRINT ARRAYREDUCE(a, \"Начало:\", AccumulateText)", new[] { "Начало:1234", "4", "1234" } };
        yield return new object[] { "filtered = ARRAYDISTINCT(a)\nPRINT LBOUND(filtered)\nPRINT UBOUND(filtered)\nFOR i = 1 TO 4\nPRINT filtered(i)\nNEXT i", new[] { "1", "4", "1", "2", "3", "4", "0", "" } };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MultidimensionalOrderAndPolymorphicResultsMatchIndependentExpectations(string statement, string[] expected)
    {
        CheckBoth(Setup + "\n" + statement + "\nPRINT calls\nPRINT trace\n" + Functions, expected);
    }

    [Fact]
    public void CallbackFailureIsCatchableAndStopsBeforeLaterElements()
    {
        var source = Setup + "\nDIM result(,) AS INTEGER\nTRY\nresult = ARRAYMAP(a, Fault)\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT calls\nPRINT trace\nPRINT a(-2, 7)\nPRINT a(-2, 8)\nPRINT a(-1, 7)\nPRINT a(-1, 8)\n" + Functions;
        CheckBoth(source, new[] { "MB3001", "2", "12", "1", "2", "3", "4" });
    }

    [Theory]
    [InlineData("ARRAYEVERY(a, Wrong)", "FUNCTION Wrong(value AS INTEGER) AS INTEGER\nRETURN value\nEND FUNCTION")]
    [InlineData("ARRAYSOME(a, Wrong)", "FUNCTION Wrong(value AS STRING) AS BOOLEAN\nRETURN TRUE\nEND FUNCTION")]
    [InlineData("ARRAYFILTER(a, Wrong)", "FUNCTION Wrong(value AS INTEGER, other AS INTEGER) AS BOOLEAN\nRETURN TRUE\nEND FUNCTION")]
    [InlineData("ARRAYMAP(a, Wrong)", "FUNCTION Wrong(value AS STRING) AS STRING\nRETURN value\nEND FUNCTION")]
    [InlineData("ARRAYREDUCE(a, 0, Wrong)", "FUNCTION Wrong(acc AS INTEGER) AS INTEGER\nRETURN acc\nEND FUNCTION")]
    public void IncompatibleCallbackSignaturesAreRejectedBeforeExecution(string call, string function)
    {
        var source = Setup + "\nPRINT " + call + "\n" + function;
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.NotEmpty(second.Diagnostics);
        Assert.All(second.Diagnostics, d => Assert.StartsWith("MB2", d.Code));
    }


    [Fact]
    public void CallbackCanHandleItsOwnFailureAndRunFinallyForEveryElement()
    {
        var source = Setup + "\nDIM result(,) AS INTEGER\nDIM finalized AS INTEGER\nresult = ARRAYMAP(a, Recover)\nPRINT result(-2, 7)\nPRINT result(-2, 8)\nPRINT result(-1, 7)\nPRINT result(-1, 8)\nPRINT finalized\n" + """
FUNCTION Recover(value AS INTEGER) AS INTEGER
    TRY
        RETURN CINT(10 / (value - 2))
    CATCH err
        RETURN 99
    FINALLY
        finalized = finalized + 1
    END TRY
END FUNCTION
""";
        CheckBoth(source, new[] { "-10", "99", "10", "5", "4" });
    }

    [Fact]
    public void NestedCallbackFailureUnwindsFinallyBeforeOuterCatch()
    {
        var source = Setup + "\nDIM result(,) AS INTEGER\nDIM finalized AS INTEGER\nTRY\nresult = ARRAYMAP(a, Nested)\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT calls\nPRINT trace\nPRINT finalized\n" + """
FUNCTION Nested(value AS INTEGER) AS INTEGER
    calls = calls + 1
    trace = trace + STR(value)
    DIM one(1 TO 1) AS INTEGER
    one(1) = value
    TRY
        RETURN ARRAYREDUCE(one, 0, NestedFault)
    FINALLY
        finalized = finalized + 1
    END TRY
END FUNCTION
FUNCTION NestedFault(acc AS INTEGER, value AS INTEGER) AS INTEGER
    RETURN acc + CINT(10 / (value - 2))
END FUNCTION
""";
        CheckBoth(source, new[] { "MB3001", "2", "12", "2" });
    }

    [Fact]
    public void UnhandledCallbackFailurePreservesOriginalDiagnosticLocation()
    {
        var source = Setup + "\nDIM result(,) AS INTEGER\nresult = ARRAYMAP(a, Fault)\n" + Functions;
        var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(second.Succeeded);
        Assert.Equal("MB3001", Assert.Single(second.Diagnostics).Code);
    }

    private static void CheckBoth(string source, string[] expected)
    {

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
}
