using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Типизированное множество уникальных значений времени выполнения. Элементы
/// хранятся в порядке первого добавления: повторное добавление не меняет место,
/// удаление и повторное добавление переносят элемент в конец. Наличие элемента
/// определяется точным ключом коллекции, общим со словарём.
/// </summary>
public sealed class RuntimeSetValue
{
    /// <summary>
    /// Предел числа элементов следует общему пределу элементов коллекции, чтобы
    /// рост множества не превращался в необратимый отказ процесса.
    /// </summary>
    internal const int MaxEntryCount = RuntimeArrayValue.MaxElementCount;

    /// <summary>
    /// Шаг опроса отмены в собственных обходах множества. Достаточно редкий,
    /// чтобы не удорожать обход, и достаточно частый, чтобы отмена не ждала
    /// конца многомиллионной операции.
    /// </summary>
    private const int CancellationCheckInterval = 4096;

    /// <summary>
    /// Явный предел для проверок отказа. Достичь общего предела в проверке
    /// нельзя: он требует десятков миллионов добавлений, поэтому проверка
    /// подставляет малый предел в своём контексте выполнения. Значение живёт в
    /// AsyncLocal, чтобы параллельные проверки не влияли друг на друга.
    /// </summary>
    private static readonly AsyncLocal<int?> EntryLimitOverride = new();

    internal static IDisposable UseEntryLimitForTesting(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

        var previous = EntryLimitOverride.Value;
        EntryLimitOverride.Value = limit;
        return new EntryLimitScope(previous);
    }

    private sealed class EntryLimitScope(int? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            EntryLimitOverride.Value = previous;
        }
    }

    private readonly List<RuntimeCollectionKey?> _entries = [];
    private readonly Dictionary<RuntimeCollectionKey, int> _indexByKey = [];
    private readonly int _maxEntryCount;
    private int _liveCount;
    private int _removedCount;

    public RuntimeSetValue(SetTypeSymbol type)
        : this(type, EntryLimitOverride.Value ?? MaxEntryCount)
    {
    }

    /// <summary>
    /// Конструктор с явным пределом нужен проверкам: отказ по пределу
    /// проверяется на малом числе без выделения коллекции предельного размера.
    /// </summary>
    internal RuntimeSetValue(SetTypeSymbol type, int maxEntryCount)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (maxEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntryCount), maxEntryCount, "Set entry limit must be non-negative.");
        }

        Type = type;
        _maxEntryCount = maxEntryCount;
    }

    public SetTypeSymbol Type { get; }

    public int Count => _liveCount;

    public bool Contains(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return _indexByKey.ContainsKey(NormalizeElement(value));
    }

    /// <summary>
    /// Добавляет элемент. Повторный элемент не создаёт второй записи и не
    /// меняет порядок: вызывающий код получает FALSE.
    /// </summary>
    public bool TryAdd(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = NormalizeElement(value);
        if (_indexByKey.ContainsKey(normalized))
        {
            return false;
        }

        Append(normalized);
        return true;
    }

    /// <summary>
    /// Удаляет элемент. Отсутствующий элемент не является ошибкой: вызывающий
    /// код получает FALSE и продолжает работу.
    /// </summary>
    public bool Remove(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = NormalizeElement(value);
        if (!_indexByKey.Remove(normalized, out var index))
        {
            return false;
        }

        _entries[index] = null;
        _liveCount--;
        _removedCount++;

        // Освободившиеся места убираются, когда их становится больше половины:
        // порядок оставшихся элементов при этом не меняется.
        if (_removedCount > _liveCount)
        {
            Compact();
        }

        return true;
    }

    /// <summary>
    /// Элементы в порядке добавления как самостоятельный одномерный массив с
    /// границами 1..Count; для пустого множества границы 1 TO 0. Изменение
    /// множества после вызова не меняет результат.
    /// </summary>
    public RuntimeValue CreateElementsArray(IRuntimeCancellationProvider? cancellation = null)
    {
        var elementType = Type.ElementType;
        var array = new RuntimeArrayValue(elementType, fixedStringLength: null, rank: 1);
        array.Allocate([1], [_liveCount]);

        var position = 1;
        var inspected = 0;
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            // Снимок только читает множество: прерывание оставляет вызывающий
            // код без результата, но не меняет исходные данные.
            CheckCancellation(cancellation, ++inspected);

            array.TrySetValue1(position, entry.Value.ToRuntimeValue(elementType));
            position++;
        }

        return RuntimeValue.FromArray((ArrayTypeSymbol)ArrayTypeSymbol.Create(elementType), array);
    }

    /// <summary>
    /// Добавляет в получателя элементы другого множества, которых в нём ещё
    /// нет, сохраняя порядок обхода другого множества. Правый операнд не
    /// изменяется. При логическом превышении предела получатель остаётся
    /// прежним.
    /// </summary>
    public void UnionWith(RuntimeSetValue other, IRuntimeCancellationProvider? cancellation = null)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameType(other);

        // Самооперация не меняет ни состав, ни порядок.
        if (ReferenceEquals(this, other))
        {
            return;
        }

        // Первая фаза: только расчёт недостающих элементов без изменения
        // получателя. Предел и отмена проверяются до первого изменения,
        // поэтому отказ не оставляет получателя частично изменённым.
        var missing = new List<RuntimeCollectionKey>();
        var inspected = 0;
        foreach (var entry in other._entries)
        {
            if (entry is null)
            {
                continue;
            }

            CheckCancellation(cancellation, ++inspected);

            if (_indexByKey.ContainsKey(entry.Value))
            {
                continue;
            }

            missing.Add(entry.Value);
        }

        if (missing.Count == 0)
        {
            return;
        }

        if (_liveCount + missing.Count > _maxEntryCount)
        {
            throw new SetEntryLimitException(
                $"Не удалось объединить множества: число элементов превышает предел {_maxEntryCount}.");
        }

        // Вторая фаза: изменение получателя после успешной проверки. Отмена
        // здесь не опрашивается: иначе получатель остался бы изменённым
        // частично, а договор требует отказа без частичного изменения.
        foreach (var key in missing)
        {
            Append(key);
        }
    }

    /// <summary>
    /// Оставляет в получателе только элементы, которые есть в другом
    /// множестве, сохраняя прежний порядок получателя. Правый операнд не
    /// изменяется.
    /// </summary>
    public void IntersectWith(RuntimeSetValue other, IRuntimeCancellationProvider? cancellation = null)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameType(other);

        // Самооперация не меняет ни состав, ни порядок.
        if (ReferenceEquals(this, other))
        {
            return;
        }

        // Первая фаза: чистый расчёт. Пересечение не расширяет получателя,
        // поэтому предел здесь недостижим, но расчёт отделён от изменения.
        // Отмена опрашивается только здесь: получатель ещё не изменён.
        var keep = new HashSet<RuntimeCollectionKey>(other._indexByKey.Keys);
        var removed = new List<RuntimeCollectionKey>();
        var inspected = 0;
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            CheckCancellation(cancellation, ++inspected);

            if (keep.Contains(entry.Value))
            {
                continue;
            }

            removed.Add(entry.Value);
        }

        // Вторая фаза: удаление после полного расчёта. Отмена здесь не
        // опрашивается, чтобы получатель не остался изменённым частично.
        foreach (var key in removed)
        {
            _indexByKey.Remove(key, out var index);
            _entries[index] = null;
            _liveCount--;
            _removedCount++;
        }

        if (_removedCount > _liveCount)
        {
            Compact();
        }
    }

    /// <summary>
    /// Копия для снимка состояния отладчика: элементы сохраняют порядок, а
    /// значения копируются отдельным обходом, чтобы общие ссылки не терялись.
    /// </summary>
    public RuntimeSetValue CreateEmptyForSnapshot()
    {
        return new RuntimeSetValue(Type);
    }

    internal RuntimeSetValue CreateEmptyForValueCopy() => new(Type, _maxEntryCount);

    /// <summary>
    /// Добавляет элемент при восстановлении снимка без проверки повтора:
    /// снимок уже содержит согласованный набор элементов.
    /// </summary>
    public void AppendForSnapshot(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var normalized = NormalizeElement(value);
        if (_indexByKey.ContainsKey(normalized))
        {
            return;
        }

        // Снимок уже содержит согласованный набор элементов, поэтому повторное
        // копирование значений не выполняется: общие ссылки сохраняет обход графа.
        Append(normalized);
    }

    public IEnumerable<RuntimeValue> Entries()
    {
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            yield return entry.Value.ToRuntimeValue(Type.ElementType);
        }
    }

    private void EnsureSameType(RuntimeSetValue other)
    {
        if (Type != other.Type)
        {
            throw new ArgumentException(
                $"Операция над множествами требует совпадения типов: получатель {Type.Name}, аргумент {other.Type.Name}.",
                nameof(other));
        }
    }

    /// <summary>
    /// Опрос отмены внутри собственного обхода. Проверка выполняется не на
    /// каждой итерации, а раз в <see cref="CancellationCheckInterval"/> элементов:
    /// частый опрос дороже самого обхода.
    /// </summary>
    private static void CheckCancellation(IRuntimeCancellationProvider? cancellation, int inspected)
    {
        if (cancellation is null || inspected % CancellationCheckInterval != 0)
        {
            return;
        }

        if (cancellation.IsCancellationRequested)
        {
            throw new SetOperationCancelledException();
        }
    }

    private void Append(RuntimeCollectionKey key)
    {
        if (_liveCount >= _maxEntryCount)
        {
            throw new SetEntryLimitException(
                $"Не удалось добавить элемент в множество: число элементов превышает предел {_maxEntryCount}.");
        }

        _entries.Add(key);
        _indexByKey[key] = _entries.Count - 1;
        _liveCount++;
    }

    private void Compact()
    {
        var compacted = new List<RuntimeCollectionKey?>(_liveCount);
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            _indexByKey[entry.Value] = compacted.Count;
            compacted.Add(entry);
        }

        _entries.Clear();
        _entries.AddRange(compacted);
        _removedCount = 0;
    }

    private RuntimeCollectionKey NormalizeElement(RuntimeValue value)
    {
        return RuntimeCollectionKey.Create(value, Type.ElementType);
    }
}

/// <summary>
/// Число элементов множества превысило предел коллекции. Отказ перехватывается
/// обычным TRY/CATCH как ошибка времени выполнения.
/// </summary>
public sealed class SetEntryLimitException : Exception
{
    public SetEntryLimitException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Длительная операция множества прервана запросом отмены. Исключение
/// возникает только в чистой фазе, поэтому получатель не изменён: вызывающий
/// код прекращает выполнение, как при обычной отмене VM.
/// </summary>
public sealed class SetOperationCancelledException : Exception
{
    public SetOperationCancelledException()
        : base("Операция над множеством прервана запросом отмены.")
    {
    }
}
