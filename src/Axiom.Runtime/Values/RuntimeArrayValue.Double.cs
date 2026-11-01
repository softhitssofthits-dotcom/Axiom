using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetDouble1(int index0, out double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex1(index0, out var linearIndex))
        {
            value = 0d;
            return false;
        }
        value = _elements!.ReadDouble(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetDouble2(int index0, int index1, out double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex2(index0, index1, out var linearIndex))
        {
            value = 0d;
            return false;
        }
        value = _elements!.ReadDouble(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetDouble3(int index0, int index1, int index2, out double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
        {
            value = 0d;
            return false;
        }
        value = _elements!.ReadDouble(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetDouble1(int index0, double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex1(index0, out var linearIndex))
            return false;
        _elements!.WriteDouble(linearIndex, value);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetDouble2(int index0, int index1, double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex2(index0, index1, out var linearIndex))
            return false;
        _elements!.WriteDouble(linearIndex, value);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetDouble3(int index0, int index1, int index2, double value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Double || !TryGetLinearIndex3(index0, index1, index2, out var linearIndex))
            return false;
        _elements!.WriteDouble(linearIndex, value);
        return true;
    }
}
