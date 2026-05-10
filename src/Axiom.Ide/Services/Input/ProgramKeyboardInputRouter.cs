using System.Windows.Input;

namespace Axiom.Ide.Services.Input;

internal static class ProgramKeyboardInputRouter
{
    public static bool HandlePreviewKeyDown(IProgramConsoleService inputSink, Key key, ModifierKeys modifiers)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        var stateKeyText = KeyboardInputNameMapper.MapKeyStateName(key);
        if (stateKeyText is not null)
        {
            inputSink.SetKeyDown(stateKeyText);
        }

        var keyText = KeyboardInputNameMapper.MapSpecialKey(key, modifiers)
            ?? KeyboardInputNameMapper.MapModifiedPrintableKey(key, modifiers);
        if (keyText is null)
        {
            return false;
        }

        inputSink.EnqueueKey(keyText);
        return true;
    }

    public static void HandlePreviewKeyUp(IProgramConsoleService inputSink, Key key)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        var keyText = KeyboardInputNameMapper.MapKeyStateName(key);
        if (keyText is not null)
        {
            inputSink.SetKeyUp(keyText);
        }
    }

    public static bool HandlePreviewTextInput(IProgramConsoleService inputSink, string text, ModifierKeys modifiers)
    {
        ArgumentNullException.ThrowIfNull(inputSink);

        if (string.IsNullOrEmpty(text) || KeyboardInputNameMapper.HasCommandModifier(modifiers))
        {
            return false;
        }

        inputSink.EnqueueKey(text);
        return true;
    }
}
