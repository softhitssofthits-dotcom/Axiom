using Axiom.Runtime;

namespace Axiom.Execution.Tests;

public sealed class GraphicsQueryBoundaryEvidenceTests
{
    [Fact]
    public void IdentityPMapKeepsExtremeFiniteCoordinatesAndFractions()
    {
        Check(new QueryHost(canvas: false), """
            DIM largest AS DOUBLE = VAL("1.7976931348623157e308")
            PRINT PMAP(largest, 0) = largest
            PRINT PMAP(-largest, 1) = -largest
            PRINT PMAP(largest, 2) = largest
            PRINT PMAP(-largest, 3) = -largest
            PRINT PMAP(0.125, 0) = 0.125
            PRINT PMAP(-0.125, 3) = -0.125
            """, ["TRUE", "TRUE", "TRUE", "TRUE", "TRUE", "TRUE"]);
    }

    [Fact]
    public void CanvasMetricsCountUtf16CellsAndLineFeeds()
    {
        Check(new QueryHost(canvas: true), """
            PRINT TEXTWIDTH("")
            PRINT TEXTHEIGHT("")
            PRINT TEXTWIDTH("😀")
            PRINT TEXTWIDTH(LEFT("😀", 1))
            PRINT TEXTWIDTH(CHRW(0))
            PRINT TEXTWIDTH("é")
            PRINT TEXTWIDTH("é")
            PRINT TEXTWIDTH("😀" + CHRW(13) + CHRW(10) + "А")
            PRINT TEXTHEIGHT("😀" + CHRW(13) + CHRW(10) + "А")
            PRINT TEXTWIDTH(CHRW(13))
            PRINT TEXTHEIGHT(CHRW(10))
            """, ["0", "12", "16", "8", "8", "16", "8", "16", "24", "0", "24"]);
    }

    [Fact]
    public void ExtremeSelectorsAndMissingSurfaceAreCatchable()
    {
        foreach (var expression in new[] { "POINT(-2147483648)", "POINT(2147483647)", "PMAP(0, -2147483648)", "PMAP(0, 2147483647)" })
            Check(new QueryHost(canvas: false), Catch(expression), ["MB3007", "42"]);
        foreach (var expression in new[] { "TEXTWIDTH(\"😀\")", "TEXTHEIGHT(CHRW(0))", "POINT(0)", "POINT(0, 0)", "PMAP(0, 0)" })
            Check(null, Catch(expression), ["MB3010", "42"]);
    }

    private static string Catch(string expression) => $"TRY\nPRINT {expression}\nCATCH err\nPRINT err.Code\nFINALLY\nPRINT 42\nEND TRY";

    private static void Check(IInputProvider? host, string source, string[] expected)
    {
        var pipeline = new AxiomExecutionPipeline();
        // Проверяем перехват ошибок самой VM при неизвестном компилятору
        // входном состоянии; отдельная проверка IDE отвергает известный режим.
        var compiled = pipeline.Compile(new CompilationRequest(source,
            initialGraphicsContext: Axiom.Semantics.InitialGraphicsContext.Unknown));
        Assert.True(compiled.Succeeded, string.Join("\n", compiled.Diagnostics.Select(d => d.Message)));
        var run = pipeline.RunCompiled(compiled.CompiledProgram!, new ExecutionRequest(source, inputProvider: host));
        Assert.True(run.Succeeded, string.Join("\n", run.Diagnostics.Select(d => d.Message)));
        Assert.Empty(run.Diagnostics);
        Assert.Equal(ExecutionEngineKind.BytecodeVm, run.ActualEngine);
        Assert.Equal(expected, run.Output);
    }

    private sealed class QueryHost(bool canvas) : IInputProvider, IGraphicsHost
    {
        public bool IsCanvasActive => canvas;
        public bool IsScreenActive => !canvas;
        public bool IsScreenOutputActive => false;
        public int ScreenWidth => 16;
        public int ScreenHeight => 16;
        public string ReadInput(InputRequest request) => throw new InvalidOperationException("Ввод не ожидается.");
    }
}
