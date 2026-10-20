using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed record EditorSymbolOccurrence(Symbol Symbol, TextLocation Location, bool IsDeclaration);

public sealed partial class Binder
{
    private List<EditorSymbolOccurrence>? _editorReferences;
    private List<SyntaxToken>? _editorGlobalConstantReferences;
    // Сбор выполняется только по запросу редактора и использует уже связанные символы.
    private IReadOnlyList<EditorSymbolOccurrence> CollectEditorSymbols()
    {
        var result = new List<EditorSymbolOccurrence>(_editorReferences ?? []);
        void Add(Symbol symbol, TextLocation location, bool declaration = false)
        {
            if (location.Span.Length > 0 && symbol is not FunctionSymbol { IsBuiltIn: true })
                result.Add(new(symbol, location, declaration));
        }
        foreach (var (member, symbol) in _declaredFunctions)
        {
            var (identifier, parameters) = member switch
            {
                SubDeclarationSyntax sub => (sub.Identifier, sub.Parameters),
                FunctionDeclarationSyntax function => (function.Identifier, function.Parameters),
                _ => (null, null)
            };
            if (identifier is null || parameters is null) continue;
            Add(symbol, identifier.Location, true);
            foreach (var parameter in parameters)
                if (symbol.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Identifier.Text, StringComparison.OrdinalIgnoreCase)) is { } bound)
                    Add(bound, parameter.Identifier.Location, true);
        }
        // Предварительное вычисление размеров может предшествовать объявлению глобальной CONST.
        foreach (var token in _editorGlobalConstantReferences ?? [])
            if (_scope.TryLookupVariable(token.Text, out var constant) && constant is { IsReadOnly: true })
                Add(constant, token.Location);
        // Обход общий с редакторскими проекциями Host: правила посещения узлов
        // заданы в BoundTreeTraversal, а не повторяются отражением в двух местах.
        foreach (var node in BoundTreeTraversal.Descendants(_globalStatements.Cast<object>().Concat(_routines)))
        {
            switch (node)
            {
                case BoundVariableDeclarationStatement declaration: Add(declaration.Variable, declaration.IdentifierLocation, true); break;
                case BoundVariableExpression variable: Add(variable.Variable, variable.Location); break;
                case BoundFunctionExpression function: Add(function.Function, function.Location); break;
                case BoundFieldAccessExpression field: Add(field.Field, field.Location); break;
            }
            if (node is not BoundStatement statement || _editorStatements?.TryGetValue(statement, out var original) != true) continue;
            switch (statement, original)
            {
                case (BoundForStatement loop, ForStatementSyntax text):
                    Add(loop.Variable, text.Identifier.Location, loop.DeclaresVariable);
                    if (text.NextCounterIdentifier is { } next && next.Text.Equals(loop.Variable.Name, StringComparison.OrdinalIgnoreCase))
                        Add(loop.Variable, next.Location);
                    break;
                case (BoundInputStatement input, InputStatementSyntax text):
                    foreach (var token in text.Identifiers)
                        if (input.Variables.FirstOrDefault(v => v.Name.Equals(token.Text, StringComparison.OrdinalIgnoreCase)) is { } variable)
                            Add(variable, token.Location);
                    break;
                case (BoundOpenStatement open, OpenStatementSyntax text): Add(open.FileVariable, text.FileIdentifier.Location); break;
                case (BoundCloseStatement close, CloseStatementSyntax text): Add(close.FileVariable, text.FileIdentifier.Location); break;
                case (BoundTryStatement { CatchVariable: not null } guarded, TryStatementSyntax { CatchClause: not null } text):
                    Add(guarded.CatchVariable, text.CatchClause.Identifier.Location, true); break;
            }
        }
        return result.Distinct().ToArray();
    }
}
