using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Parsing.Tests;

public sealed class SquareBracketParsingTests
{
    [Theory]
    [InlineData("PRINT fruits[ChooseFruit()]")]
    [InlineData("PRINT MakeFruits()[2]")]
    [InlineData("PRINT bag.fruits[]")]
    [InlineData("PRINT grid[-1, 7 TO 8]")]
    [InlineData("PRINT fruits[ChooseFruit() + (offset * 2)]")]
    [InlineData("PRINT fruits[ ]")]
    [InlineData("PRINT $\"Фрукт: {fruits[2]}\"")]
    [InlineData("fruits[2] = \"груша\"")]
    [InlineData("DIM fruits[] AS STRING")]
    [InlineData("DIM fruits[] = MakeFruits()")]
    [InlineData("DIM grid[,] AS INTEGER")]
    [InlineData("DIM grid[1 TO 2, -1 TO 3] AS INTEGER")]
    [InlineData("TYPE Bag\nfruits[] AS STRING\nEND TYPE")]
    [InlineData("SUB ShowBasket(values[] AS STRING)\nPRINT values[2]\nEND SUB")]
    [InlineData("SUB Edit(BYREF values[,] AS INTEGER)\nEND SUB")]
    [InlineData("FUNCTION MakeFruits() AS STRING[]\nRETURN fruits[]\nEND FUNCTION")]
    [InlineData("DIM reader AS FUNCTION(INTEGER[,]) AS STRING[]")]
    [InlineData("REDIM PRESERVE fruits[1 TO 5]")]
    [InlineData("REDIM bags[ChooseBag()].fruits[1 TO 5]")]
    [InlineData("ERASE bag.fruits[]")]
    [InlineData("PRINT []")]
    [InlineData("DIM fruits[] AS STRING = [\"яблоко\", \"банан\"]")]
    [InlineData("PRINT [10, 20, 30][2]")]
    public void Parse_AcceptsSquareBracketForms(string source)
    {
        Parser.ParseSquareBracketSyntax(source, out var diagnostics);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("PRINT grid[,]")]
    [InlineData("PRINT fruits[2,]")]
    [InlineData("PRINT fruits[,2]")]
    [InlineData("PRINT fruits[2]]")]
    [InlineData("PRINT fruits[index := 2]")]
    [InlineData("REDIM fruits[]")]
    [InlineData("SUB Show(values[1 TO 2] AS STRING)\nEND SUB")]
    public void Parse_RejectsMalformedBracketForms(string source)
    {
        Parser.ParseSquareBracketSyntax(source, out var diagnostics);
        Assert.NotEmpty(diagnostics);
    }

    [Theory]
    [InlineData("DIM fruits[\nPRINT 7")]
    [InlineData("DIM fruits[1 TO\nPRINT 7")]
    [InlineData("REDIM fruits[1 TO\nPRINT 7")]
    [InlineData("PRINT fruits[ChooseFruit()\nPRINT 7")]
    [InlineData("PRINT fruits[1 TO\nPRINT 7")]
    public void Parse_MalformedBracketsPreserveFollowingLine(string source)
    {
        var program = Parser.ParseSquareBracketSyntax(source, out var diagnostics);
        Assert.NotEmpty(diagnostics);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[^1]).Statement);
        Assert.Equal(7, Assert.IsType<LiteralExpressionSyntax>(Assert.Single(print.Items).Expression).LiteralToken.Value);
    }

    [Fact]
    public void Parse_PublicPreparationUsesSquareArraySyntax()
    {
        var program = Parser.Parse("PRINT fruits[2]", out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.True(program.UsesSquareArraySyntax);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(Assert.Single(program.Members)).Statement);
        Assert.IsType<ArrayAccessExpressionSyntax>(Assert.Single(print.Items).Expression);
    }

    [Fact]
    public void Parse_SeparatesArrayIndicesFromFunctionCalls()
    {
        var array = Assert.IsType<ArrayAccessExpressionSyntax>(PrintExpression("fruits[ChooseFruit() + (offset * 2)]"));
        Assert.Equal("fruits", Assert.IsType<NameExpressionSyntax>(array.Target).IdentifierToken.Text);
        var index = Assert.IsType<BinaryExpressionSyntax>(Assert.Single(array.Indices));
        Assert.IsType<CallExpressionSyntax>(index.Left);
        Assert.IsType<ParenthesizedExpressionSyntax>(index.Right);
        Assert.False(array.IsWholeArray);
        Assert.Equal(SyntaxKind.OpenBracketToken, array.OpenBracketToken.Kind);
        Assert.Equal(SyntaxKind.CloseBracketToken, array.CloseBracketToken.Kind);
    }

    [Fact]
    public void Parse_IndexesFunctionResultAndMarksWholeArrayField()
    {
        var element = Assert.IsType<ArrayAccessExpressionSyntax>(PrintExpression("MakeFruits()[2]"));
        Assert.IsType<CallExpressionSyntax>(element.Target);
        Assert.Equal(2, Assert.IsType<LiteralExpressionSyntax>(Assert.Single(element.Indices)).LiteralToken.Value);
        var whole = Assert.IsType<ArrayAccessExpressionSyntax>(PrintExpression("bag.fruits[]"));
        Assert.IsType<FieldAccessExpressionSyntax>(whole.Target);
        Assert.True(whole.IsWholeArray);
    }

    [Fact]
    public void Parse_PreservesRangesInMultidimensionalAccess()
    {
        var array = Assert.IsType<ArrayAccessExpressionSyntax>(PrintExpression("grid[-1, 7 TO 8]"));
        Assert.Equal(2, array.Indices.Count);
        Assert.IsType<UnaryExpressionSyntax>(array.Indices[0]);
        Assert.IsType<RangeExpressionSyntax>(array.Indices[1]);
        Assert.Single(array.CommaTokens);
    }

    [Theory]
    [InlineData("[]", 1, true)]
    [InlineData("[,]", 2, true)]
    [InlineData("[1 TO 2, -1 TO 3]", 2, false)]
    public void Parse_PreservesDeclarationRankAndBounds(string suffix, int rank, bool dynamic)
    {
        var program = Parser.ParseSquareBracketSyntax($"DIM grid{suffix} AS INTEGER", out var diagnostics);
        Assert.Empty(diagnostics);
        var dim = Assert.IsType<DimStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(Assert.Single(program.Members)).Statement);
        var bounds = Assert.Single(Assert.Single(dim.Declarations).Declarators).ArrayBounds!;
        Assert.Equal(rank, bounds.Rank);
        Assert.Equal(dynamic, bounds.IsDynamic);
        Assert.Equal("[", bounds.OpenDelimiterToken.Text);
        Assert.Equal("]", bounds.CloseDelimiterToken.Text);
    }

    [Theory]
    [InlineData("[]", 1)]
    [InlineData("[,]", 2)]
    public void Parse_FixedStringArrayTypeDoesNotIndexItsLength(string suffix, int rank)
    {
        var program = Parser.ParseSquareBracketSyntax($"FUNCTION Make() AS STRING * (2 + 2){suffix}\nEND FUNCTION", out var diagnostics);
        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        var array = Assert.IsType<ArrayTypeSyntax>(function.ReturnType.Type);
        Assert.Equal(rank, array.Rank);
        var element = Assert.IsType<FixedLengthStringTypeSyntax>(array.ElementType);
        Assert.IsType<ParenthesizedExpressionSyntax>(element.LengthExpression);
    }

    [Fact]
    public void Parse_ParameterSuffixStaysAttachedToName()
    {
        var program = Parser.ParseSquareBracketSyntax("FUNCTION Read(values[,] AS INTEGER) AS STRING[]\nRETURN result[]\nEND FUNCTION", out var diagnostics);
        Assert.Empty(diagnostics);
        var function = Assert.IsType<FunctionDeclarationSyntax>(Assert.Single(program.Members));
        var parameter = Assert.Single(function.Parameters);
        Assert.Equal(2, parameter.ArrayBounds!.Rank);
        Assert.IsType<NamedTypeSyntax>(parameter.TypeClause.Type);
        var resultType = Assert.IsType<ArrayTypeSyntax>(function.ReturnType.Type);
        Assert.Equal(1, resultType.Rank);
        Assert.Equal("[", resultType.OpenDelimiterToken.Text);
    }

    [Fact]
    public void Parse_ReadsArrayLiteralFromLeadingBrackets()
    {
        var literal = Assert.IsType<ArrayLiteralExpressionSyntax>(PrintExpression("[\"яблоко\", \"банан\"]"));
        Assert.Equal(2, literal.Items.Count);
        Assert.Single(literal.CommaTokens);
        Assert.False(literal.IsEmpty);
        Assert.Equal("яблоко", Assert.IsType<LiteralExpressionSyntax>(literal.Items[0]).LiteralToken.Value);
        Assert.Equal("банан", Assert.IsType<LiteralExpressionSyntax>(literal.Items[1]).LiteralToken.Value);
    }

    [Fact]
    public void Parse_ReadsEmptyArrayLiteral()
    {
        var literal = Assert.IsType<ArrayLiteralExpressionSyntax>(PrintExpression("[]"));
        Assert.True(literal.IsEmpty);
        Assert.Empty(literal.Items);
        Assert.Empty(literal.CommaTokens);
    }

    [Fact]
    public void Parse_IndexesArrayLiteral()
    {
        var access = Assert.IsType<ArrayAccessExpressionSyntax>(PrintExpression("[10, 20, 30][2]"));
        Assert.IsType<ArrayLiteralExpressionSyntax>(access.Target);
        Assert.Equal(2, Assert.IsType<LiteralExpressionSyntax>(Assert.Single(access.Indices)).LiteralToken.Value);
    }

    [Fact]
    public void Parse_KeepsNestedAccessInsideArrayLiteral()
    {
        var literal = Assert.IsType<ArrayLiteralExpressionSyntax>(PrintExpression("[fruits[1], fruits[2]]"));
        Assert.Equal(2, literal.Items.Count);
        Assert.All(literal.Items, item => Assert.IsType<ArrayAccessExpressionSyntax>(item));
    }

    private static ExpressionSyntax PrintExpression(string expression)
    {
        var program = Parser.ParseSquareBracketSyntax($"PRINT {expression}", out var diagnostics);
        Assert.Empty(diagnostics);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(Assert.Single(program.Members)).Statement);
        return Assert.Single(print.Items).Expression;
    }

    [Fact]
    public void Parse_MissingCloseBracketPreservesFollowingStatement()
    {
        var program = Parser.ParseSquareBracketSyntax("PRINT fruits[2\nPRINT 7", out var diagnostics);
        Assert.Single(diagnostics);
        Assert.Equal(2, program.Members.Count);
        var print = Assert.IsType<PrintStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[1]).Statement);
        Assert.Equal(7, Assert.IsType<LiteralExpressionSyntax>(Assert.Single(print.Items).Expression).LiteralToken.Value);
    }
}
