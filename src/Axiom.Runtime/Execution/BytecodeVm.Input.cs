using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
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

        var request = new InputRequest(plan.PromptText, plan.Targets.Length);
        var text = _host.IsScreenOutputActive ? _host.ReadGraphicsInput(request) : _host.ReadInput(request);
        // Остановка во время ожидания не является вводом пустой строки.
        // Не преобразовываем значения и не записываем цели отменённого INPUT.
        if (CheckCancellationRequested())
        {
            return;
        }
        InvalidateReverseExecutionHistory();

        if (!TryParseAndStoreInputValues(frame, plan.Targets, text, location, instructionIndex))
        {
            throw new RuntimeFailureException();
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
        var result = BuiltinIOHelpers.TryParseInputField(type, text);

        // Для поддержанного типа null означает отказ разбора, а не отсутствие
        // поддержки: вызывающий код сообщает MB3003 с текстом значения.
        if (IsSupportedInputType(type))
        {
            return result;
        }

        return ReportInvalidRuntimeState<RuntimeValue?>($"INPUT не поддерживает тип {type.Name}.", location);
    }

    private static bool IsSupportedInputType(TypeSymbol type)
    {
        return type == TypeSymbol.String || type == TypeSymbol.Boolean
            || type == TypeSymbol.Integer || type == TypeSymbol.Long
            || type == TypeSymbol.Double || type == TypeSymbol.Decimal
            || type == TypeSymbol.Date || type == TypeSymbol.Time
            || type == TypeSymbol.DateTime || type == TypeSymbol.UtcDateTime;
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

        if (!TrySplitInputFields(text, targets.Count, location, out var fields))
        {
            return false;
        }

        var parsedValues = new RuntimeValue[targets.Count];

        for (var index = 0; index < targets.Count; index++)
        {
            var field = NormalizeInputField(fields[index], trim: true);
            var parsedValue = TryParseInput(targets[index].Type, field, location);

            if (parsedValue is null)
            {
                ReportInputConversionFailure(targets[index].Type, field.ToString(), location);
                return false;
            }

            parsedValues[index] = parsedValue;
        }

        values = parsedValues;
        return true;
    }

    // Один разбор полей для всех многозначных форм INPUT: две и три цели больше
    // не имеют собственной логики запятых, а сообщение о числе полей считается
    // одинаково.
    private bool TrySplitInputFields(string text, int expectedCount, TextLocation location, out string[] fields)
    {
        fields = text.Split(',', StringSplitOptions.None);

        if (fields.Length != expectedCount)
        {
            ReportInputValueCountFailure(expectedCount, fields.Length, location);
            return false;
        }

        return true;
    }

    private bool TryParseAndStoreInputValues(
        BytecodeCallFrame frame,
        InputTarget[] targets,
        string text,
        TextLocation location,
        int instructionIndex)
    {
        switch (targets.Length)
        {
            case 0:
                return true;

            case 1:
            {
                var parsedValue = TryParseInput(targets[0].Type, text.AsSpan(), location);
                if (parsedValue is null)
                {
                    ReportInputConversionFailure(targets[0].Type, text, location);
                    return false;
                }

                StoreInputValue(frame, targets[0], parsedValue, instructionIndex);
                return true;
            }

            case 2:
            case 3:
            default:
                return TryParseAndStoreMultipleInputValues(frame, targets, text, location, instructionIndex);
        }
    }

    private bool TryParseAndStoreMultipleInputValues(
        BytecodeCallFrame frame,
        InputTarget[] targets,
        string text,
        TextLocation location,
        int instructionIndex)
    {
        if (!TrySplitInputFields(text, targets.Length, location, out var fields))
        {
            return false;
        }

        var values = new RuntimeValue[targets.Length];

        for (var index = 0; index < targets.Length; index++)
        {
            var field = NormalizeInputField(fields[index], trim: true);
            var parsedValue = TryParseInput(targets[index].Type, field, location);

            if (parsedValue is null)
            {
                ReportInputConversionFailure(targets[index].Type, field.ToString(), location);
                return false;
            }

            values[index] = parsedValue;
        }

        for (var index = 0; index < targets.Length; index++)
        {
            StoreInputValue(frame, targets[index], values[index], instructionIndex);
        }

        return true;
    }
    private static int CountInputFields(string text)
    {
        return BuiltinIOHelpers.CountInputFields(text);
    }

    private static ReadOnlySpan<char> NormalizeInputField(ReadOnlySpan<char> text, bool trim)
    {
        return BuiltinIOHelpers.NormalizeInputField(text, trim);
    }

    private void ReportInputConversionFailure(TypeSymbol type, string text, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            DiagnosticCodes.InputConversion,
            $"Невозможно преобразовать ввод '{text}' к типу {type.Name}.",
            location);
    }

    private void ReportInputValueCountFailure(int expectedCount, int actualCount, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            DiagnosticCodes.InputConversion,
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
