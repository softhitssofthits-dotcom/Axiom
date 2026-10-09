namespace Axiom.Execution.Tests;

public sealed class FileDestinationLinkEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "COPYFILE", "MOVEFILE" })
        foreach (var exists in new[] { false, true })
        foreach (var overwrite in new[] { false, true }) yield return [mode, operation, exists, overwrite];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DestinationLinkHasExplicitlyMeasuredOverwriteBehavior(ExecutionMode mode, string operation, bool targetExists, bool overwrite)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-destination-link-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "source.bin");
        var destination = Path.Combine(root, "link.bin");
        var target = Path.Combine(root, "target.bin");
        try
        {
            byte[] sourceBytes = [0, 0xFF, 0x80, 13, 10];
            byte[] oldBytes = [1, 2, 3];
            File.WriteAllBytes(source, sourceBytes);
            if (targetExists) File.WriteAllBytes(target, oldBytes);
            File.CreateSymbolicLink(destination, target);
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{operation}}("{{source}}", "{{destination}}", {{(overwrite ? "TRUE" : "FALSE")}})
                CATCH err
                    PRINT err.Code
                END TRY
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            if (!overwrite && !(operation == "COPYFILE" && !targetExists))
            {
                Assert.Equal(["MB3007"], result.Output);
                Assert.Equal(sourceBytes, File.ReadAllBytes(source));
                Assert.NotNull(new FileInfo(destination).LinkTarget);
                Assert.Equal(targetExists, File.Exists(target));
                if (targetExists) Assert.Equal(oldBytes, File.ReadAllBytes(target));
            }
            else
            {
                Assert.Equal(["TRUE"], result.Output);
                if (operation == "COPYFILE")
                {
                    Assert.NotNull(new FileInfo(destination).LinkTarget);
                    Assert.Equal(sourceBytes, File.ReadAllBytes(target));
                    Assert.Equal(sourceBytes, File.ReadAllBytes(source));
                }
                else
                {
                    Assert.Null(new FileInfo(destination).LinkTarget);
                    Assert.Equal(sourceBytes, File.ReadAllBytes(destination));
                    Assert.False(File.Exists(source));
                    Assert.Equal(targetExists, File.Exists(target));
                    if (targetExists) Assert.Equal(oldBytes, File.ReadAllBytes(target));
                }
            }
        }
        finally { File.Delete(destination); Directory.Delete(root, true); }
    }
}
