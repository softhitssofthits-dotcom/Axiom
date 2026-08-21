using Axiom.Execution;
using Axiom.Ide.Editor.Identifiers;

namespace Axiom.Ide.Tests;

public sealed class SmartIdentifierServiceTests
{
    private readonly AxiomSmartIdentifierService _service = new(
        new IdentifierNormalizationService());

    [Fact]
    public void NormalizeLine_UsesDeclarationCasingForVariableReference()
    {
        const string source = "DIM MyFirstDay AS INTEGER\r\nmyFIRSTday = 10";

        var normalizedLine = _service.NormalizeLine(source, "DIM MyFirstDay AS INTEGER\r\n".Length, "myFIRSTday = 10".Length);

        Assert.Equal("MyFirstDay = 10", normalizedLine);
    }

    [Fact]
    public void NormalizeDocument_UsesDeclarationCasingForConstArrayFunctionAndParameter()
    {
        const string source =
            "CONST MaxValue AS INTEGER = 10\r\n" +
            "DIM Scores(1 TO 3) AS INTEGER\r\n" +
            "FUNCTION Mix(MyFirstDay AS INTEGER) AS INTEGER\r\n" +
            "    scores(1) = maxVALUE\r\n" +
            "    mix = myfirstday + SCORES(1)\r\n" +
            "END FUNCTION";

        var normalized = _service.NormalizeDocument(source);

        Assert.Equal(
            "CONST MaxValue AS INTEGER = 10\r\n" +
            "DIM Scores(1 TO 3) AS INTEGER\r\n" +
            "FUNCTION Mix(MyFirstDay AS INTEGER) AS INTEGER\r\n" +
            "    Scores(1) = MaxValue\r\n" +
            "    Mix = MyFirstDay + Scores(1)\r\n" +
            "END FUNCTION",
            normalized);
    }

    [Fact]
    public void NormalizeDocument_DoesNotTouchCommentsStringsOrUndeclaredNames()
    {
        const string source =
            "DIM MyFirstDay AS INTEGER\r\n" +
            "PRINT \"myFIRSTday\"\r\n" +
            "' myFIRSTday\r\n" +
            "unknownName = myFIRSTday";

        var normalized = _service.NormalizeDocument(source);

        Assert.Equal(
            "DIM MyFirstDay AS INTEGER\r\n" +
            "PRINT \"myFIRSTday\"\r\n" +
            "' myFIRSTday\r\n" +
            "unknownName = MyFirstDay",
            normalized);
    }

    [Fact]
    public void NormalizeDocument_UsesForLoopDeclarationCasingInsideLoopBody()
    {
        const string source =
            "FOR ItemIndex = 1 TO 3\r\n" +
            "    print itemINDEX\r\n" +
            "NEXT";

        var normalized = _service.NormalizeDocument(source);

        Assert.Equal(
            "FOR ItemIndex = 1 TO 3\r\n" +
            "    print ItemIndex\r\n" +
            "NEXT",
            normalized);
    }
}
