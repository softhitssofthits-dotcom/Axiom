namespace Axiom.Execution.Tests;

public sealed class ArrayShapeUnicodeEvidenceTests
{
    [Fact]
    public void VisibleUnicodeOrderAndIndependentTemplateShapeArePreserved()
    {
        var source = "DIM grid[-1 TO 1, 5 TO 7] AS STRING\r\nDIM flat[] AS STRING\r\nDIM shaped[,] AS STRING\r\nDIM pattern[8 TO 11, -2 TO -1] AS STRING\r\nDIM token AS STRING = LEFT(\"😀\", 1) + CHRW(0)\r\ngrid[-1, 5] = \"Внешний\"\r\ngrid[0, 6] = token\r\ngrid[0, 7] = \"é\"\r\ngrid[1, 6] = \"😀\"\r\ngrid[1, 7] = \"é\"\r\npattern[9, -2] = \"Шаблон\"\r\nflat[] = ARRAYFLATTEN(grid[0 TO 1, 6 TO 7])\r\nPRINT LBOUND(flat[])\r\nPRINT UBOUND(flat[])\r\nPRINT flat[1] = token\r\nPRINT flat[2] = \"é\"\r\nPRINT flat[3] = \"😀\"\r\nPRINT flat[4] = \"é\"\r\nshaped[] = ARRAYRESHAPE(flat[], pattern[9 TO 10, -2 TO -1])\r\nPRINT LBOUND(shaped[], 1)\r\nPRINT UBOUND(shaped[], 1)\r\nPRINT LBOUND(shaped[], 2)\r\nPRINT UBOUND(shaped[], 2)\r\nPRINT shaped[9, -2] = token\r\nPRINT shaped[9, -1] = \"é\"\r\nPRINT shaped[10, -2] = \"😀\"\r\nPRINT shaped[10, -1] = \"é\"\r\nflat[1] = \"Замена\"\r\nPRINT grid[0, 6] = token\r\nPRINT shaped[9, -2] = token\r\nshaped[9, -2] = \"Другая замена\"\r\nPRINT pattern[9, -2] = \"Шаблон\"\r\nTRY\r\n    shaped[] = ARRAYRESHAPE(flat[], pattern[])\r\nCATCH err\r\n    PRINT err.Code\r\nEND TRY\r\nPRINT shaped[9, -2] = \"Другая замена\"\r\nPRINT grid[-1, 5] = \"Внешний\"";
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(new[] { "1", "4", "TRUE", "TRUE", "TRUE", "TRUE", "9", "10", "-2", "-1", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "MB3007", "TRUE", "TRUE" }, run.Output);
    }
}
