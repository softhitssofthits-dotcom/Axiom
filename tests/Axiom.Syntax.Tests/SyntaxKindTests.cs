using Axiom.Syntax;

namespace Axiom.Syntax.Tests;

public sealed class SyntaxKindTests
{
    [Fact]
    public void SyntaxKind_HasBadToken()
    {
        Assert.Equal(0, (int)SyntaxKind.BadToken);
    }

    [Fact]
    public void SyntaxKind_HasEndOfFileToken()
    {
        Assert.Equal(1, (int)SyntaxKind.EndOfFileToken);
    }

    [Fact]
    public void SyntaxKind_AllTokenKinds_AreLessThanFirstStatementKind()
    {
        var firstStatement = SyntaxKind.Program;
        var tokenKinds = new[]
        {
            SyntaxKind.BadToken,
            SyntaxKind.EndOfFileToken,
            SyntaxKind.NewLineToken,
            SyntaxKind.CommentToken,
            SyntaxKind.IdentifierToken,
            SyntaxKind.IntegerLiteralToken,
            SyntaxKind.LongLiteralToken,
            SyntaxKind.DoubleLiteralToken,
            SyntaxKind.StringLiteralToken,
            SyntaxKind.BooleanLiteralToken,
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,
            SyntaxKind.StarToken,
            SyntaxKind.SlashToken,
            SyntaxKind.EqualsToken,
        };

        foreach (var kind in tokenKinds)
        {
            Assert.True((int)kind < (int)firstStatement, $"{kind} should be less than {firstStatement}");
        }
    }

    [Fact]
    public void SyntaxKind_ExpressionKinds_AreDistinct()
    {
        var expressionKinds = new[]
        {
            SyntaxKind.LiteralExpression,
            SyntaxKind.NameExpression,
            SyntaxKind.UnaryExpression,
            SyntaxKind.BinaryExpression,
            SyntaxKind.RangeExpression,
            SyntaxKind.ParenthesizedExpression,
            SyntaxKind.NamedArgumentExpression,
            SyntaxKind.CallExpression,
            SyntaxKind.FieldAccessExpression,
            SyntaxKind.ErrorExpression,
        };

        Assert.Equal(expressionKinds.Length, expressionKinds.Distinct().Count());
    }

    [Fact]
    public void SyntaxKind_StatementKinds_AreDistinct()
    {
        var statementKinds = new[]
        {
            SyntaxKind.PrintStatement,
            SyntaxKind.InputStatement,
            SyntaxKind.SleepStatement,
            SyntaxKind.DimStatement,
            SyntaxKind.ConstStatement,
            SyntaxKind.AssignmentStatement,
            SyntaxKind.IfStatement,
            SyntaxKind.SelectCaseStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.DoLoopStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.TryStatement,
            SyntaxKind.ExitForStatement,
            SyntaxKind.ExitDoStatement,
            SyntaxKind.ReturnStatement,
            SyntaxKind.ExpressionStatement,
            SyntaxKind.EraseStatement,
            SyntaxKind.RedimStatement,
        };

        Assert.Equal(statementKinds.Length, statementKinds.Distinct().Count());
    }

    [Fact]
    public void SyntaxKind_KeywordKinds_AreDistinct()
    {
        var keywordKinds = new[]
        {
            SyntaxKind.AndKeyword,
            SyntaxKind.AsKeyword,
            SyntaxKind.BooleanKeyword,
            SyntaxKind.CanvasKeyword,
            SyntaxKind.CaseKeyword,
            SyntaxKind.ConstKeyword,
            SyntaxKind.DecimalKeyword,
            SyntaxKind.DimKeyword,
            SyntaxKind.DoKeyword,
            SyntaxKind.DoubleKeyword,
            SyntaxKind.ElseKeyword,
            SyntaxKind.EndKeyword,
            SyntaxKind.ErrorKeyword,
            SyntaxKind.ExitKeyword,
            SyntaxKind.ForKeyword,
            SyntaxKind.FunctionKeyword,
            SyntaxKind.IfKeyword,
            SyntaxKind.InputKeyword,
            SyntaxKind.IntegerKeyword,
            SyntaxKind.LongKeyword,
            SyntaxKind.NextKeyword,
            SyntaxKind.NotKeyword,
            SyntaxKind.OnKeyword,
            SyntaxKind.OrKeyword,
            SyntaxKind.PrintKeyword,
            SyntaxKind.ReturnKeyword,
            SyntaxKind.SelectKeyword,
            SyntaxKind.SleepKeyword,
            SyntaxKind.StringKeyword,
            SyntaxKind.SubKeyword,
            SyntaxKind.ThenKeyword,
            SyntaxKind.ToKeyword,
            SyntaxKind.TypeKeyword,
            SyntaxKind.WhileKeyword,
        };

        Assert.Equal(keywordKinds.Length, keywordKinds.Distinct().Count());
    }

    [Fact]
    public void SyntaxKind_HasExpectedCount()
    {
        var values = Enum.GetValues<SyntaxKind>();
        Assert.True(values.Length >= 200, $"Expected at least 200 SyntaxKind values, got {values.Length}");
    }
}
