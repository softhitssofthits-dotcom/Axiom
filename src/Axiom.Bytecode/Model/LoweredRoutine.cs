using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed class LoweredRoutine
{
    public LoweredRoutine(
        int id,
        string name,
        TextLocation location,
        TypeSymbol resultType,
        IReadOnlyList<LoweredInstruction> instructions,
        FunctionSymbol? symbol = null,
        bool isEntryPoint = false,
        IReadOnlyList<VariableSymbol>? localVariables = null)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Routine id must be non-negative.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(instructions);

        Id = id;
        Name = name;
        Location = location;
        ResultType = resultType;
        Instructions = instructions;
        Symbol = symbol;
        IsEntryPoint = isEntryPoint;
        LocalVariables = localVariables ?? Array.Empty<VariableSymbol>();
    }

    public int Id { get; }

    public string Name { get; }

    public TextLocation Location { get; }

    public TypeSymbol ResultType { get; }

    public IReadOnlyList<LoweredInstruction> Instructions { get; }

    public FunctionSymbol? Symbol { get; }

    public bool IsEntryPoint { get; }

    public IReadOnlyList<VariableSymbol> LocalVariables { get; }
}
