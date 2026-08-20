using Axiom.Core;
using Axiom.Semantics;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private ExecutionSignal ExecuteOpen(BoundOpenStatement statement)
    {
        var path = EvaluateExpression(statement.PathExpression).AsString();
        var existingFile = GetVariableValue(statement.FileVariable).AsFile();

        if (existingFile is not null && existingFile.IsOpen)
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Оператор OPEN: переменная '{statement.FileVariable.Name}' уже содержит открытый файл.",
                statement.Location);
            throw new RuntimeFailureException();
        }

        try
        {
            var runtimeMode = statement.Mode switch
            {
                BoundFileOpenMode.Input => RuntimeTextFileMode.Input,
                BoundFileOpenMode.Output => RuntimeTextFileMode.Output,
                BoundFileOpenMode.Append => RuntimeTextFileMode.Append,
                _ => throw new ArgumentOutOfRangeException(nameof(statement), statement.Mode, "Unsupported file mode.")
            };

            var file = new RuntimeTextFile(BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.ResourceBaseDirectory), runtimeMode, BuiltinFileSystemHelpers.FileSystemTextEncoding);
            _openFiles.Add(file);
            AssignVariable(statement.FileVariable, RuntimeValue.FromFile(file));
            return ExecutionSignal.None;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Оператор OPEN: путь не может быть открыт: {ex.Message}",
                statement.PathExpression.Location);
            throw new RuntimeFailureException();
        }
    }

    private ExecutionSignal ExecuteClose(BoundCloseStatement statement)
    {
        var file = RequireOpenFile(statement.FileVariable, statement.Location, "CLOSE");
        BuiltinFileIOHelpers.CloseFile(file, _openFiles);
        AssignVariable(statement.FileVariable, RuntimeValue.FromFile(value: null));
        return ExecutionSignal.None;
    }

    private RuntimeValue EvaluateIsOpen(RuntimeValue fileValue)
    {
        var file = fileValue.AsFile();
        return RuntimeValue.FromBoolean(file is not null && file.IsOpen);
    }

    private RuntimeValue EvaluateFilePath(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateFilePath(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFileMode(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateFileMode(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFilePos(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateFilePos(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFileLength(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateFileLength(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateLineNumber(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateLineNumber(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateEof(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateEof(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadToEnd(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateReadToEnd(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadText(BoundCallExpression expression, RuntimeValue fileValue, int count)
    {
        return BuiltinFileIOHelpers.EvaluateReadText(fileValue, count, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateReadLine(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadLines(BoundCallExpression expression, RuntimeValue fileValue, int count)
    {
        return BuiltinFileIOHelpers.EvaluateReadLines(
            fileValue, count, expression.Location, ReportFileIOError, CreateStringArrayValue);
    }

    private RuntimeValue EvaluateSkipLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateSkipLine(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateWrite(BoundCallExpression expression, RuntimeValue fileValue, string text, bool appendLineBreak)
    {
        return BuiltinFileIOHelpers.EvaluateWrite(fileValue, text, appendLineBreak, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateNewLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateNewLine(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFlush(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateFlush(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeValue EvaluateSeekFile(BoundCallExpression expression, RuntimeValue fileValue, RuntimeValue positionValue)
    {
        var position = BuiltinFileIOHelpers.AsFilePosition(
            positionValue,
            GetBuiltInArgumentLocation(expression, 1),
            (msg, loc) => ReportInvalidRuntimeState<long>(msg, loc));

        return BuiltinFileIOHelpers.EvaluateSeekFile(
            fileValue, position, expression.Location,
            ReportFileIOError,
            (funcName, argName, detail, loc) => ReportBuiltInArgumentError(funcName, argName, detail, loc));
    }

    private RuntimeValue EvaluateRewind(BoundCallExpression expression, RuntimeValue fileValue)
    {
        return BuiltinFileIOHelpers.EvaluateRewind(fileValue, expression.Location, ReportFileIOError);
    }

    private RuntimeTextFile RequireReadableFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        return BuiltinFileIOHelpers.RequireReadableFile(fileValue, location, operationName, ReportFileIOError);
    }

    private RuntimeTextFile RequireWritableFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        return BuiltinFileIOHelpers.RequireWritableFile(fileValue, location, operationName, ReportFileIOError);
    }

    private RuntimeTextFile RequireOpenFile(VariableSymbol variable, TextLocation location, string operationName)
    {
        return RequireOpenFile(GetVariableValue(variable), location, operationName);
    }

    private RuntimeTextFile RequireOpenFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        return BuiltinFileIOHelpers.RequireOpenFile(fileValue, location, operationName, ReportFileIOError);
    }

    private long AsFilePosition(BoundCallExpression expression, RuntimeValue value, int argumentIndex)
    {
        return BuiltinFileIOHelpers.AsFilePosition(
            value,
            GetBuiltInArgumentLocation(expression, argumentIndex),
            (msg, loc) => ReportInvalidRuntimeState<long>(msg, loc));
    }

    private void CloseAllOpenFiles()
    {
        BuiltinFileIOHelpers.CloseAllOpenFiles(_openFiles);
    }

    private void CloseFile(RuntimeTextFile file)
    {
        BuiltinFileIOHelpers.CloseFile(file, _openFiles);
    }

    private void ReportFileIOError(string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, detail, location);
    }
}
