namespace Axiom.Execution.Tests;

public sealed class PlannedSyntaxEvidenceTests
{
    public static IEnumerable<object[]> UnsupportedForms()
    {
        var forms = new (string Name, string Source)[]
        {
            ("array-literal", "DIM values[] AS INTEGER = {1, 2}\nPRINT values[1]"),
            ("multiline-string", "DIM text AS STRING = \"\"\"первая\nвторая\"\"\"\nPRINT text"),
            ("dictionary-literal", "DIM ages[] = {\"Алиса\": 25}\nPRINT ages[\"Алиса\"]"),
            ("default-parameter", "FUNCTION Plus(value AS INTEGER, extra AS INTEGER = 1) AS INTEGER\nRETURN value + extra\nEND FUNCTION\nPRINT Plus(2)"),
            ("lambda", "DIM values[1 TO 2] AS INTEGER\nDIM result[] AS INTEGER\nresult[] = ARRAYMAP(values[], x => x * 2)"),
            ("data", "DATA 1, 2"),
            ("read", "DIM value AS INTEGER\nREAD value"),
            ("restore", "RESTORE"),
            ("line-input", "DIM text AS STRING\nLINE INPUT text"),
        };

        foreach (var form in forms) yield return [form.Name, form.Source];
    }

    [Theory]
    [MemberData(nameof(UnsupportedForms))]
    public void ProposedSyntaxIsRejectedBeforeExecution(string name, string source)
    {
        Assert.NotEmpty(name);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.False(result.Succeeded);
        Assert.Empty(result.Output);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.True(
            diagnostic.Code.StartsWith("MB0", StringComparison.Ordinal) ||
            diagnostic.Code.StartsWith("MB1", StringComparison.Ordinal) ||
            diagnostic.Code.StartsWith("MB2", StringComparison.Ordinal), diagnostic.Code));
    }

    public static IEnumerable<object[]> Alternatives()
    {
        var cases = new (string Source, string[] Expected)[]
        {
            ("DIM values[-1 TO 1] AS INTEGER\nvalues[-1] = 10\nvalues[0] = 20\nvalues[1] = 30\nFOR index = LBOUND(values[]) TO UBOUND(values[])\nPRINT values[index]\nNEXT index", ["10", "20", "30"]),
            ("DIM values[1 TO 2] AS INTEGER\nvalues[1] = 3\nvalues[2] = 7\nDIM result[] AS INTEGER\nresult[] = ARRAYMAP(values[], Twice)\nPRINT result[1]\nPRINT result[2]\nFUNCTION Twice(value AS INTEGER) AS INTEGER\nRETURN value * 2\nEND FUNCTION", ["6", "14"]),
            ("FUNCTION Plus(value AS INTEGER, extra AS INTEGER) AS INTEGER\nRETURN value + extra\nEND FUNCTION\nFUNCTION PlusOne(value AS INTEGER) AS INTEGER\nRETURN Plus(value, 1)\nEND FUNCTION\nPRINT PlusOne(2)\nPRINT Plus(2, 4)\nPRINT \"первая\" + CHRW(10) + \"вторая\"", ["3", "6", "первая\nвторая"])
        };

        for (var index = 0; index < cases.Length; index++) yield return [index, cases[index].Source, cases[index].Expected];
    }

    [Theory]
    [MemberData(nameof(Alternatives))]
    public void ExistingFormsSolveLimitedRelatedTasks(int index, string source, string[] expected)
    {
        Assert.InRange(index, 0, 2);
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(expected, result.Output);
    }
}
