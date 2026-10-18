using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Backend.Tests;

public sealed class InputStateServiceTests
{
    [Fact]
    public void EnqueueKey_AddsKeyToQueue()
    {
        var service = new InputStateService();

        service.EnqueueKey("A");
        var result = service.ReadKey();

        Assert.Equal("A", result);
    }

    [Fact]
    public void EnqueueKey_IgnoresEmpty()
    {
        var service = new InputStateService();

        service.EnqueueKey("");
        var result = service.ReadKey();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SetKeyDown_MakesKeyReportedAsDown()
    {
        var service = new InputStateService();

        service.SetKeyDown("A");

        Assert.True(service.IsKeyDown("A"));
    }

    [Fact]
    public void SetKeyUp_MakesKeyReportedAsUp()
    {
        var service = new InputStateService();
        service.SetKeyDown("A");

        service.SetKeyUp("A");

        Assert.False(service.IsKeyDown("A"));
    }

    [Fact]
    public void IsKeyDown_ReturnsFalseForUnknownKey()
    {
        var service = new InputStateService();

        Assert.False(service.IsKeyDown("UNKNOWN"));
    }

    [Fact]
    public void ClearKeyState_ClearsAllKeys()
    {
        var service = new InputStateService();
        service.SetKeyDown("A");

        service.ClearKeyState();

        Assert.False(service.IsKeyDown("A"));
    }

    [Fact]
    public void ReadKey_ReturnsEmptyWhenNoKeys()
    {
        var service = new InputStateService();

        Assert.Equal(string.Empty, service.ReadKey());
    }

    [Fact]
    public void SetMousePosition_StoresPosition()
    {
        var service = new InputStateService();

        service.SetMousePosition(100, 200);

        Assert.Equal(100, service.GetMouseX());
        Assert.Equal(200, service.GetMouseY());
    }

    [Fact]
    public void SetMouseButtonDown_MakesButtonReportedAsDown()
    {
        var service = new InputStateService();

        service.SetMouseButtonDown(1);

        Assert.True(service.IsMouseButtonDown(1));
    }

    [Fact]
    public void SetMouseButtonUp_MakesButtonReportedAsUp()
    {
        var service = new InputStateService();
        service.SetMouseButtonDown(1);

        service.SetMouseButtonUp(1);

        Assert.False(service.IsMouseButtonDown(1));
    }

    [Fact]
    public void SetMouseButtonDown_IgnoresInvalidButton()
    {
        var service = new InputStateService();

        service.SetMouseButtonDown(0);
        service.SetMouseButtonDown(4);

        Assert.False(service.IsMouseButtonDown(0));
        Assert.False(service.IsMouseButtonDown(4));
    }

    [Fact]
    public void EnqueueMouseButtonPress_AddsPressEvent()
    {
        var service = new InputStateService();

        service.EnqueueMouseButtonPress(1);
        var result = service.ReadMouseButtonPress();

        Assert.Equal(1, result);
    }

    [Fact]
    public void ReadMouseButtonPress_ReturnsZeroWhenEmpty()
    {
        var service = new InputStateService();

        Assert.Equal(0, service.ReadMouseButtonPress());
    }

    [Fact]
    public void ClearMouseState_ClearsAllMouseState()
    {
        var service = new InputStateService();
        service.SetMousePosition(100, 200);
        service.SetMouseButtonDown(1);

        service.ClearMouseState();

        Assert.False(service.IsMouseButtonDown(1));
    }

    [Fact]
    public void SetScreenState_UpdatesSnapshot()
    {
        var service = new InputStateService();

        service.SetScreenState(isScreenActive: true, isScreenOutputActive: true, screenMode: 10, screenWidth: 640, screenHeight: 480);

        Assert.True(service.Snapshot.IsScreenActive);
        Assert.True(service.Snapshot.IsScreenOutputActive);
        Assert.Equal(10, service.Snapshot.ScreenMode);
        Assert.Equal(640, service.Snapshot.ScreenWidth);
        Assert.Equal(480, service.Snapshot.ScreenHeight);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var service = new InputStateService();
        service.EnqueueKey("A");
        service.SetKeyDown("B");
        service.SetMousePosition(100, 200);
        service.SetMouseButtonDown(1);

        service.Reset();

        Assert.Equal(string.Empty, service.ReadKey());
        Assert.False(service.IsKeyDown("B"));
    }
}
