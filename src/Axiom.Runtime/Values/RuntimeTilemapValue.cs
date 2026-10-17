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
    public RuntimeTilemapTilesetReference(int firstGlobalId, RuntimeTilesetValue tileset, IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>>? tileProperties = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(firstGlobalId, 1);
        ArgumentNullException.ThrowIfNull(tileset);

        TileProperties = tileProperties ?? new Dictionary<int, IReadOnlyDictionary<string, string>>();
        FirstGlobalId = firstGlobalId;
        Tileset = tileset;
    }

    public IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> TileProperties { get; }

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
        RuntimeTilemapCell[]? cells, IReadOnlyDictionary<string, string>? properties = null, IReadOnlyList<RuntimeTilemapObject>? objects = null)
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
        Properties = properties ?? RuntimeTilemapMetadata.Empty;
        Objects = objects ?? Array.Empty<RuntimeTilemapObject>();
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

    public IReadOnlyDictionary<string, string> Properties { get; }

    public IReadOnlyList<RuntimeTilemapObject> Objects { get; }

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
        IReadOnlyList<RuntimeTilemapLayer> layers, IReadOnlyDictionary<string, string>? properties = null)
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
        Properties = properties ?? RuntimeTilemapMetadata.Empty;

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

    public IReadOnlyDictionary<string, string> Properties { get; }

    public int TilesetCount { get; }

    public int LayerCount { get; }

    public long MinTileLeft { get; }

    public long MaxTileRight { get; }

    public long MinTileTop { get; }

    public long MaxTileBottom { get; }

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

    public static RuntimeTilemapValue LoadFromTiledJson(string mapPath, Func<string, RuntimeImageValue?> loadResolvedImage,
        Func<bool>? isCancellationRequested = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mapPath);
        ArgumentNullException.ThrowIfNull(loadResolvedImage);

        CheckCancellation(isCancellationRequested);
        using var document = ReadBoundedJsonFile(mapPath);
        var root = document.RootElement;
        ValidateMapRoot(root, mapPath);

        var width = ReadMapInteger(root, "width", positive: false);
        var height = ReadMapInteger(root, "height", positive: false);
        var tileWidth = ReadMapInteger(root, "tilewidth", positive: true);
        var tileHeight = ReadMapInteger(root, "tileheight", positive: true);
        var renderOrder = ParseRenderOrder(root.TryGetProperty("renderorder", out var renderOrderElement) ? renderOrderElement.GetString() : null);

        var baseDirectory = Path.GetDirectoryName(mapPath) ?? Environment.CurrentDirectory;
        var tilesets = LoadTilesets(baseDirectory, ReadMapArray(root, "tilesets"), loadResolvedImage, isCancellationRequested);
        var layers = LoadLayers(ReadMapArray(root, "layers"), width, height, tilesets, isCancellationRequested);
        return new RuntimeTilemapValue(width, height, tileWidth, tileHeight, renderOrder, tilesets, layers, RuntimeTilemapMetadata.ReadProperties(root));
    }

    private static void CheckCancellation(Func<bool>? isCancellationRequested)
    {
        if (isCancellationRequested?.Invoke() == true) throw new OperationCanceledException();
    }

    private static JsonDocument ReadBoundedJsonFile(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > GraphicsResourceLimits.MaximumResourceFileBytes)
            throw RuntimeTilemapMetadata.Invalid("JSON resource exceeds 64 MiB.");
        return JsonDocument.Parse(stream);
    }
    private static void ValidateMapRoot(JsonElement root, string mapPath)
    {
        if (!root.TryGetProperty("orientation", out var orientationElement) ||
            !string.Equals(orientationElement.GetString(), "orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"LOADTILEMAP: карта '{Path.GetFileName(mapPath)}' должна быть ортогональной.");
        }

        foreach (var field in new[] { "parallaxoriginx", "parallaxoriginy" })
        {
            if (root.TryGetProperty(field, out var origin)
                && (origin.ValueKind != JsonValueKind.Number || !origin.TryGetDouble(out var value) || !double.IsFinite(value) || value != 0d))
                throw RuntimeTilemapMetadata.Invalid($"Поле {field} карты поддерживает только числовой ноль.");
        }

        if (root.TryGetProperty("infinite", out var infiniteElement))
        {
            if (infiniteElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw RuntimeTilemapMetadata.Invalid("Поле infinite должно быть BOOLEAN.");
            if (infiniteElement.GetBoolean())
                throw new InvalidDataException($"LOADTILEMAP: бесконечные карты Tiled в '{Path.GetFileName(mapPath)}' пока не поддерживаются.");
        }
    }

    private static int ReadMapInteger(JsonElement root, string name, bool positive)
    {
        if (!root.TryGetProperty(name, out var value)) throw RuntimeTilemapMetadata.Invalid($"Отсутствует поле {name}.");
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var number))
            throw RuntimeTilemapMetadata.Invalid($"Поле {name} должно быть целым числом INTEGER.");
        if (positive ? number <= 0 : number < 0)
            throw RuntimeTilemapMetadata.Invalid($"Поле {name} должно быть " + (positive ? "положительным." : "неотрицательным."));
        return number;
    }

    private static JsonElement ReadMapArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value)) throw RuntimeTilemapMetadata.Invalid($"Отсутствует поле {name}.");
        if (value.ValueKind != JsonValueKind.Array) throw RuntimeTilemapMetadata.Invalid($"Поле {name} должно быть массивом.");
        return value;
    }

    private static IReadOnlyList<RuntimeTilemapTilesetReference> LoadTilesets(
        string baseDirectory,
        JsonElement tilesetsElement,
        Func<string, RuntimeImageValue?> loadResolvedImage,
        Func<bool>? isCancellationRequested)
    {
        var tilesets = new List<RuntimeTilemapTilesetReference>();
        foreach (var tilesetEntry in tilesetsElement.EnumerateArray())
        {
            CheckCancellation(isCancellationRequested);
            var firstGlobalId = ReadMapInteger(tilesetEntry, "firstgid", positive: true);
            if (firstGlobalId > 0x0fffffff) throw RuntimeTilemapMetadata.Invalid("firstgid содержит зарезервированные биты Tiled.");
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
            foreach (var field in new[] { "columns", "tilecount", "imagewidth", "imageheight" })
            {
                var expected = field switch { "columns" => tileset.Columns, "tilecount" => tileset.TileCount, "imagewidth" => image.Width, _ => image.Height };
                if (resolvedTileset.Definition.TryGetProperty(field, out var declared)
                    && (declared.ValueKind != JsonValueKind.Number || !declared.TryGetInt32(out var number) || number != expected))
                    throw RuntimeTilemapMetadata.Invalid($"Поле {field} тайлсета не соответствует изображению и сетке.");
            }
            if (resolvedTileset.TileProperties.Keys.Any(index => index < 1 || index > tileset.TileCount))
                throw RuntimeTilemapMetadata.Invalid("id тайла выходит за границы атласа.");
            tilesets.Add(new RuntimeTilemapTilesetReference(firstGlobalId, tileset, resolvedTileset.TileProperties));
        }

        var ordered = tilesets.OrderBy(reference => reference.FirstGlobalId).ToArray();
        for (var index = 1; index < ordered.Length; index++)
        {
            if ((long)ordered[index - 1].FirstGlobalId + ordered[index - 1].Tileset.TileCount > ordered[index].FirstGlobalId)
                throw RuntimeTilemapMetadata.Invalid("Диапазоны firstgid тайлсетов пересекаются.");
        }
        return tilesets;
    }

    private static IReadOnlyList<RuntimeTilemapLayer> LoadLayers(
        JsonElement layersElement,
        int mapWidth,
        int mapHeight,
        IReadOnlyList<RuntimeTilemapTilesetReference> tilesets,
        Func<bool>? isCancellationRequested)
    {
        var layers = new List<RuntimeTilemapLayer>();
        long totalCells = 0;
        foreach (var layerElement in layersElement.EnumerateArray())
        {
            CheckCancellation(isCancellationRequested);
            var type = layerElement.GetProperty("type").GetString();
            if (string.Equals(type, "group", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("LOADTILEMAP: group layers Tiled пока не поддерживаются.");
            }

            ValidateLayerAppearance(layerElement);
            var name = layerElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            var visible = !layerElement.TryGetProperty("visible", out var visibleElement) || visibleElement.GetBoolean();
            var opacity = ReadLayerNumber(layerElement, "opacity", 1d);
            if (opacity < 0d || opacity > 1d) throw RuntimeTilemapMetadata.Invalid("Поле opacity слоя должно быть от 0 до 1.");
            var offsetX = ReadLayerNumber(layerElement, "offsetx", 0d);
            var offsetY = ReadLayerNumber(layerElement, "offsety", 0d);
            var parallaxX = ReadLayerNumber(layerElement, "parallaxx", 1d);
            var parallaxY = ReadLayerNumber(layerElement, "parallaxy", 1d);

            if (string.Equals(type, "tilelayer", StringComparison.OrdinalIgnoreCase))
            {
                var width = layerElement.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : mapWidth;
                var height = layerElement.TryGetProperty("height", out var heightElement) ? heightElement.GetInt32() : mapHeight;
                var count = (long)width * height;
                if (width < 0 || height < 0 || count > GraphicsResourceLimits.MaximumPixels - totalCells)
                    throw RuntimeTilemapMetadata.Invalid("Tile layer cell budget exceeded.");
                totalCells += count;
                var cells = ParseLayerCells(layerElement, width, height, tilesets, isCancellationRequested);
                layers.Add(new RuntimeTilemapLayer(name, RuntimeTilemapLayerKind.Tile, width, height, visible, opacity, offsetX, offsetY, parallaxX, parallaxY, cells, RuntimeTilemapMetadata.ReadProperties(layerElement)));
                continue;
            }

            if (string.Equals(type, "objectgroup", StringComparison.OrdinalIgnoreCase))
            {
                layers.Add(new RuntimeTilemapLayer(name, RuntimeTilemapLayerKind.Object, mapWidth, mapHeight, visible, opacity, offsetX, offsetY, parallaxX, parallaxY, cells: null, RuntimeTilemapMetadata.ReadProperties(layerElement), RuntimeTilemapMetadata.ReadObjects(layerElement)));
                continue;
            }

            throw new InvalidDataException($"LOADTILEMAP: слой '{name}' имеет неподдерживаемый тип '{type}'.");
        }

        return layers;
    }

    private static void ValidateLayerAppearance(JsonElement layer)
    {
        if (layer.TryGetProperty("mode", out var mode)
            && (mode.ValueKind != JsonValueKind.String || mode.GetString() != "normal"))
            throw RuntimeTilemapMetadata.Invalid("Поле mode слоя поддерживает только строку normal.");
        if (layer.TryGetProperty("tintcolor", out var tint))
        {
            var color = tint.ValueKind == JsonValueKind.String ? tint.GetString() : null;
            if (!string.Equals(color, "#FFFFFF", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(color, "#FFFFFFFF", StringComparison.OrdinalIgnoreCase))
                throw RuntimeTilemapMetadata.Invalid("Поле tintcolor слоя поддерживает только непрозрачный белый цвет.");
        }
    }

    private static double ReadLayerNumber(JsonElement layer, string name, double defaultValue)
    {
        if (!layer.TryGetProperty(name, out var value)) return defaultValue;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            throw RuntimeTilemapMetadata.Invalid($"Поле {name} слоя должно быть конечным числом DOUBLE.");
        return number;
    }

    private static RuntimeTilemapCell[] ParseLayerCells(JsonElement layerElement, int width, int height, IReadOnlyList<RuntimeTilemapTilesetReference> tilesets, Func<bool>? isCancellationRequested)
    {
        if (layerElement.TryGetProperty("chunks", out _)) throw RuntimeTilemapMetadata.Invalid("Слои chunks пока не поддерживаются.");
        if (layerElement.TryGetProperty("encoding", out var encoding)
            && (encoding.ValueKind != JsonValueKind.String || encoding.GetString() != "csv"))
            throw RuntimeTilemapMetadata.Invalid("Поддерживается только JSON-массив data с encoding csv.");
        if (layerElement.TryGetProperty("compression", out var compression)
            && (compression.ValueKind != JsonValueKind.String || compression.GetString() != ""))
            throw RuntimeTilemapMetadata.Invalid("Сжатые данные слоя пока не поддерживаются.");
        if (!layerElement.TryGetProperty("data", out var dataElement) || dataElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("LOADTILEMAP: tile layer должен содержать JSON-массив data.");
        }

        var cells = new RuntimeTilemapCell[checked(width * height)];
        var index = 0;
        foreach (var rawValue in dataElement.EnumerateArray())
        {
            if ((index & 4095) == 0) CheckCancellation(isCancellationRequested);
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
        const uint flipMask = flipHorizontal | flipVertical | flipDiagonal | 0x10000000;

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
        if (normalizedGlobalId == 0) return default;
        var selectedIndex = -1;
        for (var index = 0; index < tilesets.Count; index++)
        {
            if (tilesets[index].FirstGlobalId <= normalizedGlobalId
                && (selectedIndex < 0 || tilesets[index].FirstGlobalId > tilesets[selectedIndex].FirstGlobalId))
                selectedIndex = index;
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

    private static (long MinLeft, long MaxRight, long MinTop, long MaxBottom) ComputeTileExtents(IReadOnlyList<RuntimeTilemapTilesetReference> tilesets)
    {
        if (tilesets.Count == 0)
        {
            return (0, 0, 0, 0);
        }

        var minLeft = long.MaxValue;
        var maxRight = long.MinValue;
        var minTop = long.MaxValue;
        var maxBottom = long.MinValue;
        foreach (var tilesetRef in tilesets)
        {
            var tileset = tilesetRef.Tileset;
            minLeft = Math.Min(minLeft, tileset.TileOffsetX);
            maxRight = Math.Max(maxRight, (long)tileset.TileOffsetX + tileset.TileWidth - 1);
            minTop = Math.Min(minTop, tileset.TileOffsetY);
            maxBottom = Math.Max(maxBottom, (long)tileset.TileOffsetY + tileset.TileHeight - 1);
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

    private static (string BaseDirectory, string ImagePath, int TileWidth, int TileHeight, int Margin, int Spacing, int TileOffsetX, int TileOffsetY, IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> TileProperties, JsonElement Definition) ResolveTileset(string baseDirectory, JsonElement tilesetEntry)
    {
        JsonElement definition = tilesetEntry;
        var definitionBaseDirectory = baseDirectory;
        if (tilesetEntry.TryGetProperty("source", out var sourceElement))
        {
            var sourcePath = ResolveRelativePath(baseDirectory, sourceElement.GetString() ?? string.Empty);
            using var tilesetDocument = ReadBoundedJsonFile(sourcePath);
            definition = tilesetDocument.RootElement.Clone();
            definitionBaseDirectory = Path.GetDirectoryName(sourcePath) ?? baseDirectory;
        }

        if (!definition.TryGetProperty("image", out var imageElement))
        {
            throw new InvalidDataException("LOADTILEMAP: image collection tilesets пока не поддерживаются.");
        }

        foreach (var unsupported in new[] { "wangsets", "terrains" })
        {
            if (definition.TryGetProperty(unsupported, out _))
                throw RuntimeTilemapMetadata.Invalid($"Метаданные {unsupported} пока не поддерживаются.");
        }

        if (definition.TryGetProperty("tilerendersize", out var renderSize)
            && (renderSize.ValueKind != JsonValueKind.String || renderSize.GetString() != "tile"))
            throw RuntimeTilemapMetadata.Invalid("tilerendersize поддерживает только tile.");
        if (definition.TryGetProperty("fillmode", out var fillMode)
            && (fillMode.ValueKind != JsonValueKind.String || fillMode.GetString() is not ("stretch" or "preserve-aspect-fit")))
            throw RuntimeTilemapMetadata.Invalid("Неподдерживаемый fillmode тайлсета.");
        if (definition.TryGetProperty("transparentcolor", out _))
            throw RuntimeTilemapMetadata.Invalid("transparentcolor тайлсета пока не поддерживается; используйте альфа-канал изображения.");
        var tileOffsetX = 0;
        var tileOffsetY = 0;
        if (definition.TryGetProperty("tileoffset", out var tileOffsetElement))
        {
            tileOffsetX = tileOffsetElement.TryGetProperty("x", out var offsetXElement) ? offsetXElement.GetInt32() : 0;
            tileOffsetY = tileOffsetElement.TryGetProperty("y", out var offsetYElement) ? offsetYElement.GetInt32() : 0;
        }

        var tileProperties = new Dictionary<int, IReadOnlyDictionary<string, string>>();
        if (definition.TryGetProperty("tiles", out var tileEntries))
        {
            if (tileEntries.ValueKind != JsonValueKind.Array) throw RuntimeTilemapMetadata.Invalid("tiles должен быть массивом.");
            foreach (var tile in tileEntries.EnumerateArray())
            {
                if (tile.ValueKind != JsonValueKind.Object || !tile.TryGetProperty("id", out var id)
                    || !id.TryGetInt32(out var index) || index < 0 || index == int.MaxValue) throw RuntimeTilemapMetadata.Invalid("Неверный id тайла.");
                if (new[] { "x", "y", "width", "height", "image" }.Any(field => tile.TryGetProperty(field, out _)))
                    throw RuntimeTilemapMetadata.Invalid("Отдельное изображение или прямоугольник тайла пока не поддерживается.");
                if (tile.TryGetProperty("terrain", out _)) throw RuntimeTilemapMetadata.Invalid("Метаданные terrain тайла пока не поддерживаются.");
                if (tile.TryGetProperty("animation", out _)) throw RuntimeTilemapMetadata.Invalid("Анимация тайлов пока не поддерживается.");
                if (!tileProperties.TryAdd(index + 1, RuntimeTilemapMetadata.ReadProperties(tile)))
                    throw RuntimeTilemapMetadata.Invalid("id тайла повторяется.");
            }
        }
        return (
            definitionBaseDirectory,
            imageElement.GetString() ?? string.Empty,
            definition.GetProperty("tilewidth").GetInt32(),
            definition.GetProperty("tileheight").GetInt32(),
            definition.TryGetProperty("margin", out var marginElement) ? marginElement.GetInt32() : 0,
            definition.TryGetProperty("spacing", out var spacingElement) ? spacingElement.GetInt32() : 0,
            tileOffsetX,
            tileOffsetY, new System.Collections.ObjectModel.ReadOnlyDictionary<int, IReadOnlyDictionary<string, string>>(tileProperties), definition);
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
