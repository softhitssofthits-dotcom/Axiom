using Axiom.Ide.Documents;
using Axiom.Ide.Editor.Formatting;

namespace Axiom.Ide.Tests;

public sealed class DocumentFormattingPolicyTests
{
    [Fact]
    public void CanApplyFormatting_ReturnsTrueForUntitledDocument()
    {
        var document = new DocumentTabViewModel(initialText: "PRINT 1");

        var result = DocumentFormattingPolicy.CanApplyFormatting(document);

        Assert.True(result);
    }

    [Fact]
    public void CanApplyFormatting_ReturnsTrueForAxiomFile()
    {
        var document = new DocumentTabViewModel(@"C:\temp\program.axm", "PRINT 1");

        var result = DocumentFormattingPolicy.CanApplyFormatting(document);

        Assert.True(result);
    }

    [Fact]
    public void CanApplyFormatting_ReturnsFalseForLegacyMbasFile()
    {
        var document = new DocumentTabViewModel(@"C:\temp\program.mbas", "PRINT 1");

        var result = DocumentFormattingPolicy.CanApplyFormatting(document);

        Assert.False(result);
    }

    [Fact]
    public void CanApplyFormatting_ReturnsFalseForReadOnlyDocument()
    {
        var document = new DocumentTabViewModel(@"C:\temp\program.axm", "PRINT 1", isReadOnly: true);

        var result = DocumentFormattingPolicy.CanApplyFormatting(document);

        Assert.False(result);
    }
}
