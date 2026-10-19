using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateMakeDirBuiltin(string path, TextLocation location)
    {
        try
        {
            Directory.CreateDirectory(NormalizeFileSystemArgument("MAKEDIR", "path", path, location));
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("MAKEDIR", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateDeleteDirBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("DELETEDIR", "path", path, location);
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
            var fullPath = NormalizeFileSystemArgument("DELETEDIRRECURSIVE", "path", path, location);
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
            var sourcePath = NormalizeFileSystemArgument("COPYDIR", "source", source, location);
            var destinationPath = NormalizeFileSystemArgument("COPYDIR", "destination", destination, location);
            EnsureDirectoryExists("COPYDIR", "source", sourcePath, location);
            BuiltinFileSystemHelpers.CopyDirectory(sourcePath, destinationPath, overwrite, ThrowIfBuiltinCancellationRequested);
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
            var sourcePath = NormalizeFileSystemArgument("MOVEDIR", "source", source, location);
            var destinationPath = NormalizeFileSystemArgument("MOVEDIR", "destination", destination, location);
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
            var sourcePath = NormalizeFileSystemArgument("RENAMEDIR", "path", path, location);
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
            var fullPath = NormalizeFileSystemArgument("CHANGEDIR", "path", path, location);
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
            var fullPath = NormalizeFileSystemArgument("DIRSIZE", "path", path, location);
            EnsureDirectoryExists("DIRSIZE", "path", fullPath, location);
            return RuntimeValue.FromLong(BuiltinFileSystemHelpers.GetDirectorySize(fullPath, ThrowIfBuiltinCancellationRequested));
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
}
