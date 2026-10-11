using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class VariableUsageAnalysisTests
{
    [Theory]
    [InlineData("DIM spare AS INTEGER = 42")]
    [InlineData("DIM spare AS INTEGER\nspare = 42")]
    [InlineData("DIM spare AS INTEGER\nINPUT spare")]
    [InlineData("SUB Work()\nDIM spare AS INTEGER = 42\nEND SUB")]
    public void ValueNeverRead_ProducesWarning(string source)
    {
        var warning = Assert.Single(Analyze(source));
        Assert.Equal(DiagnosticCodes.UnusedVariable, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("spare", warning.Message);
    }

    [Theory]
    [InlineData("DIM value AS INTEGER = 42\nPRINT value")]
    [InlineData("DIM value AS INTEGER\nINPUT value\nPRINT value")]
    [InlineData("DIM value AS INTEGER = 42\nSUB Work()\nPRINT value\nEND SUB")]
    [InlineData("DIM value(0 TO 2) AS INTEGER\nvalue(1) = 42\nPRINT value(1)")]
    [InlineData("DIM finish AS INTEGER = 3\nFOR value = 1 TO finish\nPRINT value\nNEXT")]
    public void ReadValue_DoesNotProduceWarning(string source) => Assert.Empty(Analyze(source));

    [Theory]
    [InlineData("DIM spare AS INTEGER = 42", "spare")]
    [InlineData("DIM first, second AS INTEGER", "first")]
    [InlineData("DIM first, second AS INTEGER", "second")]
    [InlineData("CONST limit AS INTEGER = 42", "limit")]
    [InlineData("' Комментарий\r\n    DIM spare AS INTEGER", "spare")]
    public void WarningRange_CoversOnlyDeclaredName(string source, string name)
    {
        var warning = Assert.Single(Analyze(source), d => d.Message.Contains($"'{name}'"));
        Assert.Equal(name, source.Substring(warning.Location.Span.Start, warning.Location.Span.Length));
    }

    private static IReadOnlyList<Diagnostic> Analyze(string source)
    {
        var syntax = Parser.Parse(source, out var syntaxDiagnostics);
        Assert.Empty(syntaxDiagnostics);
        var program = Binder.Bind(syntax, out var diagnostics);
        Assert.DoesNotContain(diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return VariableUsageAnalysis.Analyze(program);
    }
}
