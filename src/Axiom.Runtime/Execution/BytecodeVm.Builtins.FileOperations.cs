using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateDeleteFileBuiltin(string path, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("DELETEFILE", "path", path, location);
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
            var sourcePath = NormalizeFileSystemArgument("COPYFILE", "source", source, location);
            var destinationPath = NormalizeFileSystemArgument("COPYFILE", "destination", destination, location);
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
            var sourcePath = NormalizeFileSystemArgument("MOVEFILE", "source", source, location);
            var destinationPath = NormalizeFileSystemArgument("MOVEFILE", "destination", destination, location);
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
            var sourcePath = NormalizeFileSystemArgument("RENAMEFILE", "path", path, location);
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
}
