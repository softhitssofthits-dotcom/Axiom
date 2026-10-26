using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Копирует граф значений и слотов виртуальной машины, сохраняя общие ссылки
/// между массивами, записями, замыканиями и захваченными ячейками.
/// </summary>
internal static class RuntimeSnapshotFactory
{
    private static readonly object FunctionSnapshotInProgress = new();

    public static bool TryCreateValueSnapshot(
        RuntimeValue value,
        int? fixedStringLength,
        Dictionary<object, object> visited,
        out RuntimeValue snapshot)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(visited);

        if (value.Type is UserDefinedTypeSymbol recordType)
        {
            var record = value.AsRecord();
            if (visited.TryGetValue(record, out var existing))
            {
                snapshot = RuntimeValue.FromRecord(recordType, (RuntimeRecordValue)existing);
                return true;
            }

            var target = RuntimeRecordValue.CreateForSnapshot(recordType);
            visited[record] = target;
            foreach (var field in recordType.Fields)
            {
                if (!TryCreateValueSnapshot(record.GetField(field), field.FixedStringLength, visited, out var fieldSnapshot))
                {
                    snapshot = RuntimeValue.Void;
                    return false;
                }

                target.SetSnapshotField(field, fieldSnapshot);
            }

            snapshot = RuntimeValue.FromRecord(recordType, target);
            return true;
        }

        if (value.Type is ArrayTypeSymbol arrayType)
        {
            var array = value.AsArray();
            if (visited.TryGetValue(array, out var existing))
            {
                snapshot = RuntimeValue.FromArray(arrayType, (RuntimeArrayValue)existing);
                return true;
            }

            if (!array.IsAllocated)
            {
                var unallocatedTarget = array.CreateEmptyForSnapshot();
                visited[array] = unallocatedTarget;
                snapshot = RuntimeValue.FromArray(arrayType, unallocatedTarget);
                return true;
            }

            var storageIdentity = array.SnapshotStorageIdentity;
            if (visited.TryGetValue(storageIdentity, out existing))
            {
                var sharedStorage = (RuntimeArrayValue)existing;
                var sharedTarget = array.IsView
                    ? array.CreateViewForSnapshot(sharedStorage)
                    : sharedStorage;
                visited[array] = sharedTarget;
                snapshot = RuntimeValue.FromArray(arrayType, sharedTarget);
                return true;
            }

            var storageTarget = array.CreateEmptyStorageForSnapshot();
            visited[storageIdentity] = storageTarget;
            var target = array.IsView
                ? array.CreateViewForSnapshot(storageTarget)
                : storageTarget;
            visited[array] = target;

            var succeeded = true;
            array.FillStorageSnapshotInto(storageTarget, element =>
            {
                if (TryCreateValueSnapshot(element, array.FixedStringLength, visited, out var elementSnapshot))
                {
                    return elementSnapshot;
                }

                succeeded = false;
                return RuntimeValue.Void;
            });

            snapshot = RuntimeValue.FromArray(arrayType, target);
            return succeeded;
        }

        if (value.Type is DictionaryTypeSymbol dictionaryType)
        {
            var dictionary = value.AsDictionary();
            if (visited.TryGetValue(dictionary, out var existingDictionary))
            {
                snapshot = RuntimeValue.FromDictionary(dictionaryType, (RuntimeDictionaryValue)existingDictionary);
                return true;
            }

            var target = dictionary.CreateEmptyForSnapshot();
            visited[dictionary] = target;

            var succeeded = true;
            foreach (var (key, entryValue) in dictionary.Entries())
            {
                if (!TryCreateValueSnapshot(entryValue, fixedStringLength: null, visited, out var valueSnapshot))
                {
                    succeeded = false;
                    break;
                }

                target.AppendForSnapshot(key, valueSnapshot);
            }

            snapshot = RuntimeValue.FromDictionary(dictionaryType, target);
            return succeeded;
        }

        if (value.Type is FunctionTypeSymbol functionType)
        {
            var functionValue = value.AsFunction();
            if (functionValue is null)
            {
                snapshot = RuntimeValue.FromFunction(functionType, value: null);
                return true;
            }

            if (visited.TryGetValue(functionValue, out var existingFunction))
            {
                if (existingFunction is RuntimeValue existingSnapshot)
                {
                    snapshot = existingSnapshot;
                    return true;
                }

                snapshot = RuntimeValue.Void;
                return false;
            }

            if (functionValue.Function.IsBuiltIn)
            {
                snapshot = functionValue.BytecodeBuiltinBindingId is { } builtinBindingId
                    ? RuntimeValue.FromBytecodeBuiltinFunction(functionValue.Function, builtinBindingId)
                    : RuntimeValue.FromBytecodeFunction(functionValue.Function, []);
                visited[functionValue] = snapshot;
                return true;
            }

            if (functionValue.BytecodeCapturedSlots is not { } capturedSlots)
            {
                snapshot = RuntimeValue.Void;
                return false;
            }

            visited[functionValue] = FunctionSnapshotInProgress;
            var snapshotCaptures = new BytecodeVariableSlot[capturedSlots.Count];
            for (var index = 0; index < capturedSlots.Count; index++)
            {
                if (!TryCreateSlotSnapshot(capturedSlots[index], visited, out snapshotCaptures[index]))
                {
                    visited.Remove(functionValue);
                    snapshot = RuntimeValue.Void;
                    return false;
                }
            }

            snapshot = functionValue.BytecodeRoutine is { } routine
                ? RuntimeValue.FromBytecodeFunction(routine, snapshotCaptures)
                : RuntimeValue.FromBytecodeFunction(functionValue.Function, snapshotCaptures);
            visited[functionValue] = snapshot;
            return true;
        }

        if (value.Type == TypeSymbol.Integer
            || value.Type == TypeSymbol.Long
            || value.Type == TypeSymbol.Double
            || value.Type == TypeSymbol.Decimal
            || value.Type == TypeSymbol.String
            || value.Type == TypeSymbol.Boolean
            || value.Type == TypeSymbol.Date
            || value.Type == TypeSymbol.Time
            || value.Type == TypeSymbol.DateTime
            || value.Type == TypeSymbol.UtcDateTime
            || value.Type == TypeSymbol.RuntimeError
            || value.Type == TypeSymbol.Void)
        {
            snapshot = value.CopyForStorage(fixedStringLength);
            return true;
        }

        snapshot = RuntimeValue.Void;
        return false;
    }

    public static bool TryCreateSlotSnapshot(
        BytecodeVariableSlot sourceSlot,
        Dictionary<object, object> visited,
        out BytecodeVariableSlot snapshotSlot)
    {
        ArgumentNullException.ThrowIfNull(sourceSlot);
        ArgumentNullException.ThrowIfNull(visited);

        if (visited.TryGetValue(sourceSlot, out var existing) && existing is BytecodeVariableSlot existingSlot)
        {
            snapshotSlot = existingSlot;
            return true;
        }

        if (sourceSlot is BytecodeAddressVariableSlot addressSlot)
        {
            if (!TryCreateSlotSnapshot(addressSlot.BaseSlot, visited, out var baseSnapshot))
            {
                snapshotSlot = BytecodeVariableSlot.Adopt(RuntimeValue.Void);
                return false;
            }

            var steps = new BytecodeAddressStep[addressSlot.Steps.Count];
            for (var stepIndex = 0; stepIndex < steps.Length; stepIndex++)
            {
                var step = addressSlot.Steps[stepIndex];
                var indices = new BytecodeVariableSlot[step.IndexSlots.Length];
                for (var index = 0; index < indices.Length; index++)
                {
                    if (!TryCreateSlotSnapshot(step.IndexSlots[index], visited, out indices[index]))
                    {
                        snapshotSlot = BytecodeVariableSlot.Adopt(RuntimeValue.Void);
                        return false;
                    }
                }

                steps[stepIndex] = step with { IndexSlots = indices };
            }

            snapshotSlot = addressSlot.Rebind(baseSnapshot, steps);
            visited[sourceSlot] = snapshotSlot;
            return true;
        }

        if (sourceSlot is BytecodeArrayElementVariableSlot arrayElementSlot)
        {
            if (!TryCreateValueSnapshot(
                    RuntimeValue.FromArray(arrayElementSlot.ArrayType, arrayElementSlot.Array),
                    fixedStringLength: null,
                    visited,
                    out var arraySnapshot))
            {
                snapshotSlot = BytecodeVariableSlot.Adopt(RuntimeValue.Void);
                return false;
            }

            snapshotSlot = new BytecodeArrayElementVariableSlot(
                arrayElementSlot.ArrayType,
                arraySnapshot.AsArray(),
                arrayElementSlot.Indices,
                arrayElementSlot.ExpectedStructuralVersion);
            visited[sourceSlot] = snapshotSlot;
            return true;
        }

        if (!TryCreateValueSnapshot(sourceSlot.Value, sourceSlot.FixedStringLength, visited, out var valueSnapshot))
        {
            snapshotSlot = BytecodeVariableSlot.Adopt(RuntimeValue.Void);
            return false;
        }

        snapshotSlot = BytecodeVariableSlot.Adopt(
            valueSnapshot,
            sourceSlot.FixedStringLength,
            sourceSlot.IsForEachItem,
            sourceSlot.IsForEachItemPending);
        visited[sourceSlot] = snapshotSlot;
        return true;
    }
}
