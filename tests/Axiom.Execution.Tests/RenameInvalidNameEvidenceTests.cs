namespace Axiom.Execution.Tests;

public sealed class RenameInvalidNameEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var directory in new[] { false, true })
        foreach (var name in new[] { "", " ", ".", "..", "sub/новое", "sub\\новое", "null" })
            yield return [mode, directory, name];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void InvalidNameCannotRenameSourceOrTouchNeighbour(ExecutionMode mode, bool directory, string name)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "rename-name-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "источник");
        var neighbour = Path.Combine(root, "сосед");
        byte[] original = [0, 255, 128, 13, 10];
        byte[] other = [42, 254];
        var sourceData = directory ? Path.Combine(source, "данные.bin") : source;
        var neighbourData = directory ? Path.Combine(neighbour, "данные.bin") : neighbour;
        if (directory) { Directory.CreateDirectory(source); Directory.CreateDirectory(neighbour); }
        File.WriteAllBytes(sourceData, original);
        File.WriteAllBytes(neighbourData, other);
        try
        {
            var argument = name == "null" ? "\"сосед\" + CHRW(0) + \"остаток\"" : $"\"{name}\"";
            var operation = directory ? "RENAMEDIR" : "RENAMEFILE";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                TRY
                    PRINT {{operation}}("{{source}}", {{argument}})
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT "Завершено"
                END TRY
                PRINT "Продолжение"
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal(new[] { "MB3007", "Завершено", "Продолжение" }, result.Output);
            Assert.Equal(original, File.ReadAllBytes(sourceData));
            Assert.Equal(other, File.ReadAllBytes(neighbourData));
            Assert.Equal(2, Directory.EnumerateFileSystemEntries(root).Count());
        }
        finally
        {
            File.Delete(sourceData); File.Delete(neighbourData);
            if (directory) { Directory.Delete(source); Directory.Delete(neighbour); }
            Directory.Delete(root);
        }
    }
}
