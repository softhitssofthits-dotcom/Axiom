using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Syntax.Tests;

public sealed class DeclarationSyntaxTests
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
    public void NamedType_HasCorrectKind()
    {
        var typeToken = Token(SyntaxKind.IntegerKeyword, "INTEGER");
        var type = new NamedTypeSyntax(typeToken);
        Assert.Equal(SyntaxKind.NamedType, type.Kind);
        Assert.Same(typeToken, type.TypeToken);
    }

    [Fact]
    public void TypeClause_HasCorrectKind()
    {
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var namedType = new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER"));
        var clause = new TypeClauseSyntax(asKeyword, namedType);
        Assert.Equal(SyntaxKind.TypeClause, clause.Kind);
    }

    [Fact]
    public void VariableDeclarator_HasCorrectKind()
    {
        var id = Token(SyntaxKind.IdentifierToken, "x");
        var decl = new VariableDeclaratorSyntax(id, null);
        Assert.Equal(SyntaxKind.VariableDeclarator, decl.Kind);
        Assert.Same(id, decl.Identifier);
        Assert.Null(decl.ArrayBounds);
    }

    [Fact]
    public void ConstantDeclaration_HasCorrectKind()
    {
        var id = Token(SyntaxKind.IdentifierToken, "MAX");
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var typeClause = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER")));
        var equals = Token(SyntaxKind.EqualsToken, "=");
        var value = new LiteralExpressionSyntax(Token(SyntaxKind.IntegerLiteralToken, "100"));
        var decl = new ConstantDeclarationSyntax(id, typeClause, equals, value);
        Assert.Equal(SyntaxKind.ConstantDeclaration, decl.Kind);
    }

    [Fact]
    public void Program_HasCorrectKind()
    {
        var eof = Token(SyntaxKind.EndOfFileToken, "");
        var program = new ProgramSyntax([], eof);
        Assert.Equal(SyntaxKind.Program, program.Kind);
        Assert.Empty(program.Members);
        Assert.Same(eof, program.EndOfFileToken);
    }

    [Fact]
    public void GlobalStatement_HasCorrectKind()
    {
        var printKeyword = Token(SyntaxKind.PrintKeyword, "PRINT");
        var item = new PrintItemSyntax(new LiteralExpressionSyntax(Token(SyntaxKind.StringLiteralToken, "\"hi\"")), null);
        var printStmt = new PrintStatementSyntax(printKeyword, [item]);
        var global = new GlobalStatementSyntax(printStmt);
        Assert.Equal(SyntaxKind.GlobalStatement, global.Kind);
        Assert.Same(printStmt, global.Statement);
    }

    [Fact]
    public void SubDeclaration_HasCorrectKind()
    {
        var subKeyword = Token(SyntaxKind.SubKeyword, "SUB");
        var id = Token(SyntaxKind.IdentifierToken, "DoSomething");
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endSub = Token(SyntaxKind.SubKeyword, "SUB");
        var decl = new SubDeclarationSyntax(subKeyword, id, open, [], [], close, [], endKeyword, endSub);
        Assert.Equal(SyntaxKind.SubDeclaration, decl.Kind);
    }

    [Fact]
    public void FunctionDeclaration_HasCorrectKind()
    {
        var funcKeyword = Token(SyntaxKind.FunctionKeyword, "FUNCTION");
        var id = Token(SyntaxKind.IdentifierToken, "Calc");
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var returnType = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER")));
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endFunc = Token(SyntaxKind.FunctionKeyword, "FUNCTION");
        var decl = new FunctionDeclarationSyntax(funcKeyword, id, open, [], [], close, returnType, [], endKeyword, endFunc);
        Assert.Equal(SyntaxKind.FunctionDeclaration, decl.Kind);
    }

    [Fact]
    public void TypeDeclaration_HasCorrectKind()
    {
        var typeKeyword = Token(SyntaxKind.TypeKeyword, "TYPE");
        var id = Token(SyntaxKind.IdentifierToken, "Point");
        var endKeyword = Token(SyntaxKind.EndKeyword, "END");
        var endType = Token(SyntaxKind.TypeKeyword, "TYPE");
        var decl = new TypeDeclarationSyntax(typeKeyword, id, [], [], endKeyword, endType);
        Assert.Equal(SyntaxKind.TypeDeclaration, decl.Kind);
    }

    [Fact]
    public void ArrayBounds_UseNeutralDelimiterPropertyNames()
    {
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var bounds = new ArrayBoundsSyntax(open, [], [], close);

        Assert.Equal(SyntaxKind.ArrayBounds, bounds.Kind);
        Assert.Same(open, bounds.OpenDelimiterToken);
        Assert.Same(close, bounds.CloseDelimiterToken);
    }

    [Fact]
    public void ArrayType_HasCorrectKind()
    {
        var elementType = new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER"));
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var arrayType = new ArrayTypeSyntax(elementType, open, [], close);
        Assert.Equal(SyntaxKind.ArrayType, arrayType.Kind);
        Assert.Equal(1, arrayType.Rank);
        Assert.Same(open, arrayType.OpenDelimiterToken);
        Assert.Same(close, arrayType.CloseDelimiterToken);
    }

    [Fact]
    public void ArrayType_MultiDimensional_RankIsCorrect()
    {
        var elementType = new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER"));
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var comma = Token(SyntaxKind.CommaToken, ",");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var arrayType = new ArrayTypeSyntax(elementType, open, [comma], close);
        Assert.Equal(2, arrayType.Rank);
    }

    [Fact]
    public void Parameter_HasCorrectKind()
    {
        var id = Token(SyntaxKind.IdentifierToken, "x");
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var typeClause = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER")));
        var param = new ParameterSyntax(null, id, typeClause);
        Assert.Equal(SyntaxKind.Parameter, param.Kind);
        Assert.Null(param.PassingKeyword);
        Assert.Null(param.ArrayBounds);
    }

    [Fact]
    public void Parameter_WithArrayBounds_TracksNameSuffix()
    {
        var id = Token(SyntaxKind.IdentifierToken, "values");
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var typeClause = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(Token(SyntaxKind.StringKeyword, "STRING")));
        var open = Token(SyntaxKind.OpenParenToken, "(");
        var close = Token(SyntaxKind.CloseParenToken, ")");
        var bounds = new ArrayBoundsSyntax(open, [], [], close);
        var param = new ParameterSyntax(null, id, typeClause, bounds);

        Assert.Same(bounds, param.ArrayBounds);
    }

    [Fact]
    public void Parameter_WithByRef_HasPassingKeyword()
    {
        var byRef = Token(SyntaxKind.ByRefKeyword, "BYREF");
        var id = Token(SyntaxKind.IdentifierToken, "x");
        var asKeyword = Token(SyntaxKind.AsKeyword, "AS");
        var typeClause = new TypeClauseSyntax(asKeyword, new NamedTypeSyntax(Token(SyntaxKind.IntegerKeyword, "INTEGER")));
        var param = new ParameterSyntax(byRef, id, typeClause);
        Assert.Same(byRef, param.PassingKeyword);
    }

    [Fact]
    public void Program_NullMembers_Throws()
    {
        var eof = Token(SyntaxKind.EndOfFileToken, "");
        Assert.Throws<ArgumentNullException>(() => new ProgramSyntax(null!, eof));
    }

    [Fact]
    public void NamedType_NullTypeToken_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NamedTypeSyntax(null!));
    }

    [Fact]
    public void VariableDeclarator_NullIdentifier_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VariableDeclaratorSyntax(null!, null));
    }
}
