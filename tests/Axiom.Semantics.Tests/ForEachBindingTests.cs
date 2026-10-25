using Axiom.Core;
using Axiom.Parsing;
using Axiom.Syntax;

namespace Axiom.Semantics.Tests;

public sealed class ForEachBindingTests
{
    [Fact]
    public void ByValIsDefaultAndInfersElementType()
    {
        var program = Bind("DIM fruits[] AS STRING = [\"яблоко\"]\nFOR EACH fruit IN fruits[]\nPRINT fruit\nNEXT", out var diagnostics);

        Assert.Empty(diagnostics);
        var loop = Assert.IsType<BoundForEachStatement>(program.GlobalStatements[1]);
        Assert.False(loop.ByRef);
        Assert.Equal(TypeSymbol.String, loop.Variable.Type);
        Assert.Equal("STRING[]", loop.SourceArrayType.Name);
    }

    [Fact]
    public void ExplicitByValKeywordBinds()
    {
        var program = Bind("DIM values[1 TO 2] AS INTEGER\nFOR EACH BYVAL item IN values[]\nPRINT item\nNEXT", out var diagnostics);

        Assert.Empty(diagnostics);
        var loop = Assert.IsType<BoundForEachStatement>(program.GlobalStatements[1]);
        Assert.False(loop.ByRef);
    }

    [Fact]
    public void AsClauseConvertsWideningElementType()
    {
        var program = Bind("DIM values[1 TO 2] AS INTEGER\nFOR EACH item AS DOUBLE IN values[]\nPRINT item\nNEXT", out var diagnostics);

        Assert.Empty(diagnostics);
        var loop = Assert.IsType<BoundForEachStatement>(program.GlobalStatements[1]);
        Assert.Equal(TypeSymbol.Double, loop.Variable.Type);
    }

    [Fact]
    public void NonArraySourceReportsDedicatedCode()
    {
        var diagnostics = BindWithDiagnostics("DIM value AS INTEGER\nFOR EACH item IN value\nPRINT item\nNEXT");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidForEachSource);
        // Переменная цикла объявлена, поэтому тело не даёт каскад MB2001.
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnknownName);
    }

    [Fact]
    public void IncompatibleAsClauseReportsDedicatedCode()
    {
        var diagnostics = BindWithDiagnostics("DIM values[] AS STRING = [\"a\"]\nFOR EACH item AS INTEGER IN values[]\nPRINT item\nNEXT");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidForEachElementType);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnknownName);
    }

    [Fact]
    public void MismatchedNextNameReportsNextCounterCode()
    {
        var diagnostics = BindWithDiagnostics("DIM values[1 TO 2] AS INTEGER\nFOR EACH item IN values[]\nPRINT item\nNEXT other");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.InvalidNextCounter);
    }

    [Fact]
    public void LoopVariableIsNotVisibleAfterNext()
    {
        var diagnostics = BindWithDiagnostics("DIM values[1 TO 2] AS INTEGER\nFOR EACH item IN values[]\nPRINT item\nNEXT\nPRINT item");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.UnknownName);
    }

    [Theory]
    [InlineData("DIM each AS INTEGER = 3")]
    [InlineData("DIM in AS INTEGER = 3")]
    [InlineData("CONST each AS INTEGER = 3")]
    [InlineData("SUB each()\nEND SUB")]
    [InlineData("FUNCTION in() AS INTEGER\nRETURN 1\nEND FUNCTION")]
    [InlineData("TYPE each\nValue AS INTEGER\nEND TYPE")]
    public void ReservedWordsCannotBeUsedAsNames(string source)
    {
        var diagnostics = AllDiagnostics(source);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.ExpectedIdentifier);
    }

    [Fact]
    public void ReservedWordInExpressionPositionIsRejected()
    {
        // В позиции выражения ключевое слово не является ни именем, ни значением.
        var diagnostics = AllDiagnostics("DIM values[1 TO 2] AS INTEGER\nvalues[each] = 1");

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == DiagnosticCodes.ExpectedExpression);
    }

    [Fact]
    public void ReservedWordReportsSinglePreciseDiagnostic()
    {
        var diagnostics = AllDiagnostics("DIM each AS INTEGER = 3");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.ExpectedIdentifier, diagnostic.Code);
        Assert.Contains("'each'", diagnostic.Message);
        Assert.Contains("нельзя использовать как имя", diagnostic.Message);
    }

    [Fact]
    public void OrdinaryNamesContainingReservedWordsStillWork()
    {
        // Зарезервированы ровно слова EACH и IN, а не любые имена с этими буквами.
        var program = Bind("DIM in2 AS INTEGER = 3\nDIM eachItem AS INTEGER = in2\nPRINT eachItem", out var diagnostics);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NumericForIsUnchangedByEachSupport()
    {
        var program = Bind("FOR i = 1 TO 3\nPRINT i\nNEXT i", out var diagnostics);

        Assert.Empty(diagnostics);
        Assert.IsType<BoundForStatement>(program.GlobalStatements[0]);
    }

    [Fact]
    public void ExitAndContinueForAreValidInsideForEach()
    {
        var program = Bind(
            "DIM values[1 TO 3] AS INTEGER\nFOR EACH item IN values[]\nCONTINUE FOR\nEXIT FOR\nNEXT",
            out var diagnostics);

        Assert.Empty(diagnostics);
        var loop = Assert.IsType<BoundForEachStatement>(program.GlobalStatements[1]);
        Assert.Collection(
            loop.Statements,
            statement => Assert.IsType<BoundContinueForStatement>(statement),
            statement => Assert.IsType<BoundExitForStatement>(statement));
    }

    [Fact]
    public void ParserAcceptsBothNextForms()
    {
        var withName = Parser.Parse("FOR EACH item IN values[]\nNEXT item", out var withNameDiagnostics);
        var withoutName = Parser.Parse("FOR EACH item IN values[]\nNEXT", out var withoutNameDiagnostics);

        Assert.Empty(withNameDiagnostics);
        Assert.Empty(withoutNameDiagnostics);
        var first = Assert.IsType<ForEachStatementSyntax>(
            Assert.IsType<GlobalStatementSyntax>(withName.Members[0]).Statement);
        Assert.Equal("item", first.NextCounterIdentifier?.Text);
        var second = Assert.IsType<ForEachStatementSyntax>(
            Assert.IsType<GlobalStatementSyntax>(withoutName.Members[0]).Statement);
        Assert.Null(second.NextCounterIdentifier);
    }

    [Fact]
    public void ParserKeepsModifiersAndTypeClause()
    {
        var program = Parser.Parse("FOR EACH BYREF item AS STRING IN values[]\nNEXT", out var diagnostics);

        Assert.Empty(diagnostics);
        var loop = Assert.IsType<ForEachStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
        Assert.NotNull(loop.ByRefKeyword);
        Assert.Null(loop.ByValKeyword);
        Assert.NotNull(loop.TypeClause);
        Assert.Equal("IN", loop.InKeyword.Text);
    }

    [Fact]
    public void ParserReportsMissingIn()
    {
        var program = Parser.Parse("FOR EACH item values[]\nNEXT", out var diagnostics);

        Assert.NotEmpty(diagnostics);
        Assert.IsType<ForEachStatementSyntax>(Assert.IsType<GlobalStatementSyntax>(program.Members[0]).Statement);
    }

    private static BoundProgram Bind(string source, out DiagnosticBag diagnostics) =>
        Binder.Bind(Parse(source), out diagnostics, checkGraphicsModes: false);

    private static IReadOnlyList<Diagnostic> BindWithDiagnostics(string source)
    {
        _ = Binder.Bind(Parse(source), out var diagnostics, checkGraphicsModes: false);
        return diagnostics.ToList();
    }

    // Для случаев с ожидаемой ошибкой: хелпер Parse требует чистого разбора,
    // поэтому здесь возвращаются ошибки разбора, а связывание выполняется только
    // на непротиворечивом дереве — так же поступает конвейер компиляции.
    private static IReadOnlyList<Diagnostic> AllDiagnostics(string source)
    {
        var syntax = Parser.Parse(source, out var parsing, document: null);
        if (parsing.Count > 0)
        {
            return parsing.ToList();
        }

        _ = Binder.Bind(syntax, out var binding, checkGraphicsModes: false);
        return binding.ToList();
    }

    private static ProgramSyntax Parse(string source)
    {
        var syntax = Parser.Parse(source, out var parsing, document: null);
        Assert.Empty(parsing);
        return syntax;
    }
}
