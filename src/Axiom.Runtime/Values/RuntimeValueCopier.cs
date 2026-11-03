using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Независимая копия данных. Массив узлов заменяет рекурсивный обход и одновременно
/// хранит соответствия «исходная вершина → копия»: каждой новой вершине соответствует
/// ровно одна работа заполнения, поэтому таблица и очередь совпадают один к одному
/// (эталоны структурного обхода: mark-stack GC CPython, стек-буфер CoreLib
/// ValueListBuilder; атомы не регистрируются — как memo в copy.deepcopy).
/// Для малых графов поиск соответствия линейный по узлам; после 32 узлов строится
/// словарь соответствий, а сами узлы остаются списком работ в прежнем порядке.
/// Функции с захватами и внешние ресурсы сохраняют прежнюю идентичность.
/// Плоская запись (только скалярные поля и строки) копируется без узлов вовсе:
/// совместные вершины внутри неё невозможны, а порядок визитов совпадает.
/// </summary>
internal sealed class RuntimeValueCopier
{
    private const int InlineNodeLimit = 32;

    private static readonly AsyncLocal<IRuntimeCancellationProvider?> Cancellation = new();
    private CopyNode[]? _nodes;
    private int _nodeCount;
    private Dictionary<object, RuntimeValue>? _copies;
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
            // Корневая плоская запись — единственная вершина графа: узлы не требуются вовсе.
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

    /// <summary>Вид узла: задаёт тип исходной вершины и вид работы заполнения.</summary>
    private enum FillKind : byte
    {
        Record = 0,
        Array = 1,
        Dictionary = 2,
        Set = 3,
    }

    /// <summary>
    /// Узел одновременной таблицы соответствий и списка работ: Identity — исходная
    /// вершина (для поиска повторов и как источник заполнения), Copy — значение копии
    /// (символ типа берётся из него), Kind — вид заполнения. Порядок узлов в массиве
    /// совпадает с прежним порядком очереди работ.
    /// </summary>
    private struct CopyNode
    {
        public object Identity;
        public RuntimeValue Copy;
        public FillKind Kind;
    }

    private void RunPendingFills()
    {
        if (_nodes is null)
        {
            return;
        }

        // Узлы добавляются в конец во время обработки: обход по индексу
        // сохраняет прежний порядок очереди без извлечения элементов.
        for (var workIndex = 0; workIndex < _nodeCount; workIndex++)
        {
            ProcessFill(_nodes[workIndex]);
        }
    }

    private void ProcessFill(in CopyNode work)
    {
        switch (work.Kind)
        {
            case FillKind.Record:
            {
                var source = (RuntimeRecordValue)work.Identity;
                var target = work.Copy.AsRecord();
                var fields = ((UserDefinedTypeSymbol)work.Copy.Type).Fields;
                for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
                {
                    var field = fields[fieldIndex];
                    target.SetSnapshotField(field, Prepare(source.GetField(field), field.FixedStringLength));
                }

                break;
            }

            case FillKind.Array:
            {
                var arrayTarget = work.Copy.AsArray();
                ((RuntimeArrayValue)work.Identity).FillSnapshotInto(arrayTarget, this, arrayTarget.FixedStringLength);
                break;
            }

            case FillKind.Dictionary:
            {
                var source = (RuntimeDictionaryValue)work.Identity;
                var target = work.Copy.AsDictionary();
                // Копия заполняется под размер источника и уже приведёнными
                // ключами: без роста индекса с нуля и промежуточных обёрток.
                target.ReserveForSnapshot(source.Count);
                foreach (var (key, element) in source.RawEntries())
                {
                    target.AppendForSnapshotRaw(key, Prepare(element));
                }

                break;
            }

            case FillKind.Set:
            {
                var source = (RuntimeSetValue)work.Identity;
                var target = work.Copy.AsSet();
                // Элементы множества — только скаляры: копия переносит ключи
                // без обёрток и с предвыделенным местом. Счётчик визитов на
                // элемент сохраняет прежние границы проверки отмены (Prepare).
                target.ReserveForSnapshot(source.Count);
                foreach (var element in source.RawEntries())
                {
                    CheckCancellationForBulkFill(1);
                    target.AppendForSnapshotRaw(element);
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

    /// <summary>Вход для заполнения массива из узла: без делегатов.</summary>
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
        var existing = FindCopy(identity);
        if (existing is not null)
        {
            return existing;
        }

        RuntimeValue copy;
        FillKind kind;
        switch (value.Type)
        {
            case UserDefinedTypeSymbol recordType:
                _ = value.AsRecord();
                var recordCopy = RuntimeRecordValue.CreateForSnapshot(recordType);
                copy = RuntimeValue.FromRecord(recordType, recordCopy);
                kind = FillKind.Record;
                break;
            case ArrayTypeSymbol arrayType:
                var array = value.AsArray();
                var arrayCopy = array.CreateEmptyForValueCopy(fixedStringLength);
                copy = RuntimeValue.FromArray(arrayType, arrayCopy);
                kind = FillKind.Array;
                break;
            case DictionaryTypeSymbol dictionaryType:
                var dictionary = value.AsDictionary();
                var dictionaryCopy = dictionary.CreateEmptyForSnapshot();
                copy = RuntimeValue.FromDictionary(dictionaryType, dictionaryCopy);
                kind = FillKind.Dictionary;
                break;
            case SetTypeSymbol setType:
                var set = value.AsSet();
                var setCopy = set.CreateEmptyForValueCopy();
                copy = RuntimeValue.FromSet(setType, setCopy);
                kind = FillKind.Set;
                break;
            default:
                throw new InvalidOperationException("Неподдерживаемый составной тип копии.");
        }

        AddNode(identity, copy, kind);
        return copy;
    }

    private RuntimeValue? FindCopy(object identity)
    {
        if (_copies is not null)
        {
            return _copies.TryGetValue(identity, out var existing) ? existing : null;
        }

        var nodes = _nodes;
        if (nodes is not null)
        {
            for (var index = 0; index < _nodeCount; index++)
            {
                if (ReferenceEquals(nodes[index].Identity, identity))
                {
                    return nodes[index].Copy;
                }
            }
        }

        return null;
    }

    private void AddNode(object identity, RuntimeValue copy, FillKind kind)
    {
        // Старт 2: самый частый граф — корень и одна составная вершина (массив
        // или вложенная запись); дальнейший рост удвоением.
        _nodes ??= new CopyNode[2];
        if (_nodeCount == _nodes.Length)
        {
            if (_nodeCount == InlineNodeLimit)
            {
                // Эскалация: дальше соответствия ищет словарь; узлы остаются
                // списком работ в прежнем порядке.
                _copies = new Dictionary<object, RuntimeValue>(ReferenceEqualityComparer.Instance);
                for (var index = 0; index < _nodeCount; index++)
                {
                    _copies.Add(_nodes[index].Identity, _nodes[index].Copy);
                }
            }

            Array.Resize(ref _nodes, _nodeCount * 2);
        }

        _nodes[_nodeCount] = new CopyNode { Identity = identity, Copy = copy, Kind = kind };
        _nodeCount++;
        _copies?.Add(identity, copy);
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
