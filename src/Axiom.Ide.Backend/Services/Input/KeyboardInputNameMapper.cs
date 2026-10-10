using System.Windows.Input;

namespace Axiom.Ide.Services.Input;

internal static class KeyboardInputNameMapper
{
    public static string? MapKeyStateName(Key key)
    {
        return key switch
        {
            Key.Left => "LEFT",
            Key.Right => "RIGHT",
            Key.Up => "UP",
            Key.Down => "DOWN",
            Key.Escape => "ESC",
            Key.Enter or Key.Return => "ENTER",
            Key.Back => "BACKSPACE",
            Key.Tab => "TAB",
            Key.Delete => "DELETE",
            Key.Insert => "INSERT",
            Key.Home => "HOME",
            Key.End => "END",
            Key.PageUp => "PAGEUP",
            Key.PageDown => "PAGEDOWN",
            Key.Space => "SPACE",
            Key.LeftShift or Key.RightShift => "SHIFT",
            Key.LeftCtrl or Key.RightCtrl => "CTRL",
            Key.LeftAlt or Key.RightAlt => "ALT",
            >= Key.A and <= Key.Z => ((char)('A' + key - Key.A)).ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((char)('0' + key - Key.NumPad0)).ToString(),
            >= Key.F1 and <= Key.F12 => $"F{key - Key.F1 + 1}",
            _ => null
        };
    }

    public static string? MapSpecialKey(Key key, ModifierKeys modifiers)
    {
        if (HasWindowsModifier(modifiers))
        {
            return null;
        }

        return key switch
        {
            Key.Left => "LEFT",
            Key.Right => "RIGHT",
            Key.Up => "UP",
            Key.Down => "DOWN",
            Key.Escape => "ESC",
            Key.Enter or Key.Return => "ENTER",
            Key.Back => "BACKSPACE",
            Key.Tab => "TAB",
            Key.Space => "SPACE",
            Key.Delete => "DELETE",
            Key.Insert => "INSERT",
            Key.Home => "HOME",
            Key.End => "END",
            Key.PageUp => "PAGEUP",
            Key.PageDown => "PAGEDOWN",
            >= Key.F1 and <= Key.F12 => $"F{key - Key.F1 + 1}",
            _ => null
        };
    }

    public static string? MapModifiedPrintableKey(Key key, ModifierKeys modifiers)
    {
        if (HasWindowsModifier(modifiers) || (modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == ModifierKeys.None)
        {
            return null;
        }

        return key switch
        {
            Key.Space => "SPACE",
            >= Key.A and <= Key.Z => ((char)('A' + key - Key.A)).ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => ((char)('0' + key - Key.NumPad0)).ToString(),
            _ => null
        };
    }

    public static bool HasCommandModifier(ModifierKeys modifiers)
    {
        const ModifierKeys commandModifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows;
        return (modifiers & commandModifiers) != ModifierKeys.None;
    }

    private static bool HasWindowsModifier(ModifierKeys modifiers) =>
        (modifiers & ModifierKeys.Windows) != ModifierKeys.None;
}
