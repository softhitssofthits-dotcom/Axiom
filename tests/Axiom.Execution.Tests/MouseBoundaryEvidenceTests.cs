using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class MouseBoundaryEvidenceTests
{
    [Fact]
    public void ScreenCoordinatesKeepFullLongRangeAndInvalidButtonsPreservePresses()
    {
        var host = new MouseHost();
        var run = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            PRINT MOUSEX()
            PRINT MOUSEY()
            PRINT MOUSEBUTTON(-2147483648)
            PRINT MOUSEBUTTON(2147483647)
            PRINT MOUSEBUTTON(0)
            PRINT MOUSEBUTTON(1)
            PRINT MOUSEPRESS(-2147483648)
            PRINT MOUSEPRESS(2147483647)
            PRINT MOUSEPRESS(0)
            PRINT MOUSEPRESS(1)
            PRINT MOUSEPRESS(1)
            PRINT MOUSEPRESS(2)
            PRINT MOUSEPRESS(3)
            """, inputProvider: host));
        Check(run, [long.MaxValue.ToString(), long.MinValue.ToString(), "FALSE", "FALSE", "FALSE", "TRUE", "FALSE", "FALSE", "FALSE", "TRUE", "FALSE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void AllMouseFormsWithoutSurfaceFailInsideLanguageAndRunFinally()
    {
        foreach (var expression in new[] { "MOUSEX()", "MOUSEY()", "MOUSEBUTTON(2147483647)", "MOUSEPRESS(-2147483648)" })
        {
            var source = $"TRY\nPRINT {expression}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT 42\nEND TRY";
            var pipeline = new AxiomExecutionPipeline();
            var compilation = pipeline.Compile(new CompilationRequest(source,
                initialGraphicsContext: Axiom.Semantics.InitialGraphicsContext.Unknown));
            Assert.True(compilation.Succeeded);
            var run = pipeline.RunCompiled(compilation.CompiledProgram!, new ExecutionRequest(source));
            Check(run, ["MB3010", "42"]);
        }
    }

    private static void Check(ExecutionResult run, string[] expected)
    {
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(expected, run.Output);
    }

    private sealed class MouseHost : IInputProvider, IMouseInputProvider, IGraphicsHost
    {
        private readonly Queue<int> _presses = new([2, 1, 3]);
        public bool IsScreenActive => true;
        public bool IsScreenOutputActive => false;
        public long GetMouseX() => long.MaxValue;
        public long GetMouseY() => long.MinValue;
        public bool IsMouseButtonDown(int buttonNumber) => buttonNumber == 1;
        public int ReadMouseButtonPress() => _presses.TryDequeue(out var button) ? button : 0;
        public string ReadInput(InputRequest request) => throw new InvalidOperationException("Ввод строки не ожидается.");
    }
}
