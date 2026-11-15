using System.Runtime.CompilerServices;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    /// <summary>
    /// Неизменяемое предвычисленное представление скомпилированного модуля для исполнения в BytecodeVm
    /// (по образцу неизменяемого PyCodeObject в CPython). Все плотные таблицы по routineId / builtinId / slot,
    /// константы, раскодированные потоки инструкций, карта исходных координат и типы записей/словарей/множеств
    /// строятся один раз для неизменившегося модуля и переиспользуются между запусками без повторных словарей и LINQ.
    /// </summary>
    private sealed class PreparedBytecodeModule
    {
        private static readonly ConditionalWeakTable<BytecodeModule, CachedPreparedModule> Cache = new();

        public PreparedBytecodeModule(
            BytecodeModule module,
            BytecodeRoutine?[] routinesById,
            Dictionary<int, BytecodeRoutine>? fallbackRoutines,
            VmInstruction[]?[] fusedInstructionsById,
            VmInstruction[]?[] unfusedInstructionsById,
            Dictionary<int, VmInstruction[]>? fallbackFusedInstructions,
            Dictionary<int, VmInstruction[]>? fallbackUnfusedInstructions,
            RoutineFrameTemplate[] frameTemplatesByRoutineId,
            Dictionary<int, RoutineFrameTemplate>? fallbackFrameTemplates,
            Dictionary<FunctionSymbol, BytecodeRoutine> routinesBySymbol,
            BuiltinBinding?[] builtinsById,
            Dictionary<int, BuiltinBinding>? fallbackBuiltins,
            BuiltinExecutionInfo?[] builtinExecutionInfosById,
            Dictionary<int, BuiltinExecutionInfo>? fallbackBuiltinExecutionInfos,
            Dictionary<FunctionSymbol, BuiltinBinding> builtinsByFunction,
            Dictionary<BuiltinFunctionId, BuiltinBinding> builtinsByCanonicalId,
            BytecodeGlobal?[] globalsBySlot,
            Dictionary<int, BytecodeGlobal>? fallbackGlobals,
            VmValue[] initialGlobalValuesBySlot,
            int?[] globalFixedStringLengths,
            bool[] hasGlobalSlot,
            RuntimeValue?[] constantValues,
            TextLocation[]?[] locationsByRoutineAndInstruction,
            Dictionary<string, UserDefinedTypeSymbol> recordTypesByIdentity,
            Dictionary<string, DictionaryTypeSymbol> dictionaryTypesByName,
            Dictionary<string, SetTypeSymbol> setTypesByName)
        {
            Module = module;
            RoutinesById = routinesById;
            FallbackRoutines = fallbackRoutines;
            FusedInstructionsById = fusedInstructionsById;
            UnfusedInstructionsById = unfusedInstructionsById;
            FallbackFusedInstructions = fallbackFusedInstructions;
            FallbackUnfusedInstructions = fallbackUnfusedInstructions;
            FrameTemplatesByRoutineId = frameTemplatesByRoutineId;
            FallbackFrameTemplates = fallbackFrameTemplates;
            RoutinesBySymbol = routinesBySymbol;
            BuiltinsById = builtinsById;
            FallbackBuiltins = fallbackBuiltins;
            BuiltinExecutionInfosById = builtinExecutionInfosById;
            FallbackBuiltinExecutionInfos = fallbackBuiltinExecutionInfos;
            BuiltinsByFunction = builtinsByFunction;
            BuiltinsByCanonicalId = builtinsByCanonicalId;
            GlobalsBySlot = globalsBySlot;
            FallbackGlobals = fallbackGlobals;
            InitialGlobalValuesBySlot = initialGlobalValuesBySlot;
            GlobalFixedStringLengths = globalFixedStringLengths;
            HasGlobalSlot = hasGlobalSlot;
            ConstantValues = constantValues;
            LocationsByRoutineAndInstruction = locationsByRoutineAndInstruction;
            RecordTypesByIdentity = recordTypesByIdentity;
            DictionaryTypesByName = dictionaryTypesByName;
            SetTypesByName = setTypesByName;
        }

        public BytecodeModule Module { get; }

        public BytecodeRoutine?[] RoutinesById { get; }

        public Dictionary<int, BytecodeRoutine>? FallbackRoutines { get; }

        public VmInstruction[]?[] FusedInstructionsById { get; }

        public VmInstruction[]?[] UnfusedInstructionsById { get; }

        public Dictionary<int, VmInstruction[]>? FallbackFusedInstructions { get; }

        public Dictionary<int, VmInstruction[]>? FallbackUnfusedInstructions { get; }

        public RoutineFrameTemplate[] FrameTemplatesByRoutineId { get; }

        public Dictionary<int, RoutineFrameTemplate>? FallbackFrameTemplates { get; }

        public Dictionary<FunctionSymbol, BytecodeRoutine> RoutinesBySymbol { get; }

        public BuiltinBinding?[] BuiltinsById { get; }

        public Dictionary<int, BuiltinBinding>? FallbackBuiltins { get; }

        public BuiltinExecutionInfo?[] BuiltinExecutionInfosById { get; }

        public Dictionary<int, BuiltinExecutionInfo>? FallbackBuiltinExecutionInfos { get; }

        public Dictionary<FunctionSymbol, BuiltinBinding> BuiltinsByFunction { get; }

        public Dictionary<BuiltinFunctionId, BuiltinBinding> BuiltinsByCanonicalId { get; }

        public BytecodeGlobal?[] GlobalsBySlot { get; }

        public Dictionary<int, BytecodeGlobal>? FallbackGlobals { get; }

        public VmValue[] InitialGlobalValuesBySlot { get; }

        public int?[] GlobalFixedStringLengths { get; }

        public bool[] HasGlobalSlot { get; }

        public RuntimeValue?[] ConstantValues { get; }

        public TextLocation[]?[] LocationsByRoutineAndInstruction { get; }

        public Dictionary<string, UserDefinedTypeSymbol> RecordTypesByIdentity { get; }

        public Dictionary<string, DictionaryTypeSymbol> DictionaryTypesByName { get; }

        public Dictionary<string, SetTypeSymbol> SetTypesByName { get; }

        public static PreparedBytecodeModule GetOrCreate(BytecodeModule module)
        {
            var stamp = ComputeInstructionStamp(module);
            if (Cache.TryGetValue(module, out var cached) && cached.Stamp == stamp)
            {
                return cached.Prepared;
            }

            var prepared = Build(module);
            Cache.AddOrUpdate(module, new CachedPreparedModule(stamp, prepared));
            return prepared;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetRoutine(int routineId, out BytecodeRoutine routine)
        {
            if ((uint)routineId < (uint)RoutinesById.Length)
            {
                var candidate = RoutinesById[routineId];
                if (candidate is not null)
                {
                    routine = candidate;
                    return true;
                }
            }

            if (FallbackRoutines is not null && FallbackRoutines.TryGetValue(routineId, out var fallback))
            {
                routine = fallback;
                return true;
            }

            routine = null!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RoutineFrameTemplate GetFrameTemplate(int routineId)
        {
            if ((uint)routineId < (uint)FrameTemplatesByRoutineId.Length)
            {
                return FrameTemplatesByRoutineId[routineId];
            }

            return FallbackFrameTemplates![routineId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VmInstruction[] GetDecodedInstructions(int routineId, bool fuse)
        {
            var dense = fuse ? FusedInstructionsById : UnfusedInstructionsById;
            if ((uint)routineId < (uint)dense.Length && dense[routineId] is { } instructions)
            {
                return instructions;
            }

            var fallback = fuse ? FallbackFusedInstructions : FallbackUnfusedInstructions;
            return fallback![routineId];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBuiltin(int builtinId, out BuiltinBinding builtin)
        {
            if ((uint)builtinId < (uint)BuiltinsById.Length)
            {
                var candidate = BuiltinsById[builtinId];
                if (candidate is not null)
                {
                    builtin = candidate;
                    return true;
                }
            }

            if (FallbackBuiltins is not null && FallbackBuiltins.TryGetValue(builtinId, out var fallback))
            {
                builtin = fallback;
                return true;
            }

            builtin = null!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetBuiltinExecutionInfo(int builtinId, out BuiltinExecutionInfo info)
        {
            if ((uint)builtinId < (uint)BuiltinExecutionInfosById.Length)
            {
                var candidate = BuiltinExecutionInfosById[builtinId];
                if (candidate is not null)
                {
                    info = candidate;
                    return true;
                }
            }

            if (FallbackBuiltinExecutionInfos is not null && FallbackBuiltinExecutionInfos.TryGetValue(builtinId, out var fallback))
            {
                info = fallback;
                return true;
            }

            info = null!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetGlobal(int slot, out BytecodeGlobal global)
        {
            if ((uint)slot < (uint)GlobalsBySlot.Length)
            {
                var candidate = GlobalsBySlot[slot];
                if (candidate is not null)
                {
                    global = candidate;
                    return true;
                }
            }

            if (FallbackGlobals is not null && FallbackGlobals.TryGetValue(slot, out var fallback))
            {
                global = fallback;
                return true;
            }

            global = null!;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextLocation ResolveLocation(int routineId, int instructionIndex)
        {
            if ((uint)routineId < (uint)LocationsByRoutineAndInstruction.Length)
            {
                var routineLocations = LocationsByRoutineAndInstruction[routineId];
                if (routineLocations is not null)
                {
                    if ((uint)instructionIndex < (uint)routineLocations.Length)
                    {
                        return routineLocations[instructionIndex];
                    }

                    if (instructionIndex >= routineLocations.Length && routineLocations.Length > 0)
                    {
                        return routineLocations[^1];
                    }
                }
            }

            return ResolveLocationSlow(routineId, instructionIndex);
        }

        private TextLocation ResolveLocationSlow(int routineId, int instructionIndex)
        {
            if (Module.DebugInfo is not null
                && Module.DebugInfo.TryGetLocation(routineId, instructionIndex, out var location))
            {
                return location;
            }

            return TryGetRoutine(routineId, out var routine)
                ? routine.Location
                : FallbackRuntimeLocation;
        }

        private static int ComputeInstructionStamp(BytecodeModule module)
        {
            var hash = new HashCode();
            var routines = module.Routines;
            hash.Add(routines.Count);

            for (var routineIndex = 0; routineIndex < routines.Count; routineIndex++)
            {
                var routine = routines[routineIndex];
                var instructions = routine.InstructionArray;
                hash.Add(routine.Id);
                hash.Add(instructions.Length);

                for (var i = 0; i < instructions.Length; i++)
                {
                    var instruction = instructions[i];
                    hash.Add((int)instruction.Opcode);
                    var operandCount = instruction.OperandCount;
                    hash.Add(operandCount);
                    if (operandCount > 0)
                    {
                        hash.Add(instruction.GetOperand(0));
                        if (operandCount > 1)
                        {
                            hash.Add(instruction.GetOperand(1));
                        }
                    }
                }
            }

            return hash.ToHashCode();
        }

        private static PreparedBytecodeModule Build(BytecodeModule module)
        {
            const int MaxDenseIndex = 65_536;

            // 1. Подпрограммы, шаблоны кадров и оба потока декодированных инструкций (быстрый и отладочный).
            var maxDenseRoutineId = -1;
            var hasSparseRoutine = false;
            var routines = module.Routines;
            for (var i = 0; i < routines.Count; i++)
            {
                var id = routines[i].Id;
                if ((uint)id <= MaxDenseIndex)
                {
                    if (id > maxDenseRoutineId)
                    {
                        maxDenseRoutineId = id;
                    }
                }
                else
                {
                    hasSparseRoutine = true;
                }
            }

            var denseRoutineLength = maxDenseRoutineId + 1;
            var routinesById = denseRoutineLength > 0 ? new BytecodeRoutine?[denseRoutineLength] : Array.Empty<BytecodeRoutine?>();
            var fusedById = denseRoutineLength > 0 ? new VmInstruction[]?[denseRoutineLength] : Array.Empty<VmInstruction[]?>();
            var unfusedById = denseRoutineLength > 0 ? new VmInstruction[]?[denseRoutineLength] : Array.Empty<VmInstruction[]?>();
            var templatesById = denseRoutineLength > 0 ? new RoutineFrameTemplate[denseRoutineLength] : Array.Empty<RoutineFrameTemplate>();

            Dictionary<int, BytecodeRoutine>? fallbackRoutines = hasSparseRoutine ? [] : null;
            Dictionary<int, VmInstruction[]>? fallbackFused = hasSparseRoutine ? [] : null;
            Dictionary<int, VmInstruction[]>? fallbackUnfused = hasSparseRoutine ? [] : null;
            Dictionary<int, RoutineFrameTemplate>? fallbackTemplates = hasSparseRoutine ? [] : null;
            var routinesBySymbol = new Dictionary<FunctionSymbol, BytecodeRoutine>(routines.Count);

            for (var i = 0; i < routines.Count; i++)
            {
                var routine = routines[i];
                var id = routine.Id;

                if (routine.Symbol is not null)
                {
                    routinesBySymbol.Add(routine.Symbol, routine);
                }

                var fused = DecodeInstructions(routine.InstructionArray, fuse: true);
                var unfused = DecodeInstructions(routine.InstructionArray, fuse: false);
                var template = CreateRoutineFrameTemplate(routine);

                if ((uint)id < (uint)denseRoutineLength)
                {
                    if (routinesById[id] is not null)
                    {
                        throw new ArgumentException($"An item with the same key has already been added. Key: {id}");
                    }

                    routinesById[id] = routine;
                    fusedById[id] = fused;
                    unfusedById[id] = unfused;
                    templatesById[id] = template;
                }
                else
                {
                    fallbackRoutines!.Add(id, routine);
                    fallbackFused!.Add(id, fused);
                    fallbackUnfused!.Add(id, unfused);
                    fallbackTemplates!.Add(id, template);
                }
            }

            // 2. Плотная таблица позиций исходного кода по (routineId, instructionIndex) — аналог co_linetable.
            var locationsByRoutine = denseRoutineLength > 0
                ? new TextLocation[]?[denseRoutineLength]
                : Array.Empty<TextLocation[]?>();

            if (denseRoutineLength > 0)
            {
                for (var routineId = 0; routineId < denseRoutineLength; routineId++)
                {
                    var routine = routinesById[routineId];
                    if (routine is null)
                    {
                        continue;
                    }

                    var count = Math.Max(1, routine.InstructionCount);
                    var table = new TextLocation[count];
                    Array.Fill(table, routine.Location);
                    locationsByRoutine[routineId] = table;
                }

                if (module.DebugInfo is { InstructionMap.Count: > 0 } debugInfo)
                {
                    // Записи для одной подпрограммы перечисляются в порядке эмиссии,
                    // но поддерживаем и произвольный порядок через отслеживание лучшего индекса.
                    var bestIndicesByRoutine = new int[]?[denseRoutineLength];
                    var maps = debugInfo.InstructionMap;
                    for (var i = 0; i < maps.Count; i++)
                    {
                        var map = maps[i];
                        var rId = map.RoutineId;
                        if ((uint)rId >= (uint)denseRoutineLength || locationsByRoutine[rId] is not { } table)
                        {
                            continue;
                        }

                        if (map.InstructionIndex < 0)
                        {
                            continue;
                        }

                        var bestIndices = bestIndicesByRoutine[rId];
                        if (bestIndices is null)
                        {
                            bestIndices = new int[table.Length];
                            Array.Fill(bestIndices, -1);
                            bestIndicesByRoutine[rId] = bestIndices;
                        }

                        for (var ip = map.InstructionIndex; ip < table.Length; ip++)
                        {
                            if (map.InstructionIndex >= bestIndices[ip])
                            {
                                bestIndices[ip] = map.InstructionIndex;
                                table[ip] = map.Location;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }

            // 3. Встроенные функции и исполняемые дескрипторы.
            var builtins = module.Builtins;
            var maxDenseBuiltinId = -1;
            var hasSparseBuiltin = false;
            for (var i = 0; i < builtins.Count; i++)
            {
                var id = builtins[i].Id;
                if ((uint)id <= MaxDenseIndex)
                {
                    if (id > maxDenseBuiltinId)
                    {
                        maxDenseBuiltinId = id;
                    }
                }
                else
                {
                    hasSparseBuiltin = true;
                }
            }

            var denseBuiltinLength = maxDenseBuiltinId + 1;
            var builtinsById = denseBuiltinLength > 0 ? new BuiltinBinding?[denseBuiltinLength] : Array.Empty<BuiltinBinding?>();
            var builtinInfosById = denseBuiltinLength > 0 ? new BuiltinExecutionInfo?[denseBuiltinLength] : Array.Empty<BuiltinExecutionInfo?>();
            Dictionary<int, BuiltinBinding>? fallbackBuiltins = hasSparseBuiltin ? [] : null;
            Dictionary<int, BuiltinExecutionInfo>? fallbackBuiltinInfos = hasSparseBuiltin ? [] : null;
            var builtinsByFunction = new Dictionary<FunctionSymbol, BuiltinBinding>(builtins.Count);
            var builtinsByCanonicalId = new Dictionary<BuiltinFunctionId, BuiltinBinding>(builtins.Count);

            for (var i = 0; i < builtins.Count; i++)
            {
                var builtin = builtins[i];
                var id = builtin.Id;
                var execution = CreateBuiltinExecution(builtin);
                var info = new BuiltinExecutionInfo(
                    execution,
                    builtin.ReturnsValue,
                    IsBuiltinReverseExecutionSafe(builtin.CanonicalId),
                    builtin.CanonicalId);

                if (builtin.Function is not null)
                {
                    builtinsByFunction.Add(builtin.Function, builtin);
                }

                builtinsByCanonicalId.TryAdd(builtin.CanonicalId, builtin);

                if ((uint)id < (uint)denseBuiltinLength)
                {
                    if (builtinsById[id] is not null)
                    {
                        throw new ArgumentException($"An item with the same key has already been added. Key: {id}");
                    }

                    builtinsById[id] = builtin;
                    builtinInfosById[id] = info;
                }
                else
                {
                    fallbackBuiltins!.Add(id, builtin);
                    fallbackBuiltinInfos!.Add(id, info);
                }
            }

            // 4. Глобальные слоты и начальный шаблон неизменяемых скалярных глобалей.
            var globals = module.Globals;
            var maxGlobalSlot = -1;
            var hasSparseGlobal = false;
            for (var i = 0; i < globals.Count; i++)
            {
                var slot = globals[i].Slot;
                if (slot >= 0 && slot <= MaxDenseIndex)
                {
                    if (slot > maxGlobalSlot)
                    {
                        maxGlobalSlot = slot;
                    }
                }
                else
                {
                    hasSparseGlobal = true;
                }
            }

            var denseGlobalLength = maxGlobalSlot + 1;
            var globalsBySlot = denseGlobalLength > 0 ? new BytecodeGlobal?[denseGlobalLength] : Array.Empty<BytecodeGlobal?>();
            Dictionary<int, BytecodeGlobal>? fallbackGlobals = hasSparseGlobal ? [] : null;
            var initialGlobalValues = denseGlobalLength > 0 ? new VmValue[denseGlobalLength] : Array.Empty<VmValue>();
            var globalFixedStringLengths = denseGlobalLength > 0 ? new int?[denseGlobalLength] : Array.Empty<int?>();
            var hasGlobalSlot = denseGlobalLength > 0 ? new bool[denseGlobalLength] : Array.Empty<bool>();

            for (var i = 0; i < globals.Count; i++)
            {
                var global = globals[i];
                var slot = global.Slot;
                if ((uint)slot < (uint)denseGlobalLength)
                {
                    if (globalsBySlot[slot] is not null)
                    {
                        throw new ArgumentException($"An item with the same key has already been added. Key: {slot}");
                    }

                    globalsBySlot[slot] = global;
                    // Ссылочные изменяемые контейнеры (ARRAY, DICTIONARY, SET) создаются отдельно на каждый запуск VM,
                    // а неизменяемые значения по умолчанию копируются единым Array.Copy из шаблона.
                    initialGlobalValues[slot] = global.Type is UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol
                        ? default
                        : VmValue.FromRuntime(RuntimeValue.Default(global.Type, global.FixedStringLength));
                    globalFixedStringLengths[slot] = global.FixedStringLength;
                    hasGlobalSlot[slot] = true;
                }
                else
                {
                    fallbackGlobals!.Add(slot, global);
                }
            }

            // 5. Таблица констант пула.
            var constantEntries = module.ConstantPool.Entries;
            var constantValues = constantEntries.Count == 0
                ? Array.Empty<RuntimeValue?>()
                : new RuntimeValue?[constantEntries.Count];
            for (var i = 0; i < constantEntries.Count; i++)
            {
                constantValues[i] = TryCreateRuntimeConstant(constantEntries[i]);
            }

            // 6. Единый однократный обход графа типов модуля для TYPE, DICTIONARY и SET.
            var recordTypes = new Dictionary<string, UserDefinedTypeSymbol>(StringComparer.OrdinalIgnoreCase);
            var dictionaryTypes = new Dictionary<string, DictionaryTypeSymbol>(StringComparer.OrdinalIgnoreCase);
            var setTypes = new Dictionary<string, SetTypeSymbol>(StringComparer.OrdinalIgnoreCase);
            CollectModuleTypes(module, recordTypes, dictionaryTypes, setTypes);

            return new PreparedBytecodeModule(
                module,
                routinesById,
                fallbackRoutines,
                fusedById,
                unfusedById,
                fallbackFused,
                fallbackUnfused,
                templatesById,
                fallbackTemplates,
                routinesBySymbol,
                builtinsById,
                fallbackBuiltins,
                builtinInfosById,
                fallbackBuiltinInfos,
                builtinsByFunction,
                builtinsByCanonicalId,
                globalsBySlot,
                fallbackGlobals,
                initialGlobalValues,
                globalFixedStringLengths,
                hasGlobalSlot,
                constantValues,
                locationsByRoutine,
                recordTypes,
                dictionaryTypes,
                setTypes);
        }

        private static void CollectModuleTypes(
            BytecodeModule module,
            Dictionary<string, UserDefinedTypeSymbol> recordTypes,
            Dictionary<string, DictionaryTypeSymbol> dictionaryTypes,
            Dictionary<string, SetTypeSymbol> setTypes)
        {
            var visited = new HashSet<TypeSymbol>(ReferenceEqualityComparer.Instance);

            var globals = module.Globals;
            for (var i = 0; i < globals.Count; i++)
            {
                CollectTypeSymbol(globals[i].Type, visited, recordTypes, dictionaryTypes, setTypes);
            }

            var routines = module.Routines;
            for (var i = 0; i < routines.Count; i++)
            {
                var routine = routines[i];
                CollectTypeSymbol(routine.ResultType, visited, recordTypes, dictionaryTypes, setTypes);

                var parameters = routine.ParameterVariables;
                for (var j = 0; j < parameters.Count; j++)
                {
                    CollectTypeSymbol(parameters[j].Type, visited, recordTypes, dictionaryTypes, setTypes);
                }

                var locals = routine.LocalVariables;
                for (var j = 0; j < locals.Count; j++)
                {
                    CollectTypeSymbol(locals[j].Type, visited, recordTypes, dictionaryTypes, setTypes);
                }

                var captures = routine.CapturedVariables;
                for (var j = 0; j < captures.Count; j++)
                {
                    CollectTypeSymbol(captures[j].Type, visited, recordTypes, dictionaryTypes, setTypes);
                }
            }
        }

        private static void CollectTypeSymbol(
            TypeSymbol? type,
            HashSet<TypeSymbol> visited,
            Dictionary<string, UserDefinedTypeSymbol> recordTypes,
            Dictionary<string, DictionaryTypeSymbol> dictionaryTypes,
            Dictionary<string, SetTypeSymbol> setTypes)
        {
            if (type is null || !visited.Add(type))
            {
                return;
            }

            if (type is UserDefinedTypeSymbol userType)
            {
                recordTypes.TryAdd(userType.TypeIdentity, userType);
                var fields = userType.Fields;
                for (var i = 0; i < fields.Count; i++)
                {
                    CollectTypeSymbol(fields[i].Type, visited, recordTypes, dictionaryTypes, setTypes);
                }

                return;
            }

            if (type is ArrayTypeSymbol arrayType)
            {
                CollectTypeSymbol(arrayType.ElementType, visited, recordTypes, dictionaryTypes, setTypes);
                return;
            }

            if (type is DictionaryTypeSymbol dictionaryType)
            {
                dictionaryTypes.TryAdd(dictionaryType.Name, dictionaryType);
                CollectTypeSymbol(dictionaryType.KeyType, visited, recordTypes, dictionaryTypes, setTypes);
                CollectTypeSymbol(dictionaryType.ValueType, visited, recordTypes, dictionaryTypes, setTypes);
                return;
            }

            if (type is SetTypeSymbol setType)
            {
                setTypes.TryAdd(setType.Name, setType);
                CollectTypeSymbol(setType.ElementType, visited, recordTypes, dictionaryTypes, setTypes);
            }
        }

        private sealed record CachedPreparedModule(int Stamp, PreparedBytecodeModule Prepared);
    }
}
