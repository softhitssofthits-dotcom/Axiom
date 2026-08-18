using System.IO;
using Axiom.Ide.Documents;
using Axiom.Ide.Files;
using Axiom.Ide.Services.EventLog;

namespace Axiom.Ide.ViewModels;

public sealed partial class MainWindowViewModel
{
    private void CloseFilesPane()
    {
        FilesPane.IsVisible = false;
    }

    private void OpenFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsDirectory || node.IsPlaceholder)
        {
            return;
        }

        OpenFilesPaneNodeInEditor(node);
        LogIdeEvent(
            IdeEventLevel.Info,
            "files",
            "file.opened-from-tree",
            commandName: "OpenFromFileManager",
            document: Documents.ActiveDocument,
            textPreview: node.FullPath);
    }

    private void NavigateFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        if (!node.IsDirectory)
        {
            OpenFilesPaneNodeInEditor(node);
            return;
        }

        FilesPane.RootPath = node.FullPath;
        ReloadFilesPane(preferredSelectionPath: node.FullPath);
        FilesPane.IsVisible = true;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Navigate.Status", "Navigated to {0}.", node.FullPath);
    }

    private void OpenFilesPaneNodeExternally(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        try
        {
            _externalNavigationService.OpenPath(node.FullPath);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.External.StatusOpened", "Opened externally: {0}.", node.DisplayName);
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.opened-externally",
                commandName: "OpenExternal",
                document: Documents.ActiveDocument,
                textPreview: node.FullPath);
        }
        catch (Exception ex)
        {
            var message = GetLocalizedFormattedString(
                "Files.External.ErrorMessage",
                "Could not open '{0}' in the default application.\r\n\r\n{1}",
                node.FullPath,
                ex.Message);
            _promptService.ShowError(message, GetLocalizedString("Files.Title", "File Manager"));
            StatusBar.StatusText = GetLocalizedFormattedString("Files.External.StatusFailed", "Open failed: {0}.", node.DisplayName);
        }
    }

    private void CreateFilesPaneFile(object? parameter)
    {
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Common.TargetDirectoryUnavailable", "Target directory is not available.");
            return;
        }

        var fileName = _promptService.ShowTextInput(
            GetLocalizedString("Files.NewFile.Prompt", "Enter the name of the new file."),
            GetLocalizedString("Files.NewFile.Title", "New File"),
            GetLocalizedString("Files.NewFile.DefaultName", "New file.axm"),
            GetLocalizedString("Files.NewItem.CreateButton", "Create"));

        if (fileName is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.NewFile.StatusCancelled", "File creation cancelled.");
            return;
        }

        try
        {
            var createdPath = _fileOperationsService.CreateEmptyFile(targetDirectoryPath, fileName);
            var createdNode = ReloadFilesPane(preferredSelectionPath: createdPath);
            OpenFilesPaneNodeInEditor(createdNode ?? new FileTreeNode
            {
                FullPath = createdPath,
                DisplayName = Path.GetFileName(createdPath),
                IsDirectory = false
            });
            StatusBar.StatusText = GetLocalizedFormattedString("Files.NewFile.StatusCreated", "Created file: {0}.", Path.GetFileName(createdPath));
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.created",
                commandName: "NewFile",
                document: Documents.ActiveDocument,
                textPreview: createdPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.NewFile.Title", "New File"));
            StatusBar.StatusText = GetLocalizedString("Files.NewFile.StatusFailed", "File creation failed.");
        }
    }

    private void CreateFilesPaneDirectory(object? parameter)
    {
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Common.TargetDirectoryUnavailable", "Target directory is not available.");
            return;
        }

        var directoryName = _promptService.ShowTextInput(
            GetLocalizedString("Files.NewDirectory.Prompt", "Enter the name of the new folder."),
            GetLocalizedString("Files.NewDirectory.Title", "New Folder"),
            GetLocalizedString("Files.NewDirectory.DefaultName", "New Folder"),
            GetLocalizedString("Files.NewItem.CreateButton", "Create"));

        if (directoryName is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.NewDirectory.StatusCancelled", "Folder creation cancelled.");
            return;
        }

        try
        {
            var createdPath = _fileOperationsService.CreateDirectory(targetDirectoryPath, directoryName);
            ReloadFilesPane(preferredSelectionPath: createdPath);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.NewDirectory.StatusCreated", "Created folder: {0}.", Path.GetFileName(createdPath));
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "directory.created",
                commandName: "NewDirectory",
                document: Documents.ActiveDocument,
                textPreview: createdPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.NewDirectory.Title", "New Folder"));
            StatusBar.StatusText = GetLocalizedString("Files.NewDirectory.StatusFailed", "Folder creation failed.");
        }
    }

    private void CutFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        _filesPaneClipboardPath = node.FullPath;
        _filesPaneClipboardIsCut = true;
        _filesPaneClipboardIsDirectory = node.IsDirectory;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Clipboard.StatusCut", "Cut: {0}.", node.DisplayName);
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private void CopyFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        _filesPaneClipboardPath = node.FullPath;
        _filesPaneClipboardIsCut = false;
        _filesPaneClipboardIsDirectory = node.IsDirectory;
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Clipboard.StatusCopied", "Copied: {0}.", node.DisplayName);
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private void PasteIntoFilesPane(object? parameter)
    {
        var sourcePath = _filesPaneClipboardPath;
        var targetDirectoryPath = GetFilesPaneTargetDirectoryPath(parameter);

        if (string.IsNullOrWhiteSpace(sourcePath) || targetDirectoryPath is null)
        {
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusNothing", "Nothing to paste.");
            return;
        }

        if (!_fileOperationsService.Exists(sourcePath))
        {
            ClearFilesPaneClipboard();
            _promptService.ShowError(
                GetLocalizedString("Files.Paste.ErrorMissingSource", "The selected file or folder is no longer available."),
                GetLocalizedString("Files.Context.Paste", "Paste"));
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusFailed", "Paste failed.");
            return;
        }

        try
        {
            var isCutOperation = _filesPaneClipboardIsCut;
            var clipboardContainsDirectory = _filesPaneClipboardIsDirectory;
            var targetPath = _filesPaneClipboardIsCut
                ? _fileOperationsService.MoveToDirectory(sourcePath, targetDirectoryPath)
                : _fileOperationsService.CopyToDirectory(sourcePath, targetDirectoryPath);

            if (isCutOperation)
            {
                RetargetOpenDocuments(sourcePath, targetPath, clipboardContainsDirectory);
                ClearFilesPaneClipboard();
                StatusBar.StatusText = GetLocalizedFormattedString("Files.Paste.StatusMoved", "Moved to {0}.", targetDirectoryPath);
                LogIdeEvent(
                    IdeEventLevel.Info,
                    "files",
                    "file.moved",
                    commandName: "PasteMove",
                    document: Documents.ActiveDocument,
                    textPreview: targetPath);
            }
            else
            {
                StatusBar.StatusText = GetLocalizedFormattedString("Files.Paste.StatusCopied", "Copied to {0}.", targetDirectoryPath);
                LogIdeEvent(
                    IdeEventLevel.Info,
                    "files",
                    "file.copied",
                    commandName: "PasteCopy",
                    document: Documents.ActiveDocument,
                    textPreview: targetPath);
            }

            ReloadFilesPane(
                preferredSelectionPath: targetPath,
                remappedPathOld: isCutOperation && clipboardContainsDirectory ? sourcePath : null,
                remappedPathNew: isCutOperation && clipboardContainsDirectory ? targetPath : null,
                remapDescendants: isCutOperation && clipboardContainsDirectory);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Context.Paste", "Paste"));
            StatusBar.StatusText = GetLocalizedString("Files.Paste.StatusFailed", "Paste failed.");
        }
    }

    private void RenameFilesPaneNode(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        BeginFilesPaneNodeRename(node);
    }

    private void MoveFilesPaneNodeToRecycleBin(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        var confirmationMessage = node.IsDirectory
            ? GetLocalizedFormattedString("Files.Delete.ConfirmDirectory", "Delete folder '{0}'?", node.DisplayName)
            : GetLocalizedFormattedString("Files.Delete.ConfirmFile", "Delete file '{0}'?", node.DisplayName);

        if (!_promptService.ShowConfirmation(confirmationMessage, GetLocalizedString("Files.Delete.Title", "Delete")))
        {
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusCancelled", "Delete cancelled.");
            return;
        }

        if (!CloseDocumentsAffectedByPath(node.FullPath, node.IsDirectory))
        {
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusCancelled", "Delete cancelled.");
            return;
        }

        try
        {
            _fileOperationsService.SendToRecycleBin(node.FullPath);

            if (!string.IsNullOrWhiteSpace(_filesPaneClipboardPath)
                && IsPathEqualOrDescendant(_filesPaneClipboardPath!, node.FullPath, node.IsDirectory))
            {
                ClearFilesPaneClipboard();
            }

            var parentDirectoryPath = Path.GetDirectoryName(node.FullPath) ?? FilesPane.RootPath;
            ReloadFilesPane(
                preferredSelectionPath: parentDirectoryPath,
                removedPath: node.FullPath,
                removeDescendants: node.IsDirectory);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.Delete.StatusRecycled", "Moved to Recycle Bin: {0}.", node.DisplayName);
            LogIdeEvent(
                IdeEventLevel.Info,
                "files",
                "file.deleted",
                commandName: "Delete",
                document: Documents.ActiveDocument,
                textPreview: node.FullPath);
        }
        catch (Exception ex)
        {
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Delete.ErrorTitle", "Recycle Bin"));
            StatusBar.StatusText = GetLocalizedString("Files.Delete.StatusFailed", "Delete failed.");
        }
    }

    public void BeginFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null || node.IsPlaceholder || node.IsDrive)
        {
            return;
        }

        foreach (var treeNode in EnumerateFilesPaneNodes(FilesPane.Nodes))
        {
            if (!ReferenceEquals(treeNode, node))
            {
                treeNode.IsRenaming = false;
            }
        }

        node.RenameText = node.DisplayName;
        node.IsRenaming = true;
    }

    public void CancelFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null)
        {
            return;
        }

        node.RenameText = node.DisplayName;
        node.IsRenaming = false;
        StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusCancelled", "Rename cancelled.");
    }

    public void CommitFilesPaneNodeRename(FileTreeNode? node)
    {
        if (node is null || !node.IsRenaming)
        {
            return;
        }

        var newName = node.RenameText.Trim();
        node.IsRenaming = false;

        if (string.IsNullOrWhiteSpace(newName))
        {
            node.RenameText = node.DisplayName;
            StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusCancelled", "Rename cancelled.");
            return;
        }

        if (string.Equals(newName, node.DisplayName, StringComparison.Ordinal))
        {
            node.RenameText = node.DisplayName;
            return;
        }

        try
        {
            var targetPath = _fileOperationsService.Rename(node.FullPath, newName);
            RetargetOpenDocuments(node.FullPath, targetPath, node.IsDirectory);
            ReloadFilesPane(
                preferredSelectionPath: targetPath,
                remappedPathOld: node.IsDirectory ? node.FullPath : null,
                remappedPathNew: node.IsDirectory ? targetPath : null,
                remapDescendants: node.IsDirectory);
            StatusBar.StatusText = GetLocalizedFormattedString("Files.Rename.StatusSucceeded", "Renamed to {0}.", Path.GetFileName(targetPath));
        }
        catch (Exception ex)
        {
            node.RenameText = node.DisplayName;
            _promptService.ShowError(ex.Message, GetLocalizedString("Files.Context.Rename", "Rename"));
            StatusBar.StatusText = GetLocalizedString("Files.Rename.StatusFailed", "Rename failed.");
        }
    }

    private void ShowFilesPaneNodeProperties(object? parameter)
    {
        if (GetTargetFilesPaneNode(parameter) is not { } node || node.IsPlaceholder)
        {
            return;
        }

        _promptService.ShowInfo(
            BuildFilesPaneNodePropertiesText(node),
            GetLocalizedString("Files.Context.Properties", "Properties"));
        StatusBar.StatusText = GetLocalizedFormattedString("Files.Properties.StatusShown", "Properties: {0}.", node.DisplayName);
    }

    private void ExpandFilesPaneNode(object? parameter)
    {
        if (parameter is not FileTreeNode node || !node.IsDirectory || node.IsLoaded)
        {
            return;
        }

        var children = _fileTreeService.BuildChildNodes(node.FullPath, FilesPane.ShowHiddenFiles);
        node.Children.Clear();

        foreach (var child in children)
        {
            node.Children.Add(child);
        }

        node.IsLoaded = true;
    }

    private bool CanOpenFilesPaneNode(object? parameter) =>
        GetTargetFilesPaneNode(parameter) is FileTreeNode { IsDirectory: false, IsPlaceholder: false } node
        && !IsExecutableFile(node.FullPath)
        && CanModifyDocuments();

    private static bool IsExecutableFile(string filePath) =>
        string.Equals(Path.GetExtension(filePath), ".exe", StringComparison.OrdinalIgnoreCase);

    private void OpenFilesPaneNodeInEditor(FileTreeNode node)
    {
        FilesPane.SelectedNode = node;

        if (IsExecutableFile(node.FullPath))
        {
            StatusBar.StatusText = GetLocalizedString("Files.OpenExecutableBlocked", "Executable .exe files are not opened in editor.");
            return;
        }

        var document = Documents.OpenDocumentFromPath(node.FullPath, out var statusMessage);

        if (document is not null)
        {
            StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusOpened", "Opened {0}.", document.DisplayName);
            UpdateFilesPaneRoot();
            RefreshRecentFiles();
            UpdateTitle();
            return;
        }

        StatusBar.StatusText = statusMessage ?? GetLocalizedFormattedString("Documents.Open.StatusFailedByName", "Open failed: {0}.", Path.GetFileName(node.FullPath));
    }

    private string BuildFilesPaneNodePropertiesText(FileTreeNode node)
    {
        var lines = new List<string>
        {
            $"{GetLocalizedString("Files.Properties.Name", "Name")}: {node.DisplayName}",
            $"{GetLocalizedString("Files.Properties.Type", "Type")}: {GetLocalizedString(node.IsDirectory ? "Files.Properties.TypeDirectory" : "Files.Properties.TypeFile", node.IsDirectory ? "Folder" : "File")}",
            $"{GetLocalizedString("Files.Properties.Path", "Path")}: {node.FullPath}"
        };

        try
        {
            if (node.IsDirectory)
            {
                var directory = new DirectoryInfo(node.FullPath);
                if (directory.Exists)
                {
                    lines.Add($"{GetLocalizedString("Files.Properties.Modified", "Modified")}: {directory.LastWriteTime}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Attributes", "Attributes")}: {directory.Attributes}");
                }
            }
            else
            {
                var file = new FileInfo(node.FullPath);
                if (file.Exists)
                {
                    lines.Add(
                        $"{GetLocalizedString("Files.Properties.Size", "Size")}: {file.Length:N0} {GetLocalizedString("Files.Properties.Bytes", "bytes")}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Modified", "Modified")}: {file.LastWriteTime}");
                    lines.Add($"{GetLocalizedString("Files.Properties.Attributes", "Attributes")}: {file.Attributes}");
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return string.Join(Environment.NewLine, lines);
    }

    private void RaiseFilesPaneCommandCanExecuteChanged()
    {
        NewFilesPaneFileCommand.RaiseCanExecuteChanged();
        NewFilesPaneDirectoryCommand.RaiseCanExecuteChanged();
        CutFilesPaneNodeCommand.RaiseCanExecuteChanged();
        CopyFilesPaneNodeCommand.RaiseCanExecuteChanged();
        PasteIntoFilesPaneCommand.RaiseCanExecuteChanged();
        RenameFilesPaneNodeCommand.RaiseCanExecuteChanged();
        MoveFilesPaneNodeToRecycleBinCommand.RaiseCanExecuteChanged();
    }

    private void ClearFilesPaneClipboard()
    {
        _filesPaneClipboardPath = null;
        _filesPaneClipboardIsCut = false;
        _filesPaneClipboardIsDirectory = false;
        RaiseFilesPaneCommandCanExecuteChanged();
    }

    private bool CloseDocumentsAffectedByPath(string path, bool includeDescendants)
    {
        var affectedDocuments = GetDocumentsAffectedByPath(path, includeDescendants).ToList();

        foreach (var document in affectedDocuments)
        {
            var closed = Documents.CloseDocument(document, PromptSaveDiscardCancel);

            if (!closed)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerable<DocumentTabViewModel> GetDocumentsAffectedByPath(string sourcePath, bool includeDescendants)
    {
        foreach (var document in Documents.Documents)
        {
            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                continue;
            }

            if (IsPathEqualOrDescendant(document.FilePath, sourcePath, includeDescendants))
            {
                yield return document;
            }
        }
    }

    private void RetargetOpenDocuments(string oldPath, string newPath, bool includeDescendants)
    {
        foreach (var document in GetDocumentsAffectedByPath(oldPath, includeDescendants))
        {
            if (document.FilePath is null)
            {
                continue;
            }

            document.FilePath = includeDescendants
                ? ReplacePathPrefix(document.FilePath, oldPath, newPath)
                : newPath;
        }
    }

    private static string ReplacePathPrefix(string currentPath, string oldPrefix, string newPrefix)
    {
        if (string.Equals(currentPath, oldPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return newPrefix;
        }

        var suffix = currentPath[oldPrefix.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return Path.Combine(newPrefix, suffix);
    }

    private static HashSet<string> CollectExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes)
    {
        var expandedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectExpandedDirectoryPaths(nodes, expandedPaths);
        return expandedPaths;
    }

    private static void CollectExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes, ISet<string> expandedPaths)
    {
        foreach (var node in nodes)
        {
            if (!node.IsDirectory || !node.IsExpanded)
            {
                continue;
            }

            expandedPaths.Add(node.FullPath);
            CollectExpandedDirectoryPaths(node.Children, expandedPaths);
        }
    }

    private static IEnumerable<FileTreeNode> EnumerateFilesPaneNodes(IEnumerable<FileTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;

            foreach (var child in EnumerateFilesPaneNodes(node.Children))
            {
                yield return child;
            }
        }
    }

    private void RestoreExpandedDirectoryPaths(IEnumerable<FileTreeNode> nodes, ISet<string> expandedPaths)
    {
        foreach (var node in nodes)
        {
            if (!node.IsDirectory || !expandedPaths.Contains(node.FullPath))
            {
                continue;
            }

            node.IsExpanded = true;

            if (!node.IsLoaded)
            {
                var children = _fileTreeService.BuildChildNodes(node.FullPath, FilesPane.ShowHiddenFiles);
                node.Children.Clear();

                foreach (var child in children)
                {
                    node.Children.Add(child);
                }

                node.IsLoaded = true;
            }

            RestoreExpandedDirectoryPaths(node.Children, expandedPaths);
        }
    }

    private static void ExpandAncestorDirectoryPaths(ISet<string> expandedPaths, string? selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        var currentPath = Path.GetDirectoryName(selectedPath);

        while (!string.IsNullOrWhiteSpace(currentPath))
        {
            expandedPaths.Add(currentPath);
            currentPath = Path.GetDirectoryName(
                currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
    }

    private static HashSet<string> RemapTrackedPaths(
        IEnumerable<string> trackedPaths,
        string oldPath,
        string newPath,
        bool includeDescendants)
    {
        var remappedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var trackedPath in trackedPaths)
        {
            remappedPaths.Add(RemapTrackedPath(trackedPath, oldPath, newPath, includeDescendants) ?? trackedPath);
        }

        return remappedPaths;
    }

    private static string? RemapTrackedPath(
        string? trackedPath,
        string oldPath,
        string newPath,
        bool includeDescendants)
    {
        if (string.IsNullOrWhiteSpace(trackedPath)
            || !IsPathEqualOrDescendant(trackedPath, oldPath, includeDescendants))
        {
            return trackedPath;
        }

        return includeDescendants
            ? ReplacePathPrefix(trackedPath, oldPath, newPath)
            : newPath;
    }

    private static void RemoveTrackedPaths(ISet<string> trackedPaths, string removedPath, bool includeDescendants)
    {
        foreach (var trackedPath in trackedPaths
                     .Where(path => IsPathEqualOrDescendant(path, removedPath, includeDescendants))
                     .ToArray())
        {
            trackedPaths.Remove(trackedPath);
        }
    }

    private static FileTreeNode? FindNodeByPath(IEnumerable<FileTreeNode> nodes, string? targetPath)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return null;
        }

        foreach (var node in nodes)
        {
            if (string.Equals(node.FullPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var childMatch = FindNodeByPath(node.Children, targetPath);
            if (childMatch is not null)
            {
                return childMatch;
            }
        }

        return null;
    }

    private static bool IsPathEqualOrDescendant(string candidatePath, string parentPath, bool includeDescendants)
    {
        if (string.Equals(candidatePath, parentPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!includeDescendants)
        {
            return false;
        }

        var normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedCandidate.StartsWith(
            normalizedParent + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }
}
