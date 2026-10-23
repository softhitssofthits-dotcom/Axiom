using Axiom.Formatting;

namespace Axiom.Ide.Backend.Tests;

public sealed class SquareBracketFormattingGuardTests
{
    [Theory]
    [InlineData("print fruits[ 2 ]", "PRINT fruits[2]", "Print fruits[2]")]
    [InlineData("dim fruits[ ] as string", "DIM fruits[] AS STRING", "Dim fruits[] As String")]
    [InlineData("print grid[1,2]", "PRINT grid[1, 2]", "Print grid[1, 2]")]
    [InlineData("print MakeFruits ( )[ 2 ]", "PRINT MakeFruits()[2]", "Print MakeFruits()[2]")]
    [InlineData("print $\"Фрукт: {fruits[ 2 ]}\"", "PRINT $\"Фрукт: {fruits[2]}\"", "Print $\"Фрукт: {fruits[2]}\"")]
    public void SquareTokensAreFormattedAndNeverDropped(string source, string classic, string modern)
    {
        var formatter = new AxiomCodeFormattingService();
        Assert.Equal(classic, formatter.FormatLine(source, CodeFormattingStyle.Classic));
        Assert.Equal(modern, formatter.FormatLine(source, CodeFormattingStyle.Modern));
        Assert.True(formatter.IsLineSyntaxValid(source));
        Assert.Equal(classic, formatter.FormatLine(classic, CodeFormattingStyle.Classic));
        Assert.Equal(modern, formatter.FormatLine(modern, CodeFormattingStyle.Modern));
    }

    [Theory]
    [InlineData("print fruits[2")]
    [InlineData("print fruits[2)")]
    [InlineData("print fruits]2[")]
    [InlineData("print fruits[ @ ]")]
    public void IncompleteOrInvalidSquareTokensRemainUntouched(string source)
    {
        var formatter = new AxiomCodeFormattingService();
        foreach (var style in Enum.GetValues<CodeFormattingStyle>())
        {
            Assert.Equal(source, formatter.FormatLine(source, style));
        }
        Assert.False(formatter.IsLineSyntaxValid(source));
    }
}
