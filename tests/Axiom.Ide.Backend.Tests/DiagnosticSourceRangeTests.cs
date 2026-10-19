using Axiom.Ide.Models;

namespace Axiom.Ide.Backend.Tests;

public sealed class DiagnosticSourceRangeTests
{
    [Fact]
    public void MappingRetainsUtf16OffsetsFromTheOriginalDiagnostic()
    {
        var item = new ProgramRunServiceFixture().Compile("PRINT missing\r\n").Single();
        Assert.Equal(6, ReadSpan(item, "SpanStart"));
        Assert.Equal(7, ReadSpan(item, "SpanLength"));
    }

    private static int ReadSpan(ProgramDiagnosticItem item, string name)
    {
        var property = typeof(ProgramDiagnosticItem).GetProperty(name);
        Assert.NotNull(property);
        return Assert.IsType<int>(property.GetValue(item));
    }

    private sealed class ProgramRunServiceFixture
    {
        public IReadOnlyList<ProgramDiagnosticItem> Compile(string source) =>
            new Axiom.Ide.Services.Execution.ProgramRunService(
                new Axiom.Execution.AxiomExecutionPipeline(),
                new Axiom.Ide.Services.Input.ProgramConsoleService()).Compile(source).Diagnostics;
    }
}
