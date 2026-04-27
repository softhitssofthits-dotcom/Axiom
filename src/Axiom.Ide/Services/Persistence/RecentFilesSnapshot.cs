using Axiom.Execution;

namespace Axiom.Ide.Services.Persistence;

public sealed record RecentFilesSnapshot(
    IReadOnlyList<string> RecentFiles,
    string? LastDirectoryPath,
    bool IsFilesPaneVisible,
    IdeWindowState WindowState,
    bool IsStatusBarVisible = true,
    IdeTheme Theme = IdeTheme.Light,
    IdeLanguage Language = IdeLanguage.Russian,
    bool SmartIdentifierEnabled = false,
    ExecutionMode PreferredExecutionMode = ExecutionMode.Bytecode)
{
    public static RecentFilesSnapshot Empty { get; } =
        new(Array.Empty<string>(), null, true, IdeWindowState.Normal, true, IdeTheme.Light, IdeLanguage.Russian, false, ExecutionMode.Bytecode);
}
