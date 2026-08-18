using System.Text.Json;

namespace Axiom.Runtime;

[Flags]
public enum RuntimeTilemapFlipFlags
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    Diagonal = 4
}

public enum RuntimeTilemapLayerKind
{
    Tile,
    Object
}

public enum RuntimeTilemapRenderOrder
{
    RightDown,
    RightUp,
    LeftDown,
    LeftUp
}

public readonly record struct RuntimeTilemapCell(
    int TilesetIndex,
    int TileIndex,
    RuntimeTilemapFlipFlags Flags)
{
    public bool IsEmpty => TilesetIndex <= 0 || TileIndex <= 0;
}

public sealed class RuntimeTilemapTilesetReference
{
    public RuntimeTilemapTilesetReference(int firstGlobalId, RuntimeTilesetValue tileset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(firstGlobalId, 1);
        ArgumentNullException.ThrowIfNull(tileset);

        FirstGlobalId = firstGlobalId;
        Tileset = tileset;
    }

    public int FirstGlobalId { get; }

    public RuntimeTilesetValue Tileset { get; }
}

public sealed class RuntimeTilemapLayer
{
    public RuntimeTilemapLayer(
        string name,
        RuntimeTilemapLayerKind kind,
        int width,
        int height,
        bool visible,
        double opacity,
        double offsetX,
        double offsetY,
        double parallaxX,
        double parallaxY,
        RuntimeTilemapCell[]? cells)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (width < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        if (cells is not null && cells.Length != checked(width * height))
        {
            throw new ArgumentException("Tile layer cell count must match width * height.", nameof(cells));
        }

        Name = name;
        Kind = kind;
        Width = width;
        Height = height;
        Visible = visible;
        Opacity = opacity;
        OffsetX = offsetX;
        OffsetY = offsetY;
        ParallaxX = parallaxX;
        ParallaxY = parallaxY;
        Cells = cells;
    }

    public string Name { get; }

    public RuntimeTilemapLayerKind Kind { get; }

    public int Width { get; }

    public int Height { get; }

    public bool Visible { get; }

    public double Opacity { get; }

    public double OffsetX { get; }

    public double OffsetY { get; }

    public double ParallaxX { get; }

    public double ParallaxY { get; }

    public RuntimeTilemapCell[]? Cells { get; }

    public bool IsTileLayer => Kind == RuntimeTilemapLayerKind.Tile;

    public RuntimeTilemapCell GetCell(int zeroBasedColumn, int zeroBasedRow)
    {
        if (!IsTileLayer || Cells is null)
        {
            return default;
        }

        return Cells[checked((zeroBasedRow * Width) + zeroBasedColumn)];
    }

    public void SetCell(int zeroBasedColumn, int zeroBasedRow, RuntimeTilemapCell cell)
    {
        if (!IsTileLayer || Cells is null)
        {
            throw new InvalidOperationException("Object layers do not contain tile cells.");
        }

        Cells[checked((zeroBasedRow * Width) + zeroBasedColumn)] = cell;
    }
}

public sealed class RuntimeTilemapValue
{
    public RuntimeTilemapValue(
        int width,
        int height,
        int tileWidth,
        int tileHeight,
        RuntimeTilemapRenderOrder renderOrder,
        IReadOnlyList<RuntimeTilemapTilesetReference> tilesets,
        IReadOnlyList<RuntimeTilemapLayer> layers)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tileWidth, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(tileHeight, 0);
        ArgumentNullException.ThrowIfNull(tilesets);
        ArgumentNullException.ThrowIfNull(layers);

        Width = width;
        Height = height;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        PixelWidth = checked(width * tileWidth);
        PixelHeight = checked(height * tileHeight);
        RenderOrder = renderOrder;
        Tilesets = tilesets;
        Layers = layers;

        LayerCount = layers.Count;
        TilesetCount = tilesets.Count;
        (MinTileLeft, MaxTileRight, MinTileTop, MaxTileBottom) = ComputeTileExtents(tilesets);
    }

    public int Width { get; }

    public int Height { get; }

    public int TileWidth { get; }

    public int TileHeight { get; }

    public int PixelWidth { get; }

    public int PixelHeight { get; }

    public RuntimeTilemapRenderOrder RenderOrder { get; }

    public IReadOnlyList<RuntimeTilemapTilesetReference> Tilesets { get; }

    public IReadOnlyList<RuntimeTilemapLayer> Layers { get; }

    public int TilesetCount { get; }

    public int LayerCount { get; }

    public int MinTileLeft { get; }

    public int MaxTileRight { get; }

    public int MinTileTop { get; }

    public int MaxTileBottom { get; }

    public RuntimeTilemapLayer GetLayer1Based(int layerIndex)
    {
        if (layerIndex < 1 || layerIndex > Layers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
        }

        return Layers[layerIndex - 1];
    }

    public RuntimeTilemapTilesetReference GetTileset1Based(int tilesetIndex)
    {
        if (tilesetIndex < 1 || tilesetIndex > Tilesets.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(tilesetIndex));
        }

        return Tilesets[tilesetIndex - 1];
    }

    public static RuntimeTilemapValue LoadFromTiledJson(string mapPath, Func<string, RuntimeImageValue?> loadResolvedImage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPath);
        ArgumentNullException.ThrowIfNull(loadResolvedImage);

        using var document = JsonDocument.Parse(File.ReadAllText(mapPath));
        var root = document.RootElement;
        ValidateMapRoot(root, mapPath);

        var width = root.GetProperty("width").GetInt32();
        var height = root.GetProperty("height").GetInt32();
        var tileWidth = root.GetProperty("tilewidth").GetInt32();
        var tileHeight = root.GetProperty("tileheight").GetInt32();
        var renderOrder = ParseRenderOrder(root.TryGetProperty("renderorder", out var renderOrderElement) ? renderOrderElement.GetString() : null);

        var baseDirectory = Path.GetDirectoryName(mapPath) ?? Environment.CurrentDirectory;
        var tilesets = LoadTilesets(baseDirectory, root.GetProperty("tilesets"), loadResolvedImage);
        var layers = LoadLayers(root.GetProperty("layers"), width, height, tilesets);
        return new RuntimeTilemapValue(width, height, tileWidth, tileHeight, renderOrder, tilesets, layers);
    }

    private static void ValidateMapRoot(JsonElement root, string mapPath)
    {
        if (!root.TryGetProperty("orientation", out var orientationElement) ||
            !string.Equals(orientationElement.GetString(), "orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"LOADTILEMAP: карта '{Path.GetFileName(mapPath)}' должна быть ортогональной.");
        }

        if (root.TryGetProperty("infinite", out var infiniteElement) && infiniteElement.ValueKind is JsonValueKind.True)
        {
            throw new InvalidDataException($"LOADTILEMAP: бесконечные карты Tiled в '{Path.GetFileName(mapPath)}' пока не поддерживаются.");
        }
    }

    private static IReadOnlyList<RuntimeTilemapTilesetReference> LoadTilesets(
        string baseDirectory,
        JsonElement tilesetsElement,
        Func<string, RuntimeImageValue?> loadResolvedImage)
    {
        var tilesets = new List<RuntimeTilemapTilesetReference>();
        foreach (var tilesetEntry in tilesetsElement.EnumerateArray())
        {
            var firstGlobalId = tilesetEntry.GetProperty("firstgid").GetInt32();
            var resolvedTileset = ResolveTileset(baseDirectory, tilesetEntry);
            var imagePath = ResolveRelativePath(resolvedTileset.BaseDirectory, resolvedTileset.ImagePath);
            var image = loadResolvedImage(imagePath);
            if (image is null)
            {
                throw new InvalidDataException($"LOADTILEMAP: не удалось загрузить изображение тайлсета '{resolvedTileset.ImagePath}'.");
            }

            var tileset = new RuntimeTilesetValue(
                image,
                resolvedTileset.TileWidth,
                resolvedTileset.TileHeight,
                resolvedTileset.Margin,
                resolvedTileset.Spacing,
                resolvedTileset.TileOffsetX,
                resolvedTileset.TileOffsetY);
            tilesets.Add(new RuntimeTilemapTilesetReference(firstGlobalId, tileset));
        }

        return tilesets;
    }

    private static IReadOnlyList<RuntimeTilemapLayer> LoadLayers(
        JsonElement layersElement,
        int mapWidth,
        int mapHeight,
        IReadOnlyList<RuntimeTilemapTilesetReference> tilesets)
    {
        var layers = new List<RuntimeTilemapLayer>();
        foreach (var layerElement in layersElement.EnumerateArray())
        {
            var type = layerElement.GetProperty("type").GetString();
            if (string.Equals(type, "group", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("LOADTILEMAP: group layers Tiled пока не поддерживаются.");
            }

            var name = layerElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            var visible = !layerElement.TryGetProperty("visible", out var visibleElement) || visibleElement.GetBoolean();
            var opacity = layerElement.TryGetProperty("opacity", out var opacityElement) ? opacityElement.GetDouble() : 1d;
            var offsetX = layerElement.TryGetProperty("offsetx", out var offsetXElement) ? offsetXElement.GetDouble() : 0d;
            var offsetY = layerElement.TryGetProperty("offsety", out var offsetYElement) ? offsetYElement.GetDouble() : 0d;
            var parallaxX = layerElement.TryGetProperty("parallaxx", out var parallaxXElement) ? parallaxXElement.GetDouble() : 1d;
            var parallaxY = layerElement.TryGetProperty("parallaxy", out var parallaxYElement) ? parallaxYElement.GetDouble() : 1d;

            if (string.Equals(type, "tilelayer", StringComparison.OrdinalIgnoreCase))
            {
                var width = layerElement.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : mapWidth;
                var height = layerElement.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : mapHeight;
                var cells = ParseLayerCells(layerElement, width, height, tilesets);
                layers.Add(new RuntimeTilemapLayer(name, RuntimeTilemapLayerKind.Tile, width, height, visible, opacity, offsetX, offsetY, parallaxX, parallaxY, cells));
                continue;
            }

            if (string.Equals(type, "objectgroup", StringComparison.OrdinalIgnoreCase))
            {
                layers.Add(new RuntimeTilemapLayer(name, RuntimeTilemapLayerKind.Object, mapWidth, mapHeight, visible, opacity, offsetX, offsetY, parallaxX, parallaxY, cells: null));
                continue;
            }

            throw new InvalidDataException($"LOADTILEMAP: слой '{name}' имеет неподдерживаемый тип '{type}'.");
        }

        return layers;
    }

    private static RuntimeTilemapCell[] ParseLayerCells(JsonElement layerElement, int width, int height, IReadOnlyList<RuntimeTilemapTilesetReference> tilesets)
    {
        if (!layerElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("LOADTILEMAP: tile layer должен содержать JSON-массив data.");
        }

        var cells = new RuntimeTilemapCell[checked(width * height)];
        var index = 0;
        foreach (var rawValue in dataElement.EnumerateArray())
        {
            if (index >= cells.Length)
            {
                throw new InvalidDataException("LOADTILEMAP: размер data не совпадает с размерами тайлового слоя.");
            }

            cells[index++] = NormalizeCell(rawValue.GetUInt32(), tilesets);
        }

        if (index != cells.Length)
        {
            throw new InvalidDataException("LOADTILEMAP: размер data не совпадает с размерами тайлового слоя.");
        }

        return cells;
    }

    private static RuntimeTilemapCell NormalizeCell(uint rawValue, IReadOnlyList<RuntimeTilemapTilesetReference> tilesets)
    {
        const uint flipHorizontal = 0x80000000;
        const uint flipVertical = 0x40000000;
        const uint flipDiagonal = 0x20000000;
        const uint flipMask = flipHorizontal | flipVertical | flipDiagonal;

        if (rawValue == 0)
        {
            return default;
        }

        var flags = RuntimeTilemapFlipFlags.None;
        if ((rawValue & flipHorizontal) != 0)
        {
            flags |= RuntimeTilemapFlipFlags.Horizontal;
        }

        if ((rawValue & flipVertical) != 0)
        {
            flags |= RuntimeTilemapFlipFlags.Vertical;
        }

        if ((rawValue & flipDiagonal) != 0)
        {
            flags |= RuntimeTilemapFlipFlags.Diagonal;
        }

        var normalizedGlobalId = checked((int)(rawValue & ~flipMask));
        var selectedIndex = -1;
        for (var index = 0; index < tilesets.Count; index++)
        {
            if (tilesets[index].FirstGlobalId <= normalizedGlobalId)
            {
                selectedIndex = index;
            }
            else
            {
                break;
            }
        }

        if (selectedIndex < 0)
        {
            throw new InvalidDataException($"LOADTILEMAP: global tile id {normalizedGlobalId} не привязан ни к одному тайлсету.");
        }

        var tilesetRef = tilesets[selectedIndex];
        var tileIndex = checked(normalizedGlobalId - tilesetRef.FirstGlobalId + 1);
        if (tileIndex < 1 || tileIndex > tilesetRef.Tileset.TileCount)
        {
            throw new InvalidDataException($"LOADTILEMAP: global tile id {normalizedGlobalId} выходит за границы тайлсета.");
        }

        return new RuntimeTilemapCell(selectedIndex + 1, tileIndex, flags);
    }

    private static (int MinLeft, int MaxRight, int MinTop, int MaxBottom) ComputeTileExtents(IReadOnlyList<RuntimeTilemapTilesetReference> tilesets)
    {
        if (tilesets.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        var minLeft = int.MaxValue;
        var maxRight = int.MinValue;
        var minTop = int.MaxValue;
        var maxBottom = int.MinValue;
        foreach (var tilesetRef in tilesets)
        {
            var tileset = tilesetRef.Tileset;
            minLeft = Math.Min(minLeft, tileset.TileOffsetX);
            maxRight = Math.Max(maxRight, tileset.TileOffsetX + tileset.TileWidth - 1);
            minTop = Math.Min(minTop, tileset.TileOffsetY);
            maxBottom = Math.Max(maxBottom, tileset.TileOffsetY + tileset.TileHeight - 1);
        }

        return (minLeft, maxRight, minTop, maxBottom);
    }

    private static RuntimeTilemapRenderOrder ParseRenderOrder(string? renderOrder)
    {
        return renderOrder?.ToLowerInvariant() switch
        {
            null or "" or "right-down" => RuntimeTilemapRenderOrder.RightDown,
            "right-up" => RuntimeTilemapRenderOrder.RightUp,
            "left-down" => RuntimeTilemapRenderOrder.LeftDown,
            "left-up" => RuntimeTilemapRenderOrder.LeftUp,
            _ => throw new InvalidDataException($"LOADTILEMAP: renderorder '{renderOrder}' не поддерживается.")
        };
    }

    private static (string BaseDirectory, string ImagePath, int TileWidth, int TileHeight, int Margin, int Spacing, int TileOffsetX, int TileOffsetY) ResolveTileset(string baseDirectory, JsonElement tilesetEntry)
    {
        JsonElement definition = tilesetEntry;
        var definitionBaseDirectory = baseDirectory;
        if (tilesetEntry.TryGetProperty("source", out var sourceElement))
        {
            var sourcePath = ResolveRelativePath(baseDirectory, sourceElement.GetString() ?? string.Empty);
            definition = JsonDocument.Parse(File.ReadAllText(sourcePath)).RootElement.Clone();
            definitionBaseDirectory = Path.GetDirectoryName(sourcePath) ?? baseDirectory;
        }

        if (!definition.TryGetProperty("image", out var imageElement))
        {
            throw new InvalidDataException("LOADTILEMAP: image collection tilesets пока не поддерживаются.");
        }

        var tileOffsetX = 0;
        var tileOffsetY = 0;
        if (definition.TryGetProperty("tileoffset", out var tileOffsetElement))
        {
            tileOffsetX = tileOffsetElement.TryGetProperty("x", out var offsetXElement) ? offsetXElement.GetInt32() : 0;
            tileOffsetY = tileOffsetElement.TryGetProperty("y", out var offsetYElement) ? offsetYElement.GetInt32() : 0;
        }

        return (
            definitionBaseDirectory,
            imageElement.GetString() ?? string.Empty,
            definition.GetProperty("tilewidth").GetInt32(),
            definition.GetProperty("tileheight").GetInt32(),
            definition.TryGetProperty("margin", out var marginElement) ? marginElement.GetInt32() : 0,
            definition.TryGetProperty("spacing", out var spacingElement) ? spacingElement.GetInt32() : 0,
            tileOffsetX,
            tileOffsetY);
    }

    private static string ResolveRelativePath(string baseDirectory, string relativePath)
    {
        if (Path.IsPathFullyQualified(relativePath))
        {
            return relativePath;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, relativePath));
    }
}
