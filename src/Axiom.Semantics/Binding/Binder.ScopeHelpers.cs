using Axiom.Core;
using Axiom.Syntax;

namespace Axiom.Semantics;

public sealed partial class Binder
{
    private VariableSymbol LookupVariable(string name, TextLocation location)
    {
        if (_scope.TryLookupVariable(name, out var variable) && variable is not null)
        {
            return variable;
        }

        if (TryLookupPredefinedConstantVariable(name, out var predefinedConstant))
        {
            return predefinedConstant;
        }

        ReportUnknownName(name, location);
        return new VariableSymbol(name, TypeSymbol.Error, isReadOnly: false, isGlobal: _currentFunction is null);
    }

    private void EnterScope(string? declarationContext = null)
    {
        _declarationContexts.Push(declarationContext);
        _scope = new BoundScope(_scope);
    }

    private void LeaveScope()
    {
        _declarationContexts.Pop();
        _scope = _scope.EnclosingScope ?? throw new InvalidOperationException("Cannot leave the root binder scope.");
    }

    private IReadOnlyList<BoundStatement> BindStatementsInChildScope(
        IReadOnlyList<StatementSyntax> statements,
        string? declarationContext = null)
    {
        EnterScope(declarationContext);

        try
        {
            return BindStatements(statements);
        }
        finally
        {
            LeaveScope();
        }
    }

    private bool TryDeclareScopedVariable(
        VariableSymbol variable,
        TextLocation location,
        string? declarationContextOverride = null)
    {
        if (TryLookupPredefinedConstantVariable(variable.Name, out _))
        {
            ReportDuplicateDeclaration(variable.Name, location);
            return false;
        }

        if (_scope.TryLookupVariableInCurrentScope(variable.Name, out _))
        {
            ReportDuplicateDeclaration(variable.Name, location);
            return false;
        }

        if (_scope.TryLookupVariableInParents(variable.Name, out _))
        {
            ReportShadowedDeclaration(
                variable.Name,
                location,
                declarationContextOverride ?? GetCurrentDeclarationContext());
            return _scope.TryDeclareVariable(variable);
        }

        return _scope.TryDeclareVariable(variable);
    }

    private static TypeSymbol InferForControlVariableType(params BoundExpression?[] expressions)
    {
        var sawDouble = false;
        var sawLong = false;
        var sawNumeric = false;

        foreach (var expression in expressions)
        {
            if (expression is null || expression.Type == TypeSymbol.Error)
            {
                continue;
            }

            if (expression.Type == TypeSymbol.Decimal)
            {
                return TypeSymbol.Error;
            }

            if (expression.Type == TypeSymbol.Double)
            {
                sawDouble = true;
                sawNumeric = true;
                continue;
            }

            if (expression.Type == TypeSymbol.Long)
            {
                sawLong = true;
                sawNumeric = true;
                continue;
            }

            if (expression.Type == TypeSymbol.Integer)
            {
                sawNumeric = true;
                continue;
            }

            return TypeSymbol.Error;
        }

        if (!sawNumeric)
        {
            return TypeSymbol.Integer;
        }

        if (sawDouble)
        {
            return TypeSymbol.Double;
        }

        return sawLong ? TypeSymbol.Long : TypeSymbol.Integer;
    }

    private string? GetCurrentDeclarationContext()
    {
        foreach (var context in _declarationContexts)
        {
            if (!string.IsNullOrWhiteSpace(context))
            {
                return context;
            }
        }

        return null;
    }
}
