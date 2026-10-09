using System.IO;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Tests;
public sealed class TilemapCullingBoundaryTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, "x")]
    [InlineData(ExecutionMode.Bytecode, "x")]
    [InlineData(ExecutionMode.Interpreter, "y")]
    [InlineData(ExecutionMode.Bytecode, "y")]
    public void LargeTilesetOffsetCancelledByLayerOffsetPreservesVisiblePixel(ExecutionMode mode, string axis)
    {
        var reference = Run("CANVAS 64, 64\nDIM t AS TILESET\nt = LOADTILESET(\"../../graphics/sprites/assets/walk_sheet.png\", 32, 32)\nDRAWTILE t, 1, 0, 0\nPRINT POINT(8, 8)", mode);
        Assert.Equal(ProgramRunStatus.Success, reference.Status);
        Assert.NotEqual("0", reference.OutputText);
        var coordinates = "0, 0";
        var result = Run("CANVAS 64, 64\nDIM m AS TILEMAP\nm = LOADTILEMAP(\"offset_limit_" + axis + ".tmj\")\nDRAWTILEMAP m, " + coordinates + "\nPRINT POINT(8, 8)", mode);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(reference.OutputText, result.OutputText);
    }
    [Theory]
    [InlineData(ExecutionMode.Interpreter, 100000000000L)]
    [InlineData(ExecutionMode.Bytecode, 100000000000L)]
    [InlineData(ExecutionMode.Interpreter, -100000000000L)]
    [InlineData(ExecutionMode.Bytecode, -100000000000L)]
    public void FarOffscreenMapIsSkippedWithoutOverflow(ExecutionMode mode, long origin)
    {
        var file = origin > 0 ? "far_layer_positive.tmj" : "far_layer_negative.tmj";
        var result = Run("CANVAS 64, 64\nDIM m AS TILEMAP\nm = LOADTILEMAP(\"" + file + "\")\nDRAWTILEMAP m, 0, 0\nPRINT \"Готово\"", mode);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal("Готово", result.OutputText);
    }
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void BoundaryAcceptanceSamplePreservesPixelAndCompletes(ExecutionMode mode)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var reference = Run("CANVAS 64, 64\nDIM t AS TILESET\nt = LOADTILESET(\"../../graphics/sprites/assets/walk_sheet.png\", 32, 32)\nDRAWTILE t, 1, 0, 0\nPRINT POINT(8, 8)", mode);
        Assert.Equal(ProgramRunStatus.Success, reference.Status);
        Assert.NotEqual("0", reference.OutputText);
        var source = File.ReadAllText(Path.Combine(repository.FullName, "samples/operators/drawtilemap/culling_boundaries.axm"));
        var result = Run(source, mode);
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(string.Join(Environment.NewLine, "Пиксель видимого тайла:", reference.OutputText, "Далёкая карта обработана."), result.OutputText);
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
