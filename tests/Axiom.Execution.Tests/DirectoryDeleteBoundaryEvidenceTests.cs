namespace Axiom.Execution.Tests;

public sealed class DirectoryDeleteBoundaryEvidenceTests
{
    [Fact]
    public void DeletionStaysInsideOwnedTreeAndDistinguishesKinds()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-delete-tests", Guid.NewGuid().ToString("N")));
        var target = Path.GetFullPath(Path.Combine(root, "é😀"));
        Assert.StartsWith(root + Path.DirectorySeparatorChar, target);
        var child = Path.Combine(target, "вложенный");
        var neighbor = Path.Combine(root, "é😀");
        var payload = Path.Combine(child, "данные.bin");
        var preserved = Path.Combine(neighbor, "данные.bin");
        Directory.CreateDirectory(child);
        Directory.CreateDirectory(neighbor);
        byte[] bytes = [0, 255, 13, 10];
        File.WriteAllBytes(payload, bytes);
        File.WriteAllBytes(preserved, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                PRINT DELETEDIR("{{payload}}")
                PRINT DELETEDIRRECURSIVE("{{payload}}")
                TRY
                    PRINT DELETEDIR("{{target}}")
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT FILEEXISTS("{{payload}}")
                END TRY
                PRINT DELETEDIRRECURSIVE("{{target}}")
                PRINT PATHEXISTS("{{target}}")
                PRINT DELETEDIRRECURSIVE("{{target}}")
                PRINT DELETEDIR("{{target}}")
                PRINT FILEEXISTS("{{preserved}}")
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["FALSE", "FALSE", "MB3007", "TRUE", "TRUE", "FALSE", "FALSE", "FALSE", "TRUE"], result.Output);
            Assert.False(Directory.Exists(target));
            Assert.Equal(bytes, File.ReadAllBytes(preserved));
        }
        finally
        {
            if (File.Exists(payload)) File.Delete(payload);
            if (Directory.Exists(child)) Directory.Delete(child);
            if (Directory.Exists(target)) Directory.Delete(target);
            File.Delete(preserved);
            Directory.Delete(neighbor);
            Directory.Delete(root);
        }
    }
}
