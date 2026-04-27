using System.Globalization;
using System.Security;
using System.Text;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private static readonly Encoding FileSystemTextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding StrictUtf8TextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Windows1251TextEncoding = CreateWindows1251TextEncoding();

    private RuntimeValue EvaluateFileExists(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromBoolean(File.Exists(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEEXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirExists(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromBoolean(Directory.Exists(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIREXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathExists(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            return RuntimeValue.FromBoolean(File.Exists(fullPath) || Directory.Exists(fullPath));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "PATHEXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadFile(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(ReadTextFileWithFallback(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "READFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadAllLines(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureFileExists(expression, "READALLLINES", "path", 0, fullPath);

            var lines = ReadDecodedLines(ReadTextFileWithFallback(fullPath));
            return CreateStringArrayValue(lines);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "READALLLINES", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private static string ReadTextFileWithFallback(string fullPath)
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

    private static string StripUtf8Bom(string text)
    {
        return text.Length > 0 && text[0] == '\uFEFF'
            ? text[1..]
            : text;
    }

    private static Encoding CreateWindows1251TextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(1251);
    }

    private static string[] ReadDecodedLines(string text)
    {
        using var reader = new StringReader(text);
        var lines = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return [.. lines];
    }

    private RuntimeValue EvaluateWriteFile(BoundCallExpression expression, string path, string text)
    {
        try
        {
            File.WriteAllText(NormalizeFileSystemPath(path), text, FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "WRITEFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateAppendFile(BoundCallExpression expression, string path, string text)
    {
        try
        {
            File.AppendAllText(NormalizeFileSystemPath(path), text, FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "APPENDFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileSize(BoundCallExpression expression, string path)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));

            if (!file.Exists)
            {
                ReportFileSystemMissingFile(expression, "FILESIZE", "path", 0);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromLong(file.Length);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILESIZE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileDate(BoundCallExpression expression, string path)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));

            if (!file.Exists)
            {
                ReportFileSystemMissingFile(expression, "FILEDATE", "path", 0);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromString(file.LastWriteTime.ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileCreateDate(BoundCallExpression expression, string path)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));

            if (!file.Exists)
            {
                ReportFileSystemMissingFile(expression, "FILECREATEDATE", "path", 0);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromString(file.CreationTime.ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILECREATEDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileAccessDate(BoundCallExpression expression, string path)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));

            if (!file.Exists)
            {
                ReportFileSystemMissingFile(expression, "FILEACCESSEDDATE", "path", 0);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromString(file.LastAccessTime.ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEACCESSEDDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirDate(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "DIRDATE", "path", 0, fullPath);
            return RuntimeValue.FromString(new DirectoryInfo(fullPath).LastWriteTime.ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIRDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetFiles(
        BoundCallExpression expression,
        string path,
        string pattern,
        int patternArgumentIndex)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "GETFILES", "path", 0, fullPath);
            EnsureSearchPattern(expression, "GETFILES", pattern, patternArgumentIndex);

            var files = Directory.EnumerateFiles(fullPath, pattern)
                .OrderBy(static filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return CreateStringArrayValue(files);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "GETFILES", patternArgumentIndex < 0 ? "path" : "pattern", patternArgumentIndex < 0 ? 0 : patternArgumentIndex, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetDirs(
        BoundCallExpression expression,
        string path,
        string pattern,
        int patternArgumentIndex)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "GETDIRS", "path", 0, fullPath);
            EnsureSearchPattern(expression, "GETDIRS", pattern, patternArgumentIndex);

            var directories = Directory.EnumerateDirectories(fullPath, pattern)
                .OrderBy(static directoryPath => directoryPath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return CreateStringArrayValue(directories);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "GETDIRS", patternArgumentIndex < 0 ? "path" : "pattern", patternArgumentIndex < 0 ? 0 : patternArgumentIndex, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCombinePath(BoundCallExpression expression, string left, string right)
    {
        try
        {
            return RuntimeValue.FromString(Path.Combine(left, right));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COMBINEPATH", "left", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetFileName(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILENAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetDirectoryName(NormalizeFileSystemPath(path)) ?? string.Empty);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIRNAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExtName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetExtension(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "EXTNAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFullPath(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(NormalizeFileSystemPath(path));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FULLPATH", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathRoot(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetPathRoot(NormalizeFileSystemPath(path)) ?? string.Empty);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "PATHROOT", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsAbsolutePath(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromBoolean(Path.IsPathFullyQualified(Environment.ExpandEnvironmentVariables(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "ISABSOLUTEPATH", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeDir(BoundCallExpression expression, string path)
    {
        try
        {
            Directory.CreateDirectory(NormalizeFileSystemPath(path));
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MAKEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteFile(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);

            if (!File.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            File.Delete(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyFile(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);

            EnsureFileExists(expression, "COPYFILE", "source", 0, sourcePath);
            File.Copy(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COPYFILE", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveFile(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);

            EnsureFileExists(expression, "MOVEFILE", "source", 0, sourcePath);
            File.Move(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MOVEFILE", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameFile(BoundCallExpression expression, string path, string newName)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(path);
            EnsureFileExists(expression, "RENAMEFILE", "path", 0, sourcePath);
            EnsureSimpleFileSystemName(expression, "RENAMEFILE", "newName", 1, newName);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError(
                    "RENAMEFILE",
                    "path",
                    "не содержит родительскую папку.",
                    GetBuiltInArgumentLocation(expression, 0));
                throw new RuntimeFailureException();
            }

            File.Move(sourcePath, Path.Combine(parent, newName));
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "RENAMEFILE", "newName", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDir(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);

            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: false);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirRecursive(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);

            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEDIRRECURSIVE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyDir(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);

            EnsureDirectoryExists(expression, "COPYDIR", "source", 0, sourcePath);
            CopyDirectory(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COPYDIR", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveDir(BoundCallExpression expression, string source, string destination)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);

            EnsureDirectoryExists(expression, "MOVEDIR", "source", 0, sourcePath);
            Directory.Move(sourcePath, destinationPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MOVEDIR", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameDir(BoundCallExpression expression, string path, string newName)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "RENAMEDIR", "path", 0, sourcePath);
            EnsureSimpleFileSystemName(expression, "RENAMEDIR", "newName", 1, newName);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError(
                    "RENAMEDIR",
                    "path",
                    "не содержит родительскую папку.",
                    GetBuiltInArgumentLocation(expression, 0));
                throw new RuntimeFailureException();
            }

            Directory.Move(sourcePath, Path.Combine(parent, newName));
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "RENAMEDIR", "newName", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCurrentDir(BoundCallExpression expression)
    {
        try
        {
            return RuntimeValue.FromString(NormalizeFileSystemPath(Environment.CurrentDirectory));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "CURRENTDIR", "path", -1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateChangeDir(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "CHANGEDIR", "path", 0, fullPath);
            Directory.SetCurrentDirectory(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "CHANGEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirSize(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists(expression, "DIRSIZE", "path", 0, fullPath);
            return RuntimeValue.FromLong(GetDirectorySize(fullPath));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (OverflowException)
        {
            ReportNumericOverflow("Суммарный размер папки в функции DIRSIZE", TypeSymbol.Long, expression.Location);
            throw new RuntimeFailureException();
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIRSIZE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFreeSpace(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            var rootPath = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                ReportBuiltInArgumentError(
                    "FREESPACE",
                    "path",
                    "не указывает на диск или корень файловой системы.",
                    GetBuiltInArgumentLocation(expression, 0));
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromLong(new DriveInfo(rootPath).AvailableFreeSpace);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FREESPACE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTotalFreeSpace(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromLong(GetDriveInfo(expression, "TOTALFREESPACE", "path", 0, path).TotalFreeSpace);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "TOTALFREESPACE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTotalSpace(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromLong(GetDriveInfo(expression, "TOTALSPACE", "path", 0, path).TotalSize);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "TOTALSPACE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsReadOnly(BoundCallExpression expression, string path)
    {
        try
        {
            var info = GetExistingFileSystemInfo(expression, "ISREADONLY", "path", 0, path);
            return RuntimeValue.FromBoolean((info.Attributes & FileAttributes.ReadOnly) != 0);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "ISREADONLY", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsHidden(BoundCallExpression expression, string path)
    {
        try
        {
            var info = GetExistingFileSystemInfo(expression, "ISHIDDEN", "path", 0, path);
            return RuntimeValue.FromBoolean((info.Attributes & FileAttributes.Hidden) != 0);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "ISHIDDEN", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(GetDriveInfo(expression, "DRIVENAME", "path", 0, path).Name);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DRIVENAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveFormat(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(GetDriveInfo(expression, "DRIVEFORMAT", "path", 0, path).DriveFormat);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DRIVEFORMAT", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveType(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(GetDriveInfo(expression, "DRIVETYPE", "path", 0, path).DriveType.ToString());
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DRIVETYPE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeTempFile(BoundCallExpression expression, string prefix)
    {
        try
        {
            EnsureSimpleTempPrefix(expression, "MAKETEMPFILE", 0, prefix);
            return RuntimeValue.FromString(CreateTemporaryFile(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MAKETEMPFILE", "prefix", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeTempDir(BoundCallExpression expression, string prefix)
    {
        try
        {
            EnsureSimpleTempPrefix(expression, "MAKETEMPDIR", 0, prefix);
            return RuntimeValue.FromString(CreateTemporaryDirectory(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MAKETEMPDIR", "prefix", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private static string NormalizeFileSystemPath(string path)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
    }

    private void EnsureFileExists(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        if (File.Exists(path))
        {
            return;
        }

        ReportFileSystemMissingFile(expression, functionName, argumentName, argumentIndex);
        throw new RuntimeFailureException();
    }

    private void EnsureDirectoryExists(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        if (Directory.Exists(path))
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "указывает на папку, которая не существует.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private void EnsureSearchPattern(
        BoundCallExpression expression,
        string functionName,
        string pattern,
        int argumentIndex)
    {
        if (pattern.Length > 0)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            "pattern",
            "не может быть пустой строкой.",
            GetBuiltInArgumentLocation(expression, argumentIndex < 0 ? 0 : argumentIndex));
        throw new RuntimeFailureException();
    }

    private void EnsureSimpleFileSystemName(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value == Path.GetFileName(value)
            && value is not "." and not "..")
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "должен быть простым именем без пути.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private void EnsureSimpleTempPrefix(
        BoundCallExpression expression,
        string functionName,
        int argumentIndex,
        string prefix)
    {
        if (prefix == Path.GetFileName(prefix)
            && prefix is not "." and not ".."
            && prefix.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
        {
            return;
        }

        ReportBuiltInArgumentError(
            functionName,
            "prefix",
            "должен быть простым префиксом имени без пути.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private FileSystemInfo GetExistingFileSystemInfo(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        var fullPath = NormalizeFileSystemPath(path);

        if (File.Exists(fullPath))
        {
            return new FileInfo(fullPath);
        }

        if (Directory.Exists(fullPath))
        {
            return new DirectoryInfo(fullPath);
        }

        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "указывает на файл или папку, которые не существуют.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
        throw new RuntimeFailureException();
    }

    private DriveInfo GetDriveInfo(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        var fullPath = NormalizeFileSystemPath(path);
        var rootPath = Path.GetPathRoot(fullPath);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "не указывает на диск или корень файловой системы.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return new DriveInfo(rootPath);
    }

    private static void CopyDirectory(string sourcePath, string destinationPath, bool overwrite)
    {
        Directory.CreateDirectory(destinationPath);

        foreach (var filePath in Directory.EnumerateFiles(sourcePath))
        {
            var targetPath = Path.Combine(destinationPath, Path.GetFileName(filePath));
            File.Copy(filePath, targetPath, overwrite);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(sourcePath))
        {
            var targetPath = Path.Combine(destinationPath, Path.GetFileName(directoryPath));
            CopyDirectory(directoryPath, targetPath, overwrite);
        }
    }

    private static long GetDirectorySize(string path)
    {
        long totalSize = 0;

        foreach (var filePath in Directory.EnumerateFiles(path))
        {
            totalSize = checked(totalSize + new FileInfo(filePath).Length);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(path))
        {
            var attributes = File.GetAttributes(directoryPath);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                continue;
            }

            totalSize = checked(totalSize + GetDirectorySize(directoryPath));
        }

        return totalSize;
    }

    private static string CreateTemporaryFile(string prefix)
    {
        var tempRoot = NormalizeFileSystemPath(Path.GetTempPath());
        Directory.CreateDirectory(tempRoot);

        while (true)
        {
            var fullPath = Path.Combine(tempRoot, prefix + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return fullPath;
            }
            catch (IOException) when (File.Exists(fullPath))
            {
            }
        }
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var tempRoot = NormalizeFileSystemPath(Path.GetTempPath());
        Directory.CreateDirectory(tempRoot);

        while (true)
        {
            var fullPath = Path.Combine(tempRoot, prefix + Guid.NewGuid().ToString("N"));
            if (Directory.Exists(fullPath) || File.Exists(fullPath))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(fullPath);
                return fullPath;
            }
            catch (IOException) when (Directory.Exists(fullPath))
            {
            }
        }
    }

    private void ReportFileSystemMissingFile(BoundCallExpression expression, string functionName, string argumentName, int argumentIndex)
    {
        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            "указывает на файл, который не существует.",
            GetBuiltInArgumentLocation(expression, argumentIndex));
    }

    private void ReportFileSystemFailure(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        Exception exception)
    {
        ReportBuiltInArgumentError(
            functionName,
            argumentName,
            $"не может быть обработан файловой системой: {exception.Message}",
            GetBuiltInArgumentLocation(expression, argumentIndex));
    }

    private static bool IsFileSystemException(Exception exception)
    {
        return exception is ArgumentException
            or IOException
            or NotSupportedException
            or UnauthorizedAccessException
            or SecurityException;
    }
}
