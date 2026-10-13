using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace Axiom.Ide.Graphics;

internal sealed class CanvasRenderSession : IDisposable
{
    private sealed record Work(Action<CanvasGpuSurface> Action, TaskCompletionSource? Completion = null);
    private sealed record WindowSize(int Width, int Height);
    internal const int MaxQueuedCommands = 4096;
    private readonly BlockingCollection<Work> _commands = new(new ConcurrentQueue<Work>(), MaxQueuedCommands);
    private readonly int _dispatcherThreadId = Environment.CurrentManagedThreadId;
    private readonly Thread _thread;
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile ExceptionDispatchInfo? _failure;
    private volatile bool _disposed;
    private bool _smooth;
    private int _repaintPending;
    private int _resizePending;
    private WindowSize? _requestedSize;
    private Task _lastPresentation = Task.CompletedTask;
    public string DriverName { get; private set; } = "";
    public int Width { get; }
    public int Height { get; }
    public event Action<Exception>? Faulted;

    public CanvasRenderSession(int width, int height)
    {
        Width = width; Height = height;
        _thread = new Thread(Run) { IsBackground = true, Name = "Axiom CANVAS DirectX" };
        _thread.Start();
        try { _ready.Task.GetAwaiter().GetResult(); }
        catch { _commands.CompleteAdding(); _thread.Join(); _commands.Dispose(); throw; }
    }

    private void Run()
    {
        try
        {
            using var surface = new CanvasGpuSurface(Width, Height);
            DriverName = surface.DriverName;
            _ready.SetResult();
            foreach (var work in _commands.GetConsumingEnumerable())
            {
                try
                {
                    _failure?.Throw();
                    work.Action(surface);
                    work.Completion?.SetResult();
                }
                catch (Exception e)
                {
                    var firstFailure = _failure is null;
                    _failure ??= ExceptionDispatchInfo.Capture(e);
                    work.Completion?.SetException(e);
                    if (firstFailure) { try { Faulted?.Invoke(e); } catch { } }
                }
            }
        }
        catch (Exception e) { _failure = ExceptionDispatchInfo.Capture(e); _ready.TrySetException(e); }
    }

    public void Enqueue(Action<CanvasGpuSurface> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _failure?.Throw();
        _commands.Add(new Work(action));
    }

    public void WaitForQueueSpace()
    {
        if (Environment.CurrentManagedThreadId == _dispatcherThreadId) return;
        try
        {
            while (!_disposed && _commands.Count >= MaxQueuedCommands / 2)
            {
                _failure?.Throw();
                Thread.Sleep(1);
            }
        }
        catch (ObjectDisposedException) when (_disposed) { }
    }

    public T Invoke<T>(Func<CanvasGpuSurface, T> action)
    {
        T result = default!;
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _failure?.Throw();
        _commands.Add(new Work(surface => result = action(surface), completion));
        completion.Task.GetAwaiter().GetResult();
        return result;
    }

    public void Publish(bool smooth)
    {
        _failure?.Throw();
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _lastPresentation = completion.Task;
        _commands.Add(new Work(surface => { _smooth = smooth; surface.Publish(); surface.Render(smooth); }, completion));
    }
    public void WaitForPreviousPresentation() => _lastPresentation.GetAwaiter().GetResult();
    public void SetSmoothing(bool smooth) => Enqueue(surface => { _smooth = smooth; surface.Render(smooth); });
    public void AttachWindow(IntPtr hwnd, int width, int height) =>
        Invoke(surface => { surface.AttachWindow(hwnd, width, height); surface.Render(_smooth); return true; });
    public void DetachWindow()
    {
        if (_failure is not null || _disposed) return;
        try { Invoke(surface => { surface.DetachWindow(); return true; }); }
        catch when (_failure is not null) { }
    }
    public void Resize(int width, int height)
    {
        if (_disposed || _failure is not null) return;
        Volatile.Write(ref _requestedSize, new WindowSize(width, height));
        if (Interlocked.Exchange(ref _resizePending, 1) != 0) return;
        QueueWindowWork(surface =>
        {
            Interlocked.Exchange(ref _resizePending, 0);
            var size = Volatile.Read(ref _requestedSize)!;
            surface.Resize(size.Width, size.Height, _smooth);
        });
    }
    public void Repaint()
    {
        if (_disposed || _failure is not null || Interlocked.Exchange(ref _repaintPending, 1) != 0) return;
        QueueWindowWork(surface => { Interlocked.Exchange(ref _repaintPending, 0); surface.Render(_smooth); });
    }

    private void QueueWindowWork(Action<CanvasGpuSurface> action)
    {
        try { Enqueue(action); }
        catch (Exception) when (_disposed || _failure is not null) { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _commands.CompleteAdding();
        _thread.Join();
        _commands.Dispose();
    }
}
