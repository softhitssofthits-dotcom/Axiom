using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Bytecode;

public sealed class BytecodeRoutine
{
    private readonly BytecodeInstruction[] _instructionArray;

    public BytecodeRoutine(
        int id,
        string name,
        TextLocation location,
        TypeSymbol resultType,
        int parameterCount,
        int localSlotCount,
        IReadOnlyList<BytecodeInstruction> instructions,
        int maxEvaluationStackDepth = 1,
        bool isEntryPoint = false,
        FunctionSymbol? symbol = null,
        IReadOnlyList<VariableSymbol>? parameterVariables = null,
        IReadOnlyList<VariableSymbol>? localVariables = null,
        IReadOnlyList<VariableSymbol>? capturedVariables = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(instructions);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxEvaluationStackDepth, 1);

        Id = id;
        Name = name;
        Location = location;
        ResultType = resultType;
        ParameterCount = parameterCount;
        LocalSlotCount = localSlotCount;
        _instructionArray = instructions as BytecodeInstruction[] ?? instructions.ToArray();
        Instructions = _instructionArray;
        // Точная максимальная высота стека вычислений (аналог co_stacksize
        // CPython) считается один раз при компиляции или загрузке снимка через
        // BytecodeStackDepth и передаётся готовым числом. Значение по умолчанию
        // предназначено только для утилит и тестов: недооценка приводит к
        // перевыделению массива стека через Grow, но не к ошибкам исполнения.
        MaxEvaluationStackDepth = maxEvaluationStackDepth;
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

    public BytecodeInstruction[] InstructionArray => _instructionArray;

    public int InstructionCount => _instructionArray.Length;

    public int MaxEvaluationStackDepth { get; }

    public bool IsEntryPoint { get; }

    public FunctionSymbol? Symbol { get; }

    public IReadOnlyList<VariableSymbol> ParameterVariables { get; }

    public IReadOnlyList<VariableSymbol> LocalVariables { get; }

    public IReadOnlyList<VariableSymbol> CapturedVariables { get; }
}
