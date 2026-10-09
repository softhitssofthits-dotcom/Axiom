namespace Axiom.Runtime.Tests;
public sealed class TilemapExtentBoundaryTests
{
    [Theory]
    [InlineData(int.MaxValue, false)]
    [InlineData(int.MaxValue, true)]
    [InlineData(int.MinValue, false)]
    [InlineData(int.MinValue, true)]
    public void TileBoundsKeepFullOffsetPlusSize(int offset, bool vertical)
    {
        var tiles = new RuntimeTilesetValue(new RuntimeImageValue(32, 32, new uint[1024]), 32, 32,
            tileOffsetX: vertical ? 0 : offset, tileOffsetY: vertical ? offset : 0);
        var map = new RuntimeTilemapValue(1, 1, 32, 32, RuntimeTilemapRenderOrder.RightDown,
            new[] { new RuntimeTilemapTilesetReference(1, tiles) }, Array.Empty<RuntimeTilemapLayer>());
        Assert.Equal((long)offset, vertical ? map.MinTileTop : map.MinTileLeft);
        Assert.Equal((long)offset + 31, vertical ? map.MaxTileBottom : map.MaxTileRight);
    }
}
