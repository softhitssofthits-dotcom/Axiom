namespace Axiom.Execution.Tests;

public sealed class AuditContinuationSampleTests
{
    [Fact]
    public void PrintSampleKeepsEmptyAndJoinedLines()
    {
        var run = RunSample("operators/print/console_line_boundaries.axm");
        Assert.Equal(new[] { "Первая строка", "", "Начало: конец", "Число: 42", "Завершено" }, run.Output);
    }

    [Fact]
    public void PowSampleShowsRepresentableUnderflowBoundary()
    {
        var run = RunSample("functions/pow/rounding_boundary.axm");
        Assert.Equal(4, run.Output.Count);
        Assert.Equal("Минимальная положительная степень: 5E-324", run.Output[0]);
        Assert.Equal("Результат на середине в этой среде: " + Math.Pow(2, -1075).ToString("G", System.Globalization.CultureInfo.InvariantCulture), run.Output[1]);
        Assert.Equal("Результат в проверенном интервале: TRUE", run.Output[2]);
        Assert.Equal("Ещё меньшая степень: 0", run.Output[3]);
    }

    private static ExecutionResult RunSample(string relativePath)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PROJECT.md")))
            root = root.Parent;
        Assert.NotNull(root);
        var path = Path.Combine(root.FullName, "samples", relativePath);
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(File.ReadAllText(path)));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        return run;
    }
}
