namespace Axiom.Execution.Tests;

public sealed class FileMetadataKindEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var function in new[] { "FILESIZE", "FILEDATE", "FILECREATEDATE", "FILEACCESSEDDATE", "DIRDATE" })
            yield return [mode, function];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void WrongExistingResourceKindIsCatchableWithoutMutation(ExecutionMode mode, string function)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "metadata-kind-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "данные😀.bin");
        byte[] bytes = [0, 255, 128, 13, 10];
        File.WriteAllBytes(file, bytes);
        try
        {
            var argument = function == "DIRDATE" ? file : root;
            var source = $$"""
                TRY
                    PRINT {{function}}("{{argument}}")
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT "Завершено"
                END TRY
                PRINT "Продолжение"
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(file));
            Assert.Equal(new[] { file }, Directory.GetFiles(root));
        }
        finally { File.Delete(file); Directory.Delete(root); }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void DirectoryAttributesAreSupportedAndNotConfusedWithFileSize(ExecutionMode mode)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "metadata-attribute-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var attributes = File.GetAttributes(root);
        try
        {
            File.SetAttributes(root, attributes | FileAttributes.Hidden | FileAttributes.ReadOnly);
            var source = $"PRINT ISHIDDEN(\"{root}\")\nPRINT ISREADONLY(\"{root}\")";
            var first = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, first.Status);
            Assert.Empty(first.Diagnostics);
            Assert.False(first.UsedInterpreterFallback);
            Assert.Equal(new[] { "TRUE", "TRUE" }, first.Output);
            File.SetAttributes(root, attributes & ~FileAttributes.Hidden & ~FileAttributes.ReadOnly);
            var second = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode));
            Assert.Equal(ExecutionStatus.Success, second.Status);
            Assert.Empty(second.Diagnostics);
            Assert.False(second.UsedInterpreterFallback);
            Assert.Equal(new[] { "FALSE", "FALSE" }, second.Output);
            Assert.Empty(Directory.GetFileSystemEntries(root));
        }
        finally { File.SetAttributes(root, attributes); Directory.Delete(root); }
    }
}
