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
        if (TryMatchCollapsedLocalForLoopCheck(instructions, index, parameterSlots, localSlots, captureSlots, out _))
        {
            width = 15;
            return true;
        }

        if (TryMatchCollapsedAdvanceForLoopLocal(instructions, index, parameterSlots, localSlots, captureSlots, out _, out _))
        {
            width = 4;
            return true;
        }

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

        if (TryMatchCollapsedLocalForLoopCheck(
                routine.Instructions,
                loweredIndex,
                parameterSlots,
                localSlots,
                captureSlots,
                out var collapsedForLoopCheck))
        {
            if (!labelTargets.TryGetValue(collapsedForLoopCheck.EndLabel, out var endTargetInstruction))
            {
                Report(
                    "BC5012",
                    $"Эмиттер байткода не нашёл метку '{collapsedForLoopCheck.EndLabel}' в процедуре '{routine.Name}'.",
                    collapsedForLoopCheck.Location);
                return true;
            }

            instruction = new BytecodeInstruction(
                Opcode.JumpIfForDoneLocal,
                [
                    collapsedForLoopCheck.CurrentSlot,
                    collapsedForLoopCheck.EndSlot,
                    collapsedForLoopCheck.StepSlot,
                    endTargetInstruction
                ]);
            sourceLocation = collapsedForLoopCheck.Location;
            return true;
        }

        if (TryMatchCollapsedAdvanceForLoopLocal(
                routine.Instructions,
                loweredIndex,
                parameterSlots,
                localSlots,
                captureSlots,
                out var currentSlot,
                out var stepSlot))
        {
            instruction = new BytecodeInstruction(Opcode.AdvanceForLoopLocal, [currentSlot, stepSlot]);
            sourceLocation = routine.Instructions[loweredIndex + 3].Location;
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

    private bool TryMatchCollapsedLocalForLoopCheck(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out CollapsedLocalForLoopCheck collapsed)
    {
        collapsed = default;
        if (index + 14 >= instructions.Count)
        {
            return false;
        }

        var stepLoadForSign = instructions[index];
        var zeroLiteral = instructions[index + 1];
        var compareStepToZero = instructions[index + 2];
        var jumpToNegative = instructions[index + 3];
        var currentLoadPositive = instructions[index + 4];
        var endLoadPositive = instructions[index + 5];
        var comparePositive = instructions[index + 6];
        var jumpPositiveEnd = instructions[index + 7];
        var jumpToBody = instructions[index + 8];
        var negativeLabel = instructions[index + 9];
        var currentLoadNegative = instructions[index + 10];
        var endLoadNegative = instructions[index + 11];
        var compareNegative = instructions[index + 12];
        var jumpNegativeEnd = instructions[index + 13];
        var bodyLabel = instructions[index + 14];

        if (!TryResolveVariableSlotForCollapse(stepLoadForSign, parameterSlots, localSlots, captureSlots, out var stepLoadOpcode, out var stepSlot)
            || stepLoadOpcode != Opcode.LoadLocal)
        {
            return false;
        }

        if (zeroLiteral.Kind != LoweredInstructionKind.LoadLiteral
            || zeroLiteral.ValueType != stepLoadForSign.Variable?.Type
            || !Equals(zeroLiteral.LiteralValue, CreateZeroLiteral(stepLoadForSign.Variable!.Type)))
        {
            return false;
        }

        if (compareStepToZero.Kind != LoweredInstructionKind.BinaryOperation
            || compareStepToZero.OperatorKind != SyntaxKind.GreaterToken
            || jumpToNegative.Kind != LoweredInstructionKind.JumpIfFalse
            || string.IsNullOrWhiteSpace(jumpToNegative.Label)
            || negativeLabel.Kind != LoweredInstructionKind.Label
            || negativeLabel.Label != jumpToNegative.Label
            || jumpToBody.Kind != LoweredInstructionKind.Jump
            || string.IsNullOrWhiteSpace(jumpToBody.Label)
            || bodyLabel.Kind != LoweredInstructionKind.Label
            || bodyLabel.Label != jumpToBody.Label)
        {
            return false;
        }

        if (!TryResolveVariableSlotForCollapse(currentLoadPositive, parameterSlots, localSlots, captureSlots, out var currentPositiveOpcode, out var currentSlot)
            || currentPositiveOpcode != Opcode.LoadLocal
            || !TryResolveVariableSlotForCollapse(endLoadPositive, parameterSlots, localSlots, captureSlots, out var endPositiveOpcode, out var endSlot)
            || endPositiveOpcode != Opcode.LoadLocal
            || comparePositive.Kind != LoweredInstructionKind.BinaryOperation
            || comparePositive.OperatorKind != SyntaxKind.GreaterToken
            || jumpPositiveEnd.Kind != LoweredInstructionKind.JumpIfTrue
            || string.IsNullOrWhiteSpace(jumpPositiveEnd.Label))
        {
            return false;
        }

        if (!TryResolveVariableSlotForCollapse(currentLoadNegative, parameterSlots, localSlots, captureSlots, out var currentNegativeOpcode, out var currentNegativeSlot)
            || currentNegativeOpcode != Opcode.LoadLocal
            || currentNegativeSlot != currentSlot
            || !TryResolveVariableSlotForCollapse(endLoadNegative, parameterSlots, localSlots, captureSlots, out var endNegativeOpcode, out var endNegativeSlot)
            || endNegativeOpcode != Opcode.LoadLocal
            || endNegativeSlot != endSlot
            || compareNegative.Kind != LoweredInstructionKind.BinaryOperation
            || compareNegative.OperatorKind != SyntaxKind.LessToken
            || jumpNegativeEnd.Kind != LoweredInstructionKind.JumpIfTrue
            || jumpNegativeEnd.Label != jumpPositiveEnd.Label)
        {
            return false;
        }

        collapsed = new CollapsedLocalForLoopCheck(currentSlot, endSlot, stepSlot, jumpPositiveEnd.Label!, jumpPositiveEnd.Location);
        return true;
    }

    private bool TryMatchCollapsedAdvanceForLoopLocal(
        IReadOnlyList<LoweredInstruction> instructions,
        int index,
        IReadOnlyDictionary<ParameterSymbol, int> parameterSlots,
        IReadOnlyDictionary<VariableSymbol, int> localSlots,
        IReadOnlyDictionary<VariableSymbol, int> captureSlots,
        out int currentSlot,
        out int stepSlot)
    {
        currentSlot = default;
        stepSlot = default;
        if (index + 3 >= instructions.Count)
        {
            return false;
        }

        var currentLoad = instructions[index];
        var stepLoad = instructions[index + 1];
        var addInstruction = instructions[index + 2];
        var currentStore = instructions[index + 3];

        if (!TryResolveVariableSlotForCollapse(currentLoad, parameterSlots, localSlots, captureSlots, out var currentLoadOpcode, out currentSlot)
            || currentLoadOpcode != Opcode.LoadLocal
            || !TryResolveVariableSlotForCollapse(stepLoad, parameterSlots, localSlots, captureSlots, out var stepLoadOpcode, out stepSlot)
            || stepLoadOpcode != Opcode.LoadLocal
            || addInstruction.Kind != LoweredInstructionKind.BinaryOperation
            || addInstruction.OperatorKind != SyntaxKind.PlusToken
            || !TryResolveVariableSlotForCollapse(currentStore, parameterSlots, localSlots, captureSlots, out var currentStoreOpcode, out var currentStoreSlot)
            || currentStoreOpcode != Opcode.StoreLocal
            || currentStoreSlot != currentSlot)
        {
            return false;
        }

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
                opcode = instruction.Kind == LoweredInstructionKind.StoreVariable ? Opcode.StoreArgument : Opcode.LoadArgument;
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
