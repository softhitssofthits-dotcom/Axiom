using Axiom.Execution;
using Axiom.Ide.Models;
using Axiom.Ide.Services.Execution;
using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class ExecutionContextRegressionTests
{
    [Theory]
    [InlineData(ExecutionMode.Interpreter)]
    [InlineData(ExecutionMode.Bytecode)]
    public void PointReadsPhysicalCanvasPixelWithCameraAndViewport(ExecutionMode mode)
    {
        var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
        var result = service.Run("""
CANVAS 24, 18
VIEWPORT (10, 6)-(15, 11)
CAMERA 100, 50
PSET (100, 50), 6
PRINT POINT(10, 6)
PRINT POINT(100, 50)
""", mode);

        Assert.Equal(ProgramRunStatus.Success, result.Status);
        Assert.Equal(string.Join(Environment.NewLine, "6", "0"), result.OutputText);
    }

    [Fact]
    public void DebuggerAdapterPreservesSourceSpanForStoppedInstruction()
    {
        const string source = "PRINT 1\nPRINT 2";
        var service = new ProgramRunService(new AxiomExecutionPipeline(), new ProgramConsoleService());
        var result = service.Run(source, ExecutionMode.Bytecode, sourceBreakpointLines: [2]);

        Assert.True(result.Stopped);
        var location = Assert.IsType<Axiom.Core.TextLocation>(result.DebuggerState!.StopInfo.SourceLocation);
        Assert.Equal(2, location.Line);
        Assert.True(location.Span.Length > 0);
        Assert.InRange(location.Span.Start, source.IndexOf("PRINT 2", StringComparison.Ordinal), source.Length - 1);
        Assert.True(location.Span.End <= source.Length);
    }
}
