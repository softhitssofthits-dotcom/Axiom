using System.IO;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Backend.Tests;
public sealed class TilemapUnsupportedFeatureTests
{
    public static IEnumerable<object[]> Cases()
    {

        for (var index = 0; index < 18; index++)
        {
            yield return [index, false];
            if (index is 7 or 12 or 13 or 14) yield return [index, true];
        }
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void DeferredFeaturesFailInsteadOfBeingSilentlyDiscarded(int index, bool external)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var root = Path.Combine(Path.GetTempPath(), "axiom-tiled-unsupported-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var map = JsonNode.Parse("""
                {"orientation":"orthogonal","infinite":false,"width":1,"height":1,"tilewidth":16,"tileheight":16,"tilesets":[],"layers":[]}
                """)!;
            var tileset = new JsonObject { ["firstgid"] = 1, ["image"] = Path.Combine(directory.FullName, "samples/graphics/sprites/assets/walk_sheet.png"), ["tilewidth"] = 16, ["tileheight"] = 16 };
            map["tilesets"] = new JsonArray(tileset);
            switch (index)
            {
                case 1: map["orientation"] = "isometric"; break;
                case 2: map["orientation"] = "staggered"; break;
                case 3: map["orientation"] = "hexagonal"; break;
                case 4: map["infinite"] = true; break;
                case 5: map["layers"] = JsonNode.Parse("[{\"type\":\"group\",\"name\":\"Группа\",\"layers\":[]}]"); break;
                case 6: tileset.Remove("image"); tileset["tiles"] = JsonNode.Parse("[{\"id\":0,\"image\":\"tile.png\"}]"); break;
                case 7: tileset["tiles"] = JsonNode.Parse("[{\"id\":0,\"animation\":[{\"tileid\":0,\"duration\":100}]}]"); break;
                case 8: AddObject(map, "{\"ellipse\":true}"); break;
                case 9: AddObject(map, "{\"polygon\":[{\"x\":0,\"y\":0},{\"x\":1,\"y\":0},{\"x\":0,\"y\":1}]}"); break;
                case 10: AddObject(map, "{\"polyline\":[{\"x\":0,\"y\":0},{\"x\":1,\"y\":1}]}"); break;
                case 11: AddObject(map, "{\"gid\":1}"); break;
                case 12: tileset["wangsets"] = JsonNode.Parse("[{\"name\":\"Дорога\",\"type\":\"edge\",\"tile\":0,\"colors\":[],\"wangtiles\":[]}]"); break;
                case 13: tileset["terrains"] = JsonNode.Parse("[{\"name\":\"Земля\",\"tile\":0}]"); break;
                case 14: tileset["tiles"] = JsonNode.Parse("[{\"id\":0,\"terrain\":[0,0,0,0]}]"); break;
                case 15: File.WriteAllText(Path.Combine(root, "atlas.tsx"), "<tileset name=\"Атлас\" tilewidth=\"16\" tileheight=\"16\"/>"); map["tilesets"] = JsonNode.Parse("[{\"firstgid\":1,\"source\":\"atlas.tsx\"}]"); break;
                case 16: AddObject(map, "{\"capsule\":true,\"width\":8,\"height\":16}"); break;
                case 17: map["layers"] = JsonNode.Parse("[{\"type\":\"imagelayer\",\"name\":\"Фон\",\"image\":\"image.png\"}]"); break;
            }
            if (external)
            {
                var definition = tileset.DeepClone(); definition.AsObject().Remove("firstgid");
                File.WriteAllText(Path.Combine(root, "atlas.tsj"), definition.ToJsonString());
                map["tilesets"] = JsonNode.Parse("[{\"firstgid\":1,\"source\":\"atlas.tsj\"}]");
            }
            var file = index == 0 ? "map.tmx" : "map.tmj";
            File.WriteAllText(Path.Combine(root, file), index == 0 ? "<map orientation=\"orthogonal\" width=\"1\" height=\"1\" tilewidth=\"16\" tileheight=\"16\"/>" : map.ToJsonString());
            var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
            var source = "DIM m AS TILEMAP\nTRY\nm = LOADTILEMAP(\"" + file + "\")\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT MAPWIDTH(m)";
            var result = service.Run(source, sourceFilePath: Path.Combine(root, "check.axm"));
            Assert.Equal(ProgramRunStatus.Success, result.Status);
            Assert.Equal(string.Join(Environment.NewLine, "MB3005", "0"), result.OutputText);
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }
    private static void AddObject(JsonNode map, string json)
    {
        map["layers"] = new JsonArray(new JsonObject { ["type"] = "objectgroup", ["objects"] = new JsonArray(JsonNode.Parse(json)) });
    }
}
