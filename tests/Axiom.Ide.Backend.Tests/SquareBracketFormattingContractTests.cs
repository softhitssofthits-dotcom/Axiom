using Axiom.Formatting;

namespace Axiom.Ide.Backend.Tests;

public sealed class SquareBracketFormattingContractTests
{
    [Theory]
    [InlineData("dim grid [ , ] as integer", "DIM grid[,] AS INTEGER", "Dim grid[,] As Integer")]
    [InlineData("dim cube [ , , ] as integer", "DIM cube[,,] AS INTEGER", "Dim cube[,,] As Integer")]
    [InlineData("dim values[ ]=MakeFruits( )", "DIM values[] = MakeFruits()", "Dim values[] = MakeFruits()")]
    [InlineData("sub Show(byref values[ , ] as integer)", "SUB Show(BYREF values[,] AS INTEGER)", "Sub Show(Byref values[,] As Integer)")]
    [InlineData("function MakeFruits() as string [ ]", "FUNCTION MakeFruits() AS STRING[]", "Function MakeFruits() As String[]")]
    [InlineData("print grid[ -1 , 7 to -2 ]", "PRINT grid[-1, 7 TO -2]", "Print grid[-1, 7 To -2]")]
    [InlineData("print fruits[ChooseFruit()+ (offset*2)]", "PRINT fruits[ChooseFruit() + (offset * 2)]", "Print fruits[ChooseFruit() + (offset * 2)]")]
    [InlineData("print MakeFruits()[2]-other[1]", "PRINT MakeFruits()[2] - other[1]", "Print MakeFruits()[2] - other[1]")]
    [InlineData("redim preserve bag.items[1 to 3, -2 to 4]", "REDIM PRESERVE bag.items[1 TO 3, -2 TO 4]", "Redim Preserve bag.items[1 To 3, -2 To 4]")]
    [InlineData("erase bag.items [ ]", "ERASE bag.items[]", "Erase bag.items[]")]
    [InlineData("print values[Choose(index:=2)]", "PRINT values[Choose(index := 2)]", "Print values[Choose(index := 2)]")]
    [InlineData("print len(values[2])+len(\"[ 2 ]\") ' grid(2) []", "PRINT LEN(values[2]) + LEN(\"[ 2 ]\") ' grid(2) []", "Print Len(values[2]) + Len(\"[ 2 ]\") ' grid(2) []")]
    [InlineData("print $\"[ текст ] {grid[ 1 , 2 ]:F2}\"", "PRINT $\"[ текст ] {grid[1, 2]:F2}\"", "Print $\"[ текст ] {grid[1, 2]:F2}\"")]
    [InlineData("print $\"{$\"{values[ 2 ]}\"}\"", "PRINT $\"{$\"{values[2]}\"}\"", "Print $\"{$\"{values[2]}\"}\"")]
    [InlineData("print legacy( 2 )+MakeFruits( )", "PRINT legacy(2) + MakeFruits()", "Print legacy(2) + MakeFruits()")]
    [InlineData("dim fruits[] as string=[ \"яблоко\" , \"банан\" ]", "DIM fruits[] AS STRING = [\"яблоко\", \"банан\"]", "Dim fruits[] As String = [\"яблоко\", \"банан\"]")]
    [InlineData("dim empty[] as integer=[ ]", "DIM empty[] AS INTEGER = []", "Dim empty[] As Integer = []")]
    [InlineData("print [ 10 , 20 , 30 ][ 2 ]", "PRINT [10, 20, 30][2]", "Print [10, 20, 30][2]")]
    public void FormatsArrayFormsAndPreservesCalls(string source, string classic, string modern)
    {
        var formatter = new AxiomCodeFormattingService();
        Assert.Equal(classic, formatter.FormatLine(source, CodeFormattingStyle.Classic));
        Assert.Equal(modern, formatter.FormatLine(source, CodeFormattingStyle.Modern));
        Assert.Equal(classic, formatter.FormatLine(classic, CodeFormattingStyle.Classic));
        Assert.Equal(modern, formatter.FormatLine(modern, CodeFormattingStyle.Modern));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void DocumentFormattingPreservesIndentationAndLineEndings(string newline)
    {
        var formatter = new AxiomCodeFormattingService();
        var source = "sub Show(values[] as string)" + newline + "    print values [ 2 ]" + newline + "end sub" + newline;
        var expected = "SUB Show(values[] AS STRING)" + newline + "    PRINT values[2]" + newline + "END SUB" + newline;
        Assert.Equal(expected, formatter.FormatDocument(source, CodeFormattingStyle.Classic));
        Assert.Equal(expected, formatter.FormatDocument(expected, CodeFormattingStyle.Classic));
    }
}
