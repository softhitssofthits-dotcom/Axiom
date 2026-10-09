using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateFileExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(File.Exists(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FILEEXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(Directory.Exists(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIREXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            return RuntimeValue.FromBoolean(File.Exists(fullPath) || Directory.Exists(fullPath));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("PATHEXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadFileBuiltin(string path, TextLocation location, string? encodingName = null)
    {
        try
        {
            return RuntimeValue.FromString(encodingName is null ? BuiltinFileSystemHelpers.ReadTextFileWithFallback(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)) : BuiltinTextEncodingHelpers.Read(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory), encodingName));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("READFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadAllLinesBuiltin(string path, TextLocation location, string? encodingName = null)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            EnsureFileExists("READALLLINES", "path", fullPath, location);
            return CreateStringArrayValue(BuiltinFileSystemHelpers.ReadDecodedLines(encodingName is null ? BuiltinFileSystemHelpers.ReadTextFileWithFallback(fullPath) : BuiltinTextEncodingHelpers.Read(fullPath, encodingName)));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("READALLLINES", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateWriteFileBuiltin(string path, string text, TextLocation location, string? encodingName = null)
    {
        try
        {
            if (encodingName is null)
                File.WriteAllText(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            else
                BuiltinTextEncodingHelpers.Write(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory), text, encodingName, append: false);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("WRITEFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateAppendFileBuiltin(string path, string text, TextLocation location, string? encodingName = null)
    {
        try
        {
            if (encodingName is null)
                File.AppendAllText(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            else
                BuiltinTextEncodingHelpers.Write(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory), text, encodingName, append: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("APPENDFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileSizeBuiltin(string path, TextLocation location)
    {
        try
        {
            var file = new FileInfo(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory));
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            EnsureDirectoryExists("DIRDATE", "path", fullPath, location);
            return RuntimeValue.FromString(new DirectoryInfo(fullPath).LastWriteTime.ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRDATE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetFilesBuiltin(string path, string pattern, int patternArgumentIndex, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("GETFILES", patternArgumentIndex < 0 ? "path" : "pattern", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetDirsBuiltin(string path, string pattern, int patternArgumentIndex, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COMBINEPATH", "left", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetFileName(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FILENAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetDirectoryName(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)) ?? string.Empty);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRNAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExtNameBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetExtension(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("EXTNAME", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFullPathBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("FULLPATH", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathRootBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetPathRoot(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory)) ?? string.Empty);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("ISABSOLUTEPATH", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeDirBuiltin(string path, TextLocation location)
    {
        try
        {
            Directory.CreateDirectory(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory));
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteFileBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            if (!File.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            File.Delete(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DELETEFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyFileBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.CurrentDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.CurrentDirectory);
            EnsureFileExists("COPYFILE", "source", sourcePath, location);
            File.Copy(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COPYFILE", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveFileBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.CurrentDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.CurrentDirectory);
            EnsureFileExists("MOVEFILE", "source", sourcePath, location);
            File.Move(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MOVEFILE", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameFileBuiltin(string path, string newName, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("RENAMEFILE", "newName", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: false);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DELETEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirRecursiveBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DELETEDIRRECURSIVE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyDirBuiltin(string source, string destination, bool overwrite, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.CurrentDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.CurrentDirectory);
            EnsureDirectoryExists("COPYDIR", "source", sourcePath, location);
            BuiltinFileSystemHelpers.CopyDirectory(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("COPYDIR", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveDirBuiltin(string source, string destination, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.CurrentDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.CurrentDirectory);
            EnsureDirectoryExists("MOVEDIR", "source", sourcePath, location);
            Directory.Move(sourcePath, destinationPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MOVEDIR", "destination", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameDirBuiltin(string path, string newName, TextLocation location)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("RENAMEDIR", "newName", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCurrentDirBuiltin(TextLocation location)
    {
        try
        {
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.NormalizeFileSystemPath(_host.CurrentDirectory));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("CURRENTDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateChangeDirBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            EnsureDirectoryExists("CHANGEDIR", "path", fullPath, location);
            _host.ChangeCurrentDirectory(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("CHANGEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirSizeBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
            EnsureDirectoryExists("DIRSIZE", "path", fullPath, location);
            return RuntimeValue.FromLong(BuiltinFileSystemHelpers.GetDirectorySize(fullPath));
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRSIZE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFreeSpaceBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.CreateTemporaryFile(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.CreateTemporaryDirectory(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKETEMPDIR", "prefix", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExistingFileDate(string functionName, string path, Func<FileInfo, DateTime> dateSelector, TextLocation location)
    {
        try
        {
            var file = new FileInfo(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory));
            if (!file.Exists)
            {
                ReportFileSystemMissingFile(functionName, "path", location);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromString(dateSelector(file).ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(functionName, "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private static RuntimeValue CreateStringArrayValue(IReadOnlyList<string> values)
    {
        var arrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.String);
        return CreateCompactArrayValue(arrayType, values.Select(RuntimeValue.FromString).ToArray());
    }

    private void EnsureFileExists(string functionName, string argumentName, string path, TextLocation location)
    {
        if (!BuiltinFileSystemHelpers.EnsureFileExists(functionName, argumentName, path, location, ReportBuiltInArgumentError))
        {
            throw new RuntimeFailureException();
        }
    }

    private void EnsureDirectoryExists(string functionName, string argumentName, string path, TextLocation location)
    {
        if (!BuiltinFileSystemHelpers.EnsureDirectoryExists(functionName, argumentName, path, location, ReportBuiltInArgumentError))
        {
            throw new RuntimeFailureException();
        }
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
        var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);

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
        var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
        var rootPath = Path.GetPathRoot(fullPath);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            ReportBuiltInArgumentError(functionName, argumentName, "не указывает на диск или корень файловой системы.", location);
            throw new RuntimeFailureException();
        }

        return new DriveInfo(rootPath);
    }

    private void ReportFileSystemMissingFile(string functionName, string argumentName, TextLocation location)
    {
        ReportBuiltInArgumentError(functionName, argumentName, "указывает на файл, который не существует.", location);
    }

    private void ReportFileSystemFailure(string functionName, string argumentName, Exception exception, TextLocation location)
    {
        BuiltinFileSystemHelpers.ReportFileSystemFailure(functionName, argumentName, exception, location, ReportBuiltInArgumentError);
    }
}
