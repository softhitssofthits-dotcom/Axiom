using Axiom.Semantics;

namespace Axiom.Runtime;

/// <summary>
/// Address of one array element captured by a FOR EACH BYREF iteration.
/// </summary>
internal sealed class BytecodeArrayElementVariableSlot : BytecodeVariableSlot
{
    public BytecodeArrayElementVariableSlot(
        ArrayTypeSymbol arrayType,
        RuntimeArrayValue array,
        IReadOnlyList<int> indices,
        long expectedStructuralVersion)
        : base(
            () => Read(array, indices, expectedStructuralVersion),
            value => Write(array, indices, expectedStructuralVersion, value),
            array.FixedStringLength,
            isForEachItem: true)
    {
        ArgumentNullException.ThrowIfNull(arrayType);
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(indices);

        ArrayType = arrayType;
        Array = array;
        Indices = indices.ToArray();
        ExpectedStructuralVersion = expectedStructuralVersion;
    }

    public ArrayTypeSymbol ArrayType { get; }

    public RuntimeArrayValue Array { get; }

    public IReadOnlyList<int> Indices { get; }

    public long ExpectedStructuralVersion { get; }

    private static RuntimeValue Read(RuntimeArrayValue array, IReadOnlyList<int> indices, long expectedVersion)
    {
        EnsureCurrent(array, expectedVersion);
        return array.GetValue(indices);
    }

    private static void Write(RuntimeArrayValue array, IReadOnlyList<int> indices, long expectedVersion, RuntimeValue value)
    {
        EnsureCurrent(array, expectedVersion);
        array.SetValue(indices, value);
    }

    private static void EnsureCurrent(RuntimeArrayValue array, long expectedVersion)
    {
        if (!array.IsStructuralVersionCurrent(expectedVersion))
        {
            throw new ExpiredArrayViewException();
        }
    }
}
