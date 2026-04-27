using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace Axiom.Ide.Files;

public interface IFileOperationsService
{
    string CreateEmptyFile(string directoryPath, string fileName);
    string CreateDirectory(string directoryPath, string directoryName);
    string Rename(string sourcePath, string newName);
    string CopyToDirectory(string sourcePath, string destinationDirectoryPath);
    string MoveToDirectory(string sourcePath, string destinationDirectoryPath);
    void SendToRecycleBin(string path);
    bool Exists(string path);
}

public sealed class FileOperationsService : IFileOperationsService
{
    public string CreateEmptyFile(string directoryPath, string fileName)
    {
        ValidateExistingDirectory(directoryPath);
        var targetPath = BuildTargetPath(directoryPath, fileName);

        using var stream = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        return targetPath;
    }

    public string CreateDirectory(string directoryPath, string directoryName)
    {
        ValidateExistingDirectory(directoryPath);
        var targetPath = BuildTargetPath(directoryPath, directoryName);
        Directory.CreateDirectory(targetPath);
        return targetPath;
    }

    public string Rename(string sourcePath, string newName)
    {
        var currentPath = NormalizeExistingPath(sourcePath);
        var parentDirectoryPath = Path.GetDirectoryName(currentPath)
            ?? throw new InvalidOperationException("Cannot rename the selected item because its parent directory is unavailable.");
        var targetPath = BuildTargetPath(parentDirectoryPath, newName);

        if (PathsEqual(currentPath, targetPath))
        {
            throw new IOException("The new name matches the current name.");
        }

        if (Directory.Exists(currentPath))
        {
            Directory.Move(currentPath, targetPath);
        }
        else
        {
            File.Move(currentPath, targetPath);
        }

        return targetPath;
    }

    public string CopyToDirectory(string sourcePath, string destinationDirectoryPath)
    {
        var currentPath = NormalizeExistingPath(sourcePath);
        ValidateExistingDirectory(destinationDirectoryPath);

        if (Directory.Exists(currentPath) && IsSameOrDescendantPath(destinationDirectoryPath, currentPath))
        {
            throw new IOException("Cannot copy a folder into itself or one of its child folders.");
        }

        var targetPath = BuildTargetPath(destinationDirectoryPath, Path.GetFileName(currentPath));

        if (Directory.Exists(currentPath))
        {
            CopyDirectoryRecursive(currentPath, targetPath);
        }
        else
        {
            File.Copy(currentPath, targetPath);
        }

        return targetPath;
    }

    public string MoveToDirectory(string sourcePath, string destinationDirectoryPath)
    {
        var currentPath = NormalizeExistingPath(sourcePath);
        ValidateExistingDirectory(destinationDirectoryPath);

        if (Directory.Exists(currentPath) && IsSameOrDescendantPath(destinationDirectoryPath, currentPath))
        {
            throw new IOException("Cannot move a folder into itself or one of its child folders.");
        }

        var targetPath = BuildTargetPath(destinationDirectoryPath, Path.GetFileName(currentPath));

        if (PathsEqual(currentPath, targetPath))
        {
            throw new IOException("The destination already matches the selected item location.");
        }

        if (Directory.Exists(currentPath))
        {
            MoveDirectory(currentPath, targetPath);
        }
        else
        {
            File.Move(currentPath, targetPath);
        }

        return targetPath;
    }

    public void SendToRecycleBin(string path)
    {
        var currentPath = NormalizeExistingPath(path);

        if (Directory.Exists(currentPath))
        {
            FileSystem.DeleteDirectory(
                currentPath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);
            return;
        }

        FileSystem.DeleteFile(
            currentPath,
            UIOption.OnlyErrorDialogs,
            RecycleOption.SendToRecycleBin,
            UICancelOption.ThrowException);
    }

    public bool Exists(string path) =>
        File.Exists(path) || Directory.Exists(path);

    private static void MoveDirectory(string sourcePath, string targetPath)
    {
        try
        {
            Directory.Move(sourcePath, targetPath);
        }
        catch (IOException)
        {
            CopyDirectoryRecursive(sourcePath, targetPath);
            Directory.Delete(sourcePath, recursive: true);
        }
    }

    private static void CopyDirectoryRecursive(string sourcePath, string targetPath)
    {
        if (Directory.Exists(targetPath) || File.Exists(targetPath))
        {
            throw new IOException($"'{Path.GetFileName(targetPath)}' already exists in the destination.");
        }

        Directory.CreateDirectory(targetPath);

        foreach (var directoryPath in Directory.GetDirectories(sourcePath))
        {
            var childTargetPath = Path.Combine(targetPath, Path.GetFileName(directoryPath));
            CopyDirectoryRecursive(directoryPath, childTargetPath);
        }

        foreach (var filePath in Directory.GetFiles(sourcePath))
        {
            var childTargetPath = Path.Combine(targetPath, Path.GetFileName(filePath));
            File.Copy(filePath, childTargetPath);
        }
    }

    private static string BuildTargetPath(string directoryPath, string name)
    {
        var normalizedName = NormalizeName(name);
        var targetPath = Path.Combine(directoryPath, normalizedName);

        if (File.Exists(targetPath) || Directory.Exists(targetPath))
        {
            throw new IOException($"'{normalizedName}' already exists.");
        }

        return targetPath;
    }

    private static string NormalizeExistingPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new FileNotFoundException("The selected file system item was not found.", fullPath);
        }

        return fullPath;
    }

    private static void ValidateExistingDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmedName = name.Trim();
        if (trimmedName.Length == 0)
        {
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        }

        if (trimmedName is "." or "..")
        {
            throw new ArgumentException("Name is not valid.", nameof(name));
        }

        if (trimmedName.Contains(Path.DirectorySeparatorChar)
            || trimmedName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("Name cannot contain path separators.", nameof(name));
        }

        if (trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Name contains invalid characters.", nameof(name));
        }

        return trimmedName;
    }

    private static bool IsSameOrDescendantPath(string candidatePath, string parentPath)
    {
        var normalizedCandidate = Path.GetFullPath(candidatePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedParent = Path.GetFullPath(parentPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalizedCandidate.StartsWith(
            normalizedParent + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedCandidate, normalizedParent, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsEqual(string leftPath, string rightPath) =>
        string.Equals(
            Path.GetFullPath(leftPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(rightPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
