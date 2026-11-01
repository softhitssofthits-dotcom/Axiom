using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Независимая копия данных. Список работ заменяет рекурсивный обход, а таблица
/// соответствий сохраняет повторные связи и циклы внутри нового графа.
/// Функции с захватами и внешние ресурсы сохраняют прежнюю идентичность.
/// Плоская запись (только скалярные поля и строки) копируется без списка
/// и без таблицы: совместные вершины внутри неё невозможны, а порядок
/// визитов совпадает с общим путём. Список работ структурный (без замыканий
/// и делегатов): эталон — тегированные структурные обходы (GC mark-stack
/// и проходы компилятора CPython).
/// </summary>
internal sealed class RuntimeValueCopier
{
    private static readonly AsyncLocal<IRuntimeCancellationProvider?> Cancellation = new();
    private readonly Dictionary<object, RuntimeValue> _copies = new(ReferenceEqualityComparer.Instance);
    private List<FillWork>? _pending;
    private long _visited;

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
        if (fixedStringLength is null
            && value.Type is UserDefinedTypeSymbol rootType
            && IsFlatScalarRecordType(rootType))
        {
            // Корневая плоская запись — единственная вершина графа: таблица
            // соответствий и список работ не требуются вовсе.
            return CopyFlatScalarRecordRoot(value, rootType);
        }

        var copier = new RuntimeValueCopier();
        var result = copier.Prepare(value, fixedStringLength);
        copier.RunPendingFills();
        return result;
    }

    private static RuntimeValue CopyFlatScalarRecordRoot(RuntimeValue value, UserDefinedTypeSymbol recordType)
    {
        var record = value.AsRecord();
        var recordCopy = RuntimeRecordValue.CreateForSnapshot(recordType);
        // Счёт визитов повторяет общий путь: корень, затем каждое поле.
        long visited = 1;
        if ((visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
        {
            throw new ValueCopyCancelledException();
        }

        var rootFields = recordType.Fields;
        for (var fieldIndex = 0; fieldIndex < rootFields.Count; fieldIndex++)
        {
            var field = rootFields[fieldIndex];
            if ((++visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
            {
                throw new ValueCopyCancelledException();
            }

            var fieldValue = record.GetField(field);
            RuntimeValue copied;
            if (fieldValue.Type == TypeSymbol.String)
            {
                copied = fieldValue.CopyForStorage(field.FixedStringLength);
            }
            else if (fieldValue.Type is UserDefinedTypeSymbol or ArrayTypeSymbol
                     or DictionaryTypeSymbol or SetTypeSymbol)
            {
                // Недостижимо для плоской записи: типы полей проверены заранее.
                throw new InvalidOperationException("Неподдерживаемый составной тип копии.");
            }
            else
            {
                copied = fieldValue;
            }

            recordCopy.SetSnapshotField(field, copied);
        }

        return RuntimeValue.FromRecord(recordType, recordCopy);
    }

    /// <summary>Вид узла заполнения структурного списка работ.</summary>
    private enum FillKind : byte
    {
        Record = 0,
        Array = 1,
        Dictionary = 2,
        Set = 3,
    }

    /// <summary>Структурный элемент списка работ: без замыканий и делегатов.</summary>
    private struct FillWork
    {
        public FillKind Kind;
        public UserDefinedTypeSymbol? RecordType;
        public RuntimeRecordValue? RecordSource;
        public RuntimeRecordValue? RecordCopy;
        public RuntimeArrayValue? ArraySource;
        public RuntimeArrayValue? ArrayTarget;
        public RuntimeDictionaryValue? DictionarySource;
        public RuntimeDictionaryValue? DictionaryCopy;
        public RuntimeSetValue? SetSource;
        public RuntimeSetValue? SetCopy;
        public int? FixedStringLength;
    }

    private void RunPendingFills()
    {
        if (_pending is null)
        {
            return;
        }

        // Заполнения добавляются в конец во время обработки: обход по индексу
        // сохраняет прежний порядок очереди без извлечения элементов.
        for (var workIndex = 0; workIndex < _pending.Count; workIndex++)
        {
            ProcessFill(_pending[workIndex]);
        }
    }

    private void ProcessFill(in FillWork work)
    {
        switch (work.Kind)
        {
            case FillKind.Record:
            {
                var fields = work.RecordType!.Fields;
                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    var field = fields[fieldIndex];
                    work.RecordCopy!.SetSnapshotField(field, Prepare(work.RecordSource!.GetField(field), field.FixedStringLength));
                }

                break;
            }

            case FillKind.Array:
                work.ArraySource!.FillSnapshotInto(work.ArrayTarget!, this, work.FixedStringLength);
                break;

            case FillKind.Dictionary:
            {
                foreach (var (key, element) in work.DictionarySource!.Entries())
                {
                    work.DictionaryCopy!.AppendForSnapshot(key, Prepare(element));
                }

                break;
            }

            case FillKind.Set:
            {
                foreach (var element in work.SetSource!.Entries())
                {
                    work.SetCopy!.AppendForSnapshot(Prepare(element));
                }

                break;
            }
        }
    }

    /// <summary>
    /// Точка отмены для прямого заполнения буфера, которое заменяет
    /// поэлементные визиты: срабатывает, когда поэлементный путь пересёк
    /// очередную границу в 4096 визитов. Договор совпадает с прежним
    /// поведением: исключение до изменения источника, заготовка отбрасывается.
    /// </summary>
    internal void CheckCancellationForBulkFill(int elementVisits)
    {
        var completedVisits = _visited + elementVisits;
        var cancellation = Cancellation.Value;
        if (cancellation is not null)
        {
            for (var boundary = _visited / 4096; boundary < completedVisits / 4096; boundary++)
            {
                if (cancellation.IsCancellationRequested)
                    throw new ValueCopyCancelledException();
            }
        }
        _visited = completedVisits;
    }

    /// <summary>Вход для заполнения массива из списка работ: без делегатов.</summary>
    internal RuntimeValue PrepareValueForFill(RuntimeValue value, int? fixedStringLength)
    {
        return Prepare(value, fixedStringLength);
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
                if (IsFlatScalarRecordType(recordType))
                {
                    // Плоская запись как вложенная вершина: поля копируются сразу,
                    // без постановки в список работ. Индексный цикл без
                    // упакованного энумератора интерфейсного IReadOnlyList.
                    var flatFields = recordType.Fields;
                    for (var fieldIndex = 0; fieldIndex < flatFields.Count; fieldIndex++)
                    {
                        var field = flatFields[fieldIndex];
                        recordCopy.SetSnapshotField(field, Prepare(record.GetField(field), field.FixedStringLength));
                    }
                }
                else
                {
                    EnqueueFill(new FillWork
                    {
                        Kind = FillKind.Record,
                        RecordType = recordType,
                        RecordSource = record,
                        RecordCopy = recordCopy,
                    });
                }
                break;
            case ArrayTypeSymbol arrayType:
                var array = value.AsArray();
                var arrayCopy = array.CreateEmptyForValueCopy(fixedStringLength);
                copy = RuntimeValue.FromArray(arrayType, arrayCopy);
                EnqueueFill(new FillWork
                {
                    Kind = FillKind.Array,
                    ArraySource = array,
                    ArrayTarget = arrayCopy,
                    FixedStringLength = arrayCopy.FixedStringLength,
                });
                break;
            case DictionaryTypeSymbol dictionaryType:
                var dictionary = value.AsDictionary();
                var dictionaryCopy = dictionary.CreateEmptyForSnapshot();
                copy = RuntimeValue.FromDictionary(dictionaryType, dictionaryCopy);
                EnqueueFill(new FillWork
                {
                    Kind = FillKind.Dictionary,
                    DictionarySource = dictionary,
                    DictionaryCopy = dictionaryCopy,
                });
                break;
            case SetTypeSymbol setType:
                var set = value.AsSet();
                var setCopy = set.CreateEmptyForValueCopy();
                copy = RuntimeValue.FromSet(setType, setCopy);
                EnqueueFill(new FillWork
                {
                    Kind = FillKind.Set,
                    SetSource = set,
                    SetCopy = setCopy,
                });
                break;
            default:
                throw new InvalidOperationException("Неподдерживаемый составной тип копии.");
        }
        _copies.Add(identity, copy);
        return copy;
    }

    private void EnqueueFill(in FillWork work)
    {
        (_pending ??= new List<FillWork>()).Add(work);
    }

    private static bool IsFlatScalarRecordType(UserDefinedTypeSymbol type)
    {
        var fields = type.Fields;
        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            if (fields[fieldIndex].Type is UserDefinedTypeSymbol or ArrayTypeSymbol
                or DictionaryTypeSymbol or SetTypeSymbol)
            {
                return false;
            }
        }

        return true;
    }
}

internal sealed class ValueCopyCancelledException : Exception;
