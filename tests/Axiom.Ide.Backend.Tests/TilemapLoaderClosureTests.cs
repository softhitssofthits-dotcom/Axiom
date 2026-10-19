using System.IO;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Backend.Tests;
public sealed class TilemapLoaderClosureTests
{
    public static IEnumerable<object[]> Cases()
    {

        for (var index = 0; index < 26; index++)
        {
            yield return [index, false];
            if (index is >= 4 and <= 10 or 16 or 17 or 20 or 21 or 22) yield return [index, true];
        }
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void FinalLoaderBoundariesAreExplicit(int index, bool external)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var artifactRoot = Path.Combine(repository.FullName, ".artifacts", "tile-loader-tests");
        var root = Path.Combine(artifactRoot, "axiom-tile-final-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var map = JsonNode.Parse("{\"orientation\":\"orthogonal\",\"width\":1,\"height\":1,\"tilewidth\":16,\"tileheight\":16,\"tilesets\":[],\"layers\":[{\"type\":\"tilelayer\",\"width\":1,\"height\":1,\"data\":[1]}]}")!;
            var tiles = new JsonObject { ["firstgid"] = 1, ["image"] = Path.Combine(repository.FullName, "samples/graphics/sprites/assets/walk_sheet.png"), ["tilewidth"] = 16, ["tileheight"] = 16 };
            map["tilesets"] = new JsonArray(tiles);
            switch (index)
            {
                case 0: map["parallaxoriginx"] = 1; break;
                case 1: map["parallaxoriginy"] = 1; break;
                case 2: map["parallaxoriginx"] = "один"; break;
                case 3: map["parallaxoriginy"] = JsonNode.Parse("1e400"); break;
                case 4: tiles["tilerendersize"] = "grid"; break;
                case 5: tiles["tilerendersize"] = "неизвестно"; break;
                case 6: tiles["fillmode"] = "неизвестно"; break;
                case 7: tiles["columns"] = 1; break;
                case 8: tiles["tilecount"] = 1; break;
                case 9: tiles["imagewidth"] = 31; break;
                case 10: tiles["imageheight"] = 31; break;
                case 11: map["tilesets"].AsArray().Add(tiles.DeepClone()); break;
                case 12: var overlap = tiles.DeepClone(); overlap["firstgid"] = 3; map["tilesets"].AsArray().Add(overlap); break;
                case 13:
                    var high = tiles.DeepClone(); high["firstgid"] = 100; map["tilesets"].AsArray().Add(high);
                    var middle = tiles.DeepClone(); middle["firstgid"] = 10; map["tilesets"].AsArray().Add(middle);
                    map["layers"][0]["data"][0] = 100; break;
                case 14: map["layers"][0]["data"][0] = 0x10000001; break;
                case 15: map["parallaxoriginx"] = 0; map["parallaxoriginy"] = 0; break;
                case 16: tiles["tilerendersize"] = "tile"; tiles["fillmode"] = "preserve-aspect-fit"; break;
                case 17: tiles["columns"] = 2; tiles["tilecount"] = 4; tiles["imagewidth"] = 32; tiles["imageheight"] = 32; break;
                case 18: tiles["firstgid"] = 0x10000000; break;
                case 19: map["parallaxoriginx"] = true; break;
                case 20: tiles["columns"] = 1.5; break;
                case 21: tiles["transparentcolor"] = "#FF00FF"; break;
                case 22: tiles["tiles"] = JsonNode.Parse("[{\"id\":0,\"x\":1,\"y\":1,\"width\":8,\"height\":8}]"); break;
                case 23: map["layers"][0]["chunks"] = new JsonArray(); break;
                case 24: map["layers"][0]["encoding"] = "base64"; break;
                case 25: map["layers"][0]["compression"] = "gzip"; break;
            }
            if (external)
            {
                var definition = tiles.DeepClone(); definition.AsObject().Remove("firstgid");
                File.WriteAllText(Path.Combine(root, "atlas.tsj"), definition.ToJsonString());
                map["tilesets"] = JsonNode.Parse("[{\"firstgid\":1,\"source\":\"atlas.tsj\"}]");
            }
            File.WriteAllText(Path.Combine(root, "map.tmj"), map.ToJsonString());
            var result = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run("DIM m AS TILEMAP\nTRY\nm = LOADTILEMAP(\"map.tmj\")\nPRINT MAPTILESET(m, 1, 1, 1)\nPRINT MAPTILE(m, 1, 1, 1)\nPRINT MAPTILEFLAGS(m, 1, 1, 1)\nCATCH err\nPRINT err.Code\nEND TRY", sourceFilePath: Path.Combine(root, "check.axm"));
            Assert.Equal(ProgramRunStatus.Success, result.Status);
            var expected = index == 13 ? string.Join(Environment.NewLine, "2", "1", "0")
                : index is 14 or 15 or 16 or 17 ? string.Join(Environment.NewLine, "1", "1", "0") : "MB3005";
            Assert.Equal(expected, result.OutputText);
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(artifactRoot).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }
}
