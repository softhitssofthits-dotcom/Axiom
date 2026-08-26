namespace Axiom.Runtime;

/// <summary>
/// Single source of truth for color normalization (ABS(color) MOD 256) used by
/// the Axiom graphics subsystem.
///
/// Consolidates four independent implementations that previously lived in
/// GraphicsDrawExecutor, GraphicsFloodFill and GraphicsBufferTransfer
/// (audit SYS-G-003 / TASK-G-03). The integral path is safe for long.MinValue:
/// it uses unsigned arithmetic instead of Math.Abs, which would throw
/// OverflowException for long.MinValue.
/// </summary>
public static class GraphicsColorNormalization
{
    /// <summary>
    /// Normalizes a 64-bit integral color value into the range [0, 255]
    /// using ABS(value) MOD 256. Never throws, including for long.MinValue.
    /// </summary>
    public static int Normalize(long value)
    {
        ulong magnitude = value switch
        {
            >= 0 => (ulong)value,
            long.MinValue => (ulong)long.MaxValue + 1UL,
            _ => (ulong)(-value)
        };

        return (int)(magnitude % 256UL);
    }

    /// <summary>
    /// Normalizes a 32-bit integral color value into the range [0, 255].
    /// </summary>
    public static int Normalize(int value) => Normalize((long)value);

    /// <summary>
    /// Normalizes a decimal color value into the range [0, 255].
    /// Decimal has no non-finite states, so this always succeeds.
    /// </summary>
    public static int Normalize(decimal value) =>
        (int)(decimal.Abs(decimal.Truncate(value)) % 256m);

    /// <summary>
    /// Normalizes a double color value into the range [0, 255].
    /// Returns false (and sets color to 0) for non-finite values.
    /// </summary>
    public static bool TryNormalize(double value, out int color)
    {
        if (!double.IsFinite(value))
        {
            color = 0;
            return false;
        }

        color = (int)(Math.Abs(Math.Truncate(value)) % 256d);
        return true;
    }
}
