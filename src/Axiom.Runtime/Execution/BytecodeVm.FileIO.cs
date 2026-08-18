using System.Security;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteOpenFile(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var existingFileValue = PopValue(frame, instructionIndex);
        var path = PopValue(frame, instructionIndex).AsString();
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var variableName = ReadDebugName(instruction.GetOperand(1));

        var existingFile = existingFileValue.AsFile();
        if (existingFile is not null && existingFile.IsOpen)
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Оператор OPEN: переменная '{variableName}' уже содержит открытый файл.",
                location);
            throw new RuntimeFailureException();
        }

        try
        {
            var file = new RuntimeTextFile(
                BuiltinFileSystemHelpers.NormalizeFileSystemPath(path),
                ReadOpenMode(instruction.GetOperand(0), location),
                BuiltinFileSystemHelpers.FileSystemTextEncoding);

            _openFiles.Add(file);
            InvalidateReverseExecutionHistory();
            frame.EvaluationStack.Push(RuntimeValue.FromFile(file));
        }
        catch (RuntimeFailureException)
        {
            throw;
        }
        catch (Exception ex) when (BuiltinFileSystemHelpers.IsFileSystemException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Оператор OPEN: путь не может быть открыт: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private void ExecuteCloseFile(BytecodeCallFrame frame, int instructionIndex)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);
        var file = RequireOpenFile(PopValue(frame, instructionIndex), location, "CLOSE");

        CloseFile(file);
        InvalidateReverseExecutionHistory();
        frame.EvaluationStack.Push(RuntimeValue.FromFile(value: null));
    }

    private RuntimeValue EvaluateIsOpenBuiltin(RuntimeValue fileValue)
    {
        var file = fileValue.AsFile();
        return RuntimeValue.FromBoolean(file is not null && file.IsOpen);
    }

    private RuntimeValue EvaluateFilePathBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "FILEPATH");
        return RuntimeValue.FromString(file.Path);
    }

    private RuntimeValue EvaluateFileModeBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "FILEMODE");
        return RuntimeValue.FromString(GetFileModeName(file.Mode));
    }

    private RuntimeValue EvaluateFilePosBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "FILEPOS");
        return RuntimeValue.FromLong(file.Position);
    }

    private RuntimeValue EvaluateFileLengthBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "FILELENGTH");
        return RuntimeValue.FromLong(file.Length);
    }

    private RuntimeValue EvaluateLineNumberBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "LINENUMBER");
        return RuntimeValue.FromLong(file.LineNumber);
    }

    private RuntimeValue EvaluateEofBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "EOF");
        return RuntimeValue.FromBoolean(file.IsEndOfFile);
    }

    private RuntimeValue EvaluateReadToEndBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "READTOEND");

        try
        {
            return RuntimeValue.FromString(file.ReadToEnd());
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция READTOEND: не удалось прочитать текст из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadTextBuiltin(RuntimeValue fileValue, int count, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "READTEXT");

        try
        {
            return RuntimeValue.FromString(file.ReadText(count));
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция READTEXT: не удалось прочитать текст из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "READLINE");

        if (file.IsEndOfFile)
        {
            ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, "Функция READLINE: достигнут конец файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            return RuntimeValue.FromString(file.ReadLine());
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция READLINE: не удалось прочитать строку из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateReadLinesBuiltin(RuntimeValue fileValue, int count, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "READLINES");

        try
        {
            return CreateStringArrayValue(file.ReadLines(count));
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция READLINES: не удалось прочитать строки из файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateSkipLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireReadableFile(fileValue, location, "SKIPLINE");

        if (file.IsEndOfFile)
        {
            ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, "Функция SKIPLINE: достигнут конец файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.SkipLine();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция SKIPLINE: не удалось пропустить строку файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateWriteBuiltin(RuntimeValue fileValue, string text, bool appendLineBreak, TextLocation location)
    {
        var operationName = appendLineBreak ? "WRITELINE" : "WRITE";
        var file = RequireWritableFile(fileValue, location, operationName);

        try
        {
            file.Write(text, appendLineBreak);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileWriteException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция {operationName}: не удалось записать в файл: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateNewLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireWritableFile(fileValue, location, "NEWLINE");

        try
        {
            file.Write(string.Empty, appendLineBreak: true);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileWriteException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция NEWLINE: не удалось записать перевод строки в файл: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateFlushBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireWritableFile(fileValue, location, "FLUSH");

        try
        {
            file.Flush();
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileWriteException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция FLUSH: не удалось сбросить буфер файла: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateSeekFileBuiltin(RuntimeValue fileValue, RuntimeValue positionValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "SEEKFILE");
        var position = AsFilePosition(positionValue, location);

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                "Функция SEEKFILE: файл, открытый в режиме APPEND, не поддерживает явный переход по позиции.",
                location);
            throw new RuntimeFailureException();
        }

        if (position < 0)
        {
            ReportBuiltInArgumentError("SEEKFILE", "position", "не может быть отрицательным.", location);
            throw new RuntimeFailureException();
        }

        if (position > file.Length)
        {
            ReportBuiltInArgumentError("SEEKFILE", "position", "не может выходить за пределы файла.", location);
            throw new RuntimeFailureException();
        }

        try
        {
            file.Seek(position);
            return RuntimeValue.FromBoolean(true);
        }
        catch (Exception ex) when (IsFileIoException(ex))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция SEEKFILE: не удалось перейти к указанной позиции: {ex.Message}",
                location);
            throw new RuntimeFailureException();
        }
    }

    private RuntimeValue EvaluateRewindBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        var file = RequireOpenFile(fileValue, location, "REWIND");

        if (file.Mode == RuntimeTextFileMode.Append)
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
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
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                $"Функция REWIND: не удалось перейти в начало файла: {ex.Message}",
                location);
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
            DiagnosticCodes.BuiltinArgument,
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
            DiagnosticCodes.BuiltinArgument,
            $"Функция {operationName}: аргумент 'file' должен быть открыт для записи.",
            location);
        throw new RuntimeFailureException();
    }

    private RuntimeTextFile RequireOpenFile(RuntimeValue fileValue, TextLocation location, string operationName)
    {
        var file = fileValue.AsFile();
        if (file is not null && file.IsOpen)
        {
            return file;
        }

        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, $"Операция {operationName}: нужен открытый файл.", location);
        throw new RuntimeFailureException();
    }

    private RuntimeTextFileMode ReadOpenMode(int mode, TextLocation location)
    {
        return mode switch
        {
            0 => RuntimeTextFileMode.Input,
            1 => RuntimeTextFileMode.Output,
            2 => RuntimeTextFileMode.Append,
            _ => ReportInvalidRuntimeState<RuntimeTextFileMode>($"OPEN получил неизвестный режим файла {mode}.", location)
        };
    }

    private long AsFilePosition(RuntimeValue value, TextLocation location)
    {
        return value.Type switch
        {
            var type when type == Axiom.Semantics.TypeSymbol.Integer => value.AsInteger(),
            var type when type == Axiom.Semantics.TypeSymbol.Long => value.AsLong(),
            _ => ReportInvalidRuntimeState<long>(
                $"Функция SEEKFILE получила неподдерживаемый тип позиции '{value.Type.Name}'.",
                location)
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

    private static bool IsFileIoException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or SecurityException
            or ObjectDisposedException
            or InvalidOperationException;
    }

    private static bool IsFileWriteException(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or SecurityException
            or ObjectDisposedException;
    }
}
