using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteFormatInterpolatedPart(BytecodeCallFrame frame, int instructionIndex)
    {
        var stack = frame.EvaluationStack;
        var formatSpec = stack.Pop().AsString();
        var alignment = stack.Pop().AsInteger();
        var value = stack.Pop();

        try
        {
            var formatted = FormatInterpolatedValue(value, alignment, formatSpec);
            stack.Push(RuntimeValue.FromString(formatted));
        }
        catch (FormatException)
        {
            ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument,
                $"Numeric format '{formatSpec}' is not supported for {value.Type.Name}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }
    }

    private static string FormatInterpolatedValue(RuntimeValue value, int alignment, string format)
    {
        string formatted;

        if (!string.IsNullOrEmpty(format))
        {
            var upperFormat = format.ToUpperInvariant();
            formatted = value.Type switch
            {
                var t when t == TypeSymbol.Integer => ((int)value.AsInteger()).ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Long => value.AsLong().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Double => value.AsDouble().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                var t when t == TypeSymbol.Decimal => value.AsDecimal().ToString(upperFormat, System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToDisplayString()
            };
        }
        else
        {
            formatted = value.ToDisplayString();
        }

        if (alignment != 0)
        {
            RuntimeStringLimits.Check(Math.Abs((long)alignment));
            formatted = alignment > 0
                ? formatted.PadLeft(alignment)
                : formatted.PadRight(-alignment);
        }

        return formatted;
    }

    private void ExecuteAppendStringGlobal(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var slot = instruction.GetOperand(0);
        var right = PopValue(frame, instructionIndex);
        var useDisplayString = instruction.GetOperand(1) != 0;
        var fixedStringLength = GetGlobalFixedStringLength(slot);
        var current = LoadGlobalUnchecked(slot);
        var value = AppendStringValue(current, right, useDisplayString, fixedStringLength);
        StoreAppendStringGlobalValue(slot, value);
    }

    private void ExecuteAppendStringLocal(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var slot = instruction.GetOperand(0);
        var right = PopValue(frame, instructionIndex);
        var useDisplayString = instruction.GetOperand(1) != 0;
        var fixedStringLength = frame.GetLocalFixedStringLength(slot);
        var current = frame.LoadLocalValue(slot);
        var value = AppendStringValue(current, right, useDisplayString, fixedStringLength);
        frame.StoreAppendStringLocalValue(slot, value);
    }

    private static void ExecuteAppendStringSlot(BytecodeVariableSlot slot, RuntimeValue right, bool useDisplayString)
    {
        var value = AppendStringValue(slot.Value, right, useDisplayString, slot.FixedStringLength);
        slot.StoreAppendStringValue(value);
    }

    private static RuntimeValue AppendStringValue(RuntimeValue current, RuntimeValue right, bool useDisplayString, int? fixedStringLength)
    {
        var appendText = useDisplayString ? right.ToDisplayString() : right.AsString();

        if (fixedStringLength is not null)
        {
            // Нормализованная фиксированная строка уже занимает всю ширину.
            return current.CopyForStorage(fixedStringLength);
        }

        var accumulator = current.AsStringAccumulator();
        if (accumulator is null)
        {
            accumulator = new RuntimeStringAccumulator(current.AsString());
        }

        accumulator.Append(appendText);
        return RuntimeValue.FromStringAccumulator(accumulator);
    }
}
