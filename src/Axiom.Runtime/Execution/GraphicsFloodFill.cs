namespace Axiom.Runtime;

public static class GraphicsFloodFill
{
    public static void Paint(
        long startX,
        long startY,
        int fillColor,
        int borderColor,
        Func<long, long, int> getPixel,
        Action<long, long, int> setPixel)
    {
        ArgumentNullException.ThrowIfNull(getPixel);
        ArgumentNullException.ThrowIfNull(setPixel);

        fillColor = NormalizeColor(fillColor);
        borderColor = NormalizeColor(borderColor);

        var startColor = NormalizeColor(getPixel(startX, startY));
        if (startColor == fillColor || startColor == borderColor)
        {
            return;
        }

        var pending = new Stack<(long X, long Y)>();
        pending.Push((startX, startY));

        while (pending.Count > 0)
        {
            var (x, y) = pending.Pop();
            var currentColor = NormalizeColor(getPixel(x, y));
            if (currentColor == fillColor || currentColor == borderColor)
            {
                continue;
            }

            setPixel(x, y, fillColor);
            pending.Push((x - 1, y));
            pending.Push((x + 1, y));
            pending.Push((x, y - 1));
            pending.Push((x, y + 1));
        }
    }

    private static int NormalizeColor(int color) => (int)(Math.Abs((long)color) % 256);
}
