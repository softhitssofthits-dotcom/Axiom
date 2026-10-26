using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Независимая копия данных. Очередь заменяет рекурсивный обход, а таблица
/// соответствий сохраняет повторные связи и циклы внутри нового графа.
/// Функции с захватами и внешние ресурсы сохраняют прежнюю идентичность.
/// </summary>
internal sealed class RuntimeValueCopier
{
    private static readonly AsyncLocal<IRuntimeCancellationProvider?> Cancellation = new();
    private readonly Dictionary<object, RuntimeValue> _copies = new(ReferenceEqualityComparer.Instance);
    private readonly Queue<Action> _pending = new();
    private int _visited;

    public static IDisposable WithCancellation(IRuntimeCancellationProvider provider)
    {
        var previous = Cancellation.Value;
        Cancellation.Value = provider;
        return new CancellationScope(previous);
    }

    private sealed class CancellationScope(IRuntimeCancellationProvider? previous) : IDisposable
    {
        public void Dispose() => Cancellation.Value = previous;
    }

    public static RuntimeValue Copy(RuntimeValue value, int? fixedStringLength = null)
    {
        var copier = new RuntimeValueCopier();
        var result = copier.Prepare(value, fixedStringLength);
        while (copier._pending.TryDequeue(out var fill))
        {
            fill();
        }
        return result;
    }

    private RuntimeValue Prepare(RuntimeValue value, int? fixedStringLength = null)
    {
        if ((++_visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
        {
            throw new ValueCopyCancelledException();
        }

        if (value.Type == TypeSymbol.String)
        {
            return value.CopyForStorage(fixedStringLength);
        }

        if (value.Type is not (UserDefinedTypeSymbol or ArrayTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol))
        {
            return value;
        }

        var identity = value.Value!;
        if (_copies.TryGetValue(identity, out var existing))
        {
            return existing;
        }

        RuntimeValue copy;
        switch (value.Type)
        {
            case UserDefinedTypeSymbol recordType:
                var record = value.AsRecord();
                var recordCopy = RuntimeRecordValue.CreateForSnapshot(recordType);
                copy = RuntimeValue.FromRecord(recordType, recordCopy);
                _pending.Enqueue(() =>
                {
                    foreach (var field in recordType.Fields)
                        recordCopy.SetSnapshotField(field, Prepare(record.GetField(field), field.FixedStringLength));
                });
                break;
            case ArrayTypeSymbol arrayType:
                var array = value.AsArray();
                var arrayCopy = array.CreateEmptyForValueCopy(fixedStringLength);
                copy = RuntimeValue.FromArray(arrayType, arrayCopy);
                _pending.Enqueue(() => array.FillSnapshotInto(arrayCopy,
                    element => Prepare(element, arrayCopy.FixedStringLength)));
                break;
            case DictionaryTypeSymbol dictionaryType:
                var dictionary = value.AsDictionary();
                var dictionaryCopy = dictionary.CreateEmptyForSnapshot();
                copy = RuntimeValue.FromDictionary(dictionaryType, dictionaryCopy);
                _pending.Enqueue(() =>
                {
                    foreach (var (key, element) in dictionary.Entries())
                        dictionaryCopy.AppendForSnapshot(key, Prepare(element));
                });
                break;
            case SetTypeSymbol setType:
                var set = value.AsSet();
                var setCopy = set.CreateEmptyForValueCopy();
                copy = RuntimeValue.FromSet(setType, setCopy);
                _pending.Enqueue(() =>
                {
                    foreach (var element in set.Entries())
                        setCopy.AppendForSnapshot(Prepare(element));
                });
                break;
            default:
                throw new InvalidOperationException("Неподдерживаемый составной тип копии.");
        }
        _copies.Add(identity, copy);
        return copy;
    }
}

internal sealed class ValueCopyCancelledException : Exception;
