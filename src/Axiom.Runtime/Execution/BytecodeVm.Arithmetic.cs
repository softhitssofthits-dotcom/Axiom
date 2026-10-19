using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteBinaryInstruction(BytecodeCallFrame frame, Opcode opcode, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateFastBinaryInstruction(opcode, left, right, out var fastResult))
        {
            stack.Push(fastResult);
            return;
        }

        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        stack.Push(opcode switch
        {
            Opcode.Add => EvaluateAdd(left, right, location),
            Opcode.Concat => RuntimeValue.FromString(RuntimeStringLimits.Concat(left.ToDisplayString(), right.ToDisplayString())),
            Opcode.Subtract => EvaluateSubtract(left, right, location),
            Opcode.Multiply => EvaluateMultiply(left, right, location),
            Opcode.Power => EvaluatePower(left, right, location),
            Opcode.Divide => EvaluateDivide(left, right, location),
            Opcode.IntegralDivide => EvaluateIntegralDivide(left, right, location),
            Opcode.Modulo => EvaluateModulo(left, right, location),
            Opcode.And => EvaluateAnd(left, right, location),
            Opcode.Or => EvaluateOr(left, right, location),
            Opcode.Xor => EvaluateXor(left, right, location),
            Opcode.Eqv => EvaluateEqv(left, right, location),
            Opcode.Imp => EvaluateImp(left, right, location),
            Opcode.ShiftLeft => EvaluateShiftLeft(left, right, location),
            Opcode.ShiftRight => EvaluateShiftRight(left, right, location),
            Opcode.CompareEqual => RuntimeValue.FromBoolean(ValuesEqual(left, right)),
            Opcode.CompareNotEqual => RuntimeValue.FromBoolean(!ValuesEqual(left, right)),
            Opcode.CompareLess => RuntimeValue.FromBoolean(Compare(left, right, location) < 0),
            Opcode.CompareLessOrEqual => RuntimeValue.FromBoolean(Compare(left, right, location) <= 0),
            Opcode.CompareGreater => RuntimeValue.FromBoolean(Compare(left, right, location) > 0),
            Opcode.CompareGreaterOrEqual => RuntimeValue.FromBoolean(Compare(left, right, location) >= 0),
            _ => throw new InvalidOperationException($"Unsupported binary opcode '{opcode}'.")
        });
    }

    private void ExecuteAddInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotAdd(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateAdd(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteAddLongIntegerInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (left.Type == TypeSymbol.Long && right.Type == TypeSymbol.Integer)
        {
            try
            {
                stack.PushUnchecked(RuntimeValue.FromLong(checked(GetLongUnchecked(left) + GetIntegerUnchecked(right))));
                return;
            }
            catch (OverflowException)
            {
            }
        }

        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        stack.PushUnchecked(EvaluateAdd(
            ConvertValue(right, BytecodeScalarTypeCode.Long, location),
            left,
            location));
    }

    private void ExecuteSubtractInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotSubtract(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateSubtract(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteModuloInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotModulo(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateModulo(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteAndInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotAnd(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateAnd(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteOrInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotOr(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateOr(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteXorInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotXor(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateXor(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteShiftLeftInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotShiftLeft(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateShiftLeft(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteShiftRightInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotShiftRight(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateShiftRight(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteNegateInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var operand = frame.EvaluationStack.PopUnchecked();

        if (TryEvaluateHotNegate(operand, out var result))
        {
            frame.EvaluationStack.PushUnchecked(result);
            return;
        }

        frame.EvaluationStack.Push(EvaluateNegate(operand, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private static bool TryEvaluateHotNegate(RuntimeValue operand, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (operand.Type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(-GetIntegerUnchecked(operand)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (operand.Type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(-GetLongUnchecked(operand)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (operand.Type == TypeSymbol.Double)
        {
            var value = -GetDoubleUnchecked(operand);
            if (double.IsFinite(value))
            {
                result = RuntimeValue.FromDouble(value);
                return true;
            }

            return false;
        }

        if (operand.Type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(-GetDecimalUnchecked(operand)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private void ExecuteNotInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var operand = frame.EvaluationStack.PopUnchecked();

        if (TryEvaluateHotNot(operand, out var result))
        {
            frame.EvaluationStack.PushUnchecked(result);
            return;
        }

        frame.EvaluationStack.Push(EvaluateNot(operand, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private static bool TryEvaluateHotNot(RuntimeValue operand, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (operand.Type == TypeSymbol.Boolean)
        {
            result = RuntimeValue.FromBoolean(!GetBooleanUnchecked(operand));
            return true;
        }

        if (operand.Type == TypeSymbol.Integer)
        {
            result = RuntimeValue.FromInteger(~GetIntegerUnchecked(operand));
            return true;
        }

        if (operand.Type == TypeSymbol.Long)
        {
            result = RuntimeValue.FromLong(~GetLongUnchecked(operand));
            return true;
        }

        return false;
    }

    private void ExecuteJumpIfEqualInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();
        var targetInstruction = instruction.GetOperand(0);

        if (TryEvaluateFastComparisonCondition(Opcode.CompareEqual, left, right, out var fastShouldJump))
        {
            if (fastShouldJump)
            {
                frame.InstructionPointer = targetInstruction;
            }

            return;
        }

        if (ValuesEqual(left, right))
        {
            frame.InstructionPointer = targetInstruction;
        }
    }

    private void ExecuteCompareEqualInstruction(BytecodeCallFrame frame)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();
        stack.PushUnchecked(RuntimeValue.FromBoolean(ValuesEqual(left, right)));
    }

    private void ExecuteCompareNotEqualInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateFastComparisonCondition(Opcode.CompareNotEqual, left, right, out var result))
        {
            stack.PushUnchecked(RuntimeValue.FromBoolean(result));
            return;
        }

        stack.PushUnchecked(RuntimeValue.FromBoolean(
            !ValuesEqual(left, right)));
    }

    private void ExecuteJumpIfNotEqualInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();
        var targetInstruction = instruction.GetOperand(0);

        if (TryEvaluateFastComparisonCondition(Opcode.CompareNotEqual, left, right, out var fastShouldJump))
        {
            if (fastShouldJump)
            {
                frame.InstructionPointer = targetInstruction;
            }

            return;
        }

        if (!ValuesEqual(left, right))
        {
            frame.InstructionPointer = targetInstruction;
        }
    }

    private void ExecuteJumpIfLessInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        ExecuteOrderedComparisonJumpInstruction(frame, instruction, instructionIndex, Opcode.CompareLess);
    }

    private void ExecuteCompareLessInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        ExecuteOrderedComparisonInstruction(frame, instructionIndex, Opcode.CompareLess);
    }

    private void ExecuteJumpIfLessOrEqualInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        ExecuteOrderedComparisonJumpInstruction(frame, instruction, instructionIndex, Opcode.CompareLessOrEqual);
    }

    private void ExecuteCompareLessOrEqualInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        ExecuteOrderedComparisonInstruction(frame, instructionIndex, Opcode.CompareLessOrEqual);
    }

    private void ExecuteJumpIfGreaterInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        ExecuteOrderedComparisonJumpInstruction(frame, instruction, instructionIndex, Opcode.CompareGreater);
    }

    private void ExecuteJumpIfGreaterOrEqualInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        ExecuteOrderedComparisonJumpInstruction(frame, instruction, instructionIndex, Opcode.CompareGreaterOrEqual);
    }

    private void ExecuteCompareGreaterOrEqualInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        ExecuteOrderedComparisonInstruction(frame, instructionIndex, Opcode.CompareGreaterOrEqual);
    }

    private void ExecuteOrderedComparisonInstruction(
        BytecodeCallFrame frame,
        int instructionIndex,
        Opcode comparisonOpcode)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateFastComparisonCondition(comparisonOpcode, left, right, out var result))
        {
            stack.PushUnchecked(RuntimeValue.FromBoolean(result));
            return;
        }

        var comparison = Compare(left, right, ResolveLocation(frame.Routine.Id, instructionIndex));
        var comparisonResult = comparisonOpcode switch
        {
            Opcode.CompareLess => comparison < 0,
            Opcode.CompareLessOrEqual => comparison <= 0,
            Opcode.CompareGreaterOrEqual => comparison >= 0,
            _ => false
        };

        stack.PushUnchecked(RuntimeValue.FromBoolean(comparisonResult));
    }

    private void ExecuteOrderedComparisonJumpInstruction(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        int instructionIndex,
        Opcode comparisonOpcode)
    {
        var stack = frame.EvaluationStack;
        var right = stack.Pop();
        var left = stack.Pop();
        var targetInstruction = instruction.GetOperand(0);

        if (TryEvaluateFastComparisonCondition(comparisonOpcode, left, right, out var fastShouldJump))
        {
            if (fastShouldJump)
            {
                frame.InstructionPointer = targetInstruction;
            }

            return;
        }

        var comparison = Compare(left, right, ResolveLocation(frame.Routine.Id, instructionIndex));
        var shouldJump = comparisonOpcode switch
        {
            Opcode.CompareLess => comparison < 0,
            Opcode.CompareLessOrEqual => comparison <= 0,
            Opcode.CompareGreater => comparison > 0,
            Opcode.CompareGreaterOrEqual => comparison >= 0,
            _ => false
        };

        if (shouldJump)
        {
            frame.InstructionPointer = targetInstruction;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteJumpIfBooleanInstruction(
        BytecodeCallFrame frame,
        BytecodeInstruction instruction,
        bool invertCondition)
    {
        var shouldJump = GetBooleanUnchecked(frame.EvaluationStack.PopUnchecked());
        if (invertCondition)
        {
            shouldJump = !shouldJump;
        }

        if (shouldJump)
        {
            frame.InstructionPointer = instruction.GetOperand(0);
        }
    }

    private void ExecuteJumpIfForDoneLocalInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        frame.LoadForLoopLocalValues(
            instruction.GetOperand(0),
            instruction.GetOperand(1),
            instruction.GetOperand(2),
            out var current,
            out var end,
            out var step);

        if (TryShouldTerminateLocalForLoop(current, end, step, out var shouldJump))
        {
            if (shouldJump)
            {
                frame.InstructionPointer = instruction.GetOperand(3);
            }

            return;
        }

        if (IsZero(step))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.StepZero,
                "В цикле FOR недопустим STEP 0.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var comparison = Compare(current, end, location);
        var stepIsPositive = step.Type switch
        {
            var type when type == TypeSymbol.Integer => step.AsInteger() > 0,
            var type when type == TypeSymbol.Long => step.AsLong() > 0L,
            var type when type == TypeSymbol.Double => step.AsDouble() > 0d,
            var type when type == TypeSymbol.Decimal => step.AsDecimal() > 0m,
            _ => false
        };
        if (stepIsPositive ? comparison > 0 : comparison < 0)
        {
            frame.InstructionPointer = instruction.GetOperand(3);
        }
    }

    private void ExecuteMultiplyInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotMultiply(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateMultiply(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteDivideInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateHotDivide(left, right, out var result))
        {
            stack.PushUnchecked(result);
            return;
        }

        stack.PushUnchecked(EvaluateDivide(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private void ExecuteCompareGreaterInstruction(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var right = stack.PopUnchecked();
        var left = stack.PopUnchecked();

        if (TryEvaluateFastComparisonCondition(Opcode.CompareGreater, left, right, out var isGreater))
        {
            stack.PushUnchecked(RuntimeValue.FromBoolean(isGreater));
            return;
        }

        stack.PushUnchecked(RuntimeValue.FromBoolean(
            Compare(left, right, ResolveLocation(frame.Routine.Id, instructionIndex)) > 0));
    }

    private void ExecuteAdvanceForLoopLocalInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var currentSlot = instruction.GetOperand(0);
        frame.LoadForLoopAdvanceValues(currentSlot, instruction.GetOperand(1), out var current, out var step);

        if (TryAdvanceLocalForLoop(current, step, out var nextValue))
        {
            frame.StoreNumericLocalValueUnchecked(currentSlot, nextValue);
            return;
        }

        frame.StoreNumericLocalValueUnchecked(
            currentSlot,
            EvaluateAdd(current, step, ResolveLocation(frame.Routine.Id, instructionIndex)));
    }

    private static bool TryEvaluateFastBinaryInstruction(
        Opcode opcode,
        RuntimeValue left,
        RuntimeValue right,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (left.Type != right.Type)
        {
            return false;
        }

        if (opcode is Opcode.CompareEqual or Opcode.CompareNotEqual)
        {
            var equal = ValuesEqual(left, right);
            result = RuntimeValue.FromBoolean(opcode == Opcode.CompareEqual ? equal : !equal);
            return true;
        }

        if (TryEvaluateFastComparison(opcode, left, right, out result))
        {
            return true;
        }

        if (left.Type == TypeSymbol.Boolean)
        {
            return TryEvaluateFastBooleanBinaryInstruction(opcode, GetBooleanUnchecked(left), GetBooleanUnchecked(right), out result);
        }

        if (left.Type == TypeSymbol.Integer)
        {
            return TryEvaluateFastIntegerBinaryInstruction(opcode, GetIntegerUnchecked(left), GetIntegerUnchecked(right), out result);
        }

        if (left.Type == TypeSymbol.Long)
        {
            return TryEvaluateFastLongBinaryInstruction(opcode, GetLongUnchecked(left), GetLongUnchecked(right), out result);
        }

        if (left.Type == TypeSymbol.String && opcode == Opcode.Add)
        {
            result = RuntimeValue.FromString(RuntimeStringLimits.Concat(GetStringUnchecked(left), GetStringUnchecked(right)));
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotAdd(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(GetIntegerUnchecked(left) + GetIntegerUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(GetLongUnchecked(left) + GetLongUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Double)
        {
            result = TryCreateFiniteDouble(GetDoubleUnchecked(left) + GetDoubleUnchecked(right));
            return result != RuntimeValue.Void;
        }

        if (type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(GetDecimalUnchecked(left) + GetDecimalUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.String)
        {
            result = RuntimeValue.FromString(RuntimeStringLimits.Concat(GetStringUnchecked(left), GetStringUnchecked(right)));
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotMultiply(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(GetIntegerUnchecked(left) * GetIntegerUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(GetLongUnchecked(left) * GetLongUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Double)
        {
            result = TryCreateFiniteDouble(GetDoubleUnchecked(left) * GetDoubleUnchecked(right));
            return result != RuntimeValue.Void;
        }

        if (type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(GetDecimalUnchecked(left) * GetDecimalUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryEvaluateHotDivide(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (left.Type == TypeSymbol.Double && right.Type == TypeSymbol.Double)
        {
            var divisor = GetDoubleUnchecked(right);
            if (divisor == 0d)
            {
                return false;
            }

            result = TryCreateFiniteDouble(GetDoubleUnchecked(left) / divisor);
            return result != RuntimeValue.Void;
        }

        if (left.Type == TypeSymbol.Decimal && right.Type == TypeSymbol.Decimal)
        {
            var divisor = GetDecimalUnchecked(right);
            if (divisor == 0m)
            {
                return false;
            }

            try
            {
                result = RuntimeValue.FromDecimal(GetDecimalUnchecked(left) / divisor);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryEvaluateHotSubtract(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Integer)
        {
            try
            {
                result = RuntimeValue.FromInteger(checked(GetIntegerUnchecked(left) - GetIntegerUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Long)
        {
            try
            {
                result = RuntimeValue.FromLong(checked(GetLongUnchecked(left) - GetLongUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        if (type == TypeSymbol.Double)
        {
            result = TryCreateFiniteDouble(GetDoubleUnchecked(left) - GetDoubleUnchecked(right));
            return result != RuntimeValue.Void;
        }

        if (type == TypeSymbol.Decimal)
        {
            try
            {
                result = RuntimeValue.FromDecimal(checked(GetDecimalUnchecked(left) - GetDecimalUnchecked(right)));
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryEvaluateHotModulo(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        try
        {
            if (type == TypeSymbol.Integer)
            {
                var divisor = GetIntegerUnchecked(right);
                if (divisor == 0)
                {
                    return false;
                }

                result = RuntimeValue.FromInteger(checked(GetIntegerUnchecked(left) % divisor));
                return true;
            }

            if (type == TypeSymbol.Long)
            {
                var divisor = GetLongUnchecked(right);
                if (divisor == 0L)
                {
                    return false;
                }

                result = RuntimeValue.FromLong(checked(GetLongUnchecked(left) % divisor));
                return true;
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return false;
    }

    private static bool TryEvaluateHotAnd(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Boolean)
        {
            result = RuntimeValue.FromBoolean(GetBooleanUnchecked(left) && GetBooleanUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Integer)
        {
            result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) & GetIntegerUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            result = RuntimeValue.FromLong(GetLongUnchecked(left) & GetLongUnchecked(right));
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotOr(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Boolean)
        {
            result = RuntimeValue.FromBoolean(GetBooleanUnchecked(left) || GetBooleanUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Integer)
        {
            result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) | GetIntegerUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            result = RuntimeValue.FromLong(GetLongUnchecked(left) | GetLongUnchecked(right));
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotXor(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;
        var type = left.Type;

        if (type != right.Type)
        {
            return false;
        }

        if (type == TypeSymbol.Boolean)
        {
            result = RuntimeValue.FromBoolean(GetBooleanUnchecked(left) ^ GetBooleanUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Integer)
        {
            result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) ^ GetIntegerUnchecked(right));
            return true;
        }

        if (type == TypeSymbol.Long)
        {
            result = RuntimeValue.FromLong(GetLongUnchecked(left) ^ GetLongUnchecked(right));
            return true;
        }

        return false;
    }

    private static bool TryEvaluateHotShiftLeft(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        var count = right.Type == TypeSymbol.Integer
            ? GetIntegerUnchecked(right)
            : unchecked((int)GetLongUnchecked(right));

        if (count < 0 || count > 63)
            return false;

        if (left.Type == TypeSymbol.Integer)
        {
            if (right.Type == TypeSymbol.Integer)
            {
                result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) << count);
                return true;
            }

            if (right.Type == TypeSymbol.Long)
            {
                result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) << count);
                return true;
            }
        }

        if (left.Type == TypeSymbol.Long)
        {
            if (right.Type == TypeSymbol.Integer)
            {
                result = RuntimeValue.FromLong(GetLongUnchecked(left) << count);
                return true;
            }

            if (right.Type == TypeSymbol.Long)
            {
                result = RuntimeValue.FromLong(GetLongUnchecked(left) << count);
                return true;
            }
        }

        return false;
    }

    private static bool TryEvaluateHotShiftRight(RuntimeValue left, RuntimeValue right, out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        var count = right.Type == TypeSymbol.Integer
            ? GetIntegerUnchecked(right)
            : unchecked((int)GetLongUnchecked(right));

        if (count < 0 || count > 63)
            return false;

        if (left.Type == TypeSymbol.Integer)
        {
            if (right.Type == TypeSymbol.Integer)
            {
                result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) >> count);
                return true;
            }

            if (right.Type == TypeSymbol.Long)
            {
                result = RuntimeValue.FromInteger(GetIntegerUnchecked(left) >> count);
                return true;
            }
        }

        if (left.Type == TypeSymbol.Long)
        {
            if (right.Type == TypeSymbol.Integer)
            {
                result = RuntimeValue.FromLong(GetLongUnchecked(left) >> count);
                return true;
            }

            if (right.Type == TypeSymbol.Long)
            {
                result = RuntimeValue.FromLong(GetLongUnchecked(left) >> count);
                return true;
            }
        }

        return false;
    }

    private static bool TryShouldTerminateLocalForLoop(RuntimeValue current, RuntimeValue end, RuntimeValue step, out bool shouldTerminate)
    {
        shouldTerminate = false;
        if (current.Type != end.Type || current.Type != step.Type)
        {
            return false;
        }

        if (current.Type == TypeSymbol.Integer)
        {
            var stepValue = GetIntegerUnchecked(step);
            var currentValue = GetIntegerUnchecked(current);
            var endValue = GetIntegerUnchecked(end);
            shouldTerminate = stepValue > 0
                ? currentValue > endValue
                : currentValue < endValue;
            return true;
        }

        if (current.Type == TypeSymbol.Long)
        {
            var stepValue = GetLongUnchecked(step);
            var currentValue = GetLongUnchecked(current);
            var endValue = GetLongUnchecked(end);
            shouldTerminate = stepValue > 0L
                ? currentValue > endValue
                : currentValue < endValue;
            return true;
        }

        if (current.Type == TypeSymbol.Double)
        {
            var stepValue = GetDoubleUnchecked(step);
            var currentValue = GetDoubleUnchecked(current);
            var endValue = GetDoubleUnchecked(end);
            shouldTerminate = stepValue > 0d
                ? currentValue > endValue
                : currentValue < endValue;
            return true;
        }

        if (current.Type == TypeSymbol.Decimal)
        {
            var stepValue = GetDecimalUnchecked(step);
            var currentValue = GetDecimalUnchecked(current);
            var endValue = GetDecimalUnchecked(end);
            shouldTerminate = stepValue > 0m
                ? currentValue > endValue
                : currentValue < endValue;
            return true;
        }

        return false;
    }

    private static bool TryAdvanceLocalForLoop(RuntimeValue current, RuntimeValue step, out RuntimeValue nextValue)
    {
        nextValue = RuntimeValue.Void;
        if (current.Type != step.Type)
        {
            return false;
        }

        try
        {
            if (current.Type == TypeSymbol.Integer)
            {
                nextValue = RuntimeValue.FromInteger(checked(GetIntegerUnchecked(current) + GetIntegerUnchecked(step)));
                return true;
            }

            if (current.Type == TypeSymbol.Long)
            {
                nextValue = RuntimeValue.FromLong(checked(GetLongUnchecked(current) + GetLongUnchecked(step)));
                return true;
            }

            if (current.Type == TypeSymbol.Double)
            {
                nextValue = TryCreateFiniteDouble(GetDoubleUnchecked(current) + GetDoubleUnchecked(step));
                return nextValue.Type != TypeSymbol.Void;
            }

            if (current.Type == TypeSymbol.Decimal)
            {
                nextValue = RuntimeValue.FromDecimal(checked(GetDecimalUnchecked(current) + GetDecimalUnchecked(step)));
                return true;
            }
        }
        catch (OverflowException)
        {
        }

        nextValue = RuntimeValue.Void;
        return false;
    }

    private static RuntimeValue TryCreateFiniteDouble(double value) =>
        double.IsFinite(value)
            ? RuntimeValue.FromDouble(value)
            : RuntimeValue.Void;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIntegerUnchecked(RuntimeValue value) => (int)value.Value!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long GetLongUnchecked(RuntimeValue value) => (long)value.Value!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetDoubleUnchecked(RuntimeValue value) => (double)value.Value!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static decimal GetDecimalUnchecked(RuntimeValue value) => (decimal)value.Value!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetStringUnchecked(RuntimeValue value) => value.AsString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool GetBooleanUnchecked(RuntimeValue value) => (bool)value.Value!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeImageValue? GetImageUnchecked(RuntimeValue value) => (RuntimeImageValue?)value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeSpriteValue? GetSpriteUnchecked(RuntimeValue value) => (RuntimeSpriteValue?)value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeTilesetValue? GetTilesetUnchecked(RuntimeValue value) => (RuntimeTilesetValue?)value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RuntimeTilemapValue? GetTilemapUnchecked(RuntimeValue value) => (RuntimeTilemapValue?)value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetImageWidthFast(RuntimeValue value) => GetImageUnchecked(value)?.Width ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetImageHeightFast(RuntimeValue value) => GetImageUnchecked(value)?.Height ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSpriteWidthFast(RuntimeValue value) => GetSpriteUnchecked(value)?.FrameWidth ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSpriteHeightFast(RuntimeValue value) => GetSpriteUnchecked(value)?.FrameHeight ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSpriteCountFast(RuntimeValue value) => GetSpriteUnchecked(value)?.FrameCount ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetTileCountFast(RuntimeValue value) => GetTilesetUnchecked(value)?.TileCount ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetTileWidthFast(RuntimeValue value) => GetTilesetUnchecked(value)?.TileWidth ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetTileHeightFast(RuntimeValue value) => GetTilesetUnchecked(value)?.TileHeight ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapWidthFast(RuntimeValue value) => GetTilemapUnchecked(value)?.Width ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapHeightFast(RuntimeValue value) => GetTilemapUnchecked(value)?.Height ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapPixelWidthFast(RuntimeValue value) => GetTilemapUnchecked(value)?.PixelWidth ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapPixelHeightFast(RuntimeValue value) => GetTilemapUnchecked(value)?.PixelHeight ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapTileWidthFast(RuntimeValue value) => GetTilemapUnchecked(value)?.TileWidth ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapTileHeightFast(RuntimeValue value) => GetTilemapUnchecked(value)?.TileHeight ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetMapLayerCountFast(RuntimeValue value) => GetTilemapUnchecked(value)?.LayerCount ?? 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryGetFastWholeNumber(RuntimeValue value, out long result)
    {
        if (value.Type == TypeSymbol.Integer)
        {
            result = GetIntegerUnchecked(value);
            return true;
        }

        if (value.Type == TypeSymbol.Long)
        {
            result = GetLongUnchecked(value);
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryEvaluateFastBooleanBinaryInstruction(
        Opcode opcode,
        bool left,
        bool right,
        out RuntimeValue result)
    {
        result = opcode switch
        {
            Opcode.And => RuntimeValue.FromBoolean(left && right),
            Opcode.Or => RuntimeValue.FromBoolean(left || right),
            Opcode.Xor => RuntimeValue.FromBoolean(left ^ right),
            Opcode.Eqv => RuntimeValue.FromBoolean(left == right),
            Opcode.Imp => RuntimeValue.FromBoolean(!left || right),
            _ => RuntimeValue.Void
        };

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastIntegerBinaryInstruction(
        Opcode opcode,
        int left,
        int right,
        out RuntimeValue result)
    {
        try
        {
            result = opcode switch
            {
                Opcode.Add => RuntimeValue.FromInteger(checked(left + right)),
                Opcode.Subtract => RuntimeValue.FromInteger(checked(left - right)),
                Opcode.Multiply => RuntimeValue.FromInteger(checked(left * right)),
                Opcode.And => RuntimeValue.FromInteger(left & right),
                Opcode.Or => RuntimeValue.FromInteger(left | right),
                Opcode.Xor => RuntimeValue.FromInteger(left ^ right),
                Opcode.Eqv => RuntimeValue.FromInteger(~(left ^ right)),
                Opcode.Imp => RuntimeValue.FromInteger((~left) | right),
                Opcode.ShiftLeft => right is >= 0 and <= 31 ? RuntimeValue.FromInteger(left << right) : RuntimeValue.Void,
                Opcode.ShiftRight => right is >= 0 and <= 31 ? RuntimeValue.FromInteger(left >> right) : RuntimeValue.Void,
                _ => RuntimeValue.Void
            };
        }
        catch (OverflowException)
        {
            result = RuntimeValue.Void;
            return false;
        }

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastLongBinaryInstruction(
        Opcode opcode,
        long left,
        long right,
        out RuntimeValue result)
    {
        try
        {
            result = opcode switch
            {
                Opcode.Add => RuntimeValue.FromLong(checked(left + right)),
                Opcode.Subtract => RuntimeValue.FromLong(checked(left - right)),
                Opcode.Multiply => RuntimeValue.FromLong(checked(left * right)),
                Opcode.And => RuntimeValue.FromLong(left & right),
                Opcode.Or => RuntimeValue.FromLong(left | right),
                Opcode.Xor => RuntimeValue.FromLong(left ^ right),
                Opcode.Eqv => RuntimeValue.FromLong(~(left ^ right)),
                Opcode.Imp => RuntimeValue.FromLong((~left) | right),
                Opcode.ShiftLeft => right is >= 0 and <= 63 ? RuntimeValue.FromLong(left << unchecked((int)right)) : RuntimeValue.Void,
                Opcode.ShiftRight => right is >= 0 and <= 63 ? RuntimeValue.FromLong(left >> unchecked((int)right)) : RuntimeValue.Void,
                _ => RuntimeValue.Void
            };
        }
        catch (OverflowException)
        {
            result = RuntimeValue.Void;
            return false;
        }

        return result != RuntimeValue.Void;
    }

    private static bool TryEvaluateFastComparison(
        Opcode opcode,
        RuntimeValue left,
        RuntimeValue right,
        out RuntimeValue result)
    {
        result = RuntimeValue.Void;

        if (!TryEvaluateFastComparisonCondition(opcode, left, right, out var comparisonResult))
        {
            return false;
        }

        result = RuntimeValue.FromBoolean(comparisonResult);
        return true;
    }

    private static bool TryEvaluateFastComparisonCondition(
        Opcode opcode,
        RuntimeValue left,
        RuntimeValue right,
        out bool result)
    {
        result = false;

        if (left.Type != right.Type)
        {
            return false;
        }

        if (opcode is Opcode.CompareEqual or Opcode.CompareNotEqual)
        {
            var equal = ValuesEqual(left, right);
            result = opcode == Opcode.CompareEqual ? equal : !equal;
            return true;
        }

        var comparison = opcode switch
        {
            Opcode.CompareLess
            or Opcode.CompareLessOrEqual
            or Opcode.CompareGreater
            or Opcode.CompareGreaterOrEqual => left.Type switch
            {
                var type when type == TypeSymbol.Integer => GetIntegerUnchecked(left).CompareTo(GetIntegerUnchecked(right)),
                var type when type == TypeSymbol.Long => GetLongUnchecked(left).CompareTo(GetLongUnchecked(right)),
                var type when type == TypeSymbol.Double => GetDoubleUnchecked(left).CompareTo(GetDoubleUnchecked(right)),
                var type when type == TypeSymbol.Decimal => GetDecimalUnchecked(left).CompareTo(GetDecimalUnchecked(right)),
                var type when type == TypeSymbol.String => string.Compare(GetStringUnchecked(left), GetStringUnchecked(right), StringComparison.Ordinal),
                _ => (int?)null
            },
            _ => (int?)null
        };

        if (comparison is null)
        {
            return false;
        }

        result = opcode switch
        {
            Opcode.CompareLess => comparison < 0,
            Opcode.CompareLessOrEqual => comparison <= 0,
            Opcode.CompareGreater => comparison > 0,
            Opcode.CompareGreaterOrEqual => comparison >= 0,
            _ => false
        };
        return true;
    }

}
