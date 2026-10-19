using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateFileExistsBuiltin(string path, TextLocation location)
    {
        try
        {
            return RuntimeValue.FromBoolean(File.Exists(NormalizeFileSystemArgument("FILEEXISTS", "path", path, location)));
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
            return RuntimeValue.FromBoolean(Directory.Exists(NormalizeFileSystemArgument("DIREXISTS", "path", path, location)));
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
            var fullPath = NormalizeFileSystemArgument("PATHEXISTS", "path", path, location);
            return RuntimeValue.FromBoolean(File.Exists(fullPath) || Directory.Exists(fullPath));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("PATHEXISTS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetFilesBuiltin(string path, string pattern, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("GETFILES", "path", path, location);
            EnsureDirectoryExists("GETFILES", "path", fullPath, location);
            EnsureSearchPattern("GETFILES", pattern, location);

            var files = Directory.EnumerateFiles(fullPath, pattern)
                .Select(filePath => { ThrowIfBuiltinCancellationRequested(); return filePath; })
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
            ReportFileSystemFailure("GETFILES", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateGetDirsBuiltin(string path, string pattern, TextLocation location)
    {
        try
        {
            var fullPath = NormalizeFileSystemArgument("GETDIRS", "path", path, location);
            EnsureDirectoryExists("GETDIRS", "path", fullPath, location);
            EnsureSearchPattern("GETDIRS", pattern, location);

            var directories = Directory.EnumerateDirectories(fullPath, pattern)
                .Select(directoryPath => { ThrowIfBuiltinCancellationRequested(); return directoryPath; })
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
            ReportFileSystemFailure("GETDIRS", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateCombinePathBuiltin(string left, string right, TextLocation location)
    {
        try
        {
            ValidatePathArgument("COMBINEPATH", "left", left, location);
            ValidatePathArgument("COMBINEPATH", "right", right, location);
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
            return RuntimeValue.FromString(Path.GetFileName(NormalizeFileSystemArgument("FILENAME", "path", path, location)));
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
            return RuntimeValue.FromString(Path.GetDirectoryName(NormalizeFileSystemArgument("DIRNAME", "path", path, location)) ?? string.Empty);
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
            return RuntimeValue.FromString(Path.GetExtension(NormalizeFileSystemArgument("EXTNAME", "path", path, location)));
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
            return RuntimeValue.FromString(NormalizeFileSystemArgument("FULLPATH", "path", path, location));
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
            return RuntimeValue.FromString(Path.GetPathRoot(NormalizeFileSystemArgument("PATHROOT", "path", path, location)) ?? string.Empty);
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
            ValidatePathArgument("ISABSOLUTEPATH", "path", path, location);
            var expanded = Environment.ExpandEnvironmentVariables(path);
            ValidatePathArgument("ISABSOLUTEPATH", "path", expanded, location);
            return RuntimeValue.FromBoolean(Path.IsPathFullyQualified(expanded));
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("ISABSOLUTEPATH", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }
}
