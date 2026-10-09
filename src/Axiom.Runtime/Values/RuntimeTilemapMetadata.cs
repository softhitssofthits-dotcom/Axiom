using System.Collections.ObjectModel;
using System.Text.Json;

namespace Axiom.Runtime;

public sealed record RuntimeTilemapObject(string Name, string ClassName, string Kind, bool Visible,
    double X, double Y, double Width, double Height, IReadOnlyDictionary<string, string> Properties);

internal static class RuntimeTilemapMetadata
{
    internal static readonly IReadOnlyDictionary<string, string> Empty =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));

    internal static IReadOnlyDictionary<string, string> ReadProperties(JsonElement owner)
    {
        if (!owner.TryGetProperty("properties", out var source)) return Empty;
        if (source.ValueKind != JsonValueKind.Array) throw Invalid("properties должен быть массивом.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in source.EnumerateArray())
        {
            if (property.ValueKind != JsonValueKind.Object || !property.TryGetProperty("name", out var name)
                || name.ValueKind != JsonValueKind.String || !property.TryGetProperty("value", out var value))
                throw Invalid("Свойство должно содержать строковое имя и значение.");
            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString()!,
                JsonValueKind.True => "TRUE",
                JsonValueKind.False => "FALSE",
                JsonValueKind.Number or JsonValueKind.Object => value.GetRawText(),
                _ => throw Invalid("Неподдержанный вид значения свойства.")
            };
            if (!result.TryAdd(name.GetString()!, text)) throw Invalid("Имя свойства повторяется.");
        }
        return new ReadOnlyDictionary<string, string>(result);
    }

    internal static IReadOnlyList<RuntimeTilemapObject> ReadObjects(JsonElement layer)
    {
        if (!layer.TryGetProperty("objects", out var source)) return Array.Empty<RuntimeTilemapObject>();
        if (source.ValueKind != JsonValueKind.Array) throw Invalid("objects должен быть массивом.");
        var result = new List<RuntimeTilemapObject>();
        foreach (var item in source.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) throw Invalid("Объект карты должен быть JSON-объектом.");
            foreach (var unsupported in new[] { "ellipse", "capsule", "polygon", "polyline", "gid", "text", "template" })
                if (item.TryGetProperty(unsupported, out var field) && field.ValueKind != JsonValueKind.False)
                    throw Invalid($"Объекты {unsupported} пока не поддерживаются.");
            if (Number(item, "rotation") != 0) throw Invalid("Поворот объектов пока не поддерживается.");
            var point = Boolean(item, "point", false);
            var width = Number(item, "width");
            var height = Number(item, "height");
            if (width < 0 || height < 0) throw Invalid("Размер объекта не может быть отрицательным.");
            result.Add(new RuntimeTilemapObject(Text(item, "name"),
                item.TryGetProperty("class", out _) ? Text(item, "class") : Text(item, "type"),
                point ? "POINT" : "RECT", Boolean(item, "visible", true), Number(item, "x"), Number(item, "y"),
                point ? 0 : width, point ? 0 : height, ReadProperties(item)));
        }
        return result.AsReadOnly();
    }

    private static string Text(JsonElement owner, string name)
    {
        if (!owner.TryGetProperty(name, out var value)) return string.Empty;
        if (value.ValueKind != JsonValueKind.String) throw Invalid($"Поле {name} должно быть строкой.");
        return value.GetString()!;
    }

    private static bool Boolean(JsonElement owner, string name, bool defaultValue)
    {
        if (!owner.TryGetProperty(name, out var value)) return defaultValue;
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid($"Поле {name} должно быть BOOLEAN.");
        return value.GetBoolean();
    }

    private static double Number(JsonElement owner, string name)
    {
        if (!owner.TryGetProperty(name, out var value)) return 0;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var number) || !double.IsFinite(number))
            throw Invalid($"Поле {name} должно быть конечным числом.");
        return number;
    }

    internal static InvalidDataException Invalid(string message) => new("LOADTILEMAP: " + message);
}
