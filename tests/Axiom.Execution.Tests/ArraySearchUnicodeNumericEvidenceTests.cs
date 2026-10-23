namespace Axiom.Execution.Tests;

public sealed class ArraySearchUnicodeNumericEvidenceTests
{
    [Fact]
    public void StringsMatchWholeUtf16ValuesAndVisibleViews()
    {
        Check("DIM words[-3 TO 3] AS STRING\r\nDIM high AS STRING = LEFT(\"😀\", 1)\r\nwords[-3] = \"é\"\r\nwords[-2] = \"é\"\r\nwords[-1] = high\r\nwords[0] = \"😀\"\r\nwords[1] = \"А\" + CHRW(0)\r\nwords[2] = high\r\nwords[3] = \"\"\r\nPRINT ARRAYCOUNT(words[], high)\r\nPRINT ARRAYINDEXOF(words[], high)\r\nPRINT ARRAYLASTINDEXOF(words[], high)\r\nPRINT ARRAYCOUNT(words[], \"é\")\r\nPRINT ARRAYCOUNT(words[], \"é\")\r\nPRINT ARRAYCOUNT(words[], \"😀\")\r\nPRINT ARRAYCONTAINS(words[], \"А\")\r\nPRINT ARRAYCOUNT(words[], \"А\" + CHRW(0))\r\nPRINT ARRAYCOUNT(words[], \"\")\r\nPRINT ARRAYCONTAINS(words[-2 TO 1], high)\r\nPRINT ARRAYCOUNT(words[-2 TO 1], high)\r\nPRINT ARRAYINDEXOF(words[-2 TO 1], high)\r\nPRINT ARRAYLASTINDEXOF(words[-2 TO 1], high)\r\nPRINT words[-3] = \"é\"\r\nPRINT words[2] = high", ["2", "-1", "2", "1", "1", "1", "FALSE", "1", "1", "TRUE", "1", "-1", "-1", "TRUE", "TRUE"]);
    }

    [Fact]
    public void LongAndDecimalNeighborsRemainDistinct()
    {
        Check("DIM values[-2 TO 1] AS LONG\r\nvalues[-2] = 9007199254740992\r\nvalues[-1] = 9007199254740993\r\nvalues[0] = 9223372036854775807\r\nvalues[1] = 9007199254740993\r\nPRINT ARRAYCOUNT(values[], 9007199254740993)\r\nPRINT ARRAYINDEXOF(values[], 9007199254740993)\r\nPRINT ARRAYLASTINDEXOF(values[], 9007199254740993)\r\nPRINT ARRAYCONTAINS(values[], 9223372036854775806)\r\nDIM amounts[4 TO 6] AS DECIMAL\r\namounts[4] = 79228162514264337593543950335.0\r\namounts[5] = 79228162514264337593543950334.0\r\namounts[6] = 79228162514264337593543950335.0\r\nPRINT ARRAYCOUNT(amounts[], 79228162514264337593543950335.0)\r\nPRINT ARRAYINDEXOF(amounts[], 79228162514264337593543950334.0)\r\nPRINT ARRAYLASTINDEXOF(amounts[], 79228162514264337593543950335.0)\r\nPRINT ARRAYCONTAINS(amounts[], 79228162514264337593543950333.0)", ["2", "-1", "1", "FALSE", "2", "5", "6", "FALSE"]);
    }

    private static void Check(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Empty(result.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
        Assert.Equal(expected, result.Output);
    }
}
