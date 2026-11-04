using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;
using System.Runtime.CompilerServices;

namespace Axiom.Runtime;

internal sealed class BytecodeCallFrame
{
    private IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot>? _visibleVariables;
    private Dictionary<int, RuntimeValue>? _cachedFunctionValuesByRoutineId;
    private Dictionary<int, BytecodeVariableSlot[]>? _cachedCapturedSlotsByRoutineId;
    private readonly List<BytecodeErrorHandlerFrame> _errorHandlers = [];
    // Локали хранятся как тегированные значения: числовые виды распакованы.
    // Общие ячейки для BYREF и замыканий по-прежнему хранят RuntimeValue.
    private readonly VmValue[] _locals;
    private readonly int?[] _localFixedStringLengths;
    private BytecodeVariableSlot?[]? _sharedLocals;
    private bool _hasSharedLocals;

    public BytecodeCallFrame(
        BytecodeRoutine routine,
        BytecodeVariableSlot[] arguments,
        VmValue[] locals,
        int?[] localFixedStringLengths,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        int evaluationStackCapacity = 16,
        VmInstruction[]? decodedInstructions = null)
    {
        ArgumentNullException.ThrowIfNull(routine);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(locals);
        ArgumentNullException.ThrowIfNull(localFixedStringLengths);
        ArgumentNullException.ThrowIfNull(captures);

        Routine = routine;
        DecodedInstructions = decodedInstructions ?? DecodeRoutineInstructions(routine);
        Arguments = arguments;
        _locals = locals;
        _localFixedStringLengths = localFixedStringLengths;
        _sharedLocals = locals.Length == 0 ? null : new BytecodeVariableSlot?[locals.Length];
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        EvaluationStack = new BytecodeEvaluationStack(evaluationStackCapacity);
    }

    public BytecodeRoutine Routine { get; }

    /// <summary>Раскодированный поток инструкций подпрограммы; источником истины остаётся модуль.</summary>
    public VmInstruction[] DecodedInstructions { get; }

    private static VmInstruction[] DecodeRoutineInstructions(BytecodeRoutine routine)
    {
        var source = routine.InstructionArray;
        var decoded = new VmInstruction[source.Length];
        for (var index = 0; index < source.Length; index++)
        {
            decoded[index] = new VmInstruction(source[index]);
        }

        return decoded;
    }

    public BytecodeVariableSlot[] Arguments { get; }

    public BytecodeVariableSlot[] Captures { get; private set; }

    public void SetCapturedSlots(BytecodeVariableSlot[] captures)
    {
        ArgumentNullException.ThrowIfNull(captures);
        Captures = captures;
        _cachedCapturedSlotsByRoutineId?.Clear();
        _cachedFunctionValuesByRoutineId?.Clear();
        _visibleVariables = null;
    }

    public TextLocation CallSiteLocation { get; private set; }

    public BytecodeEvaluationStack EvaluationStack { get; }

    public Func<RuntimeValue, VmCallbackStep>? CallbackContinuation { get; set; }

    public Action<RuntimeValue>? CallbackCompletion { get; set; }

    public bool IsInputHandlerFrame { get; set; }

    public bool DiscardReturnValue { get; set; }

    public bool HasCallbackContinuation => CallbackContinuation is not null;

    public void ClearCallbackState()
    {
        CallbackContinuation = null;
        CallbackCompletion = null;
        IsInputHandlerFrame = false;
        DiscardReturnValue = false;
    }

    /// <summary>
    /// Освобождает ссылки завершённого кадра до помещения в пул. Значения
    /// захваченных ячеек остаются живыми через сами слоты замыканий.
    /// </summary>
    public void ReleaseReferences()
    {
        ClearCallbackState();
        _visibleVariables = null;
        _cachedFunctionValuesByRoutineId?.Clear();
        _cachedCapturedSlotsByRoutineId?.Clear();
        EvaluationStack.Clear();
        _errorHandlers.Clear();
        Captures = Array.Empty<BytecodeVariableSlot>();

        if (_locals.Length > 0)
        {
            Array.Clear(_locals, 0, _locals.Length);
        }

        if (_sharedLocals is not null)
        {
            Array.Clear(_sharedLocals, 0, _sharedLocals.Length);
        }

        _hasSharedLocals = false;

        for (var index = 0; index < Arguments.Length; index++)
        {
            Arguments[index] = null!;
        }
    }

    public int LocalCount => _locals.Length;

    public IReadOnlyDictionary<VariableSymbol, BytecodeVariableSlot> VisibleVariables =>
        _visibleVariables ??= CreateVisibleVariables(this);

    public int InstructionPointer { get; set; }

    public int ErrorHandlerCount => _errorHandlers.Count;

    public void Reset(
        BytecodeVariableSlot[] arguments,
        BytecodeVariableSlot[] captures,
        TextLocation callSiteLocation,
        VmValue[] defaultLocals)
    {
        Captures = captures;
        CallSiteLocation = callSiteLocation;
        InstructionPointer = 0;
        ClearCallbackState();
        _visibleVariables = null;
        _cachedFunctionValuesByRoutineId?.Clear();
        _cachedCapturedSlotsByRoutineId?.Clear();
        EvaluationStack.Clear();
        _errorHandlers.Clear();

        if (_locals.Length > 0)
        {
            Array.Copy(defaultLocals, _locals, _locals.Length);

            if (_hasSharedLocals && _sharedLocals is not null)
            {
                Array.Clear(_sharedLocals, 0, _sharedLocals.Length);
            }
        }

        _hasSharedLocals = false;

        for (var index = 0; index < Arguments.Length; index++)
        {
            Arguments[index] = arguments[index];
        }
    }

    public void PushErrorHandler(BytecodeErrorHandlerFrame handler)
    {
        _errorHandlers.Add(handler);
    }

    public BytecodeErrorHandlerFrame? PeekErrorHandler()
    {
        return _errorHandlers.Count == 0 ? null : _errorHandlers[^1];
    }

    public bool TryPopErrorHandler(out BytecodeErrorHandlerFrame handler)
    {
        if (_errorHandlers.Count == 0)
        {
            handler = null!;
            return false;
        }

        var index = _errorHandlers.Count - 1;
        handler = _errorHandlers[index];
        _errorHandlers.RemoveAt(index);
        return true;
    }

    public IReadOnlyList<BytecodeErrorHandlerFrame> ErrorHandlers => _errorHandlers;

    public bool TryResolveVisibleVariable(VariableSymbol variable, out BytecodeVariableSlot slot)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return ((Dictionary<VariableSymbol, BytecodeVariableSlot>)VisibleVariables).TryGetValue(variable, out slot!);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RuntimeValue LoadLocalValue(int slot)
    {
        if (!_hasSharedLocals)
        {
            var local = _locals[slot];
            if (local.Kind != VmValueKind.Object || local.Object!.Type != TypeSymbol.Void)
            {
                return local.Materialize();
            }

            return EnsureDefaultLocalValue(slot);
        }

        var sharedLocals = _sharedLocals!;
        if (sharedLocals[slot] is { } sharedValue)
        {
            return sharedValue.Value.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : sharedValue.Value;
        }

        var unshared = _locals[slot];
        return unshared.Kind == VmValueKind.Object && unshared.Object!.Type == TypeSymbol.Void
            ? EnsureDefaultLocalValue(slot)
            : unshared.Materialize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushLocalValueOntoEvaluationStack(int slot)
    {
        if (!_hasSharedLocals)
        {
            PushUnsharedLocalOntoEvaluationStack(slot);
            return;
        }

        var sharedLocals = _sharedLocals!;
        if (sharedLocals[slot] is { } sharedValue)
        {
            var value = sharedValue.Value;
            EvaluationStack.PushUnchecked(value.Type == TypeSymbol.Void ? EnsureDefaultLocalValue(slot) : value);
            return;
        }

        PushUnsharedLocalOntoEvaluationStack(slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PushUnsharedLocalOntoEvaluationStack(int slot)
    {
        var local = _locals[slot];
        if (local.Kind == VmValueKind.Object)
        {
            var localValue = local.Object!;
            if (localValue.Type == TypeSymbol.Void)
            {
                EvaluationStack.PushUnchecked(EnsureDefaultLocalValue(slot));
                return;
            }

            // Накопитель строк в слоте живой: при помещении на стек он
            // снимается, чтобы поздние дописывания не меняли уже вычисленное.
            if (localValue.RequiresCopyForStorage)
            {
                EvaluationStack.PushUnchecked(localValue);
                return;
            }
        }

        EvaluationStack.PushVmValue(local);
    }

    /// <summary>
    /// Возвращает текущее значение слота без изменения состояния. Нужен снимкам
    /// состояния: наблюдение не должно менять исполняемое состояние.
    /// </summary>
    public RuntimeValue PeekLocalValue(int slot)
    {
        if (!_hasSharedLocals)
        {
            return ReadUnsharedLocal(slot);
        }

        var sharedLocals = _sharedLocals!;
        return sharedLocals[slot]?.Value ?? ReadUnsharedLocal(slot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RuntimeValue ReadUnsharedLocal(int slot)
    {
        return _locals[slot].Materialize();
    }

    public bool TryStoreImmediateInteger(int slot, int value)
    {
        if ((uint)slot >= (uint)Routine.LocalVariables.Count
            || Routine.LocalVariables[slot].Type != TypeSymbol.Integer
            || (_hasSharedLocals && _sharedLocals![slot] is not null))
            return false;

        _locals[slot] = VmValue.FromInteger(value);
        return true;
    }

    /// <summary>
    /// Читает значение локального слота как int без создания RuntimeValue.
    /// Общие ячейки (BYREF/замыкания) и прочие типы дают отказ, и вызывающий
    /// код использует прежний общий путь.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadLocalInteger(int slot, out int value)
    {
        value = 0;
        if ((uint)slot >= (uint)_locals.Length
            || (_hasSharedLocals && _sharedLocals![slot] is not null))
        {
            return false;
        }

        var local = _locals[slot];
        if (local.Kind != VmValueKind.Integer)
        {
            return false;
        }

        value = local.Integer;
        return true;
    }

    public bool TryStoreImmediateLong(int slot, long value)
    {
        if ((uint)slot >= (uint)Routine.LocalVariables.Count
            || Routine.LocalVariables[slot].Type != TypeSymbol.Long
            || (_hasSharedLocals && _sharedLocals![slot] is not null))
            return false;

        _locals[slot] = VmValue.FromLong(value);
        return true;
    }

    /// <summary>
    /// Читает значение локального слота как long без создания RuntimeValue.
    /// Общие ячейки и прочие типы дают отказ. INTEGER не расширяется:
    /// смешанные типы обязан обрабатывать прежний общий путь.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryReadLocalLong(int slot, out long value)
    {
        value = 0;
        if ((uint)slot >= (uint)_locals.Length
            || (_hasSharedLocals && _sharedLocals![slot] is not null))
        {
            return false;
        }

        var local = _locals[slot];
        if (local.Kind != VmValueKind.Long)
        {
            return false;
        }

        value = local.Long;
        return true;
    }

    /// <summary>
    /// Создаёт запись локальной переменной при первом чтении, если объявление
    /// ещё не исполнялось. Служебные слоты без объявленного типа остаются VOID.
    /// </summary>
    private RuntimeValue EnsureDefaultLocalValue(int slot)
    {
        var variables = Routine.LocalVariables;
        if (slot >= variables.Count || variables[slot].Type is not (UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol))
        {
            return RuntimeValue.Void;
        }

        var created = RuntimeValue.Default(variables[slot].Type, _localFixedStringLengths[slot]);

        if (_hasSharedLocals && _sharedLocals![slot] is { } sharedSlot)
        {
            sharedSlot.Value = created;
        }
        else
        {
            _locals[slot] = VmValue.FromRuntime(created);
        }

        return created;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreLocalValue(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            var fixedStringLength = _localFixedStringLengths[slot];
            _locals[slot] = fixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
                ? VmValue.FromRuntime(value)
                : VmValue.FromObject(BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength));
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.Store(value);
            if (sharedSlot.IsForEachItemPending)
            {
                sharedSlot.IsForEachItem = true;
                sharedSlot.IsForEachItemPending = false;
                InvalidateLocalVariableBindings();
            }

            return;
        }

        var localFixedStringLength = _localFixedStringLengths[slot];
        _locals[slot] = localFixedStringLength is null && value.Type is not UserDefinedTypeSymbol && !value.RequiresCopyForStorage
            ? VmValue.FromRuntime(value)
            : VmValue.FromObject(BytecodeVariableSlot.NormalizeForStorage(value, localFixedStringLength));
    }

    // Новая плоская запись ещё нигде не опубликована. Пропускаем копирование
    // только при условиях, при которых плоский копировщик не опрашивает отмену
    // и не нормализует поля; общий или связанный слот остаётся на обычном пути.
    internal bool TryAdoptFreshFlatRecord(int slot, RuntimeValue value)
    {
        if (_localFixedStringLengths[slot] is not null
            || (_hasSharedLocals && _sharedLocals![slot] is not null)
            || value.Type is not UserDefinedTypeSymbol type
            || type.Fields.Count >= 4095)
        {
            return false;
        }

        var fields = type.Fields;
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            if (field.Ordinal != i || field.FixedStringLength is not null
                || !IsFreshRecordScalar(field.Type))
            {
                return false;
            }
        }

        _locals[slot] = VmValue.FromRuntime(value);
        return true;
    }

    private static bool IsFreshRecordScalar(TypeSymbol type)
        => ReferenceEquals(type, TypeSymbol.Integer)
           || ReferenceEquals(type, TypeSymbol.Long)
           || ReferenceEquals(type, TypeSymbol.Double)
           || ReferenceEquals(type, TypeSymbol.Decimal)
           || ReferenceEquals(type, TypeSymbol.String)
           || ReferenceEquals(type, TypeSymbol.Boolean)
           || ReferenceEquals(type, TypeSymbol.Date)
           || ReferenceEquals(type, TypeSymbol.Time)
           || ReferenceEquals(type, TypeSymbol.DateTime)
           || ReferenceEquals(type, TypeSymbol.UtcDateTime);

    public void FreshLocal(int slot)
    {
        var value = RuntimeValue.Default(Routine.LocalVariables[slot].Type, _localFixedStringLengths[slot]);
        ReplaceLocalSlot(slot, BytecodeVariableSlot.Adopt(value, _localFixedStringLengths[slot], isForEachItemPending: true));
    }

    public void ClearForEachItem(int slot)
    {
        var localSlot = _sharedLocals?[slot];
        if (localSlot is not null)
        {
            localSlot.IsForEachItem = false;
            localSlot.IsForEachItemPending = false;
            InvalidateLocalVariableBindings();
        }
    }

    public void ReplaceLocalSlot(int slot, BytecodeVariableSlot replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        _sharedLocals ??= new BytecodeVariableSlot?[_locals.Length];
        _sharedLocals[slot] = replacement;
        _locals[slot] = VmValue.FromObject(RuntimeValue.Void);
        _hasSharedLocals = true;
        InvalidateLocalVariableBindings();
    }

    public BytecodeVariableSlot? GetExistingLocalSlot(int slot) => _sharedLocals?[slot];

    private void InvalidateLocalVariableBindings()
    {
        _visibleVariables = null;
        _cachedFunctionValuesByRoutineId?.Clear();
        _cachedCapturedSlotsByRoutineId?.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreAppendStringLocalValue(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            var fixedStringLength = _localFixedStringLengths[slot];
            _locals[slot] = fixedStringLength is null
                ? VmValue.FromRuntime(value)
                : VmValue.FromObject(BytecodeVariableSlot.NormalizeForStorage(value, fixedStringLength));
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.StoreAppendStringValue(value);
            return;
        }

        var localFixedStringLength = _localFixedStringLengths[slot];
        _locals[slot] = localFixedStringLength is null
            ? VmValue.FromRuntime(value)
            : VmValue.FromObject(BytecodeVariableSlot.NormalizeForStorage(value, localFixedStringLength));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LoadForLoopLocalValues(int currentSlot, int endSlot, int stepSlot, out RuntimeValue current, out RuntimeValue end, out RuntimeValue step)
    {
        if (!_hasSharedLocals)
        {
            current = ReadUnsharedLocal(currentSlot);
            end = ReadUnsharedLocal(endSlot);
            step = ReadUnsharedLocal(stepSlot);
            return;
        }

        var sharedLocals = _sharedLocals!;
        current = sharedLocals[currentSlot]?.Value ?? ReadUnsharedLocal(currentSlot);
        end = sharedLocals[endSlot]?.Value ?? ReadUnsharedLocal(endSlot);
        step = sharedLocals[stepSlot]?.Value ?? ReadUnsharedLocal(stepSlot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LoadForLoopAdvanceValues(int currentSlot, int stepSlot, out RuntimeValue current, out RuntimeValue step)
    {
        if (!_hasSharedLocals)
        {
            current = ReadUnsharedLocal(currentSlot);
            step = ReadUnsharedLocal(stepSlot);
            return;
        }

        var sharedLocals = _sharedLocals!;
        current = sharedLocals[currentSlot]?.Value ?? ReadUnsharedLocal(currentSlot);
        step = sharedLocals[stepSlot]?.Value ?? ReadUnsharedLocal(stepSlot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StoreNumericLocalValueUnchecked(int slot, RuntimeValue value)
    {
        if (!_hasSharedLocals)
        {
            _locals[slot] = VmValue.FromRuntime(value);
            return;
        }

        var sharedLocals = _sharedLocals!;
        var sharedSlot = sharedLocals[slot];
        if (sharedSlot is not null)
        {
            sharedSlot.Value = value;
            return;
        }

        _locals[slot] = VmValue.FromRuntime(value);
    }

    public BytecodeVariableSlot GetOrCreateLocalSlot(int slot)
    {
        _sharedLocals ??= new BytecodeVariableSlot?[_locals.Length];

        var sharedSlot = _sharedLocals[slot];
        if (sharedSlot is not null)
        {
            return sharedSlot;
        }

        sharedSlot = BytecodeVariableSlot.Adopt(ReadUnsharedLocal(slot), _localFixedStringLengths[slot]);
        _sharedLocals[slot] = sharedSlot;
        _hasSharedLocals = true;
        return sharedSlot;
    }

    public int? GetLocalFixedStringLength(int slot)
    {
        return _localFixedStringLengths[slot];
    }

    public bool TryGetCachedFunctionValue(int routineId, out RuntimeValue value)
    {
        if (_cachedFunctionValuesByRoutineId is not null
            && _cachedFunctionValuesByRoutineId.TryGetValue(routineId, out value!))
        {
            return true;
        }

        value = RuntimeValue.Void;
        return false;
    }

    public void CacheFunctionValue(int routineId, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        (_cachedFunctionValuesByRoutineId ??= []).TryAdd(routineId, value);
    }

    public bool TryGetCachedCapturedSlots(int routineId, out BytecodeVariableSlot[] captures)
    {
        if (_cachedCapturedSlotsByRoutineId is not null
            && _cachedCapturedSlotsByRoutineId.TryGetValue(routineId, out captures!))
        {
            return true;
        }

        captures = Array.Empty<BytecodeVariableSlot>();
        return false;
    }

    public void CacheCapturedSlots(int routineId, BytecodeVariableSlot[] captures)
    {
        ArgumentNullException.ThrowIfNull(captures);
        (_cachedCapturedSlotsByRoutineId ??= []).TryAdd(routineId, captures);
    }

    private static Dictionary<VariableSymbol, BytecodeVariableSlot> CreateVisibleVariables(BytecodeCallFrame frame)
    {
        var visibleVariables = new Dictionary<VariableSymbol, BytecodeVariableSlot>();
        var routine = frame.Routine;
        var arguments = frame.Arguments;
        var captures = frame.Captures;

        for (var index = 0; index < routine.ParameterVariables.Count && index < arguments.Length; index++)
        {
            visibleVariables[routine.ParameterVariables[index]] = arguments[index];
        }

        for (var index = 0; index < routine.LocalVariables.Count && index < frame.LocalCount; index++)
        {
            visibleVariables[routine.LocalVariables[index]] = frame.GetOrCreateLocalSlot(index);
        }

        for (var index = 0; index < routine.CapturedVariables.Count && index < captures.Length; index++)
        {
            visibleVariables[routine.CapturedVariables[index]] = captures[index];
        }

        return visibleVariables;
    }
}
