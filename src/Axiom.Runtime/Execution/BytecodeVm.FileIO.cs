using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecuteOpenFile(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var existingFileValue = PopValue(frame, instructionIndex);
        var encodingName = instruction.OperandCount == 3 ? PopValue(frame, instructionIndex).AsString() : null;
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
                BuiltinFileSystemHelpers.NormalizeFileSystemPath(path, _host.CurrentDirectory),
                ReadOpenMode(instruction.GetOperand(0), location),
                BuiltinFileSystemHelpers.FileSystemTextEncoding,
                encodingName,
                ThrowIfBuiltinCancellationRequested);

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
        var file = BuiltinFileIOHelpers.RequireOpenFile(PopValue(frame, instructionIndex), location, "CLOSE", ReportFileIOError);

        try
        {
            BuiltinFileIOHelpers.CloseFile(file, _openFiles);
        }
        catch (Exception ex) when (BuiltinFileIOHelpers.IsFileIoException(ex))
        {
            ReportFileIOError($"Оператор CLOSE: не удалось завершить запись файла: {ex.Message}", location);
            throw new RuntimeFailureException();
        }
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
        return BuiltinFileIOHelpers.EvaluateFilePath(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFileModeBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateFileMode(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFilePosBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateFilePos(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFileLengthBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateFileLength(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateLineNumberBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateLineNumber(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateEofBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateEof(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadToEndBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateReadToEnd(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadTextBuiltin(RuntimeValue fileValue, int count, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateReadText(fileValue, count, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateReadLine(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateReadLinesBuiltin(RuntimeValue fileValue, int count, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateReadLines(
            fileValue, count, location, ReportFileIOError, CreateStringArrayValue);
    }

    private RuntimeValue EvaluateSkipLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateSkipLine(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateWriteBuiltin(RuntimeValue fileValue, string text, bool appendLineBreak, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateWrite(fileValue, text, appendLineBreak, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateNewLineBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateNewLine(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateFlushBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateFlush(fileValue, location, ReportFileIOError);
    }

    private RuntimeValue EvaluateSeekFileBuiltin(RuntimeValue fileValue, RuntimeValue positionValue, TextLocation location)
    {
        var position = BuiltinFileIOHelpers.AsFilePosition(positionValue, location, (msg, loc) => ReportInvalidRuntimeState<long>(msg, loc));

        return BuiltinFileIOHelpers.EvaluateSeekFile(
            fileValue, position, location,
            ReportFileIOError,
            (funcName, argName, detail, loc) => ReportBuiltInArgumentError(funcName, argName, detail, loc));
    }

    private RuntimeValue EvaluateRewindBuiltin(RuntimeValue fileValue, TextLocation location)
    {
        return BuiltinFileIOHelpers.EvaluateRewind(fileValue, location, ReportFileIOError);
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

    private void CloseAllOpenFiles()
    {
        BuiltinFileIOHelpers.CloseAllOpenFiles(_openFiles, (file, ex) =>
            ReportFileIOError($"Не удалось закрыть файл '{file.Path}' при завершении программы: {ex.Message}", FallbackRuntimeLocation));
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
