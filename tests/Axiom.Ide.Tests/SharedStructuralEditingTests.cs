using Axiom.Formatting;

namespace Axiom.Ide.Tests;

public sealed class SharedStructuralEditingTests
{
    [Theory]
    [InlineData("IF TRUE THEN PRINT 1")]
    [InlineData("IF TRUE THEN PRINT 1 ELSE PRINT 2 ' конец")]
    public void EnterAfterSingleLineIfDoesNotInsertEndIf(string line)
    {
        var source = line + "\n";
        var planner = new BlockEditPlanner(new AxiomCodeFormattingService());
        var plan = planner.Plan(source, 1, 2, source.Length, CodeFormattingStyle.Classic);
        Assert.Equal(source, source.Remove(plan.Start, plan.Length).Insert(plan.Start, plan.NewText));
    }

    [Fact]
    public void StructuralPlannerIsIndependentOfAvalonEdit()
    {
        var assembly = typeof(AxiomCodeFormattingService).Assembly;
        Assert.NotNull(assembly.GetType("Axiom.Formatting.BlockEditPlanner"));
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name == "ICSharpCode.AvalonEdit");
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void PlanPreservesLineBreaksAndReturnsAnApplicableEdit(string delimiter)
    {
        var source = "IF a = 0 THEN" + delimiter;
        var planner = new BlockEditPlanner(new AxiomCodeFormattingService());
        var plan = planner.Plan(source, 1, 2, source.Length, CodeFormattingStyle.Classic);
        var result = source.Remove(plan.Start, plan.Length).Insert(plan.Start, plan.NewText);
        Assert.Equal("IF a = 0 THEN" + delimiter + "    " + delimiter + "END IF", result);
        Assert.Equal(source.Length + 4, plan.NewCaretOffset);
    }
}
