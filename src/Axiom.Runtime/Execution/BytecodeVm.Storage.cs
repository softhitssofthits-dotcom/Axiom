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
    private RuntimeValue PopValue(BytecodeCallFrame frame, int instructionIndex)
    {
        if (frame.EvaluationStack.Count == 0)
        {
            ReportInvalidRuntimeState(
                $"Стек вычисления опустел в процедуре '{frame.Routine.Name}'.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        return frame.EvaluationStack.PopUnchecked();
    }

    private RuntimeValue ReadConstant(int constantIndex)
    {
        if (constantIndex < 0 || constantIndex >= _constantValues.Length)
        {
            ReportInvalidRuntimeState($"Не найдена константа с индексом {constantIndex}.", FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        var value = _constantValues[constantIndex];
        if (value is not null)
        {
            return value;
        }

        var constant = _module.ConstantPool[constantIndex];
        return ReportInvalidRuntimeState<RuntimeValue>(
            $"Константа вида '{constant.Kind}' не поддерживается Bytecode VM.",
            FallbackRuntimeLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RuntimeValue ReadConstantUnchecked(int constantIndex)
    {
        return _constantValues[constantIndex]!;
    }

    private static RuntimeValue? TryCreateRuntimeConstant(BytecodeConstant constant)
    {
        return constant.Kind switch
        {
            BytecodeConstantKind.Integer => RuntimeValue.FromInteger((int)constant.Value!),
            BytecodeConstantKind.Long => RuntimeValue.FromLong((long)constant.Value!),
            BytecodeConstantKind.Double => RuntimeValue.FromDouble((double)constant.Value!),
            BytecodeConstantKind.Decimal => RuntimeValue.FromDecimal((decimal)constant.Value!),
            BytecodeConstantKind.String => RuntimeValue.FromString((string)constant.Value!),
            BytecodeConstantKind.Boolean => RuntimeValue.FromBoolean((bool)constant.Value!),
            BytecodeConstantKind.Null => RuntimeValue.Void,
            _ => null
        };
    }

    private static RoutineFrameTemplate CreateRoutineFrameTemplate(BytecodeRoutine routine)
    {
        if (routine.LocalSlotCount == 0)
        {
            return new RoutineFrameTemplate(Array.Empty<VmValue>(), Array.Empty<int?>());
        }

        var defaultLocals = new VmValue[routine.LocalSlotCount];
        var localFixedStringLengths = new int?[routine.LocalSlotCount];

        for (var index = 0; index < defaultLocals.Length; index++)
        {
            var variable = index < routine.LocalVariables.Count ? routine.LocalVariables[index] : null;

            // Запись создаётся инструкцией объявления в момент исполнения: заранее
            // подготовленный шаблон кадра не должен выделять поля-массивы даже
            // для процедур, которые так и не будут вызваны.
            defaultLocals[index] = variable is null || variable.Type is UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol
                ? VmValue.FromObject(RuntimeValue.Void)
                : VmValue.FromRuntime(RuntimeValue.Default(variable.Type, variable.FixedStringLength));
            localFixedStringLengths[index] = variable?.FixedStringLength;
        }

        return new RoutineFrameTemplate(defaultLocals, localFixedStringLengths);
    }

    private string ReadDebugName(int constantIndex)
    {
        if (constantIndex < 0 || constantIndex >= _module.ConstantPool.Count)
        {
            return "<array>";
        }

        var constant = _module.ConstantPool[constantIndex];
        return constant.Kind == BytecodeConstantKind.String
            ? (string)constant.Value!
            : "<array>";
    }

    private RuntimeValue LoadGlobal(int slot)
    {
        if ((uint)slot < (uint)_globalValuesBySlot.Length && _hasGlobalSlot[slot])
        {
            return EnsureGlobalValueCreated(slot);
        }

        return ReportInvalidRuntimeState<RuntimeValue>($"Не найден глобальный слот {slot}.", FallbackRuntimeLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RuntimeValue LoadGlobalUnchecked(int slot)
    {
        var value = _globalValuesBySlot[slot];
        return value.Kind == VmValueKind.Empty ? EnsureGlobalValueCreated(slot) : value.Materialize();
    }

    /// <summary>
    /// Помещает значение глобального слота на стек без материализации:
    /// числа и логические значения идут напрямую, записи TYPE создаются
    /// при первом чтении, накопитель строк снимается копией.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushGlobalValueOntoEvaluationStack(BytecodeEvaluationStack stack, int slot)
    {
        var global = _globalValuesBySlot[slot];
        if (global.Kind == VmValueKind.Object)
        {
            var globalValue = global.Object!;
            if (globalValue.Type == TypeSymbol.Void)
            {
                stack.PushUnchecked(EnsureGlobalValueCreated(slot));
                return;
            }

            // Накопитель строк в слоте живой: при помещении на стек он
            // снимается, чтобы поздние дописывания не меняли уже вычисленное.
            if (globalValue.RequiresCopyForStorage)
            {
                stack.PushUnchecked(globalValue);
                return;
            }
        }
        else if (global.Kind == VmValueKind.Empty)
        {
            stack.PushUnchecked(EnsureGlobalValueCreated(slot));
            return;
        }

        stack.PushVmValue(global);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryStoreGlobalImmediateInteger(int slot, int value)
    {
        if ((uint)slot >= (uint)_globalValuesBySlot.Length
            || !_hasGlobalSlot[slot]
            || !_globalMetadata.TryGetValue(slot, out var global)
            || global.Type != TypeSymbol.Integer)
            return false;

        _globalValuesBySlot[slot] = VmValue.FromInteger(value);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryStoreGlobalImmediateLong(int slot, long value)
    {
        if ((uint)slot >= (uint)_globalValuesBySlot.Length
            || !_hasGlobalSlot[slot]
            || !_globalMetadata.TryGetValue(slot, out var global)
            || global.Type != TypeSymbol.Long)
            return false;

        _globalValuesBySlot[slot] = VmValue.FromLong(value);
        return true;
    }

    /// <summary>
    /// Записывает непосредственный DOUBLE в глобальный слот без материализации
    /// значения. Несовпадение объявленного типа и нечисловые слоты дают отказ:
    /// вызывающий код использует прежний общий путь с прежними правилами
    /// копирования.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryStoreGlobalImmediateDouble(int slot, double value)
    {
        if ((uint)slot >= (uint)_globalValuesBySlot.Length
            || !_hasGlobalSlot[slot]
            || !_globalMetadata.TryGetValue(slot, out var global)
            || global.Type != TypeSymbol.Double)
            return false;

        _globalValuesBySlot[slot] = VmValue.FromDouble(value);
        return true;
    }

    /// <summary>
    /// Запись глобальной переменной создаётся при первом чтении, если инструкция
    /// объявления ещё не исполнялась. Правила наблюдаемого поведения сохраняются:
    /// до DIM глобальная запись содержит значения по умолчанию, а выделение
    /// фиксированных полей не выполняется заранее для всей программы.
    /// </summary>
    private RuntimeValue EnsureGlobalValueCreated(int slot)
    {
        if (_globalMetadata.TryGetValue(slot, out var global)
            && global.Type is UserDefinedTypeSymbol recordType)
        {
            var created = RuntimeValue.Default(recordType, global.FixedStringLength);
            _globalValuesBySlot[slot] = VmValue.FromRuntime(created);
            return created;
        }

        return RuntimeValue.Void;
    }

    private RuntimeValue LoadFunctionValue(BytecodeCallFrame frame, int routineId, int instructionIndex)
    {
        if (frame.TryGetCachedFunctionValue(routineId, out var cachedFunctionValue))
        {
            return cachedFunctionValue;
        }

        if (!_routines.TryGetValue(routineId, out var routine) || routine.Symbol is null)
        {
            ReportInvalidRuntimeState(
                $"Не найдена процедура для значения функции с routine-id {routineId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        var captures = ResolveCapturedSlots(frame, routine, instructionIndex);
        var functionValue = RuntimeValue.FromBytecodeFunction(routine, captures);
        frame.CacheFunctionValue(routineId, functionValue);
        return functionValue;
    }
    private RuntimeValue LoadBuiltinFunctionValue(BytecodeCallFrame frame, int builtinId, int instructionIndex)
    {
        if (!_builtins.TryGetValue(builtinId, out var builtin) || builtin.Function is null)
        {
            ReportInvalidRuntimeState(
                $"Не найдена встроенная функция для значения функции с built-in-id {builtinId}.",
                ResolveLocation(frame.Routine.Id, instructionIndex));
            throw new RuntimeFailureException();
        }

        return RuntimeValue.FromBytecodeBuiltinFunction(builtin.Function, builtin.Id);
    }

    private void StoreGlobal(int slot, RuntimeValue value)
    {
        if ((uint)slot >= (uint)_globalValuesBySlot.Length || !_hasGlobalSlot[slot])
        {
            ReportInvalidRuntimeState($"Не найден глобальный слот {slot}.", FallbackRuntimeLocation);
            throw new RuntimeFailureException();
        }

        var fixedStringLength = GetGlobalFixedStringLength(slot);
        _globalValuesBySlot[slot] = VmValue.FromRuntime(
            fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
                ? value
                : BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StoreGlobalUnchecked(int slot, RuntimeValue value)
    {
        var fixedStringLength = _globalFixedStringLengths[slot];
        _globalValuesBySlot[slot] = VmValue.FromRuntime(
            fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
                ? value
                : BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StoreAppendStringGlobalValue(int slot, RuntimeValue value)
    {
        var fixedStringLength = _globalFixedStringLengths[slot];
        _globalValuesBySlot[slot] = VmValue.FromRuntime(
            fixedStringLength is null
                ? value
                : BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength));
    }

    private static RuntimeValue LoadLocal(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.LocalCount)
        {
            throw CreateInvalidSlotException(frame, "локальный", slot, instructionIndex);
        }

        return frame.LoadLocalValue(slot);
    }

    private static void StoreLocal(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.LocalCount)
        {
            throw CreateInvalidSlotException(frame, "локальный", slot, instructionIndex);
        }

        frame.StoreLocalValue(slot, value);
    }

    private static RuntimeValue LoadArgument(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Arguments.Length)
        {
            throw CreateInvalidSlotException(frame, "аргументный", slot, instructionIndex);
        }

        return frame.Arguments[slot].Value;
    }

    private static void StoreArgument(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Arguments.Length)
        {
            throw CreateInvalidSlotException(frame, "аргументный", slot, instructionIndex);
        }

        frame.Arguments[slot].Store(value);
    }

    private static RuntimeValue LoadCapture(BytecodeCallFrame frame, int slot, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Captures.Length)
        {
            throw CreateInvalidSlotException(frame, "захваченному", slot, instructionIndex);
        }

        return frame.Captures[slot].Value;
    }

    private static void StoreCapture(BytecodeCallFrame frame, int slot, RuntimeValue value, int instructionIndex)
    {
        if (slot < 0 || slot >= frame.Captures.Length)
        {
            throw CreateInvalidSlotException(frame, "захваченному", slot, instructionIndex);
        }

        frame.Captures[slot].Store(value);
    }

    private int? GetGlobalFixedStringLength(int slot)
    {
        return (uint)slot < (uint)_globalFixedStringLengths.Length && _hasGlobalSlot[slot]
            ? _globalFixedStringLengths[slot]
            : null;
    }

    private static InvalidOperationException CreateInvalidSlotException(
        BytecodeCallFrame frame,
        string kind,
        int slot,
        int instructionIndex)
    {
        return new InvalidOperationException(
            $"Процедура '{frame.Routine.Name}' обратилась к несуществующему {kind} слоту {slot} на инструкции {instructionIndex}.");
    }

}
