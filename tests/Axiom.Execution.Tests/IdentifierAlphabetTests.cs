using Axiom.Core;

namespace Axiom.Execution.Tests;

public sealed class IdentifierAlphabetTests
{
    [Fact]
    public void RussianNameAcceptanceSample_ReportsInvalidCharacter()
    {
        var source = TestProgramLoader.LoadText("samples/smoke/identifiers/russian_name_rejected.axm");
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledProgram);
        Assert.Equal(3, result.Diagnostics.Count(d => d.Code == DiagnosticCodes.InvalidCharacter));
    }

    [Theory]
    [InlineData("DIM aя AS INTEGER\naя = 67\nPRINT aя")]
    [InlineData("CONST число AS INTEGER = 67\nPRINT число")]
    [InlineData("SUB Run(значение AS INTEGER)\nEND SUB")]
    [InlineData("TYPE Данные\nvalue AS INTEGER\nEND TYPE")]
    [InlineData("DIM a AS INTEGER\nPRINT $\"Значение: {aя}\"")]
    public void Compile_RejectsRussianIdentifiers(string source)
    {
        var result = new AxiomExecutionPipeline().Compile(new CompilationRequest(source));
        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledProgram);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InvalidCharacter);
    }

    [Fact]
    public void Compile_AcceptsAsciiIdentifiersAndRussianMessages()
    {
        var source = "DIM _value1 AS INTEGER = 67\nCONST Limit2 AS INTEGER = 100\nPRINT \"Русский текст\", _value1\nINPUT \"Введите число\", _value1\n' Комментарий\nREM Ещё комментарий";
        Assert.True(new AxiomExecutionPipeline().Compile(new CompilationRequest(source)).Succeeded);
    }
}
