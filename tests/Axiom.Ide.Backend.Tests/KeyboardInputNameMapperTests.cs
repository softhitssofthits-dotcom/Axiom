using System.Windows.Input;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Backend.Tests;

public sealed class KeyboardInputNameMapperTests
{
    [Theory]
    [InlineData(Key.Left, "LEFT")]
    [InlineData(Key.Right, "RIGHT")]
    [InlineData(Key.Up, "UP")]
    [InlineData(Key.Down, "DOWN")]
    [InlineData(Key.Escape, "ESC")]
    [InlineData(Key.Enter, "ENTER")]
    [InlineData(Key.Back, "BACKSPACE")]
    [InlineData(Key.Space, "SPACE")]
    [InlineData(Key.F1, "F1")]
    [InlineData(Key.F12, "F12")]
    public void MapSpecialKey_ReturnsStableInKeyName(Key key, string expected)
    {
        Assert.Equal(expected, KeyboardInputNameMapper.MapSpecialKey(key, ModifierKeys.None));
    }

    [Fact]
    public void MapSpecialKey_AllowsControlAndAltModifiers()
    {
        Assert.Equal("LEFT", KeyboardInputNameMapper.MapSpecialKey(Key.Left, ModifierKeys.Control));
        Assert.Equal("F1", KeyboardInputNameMapper.MapSpecialKey(Key.F1, ModifierKeys.Alt));
        Assert.Equal("DELETE", KeyboardInputNameMapper.MapSpecialKey(Key.Delete, ModifierKeys.Control | ModifierKeys.Alt));
    }

    [Fact]
    public void MapSpecialKey_IgnoresWindowsModifier()
    {
        Assert.Null(KeyboardInputNameMapper.MapSpecialKey(Key.Left, ModifierKeys.Windows));
    }

    [Fact]
    public void MapSpecialKey_IgnoresPrintableKeys()
    {
        Assert.Null(KeyboardInputNameMapper.MapSpecialKey(Key.A, ModifierKeys.None));
    }

    [Theory]
    [InlineData(Key.Left, "LEFT")]
    [InlineData(Key.Space, "SPACE")]
    [InlineData(Key.LeftCtrl, "CTRL")]
    [InlineData(Key.RightCtrl, "CTRL")]
    [InlineData(Key.LeftShift, "SHIFT")]
    [InlineData(Key.RightShift, "SHIFT")]
    [InlineData(Key.LeftAlt, "ALT")]
    [InlineData(Key.A, "A")]
    [InlineData(Key.Z, "Z")]
    [InlineData(Key.D1, "1")]
    [InlineData(Key.NumPad9, "9")]
    [InlineData(Key.F5, "F5")]
    public void MapKeyStateName_ReturnsStableKeyDownName(Key key, string expected)
    {
        Assert.Equal(expected, KeyboardInputNameMapper.MapKeyStateName(key));
    }

    [Theory]
    [InlineData(Key.A, ModifierKeys.Control, "A")]
    [InlineData(Key.Z, ModifierKeys.Alt, "Z")]
    [InlineData(Key.D1, ModifierKeys.Control | ModifierKeys.Alt, "1")]
    [InlineData(Key.NumPad9, ModifierKeys.Control, "9")]
    [InlineData(Key.Space, ModifierKeys.Control, "SPACE")]
    public void MapModifiedPrintableKey_ReturnsStableKeyPressName(Key key, ModifierKeys modifiers, string expected)
    {
        Assert.Equal(expected, KeyboardInputNameMapper.MapModifiedPrintableKey(key, modifiers));
    }

    [Fact]
    public void MapModifiedPrintableKey_RequiresControlOrAltAndIgnoresWindowsModifier()
    {
        Assert.Null(KeyboardInputNameMapper.MapModifiedPrintableKey(Key.A, ModifierKeys.None));
        Assert.Null(KeyboardInputNameMapper.MapModifiedPrintableKey(Key.A, ModifierKeys.Shift));
        Assert.Null(KeyboardInputNameMapper.MapModifiedPrintableKey(Key.A, ModifierKeys.Control | ModifierKeys.Windows));
    }
}
