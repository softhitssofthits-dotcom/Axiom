using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class RegexMatchUnicodeFormEvidenceTests
{
    [Theory]
    [InlineData("REGEXISMATCH(\"😀\", \".\")", "RegexIsMatch", "BOOLEAN")]
    [InlineData("REGEXISMATCH(\"😀\", \".\", \"s\")", "RegexIsMatchModes", "BOOLEAN")]
    [InlineData("REGEXMATCH(\"😀\", \".\")", "RegexMatch", "STRING")]
    [InlineData("REGEXMATCH(\"😀\", \".\", \"s\")", "RegexMatchModes", "STRING")]
    public void MatchBindsExactBuiltinAndType(string expression, string id, string type)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(type, call.Type.Name.ToUpperInvariant());
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MatchPreservesUtf16UnitsAndNul(ExecutionMode mode)
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            PRINT REGEXMATCH("😀", ".") = high
            PRINT REGEXMATCH("😀", ".", "s") = high
            PRINT REGEXMATCH(high + nul + low, nul) = nul
            PRINT REGEXMATCH(high + nul + low, low, "s") = low
            PRINT REGEXISMATCH(high + nul + low, "😀")
            PRINT REGEXISMATCH(high + nul + low, "😀", "s")
            PRINT REGEXISMATCH("é", "é")
            PRINT REGEXISMATCH("é", "é", "i")
            PRINT REGEXMATCH("é", "é") = ""
            PRINT REGEXMATCH("é", "é", "i") = ""
            PRINT REGEXISMATCH("", "")
            PRINT REGEXMATCH("", "", "s") = ""
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "FALSE", "FALSE", "FALSE", "TRUE", "TRUE", "TRUE", "TRUE" }, run.Output);
    }
}
