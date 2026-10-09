using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinTilemapMetadataHelpers
{
    internal static RuntimeValue Evaluate(BuiltinFunctionId id, IReadOnlyList<RuntimeValue> args) =>
        Evaluate(id, (args is RuntimeValue[] array ? array : args.ToArray()).AsSpan());

    internal static RuntimeValue Evaluate(BuiltinFunctionId id, ReadOnlySpan<RuntimeValue> args)
    {
        var map = args[0].AsTilemap();
        var layer = args.Length > 1 && args[1].Type == TypeSymbol.Long ? Layer(map, args[1].AsLong()) : null;
        var item = args.Length > 2 && args[2].Type == TypeSymbol.Long ? Object(layer, args[2].AsLong()) : null;
        return id switch
        {
            BuiltinFunctionId.MapTilesetCount => RuntimeValue.FromInteger(map?.TilesetCount ?? 0),
            BuiltinFunctionId.MapProperty => Text(Property(map?.Properties, args[1].AsString())),
            BuiltinFunctionId.MapLayerProperty => Text(Property(layer?.Properties, args[2].AsString())),
            BuiltinFunctionId.MapTileProperty => Text(TileProperty(map, layer, args[2].AsLong(), args[3].AsLong(), args[4].AsString())),
            BuiltinFunctionId.MapObjectProperty => Text(Property(item?.Properties, args[3].AsString())),
            BuiltinFunctionId.MapLayerName => Text(layer?.Name),
            BuiltinFunctionId.MapLayerType => Text(layer is null ? "" : layer.IsTileLayer ? "TILE" : "OBJECT"),
            BuiltinFunctionId.MapLayerVisible => RuntimeValue.FromBoolean(layer?.Visible ?? false),
            BuiltinFunctionId.MapObjectCount => RuntimeValue.FromInteger(layer?.Objects.Count ?? 0),
            BuiltinFunctionId.MapObjectName => Text(item?.Name),
            BuiltinFunctionId.MapObjectClass => Text(item?.ClassName),
            BuiltinFunctionId.MapObjectKind => Text(item?.Kind),
            BuiltinFunctionId.MapObjectVisible => RuntimeValue.FromBoolean(item?.Visible ?? false),
            BuiltinFunctionId.MapObjectX => Number(item?.X),
            BuiltinFunctionId.MapObjectY => Number(item?.Y),
            BuiltinFunctionId.MapObjectWidth => Number(item?.Width),
            BuiltinFunctionId.MapObjectHeight => Number(item?.Height),
            BuiltinFunctionId.MapColumn => RuntimeValue.FromInteger(Cell(map, args[1].AsDouble(), args[2].AsDouble(), true)),
            BuiltinFunctionId.MapRow => RuntimeValue.FromInteger(Cell(map, args[1].AsDouble(), args[2].AsDouble(), false)),
            BuiltinFunctionId.MapWorldX => Number(World(map, args[1].AsDouble(), args[2].AsLong(), true)),
            BuiltinFunctionId.MapWorldY => Number(World(map, args[1].AsDouble(), args[2].AsLong(), false)),
            _ => throw new InvalidOperationException()
        };
    }

    private static RuntimeValue Text(string? value) => RuntimeValue.FromString(value ?? "");
    private static RuntimeValue Number(double? value) => RuntimeValue.FromDouble(value ?? 0);
    private static string Property(IReadOnlyDictionary<string, string>? properties, string name) =>
        properties is not null && properties.TryGetValue(name, out var value) ? value : "";
    private static RuntimeTilemapLayer? Layer(RuntimeTilemapValue? map, long index) =>
        map is not null && index >= 1 && index <= map.LayerCount ? map.Layers[(int)index - 1] : null;
    private static RuntimeTilemapObject? Object(RuntimeTilemapLayer? layer, long index) =>
        layer is not null && index >= 1 && index <= layer.Objects.Count ? layer.Objects[(int)index - 1] : null;

    private static string TileProperty(RuntimeTilemapValue? map, RuntimeTilemapLayer? layer, long column, long row, string name)
    {
        if (map is null || layer is null || !layer.IsTileLayer || column < 1 || row < 1 || column > layer.Width || row > layer.Height) return "";
        var cell = layer.GetCell((int)column - 1, (int)row - 1);
        if (cell.IsEmpty || cell.TilesetIndex > map.TilesetCount) return "";
        var reference = map.Tilesets[cell.TilesetIndex - 1];
        return reference.TileProperties.TryGetValue(cell.TileIndex, out var properties) ? Property(properties, name) : "";
    }

    private static int Cell(RuntimeTilemapValue? map, double origin, double world, bool horizontal)
    {
        if (map is null) return 0;
        var delta = world - origin;
        var count = horizontal ? map.Width : map.Height;
        var size = horizontal ? map.TileWidth : map.TileHeight;
        if (!double.IsFinite(delta) || delta < 0 || delta >= (double)count * size) return 0;
        return (int)Math.Floor(delta / size) + 1;
    }

    private static double World(RuntimeTilemapValue? map, double origin, long index, bool horizontal)
    {
        if (map is null || index < 1 || index > (horizontal ? map.Width : map.Height)) return 0;
        return origin + (index - 1) * (double)(horizontal ? map.TileWidth : map.TileHeight);
    }
}
