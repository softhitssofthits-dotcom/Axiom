using System.Globalization;
using System.Security;
using System.Text;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static readonly Encoding FileSystemTextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding StrictUtf8TextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Windows1251TextEncoding = CreateWindows1251TextEncoding();

    private RuntimeValue EvaluateFileExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(File.Exists(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FILEEXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(Directory.Exists(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIREXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            return RuntimeValue.FromBoolean(File.Exists(fullPath) || Directory.Exists(fullPath));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("PATHEXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadFileBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(ReadTextFileWithFallback(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("READFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadAllLinesBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureFileExists("READALLLINES", "path", fullPath, location);
            return CreateStringArrayValue(ReadDecodedLines(ReadTextFileWithFallback(fullPath)));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("READALLLINES", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateWriteFileBuiltin(string path, string text, TextLocation location)
    {
        try
        {
            File.WriteAllText(NormalizeFileSystemPath(path), text, FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("WRITEFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateAppendFileBuiltin(string path, string text, TextLocation location)
    {
        try
        {
            File.AppendAllText(NormalizeFileSystemPath(path), text, FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("APPENDFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileSizeBuiltin(string path, TextLocation location)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));
            if (!file.Exists)
            {
                ReportFileSystemMissingFile("FILESIZE", "path", location);
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
            ReportFileSystemFailure("FILESIZE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileDateBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileDate("FILEDATE", path, static file => file.LastWriteTime, location);
    }

    private RuntimeValue EvaluateFileCreateDateBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileDate("FILECREATEDATE", path, static file => file.CreationTime, location);
    }

    private RuntimeValue EvaluateFileAccessDateBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileDate("FILEACCESSEDDATE", path, static file => file.LastAccessTime, location);
    }

    private RuntimeValue EvaluateDirDateBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("DIRDATE", "path", fullPath, location);
            return RuntimeValue.FromString(new DirectoryInfo(fullPath).LastWriteTime.ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRDATE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetFilesBuiltin(string path, string pattern, int patternArgumentIndex, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("GETFILES", "path", fullPath, location);
            EnsureSearchPattern("GETFILES", pattern, location);

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
            ReportFileSystemFailure("GETFILES", patternArgumentIndex < 0 ? "path" : "pattern", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetDirsBuiltin(string path, string pattern, int patternArgumentIndex, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("GETDIRS", "path", fullPath, location);
            EnsureSearchPattern("GETDIRS", pattern, location);

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
            ReportFileSystemFailure("GETDIRS", patternArgumentIndex < 0 ? "path" : "pattern", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCombinePathBuiltin(string left, string right, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.Combine(left, right));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COMBINEPATH", "left", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetFileName(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FILENAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetDirectoryName(NormalizeFileSystemPath(path)) ?? string.Empty);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRNAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExtNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetExtension(NormalizeFileSystemPath(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("EXTNAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFullPathBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(NormalizeFileSystemPath(path));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FULLPATH", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathRootBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetPathRoot(NormalizeFileSystemPath(path)) ?? string.Empty);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("PATHROOT", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsAbsolutePathBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(Path.IsPathFullyQualified(Environment.ExpandEnvironmentVariables(path)));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("ISABSOLUTEPATH", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeDirBuiltin(string path, TextLocation location)
    {
        try
        {
            Directory.CreateDirectory(NormalizeFileSystemPath(path));
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteFileBuiltin(string path, TextLocation location)
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
            ReportFileSystemFailure("DELETEFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyFileBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);
            EnsureFileExists("COPYFILE", "source", sourcePath, location);
            File.Copy(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COPYFILE", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveFileBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);
            EnsureFileExists("MOVEFILE", "source", sourcePath, location);
            File.Move(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MOVEFILE", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameFileBuiltin(string path, string newName, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(path);
            EnsureFileExists("RENAMEFILE", "path", sourcePath, location);
            EnsureSimpleFileSystemName("RENAMEFILE", "newName", newName, location);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError("RENAMEFILE", "path", "не содержит родительскую папку.", location);
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
            ReportFileSystemFailure("RENAMEFILE", "newName", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirBuiltin(string path, TextLocation location)
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
            ReportFileSystemFailure("DELETEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirRecursiveBuiltin(string path, TextLocation location)
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
            ReportFileSystemFailure("DELETEDIRRECURSIVE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyDirBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);
            EnsureDirectoryExists("COPYDIR", "source", sourcePath, location);
            CopyDirectory(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COPYDIR", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveDirBuiltin(string source, string destination, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(source);
            var destinationPath = NormalizeFileSystemPath(destination);
            EnsureDirectoryExists("MOVEDIR", "source", sourcePath, location);
            Directory.Move(sourcePath, destinationPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MOVEDIR", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameDirBuiltin(string path, string newName, TextLocation location)
    {
        try
        {
            var sourcePath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("RENAMEDIR", "path", sourcePath, location);
            EnsureSimpleFileSystemName("RENAMEDIR", "newName", newName, location);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError("RENAMEDIR", "path", "не содержит родительскую папку.", location);
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
            ReportFileSystemFailure("RENAMEDIR", "newName", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCurrentDirBuiltin(TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(NormalizeFileSystemPath(Environment.CurrentDirectory));
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("CURRENTDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateChangeDirBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("CHANGEDIR", "path", fullPath, location);
            Directory.SetCurrentDirectory(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("CHANGEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirSizeBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            EnsureDirectoryExists("DIRSIZE", "path", fullPath, location);
            return RuntimeValue.FromLong(GetDirectorySize(fullPath));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (OverflowException)
        {
            ReportNumericOverflow("Суммарный размер папки в функции DIRSIZE", TypeSymbol.Long, location);
            throw new RuntimeFailureException();
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRSIZE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFreeSpaceBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemPath(path);
            var rootPath = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                ReportBuiltInArgumentError("FREESPACE", "path", "не указывает на диск или корень файловой системы.", location);
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
            ReportFileSystemFailure("FREESPACE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateTotalFreeSpaceBuiltin(string path, TextLocation location)
    {
        return EvaluateDriveLong("TOTALFREESPACE", path, static drive => drive.TotalFreeSpace, location);
    }

    private RuntimeValue EvaluateTotalSpaceBuiltin(string path, TextLocation location)
    {
        return EvaluateDriveLong("TOTALSPACE", path, static drive => drive.TotalSize, location);
    }

    private RuntimeValue EvaluateIsReadOnlyBuiltin(string path, TextLocation location)
    {
        try
        {
            var info = GetExistingFileSystemInfo("ISREADONLY", "path", path, location);
            return RuntimeValue.FromBoolean((info.Attributes & FileAttributes.ReadOnly) != 0);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("ISREADONLY", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateIsHiddenBuiltin(string path, TextLocation location)
    {
        try
        {
            var info = GetExistingFileSystemInfo("ISHIDDEN", "path", path, location);
            return RuntimeValue.FromBoolean((info.Attributes & FileAttributes.Hidden) != 0);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("ISHIDDEN", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveNameBuiltin(string path, TextLocation location)
    {
        return EvaluateDriveString("DRIVENAME", path, static drive => drive.Name, location);
    }

    private RuntimeValue EvaluateDriveFormatBuiltin(string path, TextLocation location)
    {
        return EvaluateDriveString("DRIVEFORMAT", path, static drive => drive.DriveFormat, location);
    }

    private RuntimeValue EvaluateDriveTypeBuiltin(string path, TextLocation location)
    {
        return EvaluateDriveString("DRIVETYPE", path, static drive => drive.DriveType.ToString(), location);
    }

    private RuntimeValue EvaluateMakeTempFileBuiltin(string prefix, TextLocation location)
    {
        try
        {
            EnsureSimpleTempPrefix("MAKETEMPFILE", prefix, location);
            return RuntimeValue.FromString(CreateTemporaryFile(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKETEMPFILE", "prefix", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeTempDirBuiltin(string prefix, TextLocation location)
    {
        try
        {
            EnsureSimpleTempPrefix("MAKETEMPDIR", prefix, location);
            return RuntimeValue.FromString(CreateTemporaryDirectory(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKETEMPDIR", "prefix", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExistingFileDate(string functionName, string path, Func<FileInfo, DateTime> dateSelector, TextLocation location)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemPath(path));
            if (!file.Exists)
            {
                ReportFileSystemMissingFile(functionName, "path", location);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromString(dateSelector(file).ToString(CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(functionName, "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveLong(string functionName, string path, Func<DriveInfo, long> selector, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromLong(selector(GetDriveInfo(functionName, "path", path, location)));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(functionName, "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDriveString(string functionName, string path, Func<DriveInfo, string> selector, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(selector(GetDriveInfo(functionName, "path", path, location)));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportFileSystemFailure(functionName, "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private static string NormalizeFileSystemPath(string path)
    {
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
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

    private static RuntimeValue CreateStringArrayValue(IReadOnlyList<string> values)
    {
        var arrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.String);
        return CreateCompactArrayValue(arrayType, values.Select(RuntimeValue.FromString).ToArray());
    }

    private void EnsureFileExists(string functionName, string argumentName, string path, TextLocation location)
    {
        if (File.Exists(path))
        {
            return;
        }

        ReportFileSystemMissingFile(functionName, argumentName, location);
        throw new RuntimeFailureException();
    }

    private void EnsureDirectoryExists(string functionName, string argumentName, string path, TextLocation location)
    {
        if (Directory.Exists(path))
        {
            return;
        }

        ReportBuiltInArgumentError(functionName, argumentName, "указывает на папку, которая не существует.", location);
        throw new RuntimeFailureException();
    }

    private void EnsureSearchPattern(string functionName, string pattern, TextLocation location)
    {
        if (pattern.Length > 0)
        {
            return;
        }

        ReportBuiltInArgumentError(functionName, "pattern", "не может быть пустой строкой.", location);
        throw new RuntimeFailureException();
    }

    private void EnsureSimpleFileSystemName(string functionName, string argumentName, string value, TextLocation location)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && value == Path.GetFileName(value)
            && value is not "." and not "..")
        {
            return;
        }

        ReportBuiltInArgumentError(functionName, argumentName, "должен быть простым именем без пути.", location);
        throw new RuntimeFailureException();
    }

    private void EnsureSimpleTempPrefix(string functionName, string prefix, TextLocation location)
    {
        if (prefix == Path.GetFileName(prefix)
            && prefix is not "." and not ".."
            && prefix.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
        {
            return;
        }

        ReportBuiltInArgumentError(functionName, "prefix", "должен быть простым префиксом имени без пути.", location);
        throw new RuntimeFailureException();
    }

    private FileSystemInfo GetExistingFileSystemInfo(string functionName, string argumentName, string path, TextLocation location)
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

        ReportBuiltInArgumentError(functionName, argumentName, "указывает на файл или папку, которые не существуют.", location);
        throw new RuntimeFailureException();
    }

    private DriveInfo GetDriveInfo(string functionName, string argumentName, string path, TextLocation location)
    {
        var fullPath = NormalizeFileSystemPath(path);
        var rootPath = Path.GetPathRoot(fullPath);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            ReportBuiltInArgumentError(functionName, argumentName, "не указывает на диск или корень файловой системы.", location);
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

    private void ReportFileSystemMissingFile(string functionName, string argumentName, TextLocation location)
    {
        ReportBuiltInArgumentError(functionName, argumentName, "указывает на файл, который не существует.", location);
    }

    private void ReportFileSystemFailure(string functionName, string argumentName, Exception exception, TextLocation location)
    {
        ReportBuiltInArgumentError(functionName, argumentName, $"не может быть обработан файловой системой: {exception.Message}", location);
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
