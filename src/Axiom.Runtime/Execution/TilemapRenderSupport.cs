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

        var viewportWidth = host.ViewportRight - host.ViewportLeft + 1L;
        var viewportHeight = host.ViewportBottom - host.ViewportTop + 1L;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            return false;
        }

        var layerOffsetX = RoundToLong(layer.OffsetX);
        var layerOffsetY = RoundToLong(layer.OffsetY);
        var effectiveCameraX = RoundToLong(host.CanvasCameraX * layer.ParallaxX);
        var effectiveCameraY = RoundToLong(host.CanvasCameraY * layer.ParallaxY);
        var layerOriginX = checked(mapX + layerOffsetX);
        var layerOriginY = checked(mapY + layerOffsetY);

        var firstColumn = CeilingDivide(
            checked(effectiveCameraX - tilemap.MaxTileRight - layerOriginX),
            tilemap.TileWidth) - 1;
        var lastColumn = FloorDivide(
            checked((effectiveCameraX + viewportWidth - 1L) - tilemap.MinTileLeft - layerOriginX),
            tilemap.TileWidth) + 1;
        var firstRow = CeilingDivide(
            checked(effectiveCameraY - tilemap.MaxTileBottom - layerOriginY),
            tilemap.TileHeight) - 1;
        var lastRow = FloorDivide(
            checked((effectiveCameraY + viewportHeight - 1L) - tilemap.MinTileTop - layerOriginY),
            tilemap.TileHeight) + 1;

        firstColumn = Math.Max(0, firstColumn);
        firstRow = Math.Max(0, firstRow);
        lastColumn = Math.Min(layer.Width - 1, lastColumn);
        lastRow = Math.Min(layer.Height - 1, lastRow);
        if (lastColumn < firstColumn || lastRow < firstRow)
        {
            return false;
        }

        range = new TilemapVisibleRange(
            checked(layerOriginX + host.CanvasCameraX - effectiveCameraX),
            checked(layerOriginY + host.CanvasCameraY - effectiveCameraY),
            firstColumn,
            lastColumn,
            firstRow,
            lastRow);
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

    private static int FloorDivide(long numerator, int denominator)
    {
        var quotient = Math.DivRem(numerator, denominator, out var remainder);
        if (remainder < 0)
        {
            quotient--;
        }

        return checked((int)quotient);
    }

    private static int CeilingDivide(long numerator, int denominator)
    {
        var quotient = Math.DivRem(numerator, denominator, out var remainder);
        if (remainder > 0)
        {
            quotient++;
        }

        return checked((int)quotient);
    }

    private static long RoundToLong(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0L;
        }

        if (value <= long.MinValue)
        {
            return long.MinValue;
        }

        if (value >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return checked((long)Math.Round(value, MidpointRounding.AwayFromZero));
    }
}
