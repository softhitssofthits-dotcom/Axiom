using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.Tests;

public sealed class CodeFormattingServiceTests
{
    private readonly AxiomCodeFormattingService _service = new();

    [Fact]
    public void FormatLine_Classic_UppercasesKeywordsAndAddsOperatorSpacing()
    {
        var result = _service.FormatLine("dim a as integer=1", CodeFormattingStyle.Classic);

        Assert.Equal("DIM a AS INTEGER = 1", result);
    }

    [Fact]
    public void FormatLine_Classic_FormatsPrintExpression()
    {
        var result = _service.FormatLine("print a=b*2", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a = b * 2", result);
    }

    [Fact]
    public void FormatLine_Classic_UppercasesLogicalOperatorKeywords()
    {
        var result = _service.FormatLine("print a eqv b imp c", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a EQV b IMP c", result);
    }

    [Fact]
    public void FormatLine_Classic_UppercasesBuiltInCallsButNotPlainIdentifiers()
    {
        var builtInCall = _service.FormatLine("print len(\"x\")+1", CodeFormattingStyle.Classic);
        var plainIdentifier = _service.FormatLine("len=1", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT LEN(\"x\") + 1", builtInCall);
        Assert.Equal("len = 1", plainIdentifier);
    }

    [Fact]
    public void FormatLine_Classic_PreservesStringsAndUppercasesRemComments()
    {
        var result = _service.FormatLine("print \"rem stays\" rem note", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT \"rem stays\" REM note", result);
    }

    [Fact]
    public void FormatLine_Classic_LeavesMalformedStringUnchanged()
    {
        const string source = "print \"unterminated";

        var result = _service.FormatLine(source, CodeFormattingStyle.Classic);

        Assert.Equal(source, result);
    }

    [Fact]
    public void FormatLine_Modern_UsesTitleCaseForKeywordsTypesAndBuiltIns()
    {
        var result = _service.FormatLine("dim a as integer= len(\"x\")+1", CodeFormattingStyle.Modern);

        Assert.Equal("Dim a As Integer = Len(\"x\") + 1", result);
    }

    [Fact]
    public void FormatLine_Modern_NormalizesExtraSpacesAndRemComment()
    {
        var result = _service.FormatLine("   print   a  +   3   rem note", CodeFormattingStyle.Modern);

        Assert.Equal("   Print a + 3 Rem note", result);
    }

    [Fact]
    public void FormatDocument_Modern_FormatsAllLinesAndPreservesLineEndings()
    {
        const string source = "print len(\"x\")+1\r\ndim a as integer=1\r\n";

        var result = _service.FormatDocument(source, CodeFormattingStyle.Modern);

        Assert.Equal("Print Len(\"x\") + 1\r\nDim a As Integer = 1\r\n", result);
    }

    [Fact]
    public void FormatLine_Modern_FormatsFileKeywordsTypesAndBuiltIns()
    {
        var fileDeclaration = _service.FormatLine("dim f as file", CodeFormattingStyle.Modern);
        var fileStatement = _service.FormatLine("open path for output as f", CodeFormattingStyle.Modern);
        var builtInCall = _service.FormatLine("print copyfile(sourcePath,destinationPath,true)", CodeFormattingStyle.Modern);
        var arrayStatement = _service.FormatLine("redim preserve items(1 to 0)", CodeFormattingStyle.Modern);

        Assert.Equal("Dim f As File", fileDeclaration);
        Assert.Equal("Open path For Output As f", fileStatement);
        Assert.Equal("Print Copyfile(sourcePath, destinationPath, True)", builtInCall);
        Assert.Equal("Redim Preserve items(1 To 0)", arrayStatement);
    }

    [Fact]
    public void FormatLine_Classic_FormatsScreenKeywordsAndBuiltIns()
    {
        var screenStatement = _service.FormatLine("screen 5", CodeFormattingStyle.Classic);
        var graphicsStatement = _service.FormatLine("pset 10,20,color", CodeFormattingStyle.Classic);
        var sleepStatement = _service.FormatLine("sleep 500", CodeFormattingStyle.Classic);
        var builtInCall = _service.FormatLine("print screenmode(),screenwidth(),screenheight()", CodeFormattingStyle.Classic);

        Assert.Equal("SCREEN 5", screenStatement);
        Assert.Equal("PSET 10, 20, COLOR", graphicsStatement);
        Assert.Equal("SLEEP 500", sleepStatement);
        Assert.Equal("PRINT SCREENMODE(), SCREENWIDTH(), SCREENHEIGHT()", builtInCall);
    }

    [Fact]
    public void FormatLine_Classic_FormatsShiftPowerAndIntegerDivisionOperators()
    {
        var result = _service.FormatLine("print(a<<2)+(b>>1)+2^3+7\\2", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT (a << 2) + (b >> 1) + 2 ^ 3 + 7 \\ 2", result);
    }

    [Fact]
    public void FormatLine_Classic_CollapsesSplitShiftOperatorsWithoutInnerSpaces()
    {
        var result = _service.FormatLine("print a <    < 2, b >       > 1", CodeFormattingStyle.Classic);

        Assert.Equal("PRINT a << 2, b >> 1", result);
    }
}
