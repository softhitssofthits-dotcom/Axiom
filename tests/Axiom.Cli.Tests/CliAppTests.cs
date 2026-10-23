using System.Text;
using Axiom.Cli;

namespace Axiom.Cli.Tests;

public sealed class CliAppTests
{
    [Fact]
    public void RunAndCheckLoadImportedSiblingFiles()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "cli-modules", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            var entry = Path.Combine(directory, "main.axm");
            File.WriteAllText(entry, "IMPORT helper\nPRINT Answer()");
            File.WriteAllText(Path.Combine(directory, "helper.axm"), "PUBLIC FUNCTION Answer() AS INTEGER\nRETURN 42\nEND FUNCTION");
            var check = Run(["check", entry]);
            Assert.Equal(0, check.ExitCode);
            var run = Run(["run", entry]);
            Assert.Equal(0, run.ExitCode);
            Assert.Contains("42", run.Stdout);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static (int ExitCode, string Stdout, string Stderr) Run(
        string[] args,
        string? stdin = null,
        string? sourceFile = null,
        string? sourceContent = null)
    {
        if (sourceFile is not null && sourceContent is not null)
        {
            File.WriteAllText(sourceFile, sourceContent, Encoding.UTF8);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var stdinReader = stdin is null ? new StringReader(string.Empty) : new StringReader(stdin);
        var exitCode = CliApp.Run(args, stdout, stderr, stdinReader);
        return (exitCode, stdout.ToString(), stderr.ToString());
    }

    private static string MakeSampleFile(string name, string content)
    {
        var directory = Path.Combine(Path.GetTempPath(), "axiom-cli-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, content, Encoding.UTF8);
        return path;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(43)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void Run_RandomizeRepeatsSeededSequenceWithinAndAcrossInvocations(int seed)
    {
        var source = $$"""
RANDOMIZE {{seed}}
FOR i = 1 TO 4
    PRINT INT(RND()*1000000); "|"
NEXT i
RANDOMIZE {{seed}}
FOR i = 1 TO 4
    PRINT INT(RND()*1000000); "|"
NEXT i
""";
        var path = MakeSampleFile("seeded_sequence.axm", source);
        try
        {
            var random = new Random(seed);
            var sequence = string.Concat(Enumerable.Range(0, 4).Select(_ =>
                ((int)Math.Floor(random.NextDouble()*1000000)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + Environment.NewLine));
            foreach (var invocation in Enumerable.Range(0, 2))
            {
                var (exitCode, stdout, stderr) = Run(["run", path]);
                Assert.Equal(0, exitCode);
                Assert.Equal(string.Empty, stderr);
                Assert.Equal(sequence + sequence, stdout);
            }
        }
        finally
        {
            File.Delete(path);
            Directory.Delete(Path.GetDirectoryName(path)!);
        }
    }

    // ---- контракт потоков и кодов возврата ----

    [Fact]
    public void NoArguments_ShowsUsageOnStderr_Exits2()
    {
        var (exitCode, stdout, stderr) = Run([]);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("usage: axiom", stderr);
    }

    [Fact]
    public void UnknownCommand_ShowsUsageOnStderr_Exits2()
    {
        var (exitCode, stdout, _) = Run(["frobnicate"]);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
    }

    [Fact]
    public void Version_ShowsVersionAndRevision_Exits0()
    {
        var (exitCode, stdout, stderr) = Run(["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains($"axiom {CliApp.Version}", stderr);
        Assert.Contains("revision", stderr);
    }

    [Fact]
    public void Help_ShowsUsage_Exits0()
    {
        var (exitCode, _, stderr) = Run(["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("commands:", stderr);
    }

    [Fact]
    public void Run_MissingFile_Exits2_DiagnosticOnStderr()
    {
        var (exitCode, stdout, stderr) = Run(["run", Path.Combine(Path.GetTempPath(), "axiom-cli-tests", "no-such-file.axm")]);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("axiom:", stderr);
    }

    // ---- run ----

    [Fact]
    public void Run_PrintProgram_OutputOnlyOnStdout_Exits0()
    {
        var path = MakeSampleFile("hello.axm", "PRINT \"Привет, мир\"\n");
        var (exitCode, stdout, stderr) = Run(["run", path]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Привет, мир", stdout);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public void Run_SyntaxError_DiagnosticsOnStderrEmptyStdout_Exits1()
    {
        var path = MakeSampleFile("broken.axm", "PRINT \"незакрытая строка\n");
        var (exitCode, stdout, stderr) = Run(["run", path]);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("MB0", stderr);
    }

    [Fact]
    public void Run_InputReadsLineFromStdin_AndEchoesIt()
    {
        var path = MakeSampleFile(
            "ask.axm",
            "DIM name AS STRING\nINPUT \"Как вас зовут: \"; name\nPRINT \"Здравствуйте, \"; name\n");
        var (exitCode, stdout, _) = Run(["run", path], stdin: "Иван\n");

        Assert.Equal(0, exitCode);
        Assert.Contains("Как вас зовут: ", stdout);
        Assert.Contains("Здравствуйте, Иван", stdout);
    }

    [Fact]
    public void Run_RuntimeFailure_DiagnosticOnStderr_Exits1()
    {
        var path = MakeSampleFile("sqrtneg.axm", "PRINT SQR(-1)\n");
        var (exitCode, stdout, stderr) = Run(["run", path]);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("MB3007", stderr);
    }

    [Fact]
    public void Run_GraphicsProgram_ClearConsoleDiagnostic_Exits1()
    {
        var path = MakeSampleFile(
            "screen.axm",
            "SCREEN 1\nPRINT \"после экрана\"\n");
        var (exitCode, stdout, stderr) = Run(["run", path]);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("Графический режим не поддерживается", stderr);
    }

    [Fact]
    public void Run_SemanticError_DiagnosticWithLineAndColumn()
    {
        var path = MakeSampleFile("semantic.axm", "DIM x AS INTEGER\nx = \"текст\"\n");
        var (exitCode, _, stderr) = Run(["run", path]);

        Assert.Equal(1, exitCode);
        Assert.Contains("MB2", stderr);
        Assert.Contains("(2,", stderr);
    }

    // ---- check ----

    [Fact]
    public void Check_ValidFile_NoDiagnostics_Exits0()
    {
        var path = MakeSampleFile("ok.axm", "DIM a AS INTEGER = 2 + 2\nPRINT a\n");
        var (exitCode, stdout, stderr) = Run(["check", path]);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public void Check_BrokenFile_HumanDiagnosticOnStderr_Exits1()
    {
        var path = MakeSampleFile("bad.axm", "PRINT LEN(\n");
        var (exitCode, stdout, stderr) = Run(["check", path]);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("ошибка", stderr);
    }

    [Fact]
    public void Check_Json_OkTrueOnStdout()
    {
        var path = MakeSampleFile("ok.axm", "PRINT 1\n");
        var (exitCode, stdout, stderr) = Run(["check", path, "--json"]);

        Assert.Equal(0, exitCode);
        Assert.StartsWith("{\"ok\":true", stdout);
        Assert.Equal(string.Empty, stderr);
    }

    [Fact]
    public void Check_Json_ErrorCarriesLineColLengthCode()
    {
        var path = MakeSampleFile("bad.axm", "PRINT \"a\"\nFOR i = 1 TO\n");
        var (exitCode, stdout, _) = Run(["check", path, "--json"]);

        Assert.Equal(1, exitCode);
        Assert.StartsWith("{\"ok\":false,\"diagnostics\":[", stdout);
        Assert.Contains("\"line\":2", stdout);
        Assert.Contains("\"code\":\"MB", stdout);
        Assert.Contains("\"message\":", stdout);
        Assert.Contains("\"length\":", stdout);
    }

    [Fact]
    public void Check_Stdin_Json_DiagnosticsForBuffer()
    {
        var (exitCode, stdout, _) = Run(["check", "--stdin", "--json"], stdin: "PRINT \"x\"\nIF\n");

        Assert.Equal(1, exitCode);
        Assert.StartsWith("{\"ok\":false", stdout);
        Assert.Contains("<buffer>", stdout);
    }

    [Fact]
    public void Check_Stdin_HumanMode_WritesToStderr()
    {
        var (exitCode, stdout, stderr) = Run(["check", "--stdin"], stdin: "IF\n");

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stdout);
        Assert.Contains("<buffer>", stderr);
    }

    // ---- fmt ----

    [Fact]
    public void Fmt_File_ClassicStyleUppercasesKeywords()
    {
        var path = MakeSampleFile("style.axm", "print len(\"ab\")\n");
        var (exitCode, stdout, _) = Run(["fmt", path]);

        Assert.Equal(0, exitCode);
        Assert.Equal("PRINT LEN(\"ab\")\n", stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Fmt_Stdin_FormatsBuffer()
    {
        var (exitCode, stdout, _) = Run(["fmt", "--stdin"], stdin: "if x then print 1 else print 2\n");

        Assert.Equal(0, exitCode);
        Assert.Equal("IF x THEN PRINT 1 ELSE PRINT 2\n", stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void Fmt_MissingFile_Exits2()
    {
        var (exitCode, _, stderr) = Run(["fmt", "C:\\definitely\\missing\\file.axm"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("axiom:", stderr);
    }

    // ---- metadata ----

    [Fact]
    public void Metadata_ExportsBoundDimensionFormsAndPolymorphicPolicies()
    {
        var (_, stdout, _) = Run(["metadata"]);
        using var document = System.Text.Json.JsonDocument.Parse(stdout);
        var builtins = document.RootElement.GetProperty("builtins").EnumerateArray().ToArray();
        foreach (var name in new[] { "LBOUND", "UBOUND" })
        {
            Assert.Contains(builtins, b => b.GetProperty("name").GetString() == name
                && b.GetProperty("params").GetArrayLength() == 2);
        }
        var sum = Assert.Single(builtins, b => b.GetProperty("name").GetString() == "ARRAYSUM");
        Assert.Equal("ElementTypeOfFirstArrayArgument", sum.GetProperty("resultTypePolicy").GetString());
        Assert.Equal("T", sum.GetProperty("displayReturnType").GetString());
        Assert.Equal("T[]", sum.GetProperty("params")[0].GetProperty("displayType").GetString());
        var abs = Assert.Single(builtins, b => b.GetProperty("name").GetString() == "ABS");
        Assert.Equal("SameAsFirstArgument", abs.GetProperty("resultTypePolicy").GetString());
        Assert.Equal("T", abs.GetProperty("displayReturnType").GetString());
    }

    [Fact]
    public void Metadata_ContainsKeywordsTypesBuiltinsAndRevision()
    {
        var (exitCode, stdout, _) = Run(["metadata"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("\"keywords\":[", stdout);
        Assert.Contains("\"PRINT\"", stdout);
        Assert.Contains("\"typeNames\":[", stdout);
        Assert.Contains("\"INTEGER\"", stdout);
        Assert.Contains("\"builtins\":[", stdout);
        Assert.Contains("\"name\":\"LEN\"", stdout);
        Assert.Contains("\"returnType\":\"INTEGER\"", stdout);
        Assert.Contains("\"sourceRevision\":", stdout);
    }

    [Fact]
    public void Metadata_BuiltinFamiliesListEveryOverload()
    {
        var (_, stdout, _) = Run(["metadata"]);

        // У DATEADD три типизированные перегрузки (DATE/TIME/DATETIME) — все должны попасть в реестр.
        var count = CountOccurrences(stdout, "\"name\":\"DATEADD\"");
        Assert.True(count >= 3, $"Ожидалось минимум 3 перегрузки DATEADD, найдено {count}");
    }

    private static int CountOccurrences(string text, string fragment)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += fragment.Length;
        }

        return count;
    }
}
