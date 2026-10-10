namespace Axiom.Execution.Tests;

public sealed class SeekLongBoundaryEvidenceTests
{
    [Theory]
    [InlineData("9223372036854775807")]
    [InlineData("-9223372036854775807 - CLNG(1)")]
    public void LongExtremaCannotChangeInputPositionOrBytes(string position)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "seek-long-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "А😀.txt");
        byte[] bytes = [0xD0, 0x90, 0xF0, 0x9F, 0x98, 0x80];
        File.WriteAllBytes(path, bytes);
        try
        {
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR INPUT AS f USING "UTF-8"
                PRINT READTEXT(f, 1)
                TRY
                    PRINT SEEKFILE(f, {{position}})
                CATCH err
                    PRINT err.Code
                FINALLY
                    PRINT FILEPOS(f)
                END TRY
                PRINT EOF(f)
                PRINT READTEXT(f, 1)
                PRINT FILEPOS(f)
                CLOSE f
                """));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(ExecutionEngineKind.BytecodeVm, result.ActualEngine);
            Assert.Equal(["А", "MB3007", "2", "FALSE", "😀", "6"], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
            using var released = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); Directory.Delete(root); }
    }
}
