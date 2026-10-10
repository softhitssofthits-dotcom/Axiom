namespace Axiom.Execution.Tests;

public sealed class FileModeStateEvidenceTests
{
    [Theory]
    [InlineData("INPUT", "FLUSH(f)")]
    [InlineData("OUTPUT", "EOF(f)")]
    [InlineData("OUTPUT", "LINENUMBER(f)")]
    [InlineData("APPEND", "EOF(f)")]
    [InlineData("APPEND", "LINENUMBER(f)")]
    [InlineData("APPEND", "REWIND(f)")]
    [InlineData("OUTPUT", "READTOEND(f)")]
    [InlineData("APPEND", "READTOEND(f)")]
    [InlineData("OUTPUT", "READLINE(f)")]
    [InlineData("APPEND", "READLINE(f)")]
    [InlineData("OUTPUT", "SKIPLINE(f)")]
    [InlineData("APPEND", "SKIPLINE(f)")]
    [InlineData("INPUT", "WRITE(f, \"😀\")")]
    [InlineData("INPUT", "WRITELINE(f, \"😀\")")]
    [InlineData("INPUT", "NEWLINE(f)")]
    public void WrongModeKeepsOpenStateAndPendingBytes(string fileMode, string call)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "file-mode-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "А😀.txt");
        File.WriteAllBytes(path, [0xD0, 0x90]);
        try
        {
            var write = fileMode == "INPUT" ? "" : "PRINT WRITE(f, \"😀\")\n";
            var position = fileMode == "INPUT" ? "0" : fileMode == "OUTPUT" ? "4" : "6";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"DIM f AS FILE\nOPEN \"{path}\" FOR {fileMode} AS f USING \"UTF-8\"\n" + write + $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT ISOPEN(f)\nEND TRY\nPRINT FILEPOS(f)\nCLOSE f\nPRINT ISOPEN(f)"));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal((write.Length == 0 ? Array.Empty<string>() : new[] { "TRUE" }).Concat(["MB3007", "TRUE", position, "FALSE"]), result.Output);
            byte[] expected = fileMode switch { "INPUT" => [0xD0, 0x90], "OUTPUT" => [0xF0, 0x9F, 0x98, 0x80], _ => [0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80] };
            Assert.Equal(expected, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
