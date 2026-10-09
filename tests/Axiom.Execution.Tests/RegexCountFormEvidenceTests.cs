using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class RegexCountFormEvidenceTests
{
    [Theory]
    [InlineData("REGEXCOUNT(\"😀\", \".\")", "RegexCount")]
    [InlineData("REGEXCOUNT(\"😀\", \".\", \"s\")", "RegexCountModes")]
    public void CountBindsExactBuiltinAndInteger(string expression, string id)
    {
        var syntax = Parser.Parse("PRINT " + expression, out var parsing);
        Assert.Empty(parsing);
        var program = Binder.Bind(syntax, out var binding);
        Assert.Empty(binding);
        var print = Assert.IsType<BoundPrintStatement>(Assert.Single(program.GlobalStatements));
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.Equal(id, Assert.IsType<BoundFunctionExpression>(call.Target).Function.BuiltinId?.ToString());
        Assert.Equal(TypeSymbol.Integer, call.Type);
    }
}
