using Axiom.Core;
using Axiom.Semantics;

namespace Axiom.Runtime;

public sealed partial class Interpreter
{
    private readonly Dictionary<string, KeyboardHandlerRegistration> _keyboardHandlers = new(StringComparer.Ordinal);
    private readonly Queue<string> _bufferedKeys = [];
    private int _keyboardHandlerDepth;

    private void DispatchKeyboardHandlers(TextLocation callSiteLocation)
    {
        if (_keyboardHandlerDepth > 0)
        {
            return;
        }

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
            InvokeKeyboardHandler(registration, handlerKeyText, callSiteLocation);
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

    private void RegisterKeyboardHandler(string keyText, RuntimeFunctionValue handler, bool passKeyText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyText);
        ArgumentNullException.ThrowIfNull(handler);
        var chord = KeyboardInputNormalizer.NormalizeChord(keyText);
        if (chord.IsEmpty)
        {
            return;
        }

        _keyboardHandlers[chord.CanonicalText] = new KeyboardHandlerRegistration(handler, passKeyText, chord, IsEnabled: true);
    }

    private void UnregisterKeyboardHandler(string keyText)
    {
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

    private void PauseKeyboardHandler(string keyText)
    {
        SetKeyboardHandlerEnabled(keyText, isEnabled: false);
    }

    private void ResumeKeyboardHandler(string keyText)
    {
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

    private void InvokeKeyboardHandler(KeyboardHandlerRegistration registration, string normalizedKey, TextLocation callSiteLocation)
    {
        _keyboardHandlerDepth++;

        try
        {
            var arguments = registration.PassKeyText
                ? new[] { RuntimeValue.FromString(normalizedKey) }
                : Array.Empty<RuntimeValue>();

            _ = InvokeUserRoutine(registration.Handler, arguments, callSiteLocation);
        }
        finally
        {
            _keyboardHandlerDepth--;
        }
    }

    private readonly record struct KeyboardHandlerRegistration(RuntimeFunctionValue Handler, bool PassKeyText, KeyboardInputChord Chord, bool IsEnabled);
}
