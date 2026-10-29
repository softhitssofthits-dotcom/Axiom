using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class RuntimeArrayValue
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetLong1(int index0, out long value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Long || !TryGetLinearIndex1(index0, out var linearIndex))
        {
            value = 0L;
            return false;
        }
        value = _elements!.ReadLong(linearIndex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TrySetLong1(int index0, long value)
    {
        EnsureAllocated();
        if (ElementType != TypeSymbol.Long || !TryGetLinearIndex1(index0, out var linearIndex))
            return false;
        _elements!.WriteLong(linearIndex, value);
        return true;
    }
}
