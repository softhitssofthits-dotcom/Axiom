using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed class BuiltinBinding
{
    public BuiltinBinding(int id, BuiltinFunctionId canonicalId, string name, int parameterCount, bool returnsValue)
        : this(id, canonicalId, name, parameterCount, returnsValue, function: null)
    {
    }

    public BuiltinBinding(
        int id,
        BuiltinFunctionId canonicalId,
        string name,
        int parameterCount,
        bool returnsValue,
        FunctionSymbol? function)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        CanonicalId = canonicalId;
        Name = name;
        ParameterCount = parameterCount;
        ReturnsValue = returnsValue;
        Function = function;
    }

    public int Id { get; }

    public BuiltinFunctionId CanonicalId { get; }

    public string Name { get; }

    public int ParameterCount { get; }

    public bool ReturnsValue { get; }

    public FunctionSymbol? Function { get; }
}
