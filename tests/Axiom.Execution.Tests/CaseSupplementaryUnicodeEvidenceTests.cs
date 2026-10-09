namespace Axiom.Execution.Tests;

public sealed class CaseSupplementaryUnicodeEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CaseConversionPreservesMalformedUnitsAndConvertsSupplementaryLetters(ExecutionMode mode)
    {
        // Ожидания регистра взяты из UnicodeData: U+10400/U+10428 и U+03C2/U+03A3.
        var source = """
            DIM high AS STRING = LEFT("😀", 1)
            DIM low AS STRING = RIGHT("😀", 1)
            DIM capital AS STRING = CHRW(66560)
            DIM small AS STRING = CHRW(66600)
            PRINT LCASE(capital) = small
            PRINT UCASE(small) = capital
            PRINT LEN(LCASE(capital))
            PRINT LEN(UCASE(small))
            PRINT LCASE(high + "А" + CHRW(0) + low + capital) = high + "а" + CHRW(0) + low + small
            PRINT UCASE(low + "я" + CHRW(0) + high + small) = low + "Я" + CHRW(0) + high + capital
            PRINT LCASE("É") = "é"
            PRINT UCASE("é") = "É"
            PRINT UCASE("ς") = "Σ"
            PRINT LCASE("Σ") = "σ"
            PRINT LCASE("😀" + CHRW(1114111)) = "😀" + CHRW(1114111)
            PRINT UCASE("😀" + CHRW(1114111)) = "😀" + CHRW(1114111)
            """;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
        Assert.Equal(["TRUE", "TRUE", "2", "2", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"], result.Output);
    }
}
