using System.IO;

namespace Axiom.Ide.Services.Persistence;

public static class IdeWorkspaceDefaults
{
    public const string RootPath = @"C:\Axiom";

    public static string ResolveRootPath(string? preferredPath)
    {
        if (!string.IsNullOrWhiteSpace(preferredPath))
        {
            return preferredPath;
        }

        Directory.CreateDirectory(RootPath);
        return RootPath;
    }
}
