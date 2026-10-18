using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Axiom.Ide.Services.EventLog;

namespace Axiom.Ide.Backend.Tests;

public sealed class FileIdeEventLogServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    [Fact]
    public void BeginSessionAndWrite_CreatesJsonlWithStructuredFields()
    {
        var logsDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "Axiom.Ide.EventLog.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var service = new FileIdeEventLogService(new IdeEventLogOptions
            {
                LogsDirectoryPath = logsDirectoryPath,
                QueueCapacity = 64
            });

            try
            {
                service.BeginSession("IDE test");
                service.Write(new IdeEventLogEntry
                {
                    Level = IdeEventLevel.Info,
                    Category = "commands",
                    EventName = "command.executed",
                    CommandName = "Run",
                    Source = "test",
                    Properties = new Dictionary<string, string?>(StringComparer.Ordinal)
                    {
                        ["mode"] = "Bytecode"
                    }
                });
                service.Flush();

                var logPath = service.CurrentSessionLogPath;
                service.Dispose();
            Assert.StartsWith(logsDirectoryPath, logPath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(logPath));

            var lines = File.ReadAllLines(logPath)
                .Where(static line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
            Assert.True(lines.Length >= 2);

            using var lastJson = JsonDocument.Parse(lines[^1]);
            var root = lastJson.RootElement;

            Assert.Equal("commands", root.GetProperty("category").GetString());
            Assert.Equal("command.executed", root.GetProperty("eventName").GetString());
            Assert.Equal("Run", root.GetProperty("commandName").GetString());
            Assert.Equal("Bytecode", root.GetProperty("properties").GetProperty("mode").GetString());
            Assert.True(root.GetProperty("sequence").GetInt64() > 0);
            }
            finally
            {
                service.Dispose();
            }
        }
        finally
        {
            if (Directory.Exists(logsDirectoryPath))
            {
                Directory.Delete(logsDirectoryPath, recursive: true);
            }
        }
    }

    [Fact]
    public void DeleteInactiveLogs_RemovesOldSessionFilesAndKeepsCurrentSessionFile()
    {
        var logsDirectoryPath = Path.Combine(
            Path.GetTempPath(),
            "Axiom.Ide.EventLog.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(logsDirectoryPath);
            var oldLogPath = Path.Combine(logsDirectoryPath, "ide-events-20000101-000000.jsonl");
            File.WriteAllText(oldLogPath, "{}");

            using var service = new FileIdeEventLogService(new IdeEventLogOptions
            {
                LogsDirectoryPath = logsDirectoryPath,
                QueueCapacity = 64
            });
            service.BeginSession("IDE test");
            service.Write(new IdeEventLogEntry
            {
                Level = IdeEventLevel.Info,
                Category = "test",
                EventName = "current",
                Source = "test"
            });
            service.Flush();

            var currentLogPath = service.CurrentSessionLogPath;
            var deleted = service.DeleteInactiveLogs();
            service.Flush();

            Assert.Equal(1, deleted);
            Assert.False(File.Exists(oldLogPath));
            Assert.True(File.Exists(currentLogPath));
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
