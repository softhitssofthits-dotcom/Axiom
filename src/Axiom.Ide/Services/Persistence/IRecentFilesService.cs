using Axiom.Execution;

namespace Axiom.Ide.Services.Persistence;

public interface IRecentFilesService
{
    RecentFilesSnapshot GetSnapshot();

    RecentFilesSnapshot RememberFile(string filePath);

    RecentFilesSnapshot RemoveFile(string filePath);

    RecentFilesSnapshot UpdateShellPreferences(
        bool isFilesPaneVisible,
        bool isStatusBarVisible,
        IdeWindowState windowState,
        IdeTheme theme,
        IdeLanguage language,
        bool smartIdentifierEnabled = false,
        ExecutionMode preferredExecutionMode = ExecutionMode.Bytecode);
}
