using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Parsing.Tests;

public sealed class ModuleSyntaxTests
{
    [Fact]
    public void ImportIsParsedBeforeOtherMembers()
    {
        var program = Parser.Parse("""
IMPORT math
PRINT 1
""", out var diagnostics);

        Assert.Empty(diagnostics);
        var import = Assert.IsType<ImportStatementSyntax>(program.Members[0]);
        Assert.Equal("math", import.ModuleName);
        Assert.IsType<GlobalStatementSyntax>(program.Members[1]);
    }

    [Fact]
    public void ImportAfterDeclarationIsReported()
    {
        var program = Parser.Parse("""
PRINT 1
IMPORT math
""", out var diagnostics);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnexpectedToken);
        Assert.Contains(program.Members, member => member is ImportStatementSyntax);
    }

    [Theory]
    [InlineData("PUBLIC FUNCTION Square(x AS INTEGER) AS INTEGER\nRETURN x * x\nEND FUNCTION", typeof(FunctionDeclarationSyntax))]
    [InlineData("PRIVATE FUNCTION Square(x AS INTEGER) AS INTEGER\nRETURN x * x\nEND FUNCTION", typeof(FunctionDeclarationSyntax))]
    [InlineData("PUBLIC SUB Run()\nEND SUB", typeof(SubDeclarationSyntax))]
    [InlineData("PUBLIC TYPE Box\nitem AS INTEGER\nEND TYPE", typeof(TypeDeclarationSyntax))]
    public void VisibilityModifierIsParsedOnDeclarations(string source, Type memberType)
    {
        var program = Parser.Parse(source, out var diagnostics);

        Assert.Empty(diagnostics);
        var member = Assert.Single(program.Members);
        Assert.IsType(memberType, member);

        var visibility = member switch
        {
            FunctionDeclarationSyntax function => function.VisibilityKeyword,
            SubDeclarationSyntax sub => sub.VisibilityKeyword,
            TypeDeclarationSyntax type => type.VisibilityKeyword,
            _ => null
        };

        Assert.NotNull(visibility);
    }

    [Fact]
    public void VisibilityModifierIsParsedOnTopLevelDim()
    {
        var program = Parser.Parse("PUBLIC DIM shared AS INTEGER = 1", out var diagnostics);

        Assert.Empty(diagnostics);
        var statement = Assert.IsType<ModuleStatementSyntax>(Assert.Single(program.Members));
        Assert.True(statement.IsPublic);
    }

    [Fact]
    public void VisibilityModifierIsRejectedBeforeOrdinaryStatement()
    {
        _ = Parser.Parse("PUBLIC PRINT 1", out var diagnostics);

        Assert.NotEmpty(diagnostics);
    }
}
