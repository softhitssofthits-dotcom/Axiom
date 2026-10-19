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
        // Раскрытие переменных окружения Windows может обрезать строку по NUL.
        // Проверяем исходный путь до обращения к этому API.
        if (path.IndexOf('\0') >= 0)
            throw new ArgumentException("Путь не должен содержать нулевой символ.", nameof(path));
        var expanded = Environment.ExpandEnvironmentVariables(path);

        if (baseDirectory is not null && !Path.IsPathFullyQualified(expanded))
        {
            return Path.GetFullPath(expanded, baseDirectory);
        }

        return Path.GetFullPath(expanded);
    }

    public static string ReadTextFileWithFallback(string fullPath, Action? checkCancellation = null)
    {
        var bytes = RuntimeStringLimits.ReadBytes(fullPath, checkCancellation);

        try
        {
            var start = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
            return RuntimeStringLimits.Decode(StrictUtf8TextEncoding, bytes, start);
        }
        catch (DecoderFallbackException)
        {
            return RuntimeStringLimits.Decode(Windows1251TextEncoding, bytes);
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

    public static string[] ReadDecodedLines(string text, Action? checkCancellation = null)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var lines = new List<string>();
        using var reader = new StringReader(text);
        while (true)
        {
            checkCancellation?.Invoke();
            if (reader.ReadLine() is not { } line) break;
            lines.Add(line);
        }
        return lines.ToArray();
    }

    public static void CopyDirectory(string sourceDir, string destDir, bool overwrite, Action? checkCancellation = null)
    {
        BuiltinDirectoryHelpers.CopyDirectory(sourceDir, destDir, overwrite, checkCancellation);
    }

    public static long GetDirectorySize(string dirPath, Action? checkCancellation = null)
    {
        return BuiltinDirectoryHelpers.GetDirectorySize(dirPath, checkCancellation);
    }

    public static string CreateTemporaryFile(string prefix, Func<string>? nextPath = null)
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var path = nextPath?.Invoke() ?? Path.Combine(Path.GetTempPath(), prefix + Path.GetRandomFileName());
            try
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return path;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException)
                && (File.Exists(path) || Directory.Exists(path)))
            {
                // Занятое имя не даёт права обнулить существующий объект.
            }
        }
        throw new IOException("Не удалось выбрать свободное имя временного файла за 128 попыток.");
    }

    public static string CreateTemporaryDirectory(string prefix)
    {
        return Directory.CreateTempSubdirectory(prefix).FullName;
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
