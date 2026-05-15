namespace Axiom.Runtime;

public static class GraphicsFloodFill
{
    public static void Paint(
        GraphicsPaintRequest request,
        Func<long, long, int> getPixel,
        Action<long, long, int> setPixel,
        Func<long, long, int, int, bool> isInsideScreen,
        int screenWidth,
        int screenHeight)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(getPixel);
        ArgumentNullException.ThrowIfNull(setPixel);
        ArgumentNullException.ThrowIfNull(isInsideScreen);

        if (!isInsideScreen(request.X, request.Y, screenWidth, screenHeight))
        {
            return;
        }

        var borderColor = NormalizeColor(request.BorderColor);
        var startColor = NormalizeColor(getPixel(request.X, request.Y));
        if (request.UsesPattern)
        {
            PaintPattern(
                request,
                borderColor,
                startColor,
                getPixel,
                setPixel,
                isInsideScreen,
                screenWidth,
                screenHeight);
            return;
        }

        PaintSolid(
            request,
            borderColor,
            startColor,
            getPixel,
            setPixel,
            isInsideScreen,
            screenWidth,
            screenHeight);
    }

    private static void PaintSolid(
        GraphicsPaintRequest request,
        int borderColor,
        int startColor,
        Func<long, long, int> getPixel,
        Action<long, long, int> setPixel,
        Func<long, long, int, int, bool> isInsideScreen,
        int screenWidth,
        int screenHeight)
    {
        var fillColor = NormalizeColor(request.SolidFillColor ?? 0);
        if (startColor == fillColor || startColor == borderColor)
        {
            return;
        }

        Flood(
            request.X,
            request.Y,
            getPixel,
            setPixel,
            isInsideScreen,
            screenWidth,
            screenHeight,
            (_, _, currentColor) => currentColor != borderColor && currentColor != fillColor,
            (_, _) => fillColor);
    }

    private static void PaintPattern(
        GraphicsPaintRequest request,
        int borderColor,
        int startColor,
        Func<long, long, int> getPixel,
        Action<long, long, int> setPixel,
        Func<long, long, int, int, bool> isInsideScreen,
        int screenWidth,
        int screenHeight)
    {
        var fillPatternRows = request.FillPatternRows!;
        var fillOneColor = NormalizeColor(request.PatternForegroundColor);
        var fillZeroColor = NormalizeColor(request.PatternBackgroundColor);
        var startTargetColor = GetPatternColor(fillPatternRows, request.X, request.Y, fillOneColor, fillZeroColor);
        if (startColor == borderColor)
        {
            return;
        }

        if (request.BackgroundPatternRows is null && startColor == startTargetColor)
        {
            return;
        }

        Flood(
            request.X,
            request.Y,
            getPixel,
            setPixel,
            isInsideScreen,
            screenWidth,
            screenHeight,
            (x, y, currentColor) =>
            {
                if (currentColor == borderColor)
                {
                    return false;
                }

                if (request.BackgroundPatternRows is null)
                {
                    return currentColor != GetPatternColor(fillPatternRows, x, y, fillOneColor, fillZeroColor);
                }

                var backgroundColor = GetPatternColor(request.BackgroundPatternRows, x, y, fillOneColor, fillZeroColor);
                return currentColor == startColor || currentColor == backgroundColor;
            },
            (x, y) => GetPatternColor(fillPatternRows, x, y, fillOneColor, fillZeroColor));
    }

    private static int NormalizeColor(int color) => (int)(Math.Abs((long)color) % 256);

    private static void Flood(
        long startX,
        long startY,
        Func<long, long, int> getPixel,
        Action<long, long, int> setPixel,
        Func<long, long, int, int, bool> isInsideScreen,
        int screenWidth,
        int screenHeight,
        Func<long, long, int, bool> canPaint,
        Func<long, long, int> getFillColor)
    {
        var pending = new Stack<(long X, long Y)>();
        pending.Push((startX, startY));

        while (pending.Count > 0)
        {
            var (seedX, seedY) = pending.Pop();
            if (!CanPaint(seedX, seedY))
            {
                continue;
            }

            var left = seedX;
            while (CanPaint(left - 1, seedY))
            {
                left--;
            }

            var right = seedX;
            while (CanPaint(right + 1, seedY))
            {
                right++;
            }

            for (var x = left; x <= right; x++)
            {
                setPixel(x, seedY, getFillColor(x, seedY));
            }

            EnqueueNeighborSpans(left, right, seedY - 1);
            EnqueueNeighborSpans(left, right, seedY + 1);
        }

        bool CanPaint(long x, long y)
        {
            if (!isInsideScreen(x, y, screenWidth, screenHeight))
            {
                return false;
            }

            var currentColor = NormalizeColor(getPixel(x, y));
            return canPaint(x, y, currentColor);
        }

        void EnqueueNeighborSpans(long left, long right, long y)
        {
            if (y < 0 || y >= screenHeight)
            {
                return;
            }

            var x = left;
            while (x <= right)
            {
                while (x <= right && !CanPaint(x, y))
                {
                    x++;
                }

                if (x > right)
                {
                    break;
                }

                pending.Push((x, y));
                x++;
                while (x <= right && CanPaint(x, y))
                {
                    x++;
                }
            }
        }
    }

    private static int GetPatternColor(byte[] rows, long x, long y, int oneColor, int zeroColor)
    {
        var row = rows[PositiveModulo(y, rows.Length)];
        var bitIndex = 7 - PositiveModulo(x, 8);
        return ((row >> bitIndex) & 1) != 0 ? oneColor : zeroColor;
    }

    private static int PositiveModulo(long value, int modulus)
    {
        var result = (int)(value % modulus);
        return result < 0 ? result + modulus : result;
    }
}
