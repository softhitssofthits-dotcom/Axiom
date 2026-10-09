namespace Axiom.Execution.Tests;

public sealed class PathComponentEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        var cases = new (string Path, string Name, string Directory, string Extension, string Root)[]
        {
            (@"C:\base\данные😀.tar.gz", "данные😀.tar.gz", @"C:\base", ".gz", @"C:\"),
            (@"C:\", "", "", "", @"C:\"),
            (@"C:\base\", "", @"C:\base", "", @"C:\"),
            (@"C:\base\..\other\plain", "plain", @"C:\other", "", @"C:\"),
            (@"\\server\share\dir\file.txt", "file.txt", @"\\server\share\dir", ".txt", @"\\server\share"),
            (@"C:\base\.hidden", ".hidden", @"C:\base", ".hidden", @"C:\")
        };
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var (path, name, directory, extension, root) in cases) yield return [mode, path, name, directory, extension, root];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ComponentsResolveLexicallyWithoutExistingResource(ExecutionMode mode, string path, string name, string directory, string extension, string root)
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
            PRINT FILENAME("{{path}}")
            PRINT DIRNAME("{{path}}")
            PRINT EXTNAME("{{path}}")
            PRINT PATHROOT("{{path}}")
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { name, directory, extension, root }, result.Output);
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void FullyQualifiedDiffersFromDriveRelativeAndRootRelative(ExecutionMode mode)
    {
        if (!OperatingSystem.IsWindows()) return;
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            PRINT ISABSOLUTEPATH("C:\base\file.txt")
            PRINT ISABSOLUTEPATH("\\server\share\file.txt")
            PRINT ISABSOLUTEPATH("C:file.txt")
            PRINT ISABSOLUTEPATH("\file.txt")
            PRINT ISABSOLUTEPATH("file.txt")
            PRINT ISABSOLUTEPATH("")
            """, mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        Assert.Equal(new[] { "TRUE", "TRUE", "FALSE", "FALSE", "FALSE", "FALSE" }, result.Output);
    }
}
