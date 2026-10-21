using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class TemporalStrictParsingTests
{
    public static IEnumerable<object[]> TextCases()
    {
        foreach (var (type, good, bad) in new[] {
            ("DATE", "2024-02-29", "2024-02-30"),
            ("TIME", "12:34:56", "24:34:56"),
            ("DATETIME", "2024-02-29 12:34:56", "2024-02-30 12:34:56"),
            ("UTCDATETIME", "2024-02-29T12:34:56Z", "2024-02-30T12:34:56Z") })
        {
            yield return [type, good, true];
            yield return [type, "", false];
            yield return [type, bad, false];
            yield return [type, good + "\0", false];
            yield return [type, good.Insert(3, " "), false];
            if (type == "DATE") continue;
            var body = type == "UTCDATETIME" ? good[..^1] : good;
            var end = type == "UTCDATETIME" ? "Z" : "";
            yield return [type, body + "." + end, false];
            yield return [type, body + ".12345678" + end, false];
            yield return [type, body + ".1" + end, true];
            yield return [type, body + ".1234567" + end, true];
        }
    }

    [Theory]
    [MemberData(nameof(TextCases))]
    public void AssignmentUsesStrictTemporalText(string type, string text, bool valid)
    {
        var source = $"DIM raw AS STRING = \"{text}\"\nDIM value AS {type}\nTRY\nvalue = raw\nPRINT value\nCATCH err\nPRINT err.Code\nEND TRY";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal([valid ? text : "MB3005"], result.Output);
    }

    [Theory]
    [MemberData(nameof(TextCases))]
    public void InputUsesStrictTextAndRemainsAtomic(string type, string text, bool valid)
    {
        var source = $"DIM first AS INTEGER = 7\nDIM value AS {type}\nTRY\nINPUT first, value\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT first\nPRINT value";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, inputProvider: new TextInput("99," + text)));
        Assert.True(result.Succeeded, string.Join(" | ", result.Diagnostics.Select(d => d.Message)));
        var defaultText = type switch { "DATE" => "0001-01-01", "TIME" => "00:00:00", "DATETIME" => "0001-01-01 00:00:00", _ => "0001-01-01T00:00:00Z" };
        Assert.Equal(valid ? new[] { "99", text } : new[] { "MB3003", "7", defaultText }, result.Output);
    }

    [Theory]
    [MemberData(nameof(TextCases))]
    public void FieldDefaultUsesSameStrictText(string type, string text, bool valid)
    {
        var source = $"TYPE Entry\nValue AS {type} = \"{text}\"\nEND TYPE\nDIM item AS Entry\nPRINT item.Value";
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(valid, result.Succeeded);
        if (valid) Assert.Equal([text], result.Output);
        else Assert.Contains(result.Diagnostics, d => d.Code == "MB2027");
    }

    [Fact]
    public void SurroundingSpacesFollowExistingSingleAndMultipleInputRules()
    {
        foreach (var type in new[] { "DATE", "TIME", "DATETIME", "UTCDATETIME" })
        {
            var text = type switch { "DATE" => "2024-02-29", "TIME" => "12:34:56", "DATETIME" => "2024-02-29 12:34:56", _ => "2024-02-29T12:34:56Z" };
            foreach (var padded in new[] { " " + text, text + " " })
            {
                AssignmentUsesStrictTemporalText(type, padded, false);
                FieldDefaultUsesSameStrictText(type, padded, false);
                var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"DIM value AS {type}\nTRY\nINPUT value\nCATCH err\nPRINT err.Code\nEND TRY", inputProvider: new TextInput(padded)));
                Assert.True(result.Succeeded);
                Assert.Equal(["MB3003"], result.Output);
                var multiple = new AxiomExecutionPipeline().Run(new ExecutionRequest($"DIM first AS INTEGER\nDIM value AS {type}\nINPUT first, value\nPRINT first\nPRINT value", inputProvider: new TextInput("99," + padded)));
                Assert.True(multiple.Succeeded);
                Assert.Equal(["99", text], multiple.Output);
            }
        }
    }

    private sealed class TextInput(string text) : IInputProvider
    {
        public string ReadInput(InputRequest request) => text;
    }
}
