using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed record EditorIdentifier(string Name, TextSpan Span);
public sealed record EditorHoverInfo(int Start, int Length, string Name, string Kind,
    string? Type = null, string? Signature = null, IReadOnlyList<string>? Fields = null,
    IReadOnlyList<string>? Parameters = null);

public sealed partial class Binder
{
    private Dictionary<BoundStatement, StatementSyntax>? _editorStatements;
    private List<EditorHoverInfo>? _editorConstants;
    // Подсказки используют те же символы и области видимости, что и исполнение.
    public static IReadOnlyList<EditorHoverInfo> GetEditorHovers(ProgramSyntax syntax,
        IReadOnlyList<EditorIdentifier> identifiers, IReadOnlyList<ModuleSymbol>? imports = null)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentNullException.ThrowIfNull(identifiers);
        var binder = new Binder(imports ?? [], syntax.EndOfFileToken.Location) { _editorStatements = [], _editorConstants = [] };
        var program = binder.BindProgram(syntax);
        var items = new Dictionary<int, EditorHoverInfo>();

        void Add(TextSpan span, string name, string kind, string? type = null,
            string? signature = null, IReadOnlyList<string>? fields = null, IReadOnlyList<string>? parameters = null)
        {
            if (span.Length == 0 || type == TypeSymbol.Error.Name) return;
            items[span.Start] = new(span.Start, span.Length, name, kind, type, signature, fields, parameters);
        }
        void Variable(TextSpan span, VariableSymbol symbol) => Add(span, symbol.Name,
            symbol is ParameterSymbol ? "parameter" :
            symbol.Type == TypeSymbol.RuntimeError && symbol.IsReadOnly ? "errorVariable" :
            symbol.IsReadOnly ? "constant" : "variable", symbol.Type.Name);
        void Function(TextSpan span, FunctionSymbol symbol)
        {
            if (symbol.IsBuiltIn) return; // Все перегрузки берутся из реестра языка.
            if (symbol.ReturnType == TypeSymbol.Error || symbol.Parameters.Any(p => p.Type == TypeSymbol.Error)) return;
            var parameters = symbol.Parameters.Where(p => !p.Name.Equals("THIS", StringComparison.OrdinalIgnoreCase)).Select(p =>
                (p.IsByRef ? "BYREF " : "") + p.Name + " AS " + p.Type.Name).ToArray();
            var signature = (symbol.IsSub ? "SUB " : "FUNCTION ") + symbol.ShortName +
                "(" + string.Join(", ", parameters) + ")" + (symbol.IsSub ? "" : " AS " + symbol.ReturnType.Name);
            Add(span, symbol.ShortName, symbol.IsSub ? "sub" : "function", signature: signature, parameters: parameters);
        }
        void Expression(BoundExpression expression)
        {
            foreach (var child in BoundExpressionTraversal.Immediate(expression)) Expression(child);
            switch (expression)
            {
                case BoundVariableExpression variable:
                    Variable(variable.Location.Span, variable.Variable);
                    break;
                case BoundFieldAccessExpression field:
                    Add(field.Location.Span, field.Field.Name, "field", field.Type.Name);
                    break;
                case BoundFunctionExpression function:
                    Function(function.Location.Span, function.Function);
                    break;
            }
        }
        void Statement(BoundStatement statement)
        {
            if (statement is BoundVariableDeclarationsStatement declarations)
                foreach (var declaration in declarations.Declarations) Statement(declaration);
            if (statement is BoundVariableDeclarationStatement variable)
                Variable(variable.IdentifierLocation.Span, variable.Variable);
            if (binder._editorStatements.TryGetValue(statement, out var original))
            {
                switch (statement, original)
                {
                    case (BoundForStatement loop, ForStatementSyntax loopSyntax):
                        Variable(loopSyntax.Identifier.Span, loop.Variable);
                        if (loopSyntax.NextCounterIdentifier is { } next && next.Text.Equals(loop.Variable.Name, StringComparison.OrdinalIgnoreCase))
                            Variable(next.Span, loop.Variable);
                        break;
                    case (BoundInputStatement input, InputStatementSyntax inputSyntax):
                        foreach (var token in inputSyntax.Identifiers)
                        {
                            var symbol = input.Variables.FirstOrDefault(v => v.Name.Equals(token.Text, StringComparison.OrdinalIgnoreCase));
                            if (symbol is not null) Variable(token.Span, symbol);
                        }
                        break;
                    case (BoundOpenStatement open, OpenStatementSyntax openSyntax):
                        Variable(openSyntax.FileIdentifier.Span, open.FileVariable);
                        break;
                    case (BoundCloseStatement close, CloseStatementSyntax closeSyntax):
                        Variable(closeSyntax.FileIdentifier.Span, close.FileVariable);
                        break;
                    case (BoundTryStatement { CatchVariable: not null } tryStatement, TryStatementSyntax { CatchClause: not null } trySyntax):
                        Variable(trySyntax.CatchClause.Identifier.Span, tryStatement.CatchVariable);
                        break;
                }
            }
            if (statement is BoundOnKeyStatement { HandlerExpression: not null } key) Expression(key.HandlerExpression);
            if (statement is BoundOnMouseStatement { HandlerExpression: not null } mouse) Expression(mouse.HandlerExpression);
            if (statement is BoundSelectCaseStatement select)
                foreach (var selector in select.Clauses.SelectMany(clause => clause.Selectors))
                {
                    switch (selector)
                    {
                        case BoundCaseValueSelector value: Expression(value.Expression); break;
                        case BoundCaseRangeSelector range: Expression(range.LowerBound); Expression(range.UpperBound); break;
                        case BoundCaseComparisonSelector comparison: Expression(comparison.Expression); break;
                    }
                }
            foreach (var expression in BoundExpressionTraversal.Immediate(statement)) Expression(expression);
            foreach (var child in BoundExpressionTraversal.Nested(statement)) Statement(child);
        }

        foreach (var typeSyntax in syntax.Members.OfType<TypeDeclarationSyntax>())
        {
            if (!binder._declaredTypes.TryGetValue(typeSyntax.Identifier.Text, out var type)) continue;
            var fields = type.Fields.Select(f => f.Name + " AS " + f.Type.Name).ToArray();
            foreach (var token in identifiers.Where(t => t.Name.Equals(type.Name, StringComparison.OrdinalIgnoreCase)))
                Add(token.Span, type.Name, "userType", fields: fields);
            foreach (var fieldSyntax in typeSyntax.Fields)
                foreach (var declaration in fieldSyntax.Declarators)
                {
                    var field = type.Fields.FirstOrDefault(f => f.Name.Equals(declaration.Identifier.Text, StringComparison.OrdinalIgnoreCase));
                    if (field is not null) Add(declaration.Identifier.Span, field.Name, "field", field.Type.Name);
                }
        }
        foreach (var (member, symbol) in binder._declaredFunctions)
        {
            var (identifier, parameters) = member switch
            {
                SubDeclarationSyntax sub => (sub.Identifier, sub.Parameters),
                FunctionDeclarationSyntax function => (function.Identifier, function.Parameters),
                _ => (null, null)
            };
            if (identifier is null || parameters is null) continue;
            Function(identifier.Span, symbol);
            foreach (var parameter in parameters)
            {
                var bound = symbol.Parameters.FirstOrDefault(p => p.Name.Equals(parameter.Identifier.Text, StringComparison.OrdinalIgnoreCase));
                if (bound is not null) Variable(parameter.Identifier.Span, bound);
            }
        }
        foreach (var statement in program.GlobalStatements) Statement(statement);
        foreach (var routine in program.Routines)
            foreach (var statement in routine.Statements) Statement(statement);
        foreach (var constant in binder._editorConstants) items[constant.Start] = constant;
        return items.Values.OrderBy(item => item.Start).ToArray();
    }
}
