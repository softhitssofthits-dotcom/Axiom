using System.Globalization;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteInput(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var plan = GetInputInstructionPlan(frame.Routine, instruction, instructionIndex);

        if (plan.ArrayTargetName is not null)
        {
            ReportInvalidRuntimeState($"INPUT does not support array '{plan.ArrayTargetName}'.", location);
            throw new RuntimeFailureException();
        }

        var text = _host.ReadInput(new InputRequest(plan.PromptText, plan.Targets.Length));
        InvalidateReverseExecutionHistory();

        if (!TryParseInputValues(plan.Targets, text, location, out var values))
        {
            throw new RuntimeFailureException();
        }

        for (var index = 0; index < plan.Targets.Length; index++)
        {
            StoreInputValue(frame, plan.Targets[index], values[index], instructionIndex);
        }
    }

    private string? ReadInputPrompt(int promptOperand)
    {
        if (promptOperand == 0)
        {
            return null;
        }

        return ReadConstant(promptOperand - 1).AsString();
    }

    private InputInstructionPlan GetInputInstructionPlan(BytecodeRoutine routine, BytecodeInstruction instruction, int instructionIndex)
    {
        var key = (routine.Id, instructionIndex);
        if (_inputInstructionCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var valueCount = instruction.GetOperand(0);
        var promptText = ReadInputPrompt(instruction.GetOperand(1));
        var targets = new InputTarget[valueCount];
        string? arrayTargetName = null;

        for (var index = 0; index < valueCount; index++)
        {
            var operandOffset = 2 + index * 2;
            var target = ResolveInputTarget(
                routine,
                instruction.GetOperand(operandOffset),
                instruction.GetOperand(operandOffset + 1),
                instructionIndex);
            targets[index] = target;

            if (arrayTargetName is null && target.IsArray)
            {
                arrayTargetName = target.Name;
            }
        }

        var plan = new InputInstructionPlan(promptText, targets, arrayTargetName);
        _inputInstructionCache[key] = plan;
        return plan;
    }

    private InputTarget ResolveInputTarget(BytecodeRoutine routine, int targetKind, int slot, int instructionIndex)
    {
        return targetKind switch
        {
            0 => ResolveGlobalInputTarget(slot, instructionIndex),
            1 => ResolveLocalInputTarget(routine, slot, instructionIndex),
            2 => ResolveArgumentInputTarget(routine, slot, instructionIndex),
            3 => ResolveCaptureInputTarget(routine, slot, instructionIndex),
            _ => ReportInvalidRuntimeState<InputTarget>(
                $"Bytecode VM получила неизвестный вид цели INPUT {targetKind}.",
                ResolveLocation(routine.Id, instructionIndex))
        };
    }

    private InputTarget ResolveGlobalInputTarget(int slot, int instructionIndex)
    {
        if (!_globalMetadata.TryGetValue(slot, out var global))
        {
            ReportInvalidRuntimeState($"Не найден глобальный слот INPUT {slot}.", ResolveCurrentLocation(instructionIndex));
            throw new RuntimeFailureException();
        }

        return new InputTarget(0, slot, global.Name, global.Type, global.FixedStringLength);
    }

    private static InputTarget ResolveLocalInputTarget(BytecodeRoutine routine, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= routine.LocalVariables.Count)
        {
            throw CreateInvalidSlotException(routine, "локальный", slot, instructionIndex);
        }

        var variable = routine.LocalVariables[slot];
        return new InputTarget(1, slot, variable.Name, variable.Type, variable.FixedStringLength);
    }

    private static InputTarget ResolveArgumentInputTarget(BytecodeRoutine routine, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= routine.ParameterVariables.Count)
        {
            throw CreateInvalidSlotException(routine, "аргументный", slot, instructionIndex);
        }

        var variable = routine.ParameterVariables[slot];
        return new InputTarget(2, slot, variable.Name, variable.Type, variable.FixedStringLength);
    }

    private static InputTarget ResolveCaptureInputTarget(BytecodeRoutine routine, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= routine.CapturedVariables.Count)
        {
            throw CreateInvalidSlotException(routine, "захваченный", slot, instructionIndex);
        }

        var variable = routine.CapturedVariables[slot];
        return new InputTarget(3, slot, variable.Name, variable.Type, variable.FixedStringLength);
    }

    private static InvalidOperationException CreateInvalidSlotException(BytecodeRoutine routine, string kind, int slot, int instructionIndex)
    {
        return new InvalidOperationException(
            $"Routine '{routine.Name}' имеет неверный {kind} слот {slot} в инструкции #{instructionIndex}.");
    }

    private void StoreInputValue(BytecodeCallFrame frame, InputTarget target, RuntimeValue value, int instructionIndex)
    {
        switch (target.Kind)
        {
            case 0:
                StoreGlobal(target.Slot, value);
                break;

            case 1:
                StoreLocal(frame, target.Slot, value, instructionIndex);
                break;

            case 2:
                StoreArgument(frame, target.Slot, value, instructionIndex);
                break;

            case 3:
                StoreCapture(frame, target.Slot, value, instructionIndex);
                break;

            default:
                ReportInvalidRuntimeState($"Bytecode VM получила неизвестный вид цели INPUT {target.Kind}.", ResolveCurrentLocation(instructionIndex));
                throw new RuntimeFailureException();
        }
    }

    private RuntimeValue? TryParseInput(TypeSymbol type, ReadOnlySpan<char> text, TextLocation location)
    {
        if (type == TypeSymbol.Integer)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue)
                ? RuntimeValue.FromInteger(integerValue)
                : null;
        }

        if (type == TypeSymbol.Long)
        {
            return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue)
                ? RuntimeValue.FromLong(longValue)
                : null;
        }

        if (type == TypeSymbol.Double)
        {
            return double.TryParse(text, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var doubleValue)
                   && double.IsFinite(doubleValue)
                ? RuntimeValue.FromDouble(doubleValue)
                : null;
        }

        if (type == TypeSymbol.Decimal)
        {
            return decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var decimalValue)
                ? RuntimeValue.FromDecimal(decimalValue)
                : null;
        }

        if (type == TypeSymbol.String)
        {
            return RuntimeValue.FromString(text.ToString());
        }

        if (type == TypeSymbol.Boolean)
        {
            if (text.Equals("TRUE".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(true);
            }

            if (text.Equals("FALSE".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(false);
            }

            return null;
        }

        return ReportInvalidRuntimeState<RuntimeValue?>($"INPUT не поддерживает тип {type.Name}.", location);
    }

    private bool TryParseInputValues(
        IReadOnlyList<InputTarget> targets,
        string text,
        TextLocation location,
        out RuntimeValue[] values)
    {
        ArgumentNullException.ThrowIfNull(targets);

        values = Array.Empty<RuntimeValue>();

        if (targets.Count == 0)
        {
            return true;
        }

        if (targets.Count == 1)
        {
            var parsedValue = TryParseInput(targets[0].Type, text.AsSpan(), location);
            if (parsedValue is null)
            {
                ReportInputConversionFailure(targets[0].Type, text, location);
                return false;
            }

            values = [parsedValue];
            return true;
        }

        var actualCount = CountInputFields(text);
        if (actualCount != targets.Count)
        {
            ReportInputValueCountFailure(targets.Count, actualCount, location);
            return false;
        }

        var parsedValues = new RuntimeValue[targets.Count];
        var span = text.AsSpan();
        var start = 0;

        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var remaining = span[start..];
            ReadOnlySpan<char> field;

            if (index == targets.Count - 1)
            {
                field = remaining;
            }
            else
            {
                var separatorIndex = remaining.IndexOf(',');
                field = separatorIndex >= 0
                    ? remaining[..separatorIndex]
                    : remaining;
                start += separatorIndex + 1;
            }

            field = NormalizeInputField(field, targets.Count > 1);
            var parsedValue = TryParseInput(target.Type, field, location);

            if (parsedValue is null)
            {
                ReportInputConversionFailure(target.Type, field.ToString(), location);
                return false;
            }

            parsedValues[index] = parsedValue;
        }

        values = parsedValues;
        return true;
    }

    private static int CountInputFields(string text)
    {
        var count = 1;

        foreach (var ch in text)
        {
            if (ch == ',')
            {
                count++;
            }
        }

        return count;
    }

    private static ReadOnlySpan<char> NormalizeInputField(ReadOnlySpan<char> text, bool trim)
    {
        if (!trim)
        {
            return text;
        }

        var start = 0;
        var end = text.Length - 1;

        while (start <= end && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        while (end >= start && char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        return start > end
            ? ReadOnlySpan<char>.Empty
            : text.Slice(start, end - start + 1);
    }

    private void ReportInputConversionFailure(TypeSymbol type, string text, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            InputConversionCode,
            $"Невозможно преобразовать ввод '{text}' к типу {type.Name}.",
            location);
    }

    private void ReportInputValueCountFailure(int expectedCount, int actualCount, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            InputConversionCode,
            $"Ожидалось {expectedCount} значений INPUT, но получено {actualCount}.",
            location);
    }

    private readonly record struct InputTarget(
        int Kind,
        int Slot,
        string Name,
        TypeSymbol Type,
        int? FixedStringLength)
    {
        public bool IsArray => Type is ArrayTypeSymbol;
    }

    private readonly record struct InputInstructionPlan(
        string? PromptText,
        InputTarget[] Targets,
        string? ArrayTargetName);
}
