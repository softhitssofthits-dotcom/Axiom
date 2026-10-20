using System.Text;

namespace Axiom.Cli.Tests;

/// <summary>
/// Отказ файловой системы на зависимости не должен выходить из CLI исключением:
/// check, check --json и run обязаны вернуть контролируемое сообщение и код.
/// Неизвестный IMPORT при этом остаётся языковой диагностикой, а не ошибкой файла.
/// </summary>
public sealed class CliDependencyFailureTests
{
    [Theory]
    [InlineData("check")]
    [InlineData("check-json")]
    [InlineData("run")]
    public void LockedDependencyIsReportedWithoutThrowing(string command)
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "cli-locked-dependency", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = Path.Combine(directory, "main.axm");
            var helper = Path.Combine(directory, "helper.axm");
            File.WriteAllText(entry, "IMPORT helper\nPRINT Answer()");
            File.WriteAllText(helper, "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION");

            using var locked = new FileStream(helper, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var args = command == "check-json" ? new[] { "check", entry, "--json" } : new[] { command, entry };
            var exitCode = CliApp.Run(args, stdout, stderr, new StringReader(string.Empty));

            Assert.Equal(2, exitCode);
            Assert.Contains("axiom:", stderr.ToString());
            Assert.Contains("helper.axm", stderr.ToString());

            if (command == "check-json")
            {
                // Контракт машинного ответа сохраняется даже при внешнем отказе.
                var json = stdout.ToString();
                Assert.StartsWith("{\"ok\":false", json);
                Assert.Contains("\"diagnostics\":[", json);
                using var parsed = System.Text.Json.JsonDocument.Parse(json);
                var diagnostic = Assert.Single(parsed.RootElement.GetProperty("diagnostics").EnumerateArray());
                Assert.Equal("MB4004", diagnostic.GetProperty("code").GetString());
                Assert.Equal(1, diagnostic.GetProperty("line").GetInt32());
                Assert.Equal(1, diagnostic.GetProperty("col").GetInt32());
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingImportedFileStaysALanguageDiagnostic()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "cli-missing-import", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = Path.Combine(directory, "main.axm");
            File.WriteAllText(entry, "IMPORT missing\nPRINT 1");

            var (exitCode, _, stderr) = Run(["check", entry]);

            Assert.Equal(1, exitCode);
            Assert.Contains("MB2049", stderr);
            Assert.DoesNotContain("axiom:", stderr);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LockedEntryFileIsStillReportedAsUnreadableSource()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "cli-locked-entry", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = Path.Combine(directory, "main.axm");
            File.WriteAllText(entry, "PRINT 1");

            using var locked = new FileStream(entry, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var (exitCode, _, stderr) = Run(["check", entry]);

            Assert.Equal(2, exitCode);
            Assert.Contains("не удалось прочитать", stderr);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = CliApp.Run(args, stdout, stderr, new StringReader(string.Empty));
        return (exitCode, stdout.ToString(), stderr.ToString());
    }
}
