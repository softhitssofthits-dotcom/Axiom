using System.Text;
using Axiom.Core;
using Axiom.Parsing;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Execution;

/// <summary>Правка в координатах UTF-16 исходного, а не преобразованного текста.</summary>
public sealed record ArraySyntaxMigrationEdit(TextSpan Span, string Replacement, string Kind, string? Document = null);

public sealed record ArraySyntaxMigrationDiagnostic(string Code, string Message, TextLocation Location, string? Document = null);

public sealed record ArraySyntaxMigrationResult(
    string OriginalSource,
    string TransformedSource,
    IReadOnlyList<ArraySyntaxMigrationEdit> Edits,
    IReadOnlyList<ArraySyntaxMigrationDiagnostic> Diagnostics,
    bool CanApply);

/// <summary>
/// Предварительный просмотр перевода массивов. Не выполняет программу, не читает
/// зависимости с диска и не записывает файлы. Ошибка любого переданного модуля
/// запрещает все правки; имена разрешаются только настоящим связывателем.
/// </summary>
public static class ArraySyntaxMigrationService
{
    private const string MigrationError = "AXARRAYMIGRATION";

    public static ArraySyntaxMigrationResult Migrate(
        string sourceText,
        string? sourceName = null,
        IEnumerable<SourceDocument>? additionalDocuments = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        var dependencies = additionalDocuments?.ToArray() ?? [];
        var entryName = string.IsNullOrWhiteSpace(sourceName) ? Binder.SingleModuleName : Path.GetFileNameWithoutExtension(sourceName);
        var diagnostics = new List<ArraySyntaxMigrationDiagnostic>();
        var modules = ParseModules(sourceText, sourceName, dependencies, entryName, square: false, diagnostics);
        if (diagnostics.Count > 0)
        {
            // Только полностью корректная новая программа является неподвижной
            // точкой. Квадратные знаки в строке/комментарии этот путь не выбирают.
            var squareDiagnostics = new List<ArraySyntaxMigrationDiagnostic>();
            var squareModules = ParseModules(sourceText, sourceName, dependencies, entryName, square: true, squareDiagnostics);
            if (squareDiagnostics.Count == 0)
            {
                Bind(squareModules, entryName, squareDiagnostics);
                return Result(sourceText, sourceText, [], squareDiagnostics);
            }
            return Result(sourceText, sourceText, [], diagnostics);
        }

        var bound = Bind(modules, entryName, diagnostics);
        if (bound is null || diagnostics.Count > 0) return Result(sourceText, sourceText, [], diagnostics);

        var context = new MigrationContext(sourceName, bound);
        CollectEdits(modules[0].Syntax, context);
        var edits = context.Edits.DistinctBy(edit => (edit.Span, edit.Replacement))
            .OrderBy(edit => edit.Span.Start).ThenBy(edit => edit.Span.Length).ToArray();
        for (var index = 0; index < edits.Length; index++)
        {
            var edit = edits[index];
            if (edit.Span.End > sourceText.Length || (index > 0 &&
                (edits[index - 1].Span.End > edit.Span.Start || edits[index - 1].Span.Start == edit.Span.Start)))
            {
                diagnostics.Add(new(MigrationError, "Получены пересекающиеся или недопустимые диапазоны правок.",
                    new TextLocation(edit.Span, 1, 1, sourceName), sourceName));
                return Result(sourceText, sourceText, [], diagnostics);
            }
        }

        var builder = new StringBuilder(sourceText);
        foreach (var edit in edits.Reverse())
        {
            builder.Remove(edit.Span.Start, edit.Span.Length);
            builder.Insert(edit.Span.Start, edit.Replacement);
        }
        var transformed = builder.ToString();
        var transformedModules = ParseModules(transformed, sourceName, dependencies, entryName, square: true, diagnostics);
        if (diagnostics.Count == 0) Bind(transformedModules, entryName, diagnostics);
        return Result(sourceText, transformed, edits, diagnostics);
    }

    /// <summary>Результат относится к документу выбранного модуля, зависимости не изменяются.</summary>
    public static ArraySyntaxMigrationResult Migrate(IReadOnlyList<SourceDocument> documents, string entryName)
    {
        ArgumentNullException.ThrowIfNull(documents);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);
        var entry = documents.FirstOrDefault(document => string.Equals(document.ModuleName, entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return Result(string.Empty, string.Empty, [], [new(MigrationError, $"Не найден исходник точки входа '{entryName}'.",
                new TextLocation(new TextSpan(0, 0), 1, 1, entryName), entryName)]);
        return Migrate(entry.Text, entry.Name, documents.Where(document => !ReferenceEquals(document, entry)));
    }

    private static List<ModuleSyntax> ParseModules(string text, string? name, SourceDocument[] dependencies,
        string entryName, bool square, List<ArraySyntaxMigrationDiagnostic> diagnostics)
    {
        ProgramSyntax Parse(string source, string? document)
        {
            DiagnosticBag errors;
            var syntax = square
                ? Parser.ParseSquareBracketSyntax(source, out errors, document)
                : Parser.ParseLegacySyntax(source, out errors, document);
            AddDiagnostics(diagnostics, errors);
            return syntax;
        }
        var modules = new List<ModuleSyntax> { new(entryName, Parse(text, name)) };
        foreach (var document in dependencies)
        {
            // Зависимости могут быть уже переведены или ещё оставаться старыми.
            // Каждый файл получает свой признак синтаксиса и свои области имён.
            var dependency = Parser.ParseLegacySyntax(document.Text, out var errors, document.Name);
            if (errors.Any(error => error.Severity == DiagnosticSeverity.Error))
                dependency = Parser.ParseSquareBracketSyntax(document.Text, out errors, document.Name);
            AddDiagnostics(diagnostics, errors);
            modules.Add(new(document.ModuleName, dependency));
        }
        return modules;
    }

    private static BoundProgram? Bind(IReadOnlyList<ModuleSyntax> modules, string entryName,
        List<ArraySyntaxMigrationDiagnostic> diagnostics)
    {
        try
        {
            var bound = Binder.BindModules(modules, entryName, out var errors,
                checkGraphicsModes: false, collectEditorSymbols: true);
            AddDiagnostics(diagnostics, errors);
            return bound;
        }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException)
        {
            diagnostics.Add(new(MigrationError, $"Не удалось связать исходник для миграции: {error.Message}",
                modules[0].Syntax.EndOfFileToken.Location));
            return null;
        }
    }

    private static void CollectEdits(ProgramSyntax syntax, MigrationContext context)
    {
        // Общий обход исключает повторное посещение совместно используемых узлов
        // и охватывает интерполяцию, вложенные процедуры и все контейнеры инструкций.
        var nodes = BoundTreeTraversal.Descendants(syntax).OfType<SyntaxNode>().ToArray();
        var movedTypes = new HashSet<ArrayTypeSyntax>();
        var suppressedNames = new HashSet<ExpressionSyntax>();
        foreach (var node in nodes)
        {
            switch (node)
            {
                case VariableDeclarationSyntax declaration:
                    foreach (var declarator in declaration.Declarators)
                        CollectDeclaration(declarator, declaration.TypeClause?.Type, context, movedTypes);
                    break;
                case TypeFieldSyntax field:
                    foreach (var declarator in field.Declarators)
                        CollectDeclaration(declarator, field.TypeClause.Type, context, movedTypes);
                    break;
                case ConstantDeclarationSyntax constant when constant.TypeClause.Type is ArrayTypeSyntax constantArray:
                    MoveTypeSuffix(constant.Identifier, constantArray, context, movedTypes);
                    break;
                case ParameterSyntax parameter when parameter.TypeClause.Type is ArrayTypeSyntax array:
                    MoveTypeSuffix(parameter.Identifier, array, context, movedTypes);
                    break;
                case RedimStatementSyntax redim:
                    SuppressName(redim.Target, suppressedNames);
                    ChangeDelimiters(redim.ArrayBounds.OpenDelimiterToken, redim.ArrayBounds.CloseDelimiterToken, context, "bounds");
                    break;
                case CallExpressionSyntax call when context.TypeOf(call.Target) is ArrayTypeSymbol:
                    SuppressName(call.Target, suppressedNames);
                    ChangeDelimiters(call.OpenParenToken, call.CloseParenToken, context, "access");
                    break;
            }
        }

        foreach (var node in nodes)
        {
            switch (node)
            {
                case ArrayBoundsSyntax bounds:
                    ChangeDelimiters(bounds.OpenDelimiterToken, bounds.CloseDelimiterToken, context, "bounds");
                    break;
                case ArrayTypeSyntax array when !movedTypes.Contains(array):
                    ChangeDelimiters(array.OpenDelimiterToken, array.CloseDelimiterToken, context, "type");
                    break;
                case NameExpressionSyntax name when !suppressedNames.Contains(name) && context.TypeOf(name) is ArrayTypeSymbol:
                    context.Insert(name.IdentifierToken, "[]", "whole-array");
                    break;
                case FieldAccessExpressionSyntax field when !suppressedNames.Contains(field) && context.TypeOf(field) is ArrayTypeSymbol:
                    context.Insert(field.Identifier, "[]", "whole-array");
                    break;
            }
        }
    }

    private static void CollectDeclaration(VariableDeclaratorSyntax declarator, TypeSyntax? type,
        MigrationContext context, HashSet<ArrayTypeSyntax> movedTypes)
    {
        if (declarator.ArrayBounds is not null) return;
        if (type is ArrayTypeSyntax array)
            MoveTypeSuffix(declarator.Identifier, array, context, movedTypes);
        else if (type is null && context.SymbolType(declarator.Identifier, declaration: true) is ArrayTypeSymbol inferred)
            context.Insert(declarator.Identifier, RankSuffix(inferred.Rank), "inferred-declaration");
    }

    private static void MoveTypeSuffix(SyntaxToken identifier, ArrayTypeSyntax array,
        MigrationContext context, HashSet<ArrayTypeSyntax> movedTypes)
    {
        context.Insert(identifier, RankSuffix(array.Rank), "declaration");
        if (!movedTypes.Add(array)) return;
        // Удаляются только синтаксические токены, не пробелы, комментарии или EOL.
        context.Replace(array.OpenDelimiterToken, string.Empty, "moved-type");
        foreach (var comma in array.CommaTokens) context.Replace(comma, string.Empty, "moved-type");
        context.Replace(array.CloseDelimiterToken, string.Empty, "moved-type");
    }

    private static void SuppressName(ExpressionSyntax expression, HashSet<ExpressionSyntax> suppressed)
    {
        // Прямая цель получает скобки вызова вместо суффикса имени. Внутри
        // группировки имя остаётся самостоятельным выражением: (a[])[1].
        suppressed.Add(expression);
    }

    private static void ChangeDelimiters(SyntaxToken open, SyntaxToken close, MigrationContext context, string kind)
    {
        if (open.Kind != SyntaxKind.OpenParenToken) return;
        context.Replace(open, "[", kind);
        context.Replace(close, "]", kind);
    }

    private static string RankSuffix(int rank) => "[" + new string(',', rank - 1) + "]";

    private static void AddDiagnostics(List<ArraySyntaxMigrationDiagnostic> destination, IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            destination.Add(new(diagnostic.Code, diagnostic.Message, diagnostic.Location, diagnostic.Location.Document));
    }

    private static ArraySyntaxMigrationResult Result(string original, string transformed,
        IReadOnlyList<ArraySyntaxMigrationEdit> edits, IReadOnlyList<ArraySyntaxMigrationDiagnostic> diagnostics) =>
        diagnostics.Count == 0 ? new(original, transformed, edits, diagnostics, true)
            : new(original, original, [], diagnostics, false);

    private sealed class MigrationContext
    {
        private readonly string? _document;
        private readonly EditorSymbolOccurrence[] _symbols;
        private readonly BoundCallExpression[] _calls;

        public MigrationContext(string? document, BoundProgram bound)
        {
            _document = document;
            _symbols = bound.EditorSymbols.Where(symbol => SameDocument(symbol.Location.Document)).ToArray();
            _calls = BoundTreeTraversal.Descendants(bound).OfType<BoundCallExpression>()
                .Where(call => SameDocument(call.Location.Document)).ToArray();
        }

        public List<ArraySyntaxMigrationEdit> Edits { get; } = [];
        private bool SameDocument(string? document) => string.Equals(document, _document, StringComparison.OrdinalIgnoreCase);

        private Symbol? SymbolAt(SyntaxToken token, bool? declaration = null)
        {
            var matches = _symbols.Where(symbol => symbol.Location.Span == token.Span &&
                (declaration is null || symbol.IsDeclaration == declaration)).Select(symbol => symbol.Symbol)
                .Distinct().ToArray();
            return matches.Length == 1 ? matches[0] as Symbol : null;
        }

        public TypeSymbol? SymbolType(SyntaxToken token, bool? declaration = null) => SymbolAt(token, declaration) switch
        {
            VariableSymbol variable => variable.Type,
            TypeFieldSymbol field => field.Type,
            FunctionSymbol function => FunctionTypeSymbol.FromFunction(function),
            _ => null
        };

        public TypeSymbol? TypeOf(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case NameExpressionSyntax name: return SymbolType(name.IdentifierToken);
                case FieldAccessExpressionSyntax field: return SymbolType(field.Identifier);
                case ParenthesizedExpressionSyntax grouped: return TypeOf(grouped.Expression);
                case CallExpressionSyntax call:
                {
                    var target = TypeOf(call.Target);
                    if (target is FunctionTypeSymbol function) return function.ReturnType;
                    if (target is ArrayTypeSymbol array)
                    {
                        if (call.Arguments.Count == 0) return array;
                        var rank = array.Rank - call.Arguments.Count(argument => argument is not RangeExpressionSyntax);
                        return rank > 0 ? ArrayTypeSymbol.Create(array.ElementType, rank) : array.ElementType;
                    }
                    // Встроенные функции намеренно отсутствуют в EditorSymbols.
                    // Их результат берётся из реально связанного вызова, не из имени.
                    var token = call.Target switch
                    {
                        NameExpressionSyntax name => name.IdentifierToken,
                        FieldAccessExpressionSyntax field => field.Identifier,
                        _ => null
                    };
                    if (token is null) return null;
                    var types = _calls.Where(bound => bound.Location.Span == token.Span)
                        .Select(bound => bound.Type).Distinct().ToArray();
                    return types.Length == 1 ? types[0] : null;
                }
                default: return null;
            }
        }

        public void Insert(SyntaxToken token, string text, string kind) =>
            Edits.Add(new(new TextSpan(token.Span.End, 0), text, kind, _document));
        public void Replace(SyntaxToken token, string text, string kind) =>
            Edits.Add(new(token.Span, text, kind, _document));
    }
}
