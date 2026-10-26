using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Типизированный словарь времени выполнения: ключ однозначно определяет
/// запись, а записи хранятся в порядке добавления. Замена значения сохраняет
/// место записи, удаление и повторное добавление помещают ключ в конец.
/// </summary>
public sealed class RuntimeDictionaryValue
{
    /// <summary>
    /// Предел числа записей следует общему пределу элементов коллекции, чтобы
    /// рост словаря не превращался в необратимый отказ процесса.
    /// </summary>
    internal const int MaxEntryCount = RuntimeArrayValue.MaxElementCount;

    private readonly List<Entry?> _entries = [];
    private readonly Dictionary<DictionaryKey, int> _indexByKey = [];
    private int _liveCount;
    private int _removedCount;

    public RuntimeDictionaryValue(DictionaryTypeSymbol type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Type = type;
    }

    public DictionaryTypeSymbol Type { get; }

    public int Count => _liveCount;

    public bool ContainsKey(RuntimeValue key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _indexByKey.ContainsKey(NormalizeKey(key));
    }

    /// <summary>
    /// Добавляет новую запись. Повторный ключ не создаёт вторую запись и
    /// сообщается вызывающему коду отдельным признаком.
    /// </summary>
    public bool TryAdd(RuntimeValue key, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var normalizedKey = NormalizeKey(key);
        if (_indexByKey.ContainsKey(normalizedKey))
        {
            return false;
        }

        Append(normalizedKey, value);
        return true;
    }

    /// <summary>
    /// Заменяет существующую запись либо добавляет отсутствующую. Замена
    /// сохраняет место в порядке добавления.
    /// </summary>
    public void Set(RuntimeValue key, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var normalizedKey = NormalizeKey(key);
        if (_indexByKey.TryGetValue(normalizedKey, out var index))
        {
            _entries[index] = new Entry(normalizedKey, PrepareValue(value));
            return;
        }

        Append(normalizedKey, value);
    }

    public bool TryGetValue(RuntimeValue key, out RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_indexByKey.TryGetValue(NormalizeKey(key), out var index))
        {
            value = _entries[index]!.Value;
            return true;
        }

        value = RuntimeValue.Void;
        return false;
    }

    /// <summary>
    /// Удаляет запись по ключу. Отсутствующий ключ не является ошибкой:
    /// вызывающий код получает FALSE и продолжает работу.
    /// </summary>
    public bool Remove(RuntimeValue key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var normalizedKey = NormalizeKey(key);
        if (!_indexByKey.Remove(normalizedKey, out var index))
        {
            return false;
        }

        _entries[index] = null;
        _liveCount--;
        _removedCount++;

        // Освободившиеся места убираются, когда их становится больше половины:
        // порядок оставшихся записей при этом не меняется.
        if (_removedCount > _liveCount)
        {
            Compact();
        }

        return true;
    }

    /// <summary>
    /// Ключи в порядке добавления как самостоятельный одномерный массив с
    /// границами 1..Count. Изменение словаря после вызова не меняет результат.
    /// </summary>
    public RuntimeValue CreateKeysArray()
    {
        var keyType = Type.KeyType;
        var array = new RuntimeArrayValue(keyType, fixedStringLength: null, rank: 1);
        array.Allocate([1], [_liveCount]);

        var position = 1;
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            array.TrySetValue1(position, entry.Key.ToRuntimeValue(keyType));
            position++;
        }

        return RuntimeValue.FromArray((ArrayTypeSymbol)ArrayTypeSymbol.Create(keyType), array);
    }

    /// <summary>
    /// Копия для снимка состояния отладчика: записи сохраняют порядок, а
    /// значения копируются отдельным обходом, чтобы общие ссылки не терялись.
    /// </summary>
    public RuntimeDictionaryValue CreateEmptyForSnapshot()
    {
        return new RuntimeDictionaryValue(Type);
    }

    /// <summary>
    /// Добавляет запись при восстановлении снимка без проверки повторного
    /// ключа: снимок уже содержит согласованный набор записей.
    /// </summary>
    public void AppendForSnapshot(RuntimeValue key, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var normalizedKey = NormalizeKey(key);
        if (_indexByKey.ContainsKey(normalizedKey))
        {
            return;
        }

        // Снимок уже содержит согласованные значения: повторное копирование
        // разорвало бы общие ссылки, сохранённые обходом графа.
        Append(normalizedKey, value, prepareValue: false);
    }

    public IEnumerable<(RuntimeValue Key, RuntimeValue Value)> Entries()
    {
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            yield return (entry.Key.ToRuntimeValue(Type.KeyType), entry.Value);
        }
    }

    /// <summary>
    /// Значение приводится к виду хранения по действующим правилам Axiom:
    /// запись TYPE копируется, массив сохраняет обычную общую ссылку, а
    /// накопитель строки превращается в готовую строку.
    /// </summary>
    private static RuntimeValue PrepareValue(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.CopyForStorage();
    }

    private void Append(DictionaryKey key, RuntimeValue value, bool prepareValue = true)
    {
        if (_liveCount >= MaxEntryCount)
        {
            throw new DictionaryEntryLimitException(
                $"Не удалось добавить запись в словарь: число записей превышает предел {MaxEntryCount}.");
        }

        _entries.Add(new Entry(key, prepareValue ? PrepareValue(value) : value));
        _indexByKey[key] = _entries.Count - 1;
        _liveCount++;
    }

    private void Compact()
    {
        var compacted = new List<Entry?>(_liveCount);
        foreach (var entry in _entries)
        {
            if (entry is null)
            {
                continue;
            }

            _indexByKey[entry.Key] = compacted.Count;
            compacted.Add(entry);
        }

        _entries.Clear();
        _entries.AddRange(compacted);
        _removedCount = 0;
    }

    private DictionaryKey NormalizeKey(RuntimeValue key)
    {
        return DictionaryKey.Create(key, Type.KeyType);
    }

    private sealed class Entry
    {
        public Entry(DictionaryKey key, RuntimeValue value)
        {
            Key = key;
            Value = value;
        }

        public DictionaryKey Key { get; }

        public RuntimeValue Value { get; }
    }

    /// <summary>
    /// Ключ словаря в приведённом виде. Строки сравниваются точно, с учётом
    /// регистра, без удаления пробелов и нормализации.
    /// </summary>
    private readonly struct DictionaryKey : IEquatable<DictionaryKey>
    {
        private DictionaryKey(string? text, int integer, long @long, TypeSymbol keyType)
        {
            Text = text;
            Integer = integer;
            Long = @long;
            KeyType = keyType;
        }

        public string? Text { get; }

        public int Integer { get; }

        public long Long { get; }

        public TypeSymbol KeyType { get; }

        public static DictionaryKey Create(RuntimeValue value, TypeSymbol keyType)
        {
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(keyType);

            if (keyType == TypeSymbol.String)
            {
                return new DictionaryKey(value.AsString(), 0, 0L, keyType);
            }

            if (keyType == TypeSymbol.Integer)
            {
                return new DictionaryKey(null, value.AsInteger(), 0L, keyType);
            }

            return new DictionaryKey(null, 0, value.AsLong(), keyType);
        }

        public RuntimeValue ToRuntimeValue(TypeSymbol keyType)
        {
            if (keyType == TypeSymbol.String)
            {
                return RuntimeValue.FromString(Text ?? string.Empty);
            }

            return keyType == TypeSymbol.Integer
                ? RuntimeValue.FromInteger(Integer)
                : RuntimeValue.FromLong(Long);
        }

        public bool Equals(DictionaryKey other)
        {
            if (!ReferenceEquals(KeyType, other.KeyType))
            {
                return false;
            }

            if (KeyType == TypeSymbol.String)
            {
                return string.Equals(Text, other.Text, StringComparison.Ordinal);
            }

            return KeyType == TypeSymbol.Integer
                ? Integer == other.Integer
                : Long == other.Long;
        }

        public override bool Equals(object? obj) => obj is DictionaryKey other && Equals(other);

        public override int GetHashCode()
        {
            if (KeyType == TypeSymbol.String)
            {
                return StringComparer.Ordinal.GetHashCode(Text ?? string.Empty);
            }

            return KeyType == TypeSymbol.Integer
                ? Integer
                : Long.GetHashCode();
        }
    }
}

/// <summary>
/// Число записей словаря превысило предел коллекции. Отказ перехватывается
/// обычным TRY/CATCH как ошибка времени выполнения.
/// </summary>
public sealed class DictionaryEntryLimitException : Exception
{
    public DictionaryEntryLimitException(string message)
        : base(message)
    {
    }
}
