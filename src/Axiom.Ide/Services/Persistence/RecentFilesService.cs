using Axiom.Execution;
using System.IO;
using System.Text.Json;

namespace Axiom.Ide.Services.Persistence;

public sealed class RecentFilesService : IRecentFilesService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly int _maxRecentFiles;
    private readonly string _stateFilePath;
    private RecentFilesState _state;

    public RecentFilesService(string? stateFilePath = null, int maxRecentFiles = 8)
    {
        if (maxRecentFiles <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRecentFiles), "Maximum recent file count must be positive.");
        }

        _maxRecentFiles = maxRecentFiles;
        _stateFilePath = stateFilePath ?? CreateDefaultStateFilePath();
        _state = LoadState();
    }

    public RecentFilesSnapshot GetSnapshot() => CreateSnapshot(_state);

    public RecentFilesSnapshot RememberFile(string filePath)
    {
        if (!TryNormalizeFilePath(filePath, out var normalizedFilePath) || !File.Exists(normalizedFilePath))
        {
            return GetSnapshot();
        }

        var recentFiles = new List<string> { normalizedFilePath };

        foreach (var existingFilePath in _state.RecentFiles)
        {
            if (!TryNormalizeFilePath(existingFilePath, out var normalizedExistingFilePath) ||
                !File.Exists(normalizedExistingFilePath) ||
                normalizedExistingFilePath.Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            recentFiles.Add(normalizedExistingFilePath);
        }

        _state = NormalizeState(new RecentFilesState
        {
            RecentFiles = recentFiles,
            LastDirectoryPath = Path.GetDirectoryName(normalizedFilePath),
            IsFilesPaneVisible = _state.IsFilesPaneVisible,
            IsStatusBarVisible = _state.IsStatusBarVisible,
            WindowState = _state.WindowState,
            Theme = _state.Theme,
            Language = _state.Language,
            SmartIdentifierEnabled = _state.SmartIdentifierEnabled
        });

        SaveState();
        return CreateSnapshot(_state);
    }

    public RecentFilesSnapshot RemoveFile(string filePath)
    {
        if (!TryNormalizeFilePath(filePath, out var normalizedFilePath))
        {
            return GetSnapshot();
        }

        var recentFiles = _state.RecentFiles
            .Where(existingFilePath => !existingFilePath.Equals(normalizedFilePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        _state = NormalizeState(new RecentFilesState
        {
            RecentFiles = recentFiles,
            LastDirectoryPath = _state.LastDirectoryPath,
            IsFilesPaneVisible = _state.IsFilesPaneVisible,
            IsStatusBarVisible = _state.IsStatusBarVisible,
            WindowState = _state.WindowState,
            Theme = _state.Theme,
            Language = _state.Language,
            SmartIdentifierEnabled = _state.SmartIdentifierEnabled
        });

        SaveState();
        return CreateSnapshot(_state);
    }

    public RecentFilesSnapshot UpdateShellPreferences(
        bool isFilesPaneVisible,
        bool isStatusBarVisible,
        IdeWindowState windowState,
        IdeTheme theme,
        IdeLanguage language,
        bool smartIdentifierEnabled = false)
    {
        _state = NormalizeState(new RecentFilesState
        {
            RecentFiles = _state.RecentFiles.ToList(),
            LastDirectoryPath = _state.LastDirectoryPath,
            IsFilesPaneVisible = isFilesPaneVisible,
            IsStatusBarVisible = isStatusBarVisible,
            WindowState = windowState.ToString(),
            Theme = theme.ToString(),
            Language = language.ToString(),
            SmartIdentifierEnabled = smartIdentifierEnabled
        });

        SaveState();
        return CreateSnapshot(_state);
    }

    private RecentFilesState LoadState()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new RecentFilesState();
        }

        try
        {
            using var stream = File.OpenRead(_stateFilePath);
            var rawState = JsonSerializer.Deserialize<RecentFilesState>(stream, SerializerOptions) ?? new RecentFilesState();
            var normalizedState = NormalizeState(rawState);

            if (!StateEquals(rawState, normalizedState))
            {
                _state = normalizedState;
                SaveState();
            }

            return normalizedState;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new RecentFilesState();
        }
    }

    private void SaveState()
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(_stateFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = JsonSerializer.Serialize(_state, SerializerOptions);
            File.WriteAllText(_stateFilePath, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private RecentFilesState NormalizeState(RecentFilesState state)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var recentFiles = new List<string>();

        foreach (var filePath in state.RecentFiles)
        {
            if (!TryNormalizeFilePath(filePath, out var normalizedFilePath) ||
                !File.Exists(normalizedFilePath) ||
                !seen.Add(normalizedFilePath))
            {
                continue;
            }

            recentFiles.Add(normalizedFilePath);

            if (recentFiles.Count == _maxRecentFiles)
            {
                break;
            }
        }

        return new RecentFilesState
        {
            RecentFiles = recentFiles,
            LastDirectoryPath = NormalizeDirectoryPath(state.LastDirectoryPath, recentFiles),
            IsFilesPaneVisible = state.IsFilesPaneVisible ?? true,
            IsStatusBarVisible = state.IsStatusBarVisible ?? true,
            WindowState = NormalizeWindowState(state.WindowState).ToString(),
            Theme = NormalizeTheme(state.Theme).ToString(),
            Language = NormalizeLanguage(state.Language).ToString(),
            SmartIdentifierEnabled = state.SmartIdentifierEnabled ?? false
        };
    }

    private static IdeWindowState NormalizeWindowState(string? windowStateValue)
    {
        if (string.IsNullOrWhiteSpace(windowStateValue))
        {
            return IdeWindowState.Normal;
        }

        return Enum.TryParse<IdeWindowState>(windowStateValue, ignoreCase: true, out var windowState)
            ? windowState
            : IdeWindowState.Normal;
    }

    private static IdeTheme NormalizeTheme(string? themeValue)
    {
        if (string.IsNullOrWhiteSpace(themeValue))
        {
            return IdeTheme.Light;
        }

        return Enum.TryParse<IdeTheme>(themeValue, ignoreCase: true, out var theme)
            ? theme
            : IdeTheme.Light;
    }

    private static IdeLanguage NormalizeLanguage(string? languageValue)
    {
        if (string.IsNullOrWhiteSpace(languageValue))
        {
            return IdeLanguage.Russian;
        }

        return Enum.TryParse<IdeLanguage>(languageValue, ignoreCase: true, out var language)
            ? language
            : IdeLanguage.Russian;
    }

    private static string? NormalizeDirectoryPath(string? persistedDirectoryPath, IReadOnlyList<string> recentFiles)
    {
        if (!string.IsNullOrWhiteSpace(persistedDirectoryPath))
        {
            try
            {
                var normalizedDirectoryPath = Path.GetFullPath(persistedDirectoryPath);

                if (Directory.Exists(normalizedDirectoryPath))
                {
                    return normalizedDirectoryPath;
                }
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
            {
            }
        }

        foreach (var filePath in recentFiles)
        {
            var directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directoryPath) && Directory.Exists(directoryPath))
            {
                return directoryPath;
            }
        }

        return null;
    }

    private static bool TryNormalizeFilePath(string? filePath, out string normalizedFilePath)
    {
        normalizedFilePath = string.Empty;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        try
        {
            normalizedFilePath = Path.GetFullPath(filePath);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or NotSupportedException)
        {
            return false;
        }
    }

    private static string CreateDefaultStateFilePath()
    {
        var baseDirectoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Axiom",
            "Axiom.Ide");

        return Path.Combine(baseDirectoryPath, "recent-files.json");
    }

    private static bool StateEquals(RecentFilesState left, RecentFilesState right)
    {
        return string.Equals(left.LastDirectoryPath, right.LastDirectoryPath, StringComparison.OrdinalIgnoreCase) &&
               left.RecentFiles.SequenceEqual(right.RecentFiles, StringComparer.OrdinalIgnoreCase) &&
               (left.IsFilesPaneVisible ?? true) == (right.IsFilesPaneVisible ?? true) &&
               (left.IsStatusBarVisible ?? true) == (right.IsStatusBarVisible ?? true) &&
               NormalizeWindowState(left.WindowState) == NormalizeWindowState(right.WindowState) &&
               NormalizeTheme(left.Theme) == NormalizeTheme(right.Theme) &&
               NormalizeLanguage(left.Language) == NormalizeLanguage(right.Language) &&
               (left.SmartIdentifierEnabled ?? false) == (right.SmartIdentifierEnabled ?? false);
    }

    private static RecentFilesSnapshot CreateSnapshot(RecentFilesState state)
    {
        return new RecentFilesSnapshot(
            state.RecentFiles.AsReadOnly(),
            state.LastDirectoryPath,
            state.IsFilesPaneVisible ?? true,
            NormalizeWindowState(state.WindowState),
            state.IsStatusBarVisible ?? true,
            NormalizeTheme(state.Theme),
            NormalizeLanguage(state.Language),
            state.SmartIdentifierEnabled ?? false);
    }
}
