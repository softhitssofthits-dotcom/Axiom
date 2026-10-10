namespace Axiom.Execution.Tests;

public sealed class FileClosedPropertiesEvidenceTests
{
    public static IEnumerable<object[]> Cases()
    {

        foreach (var closed in new[] { false, true })
        foreach (var call in new[] { "FILEPATH(f)", "FILEMODE(f)", "FILEPOS(f)", "FILELENGTH(f)", "SEEKFILE(f, 0)", "EOF(f)", "LINENUMBER(f)", "REWIND(f)", "FLUSH(f)", "READTOEND(f)", "READLINE(f)", "SKIPLINE(f)", "WRITE(f, \"😀\")", "WRITELINE(f, \"😀\")", "NEWLINE(f)" })
            yield return [closed, call];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void ClosedAndUnopenedPropertiesAreCatchableWithoutChangingBytes(bool closed, string call)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "closed-file-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "А😀.txt");
        byte[] bytes = [0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80];
        File.WriteAllBytes(path, bytes);
        try
        {
            var setup = closed ? $"OPEN \"{path}\" FOR INPUT AS f USING \"UTF-8\"\nCLOSE f\n" : "";
            var source = "DIM f AS FILE\n" + setup + $"TRY\nPRINT {call}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT ISOPEN(f)\nEND TRY\nPRINT \"Продолжение\"";
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["MB3007", "FALSE", "Продолжение"], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
