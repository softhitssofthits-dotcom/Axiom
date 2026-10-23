using System.Text;
using System.Text.Json;
using Axiom.Cli;

namespace Axiom.Cli.Tests;

public sealed class ArraySyntaxMigrationCliTests
{
    [Fact]
    public void MigrateArrays_DefaultPrintsPreviewAndDoesNotWrite()
    {
        var path = Path.Combine(".artifacts", "tests", "migrate-arrays", Guid.NewGuid().ToString("N"), "main.axm");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "DIM values AS INTEGER()\nPRINT values(1)\n", Encoding.UTF8);
        var originalBytes = File.ReadAllBytes(path);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var exitCode = CliApp.Run(["migrate-arrays", path], stdout, stderr, new StringReader(string.Empty));

            Assert.Equal(0, exitCode);
            Assert.Equal("DIM values[] AS INTEGER\nPRINT values[1]\n", stdout.ToString().Replace("\r\n", "\n"));
            Assert.Equal(string.Empty, stderr.ToString());
            Assert.Equal("DIM values AS INTEGER()\nPRINT values(1)\n", File.ReadAllText(path));
            Assert.Equal(originalBytes, File.ReadAllBytes(path));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void MigrateArrays_JsonReturnsEditsSourceIssuesAndCanApply()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = CliApp.Run(["migrate-arrays", "--stdin", "--json"], stdout, stderr,
            new StringReader("DIM values AS INTEGER()\nPRINT values(1)\n"));

        Assert.Equal(0, exitCode);
        using var json = JsonDocument.Parse(stdout.ToString());
        Assert.True(json.RootElement.GetProperty("canApply").GetBoolean());
        Assert.Contains("values[]", json.RootElement.GetProperty("source").GetString());
        Assert.True(json.RootElement.GetProperty("edits").GetArrayLength() > 0);
        Assert.Equal(0, json.RootElement.GetProperty("issues").GetArrayLength());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public void MigrateArrays_InvalidSourceHasNoEditsAndReturnsOne()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = CliApp.Run(["migrate-arrays", "--stdin", "--json"], stdout, stderr,
            new StringReader("DIM values AS INTEGER()\nIF\n"));

        Assert.Equal(1, exitCode);
        using var json = JsonDocument.Parse(stdout.ToString());
        Assert.False(json.RootElement.GetProperty("canApply").GetBoolean());
        Assert.Equal(0, json.RootElement.GetProperty("edits").GetArrayLength());
        Assert.True(json.RootElement.GetProperty("issues").GetArrayLength() > 0);
    }

    [Fact]
    public void MigrateArrays_ResolvesImportsWithoutWritingDependencies()
    {
        var directory = Path.GetFullPath(Path.Combine(".artifacts", "tests", "migrate-arrays", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        var main = Path.Combine(directory, "main.axm");
        var helper = Path.Combine(directory, "helper.axm");
        File.WriteAllText(main, "IMPORT helper\nPRINT LBOUND(values)");
        File.WriteAllText(helper, "PUBLIC DIM values() AS INTEGER");
        var originalMain = File.ReadAllBytes(main);
        var originalHelper = File.ReadAllBytes(helper);
        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var code = CliApp.Run(["migrate-arrays", main], stdout, stderr, new StringReader(string.Empty));
            Assert.Equal(0, code);
            Assert.Equal("IMPORT helper\nPRINT LBOUND(values[])", stdout.ToString());
            Assert.Equal(originalMain, File.ReadAllBytes(main));
            Assert.Equal(originalHelper, File.ReadAllBytes(helper));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void MigrateArrays_DoesNotExecuteProgram()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var code = CliApp.Run(["migrate-arrays", "--stdin"], stdout, stderr,
            new StringReader("ERROR \"Не выполнять программу\"\nDIM values() AS INTEGER"));
        Assert.Equal(0, code);
        Assert.Equal("ERROR \"Не выполнять программу\"\nDIM values[] AS INTEGER", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--write")]
    [InlineData("--json", "--json")]
    public void MigrateArrays_RejectsUnsupportedOrDuplicateFlags(params string[] flags)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var arguments = new[] { "migrate-arrays", "--stdin" }.Concat(flags).ToArray();
        Assert.Equal(2, CliApp.Run(arguments, stdout, stderr, new StringReader("PRINT 1")));
        Assert.Equal(string.Empty, stdout.ToString());
    }

    [Fact]
    public void MigrateArrays_WriteOptionIsRejected()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exitCode = CliApp.Run(["migrate-arrays", "--stdin", "--write"], stdout, stderr,
            new StringReader("PRINT 1\n"));

        Assert.Equal(2, exitCode);
        Assert.Contains("usage:", stderr.ToString());
        Assert.Equal(string.Empty, stdout.ToString());
    }
}
