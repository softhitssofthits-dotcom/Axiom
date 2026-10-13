using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class NextCounterBindingTests
{
    [Theory]
    [InlineData("")]
    [InlineData("counter")]
    [InlineData("COUNTER")]
    [InlineData("counter 'Комментарий после NEXT")]
    [InlineData("'Комментарий после NEXT")]
    public void Bind_AcceptsOmittedOrMatchingNextCounter(string nextName)
    {
        var source = $"FOR counter = 1 TO 2\nPRINT counter\nNEXT {nextName}";
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundForStatement>(Assert.Single(program.GlobalStatements));
    }

    [Fact]
    public void Bind_AcceptsNamesOfBothNestedCounters()
    {
        const string source = "FOR outer = 1 TO 2\nFOR inner = 1 TO 2\nPRINT outer, inner\nNEXT INNER\nNEXT OUTER";
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Bind_AcceptsLineAsBothForAndNextCounterName()
    {
        const string source = "FOR line = 1 TO 2\nPRINT line\nNEXT LINE";
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        var program = Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundForStatement>(Assert.Single(program.GlobalStatements));
    }

    [Theory]
    [InlineData("", "missing")]
    [InlineData("", "line")]
    [InlineData("DIM wrong AS INTEGER\n", "wrong")]
    [InlineData("FOR outer = 1 TO 2\n", "outer")]
    public void Bind_ReportsMismatchedNextCounterAtItsIdentifier(string prefix, string nextName)
    {
        var suffix = prefix.StartsWith("FOR", StringComparison.Ordinal) ? "\nNEXT outer" : "";
        var source = $"{prefix}FOR counter = 1 TO 2\nPRINT counter\nNEXT {nextName}{suffix}";
        var syntax = Parser.Parse(source, out var parserDiagnostics);
        Assert.Empty(parserDiagnostics);

        Binder.Bind(syntax, out var diagnostics, checkGraphicsModes: false);

        var error = Assert.Single(diagnostics, d => d.Code == "MB2048");
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal($"Имя счётчика после NEXT '{nextName}' должно совпадать с именем счётчика FOR 'counter'.", error.Message);
        Assert.Equal(source.IndexOf($"NEXT {nextName}", StringComparison.Ordinal) + "NEXT ".Length, error.Location.Span.Start);
        Assert.Equal(nextName.Length, error.Location.Length);
        Assert.DoesNotContain(diagnostics, d => d.Code == DiagnosticCodes.UnknownName);
    }

    [Theory]
    [InlineData("", "extra", "extra")]
    [InlineData("DIM extra AS INTEGER\n", "extra", "extra")]
    [InlineData("", "PRINT 1", "PRINT")]
    [InlineData("", "1", "1")]
    [InlineData("", "+ 1", "+")]
    public void Parse_RejectsUnexpectedTokensAfterNextCounter(string prefix, string tail, string token)
    {
        var source = $"{prefix}FOR counter = 1 TO 2\nPRINT counter\nNEXT counter {tail}";

        Parser.Parse(source, out var diagnostics);

        var error = Assert.Single(diagnostics, d => d.Code == DiagnosticCodes.UnexpectedToken);
        Assert.Equal(source.LastIndexOf(tail, StringComparison.Ordinal), error.Location.Span.Start);
        Assert.Equal(token.Length, error.Location.Length);
    }
}
