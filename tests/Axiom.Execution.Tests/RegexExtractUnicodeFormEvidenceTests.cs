using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class RegexExtractUnicodeFormEvidenceTests
{
    [Theory]
    [InlineData("REGEXEXTRACT(\"😀\", \"(.)\", 1)", "RegexExtract", false)]
    [InlineData("REGEXEXTRACT(\"😀\", \"(.)\", 1, \"s\")", "RegexExtractModes", false)]
    [InlineData("REGEXEXTRACTALL(\"😀\", \"(.)\", 1)", "RegexExtractAll", true)]
    [InlineData("REGEXEXTRACTALL(\"😀\", \"(.)\", 1, \"s\")", "RegexExtractAllModes", true)]
    public void ExtractBindsExactBuiltinAndResult(string expression, string id, bool array)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        if (array)
        {
            var type = Assert.IsType<ArrayTypeSymbol>(call.Type);
            Assert.Equal(1, type.Rank);
            Assert.Equal(TypeSymbol.String, type.ElementType);
        }
        else Assert.Equal(TypeSymbol.String, call.Type);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void GroupsPreserveMalformedUnitsAndExplicitNumbers(ExecutionMode mode)
    {
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM nul AS STRING = CHRW(0)
            DIM values() AS STRING
            PRINT REGEXEXTRACT("😀", "(.)", 1) = high
            PRINT REGEXEXTRACT(high + nul + low, "(.)(.)(.)", 2, "s") = nul
            PRINT REGEXEXTRACT(high + nul + low, "(.)(.)(.)", 3) = low
            PRINT REGEXEXTRACT("😀", "(?<10>..)", 10, "s") = "😀"
            PRINT REGEXEXTRACT("😀", "(?<10>..)", 1) = ""
            values = REGEXEXTRACTALL(high + nul + low, "(.)", 1)
            PRINT LBOUND(values)
            PRINT UBOUND(values)
            PRINT values(1) = high
            PRINT values(2) = nul
            PRINT values(3) = low
            values = REGEXEXTRACTALL("😀😀", "(?<10>..)", 10, "s")
            PRINT ARRAYLENGTH(values)
            PRINT values(1) = "😀"
            PRINT values(2) = "😀"
            values = REGEXEXTRACTALL("é", "(é)", 1)
            PRINT ARRAYISALLOCATED(values)
            PRINT ARRAYLENGTH(values)
            """;
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.False(run.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, run.ActualEngine);
        Assert.Equal(new[] { "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "1", "3", "TRUE", "TRUE", "TRUE", "2", "TRUE", "TRUE", "TRUE", "0" }, run.Output);
    }
}
