using System.IO;

namespace Axiom.Ide.Models;

public sealed record RecentFileItem(string FilePath, string FileName, string DirectoryPath)
{
    public static RecentFileItem FromPath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return new RecentFileItem(
            filePath,
            Path.GetFileName(filePath),
            Path.GetDirectoryName(filePath) ?? string.Empty);
    }
}
