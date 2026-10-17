namespace Axiom.Runtime.Tests;

public sealed class MouseCoordinateSpaceTests
{
    [Fact]
    public void CanvasPointInsideViewportBecomesWorldCoordinate()
    {
        var host = new TestRuntimeHost(inputs: []);
        host.ActivateCanvas(480, 270);
        host.SetViewport(30, 80, 449, 249, null, null, useScreenCoordinates: true);
        host.SetCanvasCamera(100, 50);

        var translated = GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(
            host, 40, 90, out var logicalX, out var logicalY);

        Assert.True(translated);
        Assert.Equal(110, logicalX);
        Assert.Equal(60, logicalY);
    }

    [Fact]
    public void CanvasPointOutsideViewportHasNoWorldCoordinate()
    {
        var host = new TestRuntimeHost(inputs: []);
        host.ActivateCanvas(480, 270);
        host.SetViewport(30, 80, 449, 249, null, null, useScreenCoordinates: true);

        var translated = GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(
            host, 10, 90, out _, out _);

        Assert.False(translated);
    }

    [Fact]
    public void CanvasPointOutsideSurfaceHasNoWorldCoordinate()
    {
        var host = new TestRuntimeHost(inputs: []);
        host.ActivateCanvas(480, 270);

        var translated = GraphicsCoordinateSpaceMath.TryTranslateAbsoluteToLogical(
            host, 480, 10, out _, out _);

        Assert.False(translated);
    }
}
