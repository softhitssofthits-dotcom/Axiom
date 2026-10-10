using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class RegexReplaceUnicodeFormEvidenceTests
{
    [Theory]
    [InlineData("REGEXREPLACE(\"😀\", \".\", \"x\")", "RegexReplace")]
    [InlineData("REGEXREPLACE(\"😀\", \".\", \"x\", \"s\")", "RegexReplaceModes")]
    [InlineData("REGEXREPLACE(\"😀\", \".\", \"x\", 1)", "RegexReplaceCount")]
    [InlineData("REGEXREPLACE(\"😀\", \".\", \"x\", \"s\", 1)", "RegexReplaceModesCount")]
    public void ReplaceBindsExactBuiltinAndString(string expression, string id)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(TypeSymbol.String, call.Type);
    }

    [Fact]
    public void ReplacementKeepsUtf16UnitsAndCountLimits()
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            PRINT REGEXREPLACE("😀", ".", "[" + "$&" + "]") = "[" + high + "][" + low + "]"
            PRINT REGEXREPLACE("😀", "(.)(.)", "$2" + nul + "$1", "s") = low + nul + high
            PRINT REGEXREPLACE("😀", ".", nul, 1) = nul + low
            PRINT REGEXREPLACE("😀", ".", high, "s", 1) = "😀"
            PRINT REGEXREPLACE(high + nul + low, ".", "😀", 2147483647) = "😀😀😀"
            PRINT REGEXREPLACE(high + nul + low, ".", "😀", "s", 2147483647) = "😀😀😀"
            PRINT REGEXREPLACE("😀", "", "|", 2) = "|" + high + "|" + low
            PRINT REGEXREPLACE("😀", "", "|", "s", 2147483647) = "|" + high + "|" + low + "|"
            PRINT REGEXREPLACE("é", "é", "Замена") = "é"
            PRINT REGEXREPLACE("é", "é", "Замена", "i") = "é"
            PRINT REGEXREPLACE(high + nul + low, "[", "Замена", "q", 0) = high + nul + low
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(Enumerable.Repeat("TRUE", 11), run.Output);
    }
}
