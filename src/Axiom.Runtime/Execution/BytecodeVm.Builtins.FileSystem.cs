using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private static RuntimeValue CreateStringArrayValue(IReadOnlyList<string> values)
    {
        var arrayType = (ArrayTypeSymbol)ArrayTypeSymbol.Create(TypeSymbol.String);
        var items = new RuntimeValue[values.Count];

        // Заполнение по индексу не создаёт промежуточный построитель LINQ:
        // массив значений всё равно нужен CreateCompactArrayValue.
        for (var index = 0; index < items.Length; index++)
        {
            items[index] = RuntimeValue.FromString(values[index]);
        }

        return CreateCompactArrayValue(arrayType, items);
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

    private string NormalizeFileSystemArgument(string functionName, string argumentName, string path, TextLocation location)
    {
        try
        {
            return BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory);
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportFileSystemFailure(functionName, argumentName, ex, location);
            throw new RuntimeFailureException();
        }
    }

    private void ValidatePathArgument(string functionName, string argumentName, string path, TextLocation location)
    {
        if (path.IndexOf('\0') < 0) return;
        ReportBuiltInArgumentError(functionName, argumentName, "не должен содержать нулевой символ.", location);
        throw new RuntimeFailureException();
    }

    private void ValidateEncodingArgument(string functionName, string? encodingName, TextLocation location)
    {
        if (encodingName is null) return;
        try
        {
            _ = BuiltinTextEncodingHelpers.Resolve(encodingName);
        }
        catch (ArgumentException ex)
        {
            ReportFileSystemFailure(functionName, "encoding", ex, location);
            throw new RuntimeFailureException();
        }
    }

    private void EnsureSearchPattern(string functionName, string pattern, TextLocation location)
    {
        if (pattern.Length > 0 && pattern.IndexOfAny(['\\', '/', ':', '\0']) < 0)
        {
            return;
        }

        var detail = pattern.Length == 0
            ? "не может быть пустой строкой."
            : "должен задавать маску имени без частей пути и нулевого символа.";
        ReportBuiltInArgumentError(functionName, "pattern", detail, location);
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

    private void ReportFileSystemMissingFile(string functionName, string argumentName, TextLocation location)
    {
        ReportBuiltInArgumentError(functionName, argumentName, "указывает на файл, который не существует.", location);
    }

    private void ReportFileSystemFailure(string functionName, string argumentName, Exception exception, TextLocation location)
    {
        BuiltinFileSystemHelpers.ReportFileSystemFailure(functionName, argumentName, exception, location, ReportBuiltInArgumentError);
    }
}
