namespace Axiom.Runtime;

internal static class GraphicsDrawExecutor
{
    public static void Execute(IGraphicsHost host, string commandText)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(commandText);

        var commands = GraphicsDrawParser.Parse(commandText);
        var color = host.CurrentForegroundColor;
        var scale = 4d;
        var angleDegrees = 0d;

        foreach (var command in commands)
        {
            switch (command.Kind)
            {
                case GraphicsDrawCommandKind.Move:
                    ExecuteDirectionalMove(host, command, color, scale, angleDegrees);
                    break;
                case GraphicsDrawCommandKind.SetColor:
                    color = NormalizeColor(command.NumericValue, command.Position);
                    break;
                case GraphicsDrawCommandKind.SetScale:
                    if (!double.IsFinite(command.NumericValue) || command.NumericValue <= 0)
                    {
                        throw new GraphicsDrawException("Команда S требует положительный конечный масштаб.", command.Position);
                    }

                    scale = command.NumericValue;
                    break;
                case GraphicsDrawCommandKind.SetAngleQuadrant:
                    if (!double.IsFinite(command.NumericValue) || command.NumericValue < 0 || command.NumericValue > 3 || command.NumericValue != Math.Truncate(command.NumericValue))
                    {
                        throw new GraphicsDrawException("Команда A допускает только значения 0, 1, 2 или 3.", command.Position);
                    }

                    angleDegrees = command.NumericValue * 90d;
                    break;
                case GraphicsDrawCommandKind.SetAngleDegrees:
                    if (!double.IsFinite(command.NumericValue))
                    {
                        throw new GraphicsDrawException("Команда TA требует конечный числовой угол.", command.Position);
                    }

                    angleDegrees = command.NumericValue;
                    break;
                case GraphicsDrawCommandKind.MoveTo:
                    ExecuteMoveTo(host, command, color);
                    break;
                case GraphicsDrawCommandKind.Paint:
                    ExecutePaint(host, command);
                    break;
                case GraphicsDrawCommandKind.UnsupportedHistoricalX:
                    throw new GraphicsDrawException("Историческая форма X/VARPTR$ в DRAW не поддерживается в Axiom по проектному решению.", command.Position);
                default:
                    throw new GraphicsDrawException("Встречена неподдерживаемая внутренняя команда DRAW.", command.Position);
            }
        }
    }

    private static void ExecuteDirectionalMove(IGraphicsHost host, GraphicsDrawCommand command, int color, double scale, double angleDegrees)
    {
        var length = command.NumericValue * scale / 4d;
        var (baseDx, baseDy) = command.Direction switch
        {
            GraphicsDrawDirection.Up => (0d, -length),
            GraphicsDrawDirection.Down => (0d, length),
            GraphicsDrawDirection.Left => (-length, 0d),
            GraphicsDrawDirection.Right => (length, 0d),
            GraphicsDrawDirection.UpRight => (length, -length),
            GraphicsDrawDirection.DownRight => (length, length),
            GraphicsDrawDirection.DownLeft => (-length, length),
            GraphicsDrawDirection.UpLeft => (-length, -length),
            _ => throw new GraphicsDrawException("Неизвестное направление DRAW.", command.Position)
        };

        var (dx, dy) = Rotate(baseDx, baseDy, angleDegrees);
        var targetX = CheckedAdd(host.CurrentGraphicsX, RoundToLong(dx, command.Position), command.Position);
        var targetY = CheckedAdd(host.CurrentGraphicsY, RoundToLong(dy, command.Position), command.Position);
        ExecuteSegment(host, targetX, targetY, color, command.Blind, command.ReturnToOrigin);
    }

    private static void ExecuteMoveTo(IGraphicsHost host, GraphicsDrawCommand command, int color)
    {
        var targetX = command.IsRelativeMove ? CheckedAdd(host.CurrentGraphicsX, command.X, command.Position) : command.X;
        var targetY = command.IsRelativeMove ? CheckedAdd(host.CurrentGraphicsY, command.Y, command.Position) : command.Y;
        ExecuteSegment(host, targetX, targetY, color, command.Blind, command.ReturnToOrigin);
    }

    private static void ExecutePaint(IGraphicsHost host, GraphicsDrawCommand command)
    {
        var fillColor = NormalizeColor(command.FillColor, command.Position);
        var borderColor = NormalizeColor(command.BorderColor ?? fillColor, command.Position);
        host.Paint(GraphicsPaintRequest.CreateSolid(
            host.CurrentGraphicsX,
            host.CurrentGraphicsY,
            fillColor,
            borderColor));
    }

    private static void ExecuteSegment(IGraphicsHost host, long targetX, long targetY, int color, bool blind, bool returnToOrigin)
    {
        var originX = host.CurrentGraphicsX;
        var originY = host.CurrentGraphicsY;
        if (blind)
        {
            host.SetGraphicsCurrentPosition(targetX, targetY);
        }
        else
        {
            host.DrawLine(originX, originY, targetX, targetY, color, GraphicsLineKind.Normal);
        }

        if (returnToOrigin)
        {
            host.SetGraphicsCurrentPosition(originX, originY);
        }
    }

    private static (double X, double Y) Rotate(double x, double y, double angleDegrees)
    {
        if (angleDegrees == 0)
        {
            return (x, y);
        }

        var radians = angleDegrees * Math.PI / 180d;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return (
            x * cos - y * sin,
            x * sin + y * cos);
    }

    private static long RoundToLong(double value, int position)
    {
        if (!double.IsFinite(value))
        {
            throw new GraphicsDrawException("Команда DRAW привела к неконечному смещению.", position);
        }

        var rounded = Math.Round(value, MidpointRounding.AwayFromZero);
        if (rounded >= long.MaxValue || rounded <= long.MinValue)
        {
            throw new GraphicsDrawException("Команда DRAW привела к переполнению координат.", position);
        }

        try
        {
            return checked((long)rounded);
        }
        catch (OverflowException)
        {
            throw new GraphicsDrawException("Команда DRAW привела к переполнению координат.", position);
        }
    }

    private static long CheckedAdd(long left, long right, int position)
    {
        try
        {
            return checked(left + right);
        }
        catch (OverflowException)
        {
            throw new GraphicsDrawException("Команда DRAW привела к переполнению координат.", position);
        }
    }

    private static int NormalizeColor(double value, int position)
    {
        if (!double.IsFinite(value))
        {
            throw new GraphicsDrawException("Команда DRAW требует конечный числовой цвет.", position);
        }

        try
        {
            return GraphicsColorNormalization.Normalize(checked((long)Math.Truncate(value)));
        }
        catch (OverflowException)
        {
            throw new GraphicsDrawException("Команда DRAW содержит слишком большой числовой цвет.", position);
        }
    }
}
