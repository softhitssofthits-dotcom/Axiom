using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Syntax.Tests;

public sealed class StatementSyntaxTests
{
    private static SyntaxToken Token(SyntaxKind kind, string text = "x")
    {
        return new SyntaxToken(kind, text, new TextLocation(new TextSpan(0, text.Length), line: 1, column: 1));
    }

    private static NameExpressionSyntax NameExpr(string name = "x")
    {
        return new NameExpressionSyntax(Token(SyntaxKind.IdentifierToken, name));
    }

    [Fact]
    public void PrintStatement_HasCorrectKind()
    {
        var keyword = Token(SyntaxKind.PrintKeyword, "PRINT");
        var item = new PrintItemSyntax(new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"hi\"")), null);
        var stmt = new PrintStatementSyntax(keyword, [item]);
        Assert.Equal(SyntaxKind.PrintStatement, stmt.Kind);
        Assert.Single(stmt.Items);
    }

    [Fact]
    public void DimStatement_HasCorrectKind()
    {
        var keyword = Token(SyntaxKind.DimKeyword, "DIM");
        var identifier = Token(SyntaxKind.IdentifierToken, "x");
        var declarator = new VariableDeclaratorSyntax(identifier, null);
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var typeToken = Token(SyntaxKind.IntegerKeyword, "INTEGER");
        var typeClause = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(typeToken));
        var declaration = new VariableDeclarationSyntax([declarator], [], typeClause, null, null);
        var stmt = new DimStatementSyntax(keyword, [declaration], []);
        Assert.Equal(SyntaxKind.DimStatement, stmt.Kind);
    }

    [Fact]
    public void IfStatement_HasCorrectKind()
    {
        var ifKeyword = Token(SyntaxKind.IfKeyword, "IF");
        var condition = new LiteralExpressionSyntax(Token(SyntaxKind.BooleanLiteralToken, "TRUE"));
        var thenKeyword = Token(SyntaxKind.ThenKeyword, "THEN");
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endIfKeyword = Token(SyntaxKind.IfKeyword, "IF");
        var stmt = new IfStatementSyntax(ifKeyword, condition, thenKeyword, [], [], null, endKeyword, endIfKeyword);
        Assert.Equal(SyntaxKind.IfStatement, stmt.Kind);
    }

    [Fact]
    public void ForStatement_HasCorrectKind()
    {
        var forKeyword = Token(SyntaxKind.ForKeyword, "FOR");
        var identifier = Token(SyntaxKind.IdentifierToken, "i");
        var equalsToken = Token(SyntaxKind.EqualsToken, "=");
        var start = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var toKeyword = Token(SyntaxKind.ToKeyword, "TO");
        var end = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "10"));
        var nextKeyword = Token(SyntaxKind.NextKeyword, "NEXT");
        var stmt = new ForStatementSyntax(forKeyword, identifier, equalsToken, start, toKeyword, end, null, [], nextKeyword);
        Assert.Equal(SyntaxKind.ForStatement, stmt.Kind);
    }

    [Fact]
    public void WhileStatement_HasCorrectKind()
    {
        var whileKeyword = Token(SyntaxKind.WhileKeyword, "WHILE");
        var condition = new LiteralExpressionSyntax(Token(SyntaxKind.BooleanLiteralToken, "TRUE"));
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endWhileKeyword = Token(SyntaxKind.WhileKeyword, "WHILE");
        var stmt = new WhileStatementSyntax(whileKeyword, condition, [], endKeyword, endWhileKeyword);
        Assert.Equal(SyntaxKind.WhileStatement, stmt.Kind);
    }

    [Fact]
    public void AssignmentStatement_HasCorrectKind()
    {
        var target = NameExpr("x");
        var equals = Token(SyntaxKind.EqualsToken, "=");
        var value = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "5"));
        var stmt = new AssignmentStatementSyntax(target, equals, value);
        Assert.Equal(SyntaxKind.AssignmentStatement, stmt.Kind);
    }

    [Fact]
    public void SelectCaseStatement_HasCorrectKind()
    {
        var selectKeyword = Token(SyntaxKind.SelectKeyword, "SELECT");
        var caseKeyword = Token(SyntaxKind.CaseKeyword, "CASE");
        var testExpr = NameExpr("x");
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endSelectKeyword = Token(SyntaxKind.SelectKeyword, "SELECT");
        var stmt = new SelectCaseStatementSyntax(selectKeyword, caseKeyword, testExpr, [], null, endKeyword, endSelectKeyword);
        Assert.Equal(SyntaxKind.SelectCaseStatement, stmt.Kind);
    }

    [Fact]
    public void InputStatement_HasCorrectKind()
    {
        var inputKeyword = Token(SyntaxKind.InputKeyword, "INPUT");
        var id = Token(SyntaxKind.IdentifierToken, "x");
        var stmt = new InputStatementSyntax(inputKeyword, null, null, [id], []);
        Assert.Equal(SyntaxKind.InputStatement, stmt.Kind);
    }

    [Fact]
    public void SleepStatement_HasCorrectKind()
    {
        var sleepKeyword = Token(SyntaxKind.SleepKeyword, "SLEEP");
        var stmt = new SleepStatementSyntax(sleepKeyword, null);
        Assert.Equal(SyntaxKind.SleepStatement, stmt.Kind);
    }

    [Fact]
    public void ExpressionStatement_HasCorrectKind()
    {
        var expr = new CallExpressionSyntax(
            NameExpr("PRINT"),
            Token(SyntaxKind.OpenParenToken, "("),
            [new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"hi\""))],
            [],
            Token(SyntaxKind.CloseParenToken, ")"));
        var stmt = new ExpressionStatementSyntax(expr);
        Assert.Equal(SyntaxKind.ExpressionStatement, stmt.Kind);
    }

    [Fact]
    public void ReturnStatement_HasCorrectKind()
    {
        var keyword = Token(SyntaxKind.ReturnKeyword, "RETURN");
        var stmt = new ReturnStatementSyntax(keyword, null);
        Assert.Equal(SyntaxKind.ReturnStatement, stmt.Kind);
    }

    [Fact]
    public void ExitForStatement_HasCorrectKind()
    {
        var exitKeyword = Token(SyntaxKind.ExitKeyword, "EXIT");
        var forKeyword = Token(SyntaxKind.ForKeyword, "FOR");
        var stmt = new ExitForStatementSyntax(exitKeyword, forKeyword);
        Assert.Equal(SyntaxKind.ExitForStatement, stmt.Kind);
    }

    [Fact]
    public void ExitDoStatement_HasCorrectKind()
    {
        var exitKeyword = Token(SyntaxKind.ExitKeyword, "EXIT");
        var doKeyword = Token(SyntaxKind.DoKeyword, "DO");
        var stmt = new ExitDoStatementSyntax(exitKeyword, doKeyword);
        Assert.Equal(SyntaxKind.ExitDoStatement, stmt.Kind);
    }

    [Fact]
    public void ContinueForStatement_HasCorrectKind()
    {
        var continueKeyword = Token(SyntaxKind.ContinueKeyword, "CONTINUE");
        var forKeyword = Token(SyntaxKind.ForKeyword, "FOR");
        var stmt = new ContinueForStatementSyntax(continueKeyword, forKeyword);
        Assert.Equal(SyntaxKind.ContinueForStatement, stmt.Kind);
    }

    [Fact]
    public void ContinueWhileStatement_HasCorrectKind()
    {
        var continueKeyword = Token(SyntaxKind.ContinueKeyword, "CONTINUE");
        var whileKeyword = Token(SyntaxKind.WhileKeyword, "WHILE");
        var stmt = new ContinueWhileStatementSyntax(continueKeyword, whileKeyword);
        Assert.Equal(SyntaxKind.ContinueWhileStatement, stmt.Kind);
    }

    [Fact]
    public void ContinueDoStatement_HasCorrectKind()
    {
        var continueKeyword = Token(SyntaxKind.ContinueKeyword, "CONTINUE");
        var doKeyword = Token(SyntaxKind.DoKeyword, "DO");
        var stmt = new ContinueDoStatementSyntax(continueKeyword, doKeyword);
        Assert.Equal(SyntaxKind.ContinueDoStatement, stmt.Kind);
    }

    [Fact]
    public void EraseStatement_HasCorrectKind()
    {
        var keyword = Token(SyntaxKind.EraseKeyword, "ERASE");
        var target = NameExpr("arr");
        var stmt = new EraseStatementSyntax(keyword, target);
        Assert.Equal(SyntaxKind.EraseStatement, stmt.Kind);
    }

    [Fact]
    public void PrintItem_NullExpression_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PrintItemSyntax(null!, null));
    }

    [Fact]
    public void PrintStatement_NullKeyword_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PrintStatementSyntax(null!, []));
    }

    [Fact]
    public void IfStatement_NullIfKeyword_Throws()
    {
        var condition = new LiteralExpressionSyntax(Token(SyntaxKind.BooleanLiteralToken, "TRUE"));
        var thenKeyword = Token(SyntaxKind.ThenKeyword, "THEN");
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endIfKeyword = Token(SyntaxKind.IfKeyword, "IF");
        Assert.Throws<ArgumentNullException>(() => new IfStatementSyntax(null!, condition, thenKeyword, [], [], null, endKeyword, endIfKeyword));
    }

    [Fact]
    public void ForStatement_NullForKeyword_Throws()
    {
        var identifier = Token(SyntaxKind.IdentifierToken, "i");
        var equals = Token(SyntaxKind.EqualsToken, "=");
        var start = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "1"));
        var toKeyword = Token(SyntaxKind.ToKeyword, "TO");
        var end = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "10"));
        var nextKeyword = Token(SyntaxKind.NextKeyword, "NEXT");
        Assert.Throws<ArgumentNullException>(() => new ForStatementSyntax(null!, identifier, equals, start, toKeyword, end, null, [], nextKeyword));
    }
}
