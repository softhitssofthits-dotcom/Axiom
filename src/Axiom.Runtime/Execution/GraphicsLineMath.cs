namespace Axiom.Runtime;

public static class GraphicsLineMath
{
    public static long SaturatingAdd(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right)
        {
            return long.MaxValue;
        }

        if (right < 0 && left < long.MinValue - right)
        {
            return long.MinValue;
        }

        return left + right;
    }

    public static long SaturatingDouble(long value)
    {
        if (value > long.MaxValue / 2)
        {
            return long.MaxValue;
        }

        if (value < long.MinValue / 2)
        {
            return long.MinValue;
        }

        return value * 2;
    }
}
