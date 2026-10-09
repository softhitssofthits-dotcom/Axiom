using System.Numerics;

namespace Axiom.Runtime;

internal readonly record struct TileTransformCacheKey(
    RuntimeTilesetValue Tileset,
    int TileIndex,
    RuntimeTilemapFlipFlags Flags);

internal readonly record struct TilemapVisibleRange(
    long AdjustedOriginX,
    long AdjustedOriginY,
    int FirstColumn,
    int LastColumn,
    int FirstRow,
    int LastRow);

internal static class TilemapRenderSupport
{
    public static void DrawTile(
        IGraphicsHost host,
        RuntimeTilesetValue tileset,
        int tileIndex,
        RuntimeTilemapFlipFlags flags,
        long x,
        long y,
        Dictionary<TileTransformCacheKey, RuntimeImageValue> transformCache)
    {
        if (flags == RuntimeTilemapFlipFlags.None)
        {
            host.DrawImageRegion(
                tileset.Image,
                tileset.GetTileLeft(tileIndex),
                tileset.GetTileTop(tileIndex),
                tileset.TileWidth,
                tileset.TileHeight,
                x,
                y,
                tileset.TileWidth,
                tileset.TileHeight);
            return;
        }

        var image = GetOrCreateTransformedTileImage(tileset, tileIndex, flags, transformCache);
        host.DrawImage(image, x, y, image.Width, image.Height);
    }

    public static bool TryCreateVisibleRange(
        IGraphicsHost host,
        RuntimeTilemapValue tilemap,
        RuntimeTilemapLayer layer,
        long mapX,
        long mapY,
        out TilemapVisibleRange range)
    {
        range = default;
        if (layer.Width <= 0 || layer.Height <= 0)
        {
            return false;
        }

        var viewportWidth = (Int128)host.ViewportRight - host.ViewportLeft + 1;
        var viewportHeight = (Int128)host.ViewportBottom - host.ViewportTop + 1;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return false;
        }

        if (!TryComputeRelativeOrigin(mapX, layer.OffsetX, host.CanvasCameraX, layer.ParallaxX, out var originX)
            || !TryComputeRelativeOrigin(mapY, layer.OffsetY, host.CanvasCameraY, layer.ParallaxY, out var originY))
            return false;

        var firstColumn = CeilingDivide(-tilemap.MaxTileRight - originX, tilemap.TileWidth) - 1;
        var lastColumn = FloorDivide(viewportWidth - 1 - tilemap.MinTileLeft - originX, tilemap.TileWidth) + 1;
        var firstRow = CeilingDivide(-tilemap.MaxTileBottom - originY, tilemap.TileHeight) - 1;
        var lastRow = FloorDivide(viewportHeight - 1 - tilemap.MinTileTop - originY, tilemap.TileHeight) + 1;

        firstColumn = firstColumn < 0 ? 0 : firstColumn;
        firstRow = firstRow < 0 ? 0 : firstRow;
        lastColumn = lastColumn > layer.Width - 1 ? layer.Width - 1 : lastColumn;
        lastRow = lastRow > layer.Height - 1 ? layer.Height - 1 : lastRow;
        if (lastColumn < firstColumn || lastRow < firstRow) return false;

        range = new TilemapVisibleRange(
            checked((long)(originX + host.CanvasCameraX)),
            checked((long)(originY + host.CanvasCameraY)),
            checked((int)firstColumn), checked((int)lastColumn),
            checked((int)firstRow), checked((int)lastRow));
        return true;
    }

    public static void VisitCells(
        RuntimeTilemapRenderOrder renderOrder,
        TilemapVisibleRange range,
        Action<int, int> visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        switch (renderOrder)
        {
            case RuntimeTilemapRenderOrder.RightDown:
                for (var row = range.FirstRow; row <= range.LastRow; row++)
                {
                    for (var column = range.FirstColumn; column <= range.LastColumn; column++)
                    {
                        visitor(column, row);
                    }
                }
                break;

            case RuntimeTilemapRenderOrder.RightUp:
                for (var row = range.LastRow; row >= range.FirstRow; row--)
                {
                    for (var column = range.FirstColumn; column <= range.LastColumn; column++)
                    {
                        visitor(column, row);
                    }
                }
                break;

            case RuntimeTilemapRenderOrder.LeftDown:
                for (var row = range.FirstRow; row <= range.LastRow; row++)
                {
                    for (var column = range.LastColumn; column >= range.FirstColumn; column--)
                    {
                        visitor(column, row);
                    }
                }
                break;

            case RuntimeTilemapRenderOrder.LeftUp:
                for (var row = range.LastRow; row >= range.FirstRow; row--)
                {
                    for (var column = range.LastColumn; column >= range.FirstColumn; column--)
                    {
                        visitor(column, row);
                    }
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(renderOrder));
        }
    }

    private static RuntimeImageValue GetOrCreateTransformedTileImage(
        RuntimeTilesetValue tileset,
        int tileIndex,
        RuntimeTilemapFlipFlags flags,
        Dictionary<TileTransformCacheKey, RuntimeImageValue> transformCache)
    {
        var key = new TileTransformCacheKey(tileset, tileIndex, flags);
        if (transformCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var created = CreateTransformedTileImage(tileset, tileIndex, flags);
        transformCache[key] = created;
        return created;
    }

    private static RuntimeImageValue CreateTransformedTileImage(
        RuntimeTilesetValue tileset,
        int tileIndex,
        RuntimeTilemapFlipFlags flags)
    {
        var width = tileset.TileWidth;
        var height = tileset.TileHeight;
        var sourceLeft = tileset.GetTileLeft(tileIndex);
        var sourceTop = tileset.GetTileTop(tileIndex);
        var sourceImageWidth = tileset.Image.Width;
        var sourcePixels = tileset.Image.BgraPixels;
        var targetPixels = new uint[checked(width * height)];

        for (var targetY = 0; targetY < height; targetY++)
        {
            for (var targetX = 0; targetX < width; targetX++)
            {
                MapSourcePoint(targetX, targetY, width, height, flags, out var sourceX, out var sourceY);
                targetPixels[(targetY * width) + targetX] =
                    sourcePixels[((sourceTop + sourceY) * sourceImageWidth) + sourceLeft + sourceX];
            }
        }

        return new RuntimeImageValue(width, height, targetPixels);
    }

    private static void MapSourcePoint(
        int targetX,
        int targetY,
        int width,
        int height,
        RuntimeTilemapFlipFlags flags,
        out int sourceX,
        out int sourceY)
    {
        double u = width == 1 ? 0d : targetX / (double)(width - 1);
        double v = height == 1 ? 0d : targetY / (double)(height - 1);

        if ((flags & RuntimeTilemapFlipFlags.Vertical) != 0)
        {
            v = 1d - v;
        }

        if ((flags & RuntimeTilemapFlipFlags.Horizontal) != 0)
        {
            u = 1d - u;
        }

        if ((flags & RuntimeTilemapFlipFlags.Diagonal) != 0)
        {
            (u, v) = (v, u);
        }

        sourceX = width == 1
            ? 0
            : Math.Clamp((int)Math.Round(u * (width - 1), MidpointRounding.AwayFromZero), 0, width - 1);
        sourceY = height == 1
            ? 0
            : Math.Clamp((int)Math.Round(v * (height - 1), MidpointRounding.AwayFromZero), 0, height - 1);
    }

    private static Int128 FloorDivide(Int128 numerator, int denominator)
    {
        var quotient = numerator / denominator;
        var remainder = numerator % denominator;
        if (remainder < 0)
        {
            quotient--;
        }

        return quotient;
    }

    private static Int128 CeilingDivide(Int128 numerator, int denominator)
    {
        var quotient = numerator / denominator;
        var remainder = numerator % denominator;
        if (remainder > 0)
        {
            quotient++;
        }

        return quotient;
    }

    private static bool TryComputeRelativeOrigin(long mapOrigin, double offset, long camera, double parallax, out Int128 origin)
    {
        origin = 0;
        var effectiveCamera = camera * parallax;
        // Бесконечное произведение не компенсируется конечным смещением слоя.
        if (!double.IsFinite(offset) || !double.IsFinite(effectiveCamera)) return false;
        var roundedOffset = Math.Round(offset, MidpointRounding.AwayFromZero);
        var roundedCamera = Math.Round(effectiveCamera, MidpointRounding.AwayFromZero);
        const double exclusiveMaximum = 9223372036854775808d;
        if (roundedOffset >= long.MinValue && roundedOffset < exclusiveMaximum
            && roundedCamera >= long.MinValue && roundedCamera < exclusiveMaximum)
        {
            origin = (Int128)(long)roundedOffset - (long)roundedCamera + mapOrigin;
            return true;
        }

        // В редком большом случае сохраняем точную компенсацию округлённых DOUBLE.
        var difference = new BigInteger(roundedOffset) - new BigInteger(roundedCamera) + mapOrigin;
        // Координата вне этого диапазона заведомо не пересекает конечную карту.
        // Ограничение оставляет запас для размеров окна и тайлов при отсечении.
        var bound = BigInteger.One << 100;
        if (difference <= -bound || difference >= bound) return false;
        origin = (Int128)difference;
        return true;
    }
}
