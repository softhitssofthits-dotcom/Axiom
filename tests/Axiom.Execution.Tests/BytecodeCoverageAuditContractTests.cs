using Axiom.Semantics;

namespace Axiom.Execution.Tests;

public sealed class BytecodeCoverageAuditContractTests
{
    [Fact]
    public void GroupEntriesAndPrefixedTitlesAreRecognizedAndAllAritiesCounted()
    {
        WithFixture((root, rows) =>
        {
            rows.RemoveAll(line => line.Contains("title: \"IMAGEWIDTH\"") || line.Contains("title: \"IMAGEHEIGHT\""));
            rows.Add("{ title: \"Размеры изображения\", kind: \"Функции\", functions: [\"IMAGEWIDTH\", \"IMAGEHEIGHT\"] }");
            var position = rows.FindIndex(line => line.Contains("title: \"POINT\""));
            rows[position] = "{ title: \"Функция POINT\", kind: \"Функция\" }";
            WriteIndex(root, rows);
            var output = Path.Combine(root, "audit.txt");
            Assert.Equal(0, BytecodeCoverageAudit.Run(root, output));
            var report = File.ReadAllText(output);
            var variants = BuiltinFunctions.All.Sum(f =>
            {
                Assert.True(BuiltinFunctions.TryGetDescriptors(f.Name, out var descriptors));
                return descriptors.Count;
            });
            Assert.Contains($"Declared builtin variants: {variants}", report);
            Assert.True(variants > Enum.GetValues<BuiltinFunctionId>().Length);
            Assert.Contains("Overall result: PASS", report);
        });
    }

    [Fact]
    public void ExplicitUnknownFunctionIsReportedRatherThanSilentlyIgnored()
    {
        WithFixture((root, rows) =>
        {
            rows.Add("{ title: \"Ошибочная группа\", kind: \"Функции\", functions: [\"TYPO_BUILTIN\"] }");
            WriteIndex(root, rows);
            var output = Path.Combine(root, "audit.txt");
            Assert.Equal(1, BytecodeCoverageAudit.Run(root, output));
            Assert.Contains("- TYPO_BUILTIN", File.ReadAllText(output));
        });
    }

    [Fact]
    public void MissingThematicDirectoryRemainsAnAuditFailure()
    {
        WithFixture((root, rows) =>
        {
            Directory.Delete(Path.Combine(root, "samples", "functions", "isnumeric"));
            WriteIndex(root, rows);
            var output = Path.Combine(root, "audit.txt");
            Assert.Equal(1, BytecodeCoverageAudit.Run(root, output));
            var section = File.ReadAllText(output).Split("Missing sample directories", StringSplitOptions.None)[1];
            Assert.Contains("- ISNUMERIC", section);
        });
    }

    private static void WriteIndex(string root, List<string> rows) =>
        File.WriteAllLines(Path.Combine(root, "docs", "manual", "scripts", "search-index.js"), rows);

    private static void WithFixture(Action<string, List<string>> check)
    {
        var temporaryBase = Path.GetFullPath(Path.GetTempPath());
        var root = Path.Combine(temporaryBase, "axiom-coverage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "manual", "scripts"));
        var rows = new List<string>();
        try
        {
            foreach (var function in BuiltinFunctions.All)
            {
                Directory.CreateDirectory(Path.Combine(root, "samples", "functions", function.Name.ToLowerInvariant()));
                rows.Add($"{{ title: \"{function.Name}\", kind: \"Функция\" }}");
            }
            check(root, rows);
        }
        finally
        {
            Assert.StartsWith(temporaryBase, Path.GetFullPath(root));
            Directory.Delete(root, recursive: true);
        }
    }
}
