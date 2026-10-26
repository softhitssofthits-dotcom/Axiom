using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private TypeSymbol ResolveTupleType(TupleTypeSyntax syntax)
    {
        var types = syntax.ElementTypes.Select(ResolveType).ToArray();
        var names = syntax.Names.Select(n => n?.Text).ToArray();
        var valid = types.Length >= 2 && (names.All(n => n is null) || names.All(n => n is not null));
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
            if (name is not null && (!used.Add(name) || IsTuplePositionName(name))) valid = false;
        foreach (var type in types)
            if (type == TypeSymbol.Error || type is FunctionTypeSymbol || CollectionKeyTypeSupport.IsResourceType(type)) valid = false;
        if (!valid)
        {
            ReportTuple("Кортеж требует не менее двух допустимых типов; имена должны быть уникальными, заданы у всех компонентов и не иметь вид ItemN.", syntax.OpenParenToken.Location);
            return TypeSymbol.Error;
        }
        return new TupleTypeSymbol(types, names);
    }

    private static bool IsTuplePositionName(string name) => name.StartsWith("Item", StringComparison.OrdinalIgnoreCase)
        && name.Length > 4 && name.AsSpan(4).IndexOfAnyExceptInRange('0', '9') < 0;

    private BoundExpression BindTupleExpression(TupleExpressionSyntax syntax, TypeSymbol? expected)
    {
        var target = expected as TupleTypeSymbol;
        if (target is not null && target.Fields.Count != syntax.Elements.Count)
        {
            ReportTuple("Число значений не совпадает с числом компонентов кортежа.", syntax.OpenParenToken.Location);
            return new BoundErrorExpression(syntax.OpenParenToken.Location);
        }
        var values = syntax.Elements.Select((element, i) => BindExpression(element, target?.Fields[i].Type)).ToArray();
        if (values.Any(v => v.Type == TypeSymbol.Error)) return new BoundErrorExpression(syntax.OpenParenToken.Location);
        if (values.Any(v => v.Type is FunctionTypeSymbol || CollectionKeyTypeSupport.IsResourceType(v.Type)))
        {
            ReportTuple("Функция или внешний ресурс не допускается как компонент кортежа.", syntax.OpenParenToken.Location);
            return new BoundErrorExpression(syntax.OpenParenToken.Location);
        }
        target ??= new TupleTypeSymbol(values.Select(v => v.Type).ToArray(), new string?[values.Length]);
        return new BoundRecordLiteralExpression(syntax.OpenParenToken.Location, target,
            values.Select((v, i) => new BoundRecordLiteralFieldInitializer(target.Fields[i], v)).ToArray());
    }

    private void ReportTuple(string message, TextLocation location)
        => _diagnostics.Report(DiagnosticCodes.TypeMismatch, message, location);

    private int _tupleTemporaryId;

    private BoundStatement BindTupleUnpacking(TupleUnpackingStatementSyntax syntax)
    {
        var location = syntax.EqualsToken.Location;
        var source = BindExpression(syntax.Source);
        var statements = new List<BoundStatement>();
        if (source.Type is not TupleTypeSymbol type || type.Fields.Count != syntax.Targets.Elements.Count)
        {
            ReportTuple("Распаковка требует кортежа с тем же числом компонентов, что и целей.", location);
            return new BoundVariableDeclarationsStatement(location, []);
        }
        VariableSymbol Temporary(TypeSymbol valueType) => new("$tuple" + ++_tupleTemporaryId, valueType,
            isReadOnly: false, isGlobal: _currentFunction is null);
        var sourceVariable = Temporary(type);
        statements.Add(new BoundVariableDeclarationStatement(location, sourceVariable, source, []));
        var writes = new List<BoundStatement>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < type.Fields.Count; i++)
        {
            if (syntax.Targets.Elements[i] is not NameExpressionSyntax name || !used.Add(name.IdentifierToken.Text))
            {
                ReportTuple("Цели распаковки должны быть разными именами переменных.", location);
                continue;
            }
            var fieldValue = new BoundFieldAccessExpression(location,
                new BoundVariableExpression(location, sourceVariable), type.Fields[i]);
            if (syntax.DimKeyword is not null)
            {
                var variable = new VariableSymbol(name.IdentifierToken.Text, fieldValue.Type,
                    isReadOnly: false, isGlobal: _currentFunction is null);
                TryDeclareScopedVariable(variable, name.IdentifierToken.Location);
                writes.Add(new BoundVariableDeclarationStatement(location, variable, fieldValue, [], name.IdentifierToken.Location));
            }
            else
            {
                // В списке целей имя обозначает всю переменную, включая массив.
                var variable = LookupVariable(name.IdentifierToken.Text, name.IdentifierToken.Location);
                _editorReferences?.Add(new(variable, name.IdentifierToken.Location, false));
                var target = new BoundVariableExpression(name.IdentifierToken.Location, variable);
                if (variable.IsReadOnly) ReportAssignToConst(variable.Name, name.IdentifierToken.Location);
                var converted = ApplyExpectedType(name, fieldValue, target.Type);
                var prepared = Temporary(target.Type);
                statements.Add(new BoundVariableDeclarationStatement(location, prepared, converted, []));
                writes.Add(new BoundAssignmentStatement(location, target, new BoundVariableExpression(location, prepared)));
            }
        }
        statements.AddRange(writes);
        // Общая последовательность в уже поддерживаемом дереве управления:
        // ни новая область видимости, ни повторное вычисление источника не нужны.
        return new BoundIfStatement(location, new BoundLiteralExpression(location, true, TypeSymbol.Boolean), statements, []);
    }
}
