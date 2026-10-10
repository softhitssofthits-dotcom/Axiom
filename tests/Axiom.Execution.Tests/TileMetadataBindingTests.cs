namespace Axiom.Execution.Tests;
public sealed class TileMetadataBindingTests
{
    public static IEnumerable<object[]> Cases()
    {
        var expressions = new[]
        {
            "MAPTILESETCOUNT(42)",
            "MAPTILESETCOUNT()",
            "MAPPROPERTY(42, \"role\")",
            "MAPPROPERTY()",
            "MAPLAYERPROPERTY(42, 1, \"role\")",
            "MAPLAYERPROPERTY()",
            "MAPTILEPROPERTY(42, 1, 1, 1, \"solid\")",
            "MAPTILEPROPERTY()",
            "MAPOBJECTPROPERTY(42, 3, 1, \"role\")",
            "MAPOBJECTPROPERTY()",
            "MAPLAYERNAME(42, 1)",
            "MAPLAYERNAME()",
            "MAPLAYERTYPE(42, 3)",
            "MAPLAYERTYPE()",
            "MAPLAYERVISIBLE(42, 2)",
            "MAPLAYERVISIBLE()",
            "MAPOBJECTCOUNT(42, 3)",
            "MAPOBJECTCOUNT()",
            "MAPOBJECTNAME(42, 3, 1)",
            "MAPOBJECTNAME()",
            "MAPOBJECTCLASS(42, 3, 1)",
            "MAPOBJECTCLASS()",
            "MAPOBJECTKIND(42, 3, 2)",
            "MAPOBJECTKIND()",
            "MAPOBJECTVISIBLE(42, 3, 2)",
            "MAPOBJECTVISIBLE()",
            "MAPOBJECTX(42, 3, 1)",
            "MAPOBJECTX()",
            "MAPOBJECTY(42, 3, 1)",
            "MAPOBJECTY()",
            "MAPOBJECTWIDTH(42, 3, 1)",
            "MAPOBJECTWIDTH()",
            "MAPOBJECTHEIGHT(42, 3, 1)",
            "MAPOBJECTHEIGHT()",
            "MAPCOLUMN(42, 100, 116)",
            "MAPCOLUMN()",
            "MAPROW(42, 200, 215.5)",
            "MAPROW()",
            "MAPWORLDX(42, 100, 2)",
            "MAPWORLDX()",
            "MAPWORLDY(42, 200, 2)",
            "MAPWORLDY()",
            "MAPPROPERTY(m, 1)",
            "MAPLAYERNAME(m, TRUE)",
        };

        foreach (var expression in expressions) yield return [expression];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void WrongTypesAndArgumentCountsFailBeforeExecution(string expression)
    {
        var result = new AxiomExecutionPipeline().Run(new ExecutionRequest("DIM m AS TILEMAP\nPRINT " + expression));
        Assert.False(result.Succeeded);
        Assert.Empty(result.Output);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.StartsWith("MB2", diagnostic.Code));
    }
}
