using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed class BytecodeGlobal
{
    public BytecodeGlobal(
        int slot,
        string name,
        TypeSymbol type,
        bool isReadOnly,
        GlobalInitializationStrategy initializationStrategy,
        int? fixedStringLength = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(type);

        Slot = slot;
        Name = name;
        Type = type;
        IsReadOnly = isReadOnly;
        InitializationStrategy = initializationStrategy;
        FixedStringLength = fixedStringLength;
    }

    public int Slot { get; }

    public string Name { get; }

    public TypeSymbol Type { get; }

    public bool IsReadOnly { get; }

    public GlobalInitializationStrategy InitializationStrategy { get; }

    public int? FixedStringLength { get; }
}
