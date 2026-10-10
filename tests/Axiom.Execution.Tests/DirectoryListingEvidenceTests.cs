namespace Axiom.Execution.Tests;

public sealed class DirectoryListingEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var operation in new[] { "GETFILES", "GETDIRS" })
        foreach (var filtered in new[] { false, true }) yield return [operation, filtered];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ListingIsSortedShallowAndPreservesUnicodeNames(string operation, bool filtered)
    {
        var root = Path.Combine(Path.GetTempPath(), "axiom-list-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            foreach (var name in new[] { "A", "B", "Я😀" }) Directory.CreateDirectory(Path.Combine(root, name));
            foreach (var name in new[] { "A.txt", "B.bin", "Я😀.txt" }) File.WriteAllText(Path.Combine(root, name), "Сохранить😀");
            File.WriteAllText(Path.Combine(root, "A", "nested.txt"), "Вложенный");
            var names = operation == "GETFILES"
                ? (filtered ? new[] { "A.txt", "Я😀.txt" } : new[] { "A.txt", "B.bin", "Я😀.txt" })
                : (filtered ? new[] { "A" } : new[] { "A", "B", "Я😀" });
            var pattern = operation == "GETFILES" ? "*.txt" : "A*";
            var arguments = filtered ? $"\"{root}\", \"{pattern}\"" : $"\"{root}\"";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM entries() AS STRING
                entries = {{operation}}({{arguments}})
                PRINT ARRAYLENGTH(entries)
                FOR index = 1 TO ARRAYLENGTH(entries)
                    PRINT entries(index)
                NEXT index
                entries = {{operation}}("{{root}}", "*.missing")
                PRINT ARRAYLENGTH(entries)
                TRY
                    entries = {{operation}}("{{root}}", "")
                CATCH err
                    PRINT err.Code
                END TRY
                TRY
                    entries = {{operation}}("{{root}}/missing")
                CATCH err
                    PRINT err.Code
                END TRY
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(new[] { names.Length.ToString() }.Concat(names.Select(name => Path.Combine(root, name))).Concat(new[] { "0", "MB3007", "MB3007" }), result.Output);
            Assert.Equal("Вложенный", File.ReadAllText(Path.Combine(root, "A", "nested.txt")));
            foreach (var name in new[] { "A.txt", "B.bin", "Я😀.txt" }) Assert.Equal("Сохранить😀", File.ReadAllText(Path.Combine(root, name)));
        }
        finally { Directory.Delete(root, true); }
    }
}
