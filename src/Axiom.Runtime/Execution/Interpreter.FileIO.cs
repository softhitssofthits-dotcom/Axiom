using System.Security;
using Axiom.Core;
using Axiom.Semantics;

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
                BuiltinArgumentCode,
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

            var file = new RuntimeTextFile(NormalizeFileSystemPath(path), runtimeMode, FileSystemTextEncoding);
            _openFiles.Add(file);
            AssignVariable(statement.FileVariable, RuntimeValue.FromFile(file));
            return ExecutionSignal.None;
        }
        catch (Exception ex) when (IsFileSystemException(ex))
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Оператор OPEN: путь не может быть открыт: {ex.Message}",
                statement.PathExpression.Location);
            throw new RuntimeFailureException();
        }
    }

    private ExecutionSignal ExecuteClose(BoundCloseStatement statement)
    {
        var file = RequireOpenFile(statement.FileVariable, statement.Location, "CLOSE");
        CloseFile(file);
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
        var file = RequireOpenFile(fileValue, expression.Location, "FILEPATH");
        return RuntimeValue.FromString(file.Path);
    }

    private RuntimeValue EvaluateFileMode(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireOpenFile(fileValue, expression.Location, "FILEMODE");
        return RuntimeValue.FromString(GetFileModeName(file.Mode));
    }

    private RuntimeValue EvaluateFilePos(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireOpenFile(fileValue, expression.Location, "FILEPOS");
        return RuntimeValue.FromLong(file.Position);
    }

    private RuntimeValue EvaluateFileLength(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireOpenFile(fileValue, expression.Location, "FILELENGTH");
        return RuntimeValue.FromLong(file.Length);
    }

    private RuntimeValue EvaluateLineNumber(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "LINENUMBER");
        return RuntimeValue.FromLong(file.LineNumber);
    }

    private RuntimeValue EvaluateEof(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "EOF");
        return RuntimeValue.FromBoolean(file.IsEndOfFile);
    }

    private RuntimeValue EvaluateReadToEnd(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "READTOEND");

        try
        {
            return RuntimeValue.FromString(file.ReadToEnd());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция READTOEND: не удалось прочитать текст из файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadText(BoundCallExpression expression, RuntimeValue fileValue, int count)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "READTEXT");

        try
        {
            return RuntimeValue.FromString(file.ReadText(count));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция READTEXT: не удалось прочитать текст из файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "READLINE");

        if (file.IsEndOfFile)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                "Функция READLINE: достигнут конец файла.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        try
        {
            return RuntimeValue.FromString(file.ReadLine());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция READLINE: не удалось прочитать строку из файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadLines(BoundCallExpression expression, RuntimeValue fileValue, int count)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "READLINES");

        try
        {
            return CreateStringArrayValue(file.ReadLines(count));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция READLINES: не удалось прочитать строки из файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateSkipLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireReadableFile(fileValue, expression.Location, "SKIPLINE");

        if (file.IsEndOfFile)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                "Функция SKIPLINE: достигнут конец файла.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.SkipLine();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция SKIPLINE: не удалось пропустить строку файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateWrite(BoundCallExpression expression, RuntimeValue fileValue, string text, bool appendLineBreak)
    {
        var operationName = appendLineBreak ? "WRITELINE" : "WRITE";
        var file = RequireWritableFile(fileValue, expression.Location, operationName);

        try
        {
            file.Write(text, appendLineBreak);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция {operationName}: не удалось записать в файл: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateNewLine(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireWritableFile(fileValue, expression.Location, "NEWLINE");

        try
        {
            file.Write(string.Empty, appendLineBreak: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция NEWLINE: не удалось записать перевод строки в файл: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFlush(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireWritableFile(fileValue, expression.Location, "FLUSH");

        try
        {
            file.Flush();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция FLUSH: не удалось сбросить буфер файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateSeekFile(BoundCallExpression expression, RuntimeValue fileValue, RuntimeValue positionValue)
    {
        var file = RequireOpenFile(fileValue, expression.Location, "SEEKFILE");
        var position = AsFilePosition(expression, positionValue, argumentIndex: 1);

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                "Функция SEEKFILE: файл, открытый в режиме APPEND, не поддерживает явный переход по позиции.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        if (position < 0)
        {
            ReportBuiltInArgumentError(
                "SEEKFILE",
                "position",
                "не может быть отрицательным.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        if (position > file.Length)
        {
            ReportBuiltInArgumentError(
                "SEEKFILE",
                "position",
                "не может выходить за пределы файла.",
                GetBuiltInArgumentLocation(expression, 1));
            throw new RuntimeFailureException();
        }

        try
        {
            file.Seek(position);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция SEEKFILE: не удалось перейти к указанной позиции: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRewind(BoundCallExpression expression, RuntimeValue fileValue)
    {
        var file = RequireOpenFile(fileValue, expression.Location, "REWIND");

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                "Функция REWIND: файл, открытый в режиме APPEND, не поддерживает переход в начало.",
                expression.Location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.Rewind();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or ObjectDisposedException or InvalidOperationException)
        {
            ReportRuntimeDiagnostic(
                BuiltinArgumentCode,
                $"Функция REWIND: не удалось перейти в начало файла: {ex.Message}",
                expression.Location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeTextFile RequireReadableFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        var file = RequireOpenFile(fileValue, location, operationName);

        if (file.Mode == RuntimeTextFileMode.Input)
        {
            return file;
        }

        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Функция {operationName}: аргумент 'file' должен быть открыт для чтения.",
            location);
        throw new RuntimeFailureException();
    }

    private RuntimeTextFile RequireWritableFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        var file = RequireOpenFile(fileValue, location, operationName);

        if (file.Mode is RuntimeTextFileMode.Output or RuntimeTextFileMode.Append)
        {
            return file;
        }

        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Функция {operationName}: аргумент 'file' должен быть открыт для записи.",
            location);
        throw new RuntimeFailureException();
    }

    private RuntimeTextFile RequireOpenFile(VariableSymbol variable, TextLocation location, string operationName)
    {
        return RequireOpenFile(GetVariableValue(variable), location, operationName);
    }

    private RuntimeTextFile RequireOpenFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        var file = fileValue.AsFile();

        if (file is not null && file.IsOpen)
        {
            return file;
        }

        ReportRuntimeDiagnostic(
            BuiltinArgumentCode,
            $"Операция {operationName}: нужен открытый файл.",
            location);
        throw new RuntimeFailureException();
    }

    private long AsFilePosition(BoundCallExpression expression, RuntimeValue value, int argumentIndex)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => value.AsLong(),
            _ => ReportInvalidRuntimeState<long>(
                $"Функция SEEKFILE получила неподдерживаемый тип позиции '{value.Type.Name}'.",
                GetBuiltInArgumentLocation(expression, argumentIndex))
        };
    }

    private void CloseAllOpenFiles()
    {
        foreach (var file in _openFiles.ToArray())
        {
            CloseFile(file);
        }
    }

    private void CloseFile(RuntimeTextFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        file.Dispose();
        _openFiles.Remove(file);
    }

    private static string GetFileModeName(RuntimeTextFileMode mode)
    {
        return mode switch
        {
            RuntimeTextFileMode.Input => "INPUT",
            RuntimeTextFileMode.Output => "OUTPUT",
            RuntimeTextFileMode.Append => "APPEND",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported file mode.")
        };
    }
}
