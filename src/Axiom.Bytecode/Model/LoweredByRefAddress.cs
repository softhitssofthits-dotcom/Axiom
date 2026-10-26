using Axiom.Semantics;

namespace Axiom.Bytecode;

/// <summary>
/// Виды источника BYREF-аргумента в инструкции CallByRef. Значения 0..3
/// описывают простую переменную, значение <see cref="Address"/> — адресуемый
/// получатель с цепочкой полей и элементов.
/// </summary>
public static class ByRefArgumentSourceKind
{
    public const int Global = 0;
    public const int Argument = 1;
    public const int Local = 2;
    public const int Capture = 3;
    public const int Address = 6;
}

/// <summary>
/// Шаг адреса получателя BYREF-аргумента после базовой переменной:
/// поле записи, элемент массива или значение словаря по ключу.
/// </summary>
public sealed class LoweredByRefAddressStep
{
    private LoweredByRefAddressStep(bool isField, bool isDictionaryValue)
    {
        IsField = isField;
        IsDictionaryValue = isDictionaryValue;
    }

    public bool IsField { get; }

    /// <summary>
    /// Шаг обращается к значению словаря по ключу: запись идёт в фактическую
    /// запись словаря, поэтому метод TYPE видит живой получатель.
    /// </summary>
    public bool IsDictionaryValue { get; }

    public string? FieldName { get; private init; }

    public IReadOnlyList<VariableSymbol> IndexVariables { get; private init; } = Array.Empty<VariableSymbol>();

    public VariableSymbol? KeyVariable { get; private init; }

    public static LoweredByRefAddressStep Field(string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        return new LoweredByRefAddressStep(isField: true, isDictionaryValue: false) { FieldName = fieldName };
    }

    public static LoweredByRefAddressStep Element(IReadOnlyList<VariableSymbol> indexVariables)
    {
        ArgumentNullException.ThrowIfNull(indexVariables);
        ArgumentOutOfRangeException.ThrowIfZero(indexVariables.Count);
        return new LoweredByRefAddressStep(isField: false, isDictionaryValue: false) { IndexVariables = indexVariables.ToArray() };
    }

    /// <summary>
    /// Ключ вычисляется один раз при вызове и хранится в локальной временной
    /// переменной: адрес не повторяет вычисление при каждом чтении.
    /// </summary>
    public static LoweredByRefAddressStep DictionaryValue(VariableSymbol keyVariable)
    {
        ArgumentNullException.ThrowIfNull(keyVariable);
        return new LoweredByRefAddressStep(isField: false, isDictionaryValue: true) { KeyVariable = keyVariable };
    }
}

/// <summary>
/// Адресуемый получатель BYREF-аргумента: базовая переменная и цепочка полей
/// и элементов массива. Позволяет передать поле или элемент массива без копии
/// всей записи и без отложенной записи значения обратно после возврата.
/// </summary>
public sealed class LoweredByRefAddress
{
    public LoweredByRefAddress(VariableSymbol baseVariable, IReadOnlyList<LoweredByRefAddressStep> steps)
    {
        ArgumentNullException.ThrowIfNull(baseVariable);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentOutOfRangeException.ThrowIfZero(steps.Count);
        BaseVariable = baseVariable;
        Steps = steps.ToArray();
    }

    public VariableSymbol BaseVariable { get; }

    public IReadOnlyList<LoweredByRefAddressStep> Steps { get; }
}
