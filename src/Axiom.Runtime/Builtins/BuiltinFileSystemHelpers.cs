using System.Security;
using System.Text;
using Axiom.Core;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinFileSystemHelpers
{
    public static readonly Encoding FileSystemTextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    public static readonly Encoding StrictUtf8TextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    public static readonly Encoding Windows1251TextEncoding = CreateWindows1251TextEncoding();

    public static string NormalizeFileSystemPath(string path, string? baseDirectory = null)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);

        if (baseDirectory is not null && !Path.IsPathFullyQualified(expanded))
        {
            return Path.GetFullPath(Path.Combine(baseDirectory, expanded));
        }

        return Path.GetFullPath(expanded);
    }

    public static string ReadTextFileWithFallback(string fullPath)
    {
        var bytes = File.ReadAllBytes(fullPath);

        try
        {
            return StripUtf8Bom(StrictUtf8TextEncoding.GetString(bytes));
        }
        catch (DecoderFallbackException)
        {
            return Windows1251TextEncoding.GetString(bytes);
        }
    }

    public static string StripUtf8Bom(string text)
    {
        return text.Length > 0 && text[0] == '\xFEFF' ? text[1..] : text;
    }

    public static Encoding CreateWindows1251TextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251);
    }

    public static string[] ReadDecodedLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.Length > 0 && line[^1] == '\r')
                lines[i] = line[..^1];
        }

        if (lines.Length > 0 && lines[^1].Length == 0)
        {
            Array.Resize(ref lines, lines.Length - 1);
        }

        return lines;
    }

    public static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir, overwrite);
        }
    }

    public static long GetDirectorySize(string dirPath)
    {
        long size = 0;

        foreach (var file in Directory.GetFiles(dirPath))
        {
            checked { size += new FileInfo(file).Length; }
        }

        foreach (var dir in Directory.GetDirectories(dirPath))
        {
            checked { size += GetDirectorySize(dir); }
        }

        return size;
    }

    public static string CreateTemporaryFile(string prefix)
    {
        var tempPath = Path.GetTempPath();
        string filePath;
        do
        {
            filePath = Path.Combine(tempPath, prefix + Path.GetRandomFileName());
        }
        while (File.Exists(filePath));

        using (File.Create(filePath)) { }
        return filePath;
    }

    public static string CreateTemporaryDirectory(string prefix)
    {
        var tempPath = Path.GetTempPath();
        string dirPath;
        do
        {
            dirPath = Path.Combine(tempPath, prefix + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
        }
        while (Directory.Exists(dirPath));

        Directory.CreateDirectory(dirPath);
        return dirPath;
    }

    public static bool IsFileSystemException(Exception ex)
    {
        return ex is IOException
            or UnauthorizedAccessException
            or SecurityException
            or ArgumentException
            or PathTooLongException
            or DirectoryNotFoundException
            or FileNotFoundException;
    }

    public static bool EnsureFileExists(string funcName, string argName, string path, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        if (!File.Exists(path))
        {
            reportArgumentError(funcName, argName, $"файл не найден: '{path}'.", location);
            return false;
        }
        return true;
    }

    public static bool EnsureDirectoryExists(string funcName, string argName, string path, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        if (!Directory.Exists(path))
        {
            reportArgumentError(funcName, argName, $"каталог не найден: '{path}'.", location);
            return false;
        }
        return true;
    }

    public static void ReportFileSystemFailure(string funcName, string argName, Exception ex, TextLocation location,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        reportArgumentError(funcName, argName, ex.Message, location);
    }
}
