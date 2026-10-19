using Axiom.Core;
using Axiom.Formatting;

namespace Axiom.Ide.Backend.Tests;

public sealed class ModuleFormattingTests
{
    [Theory]
    [InlineData("PUBLIC SUB Work()", BlockOpeningKind.Sub)]
    [InlineData("PRIVATE FUNCTION Work() AS INTEGER", BlockOpeningKind.Function)]
    [InlineData("PUBLIC TYPE Box", BlockOpeningKind.Type)]
    public void VisibilityPreservesBlockFormatting(string source, BlockOpeningKind kind)
    {
        Assert.Equal(kind, BlockLineClassifier.ClassifyLine(source).OpeningKind);
        Assert.True(BlockLineClassifier.ClassifyLine(source).IncreasesIndentNextLine);
        var formatter = new AxiomCodeFormattingService();
        var formatted = formatter.FormatDocument("public function Answer() as integer\nreturn 42\nend function", CodeFormattingStyle.Classic);
        Assert.Contains("PUBLIC FUNCTION", formatted);
        Assert.Equal(formatted, formatter.FormatDocument(formatted, CodeFormattingStyle.Classic));
    }
}
