namespace Axiom.Execution.Tests;

public sealed class AppendBufferedSeekFailureEvidenceTests
{
    [Fact]
    public void ForbiddenSeekPreservesPendingUnicodeWrites()
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "append-seek-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "А😀.txt");
        File.WriteAllBytes(path, [0xD0, 0x90]);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR APPEND AS f USING "UTF-8"
                PRINT FILEMODE(f)
                PRINT FILEPATH(f) = "{{path}}"
                PRINT WRITE(f, "😀")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                TRY
                    PRINT SEEKFILE(f, 0)
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT FILEPOS(f)
                END TRY
                TRY
                    PRINT SEEKFILE(f, CLNG(9223372036854775807))
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                PRINT WRITE(f, CHRW(0) + "Б")
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["APPEND", "TRUE", "TRUE", "6", "6", "MB3007", "6", "MB3007", "6", "6", "TRUE", "9", "9"], result.Output);
            Assert.Equal(new byte[] { 0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80, 0, 0xD0, 0x91 }, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
