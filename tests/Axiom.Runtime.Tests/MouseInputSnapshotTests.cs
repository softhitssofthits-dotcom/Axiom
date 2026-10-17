using Axiom.Runtime;

namespace Axiom.Runtime.Tests;

public sealed class MouseInputSnapshotTests
{
    [Fact]
    public void PublishButtonPress_SetsStateAndEventTogether()
    {
        var snapshot = new RuntimeInputSnapshot();

        snapshot.PublishMouseButtonPress(1, 12, 34);

        Assert.True(snapshot.IsMouseButtonDown(1));
        snapshot.GetMousePosition(out var x, out var y);
        Assert.Equal(12, x);
        Assert.Equal(34, y);
        Assert.True(snapshot.TryReadMouseEvent(out var mouseEvent));
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Press, 1), mouseEvent);
    }

    [Fact]
    public void PublishButtonRelease_ClearsStateBeforeEventIsRead()
    {
        var snapshot = new RuntimeInputSnapshot();
        snapshot.PublishMouseButtonPress(2, 1, 1);

        snapshot.PublishMouseButtonRelease(2, 5, 6);

        Assert.False(snapshot.IsMouseButtonDown(2));
        Assert.True(snapshot.TryReadMouseEvent(out var press));
        Assert.Equal(MouseInputEventKind.Press, press.Kind);
        Assert.True(snapshot.TryReadMouseEvent(out var release));
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Release, 2), release);
    }

    [Fact]
    public void ConsecutiveMovesCoalesceWithoutLosingPressOrder()
    {
        var snapshot = new RuntimeInputSnapshot();

        snapshot.PublishMouseMove(1, 1);
        snapshot.PublishMouseMove(2, 2);
        snapshot.PublishMouseButtonPress(1, 3, 3);
        snapshot.PublishMouseMove(4, 4);

        Assert.True(snapshot.TryReadMouseEvent(out var first));
        Assert.Equal(MouseInputEventKind.Move, first.Kind);
        Assert.True(snapshot.TryReadMouseEvent(out var second));
        Assert.Equal(new MouseInputEvent(MouseInputEventKind.Press, 1), second);
        Assert.True(snapshot.TryReadMouseEvent(out var third));
        Assert.Equal(MouseInputEventKind.Move, third.Kind);
        Assert.False(snapshot.TryReadMouseEvent(out _));
    }

    [Fact]
    public void ConsecutiveWheelStepsAccumulateAndCancelToNothing()
    {
        var snapshot = new RuntimeInputSnapshot();

        snapshot.PublishMouseWheel(120, 0, 0);
        snapshot.PublishMouseWheel(120, 0, 0);
        snapshot.PublishMouseWheel(-240, 0, 0);

        Assert.False(snapshot.TryReadMouseEvent(out _));
    }

    [Fact]
    public void ConsecutiveMovesKeepQueueAtOneEvent()
    {
        var snapshot = new RuntimeInputSnapshot();
        for (var index = 0; index < 20_000; index++)
        {
            snapshot.PublishMouseMove(index, index);
        }

        Assert.Equal(1, snapshot.PendingMouseEventCount);
        Assert.Equal(0, snapshot.DroppedMouseEventCount);
        snapshot.GetMousePosition(out var x, out var y);
        Assert.Equal(19_999, x);
        Assert.Equal(19_999, y);
    }

    [Fact]
    public void QueueIsBoundedForNonCoalescibleButtonTransitions()
    {
        var snapshot = new RuntimeInputSnapshot();
        for (var index = 0; index < 20_000; index++)
        {
            var button = index % 2 == 0 ? 1 : 2;
            snapshot.PublishMouseButtonPress(button, index, index);
            snapshot.PublishMouseButtonRelease(button, index, index);
        }

        Assert.True(snapshot.PendingMouseEventCount <= 8192);
        Assert.True(snapshot.DroppedMouseEventCount > 0);
    }

    [Fact]
    public void ClearMouseStateAdvancesGenerationAndRemovesPendingEvents()
    {
        var snapshot = new RuntimeInputSnapshot();
        snapshot.PublishMouseButtonPress(1, 3, 4);
        var generation = snapshot.MouseClearGeneration;

        snapshot.ClearMouseState();

        Assert.True(snapshot.MouseClearGeneration > generation);
        Assert.Equal(0, snapshot.PendingMouseEventCount);
        Assert.False(snapshot.IsMouseButtonDown(1));
        Assert.Equal(-1, snapshot.GetMouseX());
    }

    [Fact]
    public void PublishButtonReleaseKeepPositionPreservesCoordinates()
    {
        var snapshot = new RuntimeInputSnapshot();
        snapshot.PublishMouseButtonPress(1, 7, 8);

        snapshot.PublishMouseButtonReleaseKeepPosition(1);

        snapshot.GetMousePosition(out var x, out var y);
        Assert.Equal(7, x);
        Assert.Equal(8, y);
        Assert.False(snapshot.IsMouseButtonDown(1));
    }
}
