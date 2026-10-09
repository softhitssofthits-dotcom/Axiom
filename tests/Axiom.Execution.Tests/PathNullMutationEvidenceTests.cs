namespace Axiom.Execution.Tests;

public sealed class PathNullMutationEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "WRITEFILE", "APPENDFILE", "DELETEFILE" })
            yield return [mode, operation];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidSuffixCannotMutateTheExistingPrefixFile(ExecutionMode mode, string operation)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "path-null-mutation", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "данные😀.bin");
        byte[] original = [0, 255, 128, 13, 10, 42];
        File.WriteAllBytes(file, original);
        try
        {
            var arguments = operation == "DELETEFILE" ? "path" : "path, \"Новая запись\"";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM path AS STRING = "{{file}}" + CHR(0) + "другой.txt"
                TRY
                    PRINT {{operation}}({{arguments}})
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Продолжение" }, result.Output);
            Assert.Equal(original, File.ReadAllBytes(file));
            Assert.Equal(new[] { file }, Directory.GetFiles(root));
        }
        finally
        {
            File.Delete(file);
            Directory.Delete(root);
        }
    }
}
