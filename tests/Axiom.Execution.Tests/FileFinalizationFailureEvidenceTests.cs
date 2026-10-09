using Axiom.Runtime;
using System.Text;

namespace Axiom.Execution.Tests;

public sealed class FileFinalizationFailureEvidenceTests
{
    public static IEnumerable<object[]> FlushCases()
    {
        foreach (var mode in new[] { ExecutionMode.Interpreter, ExecutionMode.Bytecode })
        foreach (var append in new[] { false, true })
        foreach (var large in new[] { false, true }) yield return [mode, append, large];
    }

    [Theory]
    [MemberData(nameof(FlushCases))]
    public void FlushAfterUnlockWritesBufferedTextExactlyOnce(ExecutionMode mode, bool append, bool large)
    {
        if (!OperatingSystem.IsWindows()) return;
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "Начало", new UTF8Encoding(false));
            var text = large ? new string('А', 4096) + "😀" : "А😀";
            var original = append ? "Начало" : "";
            var bytes = Encoding.UTF8.GetBytes(original + text + "Я");
            using var writer = new LockingWriter(path);
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR {{(append ? "APPEND" : "OUTPUT")}} AS f
                PRINT WRITE(f, "{{text}}")
                PRINT "Заблокировать"
                TRY
                    PRINT FLUSH(f)
                CATCH err
                    PRINT err.Code
                END TRY
                PRINT ISOPEN(f)
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                PRINT "Разблокировать"
                PRINT WRITE(f, "Я")
                PRINT FLUSH(f)
                PRINT FLUSH(f)
                PRINT FILEPOS(f)
                PRINT FILELENGTH(f)
                CLOSE f
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode, outputWriter: writer));
            Assert.Equal(ExecutionStatus.Success, result.Status);
            Assert.Empty(result.Diagnostics);
            Assert.False(result.UsedInterpreterFallback);
            Assert.True(writer.Locked);
            Assert.Equal(["TRUE", "Заблокировать", "MB3007", "TRUE", (bytes.Length - 2).ToString(),
                (bytes.Length - 2).ToString(), "Разблокировать", "TRUE", "TRUE", "TRUE", bytes.Length.ToString(), bytes.Length.ToString()], result.Output);
            Assert.Equal(bytes, File.ReadAllBytes(path));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void AutomaticCloseFailurePreservesPrimaryProgramError(ExecutionMode mode)
    {
        if (!OperatingSystem.IsWindows()) return;
        var path = Path.GetTempFileName();
        try
        {
            using var writer = new LockingWriter(path);
            var source = $$"""
                DIM f AS FILE
                OPEN "{{path}}" FOR OUTPUT AS f
                PRINT WRITE(f, "Данные")
                PRINT "Заблокировать"
                ERROR "APP1173", "Первичная ошибка"
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode, outputWriter: writer));
            Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
            Assert.False(result.UsedInterpreterFallback);
            Assert.True(writer.Locked);
            Assert.Equal(["APP1173", "MB3007"], result.Diagnostics.Select(d => d.Code));
            Assert.Contains("Первичная ошибка", result.Diagnostics[0].Message);
            writer.Dispose();
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(ExecutionMode.Interpreter, false)]
    [InlineData(ExecutionMode.Bytecode, false)]
    [InlineData(ExecutionMode.Interpreter, true)]
    [InlineData(ExecutionMode.Bytecode, true)]
    public void FailedCloseReleasesAllResourcesAndReportsAxiomDiagnostic(ExecutionMode mode, bool explicitClose)
    {
        if (!OperatingSystem.IsWindows()) return;
        var first = Path.GetTempFileName();
        var second = Path.GetTempFileName();
        try
        {
            using var writer = new LockingWriter(first);
            var source = $$"""
                DIM a AS FILE
                DIM b AS FILE
                OPEN "{{first}}" FOR OUTPUT AS a
                OPEN "{{second}}" FOR OUTPUT AS b
                PRINT WRITE(a, "Первый")
                PRINT WRITE(b, "Второй")
                PRINT "Заблокировать"
                {{(explicitClose ? "TRY\nCLOSE a\nCATCH err\nPRINT err.Code\nEND TRY\nPRINT ISOPEN(a)\nCLOSE b" : "")}}
                """;
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest(source, mode: mode, outputWriter: writer));
            Assert.False(result.UsedInterpreterFallback);
            Assert.True(writer.Locked);
            if (explicitClose)
            {
                Assert.Equal(ExecutionStatus.Success, result.Status);
                Assert.Empty(result.Diagnostics);
                Assert.Contains("MB3007", result.Output);
                Assert.Contains("FALSE", result.Output);
            }
            else
            {
                Assert.Equal(ExecutionStatus.RuntimeFailure, result.Status);
                Assert.Contains(result.Diagnostics, d => d.Code == "MB3007");
            }
            writer.Dispose();
            using var exclusive = new FileStream(first, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.Equal("Второй", File.ReadAllText(second));
        }
        finally { File.Delete(first); File.Delete(second); }
    }

    private sealed class LockingWriter(string path) : IOutputWriter, IDisposable
    {
        private FileStream? _blocker;
        public bool Locked { get; private set; }
        public void WriteOutput(string text)
        {
            if (!OperatingSystem.IsWindows()) return;
            if (text.Contains("Разблокировать", StringComparison.Ordinal)) { Dispose(); return; }
            if (!text.Contains("Заблокировать", StringComparison.Ordinal) || Locked) return;
            _blocker = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _blocker.Lock(0, 4096);
            Locked = true;
        }
        public void Dispose()
        {
            if (!OperatingSystem.IsWindows()) return;
            if (_blocker is null) return;
            _blocker.Unlock(0, 4096);
            _blocker.Dispose();
            _blocker = null;
        }
    }
}
