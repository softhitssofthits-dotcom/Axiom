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
        var valueCount = instruction.GetOperand(0);
        var promptText = ReadInputPrompt(instruction.GetOperand(1));
        var targets = new InputTarget[valueCount];

        for (var index = 0; index < valueCount; index++)
        {
            var operandOffset = 2 + index * 2;
            targets[index] = ResolveInputTarget(
                frame,
                instruction.GetOperand(operandOffset),
                instruction.GetOperand(operandOffset + 1),
                instructionIndex);
        }

        if (targets.Any(static target => target.IsArray))
        {
            var arrayTarget = targets.First(static target => target.IsArray);
            ReportInvalidRuntimeState($"INPUT does not support array '{arrayTarget.Name}'.", location);
            throw new RuntimeFailureException();
        }

        var text = _host.ReadInput(new InputRequest(promptText, valueCount));
        InvalidateReverseExecutionHistory();

        if (!TryParseInputValues(targets, text, location, out var values))
        {
            throw new RuntimeFailureException();
        }

        for (var index = 0; index < targets.Length; index++)
        {
            StoreInputValue(frame, targets[index], values[index], instructionIndex);
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

    private InputTarget ResolveInputTarget(BytecodeCallFrame frame, int targetKind, int slot, int instructionIndex)
    {
        return targetKind switch
        {
            0 => ResolveGlobalInputTarget(slot, instructionIndex),
            1 => ResolveLocalInputTarget(frame, slot, instructionIndex),
            2 => ResolveArgumentInputTarget(frame, slot, instructionIndex),
            3 => ResolveCaptureInputTarget(frame, slot, instructionIndex),
            _ => ReportInvalidRuntimeState<InputTarget>(
                $"Bytecode VM получила неизвестный вид цели INPUT {targetKind}.",
                ResolveLocation(frame.Routine.Id, instructionIndex))
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

    private static InputTarget ResolveLocalInputTarget(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Locals.Length || slot >= frame.Routine.LocalVariables.Count)
        {
            throw CreateInvalidSlotException(frame, "локальный", slot, instructionIndex);
        }

        var variable = frame.Routine.LocalVariables[slot];
        return new InputTarget(1, slot, variable.Name, variable.Type, variable.FixedStringLength);
    }

    private static InputTarget ResolveArgumentInputTarget(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Arguments.Length || slot >= frame.Routine.ParameterVariables.Count)
        {
            throw CreateInvalidSlotException(frame, "аргументный", slot, instructionIndex);
        }

        var variable = frame.Routine.ParameterVariables[slot];
        return new InputTarget(2, slot, variable.Name, variable.Type, variable.FixedStringLength);
    }

    private static InputTarget ResolveCaptureInputTarget(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Captures.Length || slot >= frame.Routine.CapturedVariables.Count)
        {
            throw CreateInvalidSlotException(frame, "захваченный", slot, instructionIndex);
        }

        var variable = frame.Routine.CapturedVariables[slot];
        return new InputTarget(3, slot, variable.Name, variable.Type, variable.FixedStringLength);
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

    private RuntimeValue? TryParseInput(TypeSymbol type, string text, TextLocation location)
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
            return RuntimeValue.FromString(text);
        }

        if (type == TypeSymbol.Boolean)
        {
            if (string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeValue.FromBoolean(true);
            }

            if (string.Equals(text, "FALSE", StringComparison.OrdinalIgnoreCase))
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

        var fields = targets.Count == 1
            ? [text]
            : text.Split(',');

        if (fields.Length != targets.Count)
        {
            ReportInputValueCountFailure(targets.Count, fields.Length, location);
            return false;
        }

        var parsedValues = new RuntimeValue[targets.Count];

        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var field = NormalizeInputField(fields[index], targets.Count > 1);
            var parsedValue = TryParseInput(target.Type, field, location);

            if (parsedValue is null)
            {
                ReportInputConversionFailure(target.Type, field, location);
                return false;
            }

            parsedValues[index] = parsedValue;
        }

        values = parsedValues;
        return true;
    }

    private static string NormalizeInputField(string text, bool trim)
    {
        return trim ? text.Trim() : text;
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
}
