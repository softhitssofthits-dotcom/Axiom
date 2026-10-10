namespace Axiom.Execution.Tests;

public sealed class UnicodeBoundaryEvidenceTests
{
    private static (string Text, string Unit, string[] Parts)[] Data() => new (string Text, string Unit, string[] Parts)[]
        {
            ("\"é👨‍👩‍👧‍👦🇷🇺👍🏽1️⃣\"", "GRAPHEME", ["\"é\"", "\"👨‍👩‍👧‍👦\"", "\"🇷🇺\"", "\"👍🏽\"", "\"1️⃣\""]),
            ("\"🇷🇺🇧🇾🇺\"", "GRAPHEME", ["\"🇷🇺\"", "\"🇧🇾\"", "\"🇺\""]),
            ("\"́A가\"", "GRAPHEME", ["\"́\"", "\"A\"", "\"가\""]),
            ("\"A\" + CHRW(13) + CHRW(10) + \"B\"", "GRAPHEME", ["\"A\"", "CHRW(13) + CHRW(10)", "\"B\""]),
            ("\"é😀\"", "CODEPOINT", ["\"e\"", "\"́\"", "\"😀\""]),
            ("\"\ud800\" + \"\ud800\" + \"\udc00\"", "CODEPOINT", ["\"\ud800\"", "\"\ud800\" + \"\udc00\""]),
            ("\"\udc00\" + \"\ud800\" + \"A\"", "CODEPOINT", ["\"\udc00\"", "\"\ud800\"", "\"A\""]),
            ("\"😀\"", "UTF16", ["\"\ud83d\"", "\"\ude00\""])
        };

    public static IEnumerable<object[]> Cases()
    {

        for (var index = 0; index < Data().Length; index++) yield return [index];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryExpectedBoundaryIsCheckedAgainstLiteralParts(int index)
    {
        var (text, unit, parts) = Data()[index];
        var source = new System.Text.StringBuilder($"DIM text AS STRING = {text}\nPRINT LEN(text, \"{unit}\")\n");
        for (var i = 0; i < parts.Length; i++)
        {
            source.AppendLine($"PRINT MID(text, {i + 1}, 1, \"{unit}\") = ({parts[i]})");
            source.AppendLine($"PRINT LEFT(text, {i + 1}, \"{unit}\") = ({string.Join(" + ", parts.Take(i + 1))})");
            source.AppendLine($"PRINT RIGHT(text, {parts.Length - i}, \"{unit}\") = ({string.Join(" + ", parts.Skip(i))})");
        }
        source.AppendLine($"PRINT MID(text, {parts.Length + 1}, 1, \"{unit}\") = \"\"");
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source.ToString()));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { parts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) }.Concat(Enumerable.Repeat("TRUE", parts.Length * 3 + 1)), result.Output);
    }
}
