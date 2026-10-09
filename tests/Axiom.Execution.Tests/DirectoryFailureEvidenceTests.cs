namespace Axiom.Execution.Tests;

public sealed class DirectoryFailureEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, "COPYDIR", false)]
    [InlineData(ExecutionMode.Bytecode, "COPYDIR", false)]
    [InlineData(ExecutionMode.Interpreter, "COPYDIR", true)]
    [InlineData(ExecutionMode.Bytecode, "COPYDIR", true)]
    [InlineData(ExecutionMode.Interpreter, "MOVEDIR", false)]
    [InlineData(ExecutionMode.Bytecode, "MOVEDIR", false)]
    [InlineData(ExecutionMode.Interpreter, "MOVEDIR", true)]
    [InlineData(ExecutionMode.Bytecode, "MOVEDIR", true)]
    public void SameOrDescendantDestinationPreservesSource(ExecutionMode mode, string operation, bool descendant)
    {
        InDirectory(root =>
        {
            var source = Path.Combine(root, "источник😀");
            Directory.CreateDirectory(source);
            var file = Path.Combine(source, "данные.txt");
            File.WriteAllText(file, "Сохранить");
            var destination = descendant ? Path.Combine(source, "новый") : source;
            var result = RunCaught($"{operation}(\"{source}\", \"{destination}\")", mode);
            Assert.Equal(["MB3007"], result.Output);
            Assert.Equal("Сохранить", File.ReadAllText(file));
            Assert.Single(Directory.EnumerateFileSystemEntries(source));
        });
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CopyFailureIsNotAtomicButPreservesSourceAndConflictingFile(ExecutionMode mode)
    {
        InDirectory(root =>
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            Directory.CreateDirectory(Path.Combine(source, "child"));
            Directory.CreateDirectory(Path.Combine(destination, "child"));
            File.WriteAllText(Path.Combine(source, "first.txt"), "Первый");
            File.WriteAllText(Path.Combine(source, "child", "conflict.txt"), "Новый");
            File.WriteAllText(Path.Combine(destination, "child", "conflict.txt"), "Старый");
            var result = RunCaught($"COPYDIR(\"{source}\", \"{destination}\")", mode);
            Assert.Equal(["MB3007"], result.Output);
            Assert.Equal("Первый", File.ReadAllText(Path.Combine(destination, "first.txt")));
            Assert.Equal("Старый", File.ReadAllText(Path.Combine(destination, "child", "conflict.txt")));
            Assert.Equal("Новый", File.ReadAllText(Path.Combine(source, "child", "conflict.txt")));
            Assert.Equal("Первый", File.ReadAllText(Path.Combine(source, "first.txt")));
        });
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void CopyRejectsNestedDestinationLinkWithoutChangingItsTarget(ExecutionMode mode)
    {
        InDirectory(root =>
        {
            var source = Path.Combine(root, "source");
            var destination = Path.Combine(root, "destination");
            var outside = Path.Combine(root, "outside");
            Directory.CreateDirectory(Path.Combine(source, "child"));
            Directory.CreateDirectory(destination);
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(source, "child", "data.txt"), "Новый");
            File.WriteAllText(Path.Combine(outside, "data.txt"), "Сохранить");
            var link = Path.Combine(destination, "child");
            DirectoryTraversalContractTests.CreateDirectoryLink(link, outside);
            try
            {
                var result = RunCaught($"COPYDIR(\"{source}\", \"{destination}\", TRUE)", mode);
                Assert.Equal(["MB3007"], result.Output);
                Assert.Equal("Сохранить", File.ReadAllText(Path.Combine(outside, "data.txt")));
                Assert.Single(Directory.EnumerateFiles(outside));
            }
            finally { Directory.Delete(link); }
        });
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void MoveFailurePreservesSourceAndExistingDestination(ExecutionMode mode, bool missingParent)
    {
        InDirectory(root =>
        {
            var source = Path.Combine(root, "source");
            var destination = missingParent ? Path.Combine(root, "missing", "destination") : Path.Combine(root, "destination");
            Directory.CreateDirectory(source);
            if (!missingParent) Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(source, "data.txt"), "Источник");
            if (!missingParent) File.WriteAllText(Path.Combine(destination, "data.txt"), "Назначение");
            var result = RunCaught($"MOVEDIR(\"{source}\", \"{destination}\")", mode);
            Assert.Equal(["MB3007"], result.Output);
            Assert.Equal("Источник", File.ReadAllText(Path.Combine(source, "data.txt")));
            if (!missingParent) Assert.Equal("Назначение", File.ReadAllText(Path.Combine(destination, "data.txt")));
            else Assert.False(Directory.Exists(Path.Combine(root, "missing")));
        });
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void MoveDirectoryAliasMovesLinkRatherThanTarget(ExecutionMode mode)
    {
        InDirectory(root =>
        {
            var target = Path.Combine(root, "target");
            var alias = Path.Combine(root, "alias");
            var moved = Path.Combine(root, "moved");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "data.txt"), "Сохранить");
            DirectoryTraversalContractTests.CreateDirectoryLink(alias, target);
            try
            {
                var result = RunCaught($"MOVEDIR(\"{alias}\", \"{moved}\")", mode);
                Assert.Equal(["TRUE"], result.Output);
                Assert.False(Directory.Exists(alias));
                Assert.True((File.GetAttributes(moved) & FileAttributes.ReparsePoint) != 0);
                Assert.Equal("Сохранить", File.ReadAllText(Path.Combine(target, "data.txt")));
                Assert.Equal("Сохранить", File.ReadAllText(Path.Combine(moved, "data.txt")));
            }
            finally
            {
                if (Directory.Exists(alias)) Directory.Delete(alias);
                if (Directory.Exists(moved)) Directory.Delete(moved);
            }
        });
    }

    private static ExecutionResult RunCaught(string call, ExecutionMode mode)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nEND TRY", mode: mode));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.False(result.UsedInterpreterFallback);
        return result;
    }

    private static void InDirectory(Action<string> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-directory-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { action(root); }
        finally { Directory.Delete(root, true); }
    }
}
