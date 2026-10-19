using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private RuntimeValue EvaluateReadFileBuiltin(string path, TextLocation location, string? encodingName = null)
    {
        try
        {
            ValidateEncodingArgument("READFILE", encodingName, location);
            var fullPath = NormalizeFileSystemArgument("READFILE", "path", path, location);
            var text = encodingName is null
                ? BuiltinFileSystemHelpers.ReadTextFileWithFallback(fullPath, ThrowIfBuiltinCancellationRequested)
                : BuiltinTextEncodingHelpers.Read(fullPath, encodingName, ThrowIfBuiltinCancellationRequested);
            return RuntimeValue.FromString(text);
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
            ValidateEncodingArgument("READALLLINES", encodingName, location);
            var fullPath = NormalizeFileSystemArgument("READALLLINES", "path", path, location);
            EnsureFileExists("READALLLINES", "path", fullPath, location);
            var text = encodingName is null
                ? BuiltinFileSystemHelpers.ReadTextFileWithFallback(fullPath, ThrowIfBuiltinCancellationRequested)
                : BuiltinTextEncodingHelpers.Read(fullPath, encodingName, ThrowIfBuiltinCancellationRequested);
            return CreateStringArrayValue(BuiltinFileSystemHelpers.ReadDecodedLines(text, ThrowIfBuiltinCancellationRequested));
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
            ValidateEncodingArgument("WRITEFILE", encodingName, location);
            if (encodingName is null)
                File.WriteAllText(NormalizeFileSystemArgument("WRITEFILE", "path", path, location), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            else
                BuiltinTextEncodingHelpers.Write(NormalizeFileSystemArgument("WRITEFILE", "path", path, location), text, encodingName, append: false, ThrowIfBuiltinCancellationRequested);
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
            ValidateEncodingArgument("APPENDFILE", encodingName, location);
            if (encodingName is null)
                File.AppendAllText(NormalizeFileSystemArgument("APPENDFILE", "path", path, location), text, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            else
                BuiltinTextEncodingHelpers.Write(NormalizeFileSystemArgument("APPENDFILE", "path", path, location), text, encodingName, append: true, ThrowIfBuiltinCancellationRequested);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure("APPENDFILE", "path", ex, location);
            throw new RuntimeFailureException();
        }
    }
}
