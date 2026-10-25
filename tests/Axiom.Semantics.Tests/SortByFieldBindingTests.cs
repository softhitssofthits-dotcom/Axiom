using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class SortByFieldBindingTests
{
    private const string Records =
        "TYPE Product\nName AS STRING\nPrice AS INTEGER\nEND TYPE\n";

    [Fact]
    public void SortByFieldWithAndWithoutDirectionBinds()
    {
        var program = Bind(
            Records +
            "DIM basket[1 TO 3] AS Product\n" +
            "DIM sorted[] AS Product\n" +
            "sorted[] = ARRAYTOSORTED(basket[], \"Price\")\n" +
            "ARRAYSORT(basket[], \"Name\", STEP -1)\n" +
            "ARRAYSORT(basket[], \"Price\", STEP 1)",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var sorted = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[2]);
        var sortedCall = Assert.IsType<BoundCallExpression>(sorted.Expression);
        Assert.Equal(2, sortedCall.Arguments.Count);

        var descending = Assert.IsType<BoundExpressionStatement>(program.GlobalStatements[3]);
        var descendingCall = Assert.IsType<BoundCallExpression>(descending.Expression);
        Assert.Equal(3, descendingCall.Arguments.Count);
        Assert.Equal(-1, Assert.IsType<BoundLiteralExpression>(descendingCall.Arguments[2]).Value);

        var ascending = Assert.IsType<BoundExpressionStatement>(program.GlobalStatements[4]);
        var ascendingCall = Assert.IsType<BoundCallExpression>(ascending.Expression);
        Assert.Equal(1, Assert.IsType<BoundLiteralExpression>(ascendingCall.Arguments[2]).Value);
    }

    [Theory]
    [InlineData("ARRAYSORT(basket[], \"Prise\")", DiagnosticCodes.InvalidSortKeyField)]
    [InlineData("ARRAYSORT(basket[], \"Price\", STEP -2)", DiagnosticCodes.InvalidSortDirection)]
    [InlineData("ARRAYSORT(basket[], \"Price\", STEP 0)", DiagnosticCodes.InvalidSortDirection)]
    public void InvalidFieldOrDirectionReportsDedicatedDiagnostic(string call, string code)
    {
        var diagnostics = BindWithDiagnostics(Records + "DIM basket[1 TO 2] AS Product\n" + call);
        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public void NonConstantFieldNameIsRejected()
    {
        var diagnostics = BindWithDiagnostics(
            Records +
            "DIM basket[1 TO 2] AS Product\n" +
            "DIM fieldName AS STRING = \"Price\"\n" +
            "ARRAYSORT(basket[], fieldName)");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortKeyField);
    }

    [Fact]
    public void StringConstantFieldNameBinds()
    {
        // Сообщение и договор говорят о строковой константе, известной до
        // выполнения, поэтому CONST с литеральным значением допустим.
        var program = Bind(
            Records +
            "CONST fieldName AS STRING = \"Price\"\n" +
            "DIM basket[1 TO 2] AS Product\n" +
            "ARRAYSORT(basket[], fieldName)",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var sort = Assert.IsType<BoundExpressionStatement>(program.GlobalStatements[2]);
        Assert.IsType<BoundVariableExpression>(Assert.IsType<BoundCallExpression>(sort.Expression).Arguments[1]);
    }

    [Fact]
    public void ParenthesizedLiteralFieldNameBinds()
    {
        var program = Bind(
            Records +
            "DIM basket[1 TO 2] AS Product\n" +
            "ARRAYSORT(basket[], (\"Price\"))",
            out var diagnostics);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void StringConstantWithUnknownFieldIsStillRejected()
    {
        var diagnostics = BindWithDiagnostics(
            Records +
            "CONST fieldName AS STRING = \"Prise\"\n" +
            "DIM basket[1 TO 2] AS Product\n" +
            "ARRAYSORT(basket[], fieldName)");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortKeyField);
    }

    [Fact]
    public void ScalarArrayWithFieldNameIsRejected()
    {
        var diagnostics = BindWithDiagnostics("DIM values[1 TO 3] AS INTEGER\nARRAYSORT(values[], \"Price\")");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortKeyField);
    }

    [Fact]
    public void ArrayFieldIsRejectedAsSortKey()
    {
        var diagnostics = BindWithDiagnostics(
            "TYPE Tagged\nName AS STRING\nTags[] AS STRING\nEND TYPE\n" +
            "DIM items[1 TO 2] AS Tagged\n" +
            "ARRAYSORT(items[], \"Tags\")");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortKeyField);
    }

    [Fact]
    public void StepOutsideSortIsRejected()
    {
        var diagnostics = BindWithDiagnostics("PRINT LEN(\"abc\", STEP 1)");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortDirection);
    }

    [Fact]
    public void StepAsArrayMapArgumentReportsDiagnosticInsteadOfCrashing()
    {
        // ARRAYMAP запрашивает координаты аргумента до предметной диагностики,
        // поэтому узел STEP обязан иметь местоположение.
        var diagnostics = BindWithDiagnostics("DIM a[1 TO 2] AS INTEGER\nPRINT ARRAYMAP(a[], STEP 1)");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortDirection);
    }

    [Fact]
    public void StepAsByRefArgumentReportsDiagnosticInsteadOfCrashing()
    {
        var diagnostics = BindWithDiagnostics("SUB F(BYREF x AS INTEGER)\nEND SUB\nF(STEP 1)");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSortDirection);
    }

    [Fact]
    public void NumericDimensionFormStillBinds()
    {
        var program = Bind(
            "DIM grid[1 TO 2, 1 TO 2] AS INTEGER\n" +
            "ARRAYSORT(grid[], 2)\n" +
            "DIM copy[,] AS INTEGER\n" +
            "copy[] = ARRAYTOSORTED(grid[], 1)",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var inPlace = Assert.IsType<BoundExpressionStatement>(program.GlobalStatements[1]);
        Assert.IsType<BoundLiteralExpression>(Assert.IsType<BoundCallExpression>(inPlace.Expression).Arguments[1]);
    }

    private static BoundProgram Bind(string source, out DiagnosticBag diagnostics) =>
        Binder.Bind(Parse(source), out diagnostics, checkGraphicsModes: false);

    private static IReadOnlyList<Diagnostic> BindWithDiagnostics(string source)
    {
        _ = Binder.Bind(Parse(source), out var diagnostics, checkGraphicsModes: false);
        return diagnostics.ToList();
    }

    private static Axiom.Syntax.ProgramSyntax Parse(string source)
    {
        var syntax = Parser.Parse(source, out var parsing, document: null);
        Assert.Empty(parsing);
        return syntax;
    }
}
