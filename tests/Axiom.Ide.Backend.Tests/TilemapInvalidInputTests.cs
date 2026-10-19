using System.IO;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Backend.Tests;
public sealed class TilemapInvalidInputTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var index in Enumerable.Range(0, 20)) yield return [index];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidFieldsProduceCatchableDiagnosticAndPreservePreviousMap(int index)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-tile-invalid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var good = JsonNode.Parse("""
                {"orientation":"orthogonal","infinite":false,"width":1,"height":1,"tilewidth":16,"tileheight":16,"tilesets":[],"layers":[]}
                """)!;
            File.WriteAllText(Path.Combine(root, "good.tmj"), good.ToJsonString());
            var bad = good.DeepClone();
            var repository = new DirectoryInfo(AppContext.BaseDirectory);
            while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
            Assert.NotNull(repository);
            var tileset = new JsonObject { ["firstgid"] = 1, ["image"] = Path.Combine(repository.FullName, "samples/graphics/sprites/assets/walk_sheet.png"), ["tilewidth"] = 16, ["tileheight"] = 16 };
            switch (index)
            {
                case 0: bad["width"] = -1; break;
                case 1: bad["tilewidth"] = 0; break;
                case 2: bad.AsObject().Remove("width"); break;
                case 3: bad.AsObject().Remove("height"); break;
                case 4: bad["height"] = "один"; break;
                case 5: bad["width"] = 1.5; break;
                case 6: bad["width"] = 1000000000000L; break;
                case 7: bad["infinite"] = "да"; break;
                case 8: bad["layers"] = 42; break;
                case 9: bad.AsObject().Remove("layers"); break;
                case 10: bad["tileheight"] = -1; break;
                case 11: bad["tilesets"] = 42; break;
                case 12: bad["layers"] = JsonNode.Parse("[{\"width\":1,\"height\":1,\"data\":[0]}]"); break;
                case 13: bad["layers"] = JsonNode.Parse("[{\"type\":\"tilelayer\",\"width\":1.5,\"height\":1,\"data\":[0]}]"); break;
                case 14: bad["layers"] = JsonNode.Parse("[{\"type\":\"tilelayer\",\"width\":-1,\"height\":-1,\"data\":[0]}]"); break;
                case 15: tileset["tilewidth"] = 15; bad["tilesets"] = new JsonArray(tileset); break;
                case 16: bad["renderorder"] = 42; break;
                case 17: tileset.Remove("tilewidth"); bad["tilesets"] = new JsonArray(tileset); break;
                case 18: tileset["firstgid"] = 1.5; bad["tilesets"] = new JsonArray(tileset); break;
                case 19: tileset["firstgid"] = -1; bad["tilesets"] = new JsonArray(tileset); break;
            }
            File.WriteAllText(Path.Combine(root, "bad.tmj"), bad.ToJsonString());
            var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
            var result = service.Run("""
                DIM m AS TILEMAP
                m = LOADTILEMAP("good.tmj")
                TRY
                    m = LOADTILEMAP("bad.tmj")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT MAPWIDTH(m)
                """, sourceFilePath: Path.Combine(root, "check.axm"));
            Assert.Equal(ProgramRunStatus.Success, result.Status);
            Assert.Equal(string.Join(Environment.NewLine, "MB3005", "1"), result.OutputText);
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }
}
