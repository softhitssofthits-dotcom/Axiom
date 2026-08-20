using System.Globalization;
using Axiom.Semantics;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private RuntimeValue EvaluateFileExists(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromBoolean(File.Exists(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEEXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirExists(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromBoolean(Directory.Exists(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIREXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathExists(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            return RuntimeValue.FromBoolean(File.Exists(fullPath) || Directory.Exists(fullPath));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "PATHEXISTS", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadFile(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.ReadTextFileWithFallback(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "READFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadAllLines(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "READALLLINES", "path", 0, fullPath);

            var lines = BuiltinFileSystemHelpers.ReadDecodedLines(BuiltinFileSystemHelpers.ReadTextFileWithFallback(fullPath));
            return CreateStringArrayValue(lines);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "READALLLINES", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateWriteFile(BoundCallExpression expression, string path, string text)
    {
        try
        {
            File.WriteAllText(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "WRITEFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateAppendFile(BoundCallExpression expression, string path, string text)
    {
        try
        {
            File.AppendAllText(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "APPENDFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileSize(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "FILESIZE", "path", 0, fullPath);
            return RuntimeValue.FromLong(new FileInfo(fullPath).Length);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILESIZE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileDate(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "FILEDATE", "path", 0, fullPath);
            return RuntimeValue.FromString(new FileInfo(fullPath).LastWriteTime.ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileCreateDate(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "FILECREATEDATE", "path", 0, fullPath);
            return RuntimeValue.FromString(new FileInfo(fullPath).CreationTime.ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILECREATEDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileAccessDate(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "FILEACCESSEDDATE", "path", 0, fullPath);
            return RuntimeValue.FromString(new FileInfo(fullPath).LastAccessTime.ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILEACCESSEDDATE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirDate(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureDirectoryExists(expression, "DIRDATE", "path", 0, fullPath);
            return RuntimeValue.FromString(new DirectoryInfo(fullPath).LastWriteTime.ToString(BuiltinDateTimeHelpers.CanonicalDateTimeFormat, CultureInfo.InvariantCulture));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COMBINEPATH", "left", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFileName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetFileName(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FILENAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetDirectoryName(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)) ?? string.Empty);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIRNAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExtName(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetExtension(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "EXTNAME", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFullPath(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "FULLPATH", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluatePathRoot(BoundCallExpression expression, string path)
    {
        try
        {
            return RuntimeValue.FromString(Path.GetPathRoot(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory)) ?? string.Empty);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "ISABSOLUTEPATH", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMakeDir(BoundCallExpression expression, string path)
    {
        try
        {
            Directory.CreateDirectory(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory));
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MAKEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteFile(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);

            if (!File.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            File.Delete(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEFILE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyFile(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.ResourceBaseDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.ResourceBaseDirectory);

            EnsureFileExists(expression, "COPYFILE", "source", 0, sourcePath);
            File.Copy(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COPYFILE", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveFile(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.ResourceBaseDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.ResourceBaseDirectory);

            EnsureFileExists(expression, "MOVEFILE", "source", 0, sourcePath);
            File.Move(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MOVEFILE", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameFile(BoundCallExpression expression, string path, string newName)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureFileExists(expression, "RENAMEFILE", "path", 0, sourcePath);
            EnsureSimpleFileSystemName(expression, "RENAMEFILE", "newName", 1, newName);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError(
                    "RENAMEFILE",
                    "path",
                    "РЅРµ СЃРѕРґРµСЂР¶РёС‚ СЂРѕРґРёС‚РµР»СЊСЃРєСѓСЋ РїР°РїРєСѓ.",
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "RENAMEFILE", "newName", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDir(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);

            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: false);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirRecursive(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);

            if (!Directory.Exists(fullPath))
            {
                return RuntimeValue.FromBoolean(false);
            }

            Directory.Delete(fullPath, recursive: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DELETEDIRRECURSIVE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCopyDir(BoundCallExpression expression, string source, string destination, bool overwrite)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.ResourceBaseDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.ResourceBaseDirectory);

            EnsureDirectoryExists(expression, "COPYDIR", "source", 0, sourcePath);
            BuiltinFileSystemHelpers.CopyDirectory(sourcePath, destinationPath, overwrite);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "COPYDIR", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateMoveDir(BoundCallExpression expression, string source, string destination)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(source, _host.ResourceBaseDirectory);
            var destinationPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(destination, _host.ResourceBaseDirectory);

            EnsureDirectoryExists(expression, "MOVEDIR", "source", 0, sourcePath);
            Directory.Move(sourcePath, destinationPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MOVEDIR", "destination", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRenameDir(BoundCallExpression expression, string path, string newName)
    {
        try
        {
            var sourcePath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureDirectoryExists(expression, "RENAMEDIR", "path", 0, sourcePath);
            EnsureSimpleFileSystemName(expression, "RENAMEDIR", "newName", 1, newName);

            var parent = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(parent))
            {
                ReportBuiltInArgumentError(
                    "RENAMEDIR",
                    "path",
                    "РЅРµ СЃРѕРґРµСЂР¶РёС‚ СЂРѕРґРёС‚РµР»СЊСЃРєСѓСЋ РїР°РїРєСѓ.",
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "RENAMEDIR", "newName", 1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCurrentDir(BoundCallExpression expression)
    {
        try
        {
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.NormalizeFileSystemPath(_host.ResourceBaseDirectory ?? Environment.CurrentDirectory));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "CURRENTDIR", "path", -1, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateChangeDir(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureDirectoryExists(expression, "CHANGEDIR", "path", 0, fullPath);
            Directory.SetCurrentDirectory(fullPath);
            return RuntimeValue.FromBoolean(true);
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "CHANGEDIR", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDirSize(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            EnsureDirectoryExists(expression, "DIRSIZE", "path", 0, fullPath);
            return RuntimeValue.FromLong(BuiltinFileSystemHelpers.GetDirectorySize(fullPath));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (OverflowException)
        {
            ReportNumericOverflow("РЎСѓРјРјР°СЂРЅС‹Р№ СЂР°Р·РјРµСЂ РїР°РїРєРё РІ С„СѓРЅРєС†РёРё DIRSIZE", TypeSymbol.Long, expression.Location);
            throw new RuntimeFailureException();
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "DIRSIZE", "path", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFreeSpace(BoundCallExpression expression, string path)
    {
        try
        {
            var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
            var rootPath = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                ReportBuiltInArgumentError(
                    "FREESPACE",
                    "path",
                    "РЅРµ СѓРєР°Р·С‹РІР°РµС‚ РЅР° РґРёСЃРє РёР»Рё РєРѕСЂРµРЅСЊ С„Р°Р№Р»РѕРІРѕР№ СЃРёСЃС‚РµРјС‹.",
                    GetBuiltInArgumentLocation(expression, 0));
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.CreateTemporaryFile(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
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
            return RuntimeValue.FromString(BuiltinFileSystemHelpers.CreateTemporaryDirectory(prefix));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(expression, "MAKETEMPDIR", "prefix", 0, ex);
            throw new RuntimeFailureException();
        }
    }

    private void EnsureFileExists(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        if (!BuiltinFileSystemHelpers.EnsureFileExists(functionName, argumentName, path,
            GetBuiltInArgumentLocation(expression, argumentIndex), ReportBuiltInArgumentError))
        {
            throw new RuntimeFailureException();
        }
    }

    private void EnsureDirectoryExists(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        string path)
    {
        if (!BuiltinFileSystemHelpers.EnsureDirectoryExists(functionName, argumentName, path,
            GetBuiltInArgumentLocation(expression, argumentIndex), ReportBuiltInArgumentError))
        {
            throw new RuntimeFailureException();
        }
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
            "РЅРµ РјРѕР¶РµС‚ Р±С‹С‚СЊ РїСѓСЃС‚РѕР№ СЃС‚СЂРѕРєРѕР№.",
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
            "РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїСЂРѕСЃС‚С‹Рј РёРјРµРЅРµРј Р±РµР· РїСѓС‚Рё.",
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
            "РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ РїСЂРѕСЃС‚С‹Рј РїСЂРµС„РёРєСЃРѕРј РёРјРµРЅРё Р±РµР· РїСѓС‚Рё.",
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
        var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);

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
            "СѓРєР°Р·С‹РІР°РµС‚ РЅР° С„Р°Р№Р» РёР»Рё РїР°РїРєСѓ, РєРѕС‚РѕСЂС‹Рµ РЅРµ СЃСѓС‰РµСЃС‚РІСѓСЋС‚.",
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
        var fullPath = BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory);
        var rootPath = Path.GetPathRoot(fullPath);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            ReportBuiltInArgumentError(
                functionName,
                argumentName,
                "РЅРµ СѓРєР°Р·С‹РІР°РµС‚ РЅР° РґРёСЃРє РёР»Рё РєРѕСЂРµРЅСЊ С„Р°Р№Р»РѕРІРѕР№ СЃРёСЃС‚РµРјС‹.",
                GetBuiltInArgumentLocation(expression, argumentIndex));
            throw new RuntimeFailureException();
        }

        return new DriveInfo(rootPath);
    }

    private void ReportFileSystemFailure(
        BoundCallExpression expression,
        string functionName,
        string argumentName,
        int argumentIndex,
        Exception exception)
    {
        BuiltinFileSystemHelpers.ReportFileSystemFailure(functionName, argumentName, exception,
            GetBuiltInArgumentLocation(expression, argumentIndex), ReportBuiltInArgumentError);
    }
}
