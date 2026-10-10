using Axiom.Runtime;
namespace Axiom.Execution.Tests;
public sealed class TilemapLoadFailureTests
{
    [Fact]
    public void FailureIsReportedOnceAndNextExplicitLoadCanSucceed()
    {
        var host = new FailingOnceHost();
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("""
            DIM m AS TILEMAP
            TRY
                m = LOADTILEMAP("first.tmj")
            CATCH err
                PRINT err.Code
                PRINT err.Message
            END TRY
            m = LOADTILEMAP("second.tmj")
            PRINT MAPWIDTH(m)
            """, inputProvider: host));
        Assert.Equal(ExecutionStatus.Success, result.Status);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "MB3005", "LOADTILEMAP: Проверочный отказ.", "2" }, result.Output);
        Assert.Equal(2, host.Calls);
    }
    private sealed class FailingOnceHost : IInputProvider, IGraphicsHost
    {
        public int Calls { get; private set; }
        public string ReadInput(InputRequest request) => "";
        public RuntimeTilemapValue? LoadTilemap(string path)
        {
            Calls++;
            Assert.Equal(Calls == 1 ? "first.tmj" : "second.tmj", path);
            if (Calls == 1) throw new InvalidDataException("LOADTILEMAP: Проверочный отказ.");
            return new RuntimeTilemapValue(2, 1, 16, 16, RuntimeTilemapRenderOrder.RightDown, Array.Empty<RuntimeTilemapTilesetReference>(), Array.Empty<RuntimeTilemapLayer>());
        }
    }
}
