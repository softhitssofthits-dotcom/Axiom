namespace Axiom.Execution.Tests;

public sealed class UnicodeMalformedGraphemeEvidenceTests
{
    // Повреждённые строки создаются после передачи параметров xUnit.
    private static string[][] Parts =>
    [
        ["\ud800"],
        ["\udc00"],
        ["\ud800", "\ud800"],
        ["\udc00", "\udc00"],
        ["\udc00", "\ud800"],
        ["\ud800", "😀", "\udc00"],
        ["А", "\ud800", "Б", "\udc00", "В"],
        ["\ud800", "é", "\udc00", "🇷🇺"]
    ];

    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        for (var index = 0; index < Parts.Length; index++) yield return [mode, index];
    }

    [Theory]
    [InlineData("UTF16")]
    [InlineData("CODEPOINT")]
    [InlineData("GRAPHEME")]
    public void InvalidSliceBoundsOnMalformedTextMatchAcrossEngines(string unit)
    {
        foreach (var call in new[]
        {
            $"LEFT(text, 0, \"{unit}\")", $"RIGHT(text, -1, \"{unit}\")",
            $"MID(text, 0, 1, \"{unit}\")", $"MID(text, 1, 0, \"{unit}\")"
        })
        {
            var source = "DIM text AS STRING = \"\ud800\"\nPRINT " + call;
            var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Interpreter));
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: ExecutionMode.Bytecode));
            Assert.False(first.Succeeded);
            Assert.False(second.Succeeded);
            Assert.False(second.UsedInterpreterFallback);
            Assert.Contains(first.Diagnostics, d => d.Code == "MB3007");
            Assert.Equal(first.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column)),
                second.Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column)));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GraphemeSlicesPreserveLiteralMalformedUnits(ExecutionMode mode, int index)
    {
        var parts = Parts[index];
        var text = string.Concat(parts);
        var source = new System.Text.StringBuilder($"DIM text AS STRING = \"{text}\"\nPRINT LEN(text, \"GRAPHEME\")\n");
        for (var i = 0; i < parts.Length; i++)
        {
            source.AppendLine($"PRINT MID(text, {i + 1}, 1, \"GRAPHEME\") = \"{parts[i]}\"");
            source.AppendLine($"PRINT LEFT(text, {i + 1}, \"GRAPHEME\") = \"{string.Concat(parts.Take(i + 1))}\"");
            source.AppendLine($"PRINT RIGHT(text, {parts.Length - i}, \"GRAPHEME\") = \"{string.Concat(parts.Skip(i))}\"");
        }
        source.AppendLine("PRINT LEFT(text, 2147483647, \"GRAPHEME\") = text");
        source.AppendLine("PRINT RIGHT(text, 2147483647, \"GRAPHEME\") = text");
        source.AppendLine("PRINT MID(text, 2147483647, 1, \"GRAPHEME\") = \"\"");
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source.ToString(), mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(new[] { parts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            .Concat(Enumerable.Repeat("TRUE", parts.Length * 3 + 3)), result.Output);
    }
}
