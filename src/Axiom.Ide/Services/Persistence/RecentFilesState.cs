namespace Axiom.Ide.Services.Persistence;

public sealed class RecentFilesState
{
    public List<string> RecentFiles { get; init; } = [];

    public string? LastDirectoryPath { get; init; }

    public bool? IsFilesPaneVisible { get; init; }

    public bool? IsStatusBarVisible { get; init; }

    public string? WindowState { get; init; }

    public string? Theme { get; init; }

    public string? Language { get; init; }

    public bool? SmartIdentifierEnabled { get; init; }

    public string? PreferredExecutionMode { get; init; }
}
