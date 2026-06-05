using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeScope
{
    private readonly Dictionary<VariableSymbol, IRuntimeStoredValue> _values = [];

    public RuntimeScope(RuntimeScope? parent, FunctionSymbol? owningRoutine = null)
    {
        Parent = parent;
        OwningRoutine = owningRoutine;
    }

    public RuntimeScope? Parent { get; }

    public FunctionSymbol? OwningRoutine { get; }

    public void Define(VariableSymbol variable, IRuntimeStoredValue value)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(value);
        _values[variable] = value;
    }

    public bool TryLookup(VariableSymbol variable, out IRuntimeStoredValue? value)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (_values.TryGetValue(variable, out value))
        {
            return true;
        }

        if (Parent is not null)
        {
            return Parent.TryLookup(variable, out value);
        }

        value = null;
        return false;
    }

    public bool TryLookupStorage(VariableSymbol variable, out RuntimeScope? ownerScope, out IRuntimeStoredValue? value)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (_values.TryGetValue(variable, out value))
        {
            ownerScope = this;
            return true;
        }

        if (Parent is not null)
        {
            return Parent.TryLookupStorage(variable, out ownerScope, out value);
        }

        ownerScope = null;
        value = null;
        return false;
    }

    public bool TryAssign(VariableSymbol variable, IRuntimeStoredValue value)
    {
        ArgumentNullException.ThrowIfNull(variable);
        ArgumentNullException.ThrowIfNull(value);

        if (_values.ContainsKey(variable))
        {
            _values[variable] = value;
            return true;
        }

        if (Parent is not null)
        {
            return Parent.TryAssign(variable, value);
        }

        return false;
    }
}
