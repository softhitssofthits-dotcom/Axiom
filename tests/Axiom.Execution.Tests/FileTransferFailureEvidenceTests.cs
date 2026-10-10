namespace Axiom.Execution.Tests;

public sealed class FileTransferFailureEvidenceTests
{
    [Theory]
    [InlineData("COPYFILE")]
    [InlineData("MOVEFILE")]
    public void CopyFollowsSourceLinkWhileMoveTransfersLink(string operation)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-transfer-link-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var link = Path.Combine(root, "source.bin");
        var destination = Path.Combine(root, "destination.bin");
        var target = Path.Combine(root, "target.bin");
        try
        {
            byte[] bytes = [0, 0xFF, 0x80, 13, 10];
            File.WriteAllBytes(target, bytes);
            File.CreateSymbolicLink(link, target);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"PRINT {operation}(\"{link}\", \"{destination}\")"));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(["TRUE"], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(target));
            Assert.Equal(bytes, File.ReadAllBytes(destination));
            if (operation == "COPYFILE")
            {
                Assert.NotNull(new FileInfo(link).LinkTarget);
                Assert.Null(new FileInfo(destination).LinkTarget);
            }
            else
            {
                Assert.False(File.Exists(link));
                Assert.NotNull(new FileInfo(destination).LinkTarget);
            }
        }
        finally { File.Delete(link); File.Delete(destination); Directory.Delete(root, true); }
    }

    public static IEnumerable<object[]> Cases()
    {

        foreach (var operation in new[] { "COPYFILE", "MOVEFILE" })
        foreach (var scenario in new[] { "existing", "missing-parent", "overwrite" })
            yield return [operation, scenario];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TransferPreservesDataOnConflictAndReplacesOnlyWhenRequested(string operation, string scenario)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-transfer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "источник😀.bin");
            var target = scenario == "missing-parent" ? Path.Combine(root, "missing", "target.bin") : Path.Combine(root, "target.bin");
            byte[] original = [0, 0xFF, 0xEF, 0xBB, 0xBF, 0x80, 13, 10];
            byte[] oldTarget = [0x31, 0x32];
            File.WriteAllBytes(source, original);
            if (scenario != "missing-parent") File.WriteAllBytes(target, oldTarget);
            var overwrite = scenario == "overwrite" ? ", TRUE" : "";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{operation}}("{{source}}", "{{target}}"{{overwrite}})
                CATCH err
                    PRINT err.Code
                END TRY
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            if (scenario == "overwrite")
            {
                Assert.Equal(["TRUE"], result.Output);
                Assert.Equal(original, File.ReadAllBytes(target));
                Assert.Equal(operation == "COPYFILE", File.Exists(source));
                if (operation == "COPYFILE") Assert.Equal(original, File.ReadAllBytes(source));
            }
            else
            {
                Assert.Equal(["MB3007"], result.Output);
                Assert.Equal(original, File.ReadAllBytes(source));
                if (scenario == "existing") Assert.Equal(oldTarget, File.ReadAllBytes(target));
                else Assert.False(Directory.Exists(Path.Combine(root, "missing")));
            }
        }
        finally { Directory.Delete(root, true); }
    }
}
