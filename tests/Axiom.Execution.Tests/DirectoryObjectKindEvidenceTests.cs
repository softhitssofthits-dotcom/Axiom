namespace Axiom.Execution.Tests;

public sealed class DirectoryObjectKindEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void ExistenceAndCreationDistinguishUnicodeFileAndDirectory(ExecutionMode mode)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-kind-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "é😀");
        var directory = Path.Combine(root, "é😀");
        var nested = Path.Combine(directory, "вложенный");
        byte[] bytes = [0, 255, 13, 10];
        File.WriteAllBytes(file, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT DIREXISTS("{{file}}")
                PRINT PATHEXISTS("{{file}}")
                PRINT DIREXISTS("{{directory}}")
                PRINT PATHEXISTS("{{directory}}")
                TRY
                    PRINT MAKEDIR("{{file}}")
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT MAKEDIR("{{nested}}")
                PRINT DIREXISTS("{{directory}}")
                PRINT PATHEXISTS("{{directory}}")
                PRINT DIREXISTS("{{nested}}")
                PRINT MAKEDIR("{{nested}}")
                PRINT DIREXISTS("{{file}}")
                PRINT PATHEXISTS("{{file}}")
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(["FALSE", "TRUE", "FALSE", "FALSE", "MB3007", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "FALSE", "TRUE"], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(file));
            Assert.Equal(new[] { nested }, Directory.GetDirectories(directory));
            Assert.Empty(Directory.GetFileSystemEntries(nested));
        }
        finally
        {
            File.Delete(file);
            if (Directory.Exists(nested)) Directory.Delete(nested);
            if (Directory.Exists(directory)) Directory.Delete(directory);
            Directory.Delete(root);
        }
    }
}
