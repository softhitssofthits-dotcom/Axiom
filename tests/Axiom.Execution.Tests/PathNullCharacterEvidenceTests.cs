namespace Axiom.Execution.Tests;

public sealed class PathNullCharacterEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void AcceptanceSampleRejectsNullPath(ExecutionMode mode)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            TestProgramLoader.LoadText("samples/functions/fullpath/null_character.axm"), mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { "Ошибка пути: MB3007", "Программа продолжает работу" }, result.Output);
    }

    [Theory]
    [InlineData("FULLPATH")]
    [InlineData("FILENAME")]
    [InlineData("DIRNAME")]
    [InlineData("EXTNAME")]
    [InlineData("PATHROOT")]
    public void EmbeddedNullIsReportedAtCallAndCaughtWithoutInfrastructureFailure(string function)
    {
        var source = $"PRINT {function}(\"папка\" + CHR(0) + \"файл.txt\")";
        var results = new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode }
            .Select(mode => new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode))).ToArray();
        foreach (var result in results)
        {
            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            Assert.False(result.UsedInterpreterFallback);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB3007", diagnostic.Code);
            Assert.Equal(source.IndexOf(function, StringComparison.Ordinal), diagnostic.Location.Span.Start);
        }
        Assert.Equal(results[0].Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)),
            results[1].Diagnostics.Select(d => (d.Code, d.Location.Line, d.Location.Column, d.Location.Span)));
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        {
            var caught = new AxiomExecutionPipeline().Run(new ExecutionRequest($"TRY\n{source}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT \"Завершено\"\nEND TRY\nPRINT \"Продолжение\"", mode: mode));
            Assert.Equal(ExecutionStatus.Success, caught.Status);
            Assert.Empty(caught.Diagnostics);
            Assert.False(caught.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, caught.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, caught.Output);
        }
    }
}
