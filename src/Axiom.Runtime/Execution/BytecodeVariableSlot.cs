using System.Runtime.CompilerServices;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal sealed class BytecodeVariableSlot
{
    public BytecodeVariableSlot(RuntimeValue value, int? fixedStringLength = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        FixedStringLength = fixedStringLength;
        Value = fixedStringLength is null && value.Type is not UserDefinedTypeSymbol
            ? value
            : NormalizeForStorage(value, fixedStringLength);
    }

    public int? FixedStringLength { get; }

    public RuntimeValue Value { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Store(RuntimeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = FixedStringLength is null && value.Type is not UserDefinedTypeSymbol
            ? value
            : NormalizeForStorage(value, FixedStringLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RuntimeValue NormalizeForStorage(RuntimeValue value, int? fixedStringLength)
    {
        if (fixedStringLength is null && value.Type is not UserDefinedTypeSymbol)
        {
            return value;
        }

        return value.CopyForStorage(fixedStringLength);
    }
}
