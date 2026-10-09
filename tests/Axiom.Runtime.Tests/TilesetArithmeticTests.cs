namespace Axiom.Runtime.Tests;
public sealed class TilesetArithmeticTests
{
    [Fact]
    public void HugeMarginDoesNotWrapIntoUsableAtlas()
    {
        var image = new RuntimeImageValue(32, 32, new uint[1024]);
        Assert.Throws<ArgumentException>(() => new RuntimeTilesetValue(image, 17, 17, int.MaxValue));
    }
    [Theory]
    [InlineData(32, 0, int.MaxValue, 1, 0, 0)]
    [InlineData(14, 2, 0, 4, 16, 16)]
    [InlineData(15, 0, 2, 4, 17, 17)]
    [InlineData(16, 0, 0, 4, 16, 16)]
    public void ValidGridPreservesCountAndLastSourceCoordinates(int size, int margin, int spacing, int count, int left, int top)
    {
        var tiles = new RuntimeTilesetValue(new RuntimeImageValue(32, 32, new uint[1024]), size, size, margin, spacing);
        Assert.Equal(count, tiles.TileCount);
        Assert.Equal(left, tiles.GetTileLeft(count));
        Assert.Equal(top, tiles.GetTileTop(count));
    }
}
