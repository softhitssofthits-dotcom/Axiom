using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Execution;

/// <summary>
/// Результат терпимого связывания для редакторских метаданных. Программа может
/// содержать ошибки: для подсказок и навигации нужны только корректно
/// связанные модули и дерево текущего документа, а не исполнимый байт-код.
/// </summary>
public sealed record EditorModuleBinding(
    ProgramSyntax Syntax,
    IReadOnlyList<ModuleSymbol> ImportedModules,
    IReadOnlyList<Diagnostic> Diagnostics);

/// <summary>
/// Связывание для редактора: импорты корректных зависимостей остаются
/// доступными при ошибке в другом месте программы. Ошибочные и закрытые имена
/// не подменяются предположениями — они просто отсутствуют.
/// </summary>
public static class EditorModuleBindingService
{
    public static EditorModuleBinding Bind(
        string sourceText,
        string? sourceName,
        IReadOnlyList<SourceDocument> additionalDocuments)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(additionalDocuments);

        var diagnostics = new List<Diagnostic>();
        var syntax = Parser.Parse(sourceText, out var syntaxDiagnostics, sourceName);
        diagnostics.AddRange(syntaxDiagnostics);

        var entryModuleName = string.IsNullOrWhiteSpace(sourceName)
            ? Binder.SingleModuleName
            : Path.GetFileNameWithoutExtension(sourceName);
        var modules = new List<ModuleSyntax> { new(entryModuleName, syntax) };

        foreach (var document in additionalDocuments)
        {
            var documentSyntax = Parser.Parse(document.Text, out var documentDiagnostics, document.Name);
            diagnostics.AddRange(documentDiagnostics);
            modules.Add(new ModuleSyntax(document.ModuleName, documentSyntax));
        }

        BoundProgram program;
        try
        {
            program = Binder.BindModules(modules, entryModuleName, out var semanticDiagnostics,
                checkGraphicsModes: false);
            diagnostics.AddRange(semanticDiagnostics);
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        {
            // Непредвиденный сбой связывания не должен ломать подсказку: лучше
            // вернуть документ без импортов, чем неверные имена.
            diagnostics.Add(new Diagnostic(
                DiagnosticCodes.InvalidRuntimeState,
                $"Не удалось связать модули для редакторских метаданных: {error.Message}",
                default));
            return new EditorModuleBinding(syntax, [], diagnostics);
        }

        var importedModules = new List<ModuleSymbol>();
        foreach (var import in syntax.Members.OfType<ImportStatementSyntax>())
        {
            if (program.Modules.TryGetValue(import.ModuleName, out var module)
                && !string.Equals(import.ModuleName, entryModuleName, StringComparison.OrdinalIgnoreCase))
            {
                importedModules.Add(module);
            }
        }

        return new EditorModuleBinding(syntax, importedModules, diagnostics);
    }
}
