namespace Axiom.Execution.Tests;

public sealed class PathNullMutationEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var operation in new[] { "WRITEFILE", "APPENDFILE", "DELETEFILE" })
            yield return [operation];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidSuffixCannotMutateTheExistingPrefixFile(string operation)
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
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
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
