using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class FileSystemCancellationAuditTests
{
    [Theory]
    [InlineData("size")]
    [InlineData("copy")]
    [InlineData("open")]
    [InlineData("append")]
    public void OwnFileLoopsObserveCancellationAndBypassCatch(string operation)
    {
        var root = Path.GetFullPath(Path.Combine(".artifacts", "file-cancel", Guid.NewGuid().ToString("N")));
        var source = Path.Combine(root, "source");
        var destination = Path.Combine(root, "destination");
        var text = Path.Combine(root, "text.txt");
        Directory.CreateDirectory(source);
        try
        {
            for (var i = 0; i < 100; i++) File.WriteAllText(Path.Combine(source, i + ".txt"), "a");
            File.WriteAllText(text, new string('a', 1_000_000));
            var statement = operation switch
            {
                "size" => $"PRINT DIRSIZE(\"{source}\")",
                "copy" => $"PRINT COPYDIR(\"{source}\", \"{destination}\")",
                "open" => $"OPEN \"{text}\" FOR INPUT AS f",
                _ => $"APPENDFILE(\"{text}\", \"b\", \"UTF-8\")"
            };
            var result = new AxiomExecutionPipeline().Run(new ExecutionRequest($$"""
                DIM f AS FILE
                TRY
                    {{statement}}
                    PRINT "continued"
                CATCH e
                    PRINT "caught"
                END TRY
                """, cancellationProvider: new CancelAfterChecks(64)));
            Assert.Equal(ExecutionStatus.Cancelled, result.Status);
            Assert.DoesNotContain("caught", result.Output);
            Assert.DoesNotContain("continued", result.Output);
            Assert.Empty(result.Diagnostics);
            if (operation == "copy") Assert.InRange(Directory.GetFiles(destination).Length, 0, 99);
            Assert.Equal(1_000_000, new FileInfo(text).Length);
        }
        finally { Directory.Delete(root, true); }
    }

    private sealed class CancelAfterChecks(int limit) : IRuntimeCancellationProvider
    {
        private int _checks;
        public bool IsCancellationRequested => ++_checks >= limit;
    }
}
