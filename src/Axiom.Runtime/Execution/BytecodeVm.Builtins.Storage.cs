using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateFreeSpaceBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("FREESPACE", "path", path, location);
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

    private DriveInfo GetDriveInfo(string functionName, string argumentName, string path, TextLocation location)
    {
        var fullPath = NormalizeFileSystemArgument(functionName, argumentName, path, location);
        var rootPath = Path.GetPathRoot(fullPath);

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            ReportBuiltInArgumentError(functionName, argumentName, "не указывает на диск или корень файловой системы.", location);
            throw new RuntimeFailureException();
        }

        return new DriveInfo(rootPath);
    }
}
