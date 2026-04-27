namespace Axiom.Ide.Services.EventLog;

public sealed class IdeEventLogOptions
{
    public string? LogsDirectoryPath { get; init; }

    public int QueueCapacity { get; init; } = 4096;

    public int RetentionDays { get; init; } = 14;

    public long MaxDirectoryBytes { get; init; } = 200L * 1024L * 1024L;

    public long MaxFileBytes { get; init; } = 20L * 1024L * 1024L;

    public int AlwaysKeepLatestSessions { get; init; } = 5;
}

