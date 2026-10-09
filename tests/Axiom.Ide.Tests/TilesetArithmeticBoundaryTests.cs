using System.IO;
using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;
namespace Axiom.Ide.Tests;
public sealed class TilesetArithmeticBoundaryTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, false, false)]
    [InlineData(ExecutionMode.Interpreter, false, true)]
    [InlineData(ExecutionMode.Bytecode, false, false)]
    [InlineData(ExecutionMode.Bytecode, false, true)]
    [InlineData(ExecutionMode.Interpreter, true, false)]
    [InlineData(ExecutionMode.Interpreter, true, true)]
    [InlineData(ExecutionMode.Bytecode, true, false)]
    [InlineData(ExecutionMode.Bytecode, true, true)]
    public void AtlasArithmeticUsesActualGeometryWithoutIntegerWrap(ExecutionMode mode, bool largeSpacing, bool map)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var args = largeSpacing ? "32, 32, 0, 2147483647" : "17, 17, 2147483647, 0";
        var source = "DIM t AS TILESET\nTRY\nt = LOADTILESET(\"../../graphics/sprites/assets/walk_sheet.png\", " + args + ")\nPRINT TILECOUNT(t)\nCATCH err\nPRINT err.Code\nEND TRY";
        if (map)
        {
            var file = largeSpacing ? "large_spacing.tmj" : "large_margin.tmj";
            source = "DIM m AS TILEMAP\nTRY\nm = LOADTILEMAP(\"../loadtilemap/" + file + "\")\nPRINT MAPTILE(m, 1, 1, 1)\nCATCH err\nPRINT err.Code\nEND TRY";
        }
        var result = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService()).Run(source, mode,
            sourceFilePath: Path.Combine(repository.FullName, "samples/functions/loadtileset/check.axm"));
        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(largeSpacing ? "1" : "MB3005", result.OutputText);
    }
}
