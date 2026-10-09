using System.IO;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Tests;
public sealed class TilemapExtremeTransformTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var axis in new[] { "x", "y" })
        for (var index = 0; index < 10; index++)
        foreach (var singleLayer in new[] { false, true }) yield return [mode, axis, index, singleLayer];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void ExtremeTransformIsClippedOrCancelledWithoutWrongPixel(ExecutionMode mode, string axis, int index, bool singleLayer)
    {
        var camera = index is 4 or 5 ? 1000 : index is 6 or 7 or 8 ? 1 : index == 9 ? -1 : 0;
        var origin = index == 2 ? 1 : index == 3 ? -1 : 0;
        var cameraCoordinates = axis == "x" ? camera + ", 0" : "0, " + camera;
        var originCoordinates = axis == "x" ? origin + ", 0" : "0, " + origin;
        var draw = singleLayer ? "DRAWTILELAYER m, 1, " : "DRAWTILEMAP m, ";
        var source = "CANVAS 64, 64\nCAMERA " + cameraCoordinates + "\nDIM m AS TILEMAP\nm = LOADTILEMAP(\"extreme_" + index + "_" + axis + ".tmj\")\n" + draw + originCoordinates + "\nPRINT POINT(8, 8)";
        var result = Run(source, mode);
        Assert.True(result.Status == ProgramRunStatus.Success, string.Join("; ", result.Diagnostics.Select(d => d.Code + ": " + d.Message)));
        Assert.False(result.UsedInterpreterFallback);
        if (index >= 6)
        {
            var reference = Run("CANVAS 64, 64\nCAMERA " + cameraCoordinates + "\nDIM t AS TILESET\nt = LOADTILESET(\"../../graphics/sprites/assets/walk_sheet.png\", 32, 32)\nDRAWTILE t, 1, " + cameraCoordinates + "\nPRINT POINT(8, 8)", mode);
            Assert.Equal(ProgramRunStatus.Success, reference.Status);
            Assert.NotEqual("0", reference.OutputText);
            Assert.Equal(reference.OutputText, result.OutputText);
        }
        else Assert.Equal("0", result.OutputText);
    }
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExtremeParallaxAcceptanceSampleRuns(ExecutionMode mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var source = File.ReadAllText(Path.Combine(repository.FullName, "samples/operators/drawtilemap/extreme_parallax.axm"));
        var result = Run(source, mode);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(string.Join(Environment.NewLine, "Невидимый слой:", "0", "Компенсированный слой:", "12"), result.OutputText);
    }
    private static ProgramRunResult Run(string source, ExecutionMode mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        return new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run(source, mode,
            sourceFilePath: Path.Combine(repository.FullName, "samples/operators/drawtilemap/check.axm"));
    }
}
