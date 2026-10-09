namespace Axiom.Runtime.Builtins;

/// <summary>Обход обычных файлов и каталогов без перехода по вложенным ссылкам.</summary>
internal static class BuiltinDirectoryHelpers
{
    public static long GetDirectorySize(string directory)
    {
        long size = 0;
        var pending = new Stack<string>();
        pending.Push(directory);
        while (pending.TryPop(out var current))
        {
            foreach (var entry in Directory.EnumerateFileSystemEntries(current))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                if ((attributes & FileAttributes.Directory) != 0)
                    pending.Push(entry);
                else
                    size = checked(size + new FileInfo(entry).Length);
            }
        }
        return size;
    }

    public static void CopyDirectory(string source, string destination, bool overwrite)
    {
        var sourcePath = ResolveDirectoryPath(source);
        var destinationPath = ResolveDirectoryPath(destination);
        var comparableSource = ComparableDirectoryPath(sourcePath);
        var comparableDestination = ComparableDirectoryPath(destinationPath);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var sourcePrefix = Path.EndsInDirectorySeparator(comparableSource) ? comparableSource : comparableSource + Path.DirectorySeparatorChar;
        if (comparableDestination.Equals(comparableSource, comparison) || comparableDestination.StartsWith(sourcePrefix, comparison))
            throw new ArgumentException("Папка назначения не может совпадать с исходной папкой или находиться внутри исходной папки.");

        var pending = new Stack<(string Source, string Destination)>();
        pending.Push((sourcePath, destinationPath));
        while (pending.TryPop(out var pair))
        {
            RejectDestinationLink(pair.Destination);
            Directory.CreateDirectory(pair.Destination);
            foreach (var entry in Directory.EnumerateFileSystemEntries(pair.Source))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                var target = Path.Combine(pair.Destination, Path.GetFileName(entry));
                if ((attributes & FileAttributes.Directory) != 0)
                    pending.Push((entry, target));
                else
                {
                    RejectDestinationLink(target);
                    File.Copy(entry, target, overwrite);
                }
            }
        }
    }

    private static void RejectDestinationLink(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path))
            && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new ArgumentException("Путь назначения внутри копируемой папки не должен быть ссылкой.");
    }

    private static string ResolveDirectoryPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var root = Path.GetPathRoot(fullPath)!;
        var current = root;
        foreach (var part in fullPath[root.Length..].Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (!Directory.Exists(current)) continue;
            var info = new DirectoryInfo(current);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0)
                current = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                    ?? throw new IOException("Не удалось определить конечный путь ссылки на папку.");
        }
        return Path.TrimEndingDirectorySeparator(current);
    }

    private static string ComparableDirectoryPath(string path)
    {
        // Обычный и расширенный путь Windows должны сравниваться одинаково.
        // Иначе \\?\C:\source\child обходит проверку для C:\source.
        // Префикс сохраняется в пути, по которому выполняется сама запись.
        if (OperatingSystem.IsWindows())
        {
            if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                path = @"\\" + path[8..];
            else if (path.StartsWith(@"\\?\", StringComparison.Ordinal)
                     && path.Length >= 7 && char.IsAsciiLetter(path[4]) && path[5] == ':' && path[6] == '\\')
                path = path[4..];
        }
        return path;
    }
}
