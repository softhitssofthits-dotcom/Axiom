namespace Axiom.Execution.Tests;
public sealed class RegexContextContractTests
{
    [Fact]
    public void RegexWorksInControlFlowRoutinesRecordsArraysAndInterpolation()
    {
AssertProgram("FUNCTION FindNumber(t AS STRING) AS STRING\nRETURN REGEXMATCH(t, \"\\d+\")\nEND FUNCTION\nSUB ReplaceNumber(BYREF t AS STRING)\nt = REGEXREPLACE(t, \"\\d+\", \"x\")\nEND SUB\nTYPE Row\nvalue AS STRING\nEND TYPE\nDIM rowValue AS Row\nrowValue.value = FindNumber(\"a12\")\nDIM arrayValue[1 TO 2] AS STRING\narrayValue[1] = REGEXEXTRACT(\"A34\", \"([A-Z])(\\d+)\", 2)\nPRINT rowValue.value\nPRINT arrayValue[1]\nDIM text AS STRING = \"A12\"\nReplaceNumber(text)\nPRINT text\nFOR i = 1 TO 2\nIF REGEXISMATCH(\"a\", \"a\") THEN PRINT REGEXCOUNT(\"aa\", \"a\")\nSELECT CASE REGEXMATCH(\"A1\", \"\\d\")\nCASE \"1\"\nPRINT \"select\"\nEND SELECT\nNEXT\nDIM n AS INTEGER = 0\nWHILE REGEXISMATCH(\"a\", \"a\") AND n < 1\nPRINT \"while\"\nn = n + 1\nEND WHILE\nDO\nPRINT \"do\"\nLOOP UNTIL REGEXISMATCH(\"x\", \"x\")\nPRINT $\"count={REGEXCOUNT(\"aa\", \"a\")}\"", ["12","34","Ax","2","select","2","select","while","do","count=2"]);
    }
    private static void AssertProgram(string source, string[] expected)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.True(result.Succeeded, string.Join("\n", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(expected, result.Output);
    }
}
