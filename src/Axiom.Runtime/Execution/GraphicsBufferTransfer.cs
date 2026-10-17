using Axiom.Semantics;

namespace Axiom.Runtime;

internal readonly record struct GraphicsRectangleTransferArguments(
    long LeftAbsolute,
    long TopAbsolute,
    long RightAbsolute,
    long BottomAbsolute,
    int Width,
    int Height);

internal readonly record struct GraphicsAnchorTransferArguments(
    long AbsoluteX,
    long AbsoluteY);

internal readonly record struct GraphicsBufferShape(
    int XLowerBound,
    int XUpperBound,
    int YLowerBound,
    int YUpperBound,
    int Width,
    int Height);

internal static class GraphicsBufferTransfer
{
    public static bool TryCreateGetRectangle(
        IGraphicsHost host,
        long currentGraphicsX,
        long currentGraphicsY,
        long x1,
        long y1,
        long x2,
        long y2,
        bool isStartRelative,
        bool isEndRelative,
        out GraphicsRectangleTransferArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(host);
        arguments = default;

        if (isStartRelative
            && !GraphicsCoordinateValidation.TryResolvePoint(currentGraphicsX, currentGraphicsY, x1, y1, out x1, out y1))
        {
            return false;
        }

        if (isEndRelative
            && !GraphicsCoordinateValidation.TryResolvePoint(x1, y1, x2, y2, out x2, out y2))
        {
            return false;
        }

        if (!GraphicsCoordinateValidation.AreSupportedLineCoordinates(x1, y1, x2, y2))
        {
            return false;
        }

        if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, x1, y1, out var absoluteX1, out var absoluteY1)
            || !GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, x2, y2, out var absoluteX2, out var absoluteY2))
        {
            return false;
        }

        var left = Math.Min(absoluteX1, absoluteX2);
        var right = Math.Max(absoluteX1, absoluteX2);
        var top = Math.Min(absoluteY1, absoluteY2);
        var bottom = Math.Max(absoluteY1, absoluteY2);
        if (!TryGetSpanLength(left, right, out var width)
            || !TryGetSpanLength(top, bottom, out var height))
        {
            return false;
        }

        arguments = new GraphicsRectangleTransferArguments(left, top, right, bottom, width, height);
        return true;
    }

    public static bool TryCreatePutAnchor(
        IGraphicsHost host,
        long currentGraphicsX,
        long currentGraphicsY,
        long x,
        long y,
        bool isRelative,
        out GraphicsAnchorTransferArguments arguments)
    {
        ArgumentNullException.ThrowIfNull(host);
        arguments = default;

        if (isRelative
            && !GraphicsCoordinateValidation.TryResolvePoint(currentGraphicsX, currentGraphicsY, x, y, out x, out y))
        {
            return false;
        }

        if (!GraphicsCoordinateValidation.IsSupportedCircleCenter(x, y))
        {
            return false;
        }

        if (!GraphicsCoordinateSpaceMath.TryTranslateLogicalToAbsoluteUnchecked(host, x, y, out var absoluteX, out var absoluteY))
        {
            return false;
        }

        arguments = new GraphicsAnchorTransferArguments(absoluteX, absoluteY);
        return true;
    }

    public static bool TryResolveTwoDimensionalNumericBuffer(
        RuntimeValue bufferValue,
        out RuntimeArrayValue buffer,
        out GraphicsBufferShape shape)
    {
        shape = default;

        if (bufferValue.Type is not ArrayTypeSymbol arrayType
            || arrayType.Rank != 2
            || !IsNumericElementType(arrayType.ElementType))
        {
            buffer = null!;
            return false;
        }

        buffer = bufferValue.AsArray();
        if (!buffer.IsAllocated
            || buffer.Rank != 2
            || !IsNumericElementType(buffer.ElementType))
        {
            return false;
        }

        return TryGetVisibleShape(buffer, out shape);
    }

    public static bool BufferMatchesRectangle(GraphicsBufferShape shape, GraphicsRectangleTransferArguments rectangle) =>
        shape.Width == rectangle.Width && shape.Height == rectangle.Height && (long)shape.Width * shape.Height <= GraphicsResourceLimits.MaximumPixels;

    public static void CaptureRectangle(
        IGraphicsHost host,
        RuntimeArrayValue buffer,
        GraphicsBufferShape shape,
        GraphicsRectangleTransferArguments rectangle)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(buffer);

        var pixels = host.CaptureAbsolutePixels(rectangle.LeftAbsolute, rectangle.TopAbsolute, shape.Width, shape.Height);
        Span<int> indices = stackalloc int[2];
        for (var xOffset = 0; xOffset < shape.Width; xOffset++)
        {
            if ((host as IRuntimeCancellationProvider)?.IsCancellationRequested == true) return;
            indices[0] = shape.XLowerBound + xOffset;
            var absoluteX = rectangle.LeftAbsolute + xOffset;

            for (var yOffset = 0; yOffset < shape.Height; yOffset++)
            {
                indices[1] = shape.YLowerBound + yOffset;
                var absoluteY = rectangle.TopAbsolute + yOffset;
                var color = GraphicsCoordinateSpaceMath.IsInsideClip(host, absoluteX, absoluteY)
                    ? pixels is null ? host.GetAbsolutePixel(absoluteX, absoluteY) : pixels[yOffset * shape.Width + xOffset]
                    : 0;
                buffer.SetValue(indices, CreateBufferElementValue(buffer.ElementType, color));
            }
        }
    }

    public static bool TryPutBuffer(
        IGraphicsHost host,
        RuntimeArrayValue buffer,
        GraphicsBufferShape shape,
        GraphicsAnchorTransferArguments anchor,
        BoundPutActionKind action)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentNullException.ThrowIfNull(buffer);

        var count = (long)shape.Width * shape.Height;
        if (count > GraphicsResourceLimits.MaximumPixels) return false;
        var colors = new int[(int)count];
        Span<int> indices = stackalloc int[2];
        // Проверяем весь источник до первой записи: ошибка не оставляет полукадр.
        for (var x = 0; x < shape.Width; x++)
        {
            if ((host as IRuntimeCancellationProvider)?.IsCancellationRequested == true) return true;
            indices[0] = shape.XLowerBound + x;
            for (var y = 0; y < shape.Height; y++)
            {
                indices[1] = shape.YLowerBound + y;
                if (!buffer.TryGetValue(indices, out var value) || !TryNormalizePutSourceColor(value, out var color)) return false;
                colors[y * shape.Width + x] = color;
            }
        }

        var needsDestination = action is BoundPutActionKind.And or BoundPutActionKind.Or or BoundPutActionKind.Xor;
        var destination = needsDestination
            ? host.CaptureAbsolutePixels(anchor.AbsoluteX, anchor.AbsoluteY, shape.Width, shape.Height) : null;
        for (var y = 0; y < shape.Height; y++)
            for (var x = 0; x < shape.Width; x++)
            {
                var offset = y * shape.Width + x;
                var px = anchor.AbsoluteX + x;
                var py = anchor.AbsoluteY + y;
                if (!GraphicsCoordinateSpaceMath.IsInsideClip(host, px, py)) continue;
                var oldColor = needsDestination ? destination is null ? host.GetAbsolutePixel(px, py) : destination[offset] : 0;
                colors[offset] = ApplyPutAction(oldColor, colors[offset], action);
            }
        if (host.TryWriteAbsolutePixels(anchor.AbsoluteX, anchor.AbsoluteY, shape.Width, shape.Height, colors)) return true;
        for (var y = 0; y < shape.Height; y++)
            for (var x = 0; x < shape.Width; x++)
            {
                var px = anchor.AbsoluteX + x;
                var py = anchor.AbsoluteY + y;
                if (GraphicsCoordinateSpaceMath.IsInsideClip(host, px, py))
                    host.SetAbsolutePixel(px, py, colors[y * shape.Width + x]);
            }
        return true;
    }
    private static bool TryGetVisibleShape(RuntimeArrayValue buffer, out GraphicsBufferShape shape)
    {
        shape = default;
        if (!TryGetVisibleLength(buffer.GetLowerBound(1), buffer.GetUpperBound(1), out var width)
            || !TryGetVisibleLength(buffer.GetLowerBound(2), buffer.GetUpperBound(2), out var height))
        {
            return false;
        }

        shape = new GraphicsBufferShape(
            buffer.GetLowerBound(1),
            buffer.GetUpperBound(1),
            buffer.GetLowerBound(2),
            buffer.GetUpperBound(2),
            width,
            height);
        return true;
    }

    private static bool TryGetVisibleLength(int lowerBound, int upperBound, out int length)
    {
        if (upperBound < lowerBound)
        {
            length = 0;
            return true;
        }

        var span = (long)upperBound - lowerBound + 1;
        if (span > int.MaxValue)
        {
            length = 0;
            return false;
        }

        length = (int)span;
        return true;
    }

    private static bool IsNumericElementType(TypeSymbol type) =>
        type == TypeSymbol.Integer
        || type == TypeSymbol.Long
        || type == TypeSymbol.Double
        || type == TypeSymbol.Decimal;

    private static RuntimeValue CreateBufferElementValue(TypeSymbol elementType, int color)
    {
        if (elementType == TypeSymbol.Integer)
        {
            return RuntimeValue.FromInteger(color);
        }

        if (elementType == TypeSymbol.Long)
        {
            return RuntimeValue.FromLong(color);
        }

        if (elementType == TypeSymbol.Double)
        {
            return RuntimeValue.FromDouble(color);
        }

        if (elementType == TypeSymbol.Decimal)
        {
            return RuntimeValue.FromDecimal(color);
        }

        throw new InvalidOperationException($"Unsupported graphics buffer element type {elementType.Name}.");
    }

    private static bool TryNormalizePutSourceColor(RuntimeValue value, out int color)
    {
        if (value.Type == TypeSymbol.Integer)
        {
            color = GraphicsColorNormalization.Normalize(value.AsInteger());
            return true;
        }

        if (value.Type == TypeSymbol.Long)
        {
            color = GraphicsColorNormalization.Normalize(value.AsLong());
            return true;
        }

        if (value.Type == TypeSymbol.Double)
        {
            return GraphicsColorNormalization.TryNormalize(value.AsDouble(), out color);
        }

        if (value.Type == TypeSymbol.Decimal)
        {
            color = GraphicsColorNormalization.Normalize(value.AsDecimal());
            return true;
        }

        color = 0;
        return false;
    }

    private static int ApplyPutAction(int destinationColor, int sourceColor, BoundPutActionKind action)
    {
        return action switch
        {
            BoundPutActionKind.PSet => sourceColor,
            BoundPutActionKind.Preset => 255 - sourceColor,
            BoundPutActionKind.And => destinationColor & sourceColor,
            BoundPutActionKind.Or => destinationColor | sourceColor,
            BoundPutActionKind.Xor => destinationColor ^ sourceColor,
            _ => sourceColor
        };
    }

    private static bool TryGetSpanLength(long start, long end, out int length)
    {
        var span = end - start + 1;
        if (span < 0 || span > int.MaxValue)
        {
            length = 0;
            return false;
        }

        length = (int)span;
        return true;
    }
}
