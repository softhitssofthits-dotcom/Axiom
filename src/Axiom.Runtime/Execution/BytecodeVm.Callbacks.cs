using Axiom.Bytecode;
using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

internal abstract record VmCallbackStep;

internal sealed record VmCallbackComplete(RuntimeValue Value) : VmCallbackStep;

internal sealed record VmCallbackInvoke(
    RuntimeFunctionValue Function,
    RuntimeValue[] Arguments,
    TextLocation Location,
    Func<RuntimeValue, VmCallbackStep> Continuation,
    bool IsInputHandler = false) : VmCallbackStep;

internal sealed record VmCallbackDeferred(Func<VmCallbackStep> Next) : VmCallbackStep;

internal sealed class VmCallbackSuspension : Exception
{
    public VmCallbackSuspension(
        RuntimeFunctionValue function,
        RuntimeValue[] arguments,
        TextLocation location,
        Func<RuntimeValue, VmCallbackStep> continuation)
    {
        Function = function;
        Arguments = arguments;
        Location = location;
        Continuation = continuation;
    }

    public RuntimeFunctionValue Function { get; }

    public RuntimeValue[] Arguments { get; }

    public TextLocation Location { get; }

    public Func<RuntimeValue, VmCallbackStep> Continuation { get; }

    public Action<RuntimeValue>? Completion { get; set; }

    public bool IsInputHandler { get; set; }
}

public sealed partial class BytecodeVm
{
    private RuntimeValue RunCallbackChain(VmCallbackStep step)
    {
        while (true)
        {
            switch (step)
            {
                case VmCallbackComplete complete:
                    return complete.Value;

                case VmCallbackDeferred deferred:
                    step = deferred.Next();
                    continue;

                case VmCallbackInvoke invoke:
                    if (invoke.Function.Function.IsBuiltIn)
                    {
                        RuntimeValue value;
                        try
                        {
                            value = InvokeBuiltinFunctionValue(invoke.Function, invoke.Arguments, invoke.Location);
                        }
                        catch (VmCallbackSuspension inner)
                        {
                            throw RebindSuspension(inner, invoke.Continuation);
                        }

                        step = invoke.Continuation(value);
                        continue;
                    }

                    throw new VmCallbackSuspension(
                        invoke.Function,
                        invoke.Arguments,
                        invoke.Location,
                        invoke.Continuation);

                default:
                    throw new InvalidOperationException($"Unsupported callback step '{step.GetType().Name}'.");
            }
        }
    }

    private void ResumeCallbackChain(VmCallbackStep step, Action<RuntimeValue> completion)
    {
        while (true)
        {
            switch (step)
            {
                case VmCallbackComplete complete:
                    completion(complete.Value);
                    return;

                case VmCallbackDeferred deferred:
                    step = deferred.Next();
                    continue;

                case VmCallbackInvoke invoke:
                    if (invoke.Function.Function.IsBuiltIn)
                    {
                        RuntimeValue value;
                        try
                        {
                            value = InvokeBuiltinFunctionValue(invoke.Function, invoke.Arguments, invoke.Location);
                        }
                        catch (VmCallbackSuspension inner)
                        {
                            var rebound = RebindSuspension(inner, invoke.Continuation);
                            rebound.Completion = completion;
                            throw rebound;
                        }

                        step = invoke.Continuation(value);
                        continue;
                    }

                    PushCallbackFrame(invoke, completion);
                    return;

                default:
                    throw new InvalidOperationException($"Unsupported callback step '{step.GetType().Name}'.");
            }
        }
    }

    private static VmCallbackStep Bind(VmCallbackStep step, Func<RuntimeValue, VmCallbackStep> next)
    {
        return step switch
        {
            VmCallbackComplete complete => next(complete.Value),
            VmCallbackDeferred deferred => new VmCallbackDeferred(() => Bind(deferred.Next(), next)),
            VmCallbackInvoke invoke => new VmCallbackInvoke(
                invoke.Function,
                invoke.Arguments,
                invoke.Location,
                value => Bind(invoke.Continuation(value), next),
                invoke.IsInputHandler),
            _ => throw new InvalidOperationException($"Unsupported callback step '{step.GetType().Name}'.")
        };
    }

    private static VmCallbackSuspension RebindSuspension(
        VmCallbackSuspension suspension,
        Func<RuntimeValue, VmCallbackStep> next)
    {
        return new VmCallbackSuspension(
            suspension.Function,
            suspension.Arguments,
            suspension.Location,
            value => Bind(suspension.Continuation(value), next))
        {
            Completion = suspension.Completion,
            IsInputHandler = suspension.IsInputHandler
        };
    }

    private void PushCallbackFrame(VmCallbackInvoke invoke, Action<RuntimeValue> completion)
    {
        var function = invoke.Function;
        if (!TryResolveFunctionRoutine(function, invoke.Location, out var routine))
        {
            return;
        }

        if (_callStack.Count >= MaxCallDepth)
        {
            ReportCallStackOverflow(function.Function.Name, invoke.Location);
            throw new RuntimeFailureException();
        }

        var captures = GetFunctionCaptures(function);
        // Значения копируются до изъятия кадра из пула, как в прежнем общем
        // пути: копирование может завершиться ошибкой или вызвать проверку
        // отмены, и тогда кадр остаётся в пуле.
        var argumentCopies = new RuntimeValue[invoke.Arguments.Length];
        for (var index = 0; index < argumentCopies.Length; index++)
        {
            argumentCopies[index] = invoke.Arguments[index].CopyForStorage();
        }

        BytecodeCallFrame frame;
        var template = _prepared.GetFrameTemplate(routine.Id);
        if (_framePool.TryTake(routine.Id, out var cachedFrame))
        {
            cachedFrame.Reset(captures, invoke.Location, template.DefaultLocals);
            cachedFrame.InstallParameterValues(argumentCopies, null, byValValuesAreSnapshots: true);
            frame = cachedFrame;
        }
        else
        {
            var locals = routine.LocalSlotCount == 0
                ? Array.Empty<VmValue>()
                : (VmValue[])template.DefaultLocals.Clone();
            frame = new BytecodeCallFrame(
                routine,
                locals,
                template.LocalFixedStringLengths,
                captures,
                invoke.Location,
                routine.MaxEvaluationStackDepth,
                _prepared.GetDecodedInstructions(routine.Id, _fuseDecodedInstructions));
            frame.InstallParameterValues(argumentCopies, null, byValValuesAreSnapshots: true);
        }

        frame.CallbackContinuation = invoke.Continuation;
        frame.CallbackCompletion = completion;
        frame.IsInputHandlerFrame = invoke.IsInputHandler;
        _callStack.Push(frame);
    }

    private void PushSuspendedCallback(VmCallbackSuspension suspension)
    {
        var invoke = new VmCallbackInvoke(
            suspension.Function,
            suspension.Arguments,
            suspension.Location,
            suspension.Continuation,
            suspension.IsInputHandler);
        PushCallbackFrame(invoke, suspension.Completion ?? (_ => { }));
    }

    private bool TryResolveFunctionRoutine(
        RuntimeFunctionValue functionValue,
        TextLocation callSiteLocation,
        out BytecodeRoutine routine)
    {
        routine = null!;
        if (functionValue.Function.IsBuiltIn)
        {
            return false;
        }

        var resolved = functionValue.BytecodeRoutine;
        if (resolved is null && !_prepared.RoutinesBySymbol.TryGetValue(functionValue.Function, out resolved))
        {
            ReportInvalidRuntimeState<RuntimeValue>(
                $"Для вызова '{functionValue.Function.Name}' отсутствует байткодная процедура.",
                callSiteLocation);
            return false;
        }

        routine = resolved!;
        return true;
    }

    private static BytecodeVariableSlot[] GetFunctionCaptures(RuntimeFunctionValue functionValue)
    {
        return functionValue.BytecodeCapturedSlots switch
        {
            null => Array.Empty<BytecodeVariableSlot>(),
            BytecodeVariableSlot[] captureArray => captureArray,
            _ => functionValue.BytecodeCapturedSlots.ToArray()
        };
    }

    private bool IsInputHandlerFrameActive()
    {
        foreach (var frame in _callStack)
        {
            if (frame.IsInputHandlerFrame)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasActiveCallbackContinuation()
    {
        foreach (var frame in _callStack)
        {
            if (frame.HasCallbackContinuation)
            {
                return true;
            }
        }

        return false;
    }
}
