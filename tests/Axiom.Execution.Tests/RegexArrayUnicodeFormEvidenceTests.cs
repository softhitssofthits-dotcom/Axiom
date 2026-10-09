using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class RegexArrayUnicodeFormEvidenceTests
{
    [Theory]
    [InlineData("REGEXMATCHES(\"😀\", \".\")", "RegexMatches")]
    [InlineData("REGEXMATCHES(\"😀\", \".\", \"s\")", "RegexMatchesModes")]
    [InlineData("REGEXSPLIT(\"😀\", \"\")", "RegexSplit")]
    [InlineData("REGEXSPLIT(\"😀\", \"\", \"s\")", "RegexSplitModes")]
    public void ArrayFormBindsExactBuiltinAndStringVector(string expression, string id)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
        Assert.Equal(1, type.Rank);
        Assert.Equal(TypeSymbol.String, type.ElementType);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ArraysPreserveUtf16UnitsAndCapturedSeparators(ExecutionMode mode)
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            DIM values() AS STRING
            values = REGEXMATCHES(high + nul + low, ".")
            PRINT LBOUND(values)
            PRINT UBOUND(values)
            PRINT values(1) = high
            PRINT values(2) = nul
            PRINT values(3) = low
            values = REGEXMATCHES("😀", "", "s")
            PRINT ARRAYLENGTH(values)
            PRINT JOIN(values, "|") = "||"
            values = REGEXSPLIT("😀", "")
            PRINT ARRAYLENGTH(values)
            PRINT values(1) = ""
            PRINT values(2) = high
            PRINT values(3) = low
            PRINT values(4) = ""
            values = REGEXSPLIT(high + nul + low, "(.)", "s")
            PRINT ARRAYLENGTH(values)
            PRINT values(2) = high
            PRINT values(4) = nul
            PRINT values(6) = low
            PRINT values(1) = ""
            PRINT values(7) = ""
            values = REGEXMATCHES("é", "é", "i")
            PRINT ARRAYISALLOCATED(values)
            PRINT ARRAYLENGTH(values)
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "1", "3", "TRUE", "TRUE", "TRUE", "3", "TRUE", "4", "TRUE", "TRUE", "TRUE", "TRUE", "7", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "0" }, run.Output);
    }
}
