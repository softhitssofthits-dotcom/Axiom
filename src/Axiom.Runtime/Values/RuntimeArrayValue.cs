using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed class RuntimeArrayValue : IRuntimeStoredValue
{
    private RuntimeValue[]? _elements;

    public RuntimeArrayValue(TypeSymbol elementType, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(elementType);
        ElementType = elementType;
        FixedStringLength = fixedStringLength;
    }

    public RuntimeArrayValue(TypeSymbol elementType, int? fixedStringLength, int lowerBound, int upperBound)
    {
        ArgumentNullException.ThrowIfNull(elementType);

        ElementType = elementType;
        FixedStringLength = fixedStringLength;
        Allocate(lowerBound, upperBound);
    }

    public TypeSymbol ElementType { get; }

    public int? FixedStringLength { get; }

    public int LowerBound { get; private set; }

    public int UpperBound { get; private set; }

    public bool IsAllocated => _elements is not null;

    public int Length => IsAllocated ? _elements!.Length : 0;

    public bool ContainsIndex(int index) => IsAllocated && index >= LowerBound && index <= UpperBound;

    public RuntimeValue GetValue(int index)
    {
        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }

        return _elements![index - LowerBound];
    }

    public void SetValue(int index, RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!IsAllocated)
        {
            throw new InvalidOperationException("Array is not allocated.");
        }

        _elements![index - LowerBound] = value.CopyForStorage(FixedStringLength);
    }

    public void Allocate(int lowerBound, int upperBound)
    {
        ValidateBounds(lowerBound, upperBound);

        LowerBound = lowerBound;
        UpperBound = upperBound;
        _elements = CreateDefaultElements(lowerBound, upperBound);
    }

    public void AllocatePreserving(int lowerBound, int upperBound)
    {
        ValidateBounds(lowerBound, upperBound);

        var previousElements = _elements;
        var previousLowerBound = LowerBound;
        var previousUpperBound = UpperBound;
        var newElements = CreateDefaultElements(lowerBound, upperBound);

        if (previousElements is not null)
        {
            var copyLowerBound = Math.Max(previousLowerBound, lowerBound);
            var copyUpperBound = Math.Min(previousUpperBound, upperBound);

            ForEachIndexAscending(copyLowerBound, copyUpperBound, index =>
            {
                newElements[index - lowerBound] = previousElements[index - previousLowerBound];
            });
        }

        LowerBound = lowerBound;
        UpperBound = upperBound;
        _elements = newElements;
    }

    public void Deallocate()
    {
        LowerBound = 0;
        UpperBound = 0;
        _elements = null;
    }

    public RuntimeArrayValue Clone(int? fixedStringLengthOverride = null)
    {
        var clone = new RuntimeArrayValue(ElementType, fixedStringLengthOverride ?? FixedStringLength);

        if (!IsAllocated)
        {
            return clone;
        }

        clone.LowerBound = LowerBound;
        clone.UpperBound = UpperBound;
        clone._elements = new RuntimeValue[_elements!.Length];

        for (var index = 0; index < _elements.Length; index++)
        {
            clone._elements[index] = _elements[index].CopyForStorage(clone.FixedStringLength);
        }

        return clone;
    }

    private static void ValidateBounds(int lowerBound, int upperBound)
    {
        if (IsEmptyRange(lowerBound, upperBound))
        {
            return;
        }

        if (upperBound < lowerBound)
        {
            throw new ArgumentOutOfRangeException(
                nameof(upperBound),
                upperBound,
                "Upper bound must not be less than lower bound, except for the canonical empty range 1 TO 0.");
        }
    }

    private static bool IsEmptyRange(int lowerBound, int upperBound) => lowerBound == 1 && upperBound == 0;

    private static void ForEachIndexAscending(int lowerBound, int upperBound, Action<int> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (upperBound < lowerBound)
        {
            return;
        }

        var index = lowerBound;

        while (true)
        {
            action(index);

            if (index == upperBound)
            {
                break;
            }

            index++;
        }
    }

    private RuntimeValue[] CreateDefaultElements(int lowerBound, int upperBound)
    {
        var elements = IsEmptyRange(lowerBound, upperBound)
            ? []
            : new RuntimeValue[upperBound - lowerBound + 1];

        for (var index = 0; index < elements.Length; index++)
        {
            elements[index] = RuntimeValue.Default(ElementType, FixedStringLength);
        }

        return elements;
    }
}
