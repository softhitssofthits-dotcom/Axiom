using System.Text;

namespace Axiom.Execution.Tests;

internal static class TestProgramLoader
{
    public static string LoadText(string relativePath)
    {
        return File.ReadAllText(GetPath(relativePath), Encoding.UTF8);
    }

    public static IReadOnlyList<string> LoadLines(string relativePath)
    {
        return File.ReadAllLines(GetPath(relativePath), Encoding.UTF8);
    }

    private static string GetPath(string relativePath)
    {
        return Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Axiom.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found from the current test output directory.");
    }
}
