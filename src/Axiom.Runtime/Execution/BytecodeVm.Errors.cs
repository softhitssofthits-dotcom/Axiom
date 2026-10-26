using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Runtime.Builtins;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private void ExecutePushErrorHandler(BytecodeCallFrame frame, BytecodeInstruction instruction)
    {
        frame.PushErrorHandler(new BytecodeErrorHandlerFrame(
            instruction.GetOperand(0),
            instruction.GetOperand(1),
            instruction.GetOperand(2),
            instruction.GetOperand(3),
            instruction.GetOperand(4)));
    }

    private void ExecuteRaiseError(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        var mode = instruction.GetOperand(0);
        if (mode == 2)
        {
            var existingError = PopValue(frame, instructionIndex).AsRuntimeError();
            ReportRuntimeDiagnostic(
                existingError.Code,
                existingError.Message,
                new TextLocation(new TextSpan(0, 0), existingError.Line, existingError.Column, existingError.Document),
                preserveMessage: true);
            throw new RuntimeFailureException();
        }

        var hasMessage = mode != 0;
        var message = hasMessage ? PopValue(frame, instructionIndex).AsString() : null;
        var code = PopValue(frame, instructionIndex).AsString();
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        if (string.IsNullOrWhiteSpace(code))
        {
            ReportRuntimeDiagnostic(
                DiagnosticCodes.BuiltinArgument,
                "ERROR: код пользовательской ошибки не должен быть пустым.",
                location);
            throw new RuntimeFailureException();
        }

        ReportRuntimeDiagnostic(code, message ?? $"User error {code}.", location, preserveMessage: true);
        throw new RuntimeFailureException();
    }

    private void ExecuteEndFinally(BytecodeCallFrame frame)
    {
        var handler = frame.PeekErrorHandler();
        if (handler is null || handler.State != BytecodeErrorHandlerState.ExecutingFinally)
        {
            return;
        }

        if (handler.HasPendingReturn)
        {
            var returnValue = handler.PendingReturnValue ?? RuntimeValue.Void;
            _ = frame.TryPopErrorHandler(out _);

            if (TryEnterFinallyForReturn(frame, returnValue))
            {
                return;
            }

            CompleteReturn(frame, returnValue);
            return;
        }

        if (!handler.HasPendingUnhandledFailure)
        {
            return;
        }

        _ = frame.TryPopErrorHandler(out _);
        throw new RuntimeFailureException();
    }

    private void HandleStringResourceFailure(string message, BytecodeCallFrame frame, int instructionIndex, int minimumCallStackDepth)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.BuiltinArgument, message, ResolveLocation(frame.Routine.Id, instructionIndex));
        if (!TryHandleRuntimeFailure(minimumCallStackDepth)) throw new RuntimeFailureException();
    }

    private void HandleArrayRuntimeFailure(
        Exception failure,
        BytecodeCallFrame frame,
        int instructionIndex,
        int minimumCallStackDepth)
    {
        var location = ResolveLocation(frame.Routine.Id, instructionIndex);

        switch (failure)
        {
            case ArrayAllocationFailureException allocation:
                ReportRuntimeDiagnostic(DiagnosticCodes.ArrayAllocation, allocation.Message, location);
                break;

            case ExpiredArrayViewException:
                ReportRuntimeDiagnostic(
                    DiagnosticCodes.ExpiredArrayView,
                    "Подмассив-представление больше не связан с исходным массивом: исходный массив был переразмерен или освобождён.",
                    location);
                break;

            default:
                throw new InvalidOperationException("Неизвестный отказ массива времени выполнения.", failure);
        }

        if (!TryHandleRuntimeFailure(minimumCallStackDepth))
        {
            throw new RuntimeFailureException();
        }
    }

    // Логическое превышение предела множества сообщается как ресурсный отказ:
    // получатель при этом не изменён, поэтому TRY/CATCH продолжает работу с
    // прежним составом.
    private void HandleSetRuntimeFailure(
        SetEntryLimitException failure,
        BytecodeCallFrame frame,
        int instructionIndex,
        int minimumCallStackDepth)
    {
        ArgumentNullException.ThrowIfNull(failure);

        ReportRuntimeDiagnostic(
            DiagnosticCodes.SetEntryLimit,
            failure.Message,
            ResolveLocation(frame.Routine.Id, instructionIndex));

        if (!TryHandleRuntimeFailure(minimumCallStackDepth))
        {
            throw new RuntimeFailureException();
        }
    }

    private bool TryHandleRuntimeFailure(int minimumCallStackDepth)    {
        while (_callStack.Count > minimumCallStackDepth)
        {
            var frame = CurrentFrame;
            while (frame.PeekErrorHandler() is { } handler)
            {
                switch (handler.State)
                {
                    case BytecodeErrorHandlerState.Active when handler.CatchTarget >= 0:
                        var error = ConsumeLastRuntimeError();
                        frame.EvaluationStack.Clear();
                        StoreErrorHandlerCatchValue(frame, handler, error);
                        handler.State = BytecodeErrorHandlerState.ExecutingCatch;
                        frame.InstructionPointer = handler.CatchTarget;
                        return true;

                    case BytecodeErrorHandlerState.Active when handler.FinallyTarget >= 0:
                    case BytecodeErrorHandlerState.ExecutingCatch when handler.FinallyTarget >= 0:
                        frame.EvaluationStack.Clear();
                        handler.State = BytecodeErrorHandlerState.ExecutingFinally;
                        handler.HasPendingUnhandledFailure = true;
                        frame.InstructionPointer = handler.FinallyTarget;
                        return true;

                    default:
                        _ = frame.TryPopErrorHandler(out _);
                        break;
                }
            }

            _callStack.Pop();
            CacheReusableFrame(frame);
        }

        return false;
    }
    private RuntimeValue ConsumeLastRuntimeError()
    {
        var diagnostic = _lastRuntimeFailureDiagnostic;
        var errorMessage = _lastRuntimeErrorMessage;
        _lastRuntimeFailureDiagnostic = null;
        _lastRuntimeErrorMessage = null;

        if (diagnostic is null)
        {
            return RuntimeValue.FromRuntimeError(new RuntimeErrorValue(DiagnosticCodes.InvalidRuntimeState, "Runtime failure.", 0, 0));
        }

        _diagnostics.Remove(diagnostic);
        return RuntimeValue.FromRuntimeError(new RuntimeErrorValue(
            diagnostic.Code,
            errorMessage ?? RuntimeErrorMessageFormatter.Format(diagnostic.Code, diagnostic.Message),
            diagnostic.Location.Line,
            diagnostic.Location.Column,
            diagnostic.Location.Document));
    }

    private void StoreErrorHandlerCatchValue(BytecodeCallFrame frame, BytecodeErrorHandlerFrame handler, RuntimeValue value)
    {
        switch (handler.CatchTargetKind)
        {
            case 0:
                StoreGlobalUnchecked(handler.CatchTargetSlot, value);
                break;

            case 1:
                frame.StoreLocalValue(handler.CatchTargetSlot, value);
                break;

            case 2:
                frame.Arguments[handler.CatchTargetSlot].Store(value);
                break;

            case 3:
                frame.Captures[handler.CatchTargetSlot].Store(value);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    private void ReportDivisionByZero(TextLocation location)
    {
        RuntimeArithmeticHelpers.ReportDivisionByZero(location, ReportRuntimeDiagnostic);
    }

    private void ReportNumericOverflow(string detail, TypeSymbol type, TextLocation location)
    {
        RuntimeArithmeticHelpers.ReportNumericOverflow(detail, type, location, ReportRuntimeDiagnostic);
    }

    private void ReportEmptyFunctionValue(TextLocation location)
    {
        ReportRuntimeDiagnostic(
            DiagnosticCodes.EmptyFunctionValue,
            "Значение функционального типа не содержит функции.",
            location);
    }

    private void ReportCallStackOverflow(string functionName, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            DiagnosticCodes.CallStackOverflow,
            $"Превышена максимальная глубина стека вызовов ({MaxCallDepth}) при вызове '{functionName}'. Возможно, программа содержит бесконечную рекурсию.",
            location);
    }

    private void ReportBuiltInArgumentError(string functionName, string argumentName, string detail, TextLocation location)
    {
        ReportRuntimeDiagnostic(
            DiagnosticCodes.BuiltinArgument,
            $"Функция {functionName}: аргумент '{argumentName}' {detail}",
            location);
    }

    private void ReportResultOverflow(string message, TextLocation location)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.NumericOverflow, message, location);
    }

    private void ReportInvalidRuntimeState(string message, TextLocation? location = null)
    {
        ReportRuntimeDiagnostic(DiagnosticCodes.InvalidRuntimeState, message, location ?? FallbackRuntimeLocation);
    }

    private void ReportInvalidRuntimeStateWithChain(
        string message,
        TextLocation location,
        IReadOnlyList<RuntimeCallChainFrame> extraChainFrames)
    {
        var diagnostic = new Diagnostic(
            DiagnosticCodes.InvalidRuntimeState,
            AppendCallChain(message, extraChainFrames),
            location);
        _diagnostics.Report(diagnostic);
        _lastRuntimeFailureDiagnostic = diagnostic;
        _lastRuntimeErrorMessage = RuntimeErrorMessageFormatter.Format(diagnostic.Code, diagnostic.Message);
    }

    private T ReportInvalidRuntimeState<T>(string message, TextLocation? location = null)
    {
        ReportInvalidRuntimeState(message, location);
        throw new RuntimeFailureException();
    }

    private void ReportRuntimeDiagnostic(string code, string message, TextLocation location)
        => ReportRuntimeDiagnostic(code, message, location, preserveMessage: false);

    private void ReportRuntimeDiagnostic(string code, string message, TextLocation location, bool preserveMessage)
    {
        var diagnostic = new Diagnostic(code, AppendCallChain(message), location);
        _diagnostics.Report(diagnostic);
        _lastRuntimeFailureDiagnostic = diagnostic;
        _lastRuntimeErrorMessage = preserveMessage ? message : RuntimeErrorMessageFormatter.Format(code, diagnostic.Message);
    }

    private string AppendCallChain(string message, IReadOnlyList<RuntimeCallChainFrame>? extraChainFrames = null)
    {
        // Точка входа не является пользовательской процедурой и в цепочку не входит:
        // кадры начинаются с первой вызванной процедуры.
        var frames = new List<RuntimeCallChainFrame>(_callStack.Count + (extraChainFrames?.Count ?? 0));
        if (extraChainFrames is not null)
        {
            frames.AddRange(extraChainFrames);
        }

        foreach (var frame in _callStack)
        {
            if (frame.Routine.IsEntryPoint)
            {
                continue;
            }

            frames.Add(new RuntimeCallChainFrame(frame.Routine.Symbol, frame.Routine.Name, frame.CallSiteLocation));
        }

        return RuntimeCallChainFormatter.AppendCallChain(message, frames);
    }

}
