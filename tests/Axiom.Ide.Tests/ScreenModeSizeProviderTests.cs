using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class ScreenModeSizeProviderTests
{
    [Theory]
    [InlineData(1920, 1080, 1920, 1080)]
    [InlineData(1920, 1000, 1777, 1000)]
    [InlineData(1400, 1200, 1400, 787)]
    [InlineData(1919, 1079, 1918, 1079)]
    public void ComputeScreen10BestFitSize_FitsInsideAvailableArea(
        int availableWidth,
        int availableHeight,
        int expectedWidth,
        int expectedHeight)
    {
        var size = ScreenModeSizeProvider.ComputeScreen10BestFitSize(availableWidth, availableHeight);

        Assert.Equal((expectedWidth, expectedHeight), size);
        Assert.InRange(size.Width, 1, availableWidth);
        Assert.InRange(size.Height, 1, availableHeight);
    }
}
