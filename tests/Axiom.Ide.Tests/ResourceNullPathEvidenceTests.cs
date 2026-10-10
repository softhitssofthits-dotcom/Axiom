using System.IO;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class ResourceNullPathEvidenceTests
{
    [Fact]
    public void AcceptanceSampleRejectsNullPath()
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var file = Path.Combine(repository.FullName, "samples/smoke/resources/null_path.axm");
        var result = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run(File.ReadAllText(file), sourceFilePath: file);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(string.Join(Environment.NewLine, "Отказ загрузки: MB3005", "Программа продолжает работу"), result.OutputText);
    }

    public static IEnumerable<object[]> Cases()
    {

        foreach (var kind in new[] { "IMAGE", "SPRITE", "TILESET", "TILEMAP" })
            yield return [kind];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ExistingPrefixResourceMustNotLoadWhenPathContainsNull(string kind)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var relative = kind == "TILEMAP" ? "samples/operators/drawtilemap/simple_map.tmj" : "samples/graphics/sprites/assets/walk_sheet.png";
        var path = Path.Combine(repository.FullName, relative);
        Assert.True(File.Exists(path));
        var call = kind switch
        {
            "IMAGE" => "LOADIMAGE(path)",
            "SPRITE" => "LOADSPRITE(path, 16, 16)",
            "TILESET" => "LOADTILESET(path, 16, 16)",
            _ => "LOADTILEMAP(path)"
        };
        var source = $$"""
            DIM path AS STRING = "{{path}}" + CHR(0) + "другой.png"
            DIM resource AS {{kind}}
            TRY
                resource = {{call}}
                PRINT "Ошибочно загружен префикс"
            CATCH err
                PRINT err.Code
            END TRY
            PRINT "Продолжение"
            """;
        var result = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run(source);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(string.Join(Environment.NewLine, "MB3005", "Продолжение"), result.OutputText);
    }
}
