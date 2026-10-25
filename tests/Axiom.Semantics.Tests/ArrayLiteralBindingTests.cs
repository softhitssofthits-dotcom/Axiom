using Axiom.Core;
using Axiom.Parsing;

namespace Axiom.Semantics.Tests;

public sealed class ArrayLiteralBindingTests
{
    [Fact]
    public void ExplicitElementTypeBindsLiteral()
    {
        var program = Bind("DIM fruits[] AS STRING = [\"яблоко\", \"банан\"]", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var literal = Assert.IsType<BoundArrayLiteralExpression>(declaration.Initializer);
        Assert.Equal(2, literal.Items.Count);
        Assert.Equal("STRING[]", literal.ArrayType.Name);
    }

    [Fact]
    public void ElementTypeIsInferredWhenDeclarationOmitsType()
    {
        var program = Bind("DIM fruits[] = [\"яблоко\", \"банан\"]", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var literal = Assert.IsType<BoundArrayLiteralExpression>(declaration.Initializer);
        Assert.Equal("STRING[]", literal.ArrayType.Name);
    }

    [Theory]
    [InlineData("[1, 2]", "INTEGER[]")]
    [InlineData("[1, 2.5]", "DOUBLE[]")]
    [InlineData("[1, 3000000000]", "LONG[]")]
    public void MixedNumericValuesUseCommonType(string literalText, string expectedName)
    {
        var program = Bind($"DIM values[] = {literalText}", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        Assert.Equal(expectedName, Assert.IsType<BoundArrayLiteralExpression>(declaration.Initializer).ArrayType.Name);
    }

    [Fact]
    public void EmptyLiteralBindsWhenDeclarationProvidesType()
    {
        var program = Bind("DIM values[] AS INTEGER = []", out var diagnostics);

        Assert.Empty(diagnostics);
        var declaration = Assert.IsType<BoundVariableDeclarationStatement>(program.GlobalStatements[0]);
        var literal = Assert.IsType<BoundArrayLiteralExpression>(declaration.Initializer);
        Assert.Empty(literal.Items);
        Assert.Equal("INTEGER[]", literal.ArrayType.Name);
    }

    [Fact]
    public void EmptyLiteralWithoutContextReportsDedicatedCode()
    {
        var diagnostics = BindWithDiagnostics("PRINT []");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.ArrayLiteralTypeUnknown);
    }

    [Fact]
    public void IncompatibleElementReportsDedicatedCode()
    {
        var diagnostics = BindWithDiagnostics("DIM values[] AS INTEGER = [1, \"два\"]");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidArrayLiteralElement);
    }

    [Fact]
    public void NestedArrayElementIsRejected()
    {
        var diagnostics = BindWithDiagnostics("DIM values[] = [[1, 2], [3, 4]]");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidArrayLiteralElement);
    }

    [Fact]
    public void MultidimensionalTargetRejectsLiteral()
    {
        var diagnostics = BindWithDiagnostics("DIM grid[,] AS INTEGER = [1, 2]");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.ArrayLiteralRankMismatch);
    }

    [Fact]
    public void LiteralBindsInWholeArrayAssignment()
    {
        var program = Bind("DIM values[] AS INTEGER\nvalues[] = [7, 8, 9]", out var diagnostics);

        Assert.Empty(diagnostics);
        var assignment = Assert.IsType<BoundAssignmentStatement>(program.GlobalStatements[1]);
        Assert.IsType<BoundArrayLiteralExpression>(assignment.Expression);
    }

    [Fact]
    public void LiteralBindsAsUserFunctionArrayArgument()
    {
        var program = Bind(
            "FUNCTION Total(items[] AS INTEGER) AS INTEGER\nRETURN ARRAYSUM(items[])\nEND FUNCTION\n" +
            "PRINT Total([4, 5, 6])",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var print = Assert.IsType<BoundPrintStatement>(program.GlobalStatements[^1]);
        var call = Assert.IsType<BoundCallExpression>(Assert.Single(print.Items).Expression);
        Assert.IsType<BoundArrayLiteralExpression>(Assert.Single(call.Arguments));
    }

    [Fact]
    public void LiteralBindsAsReturnedValue()
    {
        var program = Bind(
            "FUNCTION MakeFruits() AS STRING[]\nRETURN [\"груша\", \"слива\"]\nEND FUNCTION",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var routine = program.Routines.Single();
        var statement = routine.Statements.OfType<BoundReturnStatement>().Single();
        Assert.IsType<BoundArrayLiteralExpression>(statement.Expression);
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
