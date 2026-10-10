using System.IO;
using System.Text.Json.Nodes;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class NestedResourceNullPathEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var variant in new[] { "embedded-image", "external-image", "external-source" })
            yield return [variant];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NestedPathWithNullCannotLoadPrefixResource(string variant)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var artifactRoot = Path.Combine(repository.FullName, ".artifacts", "nested-null-tests");
        var root = Path.Combine(artifactRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var map = JsonNode.Parse(File.ReadAllText(Path.Combine(repository.FullName, "samples/operators/drawtilemap/simple_map.tmj")))!;
            var tiles = map["tilesets"]![0]!;
            var image = Path.Combine(repository.FullName, "samples/graphics/sprites/assets/walk_sheet.png");
            tiles["image"] = image + (variant == "external-source" ? "" : "\0другой.png");
            if (variant != "embedded-image")
            {
                var definition = tiles.DeepClone();
                definition.AsObject().Remove("firstgid");
                var file = Path.Combine(root, "atlas.tsj");
                File.WriteAllText(file, definition.ToJsonString());
                map["tilesets"] = new JsonArray(new JsonObject
                {
                    ["firstgid"] = 1,
                    ["source"] = file + (variant == "external-source" ? "\0другой.tsj" : "")
                });
            }
            File.WriteAllText(Path.Combine(root, "map.tmj"), map.ToJsonString());
            var source = "DIM map AS TILEMAP\nTRY\nmap = LOADTILEMAP(\"map.tmj\")\nPRINT \"Ошибочная загрузка\"\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT \"Продолжение\"";
            var result = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run(source, sourceFilePath: Path.Combine(root, "check.axm"));
            Assert.Equal(ProgramRunStatus.Success, result.Status);
            Assert.Equal(string.Join(Environment.NewLine, "MB3005", "Продолжение"), result.OutputText);
        }
        finally
        {
            Assert.Equal(Path.GetFullPath(artifactRoot), Path.GetDirectoryName(Path.GetFullPath(root)));
            Directory.Delete(root, recursive: true);
        }
    }
}
