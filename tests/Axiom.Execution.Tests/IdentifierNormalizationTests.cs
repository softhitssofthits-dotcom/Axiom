namespace Axiom.Execution.Tests;

public sealed class IdentifierNormalizationTests
{
    [Theory]
    [InlineData("SUB Move(BYREF walker AS Walker)\nEND SUB\n")]
    [InlineData("DIM walker AS Walker\n")]
    [InlineData("DIM walker AS INTEGER\nFUNCTION Read() AS Walker\nEND FUNCTION\n")]
    [InlineData("DIM walker AS INTEGER\nDIM values[] AS Walker\n")]
    [InlineData("DIM walker AS INTEGER\nDIM choose AS FUNCTION(Walker) AS Walker\n")]
    [InlineData("DIM walker AS INTEGER\nTYPE Walker\n value AS INTEGER\nEND TYPE\n")]
    [InlineData("DIM walker AS INTEGER\nTYPE Holder\n value AS Walker\nEND TYPE\n")]
    public void ValueNamesDoNotRewriteTypeNames(string source)
    {
        Axiom.Parsing.Parser.Parse(source, out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.Equal(source, new IdentifierNormalizationService().NormalizeDocument(source));
    }

    [Fact]
    public void ValueReferencesStillNormalizeAndLineRangePreservesType()
    {
        const string source = "SUB Move(BYREF walker AS Walker)\nPRINT WALKER\nEND SUB\n";
        var service = new IdentifierNormalizationService();
        Assert.Equal(source.Replace("PRINT WALKER", "PRINT walker"), service.NormalizeDocument(source));
        Assert.Equal("SUB Move(BYREF walker AS Walker)", service.NormalizeLine(source, 0, source.IndexOf('\n')));
    }
}
