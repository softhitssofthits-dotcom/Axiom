using Axiom.Bytecode;
using Axiom.Core;

namespace Axiom.Runtime;

public sealed partial class BytecodeVm
{
    private readonly Dictionary<string, KeyboardHandlerRegistration> _keyboardHandlers = new(StringComparer.Ordinal);
    private readonly Queue<string> _bufferedKeys = [];
    private int _keyboardHandlerDepth;

    private void ExecuteStatementBoundary(int instructionIndex)
    {
        if (_keyboardHandlerDepth > 0)
        {
            return;
        }

        var location = ResolveLocation(CurrentFrame.Routine.Id, instructionIndex);
        DispatchMouseHandlers(location);
        var deferredKeys = new List<string>();

        while (TryReadKeyboardKey(out var keyText))
        {
            var normalizedKey = KeyboardInputNormalizer.Normalize(keyText);
            if (!TryGetKeyboardHandler(normalizedKey, out var registration))
            {
                deferredKeys.Add(keyText);
                continue;
            }

            var handlerKeyText = registration.Chord.IsSingleKey ? normalizedKey : registration.Chord.CanonicalText;
            InvokeKeyboardHandler(registration, handlerKeyText, location);
        }

        foreach (var deferredKey in deferredKeys)
        {
            _bufferedKeys.Enqueue(deferredKey);
        }
    }

    private bool TryGetKeyboardHandler(string normalizedKey, out KeyboardHandlerRegistration registration)
    {
        foreach (var candidate in _keyboardHandlers.Values
                     .Where(candidate => candidate.IsEnabled && !candidate.Chord.IsSingleKey && candidate.Chord.PrimaryKey == normalizedKey && AreRequiredKeysDown(candidate.Chord))
                     .OrderByDescending(candidate => candidate.Chord.RequiredKeys.Count))
        {
            registration = candidate;
            return true;
        }

        if (_keyboardHandlers.TryGetValue(normalizedKey, out registration) && registration.IsEnabled)
        {
            return true;
        }

        if (normalizedKey != KeyboardInputNormalizer.AnyKeyHandlerName &&
            _keyboardHandlers.TryGetValue(KeyboardInputNormalizer.AnyKeyHandlerName, out registration) &&
            registration.IsEnabled)
        {
            return true;
        }

        registration = default;
        return false;
    }

    private void ExecuteRegisterKeyHandler(BytecodeCallFrame frame, int instructionIndex, BytecodeInstruction instruction)
    {
        var handlerValue = PopValue(frame, instructionIndex).AsFunction();
        var keyText = PopValue(frame, instructionIndex).AsString();

        if (string.IsNullOrWhiteSpace(keyText))
        {
            return;
        }

        var chord = KeyboardInputNormalizer.NormalizeChord(keyText);
        if (chord.IsEmpty)
        {
            return;
        }

        var passKeyText = instruction.OperandCount > 0 && instruction.GetOperand(0) != 0;
        _keyboardHandlers[chord.CanonicalText] = new KeyboardHandlerRegistration(handlerValue!, passKeyText, chord, IsEnabled: true);
    }

    private void ExecuteUnregisterKeyHandler(BytecodeCallFrame frame, int instructionIndex)
    {
        var keyText = PopValue(frame, instructionIndex).AsString();
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return;
        }

        var chord = KeyboardInputNormalizer.NormalizeChord(keyText);
        if (!chord.IsEmpty)
        {
            _keyboardHandlers.Remove(chord.CanonicalText);
        }
    }

    private void ExecutePauseKeyHandler(BytecodeCallFrame frame, int instructionIndex)
    {
        var keyText = PopValue(frame, instructionIndex).AsString();
        SetKeyboardHandlerEnabled(keyText, isEnabled: false);
    }

    private void ExecuteResumeKeyHandler(BytecodeCallFrame frame, int instructionIndex)
    {
        var keyText = PopValue(frame, instructionIndex).AsString();
        SetKeyboardHandlerEnabled(keyText, isEnabled: true);
    }

    private void SetKeyboardHandlerEnabled(string keyText, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return;
        }

        var chord = KeyboardInputNormalizer.NormalizeChord(keyText);
        if (chord.IsEmpty || !_keyboardHandlers.TryGetValue(chord.CanonicalText, out var registration))
        {
            return;
        }

        _keyboardHandlers[chord.CanonicalText] = registration with { IsEnabled = isEnabled };
    }

    private string ReadKeyboardInput()
    {
        return TryReadKeyboardKey(out var keyText)
            ? keyText
            : string.Empty;
    }

    private void ExecuteSleepInstruction(BytecodeCallFrame frame, BytecodeInstruction instruction, int instructionIndex)
    {
        if (instruction.GetOperand(0) == 0)
        {
            WaitForAnyKey();
            return;
        }

        var milliseconds = PopValue(frame, instructionIndex).AsLong();
        SleepForMilliseconds(milliseconds, ResolveLocation(frame.Routine.Id, instructionIndex));
    }

    private void WaitForAnyKey()
    {
        while (!TryReadKeyboardKey(out _))
        {
            System.Threading.Thread.Sleep(10);
        }
    }

    private void SleepForMilliseconds(long milliseconds, TextLocation location)
    {
        if (milliseconds < 0 || milliseconds > int.MaxValue)
        {
            ReportBuiltInArgumentError("SLEEP", "milliseconds", $"должен быть в диапазоне 0..{int.MaxValue}.", location);
            throw new RuntimeFailureException();
        }

        System.Threading.Thread.Sleep((int)milliseconds);
    }

    private bool IsKeyboardKeyDown(string keyText)
    {
        var chord = KeyboardInputNormalizer.NormalizeChord(keyText);
        return !chord.IsEmpty && IsChordDown(chord);
    }

    private bool ConsumeKeyboardKeyPress(string keyText)
    {
        return TryConsumeKeyboardKeyPress(keyText);
    }

    private bool TryConsumeKeyboardKeyPress(string keyText)
    {
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return false;
        }

        var targetChord = KeyboardInputNormalizer.NormalizeChord(keyText);
        if (targetChord.IsEmpty || !AreRequiredKeysDown(targetChord))
        {
            return false;
        }

        var deferredKeys = new List<string>();
        var matched = false;

        while (TryReadKeyboardKey(out var candidate))
        {
            if (!matched && KeyboardInputNormalizer.Normalize(candidate) == targetChord.PrimaryKey)
            {
                matched = true;
                continue;
            }

            deferredKeys.Add(candidate);
        }

        foreach (var deferredKey in deferredKeys)
        {
            _bufferedKeys.Enqueue(deferredKey);
        }

        return matched;
    }

    private bool IsChordDown(KeyboardInputChord chord) =>
        chord.RequiredKeys.All(IsKeyDown) && IsKeyDown(chord.PrimaryKey);

    private bool AreRequiredKeysDown(KeyboardInputChord chord) =>
        chord.RequiredKeys.All(IsKeyDown);

    private bool IsKeyDown(string normalizedKey) => _host.IsKeyDown(normalizedKey);

    private bool TryReadKeyboardKey(out string keyText)
    {
        if (_bufferedKeys.Count > 0)
        {
            keyText = _bufferedKeys.Dequeue();
            return true;
        }

        keyText = _host.ReadKey();
        return !string.IsNullOrEmpty(keyText);
    }

    private void InvokeKeyboardHandler(KeyboardHandlerRegistration registration, string normalizedKey, TextLocation location)
    {
        _keyboardHandlerDepth++;

        try
        {
            var arguments = registration.PassKeyText
                ? new[] { RuntimeValue.FromString(normalizedKey) }
                : Array.Empty<RuntimeValue>();

            _ = InvokeBytecodeFunction(registration.Handler, arguments, location);
        }
        finally
        {
            _keyboardHandlerDepth--;
        }
    }

    private readonly record struct KeyboardHandlerRegistration(RuntimeFunctionValue Handler, bool PassKeyText, KeyboardInputChord Chord, bool IsEnabled);
}
