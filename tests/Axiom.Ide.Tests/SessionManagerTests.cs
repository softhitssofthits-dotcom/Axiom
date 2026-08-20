using Axiom.Ide.Services.Input;

namespace Axiom.Ide.Tests;

public sealed class SessionManagerTests
{
    [Fact]
    public void BeginSession_SetsActive()
    {
        var manager = new SessionManager();

        manager.BeginSession(() => { });

        Assert.True(manager.IsSessionActive);
    }

    [Fact]
    public void EndSession_ClearsActive()
    {
        var manager = new SessionManager();
        manager.BeginSession(() => { });

        manager.EndSession();

        Assert.False(manager.IsSessionActive);
    }

    [Fact]
    public void BeginSession_ResetsCancellation()
    {
        var manager = new SessionManager();
        manager.IsCancellationRequested = true;

        manager.BeginSession(() => { });

        Assert.False(manager.IsCancellationRequested);
    }

    [Fact]
    public void BeginSession_CallsOnReset()
    {
        var manager = new SessionManager();
        var resetCalled = false;

        manager.BeginSession(() => { resetCalled = true; });

        Assert.True(resetCalled);
    }

    [Fact]
    public void Reset_ResetsCancellation()
    {
        var manager = new SessionManager();
        manager.IsCancellationRequested = true;

        manager.Reset(() => { });

        Assert.False(manager.IsCancellationRequested);
    }

    [Fact]
    public void Reset_CallsOnReset()
    {
        var manager = new SessionManager();
        var resetCalled = false;

        manager.Reset(() => { resetCalled = true; });

        Assert.True(resetCalled);
    }

    [Fact]
    public void IsCancellationRequested_CanBeSet()
    {
        var manager = new SessionManager();

        manager.IsCancellationRequested = true;
        Assert.True(manager.IsCancellationRequested);

        manager.IsCancellationRequested = false;
        Assert.False(manager.IsCancellationRequested);
    }
}
