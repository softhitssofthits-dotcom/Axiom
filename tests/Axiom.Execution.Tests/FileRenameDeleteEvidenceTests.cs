namespace Axiom.Execution.Tests;

public sealed class FileRenameDeleteEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var directory in new[] { false, true })
        foreach (var scenario in new[] { "success", "conflict", "path-name" }) yield return [directory, scenario];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RenamePreservesBytesAndRejectsConflictOrPathName(bool directory, string scenario)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-rename-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "источник😀");
        var target = Path.Combine(root, "назначение😀");
        byte[] bytes = [0, 255, 128, 13, 10];
        try
        {
            if (directory) { Directory.CreateDirectory(source); File.WriteAllBytes(Path.Combine(source, "data.bin"), bytes); }
            else File.WriteAllBytes(source, bytes);
            if (scenario == "conflict")
            {
                if (directory) { Directory.CreateDirectory(target); File.WriteAllBytes(Path.Combine(target, "data.bin"), [42]); }
                else File.WriteAllBytes(target, [42]);
            }
            var operation = directory ? "RENAMEDIR" : "RENAMEFILE";
            var newName = scenario == "path-name" ? "sub/назначение😀" : "назначение😀";
            var result = Run($$"""
                TRY
                    PRINT {{operation}}("{{source}}", "{{newName}}")
                CATCH err
                    PRINT err.Code
                END TRY
                """);
            Assert.Equal(new[] { scenario == "success" ? "TRUE" : "MB3007" }, result.Output);
            var retained = scenario == "success" ? target : source;
            Assert.Equal(bytes, File.ReadAllBytes(directory ? Path.Combine(retained, "data.bin") : retained));
            if (scenario == "success") Assert.False(Path.Exists(source));
            if (scenario == "conflict") Assert.Equal(new byte[] { 42 }, File.ReadAllBytes(directory ? Path.Combine(target, "data.bin") : target));
            Assert.False(Directory.Exists(Path.Combine(root, "sub")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void NonemptyDirectoryFailurePreservesBytesAndExplicitDeletionWorks()
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-delete-" + Guid.NewGuid().ToString("N"));
        var child = Path.Combine(root, "child");
        Directory.CreateDirectory(child);
        var file = Path.Combine(child, "данные😀.bin");
        byte[] bytes = [0, 255, 128];
        File.WriteAllBytes(file, bytes);
        try
        {
            var refusal = Run($$"""
                TRY
                    PRINT DELETEDIR("{{child}}")
                CATCH err
                    PRINT err.Code
                END TRY
                """);
            Assert.Equal(new[] { "MB3007" }, refusal.Output);
            Assert.Equal(bytes, File.ReadAllBytes(file));
            var result = Run($$"""
                PRINT FILEEXISTS("{{file}}")
                PRINT DIREXISTS("{{file}}")
                PRINT PATHEXISTS("{{file}}")
                PRINT FILEEXISTS("{{child}}")
                PRINT DIREXISTS("{{child}}")
                PRINT PATHEXISTS("{{child}}")
                PRINT DELETEFILE("{{file}}")
                PRINT DELETEFILE("{{file}}")
                PRINT DELETEDIR("{{child}}")
                PRINT DELETEDIR("{{child}}")
                PRINT MAKEDIR("{{child}}/nested")
                PRINT DELETEDIRRECURSIVE("{{child}}")
                PRINT DELETEDIRRECURSIVE("{{child}}")
                """);
            Assert.Equal(new[] { "TRUE", "FALSE", "TRUE", "FALSE", "TRUE", "TRUE", "TRUE", "FALSE", "TRUE", "FALSE", "TRUE", "TRUE", "FALSE" }, result.Output);
            Assert.False(Directory.Exists(child));
        }
        finally { Directory.Delete(root, true); }
    }

    private static ExecutionResult Run(string source)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        return result;
    }
}
