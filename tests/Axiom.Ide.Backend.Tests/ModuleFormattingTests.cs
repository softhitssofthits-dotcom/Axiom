using Axiom.Core;
using Axiom.Formatting;

namespace Axiom.Ide.Backend.Tests;

public sealed class ModuleFormattingTests
{
    [Theory]
    [InlineData("PUBLIC SUB Work()", BlockOpeningKind.Sub)]
    [InlineData("PRIVATE FUNCTION Work() AS INTEGER", BlockOpeningKind.Function)]
    [InlineData("PUBLIC TYPE Box", BlockOpeningKind.Type)]
    [InlineData("FOR i = 1 TO 3", BlockOpeningKind.For)]
    [InlineData("FOR EACH fruit IN fruits[]", BlockOpeningKind.For)]
    [InlineData("FOR EACH BYVAL item AS DOUBLE IN values[]", BlockOpeningKind.For)]
    public void VisibilityPreservesBlockFormatting(string source, BlockOpeningKind kind)
    {
        Assert.Equal(kind, BlockLineClassifier.ClassifyLine(source).OpeningKind);
        Assert.True(BlockLineClassifier.ClassifyLine(source).IncreasesIndentNextLine);
        var formatter = new AxiomCodeFormattingService();
        var formatted = formatter.FormatDocument("public function Answer() as integer\nreturn 42\nend function", CodeFormattingStyle.Classic);
        Assert.Contains("PUBLIC FUNCTION", formatted);
        Assert.Equal(formatted, formatter.FormatDocument(formatted, CodeFormattingStyle.Classic));
    }

    [Fact]
    public void ForEachHeaderClosesWithNext()
    {
        const string source = "FOR EACH score IN scores[]";
        var plan = new BlockEditPlanner(new AxiomCodeFormattingService())
            .Plan(source, 1, 2, source.Length, CodeFormattingStyle.Classic);

        Assert.Contains("NEXT", plan.NewText);
    }

    [Fact]
    public void EachAndInOutsideHeaderDoNotOpenBlock()
    {
        Assert.Null(BlockLineClassifier.ClassifyLine("DIM each AS INTEGER").OpeningKind);
        Assert.Null(BlockLineClassifier.ClassifyLine("DIM in2 AS INTEGER").OpeningKind);
    }
}
