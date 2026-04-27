using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed class BytecodeRoutine
{
    public BytecodeRoutine(
        int id,
        string name,
        TextLocation location,
        TypeSymbol resultType,
        int parameterCount,
        int localSlotCount,
        IReadOnlyList<BytecodeInstruction> instructions,
        bool isEntryPoint = false,
        FunctionSymbol? symbol = null,
        IReadOnlyList<VariableSymbol>? parameterVariables = null,
        IReadOnlyList<VariableSymbol>? localVariables = null,
        IReadOnlyList<VariableSymbol>? capturedVariables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(instructions);

        Id = id;
        Name = name;
        Location = location;
        ResultType = resultType;
        ParameterCount = parameterCount;
        LocalSlotCount = localSlotCount;
        Instructions = instructions;
        IsEntryPoint = isEntryPoint;
        Symbol = symbol;
        ParameterVariables = parameterVariables ?? Array.Empty<VariableSymbol>();
        LocalVariables = localVariables ?? Array.Empty<VariableSymbol>();
        CapturedVariables = capturedVariables ?? Array.Empty<VariableSymbol>();
    }

    public int Id { get; }

    public string Name { get; }

    public TextLocation Location { get; }

    public TypeSymbol ResultType { get; }

    public int ParameterCount { get; }

    public int LocalSlotCount { get; }

    public IReadOnlyList<BytecodeInstruction> Instructions { get; }

    public bool IsEntryPoint { get; }

    public FunctionSymbol? Symbol { get; }

    public IReadOnlyList<VariableSymbol> ParameterVariables { get; }

    public IReadOnlyList<VariableSymbol> LocalVariables { get; }

    public IReadOnlyList<VariableSymbol> CapturedVariables { get; }
}
