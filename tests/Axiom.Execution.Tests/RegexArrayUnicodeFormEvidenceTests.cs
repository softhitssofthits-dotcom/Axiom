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

    [Fact]
    public void ArraysPreserveUtf16UnitsAndCapturedSeparators()
    {
        var source = "DIM high AS STRING = LEFT(\"😀\", 1)\r\nDIM low AS STRING = RIGHT(\"😀\", 1)\r\nDIM nul AS STRING = CHRW(0)\r\nDIM values[] AS STRING\r\nvalues[] = REGEXMATCHES(high + nul + low, \".\")\r\nPRINT LBOUND(values[])\r\nPRINT UBOUND(values[])\r\nPRINT values[1] = high\r\nPRINT values[2] = nul\r\nPRINT values[3] = low\r\nvalues[] = REGEXMATCHES(\"😀\", \"\", \"s\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT JOIN(values[], \"|\") = \"||\"\r\nvalues[] = REGEXSPLIT(\"😀\", \"\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[1] = \"\"\r\nPRINT values[2] = high\r\nPRINT values[3] = low\r\nPRINT values[4] = \"\"\r\nvalues[] = REGEXSPLIT(high + nul + low, \"(.)\", \"s\")\r\nPRINT ARRAYLENGTH(values[])\r\nPRINT values[2] = high\r\nPRINT values[4] = nul\r\nPRINT values[6] = low\r\nPRINT values[1] = \"\"\r\nPRINT values[7] = \"\"\r\nvalues[] = REGEXMATCHES(\"é\", \"é\", \"i\")\r\nPRINT ARRAYISALLOCATED(values[])\r\nPRINT ARRAYLENGTH(values[])";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "1", "3", "TRUE", "TRUE", "TRUE", "3", "TRUE", "4", "TRUE", "TRUE", "TRUE", "TRUE", "7", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "0" }, run.Output);
    }
}
