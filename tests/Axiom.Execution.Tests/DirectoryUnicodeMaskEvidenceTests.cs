using System.Globalization;

namespace Axiom.Execution.Tests;

public sealed class DirectoryUnicodeMaskEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var operation in new[] { "GETFILES", "GETDIRS" })
        foreach (var pattern in new[] { "*", "é*", "é*", "😀*", "null" })
            yield return [mode, operation, pattern];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ExactUnicodeMasksKeepDistinctNamesAndRejectNull(ExecutionMode mode, string operation, string pattern)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "directory-unicode-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        string[] names = ["é.txt", "é.txt", "Я.txt", "😀.txt"];
        byte[] bytes = [0, 255, 13, 10, 128];
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            foreach (var name in names)
            {
                var path = Path.Combine(root, name);
                if (operation == "GETFILES") File.WriteAllBytes(path, bytes);
                else Directory.CreateDirectory(path);
            }
            var argument = pattern == "null" ? "\"é\" + CHRW(0) + \"*\"" : $"\"{pattern}\"";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM entries() AS STRING
                TRY
                    entries = {{operation}}("{{root}}", {{argument}})
                    PRINT ARRAYLENGTH(entries)
                    FOR index = 1 TO ARRAYLENGTH(entries)
                        PRINT entries(index)
                    NEXT index
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT "Продолжение"
                """, mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            var matched = pattern switch { "*" => names, "é*" => new[] { "é.txt" }, "é*" => new[] { "é.txt" }, "😀*" => new[] { "😀.txt" }, _ => [] };
            var expected = pattern == "null" ? new[] { "MB3007", "Продолжение" }
                : new[] { matched.Length.ToString(CultureInfo.InvariantCulture) }.Concat(matched.Select(name => Path.Combine(root, name))).Append("Продолжение");
            Assert.Equal(expected, result.Output);
            foreach (var name in names)
            {
                var path = Path.Combine(root, name);
                if (operation == "GETFILES") Assert.Equal(bytes, File.ReadAllBytes(path));
                else Assert.Empty(Directory.EnumerateFileSystemEntries(path));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            foreach (var name in names)
            {
                var path = Path.Combine(root, name);
                if (operation == "GETFILES") File.Delete(path);
                else if (Directory.Exists(path)) Directory.Delete(path);
            }
            Directory.Delete(root);
        }
    }
}
