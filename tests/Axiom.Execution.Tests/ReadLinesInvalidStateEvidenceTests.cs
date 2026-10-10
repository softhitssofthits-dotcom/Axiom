namespace Axiom.Execution.Tests;

public sealed class ReadLinesInvalidStateEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var scenario in new[] { "never-zero", "never-one", "closed-zero", "closed-one", "negative-open" })
            yield return [scenario];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidHandleOrCountCannotConsumeInput(string scenario)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "readlines-state-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "строки😀.txt");
        byte[] bytes = [0xD0, 0x90, 0x0A, 0xD0, 0x91];
        File.WriteAllBytes(path, bytes);
        try
        {
            var setup = scenario.StartsWith("never", StringComparison.Ordinal) ? ""
                : $"OPEN \"{path}\" FOR INPUT AS f USING \"UTF-8\"\n";
            if (scenario.StartsWith("closed", StringComparison.Ordinal)) setup += "CLOSE f\n";
            var count = scenario == "negative-open" ? -1 : scenario.EndsWith("zero", StringComparison.Ordinal) ? 0 : 1;
            var source = "DIM f AS FILE\nDIM lines() AS STRING\n" + setup + $$"""
                TRY
                    lines = READLINES(f, {{count}})
                    PRINT "Ошибочно прочитано"
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """;
            if (scenario == "negative-open") source += "\nPRINT FILEPOS(f)\nPRINT LINENUMBER(f)\nPRINT READLINE(f)\nCLOSE f";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(scenario == "negative-open" ? new[] { "MB3007", "Продолжение", "0", "1", "А" }
                : new[] { "MB3007", "Продолжение" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
