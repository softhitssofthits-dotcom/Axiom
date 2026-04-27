using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal sealed class BytecodeCallFrame
{
    public BytecodeCallFrame(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] arguments,
        BytecodeVariableSlot[] locals,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(locals);
        ArgumentNullException.ThrowIfNull(captures);

        Routine = routine;
        Arguments = arguments;
        Locals = locals;
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        EvaluationStack = new Stack<RuntimeValue>();
        VisibleVariables = CreateVisibleVariables(routine, arguments, locals, captures);
    }

    public BytecodeRoutine Routine { get; }

    public BytecodeVariableSlot[] Arguments { get; }

    public BytecodeVariableSlot[] Locals { get; }

    public BytecodeVariableSlot[] Captures { get; }

    public TextLocation CallSiteLocation { get; }

    public Stack<RuntimeValue> EvaluationStack { get; }

    public IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot> VisibleVariables { get; }

    public int InstructionPointer { get; set; }

    public bool TryResolveVisibleVariable(VariableSymbol variable, out BytecodeVariableSlot slot)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return ((Dictionary<VariableSymbol, BytecodeVariableSlot>)VisibleVariables).TryGetValue(variable, out slot!);
    }

    private static Dictionary<VariableSymbol, BytecodeVariableSlot> CreateVisibleVariables(
        BytecodeRoutine routine,
        IReadOnlyList<BytecodeVariableSlot> arguments,
        IReadOnlyList<BytecodeVariableSlot> locals,
        IReadOnlyList<BytecodeVariableSlot> captures)
    {
        var visibleVariables = new Dictionary<VariableSymbol, BytecodeVariableSlot>();

        for (var index = 0; index < routine.ParameterVariables.Count && index < arguments.Count; index++)
        {
            visibleVariables[routine.ParameterVariables[index]] = arguments[index];
        }

        for (var index = 0; index < routine.LocalVariables.Count && index < locals.Count; index++)
        {
            visibleVariables[routine.LocalVariables[index]] = locals[index];
        }

        for (var index = 0; index < routine.CapturedVariables.Count && index < captures.Count; index++)
        {
            visibleVariables[routine.CapturedVariables[index]] = captures[index];
        }

        return visibleVariables;
    }
}
