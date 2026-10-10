namespace Axiom.Execution.Tests;

public sealed class PathNullCharacterEvidenceTests
{
    [Fact]
    public void AcceptanceSampleRejectsNullPath()
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(
            TestProgramLoader.LoadText("samples/functions/fullpath/null_character.axm")));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
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
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        {
            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal("MB3007", diagnostic.Code);
            Assert.Equal(source.IndexOf(function, StringComparison.Ordinal), diagnostic.Location.Span.Start);
        }

        {
            var caught = new AxiomExecutionPipeline().Run(new ExecutionRequest($"TRY\n{source}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT \"Завершено\"\nEND TRY\nPRINT \"Продолжение\""));
            Assert.Equal(ExecutionStatus.Success, caught.Status);
            Assert.Empty(caught.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, caught.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, caught.Output);
        }
    }
}
