using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal sealed class BytecodeCallFrame
{
    private IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot>? _visibleVariables;
    private readonly RuntimeValue[] _locals;
    private readonly int?[] _localFixedStringLengths;
    private readonly BytecodeVariableSlot?[] _sharedLocals;

    public BytecodeCallFrame(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] arguments,
        RuntimeValue[] locals,
        int?[] localFixedStringLengths,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        int evaluationStackCapacity = 16)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(locals);
        ArgumentNullException.ThrowIfNull(localFixedStringLengths);
        ArgumentNullException.ThrowIfNull(captures);

        Routine = routine;
        Arguments = arguments;
        _locals = locals;
        _localFixedStringLengths = localFixedStringLengths;
        _sharedLocals = new BytecodeVariableSlot?[locals.Length];
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        EvaluationStack = new BytecodeEvaluationStack(evaluationStackCapacity);
    }

    public BytecodeRoutine Routine { get; }

    public BytecodeVariableSlot[] Arguments { get; }

    public BytecodeVariableSlot[] Captures { get; }

    public TextLocation CallSiteLocation { get; }

    public BytecodeEvaluationStack EvaluationStack { get; }

    public int LocalCount => _locals.Length;

    public IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot> VisibleVariables =>
        _visibleVariables ??= CreateVisibleVariables(this);

    public int InstructionPointer { get; set; }

    public bool TryResolveVisibleVariable(VariableSymbol variable, out BytecodeVariableSlot slot)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return ((Dictionary<VariableSymbol, BytecodeVariableSlot>)VisibleVariables).TryGetValue(variable, out slot!);
    }

    public RuntimeValue LoadLocalValue(int slot)
    {
        return _sharedLocals[slot]?.Value ?? _locals[slot];
    }

    public void StoreLocalValue(int slot, RuntimeValue value)
    {
        var sharedSlot = _sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.Store(value);
            return;
        }

        _locals[slot] = BytecodeVariableSlot.NormalizeForStorage(value, _localFixedStringLengths[slot]);
    }

    public BytecodeVariableSlot GetOrCreateLocalSlot(int slot)
    {
        var sharedSlot = _sharedLocals[slot];
        if (sharedSlot is not null)
        {
            return sharedSlot;
        }

        sharedSlot = new BytecodeVariableSlot(_locals[slot], _localFixedStringLengths[slot]);
        _sharedLocals[slot] = sharedSlot;
        return sharedSlot;
    }

    public int? GetLocalFixedStringLength(int slot)
    {
        return _localFixedStringLengths[slot];
    }

    private static Dictionary<VariableSymbol, BytecodeVariableSlot> CreateVisibleVariables(BytecodeCallFrame frame)
    {
        var visibleVariables = new Dictionary<VariableSymbol, BytecodeVariableSlot>();
        var routine = frame.Routine;
        var arguments = frame.Arguments;
        var captures = frame.Captures;

        for (var index = 0; index < routine.ParameterVariables.Count && index < arguments.Length; index++)
        {
            visibleVariables[routine.ParameterVariables[index]] = arguments[index];
        }

        for (var index = 0; index < routine.LocalVariables.Count && index < frame.LocalCount; index++)
        {
            visibleVariables[routine.LocalVariables[index]] = frame.GetOrCreateLocalSlot(index);
        }

        for (var index = 0; index < routine.CapturedVariables.Count && index < captures.Length; index++)
        {
            visibleVariables[routine.CapturedVariables[index]] = captures[index];
        }

        return visibleVariables;
    }
}
