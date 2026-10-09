namespace Axiom.Execution.Tests;

public sealed class FileTransferLockedHandleEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "COPYFILE", "MOVEFILE" })
        foreach (var scenario in new[] { "source-locked", "target-locked", "source-missing" })
            yield return [mode, operation, scenario];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RealTransferFailurePreservesBothObjects(ExecutionMode mode, string operation, string scenario)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "transfer-locked-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "источник😀.bin");
        var target = Path.Combine(root, "приёмникé.bin");
        byte[] original = [0, 255, 128, 13, 10];
        byte[] previous = [254, 0, 42];
        if (scenario != "source-missing") File.WriteAllBytes(source, original);
        if (scenario != "source-locked") File.WriteAllBytes(target, previous);
        try
        {
            using (var locked = scenario == "source-missing" ? null
                : new FileStream(scenario == "source-locked" ? source : target, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                    TRY
                        PRINT {{operation}}("{{source}}", "{{target}}", TRUE)
                    CATCH err
                        PRINT err.Code
                    FINALLY
                        PRINT "Завершено"
                    END TRY
                    PRINT "Продолжение"
                    """, mode: mode));
                Assert.Equal(ExecutionStatus.Success, result.Status);
                Assert.Empty(result.Diagnostics);
                Assert.False(result.UsedInterpreterFallback);
                Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
                Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, result.Output);
            }
            if (scenario == "source-missing") Assert.False(File.Exists(source));
            else Assert.Equal(original, File.ReadAllBytes(source));
            if (scenario == "source-locked") Assert.False(File.Exists(target));
            else Assert.Equal(previous, File.ReadAllBytes(target));
            if (File.Exists(source)) { using var released = new FileStream(source, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
            if (File.Exists(target)) { using var released = new FileStream(target, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
        }
        finally { File.Delete(source); File.Delete(target); Directory.Delete(root); }
    }
}
