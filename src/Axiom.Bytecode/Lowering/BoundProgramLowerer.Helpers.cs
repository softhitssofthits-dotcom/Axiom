using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BoundProgramLowerer
{
    private string CreateLabel()
    {
        _nextLabelId++;
        return FormattableString.Invariant($"L{_nextLabelId:0000}");
    }

    private VariableSymbol CreateForTemporary(BoundForStatement forStatement, string role, string checkLabel)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__for_{forStatement.Variable.Name}_{role}_{checkLabel}"),
            forStatement.Variable.Type,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateSelectCaseTemporary(BoundSelectCaseStatement selectCaseStatement, string endLabel)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__select_case_test_{endLabel}"),
            selectCaseStatement.TestExpression.Type,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateRecordLiteralTemporary(BoundRecordLiteralExpression recordLiteral)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__record_literal_{_currentRoutineLocals.Count}_{recordLiteral.RecordType.Name}"),
            recordLiteral.RecordType,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateTypeConstructorTemporary(BoundTypeConstructorExpression typeConstructor)
    {
        var variable = new VariableSymbol(
            FormattableString.Invariant($"__type_constructor_{_currentRoutineLocals.Count}_{typeConstructor.RecordType.Name}"),
            typeConstructor.RecordType,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private VariableSymbol CreateInternalTemporary(TypeSymbol type, string stem)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(stem);

        var variable = new VariableSymbol(
            FormattableString.Invariant($"{stem}_{_currentRoutineLocals.Count}"),
            type,
            isReadOnly: false,
            isGlobal: false);

        _currentRoutineLocals.Add(variable);
        return variable;
    }

    private static object CreateZeroLiteral(TypeSymbol type)
    {
        if (type == TypeSymbol.Double)
        {
            return 0d;
        }

        if (type == TypeSymbol.Long)
        {
            return 0L;
        }

        return 0;
    }

    private static bool TryCreateDefaultLiteral(TypeSymbol type, out object value)
    {
        if (type == TypeSymbol.Integer)
        {
            value = 0;
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            value = 0L;
            return true;
        }

        if (type == TypeSymbol.Double)
        {
            value = 0d;
            return true;
        }

        if (type == TypeSymbol.Decimal)
        {
            value = 0m;
            return true;
        }

        if (type == TypeSymbol.String)
        {
            value = string.Empty;
            return true;
        }

        if (type == TypeSymbol.Boolean)
        {
            value = false;
            return true;
        }

        value = null!;
        return false;
    }

    private static bool CanUseImplicitDefaultSlotValue(TypeSymbol type)
    {
        return type is UserDefinedTypeSymbol or FunctionTypeSymbol
            || type == TypeSymbol.File
            || type == TypeSymbol.Image
            || type == TypeSymbol.Sprite
            || type == TypeSymbol.Tileset
            || type == TypeSymbol.Tilemap;
    }

    private void ReportUnsupported(BoundNode node, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"Понижение пока не поддерживает {category}: {node.Kind}.",
            node.Location));
    }

    private void ReportUnsupported(Axiom.Core.TextLocation location, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"Понижение пока не поддерживает {category}.",
            location));
    }

    private void ReportUnsupportedSelector(BoundSelectCaseSelector selector, string category)
    {
        _diagnostics.Add(new BytecodeDiagnostic(
            "BC3001",
            $"Понижение пока не поддерживает {category}: {selector.GetType().Name}.",
            selector.Location));
    }

    private static HashSet<VariableSymbol> CollectDeclaredVariables(IReadOnlyList<BoundStatement> statements)
    {
        var variables = new HashSet<VariableSymbol>();
        CollectDeclaredVariables(statements, variables);
        return variables;
    }

    private static void CollectDeclaredVariables(IReadOnlyList<BoundStatement> statements, HashSet<VariableSymbol> variables)
    {
        foreach (var statement in statements)
        {
            CollectDeclaredVariables(statement, variables);
        }
    }

    private static void CollectDeclaredVariables(BoundStatement statement, HashSet<VariableSymbol> variables)
    {
        switch (statement)
        {
            case BoundVariableDeclarationsStatement declarations:
                foreach (var declaration in declarations.Declarations)
                {
                    variables.Add(declaration.Variable);
                }
                break;

            case BoundVariableDeclarationStatement declaration:
                variables.Add(declaration.Variable);
                break;

            case BoundIfStatement ifStatement:
                CollectDeclaredVariables(ifStatement.Statements, variables);
                CollectDeclaredVariables(ifStatement.ElseStatements, variables);
                break;

            case BoundTryStatement tryStatement:
                if (tryStatement.CatchVariable is not null)
                {
                    variables.Add(tryStatement.CatchVariable);
                }

                CollectDeclaredVariables(tryStatement.Statements, variables);
                CollectDeclaredVariables(tryStatement.CatchStatements, variables);
                CollectDeclaredVariables(tryStatement.FinallyStatements, variables);
                break;

            case BoundWhileStatement whileStatement:
                CollectDeclaredVariables(whileStatement.Statements, variables);
                break;

            case BoundDoLoopStatement doLoopStatement:
                CollectDeclaredVariables(doLoopStatement.Statements, variables);
                break;

            case BoundForStatement forStatement:
                variables.Add(forStatement.Variable);
                CollectDeclaredVariables(forStatement.Statements, variables);
                break;

            case BoundSelectCaseStatement selectCaseStatement:
                foreach (var clause in selectCaseStatement.Clauses)
                {
                    CollectDeclaredVariables(clause.Statements, variables);
                }

                CollectDeclaredVariables(selectCaseStatement.ElseStatements, variables);
                break;
        }
    }

}
