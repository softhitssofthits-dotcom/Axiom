using Axiom.Ide.Services.Diagnostics;

namespace Axiom.Ide.Backend.Tests;

public sealed class FileIdeTraceServiceTests
{
    [Fact]
    public void BeginSession_CreatesLogFileAndWritesEntries()
    {
        var logsDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "Axiom.Ide.Backend.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var service = new FileIdeTraceService(logsDirectoryPath);

            service.BeginSession("Test IDE");
            service.Write("run.request", "document=Untitled.axm");
            service.WriteException("run.exception", new InvalidOperationException("boom"), "Failure.");

            var logFilePath = service.CurrentSessionLogPath;

            Assert.StartsWith(logsDirectoryPath, logFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(logFilePath));

            var content = File.ReadAllText(logFilePath);
            Assert.Contains("Test IDE session", content, StringComparison.Ordinal);
            Assert.Contains("run.request | document=Untitled.axm", content, StringComparison.Ordinal);
            Assert.Contains("run.exception | Failure.", content, StringComparison.Ordinal);
            Assert.Contains("InvalidOperationException: boom", content, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(logsDirectoryPath))
            {
                Directory.Delete(logsDirectoryPath, recursive: true);
            }
        }
    }
}
