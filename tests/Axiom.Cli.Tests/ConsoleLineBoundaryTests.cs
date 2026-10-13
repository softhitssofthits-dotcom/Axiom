using Axiom.Cli;

namespace Axiom.Cli.Tests;

public sealed class ConsoleLineBoundaryTests
{
    [Fact]
    public void RunPreservesCompletedEmptyAndJoinedPrintLines()
    {
        WithSource("PRINT \"Первая\"\nPRINT\nPRINT \"Начало\";\nPRINT \"Конец\"\nPRINT \"Хвост\";", (path, stdout, stderr) =>
        {
            var exit = CliApp.Run(["run", path], stdout, stderr, new StringReader(""));
            Assert.Equal(0, exit);
            Assert.Equal(string.Join(Environment.NewLine, "Первая", "", "НачалоКонец", "Хвост", ""), stdout.ToString());
            Assert.Equal("", stderr.ToString());
        });
    }

    [Fact]
    public void InputPromptStaysOnSameLineAsEchoWhilePrintEndsLine()
    {
        WithSource("DIM name AS STRING\nINPUT \"Имя: \"; name\nPRINT \"Ответ: \"; name\nPRINT \"Готово\"", (path, stdout, stderr) =>
        {
            var exit = CliApp.Run(["run", path], stdout, stderr, new StringReader("Анна\n"));
            Assert.Equal(0, exit);
            Assert.Equal("Имя: ? Ответ: Анна" + Environment.NewLine + "Готово" + Environment.NewLine, stdout.ToString());
            Assert.Equal("", stderr.ToString());
        });
    }

    private static void WithSource(string source, Action<string, StringWriter, StringWriter> check)
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "PROJECT.md"))) repository = repository.Parent;
        Assert.NotNull(repository);
        var directory = Path.Combine(repository.FullName, ".artifacts", "cli-lines-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "строки.axm");
        try
        {
            File.WriteAllText(path, source);
            using var stdout = new StringWriter();
            using var stderr = new StringWriter();
            check(path, stdout, stderr);
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(directory);
        }
    }
}
