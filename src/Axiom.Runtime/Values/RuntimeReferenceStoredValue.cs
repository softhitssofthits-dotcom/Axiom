namespace Axiom.Runtime;

internal sealed class RuntimeReferenceStoredValue : IRuntimeStoredValue
{
    private readonly Func<RuntimeValue> _getter;
    private readonly Action<RuntimeValue> _setter;

    public RuntimeReferenceStoredValue(Func<RuntimeValue> getter, Action<RuntimeValue> setter)
    {
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        _getter = getter;
        _setter = setter;
    }

    public RuntimeValue Value
    {
        get => _getter();
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _setter(value);
        }
    }
}
