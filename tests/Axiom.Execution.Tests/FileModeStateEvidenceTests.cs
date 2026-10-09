namespace Axiom.Execution.Tests;

public sealed class FileModeStateEvidenceTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter, "INPUT", "FLUSH(f)")]
    [InlineData(ExecutionMode.Bytecode, "INPUT", "FLUSH(f)")]
    [InlineData(ExecutionMode.Interpreter, "OUTPUT", "EOF(f)")]
    [InlineData(ExecutionMode.Bytecode, "OUTPUT", "EOF(f)")]
    [InlineData(ExecutionMode.Interpreter, "OUTPUT", "LINENUMBER(f)")]
    [InlineData(ExecutionMode.Bytecode, "OUTPUT", "LINENUMBER(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "EOF(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "EOF(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "LINENUMBER(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "LINENUMBER(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "REWIND(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "REWIND(f)")]
    [InlineData(ExecutionMode.Interpreter, "OUTPUT", "READTOEND(f)")]
    [InlineData(ExecutionMode.Bytecode, "OUTPUT", "READTOEND(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "READTOEND(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "READTOEND(f)")]
    [InlineData(ExecutionMode.Interpreter, "OUTPUT", "READLINE(f)")]
    [InlineData(ExecutionMode.Bytecode, "OUTPUT", "READLINE(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "READLINE(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "READLINE(f)")]
    [InlineData(ExecutionMode.Interpreter, "OUTPUT", "SKIPLINE(f)")]
    [InlineData(ExecutionMode.Bytecode, "OUTPUT", "SKIPLINE(f)")]
    [InlineData(ExecutionMode.Interpreter, "APPEND", "SKIPLINE(f)")]
    [InlineData(ExecutionMode.Bytecode, "APPEND", "SKIPLINE(f)")]
    [InlineData(ExecutionMode.Interpreter, "INPUT", "WRITE(f, \"😀\")")]
    [InlineData(ExecutionMode.Bytecode, "INPUT", "WRITE(f, \"😀\")")]
    [InlineData(ExecutionMode.Interpreter, "INPUT", "WRITELINE(f, \"😀\")")]
    [InlineData(ExecutionMode.Bytecode, "INPUT", "WRITELINE(f, \"😀\")")]
    [InlineData(ExecutionMode.Interpreter, "INPUT", "NEWLINE(f)")]
    [InlineData(ExecutionMode.Bytecode, "INPUT", "NEWLINE(f)")]
    public void WrongModeKeepsOpenStateAndPendingBytes(ExecutionMode mode, string fileMode, string call)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "file-mode-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "А😀.txt");
        File.WriteAllBytes(path, [0xD0, 0x90]);
        try
        {
            var write = fileMode == "INPUT" ? "" : "PRINT WRITE(f, \"😀\")\n";
            var position = fileMode == "INPUT" ? "0" : fileMode == "OUTPUT" ? "4" : "6";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($"DIM f AS FILE\nOPEN \"{path}\" FOR {fileMode} AS f USING \"UTF-8\"\n" + write + $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT ISOPEN(f)\nEND TRY\nPRINT FILEPOS(f)\nCLOSE f\nPRINT ISOPEN(f)", mode: mode));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.Equal(mode == ExecutionMode.Bytecode ? ExecutionEngineKind.BytecodeVm : ExecutionEngineKind.Interpreter, result.ActualEngine);
            Assert.Equal((write.Length == 0 ? Array.Empty<string>() : new[] { "TRUE" }).Concat(["MB3007", "TRUE", position, "FALSE"]), result.Output);
            byte[] expected = fileMode switch { "INPUT" => [0xD0, 0x90], "OUTPUT" => [0xF0, 0x9F, 0x98, 0x80], _ => [0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80] };
            Assert.Equal(expected, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
