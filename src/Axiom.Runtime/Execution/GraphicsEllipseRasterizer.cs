namespace Axiom.Runtime;

public static class GraphicsEllipseRasterizer
{
    private const double FullTurn = Math.Tau;
    private const double AngleEpsilon = 1e-9;

    public static void Rasterize(
        long centerX,
        long centerY,
        double radiusX,
        double radiusY,
        bool fullEllipse,
        double arcStart,
        double sweep,
        int screenWidth,
        int screenHeight,
        Action<long, long> plotPixel)
    {
        ArgumentNullException.ThrowIfNull(plotPixel);

        if (!double.IsFinite(radiusX) || !double.IsFinite(radiusY) || radiusX <= 0d || radiusY <= 0d)
        {
            return;
        }

        var rx = RoundRadius(radiusX);
        var ry = RoundRadius(radiusY);
        if (rx <= 0 || ry <= 0)
        {
            return;
        }

        var rxSq = (double)rx * rx;
        var rySq = (double)ry * ry;
        var maxIterations = ComputeMaxIterations(rx, ry, fullEllipse, sweep, screenWidth, screenHeight);
        if ((double)rx + ry > maxIterations)
        {
            RasterizeVisibleBoundary();
            return;
        }
        var iterationCount = 0;

        long x = 0;
        long y = ry;
        var dx = 0d;
        var dy = 2d * rxSq * y;
        var decisionRegion1 = rySq - (rxSq * ry) + (0.25d * rxSq);

        PlotSymmetricPoints(centerX, centerY, x, y, rx, ry, fullEllipse, arcStart, sweep, screenWidth, screenHeight, plotPixel);

        while (dx < dy)
        {
            if (++iterationCount > maxIterations)
            {
                break;
            }

            x++;
            dx += 2d * rySq;
            if (decisionRegion1 < 0d)
            {
                decisionRegion1 += dx + rySq;
            }
            else
            {
                y--;
                dy -= 2d * rxSq;
                decisionRegion1 += dx - dy + rySq;
            }

            PlotSymmetricPoints(centerX, centerY, x, y, rx, ry, fullEllipse, arcStart, sweep, screenWidth, screenHeight, plotPixel);
        }

        void RasterizeVisibleBoundary()
        {
            // На каждой половине четверти перебираем ведущую видимую ось.
            // Работа зависит от поверхности, а не от удалённого центра и радиуса.
            for (var column = 0; column < screenWidth; column++)
            {
                var distance = Math.Abs((double)((Int128)column - centerX));
                if (distance > rx) continue;
                var offset = ry * Math.Sqrt(Math.Max(0, ((rx - distance) / rx) * (1 + distance / rx)));
                if (distance * rySq > offset * rxSq) continue;
                var rounded = RoundRadius(offset);
                Plot(column, (Int128)centerY - rounded);
                Plot(column, (Int128)centerY + rounded);
            }
            for (var row = 0; row < screenHeight; row++)
            {
                var distance = Math.Abs((double)((Int128)row - centerY));
                if (distance > ry) continue;
                var offset = rx * Math.Sqrt(Math.Max(0, ((ry - distance) / ry) * (1 + distance / ry)));
                if (offset * rySq < distance * rxSq) continue;
                var rounded = RoundRadius(offset);
                Plot((Int128)centerX - rounded, row);
                Plot((Int128)centerX + rounded, row);
            }

            void Plot(Int128 px, Int128 py)
            {
                if (px < 0 || py < 0 || px >= screenWidth || py >= screenHeight) return;
                var pointX = (long)px;
                var pointY = (long)py;
                if (fullEllipse || IsAngleInSweep(centerX, centerY, pointX, pointY, rx, ry, arcStart, sweep))
                    plotPixel(pointX, pointY);
            }
        }

        var decisionRegion2 =
            (rySq * Math.Pow(x + 0.5d, 2d)) +
            (rxSq * Math.Pow(y - 1d, 2d)) -
            (rxSq * rySq);

        while (y > 0)
        {
            if (++iterationCount > maxIterations)
            {
                break;
            }

            y--;
            dy -= 2d * rxSq;
            if (decisionRegion2 > 0d)
            {
                decisionRegion2 += rxSq - dy;
            }
            else
            {
                x++;
                dx += 2d * rySq;
                decisionRegion2 += dx - dy + rxSq;
            }

            PlotSymmetricPoints(centerX, centerY, x, y, rx, ry, fullEllipse, arcStart, sweep, screenWidth, screenHeight, plotPixel);
        }
    }

    public static double NormalizeAngle(double angle)
    {
        if (!double.IsFinite(angle))
        {
            return 0d;
        }

        var normalized = angle % FullTurn;
        return normalized < 0d ? normalized + FullTurn : normalized;
    }

    public static (long X, long Y) GetEllipsePoint(long centerX, long centerY, double radiusX, double radiusY, double angle)
    {
        return (
            centerX + (long)Math.Round(Math.Cos(angle) * radiusX, MidpointRounding.AwayFromZero),
            centerY - (long)Math.Round(Math.Sin(angle) * radiusY, MidpointRounding.AwayFromZero));
    }

    public static int ComputeMaxIterations(long radiusX, long radiusY, bool fullEllipse, double sweep, int screenWidth, int screenHeight)
    {
        var safeWidth = Math.Max(1L, screenWidth);
        var safeHeight = Math.Max(1L, screenHeight);
        var fullEllipseBudget = Math.Max(1d, Math.Abs((double)radiusX) + Math.Abs((double)radiusY));
        // Обход начинается с верхней точки независимо от угла дуги.
        // Для короткой дуги тоже нужно дойти до конца четверти эллипса.
        var requestedBudget = Math.Max(8d, Math.Ceiling(fullEllipseBudget));

        // Keep the guard proportional to the visible screen size so pathological radii do not
        // turn a defensive limit into a near-infinite loop on a small surface.
        var screenBudget = Math.Max(64d, 16d * 2d * (safeWidth + safeHeight));
        return ClampIterationBudget(Math.Min(requestedBudget, screenBudget));
    }

    private static void PlotSymmetricPoints(
        long centerX,
        long centerY,
        long x,
        long y,
        long radiusX,
        long radiusY,
        bool fullEllipse,
        double arcStart,
        double sweep,
        int screenWidth,
        int screenHeight,
        Action<long, long> plotPixel)
    {
        PlotCandidate(centerX + x, centerY - y);
        PlotCandidate(centerX - x, centerY - y);
        PlotCandidate(centerX - x, centerY + y);
        PlotCandidate(centerX + x, centerY + y);

        void PlotCandidate(long pointX, long pointY)
        {
            if ((fullEllipse || IsAngleInSweep(centerX, centerY, pointX, pointY, radiusX, radiusY, arcStart, sweep)) &&
                pointX >= 0 &&
                pointY >= 0 &&
                pointX < screenWidth &&
                pointY < screenHeight)
            {
                plotPixel(pointX, pointY);
            }
        }
    }

    private static bool IsAngleInSweep(
        long centerX,
        long centerY,
        long pointX,
        long pointY,
        long radiusX,
        long radiusY,
        double arcStart,
        double sweep)
    {
        if (sweep >= FullTurn - AngleEpsilon)
        {
            return true;
        }

        var normalizedX = (double)((Int128)pointX - centerX) / radiusX;
        var normalizedY = (double)((Int128)centerY - pointY) / radiusY;
        var angle = NormalizeAngle(Math.Atan2(normalizedY, normalizedX));
        var delta = angle - arcStart;
        if (delta < 0d)
        {
            delta += FullTurn;
        }

        return delta <= sweep + AngleEpsilon;
    }

    private static long RoundRadius(double value)
    {
        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded >= long.MaxValue)
        {
            return long.MaxValue;
        }

        if (rounded <= long.MinValue)
        {
            return long.MinValue;
        }

        return (long)rounded;
    }

    private static double EstimateEllipsePerimeter(long radiusX, long radiusY)
    {
        var a = Math.Abs((double)radiusX);
        var b = Math.Abs((double)radiusY);
        if (a == 0d || b == 0d)
        {
            return 0d;
        }

        var h = Math.Pow(a - b, 2d) / Math.Pow(a + b, 2d);
        return Math.PI * (a + b) * (1d + (3d * h) / (10d + Math.Sqrt(4d - 3d * h)));
    }

    private static int ClampIterationBudget(double budget)
    {
        if (budget >= int.MaxValue)
        {
            return int.MaxValue;
        }

        return Math.Max(8, (int)Math.Ceiling(budget));
    }
}
