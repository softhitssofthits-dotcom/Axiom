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
        _instructionArray = instructions as BytecodeInstruction[] ?? instructions.ToArray();
        Instructions = _instructionArray;
        MaxEvaluationStackDepth = EstimateMaxEvaluationStackDepth(_instructionArray);
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

    private static int EstimateMaxEvaluationStackDepth(IReadOnlyList<BytecodeInstruction> instructions)
    {
        var depth = 0;
        var maxDepth = 0;

        foreach (var instruction in instructions)
        {
            depth += GetStackDelta(instruction);
            if (depth < 0)
            {
                depth = 0;
            }

            if (depth > maxDepth)
            {
                maxDepth = depth;
            }
        }

        return Math.Max(1, maxDepth);
    }

    private static int GetStackDelta(BytecodeInstruction instruction)
    {
        return instruction.Opcode switch
        {
            Opcode.PushConst => 1,
            Opcode.PushIntegerZero => 1,
            Opcode.PushIntegerOne => 1,
            Opcode.LoadFunction => 1,
            Opcode.LoadBuiltinFunction => 1,
            Opcode.LoadGlobal => 1,
            Opcode.StoreGlobal => -1,
            Opcode.LoadLocal => 1,
            Opcode.StoreLocal => -1,
            Opcode.LoadArgument => 1,
            Opcode.StoreArgument => -1,
            Opcode.LoadCapture => 1,
            Opcode.StoreCapture => -1,
            Opcode.Pop => -1,
            Opcode.Negate => 0,
            Opcode.Concat => -1,
            Opcode.Add => -1,
            Opcode.AddLongInteger => -1,
            Opcode.Subtract => -1,
            Opcode.Multiply => -1,
            Opcode.Power => -1,
            Opcode.Divide => -1,
            Opcode.IntegralDivide => -1,
            Opcode.Modulo => -1,
            Opcode.Not => 0,
            Opcode.And => -1,
            Opcode.Or => -1,
            Opcode.Xor => -1,
            Opcode.Eqv => -1,
            Opcode.Imp => -1,
            Opcode.CompareEqual => -1,
            Opcode.CompareNotEqual => -1,
            Opcode.CompareLess => -1,
            Opcode.CompareLessOrEqual => -1,
            Opcode.CompareGreater => -1,
            Opcode.CompareGreaterOrEqual => -1,
            Opcode.ShiftLeft => -1,
            Opcode.ShiftRight => -1,
            Opcode.Jump => 0,
            Opcode.JumpIfEqual => -2,
            Opcode.JumpIfNotEqual => -2,
            Opcode.JumpIfLess => -2,
            Opcode.JumpIfLessOrEqual => -2,
            Opcode.JumpIfGreater => -2,
            Opcode.JumpIfGreaterOrEqual => -2,
            Opcode.JumpIfForDoneLocal => 0,
            Opcode.JumpIfTrue => -1,
            Opcode.JumpIfFalse => -1,
            Opcode.Call => 0,
            Opcode.CallByRef => 0,
            Opcode.CallValue => 0,
            Opcode.Return => 0,
            Opcode.RedimArray => -1,
            Opcode.EraseArray => -1,
            Opcode.LoadElement => 0,
            Opcode.StoreElement => -2,
            Opcode.CreateRecord => 1,
            Opcode.LoadField => 0,
            Opcode.StoreField => -2,
            Opcode.CallBuiltin => 0,
            Opcode.ValidateTypeInvariant => -1,
            Opcode.ValidateForStep => -1,
            Opcode.AdvanceForLoopLocal => 0,
            Opcode.PrintValue => -1,
            Opcode.PrintUsing => -1 - instruction.GetOperand(0),
            Opcode.PrintSeparator => 0,
            Opcode.PrintLine => 0,
            Opcode.Input => 0,
            Opcode.Sleep => -instruction.GetOperand(0),
            Opcode.OpenFile => instruction.OperandCount == 3 ? -2 : -1,
            Opcode.CloseFile => 0,
            Opcode.Convert => 0,
            Opcode.Viewport => -instruction.GetOperand(0),
            _ => 0
        };
    }
}
