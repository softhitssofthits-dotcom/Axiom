using System.Globalization;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateFileSizeBuiltin(string path, TextLocation location)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemArgument("FILESIZE", "path", path, location));
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

    /// <summary>
    /// Типизированные формы файловых метаданных возвращают момент времени.
    /// Файловая система хранит отметку в UTC, поэтому значение не зависит от
    /// местного пояса и перевода часов; точность сохраняется до тика, а не
    /// обрезается до секунды, как в строковых формах.
    /// </summary>
    private RuntimeValue EvaluateFileDateUtcBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileMoment("FILEDATEUTC", path, static file => file.LastWriteTimeUtc, location);
    }

    private RuntimeValue EvaluateFileCreateDateUtcBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileMoment("FILECREATEDATEUTC", path, static file => file.CreationTimeUtc, location);
    }

    private RuntimeValue EvaluateFileAccessDateUtcBuiltin(string path, TextLocation location)
    {
        return EvaluateExistingFileMoment("FILEACCESSEDDATEUTC", path, static file => file.LastAccessTimeUtc, location);
    }

    private RuntimeValue EvaluateDirDateUtcBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("DIRDATEUTC", "path", path, location);
            EnsureDirectoryExists("DIRDATEUTC", "path", fullPath, location);
            return RuntimeValue.FromUtcDateTime(
                DateTime.SpecifyKind(new DirectoryInfo(fullPath).LastWriteTimeUtc, DateTimeKind.Utc));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("DIRDATEUTC", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateExistingFileMoment(
        string functionName,
        string path,
        Func<FileInfo, DateTime> dateSelector,
        TextLocation location)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemArgument(functionName, "path", path, location));
            if (!file.Exists)
            {
                ReportFileSystemMissingFile(functionName, "path", location);
                throw new RuntimeFailureException();
            }

            return RuntimeValue.FromUtcDateTime(DateTime.SpecifyKind(dateSelector(file), DateTimeKind.Utc));
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

    private RuntimeValue EvaluateDirDateBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("DIRDATE", "path", path, location);
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

    private RuntimeValue EvaluateExistingFileDate(string functionName, string path, Func<FileInfo, DateTime> dateSelector, TextLocation location)
    {
        try
        {
            var file = new FileInfo(NormalizeFileSystemArgument(functionName, "path", path, location));
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

    private FileSystemInfo GetExistingFileSystemInfo(string functionName, string argumentName, string path, TextLocation location)
    {
        var fullPath = NormalizeFileSystemArgument(functionName, argumentName, path, location);

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
}
