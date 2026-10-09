namespace Axiom.Runtime;

public sealed class RuntimeTilesetValue
{
    public RuntimeTilesetValue(
        RuntimeImageValue image,
        int tileWidth,
        int tileHeight,
        int margin = 0,
        int spacing = 0,
        int tileOffsetX = 0,
        int tileOffsetY = 0)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (tileWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileWidth), tileWidth, "Tileset tile width must be positive.");
        }

        if (tileHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileHeight), tileHeight, "Tileset tile height must be positive.");
        }

        if (margin < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(margin), margin, "Tileset margin cannot be negative.");
        }

        if (spacing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spacing), spacing, "Tileset spacing cannot be negative.");
        }

        Image = image;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Margin = margin;
        Spacing = spacing;
        TileOffsetX = tileOffsetX;
        TileOffsetY = tileOffsetY;

        var usableWidth = image.Width - ((long)margin * 2);
        var usableHeight = image.Height - ((long)margin * 2);
        Columns = ComputeAxisTileCount(usableWidth, tileWidth, spacing, nameof(image));
        Rows = ComputeAxisTileCount(usableHeight, tileHeight, spacing, nameof(image));
        TileCount = checked(Columns * Rows);
        if (TileCount <= 0)
        {
            throw new ArgumentException("Tileset atlas does not contain any tiles.", nameof(image));
        }
    }

    public RuntimeImageValue Image { get; }

    public int TileWidth { get; }

    public int TileHeight { get; }

    public int Margin { get; }

    public int Spacing { get; }

    public int TileOffsetX { get; }

    public int TileOffsetY { get; }

    public int Columns { get; }

    public int Rows { get; }

    public int TileCount { get; }

    public bool HasTiles => TileCount > 0;

    public int GetTileLeft(int tileIndex)
    {
        var zeroBasedIndex = GetZeroBasedIndex(tileIndex);
        return checked((int)(Margin + ((long)(zeroBasedIndex % Columns) * ((long)TileWidth + Spacing))));
    }

    public int GetTileTop(int tileIndex)
    {
        var zeroBasedIndex = GetZeroBasedIndex(tileIndex);
        return checked((int)(Margin + ((long)(zeroBasedIndex / Columns) * ((long)TileHeight + Spacing))));
    }

    private int GetZeroBasedIndex(int tileIndex)
    {
        if (tileIndex < 1 || tileIndex > TileCount)
        {
            throw new ArgumentOutOfRangeException(nameof(tileIndex), tileIndex, "Tileset tile index must be in the range 1..TileCount.");
        }

        return tileIndex - 1;
    }

    private static int ComputeAxisTileCount(long usableLength, int tileLength, int spacing, string parameterName)
    {
        if (usableLength < tileLength)
        {
            throw new ArgumentException("Tileset atlas does not contain a complete tile grid.", parameterName);
        }

        var numerator = usableLength + spacing;
        var denominator = (long)tileLength + spacing;
        if (numerator <= 0 || numerator % denominator != 0)
        {
            throw new ArgumentException("Tileset atlas dimensions do not match the requested tile grid.", parameterName);
        }

        var count = numerator / denominator;
        if (count <= 0)
        {
            throw new ArgumentException("Tileset atlas does not contain any tiles.", parameterName);
        }

        return checked((int)count);
    }
}
