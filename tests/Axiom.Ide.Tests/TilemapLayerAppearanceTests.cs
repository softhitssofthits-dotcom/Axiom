using System.IO;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Tests;
public sealed class TilemapLayerAppearanceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var index in Enumerable.Range(0, 11))
        foreach (var objectLayer in new[] { false, true }) yield return [mode, index, objectLayer];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidFieldsProduceCatchableDiagnosticAndPreservePreviousMap(ExecutionMode mode, int index, bool objectLayer)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-layer-appearance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var good = JsonNode.Parse("""
                {"orientation":"orthogonal","infinite":false,"width":1,"height":1,"tilewidth":16,"tileheight":16,"tilesets":[],"layers":[]}
                """)!;
            File.WriteAllText(Path.Combine(root, "good.tmj"), good.ToJsonString());
            var bad = good.DeepClone();
            var fields = new[] { "mode", "mode", "mode", "mode", "tintcolor", "tintcolor", "mode", "tintcolor", "tintcolor", "tintcolor", "mode" };
            var values = new[] { "\"normal\"", "\"multiply\"", "\"add\"", "\"screen\"", "\"#FF0000\"", "\"#FFFFFF\"", "42", "42", "\"#ffffffff\"", "\"#80FFFFFF\"", "null" };
            var layer = JsonNode.Parse("{\"type\":\"tilelayer\",\"width\":1,\"height\":1,\"data\":[0]}")!;
            if (objectLayer)
            {
                layer["type"] = "objectgroup";
                layer.AsObject().Remove("data");
                layer["objects"] = new JsonArray();
            }
            layer[fields[index]] = JsonNode.Parse(values[index]);
            bad["layers"] = new JsonArray(layer);
            File.WriteAllText(Path.Combine(root, "bad.tmj"), bad.ToJsonString());
            var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
            var result = service.Run("""
                DIM m AS TILEMAP
                m = LOADTILEMAP("good.tmj")
                TRY
                    m = LOADTILEMAP("bad.tmj")
                CATCH err
                    PRINT err.Code
                    PRINT err.Message
                END TRY
                PRINT MAPWIDTH(m)
                """, mode, sourceFilePath: Path.Combine(root, "check.axm"));
            Assert.Equal(ProgramRunStatus.Success, result.Status);
            Assert.False(result.UsedInterpreterFallback);
            var reason = fields[index] == "tintcolor" ? "LOADTILEMAP: Поле tintcolor слоя поддерживает только непрозрачный белый цвет."
                : "LOADTILEMAP: Поле mode слоя поддерживает только строку normal.";
            Assert.Equal(index is 0 or 5 or 8 ? "1" : string.Join(Environment.NewLine, "MB3005", reason, "1"), result.OutputText);
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }
}
