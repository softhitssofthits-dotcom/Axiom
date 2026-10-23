namespace Axiom.Execution.Tests;

public sealed class ArrayCallbackBoundaryContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        yield return new object[] { "PRINT ARRAYEVERY(a[], IsPositive)", new[] { "TRUE", "1" } };
        yield return new object[] { "PRINT ARRAYSOME(a[], IsNegative)", new[] { "FALSE", "1" } };
        yield return new object[] { "result[] = ARRAYFILTER(a[], IsPositive)\nPRINT ARRAYLENGTH(result[])\nPRINT result[1]", new[] { "1", "42", "1" } };
        yield return new object[] { "result[] = ARRAYMAP(a[], Twice)\nPRINT LBOUND(result[])\nPRINT UBOUND(result[])\nPRINT result[2147483647]", new[] { "2147483647", "2147483647", "84", "1" } };
        yield return new object[] { "PRINT ARRAYREDUCE(a[], 8, Add)", new[] { "50", "1" } };
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CallbackAtMaximumIndexRunsExactlyOnce(string statement, string[] expected)
    {
        var source = "DIM a[2147483647 TO 2147483647] AS INTEGER\nDIM result[] AS INTEGER\nDIM calls AS INTEGER\na[2147483647] = 42\n" + statement + "\nPRINT calls\n" + """
FUNCTION IsPositive(value AS INTEGER) AS BOOLEAN
    calls = calls + 1
    RETURN value > 0
END FUNCTION
FUNCTION IsNegative(value AS INTEGER) AS BOOLEAN
    calls = calls + 1
    RETURN value < 0
END FUNCTION
FUNCTION Twice(value AS INTEGER) AS INTEGER
    calls = calls + 1
    RETURN value * 2
END FUNCTION
FUNCTION Add(acc AS INTEGER, value AS INTEGER) AS INTEGER
    calls = calls + 1
    RETURN acc + value
END FUNCTION
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(expected, run.Output);
        }
    }
    [Theory]
    [InlineData("PRINT ARRAYEVERY(a[], Positive)", "TRUE")]
    [InlineData("PRINT ARRAYSOME(a[], Positive)", "FALSE")]
    [InlineData("result[] = ARRAYFILTER(a[], Positive)\nPRINT ARRAYLENGTH(result[])", "0")]
    [InlineData("result[] = ARRAYMAP(a[], DoubleValue)\nPRINT ARRAYLENGTH(result[])", "0")]
    [InlineData("PRINT ARRAYREDUCE(a[], 8, Sum)", "8")]
    public void EmptyArrayDoesNotInvokeCallback(string statement, string expected)
    {
        var source = "DIM seed[1 TO 1] AS INTEGER\nDIM a[] AS INTEGER\nDIM result[] AS INTEGER\nDIM calls AS INTEGER\na[] = ARRAYTAKE(seed[], 0)\n" + statement + "\nPRINT calls\n" + """
FUNCTION Positive(value AS INTEGER) AS BOOLEAN
    calls = calls + 1
    RETURN value > 0
END FUNCTION
FUNCTION DoubleValue(value AS INTEGER) AS INTEGER
    calls = calls + 1
    RETURN value * 2
END FUNCTION
FUNCTION Sum(acc AS INTEGER, value AS INTEGER) AS INTEGER
    calls = calls + 1
    RETURN acc + value
END FUNCTION
""";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { expected, "0" }, run.Output);
        }
    }

    [Theory]
    [InlineData("ARRAYEVERY", 0, "FALSE")]
    [InlineData("ARRAYSOME", 42, "TRUE")]
    public void PredicateStopsAfterFirstDecisiveElement(string function, int value, string expected)
    {
        var source = $"DIM a[5 TO 7] AS INTEGER\nDIM calls AS INTEGER\na[5] = {value}\nPRINT {function}(a[], Positive)\nPRINT calls\nFUNCTION Positive(value AS INTEGER) AS BOOLEAN\n calls = calls + 1\n RETURN value > 0\nEND FUNCTION";

        {
            var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
            Assert.Equal(new[] { expected, "1" }, run.Output);
        }
    }

}
