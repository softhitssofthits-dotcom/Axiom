namespace Axiom.Execution.Tests;

public sealed class DirectorySizeCurrentEvidenceTests
{
    [Fact]
    public void NestedByteSizeAndRejectedChangeKeepProgramDirectory()
    {
        var processDirectory = Environment.CurrentDirectory;
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-size-current", Guid.NewGuid().ToString("N")));
        var child = Path.Combine(root, "каталог😀");
        Directory.CreateDirectory(child);
        var first = Path.Combine(root, "é.bin");
        var second = Path.Combine(child, "é.bin");
        byte[] bytes = [0, 255, 13, 10];
        File.WriteAllBytes(first, bytes);
        File.WriteAllBytes(second, [128, 1, 2]);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT CURRENTDIR() = "{{root}}"
                PRINT DIRSIZE(".")
                PRINT CHANGEDIR("каталог😀")
                PRINT CURRENTDIR() = "{{child}}"
                PRINT DIRSIZE(".")
                TRY
                    PRINT CHANGEDIR("{{first}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT CURRENTDIR() = "{{child}}"
                TRY
                    PRINT DIRSIZE("{{first}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT CHANGEDIR("..")
                PRINT CURRENTDIR() = "{{root}}"
                PRINT DIRSIZE(".")
                """, resourceBaseDirectory: root));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["TRUE", "7", "TRUE", "TRUE", "3", "MB3007", "TRUE", "MB3007", "TRUE", "TRUE", "7"], result.Output);
            Assert.Equal(processDirectory, Environment.CurrentDirectory);
            Assert.Equal(bytes, File.ReadAllBytes(first));
            Assert.Equal(new byte[] { 128, 1, 2 }, File.ReadAllBytes(second));
        }
        finally { File.Delete(first); File.Delete(second); Directory.Delete(child); Directory.Delete(root); }
    }
}
