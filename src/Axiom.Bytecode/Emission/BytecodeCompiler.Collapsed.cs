using Axiom.Core;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Bytecode;

public sealed partial class BytecodeCompiler
{
    private static bool EmitsBytecodeInstruction(LoweredInstruction instruction)
    {
        return instruction.Kind switch
        {
            LoweredInstructionKind.Label => false,
            LoweredInstructionKind.UnaryOperation when instruction.OperatorKind == SyntaxKind.PlusToken => false,
            _ => true
        };
    }

    private int GetCollapsedInstructionWidth(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots)
    {
        return TryGetCollapsedInstructionWidth(instructions, index, parameterSlots, localSlots, captureSlots, out var width)
            ? width
            : 0;
    }

    private bool TryGetCollapsedInstructionWidth(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out int width)
    {
        width = 0;

        if (index + 1 >= instructions.Count)
        {
            return false;
        }

        var current = instructions[index];
        var next = instructions[index + 1];
        if (TryGetCollapsedComparisonJumpOpcode(current, next, out _))
        {
            width = 2;
            return true;
        }

        if (current.Kind == LoweredInstructionKind.Convert
            && current.ValueType == TypeSymbol.Long
            && next.Kind == LoweredInstructionKind.BinaryOperation
            && next.OperatorKind == SyntaxKind.PlusToken)
        {
            width = 2;
            return true;
        }

        return false;
    }

    private bool TryEmitCollapsedInstruction(
        LoweredRoutine routine,
        int loweredIndex,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        IReadOnlyDictionary<string, int> labelTargets,
        out BytecodeInstruction? instruction,
        out TextLocation sourceLocation)
    {
        instruction = null;
        sourceLocation = routine.Instructions[loweredIndex].Location;
        if (loweredIndex + 1 >= routine.Instructions.Count)
        {
            return false;
        }

        var current = routine.Instructions[loweredIndex];
        var next = routine.Instructions[loweredIndex + 1];

        if (TryGetCollapsedComparisonJumpOpcode(current, next, out var comparisonJumpOpcode))
        {
            if (next.Label is null || !labelTargets.TryGetValue(next.Label, out var targetInstruction))
            {
                Report(
                    "BC5012",
                    $"Эмиттер байткода не нашёл метку '{next.Label ?? "<null>"}' в процедуре '{routine.Name}'.",
                    next.Location);
                return true;
            }

            instruction = new BytecodeInstruction(comparisonJumpOpcode, [targetInstruction]);
            sourceLocation = next.Location;
            return true;
        }

        if (!TryGetCollapsedInstructionWidth(routine.Instructions, loweredIndex, parameterSlots, localSlots, captureSlots, out _))
        {
            return false;
        }

        instruction = new BytecodeInstruction(Opcode.AddLongInteger);
        sourceLocation = next.Location;
        return true;
    }

    private static bool TryResolveVariableSlotForCollapse(
        LoweredInstruction instruction,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out Opcode opcode,
        out int slot)
    {
        opcode = default;
        slot = default;

        if (instruction.Variable is null)
        {
            return false;
        }

        if (instruction.Variable is ParameterSymbol parameter)
        {
            if (parameterSlots.TryGetValue(parameter, out slot))
            {
                // Слитая раскладка кадра: параметр исполняется общими
                // LoadLocal/StoreLocal в первых слотах локального пространства.
                opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreLocal : Opcode.LoadLocal;
                return true;
            }

            if (captureSlots.TryGetValue(parameter, out slot))
            {
                opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreCapture : Opcode.LoadCapture;
                return true;
            }

            return false;
        }

        if (instruction.Variable.IsGlobal)
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreGlobal : Opcode.LoadGlobal;
            return true;
        }

        if (localSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreLocal : Opcode.LoadLocal;
            return true;
        }

        if (captureSlots.TryGetValue(instruction.Variable, out slot))
        {
            opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreCapture : Opcode.LoadCapture;
            return true;
        }

        return false;
    }

    private static bool TryGetCollapsedComparisonJumpOpcode(
        LoweredInstruction compareInstruction,
        LoweredInstruction jumpInstruction,
        out Opcode opcode)
    {
        opcode = default;
        if (compareInstruction.Kind != LoweredInstructionKind.BinaryOperation)
        {
            return false;
        }

        if (jumpInstruction.Kind is not (LoweredInstructionKind.JumpIfTrue or LoweredInstructionKind.JumpIfFalse))
        {
            return false;
        }

        var jumpIfTrue = jumpInstruction.Kind == LoweredInstructionKind.JumpIfTrue;
        opcode = compareInstruction.OperatorKind switch
        {
            SyntaxKind.EqualsToken => jumpIfTrue ? Opcode.JumpIfEqual : Opcode.JumpIfNotEqual,
            SyntaxKind.NotEqualsToken => jumpIfTrue ? Opcode.JumpIfNotEqual : Opcode.JumpIfEqual,
            SyntaxKind.LessToken => jumpIfTrue ? Opcode.JumpIfLess : Opcode.JumpIfGreaterOrEqual,
            SyntaxKind.LessOrEqualsToken => jumpIfTrue ? Opcode.JumpIfLessOrEqual : Opcode.JumpIfGreater,
            SyntaxKind.GreaterToken => jumpIfTrue ? Opcode.JumpIfGreater : Opcode.JumpIfLessOrEqual,
            SyntaxKind.GreaterOrEqualsToken => jumpIfTrue ? Opcode.JumpIfGreaterOrEqual : Opcode.JumpIfLess,
            _ => default
        };

        return opcode is Opcode.JumpIfEqual
            or Opcode.JumpIfNotEqual
            or Opcode.JumpIfLess
            or Opcode.JumpIfLessOrEqual
            or Opcode.JumpIfGreater
            or Opcode.JumpIfGreaterOrEqual;
    }

}
