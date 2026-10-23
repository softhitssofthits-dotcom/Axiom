namespace Axiom.Execution.Tests;

public sealed class SplitJoinMalformedUnicodeEvidenceTests
{
    [Fact]
    public void SurrogateHalfSeparatorHasLiteralPartsAndJoinOutput()
    {
        Check("DIM high AS STRING = LEFT(\"😀\", 1)\r\nDIM low AS STRING = RIGHT(\"😀\", 1)\r\nDIM parts[] AS STRING\r\nparts[] = SPLIT(\"😀А😀\", high)\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1] = \"\"\r\nPRINT parts[2] = low + \"А\"\r\nPRINT parts[3] = low\r\nPRINT JOIN(parts[], \"|\") = \"|\" + low + \"А|\" + low\r\nparts[] = SPLIT(\"😀А😀\", low)\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1] = high\r\nPRINT parts[2] = \"А\" + high\r\nPRINT parts[3] = \"\"\r\nPRINT JOIN(parts[]) = high + \",А\" + high + \",\"", ["3", "TRUE", "TRUE", "TRUE", "TRUE", "3", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void NullSeparatorKeepsEachEmptyElement()
    {
        Check("DIM zero AS STRING = CHRW(0)\r\nDIM parts[] AS STRING\r\nparts[] = SPLIT(zero + \"А\" + zero + zero + \"Б\" + zero, zero)\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1] = \"\"\r\nPRINT parts[2] = \"А\"\r\nPRINT parts[3] = \"\"\r\nPRINT parts[4] = \"Б\"\r\nPRINT parts[5] = \"\"\r\nPRINT JOIN(parts[], \":\") = \":А::Б:\"\r\nPRINT JOIN(parts[], zero) = zero + \"А\" + zero + zero + \"Б\" + zero", ["5", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void SeparatorIsWholeFragmentRatherThanCharacterSet()
    {
        Check("DIM high AS STRING = LEFT(\"😀\", 1)\r\nDIM low AS STRING = RIGHT(\"😀\", 1)\r\nDIM parts[] AS STRING\r\nparts[] = SPLIT(\"😀А\", high + \"А\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1] = \"😀А\"\r\nparts[] = SPLIT(high + \"А\" + low, high + \"А\")\r\nPRINT ARRAYLENGTH(parts[])\r\nPRINT parts[1] = \"\"\r\nPRINT parts[2] = low\r\nPRINT JOIN(parts[], high) = \"😀\"", ["1", "TRUE", "2", "TRUE", "TRUE", "TRUE"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
