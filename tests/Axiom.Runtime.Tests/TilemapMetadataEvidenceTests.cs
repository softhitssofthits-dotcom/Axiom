using System.Text.Json;
using System.Text.Json.Nodes;

namespace Axiom.Runtime.Tests;

public sealed class TilemapMetadataEvidenceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CustomPropertiesAndObjectsArePreservedOrRejected(bool malformedIgnoredFields, bool externalTileset)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-tile-metadata-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "карта.tmj");
            var baseline = JsonNode.Parse("""
                {
                  "orientation":"orthogonal","infinite":false,"width":1,"height":1,"tilewidth":1,"tileheight":1,
                  "tilesets":[{"firstgid":1,"image":"атлас.png","tilewidth":1,"tileheight":1}],
                  "layers":[
                    {"type":"tilelayer","name":"Земля","width":1,"height":1,"data":[1]},
                    {"type":"objectgroup","name":"Объекты","objects":[]}
                  ]
                }
                """)!;
            RuntimeImageValue Load(string resolved)
            {
                Assert.Equal(Path.Combine(root, "атлас.png"), resolved);
                return new RuntimeImageValue(1, 1, new uint[] { 0xff112233 });
            }
            File.WriteAllText(path, baseline.ToJsonString());
            var plain = RuntimeTilemapValue.LoadFromTiledJson(path, Load);
            var decorated = baseline.DeepClone();
            var properties = JsonNode.Parse(malformedIgnoredFields ? "42" : """
                [{"name":"role","type":"string","value":"выход😀"},{"name":"solid","type":"bool","value":true}]
                """)!;
            decorated["properties"] = properties.DeepClone();
            foreach (var layer in decorated["layers"]!.AsArray()) layer!["properties"] = properties.DeepClone();
            decorated["tilesets"]![0]!["tiles"] = JsonNode.Parse("""
                [{"id":0,"properties":[{"name":"solid","type":"bool","value":true}]}]
                """);
            decorated["layers"]![1]!["objects"] = JsonNode.Parse(malformedIgnoredFields ? "42" : """
                [{"id":7,"name":"Выход","type":"portal","x":12.5,"y":24,"width":8,"height":16,"visible":true,
                  "properties":[{"name":"role","type":"string","value":"выход😀"}]}]
                """);
            if (externalTileset)
            {
                var definition = decorated["tilesets"]![0]!.DeepClone();
                definition.AsObject().Remove("firstgid");
                File.WriteAllText(Path.Combine(root, "атлас.tsj"), definition.ToJsonString());
                decorated["tilesets"]![0] = JsonNode.Parse("{\"firstgid\":1,\"source\":\"атлас.tsj\"}");
            }
            File.WriteAllText(path, decorated.ToJsonString());
            if (malformedIgnoredFields)
            {
                Assert.Throws<InvalidDataException>(() => RuntimeTilemapValue.LoadFromTiledJson(path, Load));
                return;
            }
            var loaded = RuntimeTilemapValue.LoadFromTiledJson(path, Load);
            Assert.Equal(1, loaded.Width);
            Assert.Equal(1, loaded.Height);
            Assert.Equal(2, loaded.LayerCount);
            Assert.Equal(new RuntimeTilemapCell(1, 1, RuntimeTilemapFlipFlags.None), loaded.GetLayer1Based(1).GetCell(0, 0));
            Assert.Equal(RuntimeTilemapLayerKind.Object, loaded.GetLayer1Based(2).Kind);
            Assert.Equal("Объекты", loaded.GetLayer1Based(2).Name);
            Assert.Null(loaded.GetLayer1Based(2).Cells);
            Assert.Empty(plain.Properties);
            Assert.Equal("выход😀", loaded.Properties["role"]);
            Assert.Equal("TRUE", loaded.Properties["solid"]);
            Assert.Equal("выход😀", loaded.GetLayer1Based(1).Properties["role"]);
            Assert.Equal("TRUE", loaded.GetTileset1Based(1).TileProperties[1]["solid"]);
            var mapObject = Assert.Single(loaded.GetLayer1Based(2).Objects);
            Assert.Equal("Выход", mapObject.Name);
            Assert.Equal("portal", mapObject.ClassName);
            Assert.Equal("RECT", mapObject.Kind);
            Assert.True(mapObject.Visible);
            Assert.Equal(12.5, mapObject.X);
            Assert.Equal(24, mapObject.Y);
            Assert.Equal(8, mapObject.Width);
            Assert.Equal(16, mapObject.Height);
            Assert.Equal("выход😀", mapObject.Properties["role"]);
            Assert.Contains("properties", File.ReadAllText(path));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [Theory]
    [InlineData("\"properties\":42")]
    [InlineData("\"properties\":[{\"name\":\"a\",\"value\":null}]")]
    [InlineData("\"properties\":[{\"name\":\"a\",\"value\":[] }]")]
    [InlineData("\"properties\":[{\"name\":\"a\",\"value\":1},{\"name\":\"a\",\"value\":2}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":42}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"ellipse\":true}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"polygon\":[]}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"polyline\":[]}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"gid\":1}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"rotation\":90}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"x\":1e400}]}]")]
    [InlineData("\"layers\":[{\"type\":\"objectgroup\",\"objects\":[{\"width\":-1}]}]")]
    public void InvalidMetadataAndUnsupportedObjectsFailExplicitly(string fields)
    {
        var path = Path.Combine(Path.GetTempPath(), "axiom-metadata-invalid-" + Guid.NewGuid().ToString("N") + ".tmj");
        try
        {
            File.WriteAllText(path, "{\"orientation\":\"orthogonal\",\"width\":1,\"height\":1,\"tilewidth\":1,\"tileheight\":1,\"tilesets\":[],\"layers\":[]," + fields + "}");
            var error = Assert.Throws<InvalidDataException>(() => RuntimeTilemapValue.LoadFromTiledJson(path, _ => throw new InvalidOperationException()));
            Assert.StartsWith("LOADTILEMAP:", error.Message);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void PropertyNumbersKeepAllDigitsAndPointHasZeroSize()
    {
        var path = Path.Combine(Path.GetTempPath(), "axiom-metadata-point-" + Guid.NewGuid().ToString("N") + ".tmj");
        try
        {
            File.WriteAllText(path, """
                {"orientation":"orthogonal","width":1,"height":1,"tilewidth":1,"tileheight":1,"tilesets":[],
                "properties":[{"name":"number","type":"float","value":9007199254740993},{"name":"fraction","value":0.1234567890123456789012345678},
                {"name":"no","value":false},{"name":"Role","value":"Отдельное имя"}],
                "layers":[{"type":"objectgroup","objects":[{"name":"Точка","class":"spawn","type":"старый","point":true,"visible":false,"x":-0.5,"y":2,"width":7,"height":8}]}]}
                """);
            var loaded = RuntimeTilemapValue.LoadFromTiledJson(path, _ => throw new InvalidOperationException());
            Assert.Equal("9007199254740993", loaded.Properties["number"]);
            Assert.Equal("0.1234567890123456789012345678", loaded.Properties["fraction"]);
            Assert.Equal("FALSE", loaded.Properties["no"]);
            Assert.False(loaded.Properties.ContainsKey("role"));
            var point = Assert.Single(loaded.Layers[0].Objects);
            Assert.Equal("POINT", point.Kind);
            Assert.Equal("spawn", point.ClassName);
            Assert.False(point.Visible);
            Assert.Equal(-0.5, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(0, point.Width);
            Assert.Equal(0, point.Height);
        }
        finally { File.Delete(path); }
    }

}
