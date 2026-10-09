namespace Axiom.Execution.Tests;

public sealed class DirectoryTransferKindEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "COPYDIR", "COPYDIR_OVERWRITE", "MOVEDIR" })
        foreach (var wrongSource in new[] { false, true }) yield return [mode, operation, wrongSource];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FileInsteadOfDirectoryCannotDestroyEitherPayload(ExecutionMode mode, string operation, bool wrongSource)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-transfer-kind", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "é😀");
        var directory = Path.Combine(root, "é😀");
        Directory.CreateDirectory(directory);
        var payload = Path.Combine(directory, "данные.bin");
        byte[] first = [0, 255, 13, 10];
        byte[] second = [128, 1, 2];
        File.WriteAllBytes(file, first);
        File.WriteAllBytes(payload, second);
        try
        {
            var source = wrongSource ? file : directory;
            var destination = wrongSource ? directory : file;
            var call = operation == "COPYDIR_OVERWRITE" ? $"COPYDIR(\"{source}\", \"{destination}\", TRUE)" : $"{operation}(\"{source}\", \"{destination}\")";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT FILEEXISTS(\"{file}\")\nEND TRY", mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(["MB3007", "TRUE"], result.Output);
            Assert.Equal(first, File.ReadAllBytes(file));
            Assert.Equal(second, File.ReadAllBytes(payload));
            Assert.Single(Directory.GetFileSystemEntries(directory));
            Assert.Equal(2, Directory.GetFileSystemEntries(root).Length);
        }
        finally { File.Delete(file); File.Delete(payload); Directory.Delete(directory); Directory.Delete(root); }
    }
}
