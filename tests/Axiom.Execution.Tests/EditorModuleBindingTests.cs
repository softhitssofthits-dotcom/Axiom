using Axiom.Lexing;
using Axiom.Parsing;
using Axiom.Semantics;

namespace Axiom.Execution.Tests;

/// <summary>
/// Редакторские метаданные импортов не должны зависеть от успешного выпуска
/// байт-кода всей программы: посторонняя ошибка в main не отменяет корректно
/// связанный импортированный символ.
/// </summary>
public sealed class EditorModuleBindingTests
{
    private const string Helper = "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION";

    [Fact]
    public void ImportedSymbolStaysAvailableWhenMainHasAnotherSemanticError()
    {
        const string main = "IMPORT helper\nDIM localCount AS INTEGER=1\nPRINT Answer()\nPRINT missing";

        var hovers = Hovers(main, "main.axm", new SourceDocument("helper.axm", Helper));

        Assert.Contains(hovers, hover => hover.Name == "Answer" && hover.Kind == "function");
        Assert.Contains(hovers, hover => hover.Name == "localCount" && hover.Kind == "variable");
    }

    [Fact]
    public void ImportedSymbolStaysAvailableWhenMainHasASyntaxError()
    {
        const string main = "IMPORT helper\nPRINT Answer()\nPRINT (";

        var hovers = Hovers(main, "main.axm", new SourceDocument("helper.axm", Helper));

        Assert.Contains(hovers, hover => hover.Name == "Answer" && hover.Kind == "function");
    }

    [Fact]
    public void PrivateExportIsNotInvented()
    {
        var hovers = Hovers("IMPORT helper\nPRINT Answer()", "main.axm",
            new SourceDocument("helper.axm", "FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION"));

        Assert.DoesNotContain(hovers, hover => hover.Name == "Answer");
    }

    [Fact]
    public void MissingModuleKeepsImportsEmpty()
    {
        var binding = EditorModuleBindingService.Bind("IMPORT missing\nPRINT Answer()", "main.axm", []);

        Assert.Empty(binding.ImportedModules);
        Assert.Contains(binding.Diagnostics, diagnostic => diagnostic.Code == "MB2049");
    }

    [Fact]
    public void NamedSingleFileWithoutDependenciesStillBinds()
    {
        var binding = EditorModuleBindingService.Bind("PRINT 1", "main.axm", []);

        Assert.Empty(binding.ImportedModules);
        Assert.NotNull(binding.Syntax);
    }

    private static IReadOnlyList<EditorHoverInfo> Hovers(string main, string mainPath, params SourceDocument[] documents)
    {
        var binding = EditorModuleBindingService.Bind(main, mainPath, documents);
        var identifiers = Lexer.Lex(main, out _)
            .Where(token => token.Kind == TokenKind.IdentifierToken)
            .Select(token => new EditorIdentifier(token.Text, token.Location.Span))
            .ToArray();
        return Binder.GetEditorHovers(binding.Syntax, identifiers, binding.ImportedModules);
    }
}
