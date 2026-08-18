using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime.Builtins;

internal static class BuiltinFileIOHelpers
{
    public static RuntimeTextFile RequireOpenFile(
        RuntimeValue fileValue,
        TextLocation location,
        string operationName,
        Action<string, TextLocation> reportError)
    {
        var file = fileValue.AsFile();

        if (file is not null && file.IsOpen)
        {
            return file;
        }

        reportError($"Операция {operationName}: нужен открытый файл.", location);
        throw new RuntimeFailureException();
    }

    public static RuntimeTextFile RequireReadableFile(
        RuntimeValue fileValue,
        TextLocation location,
        string operationName,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, operationName, reportError);

        if (file.Mode == RuntimeTextFileMode.Input)
        {
            return file;
        }

        reportError(
            $"Функция {operationName}: аргумент 'file' должен быть открыт для чтения.",
            location);
        throw new RuntimeFailureException();
    }

    public static RuntimeTextFile RequireWritableFile(
        RuntimeValue fileValue,
        TextLocation location,
        string operationName,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, operationName, reportError);

        if (file.Mode is RuntimeTextFileMode.Output or RuntimeTextFileMode.Append)
        {
            return file;
        }

        reportError(
            $"Функция {operationName}: аргумент 'file' должен быть открыт для записи.",
            location);
        throw new RuntimeFailureException();
    }

    public static RuntimeValue EvaluateFilePath(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, "FILEPATH", reportError);
        return RuntimeValue.FromString(file.Path);
    }

    public static RuntimeValue EvaluateFileMode(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, "FILEMODE", reportError);
        return RuntimeValue.FromString(GetFileModeName(file.Mode));
    }

    public static RuntimeValue EvaluateFilePos(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, "FILEPOS", reportError);
        return RuntimeValue.FromLong(file.Position);
    }

    public static RuntimeValue EvaluateFileLength(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, "FILELENGTH", reportError);
        return RuntimeValue.FromLong(file.Length);
    }

    public static RuntimeValue EvaluateLineNumber(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "LINENUMBER", reportError);
        return RuntimeValue.FromLong(file.LineNumber);
    }

    public static RuntimeValue EvaluateEof(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "EOF", reportError);
        return RuntimeValue.FromBoolean(file.IsEndOfFile);
    }

    public static RuntimeValue EvaluateReadToEnd(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "READTOEND", reportError);

        try
        {
            return RuntimeValue.FromString(file.ReadToEnd());
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция READTOEND: не удалось прочитать текст из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateReadText(
        RuntimeValue fileValue,
        int count,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "READTEXT", reportError);

        try
        {
            return RuntimeValue.FromString(file.ReadText(count));
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция READTEXT: не удалось прочитать текст из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateReadLine(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "READLINE", reportError);

        if (file.IsEndOfFile)
        {
            reportError("Функция READLINE: достигнут конец файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            return RuntimeValue.FromString(file.ReadLine());
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция READLINE: не удалось прочитать строку из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateSkipLine(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireReadableFile(fileValue, location, "SKIPLINE", reportError);

        if (file.IsEndOfFile)
        {
            reportError("Функция SKIPLINE: достигнут конец файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.SkipLine();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция SKIPLINE: не удалось пропустить строку файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateWrite(
        RuntimeValue fileValue,
        string text,
        bool appendLineBreak,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var operationName = appendLineBreak ? "WRITELINE" : "WRITE";
        var file = RequireWritableFile(fileValue, location, operationName, reportError);

        try
        {
            file.Write(text, appendLineBreak);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция {operationName}: не удалось записать в файл: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateNewLine(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireWritableFile(fileValue, location, "NEWLINE", reportError);

        try
        {
            file.Write(string.Empty, appendLineBreak: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция NEWLINE: не удалось записать перевод строки в файл: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateFlush(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireWritableFile(fileValue, location, "FLUSH", reportError);

        try
        {
            file.Flush();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция FLUSH: не удалось сбросить буфер файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateSeekFile(
        RuntimeValue fileValue,
        long position,
        TextLocation location,
        Action<string, TextLocation> reportError,
        Action<string, string, string, TextLocation> reportArgumentError)
    {
        var file = RequireOpenFile(fileValue, location, "SEEKFILE", reportError);

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            reportError(
                "Функция SEEKFILE: файл, открытый в режиме APPEND, не поддерживает явный переход по позиции.",
                location);
            throw new RuntimeFailureException();
        }

        if (position < 0)
        {
            reportArgumentError("SEEKFILE", "position", "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        if (position > file.Length)
        {
            reportArgumentError("SEEKFILE", "position", "не может выходить за пределы файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.Seek(position);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция SEEKFILE: не удалось перейти к указанной позиции: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static RuntimeValue EvaluateRewind(
        RuntimeValue fileValue,
        TextLocation location,
        Action<string, TextLocation> reportError)
    {
        var file = RequireOpenFile(fileValue, location, "REWIND", reportError);

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            reportError(
                "Функция REWIND: файл, открытый в режиме APPEND, не поддерживает переход в начало.",
                location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.Rewind();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            reportError(
                $"Функция REWIND: не удалось перейти в начало файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    public static long AsFilePosition(RuntimeValue value, TextLocation? location, Func<string, TextLocation?, long> reportInvalidState)
    {
        return value.Type switch
        {
            var type when type == TypeSymbol.Integer => value.AsInteger(),
            var type when type == TypeSymbol.Long => value.AsLong(),
            _ => reportInvalidState(
                $"Функция SEEKFILE получила неподдерживаемый тип позиции '{value.Type.Name}'.",
                location)
        };
    }

    public static void CloseFile(RuntimeTextFile file, HashSet<RuntimeTextFile> openFiles)
    {
        ArgumentNullException.ThrowIfNull(file);

        file.Dispose();
        openFiles.Remove(file);
    }

    public static void CloseAllOpenFiles(HashSet<RuntimeTextFile> openFiles)
    {
        foreach (var file in openFiles.ToArray())
        {
            CloseFile(file, openFiles);
        }
    }

    public static string GetFileModeName(RuntimeTextFileMode mode)
    {
        return mode switch
        {
            RuntimeTextFileMode.Input => "INPUT",
            RuntimeTextFileMode.Output => "OUTPUT",
            RuntimeTextFileMode.Append => "APPEND",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported file mode.")
        };
    }

    public static bool IsFileIoException(Exception exception)
    {
        return exception is System.IO.IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or ObjectDisposedException
            or InvalidOperationException;
    }
}
