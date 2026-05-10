using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class KeyboardInputNormalizerTests
{
    [Theory]
    [InlineData("LEFT", "LEFT")]
    [InlineData("left", "LEFT")]
    [InlineData("LeftArrow", "LEFT")]
    [InlineData("ESCAPE", "ESC")]
    [InlineData("return", "ENTER")]
    [InlineData("DEL", "DELETE")]
    [InlineData("INS", "INSERT")]
    [InlineData("PAGE UP", "PAGEUP")]
    [InlineData("page-down", "PAGEDOWN")]
    [InlineData("space bar", "SPACE")]
    [InlineData("CONTROL", "CTRL")]
    [InlineData("control-key", "CTRL")]
    [InlineData("ALT_KEY", "ALT")]
    [InlineData("f12", "F12")]
    public void Normalize_MapsNamedKeyAliasesToCanonicalNames(string value, string expected)
    {
        Assert.Equal(expected, KeyboardInputNormalizer.Normalize(value));
    }

    [Fact]
    public void Normalize_DoesNotReservePrintableSingleCharacterNames()
    {
        Assert.Equal("a", KeyboardInputNormalizer.Normalize("a"));
        Assert.Equal("A", KeyboardInputNormalizer.Normalize("A"));
        Assert.Equal("a", KeyboardInputNormalizer.Normalize(" a "));
    }

    [Fact]
    public void CanonicalNamedKeyNames_ContainsDocumentedNames()
    {
        Assert.Contains("LEFT", KeyboardInputNormalizer.CanonicalNamedKeyNames);
        Assert.Contains("CTRL", KeyboardInputNormalizer.CanonicalNamedKeyNames);
        Assert.Contains("F12", KeyboardInputNormalizer.CanonicalNamedKeyNames);
    }

    [Fact]
    public void NormalizeChord_UsesLastPartAsPrimaryKey()
    {
        var chord = KeyboardInputNormalizer.NormalizeChord(" control + alt + del ");

        Assert.Equal("CTRL+ALT+DELETE", chord.CanonicalText);
        Assert.Equal("DELETE", chord.PrimaryKey);
        Assert.Equal(["CTRL", "ALT"], chord.RequiredKeys);
    }

    [Fact]
    public void NormalizeChord_AllowsLongChordStrings()
    {
        var chord = KeyboardInputNormalizer.NormalizeChord("CTRL+F+A+X");

        Assert.Equal("CTRL+F+A+X", chord.CanonicalText);
        Assert.Equal("X", chord.PrimaryKey);
        Assert.Equal(["CTRL", "F", "A"], chord.RequiredKeys);
    }
}
