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
/// Плоская запись (только скалярные поля и строки), плоский массив (элементы —
/// скаляры и строки), корневое множество и запись с единственным составным полем-плоским массивом
/// копируются без узлов вовсе: совместные вершины и циклы в таких графах
/// невозможны, а порядок визитов совпадает с общим путём.
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

        if (value.Type is ArrayTypeSymbol rootArrayType
            && IsFlatScalarArrayType(rootArrayType.ElementType))
        {
            // Корневой плоский массив: элементы-атомы не дают повторов и циклов.
            return CopyFlatScalarArrayRoot(value, rootArrayType, fixedStringLength);
        }

        if (value.Type is SetTypeSymbol rootSetType)
        {
            // Элементы множества атомарны: повторные вершины и циклы невозможны.
            var source = value.AsSet();
            var target = source.CreateEmptyForValueCopy();
            long setVisited = 1;
            FillSetCore(source, target, ref setVisited);
            return RuntimeValue.FromSet(rootSetType, target);
        }

        if (fixedStringLength is null
            && value.Type is DictionaryTypeSymbol rootDictionaryType
            && IsAtomicDictionaryType(rootDictionaryType)
            && HasOnlyAtomicValues(value.AsDictionary()))
        {
            // Ключи и значения атомарны по типу и по факту: повторные вершины
            // и циклы невозможны. Хранимое значение может отличаться от
            // заявленного типа, поэтому проверяются фактические записи.
            return CopyFlatDictionaryRoot(value, rootDictionaryType);
        }

        if (fixedStringLength is null
            && value.Type is UserDefinedTypeSymbol singleArrayType
            && IsSingleFlatArrayFieldRecordType(singleArrayType))
        {
            // Запись с единственным полем-плоским массивом: повтор невозможен,
            // массив заполняется после полей, как в общем пути.
            return CopySingleFlatArrayFieldRecordRoot(value, singleArrayType);
        }

        var copier = new RuntimeValueCopier();
        if (value.Type is ArrayTypeSymbol hintType && !IsFlatScalarArrayType(hintType.ElementType))
        {
            // Верхняя граница графа известна: корень плюс каждый элемент массива.
            copier.Reserve(value.AsArray().Length + 1);
        }

        var result = copier.Prepare(value, fixedStringLength);
        copier.RunPendingFills();
        return result;
    }

    /// <summary>
    /// Точное предвыделение узлов, когда верхняя граница графа известна:
    /// без резерва массив узлов растёт удвоением, а словарь соответствий —
    /// перехешированием, и суммарно выделяют заметно больше финального размера.
    /// Граф может превысить резерв (составные поля элементов), рост сохранён.
    /// </summary>
    internal void Reserve(int capacity)
    {
        if (capacity <= InlineNodeLimit || _nodes is not null)
        {
            return;
        }

        _nodes = new CopyNode[capacity];
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

    /// <summary>
    /// Корневой словарь с атомарными ключами и значениями: узлы не требуются,
    /// ключи переносятся как есть, значения проходят те же правила, что и
    /// Prepare общего пути (строки копируются для хранения без длины).
    /// </summary>
    private static RuntimeValue CopyFlatDictionaryRoot(RuntimeValue value, DictionaryTypeSymbol dictionaryType)
    {
        var source = value.AsDictionary();
        var target = source.CreateEmptyForSnapshot();
        long visited = 1;
        if ((visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
        {
            throw new ValueCopyCancelledException();
        }

        target.ReserveForSnapshot(source.Count);
        var entries = source.GetRawEntriesEnumerator();
        try
        {
            while (entries.MoveNext())
            {
                var entry = entries.Current;
                if (entry is null)
                    continue;
                if ((++visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
                {
                    throw new ValueCopyCancelledException();
                }

                var element = entry.Value;
                var copied = element.Type == TypeSymbol.String ? element.CopyForStorage(null) : element;
                target.AppendForSnapshotRaw(entry.Key, copied);
            }
        }
        finally
        {
            entries.Dispose();
        }

        return RuntimeValue.FromDictionary(dictionaryType, target);
    }

    /// <summary>
    /// Фактические значения записей атомарны: составное хранимое значение
    /// при атомарном заявленном типе возвращает общий путь с узлами.
    /// </summary>
    private static bool HasOnlyAtomicValues(RuntimeDictionaryValue source)
    {
        var entries = source.GetRawEntriesEnumerator();
        try
        {
            while (entries.MoveNext())
            {
                var entry = entries.Current;
                if (entry is null)
                    continue;
                if (entry.Value.Type is UserDefinedTypeSymbol or ArrayTypeSymbol
                    or DictionaryTypeSymbol or SetTypeSymbol)
                {
                    return false;
                }
            }
        }
        finally
        {
            entries.Dispose();
        }

        return true;
    }

    /// <summary>Атомарные ключи и значения словаря: общий путь с узлами не нужен.</summary>
    private static bool IsAtomicDictionaryType(DictionaryTypeSymbol type)
    {
        return type.KeyType is not (UserDefinedTypeSymbol or ArrayTypeSymbol
            or DictionaryTypeSymbol or SetTypeSymbol)
            && type.ValueType is not (UserDefinedTypeSymbol or ArrayTypeSymbol
            or DictionaryTypeSymbol or SetTypeSymbol);
    }

    /// <summary>
    /// Плоский массив без узлов: последовательность совпадает с общим путём —
    /// визит корня, заготовка через <see cref="RuntimeArrayValue.CreateEmptyForValueCopy"/>,
    /// затем заполнение <see cref="RuntimeArrayValue.FillSnapshotInto"/>, которое
    /// само выдерживает границы отмены для массового и поэлементного заполнения.
    /// </summary>
    private static RuntimeValue CopyFlatScalarArrayRoot(RuntimeValue value, ArrayTypeSymbol arrayType, int? fixedStringLength)
    {
        var source = value.AsArray();
        var copier = new RuntimeValueCopier();
        copier.Visit();
        var target = source.CreateEmptyForValueCopy(fixedStringLength);
        // Общий путь передаёт заполнению разрешённую длину цели, а не сырой параметр.
        source.FillSnapshotInto(target, copier, target.FixedStringLength);
        return RuntimeValue.FromArray(arrayType, target);
    }

    /// <summary>
    /// Запись с единственным полем-плоским массивом без узлов: поля заполняются
    /// по порядку, как в общем пути, а заполнение массива выполняется после всех
    /// полей — на месте, где общий путь обрабатывал бы его узел из очереди.
    /// </summary>
    private static RuntimeValue CopySingleFlatArrayFieldRecordRoot(RuntimeValue value, UserDefinedTypeSymbol recordType)
    {
        var source = value.AsRecord();
        var copier = new RuntimeValueCopier();
        copier.Visit();
        var recordCopy = RuntimeRecordValue.CreateForSnapshot(recordType);
        var fields = recordType.Fields;
        RuntimeArrayValue? pendingSource = null;
        RuntimeArrayValue? pendingTarget = null;
        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            var field = fields[fieldIndex];
            copier.Visit();
            var fieldValue = source.GetField(field);
            RuntimeValue copied;
            if (fieldValue.Type is ArrayTypeSymbol arrayFieldType)
            {
                var arraySource = fieldValue.AsArray();
                var arrayTarget = arraySource.CreateEmptyForValueCopy(field.FixedStringLength);
                pendingSource = arraySource;
                pendingTarget = arrayTarget;
                copied = RuntimeValue.FromArray(arrayFieldType, arrayTarget);
            }
            else if (fieldValue.Type == TypeSymbol.String)
            {
                copied = fieldValue.CopyForStorage(field.FixedStringLength);
            }
            else if (fieldValue.Type is UserDefinedTypeSymbol or DictionaryTypeSymbol or SetTypeSymbol)
            {
                // Недостижимо для записи с единственным полем-плоским массивом.
                throw new InvalidOperationException("Неподдерживаемый составной тип копии.");
            }
            else
            {
                copied = fieldValue;
            }

            recordCopy.SetSnapshotField(field, copied);
        }

        pendingSource?.FillSnapshotInto(pendingTarget!, copier, pendingTarget!.FixedStringLength);
        return RuntimeValue.FromRecord(recordType, recordCopy);
    }

    /// <summary>Визит общего пути с его границей проверки отмены.</summary>
    private void Visit()
    {
        if ((++_visited & 4095) == 0 && Cancellation.Value?.IsCancellationRequested == true)
        {
            throw new ValueCopyCancelledException();
        }
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
                var entries = source.GetRawEntriesEnumerator();
                try
                {
                    while (entries.MoveNext())
                    {
                        var entry = entries.Current;
                        if (entry is null)
                            continue;
                        target.AppendForSnapshotRaw(entry.Key, Prepare(entry.Value));
                    }
                }
                finally
                {
                    entries.Dispose();
                }

                break;
            }

            case FillKind.Set:
            {
                var source = (RuntimeSetValue)work.Identity;
                var target = work.Copy.AsSet();
                FillSet(source, target);
                break;
            }
        }
    }

    private void FillSet(RuntimeSetValue source, RuntimeSetValue target)
    {
        FillSetCore(source, target, ref _visited);
    }

    private static void FillSetCore(RuntimeSetValue source, RuntimeSetValue target, ref long visited)
    {
        target.ReserveForSnapshot(source.Count);
        var entries = source.GetRawEntriesEnumerator();
        try
        {
            while (entries.MoveNext())
            {
                var element = entries.Current;
                if (element is null)
                    continue;
                CheckCancellationForBulkFillCore(ref visited, 1);
                target.AppendForSnapshotRaw(element.Value);
            }
        }
        finally
        {
            entries.Dispose();
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
        CheckCancellationForBulkFillCore(ref _visited, elementVisits);
    }

    private static void CheckCancellationForBulkFillCore(ref long visited, int elementVisits)
    {
        var completedVisits = visited + elementVisits;
        var cancellation = Cancellation.Value;
        if (cancellation is not null)
        {
            for (var boundary = visited / 4096; boundary < completedVisits / 4096; boundary++)
            {
                if (cancellation.IsCancellationRequested)
                    throw new ValueCopyCancelledException();
            }
        }
        visited = completedVisits;
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
        if (_nodeCount == InlineNodeLimit)
        {
            // Эскалация по счёту узлов, а не по границе удвоения: при точном
            // резерве граница массива может не совпасть с порогом. Ёмкость
            // словаря берётся с резерва, чтобы избежать перехеширования.
            // Дальше соответствия ищет словарь; узлы остаются списком работ
            // в прежнем порядке.
            _copies = new Dictionary<object, RuntimeValue>(
                Math.Max(InlineNodeLimit, _nodes.Length), ReferenceEqualityComparer.Instance);
            for (var index = 0; index < _nodeCount; index++)
            {
                _copies.Add(_nodes[index].Identity, _nodes[index].Copy);
            }
        }

        if (_nodeCount == _nodes.Length)
        {
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

    /// <summary>Атомарный элемент: составные элементы оставляют общий путь с узлами.</summary>
    private static bool IsFlatScalarArrayType(TypeSymbol elementType)
    {
        return elementType is not (UserDefinedTypeSymbol or ArrayTypeSymbol
            or DictionaryTypeSymbol or SetTypeSymbol);
    }

    /// <summary>
    /// Ровно одно составное поле, и это плоский массив: в таком графе нет
    /// ни повторов, ни циклов, поэтому соответствия не требуются.
    /// </summary>
    private static bool IsSingleFlatArrayFieldRecordType(UserDefinedTypeSymbol type)
    {
        var fields = type.Fields;
        var compositeCount = 0;
        for (var fieldIndex = 0; fieldIndex < fields.Count; fieldIndex++)
        {
            if (fields[fieldIndex].Type is not (UserDefinedTypeSymbol or ArrayTypeSymbol
                or DictionaryTypeSymbol or SetTypeSymbol))
            {
                continue;
            }

            compositeCount++;
            if (compositeCount > 1
                || fields[fieldIndex].Type is not ArrayTypeSymbol arrayFieldType
                || !IsFlatScalarArrayType(arrayFieldType.ElementType))
            {
                return false;
            }
        }

        return compositeCount == 1;
    }
}

internal sealed class ValueCopyCancelledException : Exception;
