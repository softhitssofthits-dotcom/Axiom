namespace Axiom.Semantics;

public sealed class BoundScope
{
    private readonly Dictionary<string, VariableSymbol> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FunctionSymbol> _functions = new(StringComparer.OrdinalIgnoreCase);

    public BoundScope(
        BoundScope? enclosingScope,
        BoundScope? variableParent = null,
        BoundScope? functionParent = null)
    {
        EnclosingScope = enclosingScope;
        VariableParent = variableParent ?? enclosingScope;
        FunctionParent = functionParent ?? enclosingScope;
    }

    public BoundScope? EnclosingScope { get; }

    public BoundScope? VariableParent { get; }

    public BoundScope? FunctionParent { get; }

    public bool TryDeclareVariable(VariableSymbol variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return _variables.TryAdd(variable.Name, variable);
    }

    public bool TryDeclareFunction(FunctionSymbol function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return _functions.TryAdd(function.Name, function);
    }

    public bool TryLookupVariable(string name, out VariableSymbol? variable)
    {
        if (_variables.TryGetValue(name, out variable))
        {
            return true;
        }

        if (VariableParent is not null)
        {
            return VariableParent.TryLookupVariable(name, out variable);
        }

        variable = null;
        return false;
    }

    public bool TryLookupVariableInCurrentScope(string name, out VariableSymbol? variable)
    {
        return _variables.TryGetValue(name, out variable);
    }

    public bool TryLookupVariableInParents(string name, out VariableSymbol? variable)
    {
        if (VariableParent is null)
        {
            variable = null;
            return false;
        }

        return VariableParent.TryLookupVariable(name, out variable);
    }

    public bool TryLookupFunctionInCurrentScope(string name, out FunctionSymbol? function)
    {
        return _functions.TryGetValue(name, out function);
    }

    public bool TryLookupFunctionInParents(string name, out FunctionSymbol? function)
    {
        if (FunctionParent is null)
        {
            function = null;
            return false;
        }

        return FunctionParent.TryLookupFunction(name, out function);
    }

    public bool TryLookupFunction(string name, out FunctionSymbol? function)
    {
        if (_functions.TryGetValue(name, out function))
        {
            return true;
        }

        if (FunctionParent is not null)
        {
            return FunctionParent.TryLookupFunction(name, out function);
        }

        function = null;
        return false;
    }
}
