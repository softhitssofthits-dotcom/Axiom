namespace Axiom.Runtime;

public static class GraphicsLineMath
{
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
