using System.IO;
using System.Text.Json;
using Axiom.Execution;
using Axiom.Ide.Services.Persistence;

namespace Axiom.Ide.Tests;

public sealed class RecentFilesServiceTests : IDisposable
{
    private readonly string _tempDirectoryPath;

    public RecentFilesServiceTests()
    {
        _tempDirectoryPath = Path.Combine(Path.GetTempPath(), "Axiom.Ide.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectoryPath);
    }

    [Fact]
    public void RememberFile_AddsFileToRecentList()
    {
        var stateFilePath = CreateStateFilePath();
        var filePath = CreateFile("one.axm");
        var service = new RecentFilesService(stateFilePath);

        var snapshot = service.RememberFile(filePath);

        Assert.Equal([filePath], snapshot.RecentFiles);
        Assert.Equal(Path.GetDirectoryName(filePath), snapshot.LastDirectoryPath);
        Assert.True(snapshot.IsFilesPaneVisible);
        Assert.True(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Normal, snapshot.WindowState);
        Assert.Equal(IdeTheme.Light, snapshot.Theme);
        Assert.Equal(IdeLanguage.Russian, snapshot.Language);
        Assert.False(snapshot.SmartIdentifierEnabled);
        Assert.Equal(ExecutionMode.Bytecode, snapshot.PreferredExecutionMode);
    }

    [Fact]
    public void RememberFile_MovesExistingFileToTop()
    {
        var stateFilePath = CreateStateFilePath();
        var firstFilePath = CreateFile("one.axm");
        var secondFilePath = CreateFile("two.axm");
        var service = new RecentFilesService(stateFilePath);

        service.RememberFile(firstFilePath);
        service.RememberFile(secondFilePath);

        var snapshot = service.RememberFile(firstFilePath);

        Assert.Equal([firstFilePath, secondFilePath], snapshot.RecentFiles);
    }

    [Fact]
    public void RememberFile_EnforcesMaxRecentFilesLimit()
    {
        var stateFilePath = CreateStateFilePath();
        var filePaths = Enumerable.Range(1, 4)
            .Select(index => CreateFile($"file-{index}.axm"))
            .ToArray();
        var service = new RecentFilesService(stateFilePath, maxRecentFiles: 3);

        foreach (var filePath in filePaths)
        {
            service.RememberFile(filePath);
        }

        var snapshot = service.GetSnapshot();

        Assert.Equal([filePaths[3], filePaths[2], filePaths[1]], snapshot.RecentFiles);
    }

    [Fact]
    public void RememberFile_DoesNotAccumulateDuplicates()
    {
        var stateFilePath = CreateStateFilePath();
        var filePath = CreateFile("duplicate.axm");
        var service = new RecentFilesService(stateFilePath);

        service.RememberFile(filePath);
        var snapshot = service.RememberFile(filePath);

        Assert.Single(snapshot.RecentFiles);
        Assert.Equal(filePath, snapshot.RecentFiles[0]);
    }

    [Fact]
    public void GetSnapshot_LoadsPersistedRecentFiles()
    {
        var filePath = CreateFile("persisted.axm");
        var stateFilePath = CreateStateFilePath();

        File.WriteAllText(
            stateFilePath,
            JsonSerializer.Serialize(new RecentFilesState
            {
                RecentFiles = [filePath],
                LastDirectoryPath = Path.GetDirectoryName(filePath)
            }));

        var service = new RecentFilesService(stateFilePath);
        var snapshot = service.GetSnapshot();

        Assert.Equal([filePath], snapshot.RecentFiles);
        Assert.Equal(Path.GetDirectoryName(filePath), snapshot.LastDirectoryPath);
        Assert.True(snapshot.IsFilesPaneVisible);
        Assert.True(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Normal, snapshot.WindowState);
        Assert.Equal(IdeTheme.Light, snapshot.Theme);
        Assert.Equal(IdeLanguage.Russian, snapshot.Language);
        Assert.False(snapshot.SmartIdentifierEnabled);
        Assert.Equal(ExecutionMode.Bytecode, snapshot.PreferredExecutionMode);
    }

    [Fact]
    public void GetSnapshot_HandlesCorruptedPersistenceDataGracefully()
    {
        var stateFilePath = CreateStateFilePath();
        File.WriteAllText(stateFilePath, "{ this is not valid json");

        var service = new RecentFilesService(stateFilePath);
        var snapshot = service.GetSnapshot();

        Assert.Empty(snapshot.RecentFiles);
        Assert.Null(snapshot.LastDirectoryPath);
        Assert.True(snapshot.IsFilesPaneVisible);
        Assert.True(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Normal, snapshot.WindowState);
        Assert.Equal(IdeTheme.Light, snapshot.Theme);
        Assert.Equal(IdeLanguage.Russian, snapshot.Language);
        Assert.False(snapshot.SmartIdentifierEnabled);
        Assert.Equal(ExecutionMode.Bytecode, snapshot.PreferredExecutionMode);
    }

    [Fact]
    public void GetSnapshot_FiltersNonexistentFilesGracefully()
    {
        var existingFilePath = CreateFile("existing.axm");
        var missingFilePath = Path.Combine(_tempDirectoryPath, "missing.axm");
        var stateFilePath = CreateStateFilePath();

        File.WriteAllText(
            stateFilePath,
            JsonSerializer.Serialize(new RecentFilesState
            {
                RecentFiles = [missingFilePath, existingFilePath],
                LastDirectoryPath = _tempDirectoryPath
            }));

        var service = new RecentFilesService(stateFilePath);
        var snapshot = service.GetSnapshot();

        Assert.Equal([existingFilePath], snapshot.RecentFiles);
        Assert.Equal(_tempDirectoryPath, snapshot.LastDirectoryPath);
        Assert.True(snapshot.IsFilesPaneVisible);
        Assert.True(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Normal, snapshot.WindowState);
        Assert.False(snapshot.SmartIdentifierEnabled);
    }

    [Fact]
    public void UpdateShellPreferences_PersistsFilesPaneAndStatusBarVisibilityAndWindowState()
    {
        var stateFilePath = CreateStateFilePath();
        var service = new RecentFilesService(stateFilePath);

        service.UpdateShellPreferences(
            isFilesPaneVisible: false,
            isStatusBarVisible: false,
            IdeWindowState.Maximized,
            IdeTheme.Dark,
            IdeLanguage.English,
            smartIdentifierEnabled: true,
            preferredExecutionMode: ExecutionMode.Interpreter);

        var reloadedService = new RecentFilesService(stateFilePath);
        var snapshot = reloadedService.GetSnapshot();

        Assert.False(snapshot.IsFilesPaneVisible);
        Assert.False(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Maximized, snapshot.WindowState);
        Assert.Equal(IdeTheme.Dark, snapshot.Theme);
        Assert.Equal(IdeLanguage.English, snapshot.Language);
        Assert.True(snapshot.SmartIdentifierEnabled);
        Assert.Equal(ExecutionMode.Interpreter, snapshot.PreferredExecutionMode);
    }

    [Fact]
    public void GetSnapshot_LoadsPersistedShellPreferences()
    {
        var stateFilePath = CreateStateFilePath();
        File.WriteAllText(
            stateFilePath,
            JsonSerializer.Serialize(new RecentFilesState
            {
                IsFilesPaneVisible = false,
                IsStatusBarVisible = false,
                WindowState = IdeWindowState.Minimized.ToString(),
                Theme = IdeTheme.Dark.ToString(),
                Language = IdeLanguage.English.ToString(),
                SmartIdentifierEnabled = true,
                PreferredExecutionMode = ExecutionMode.Bytecode.ToString()
            }));

        var service = new RecentFilesService(stateFilePath);
        var snapshot = service.GetSnapshot();

        Assert.False(snapshot.IsFilesPaneVisible);
        Assert.False(snapshot.IsStatusBarVisible);
        Assert.Equal(IdeWindowState.Minimized, snapshot.WindowState);
        Assert.Equal(IdeTheme.Dark, snapshot.Theme);
        Assert.Equal(IdeLanguage.English, snapshot.Language);
        Assert.True(snapshot.SmartIdentifierEnabled);
        Assert.Equal(ExecutionMode.Bytecode, snapshot.PreferredExecutionMode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectoryPath))
        {
            Directory.Delete(_tempDirectoryPath, recursive: true);
        }
    }

    private string CreateFile(string fileName)
    {
        var filePath = Path.Combine(_tempDirectoryPath, fileName);
        File.WriteAllText(filePath, "PRINT 1");
        return filePath;
    }

    private string CreateStateFilePath()
    {
        return Path.Combine(_tempDirectoryPath, "recent-files.json");
    }
}

