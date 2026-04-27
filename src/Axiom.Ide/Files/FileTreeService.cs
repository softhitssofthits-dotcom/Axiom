using System.IO;

namespace Axiom.Ide.Files;

public interface IFileTreeService
{
    IReadOnlyList<FileTreeNode> BuildTree(string rootPath, bool showHiddenFiles = false);
    IReadOnlyList<FileTreeNode> BuildChildNodes(string directoryPath, bool showHiddenFiles = false);
}

public sealed class FileTreeService : IFileTreeService
{
    public IReadOnlyList<FileTreeNode> BuildTree(string rootPath, bool showHiddenFiles = false)
    {
        var roots = BuildDriveRoots();

        if (roots.Count > 0)
        {
            return roots;
        }

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return [];
        }

        var fallback = CreateDirectoryNode(new DirectoryInfo(rootPath), showHiddenFiles, useFullPathAsName: true);
        return [fallback];
    }

    public IReadOnlyList<FileTreeNode> BuildChildNodes(string directoryPath, bool showHiddenFiles = false)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return [];
        }

        return BuildChildNodes(new DirectoryInfo(directoryPath), showHiddenFiles);
    }

    private static IReadOnlyList<FileTreeNode> BuildDriveRoots()
    {
        var roots = new List<FileTreeNode>();

        foreach (var drive in DriveInfo.GetDrives().OrderBy(drive => drive.Name, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var rootDirectory = drive.RootDirectory;
                if (!rootDirectory.Exists)
                {
                    continue;
                }

                var displayName = $"Disk ({drive.Name.TrimEnd('\\')})";
                var node = CreateDirectoryNode(rootDirectory, showHiddenFiles: false, displayNameOverride: displayName);
                node.IsDrive = true;
                roots.Add(node);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return roots;
    }

    private static IReadOnlyList<FileTreeNode> BuildChildNodes(DirectoryInfo directory, bool showHiddenFiles)
    {
        var nodes = new List<FileTreeNode>();

        foreach (var childDirectory in EnumerateDirectories(directory, showHiddenFiles))
        {
            nodes.Add(CreateDirectoryNode(childDirectory, showHiddenFiles));
        }

        foreach (var file in EnumerateFiles(directory, showHiddenFiles))
        {
            nodes.Add(new FileTreeNode
            {
                FullPath = file.FullName,
                DisplayName = file.Name,
                IsDirectory = false
            });
        }

        return nodes;
    }

    private static FileTreeNode CreateDirectoryNode(
        DirectoryInfo directory,
        bool showHiddenFiles,
        bool useFullPathAsName = false,
        string? displayNameOverride = null)
    {
        var node = new FileTreeNode
        {
            FullPath = directory.FullName,
            DisplayName = displayNameOverride ?? (useFullPathAsName ? directory.FullName : directory.Name),
            IsDirectory = true
        };

        if (HasVisibleChildren(directory, showHiddenFiles))
        {
            node.Children.Add(FileTreeNode.CreatePlaceholder());
        }
        else
        {
            node.IsLoaded = true;
        }

        return node;
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectories(DirectoryInfo directory, bool showHiddenFiles)
    {
        try
        {
            return directory.EnumerateDirectories()
                .Where(item => ShouldInclude(item, showHiddenFiles))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory, bool showHiddenFiles)
    {
        try
        {
            return directory.EnumerateFiles()
                .Where(item => ShouldInclude(item, showHiddenFiles))
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool HasVisibleChildren(DirectoryInfo directory, bool showHiddenFiles)
    {
        try
        {
            return directory.EnumerateDirectories().Any(item => ShouldInclude(item, showHiddenFiles)) ||
                   directory.EnumerateFiles().Any(item => ShouldInclude(item, showHiddenFiles));
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool ShouldInclude(FileSystemInfo item, bool showHiddenFiles)
    {
        var attributes = item.Attributes;
        var isSystem = (attributes & FileAttributes.System) != 0;
        var isHidden = (attributes & FileAttributes.Hidden) != 0;

        if (isSystem)
        {
            return false;
        }

        return showHiddenFiles || !isHidden;
    }
}
