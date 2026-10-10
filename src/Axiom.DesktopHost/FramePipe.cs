using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Channels;
using Axiom.Ide.Graphics;

namespace Axiom.DesktopHost;

internal sealed class FramePipe : IDisposable
{
    private readonly NamedPipeServerStream _pipe;
    private readonly CancellationTokenSource _closed = new();
    private readonly Channel<Frame> _pending = Channel.CreateBounded<Frame>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = true });
    private readonly object _gate = new();
    private string? _inflight;
    private TaskCompletionSource? _ack;
    private long _nextFrame;
    private readonly Action<Exception> _failed;

    public FramePipe(string name, Action<Exception> failed)
    {
        if (!name.StartsWith("axiom-ide-", StringComparison.Ordinal) || name.Length > 128 || name.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')))
            throw new ArgumentException("Недопустимое имя канала кадров.");
        _failed = failed;
        _pipe = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
        _ = Send();
    }

    public void Publish(string sessionId, ScreenFramebufferSnapshot snapshot)
    {
        long bytes = checked((long)snapshot.Width * snapshot.Height * 4);
        if (snapshot.Width <= 0 || snapshot.Height <= 0 || bytes > 256L * 1024 * 1024 || snapshot.BgraPixels.LongLength * 4 != bytes)
            throw new InvalidOperationException("FRAME_TRANSPORT_LIMIT: некорректный размер кадра.");
        _pending.Writer.TryWrite(new Frame(sessionId, snapshot));
    }

    public bool Acknowledge(string frameId)
    {
        lock (_gate)
        {
            if (_inflight != frameId || _ack is null) return false;
            _ack.TrySetResult(); _inflight = null;
            return true;
        }
    }

    public void Reset()
    {
        while (_pending.Reader.TryRead(out _)) { }
        lock (_gate) { _ack?.TrySetResult(); _inflight = null; }
    }

    private async Task Send()
    {
        try
        {
            await _pipe.WaitForConnectionAsync(_closed.Token);
            await foreach (var frame in _pending.Reader.ReadAllAsync(_closed.Token))
            {
                string id = Interlocked.Increment(ref _nextFrame).ToString();
                Task ack;
                lock (_gate) { _inflight = id; _ack = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); ack = _ack.Task; }
                var snapshot = frame.Snapshot;
                byte[] header = JsonSerializer.SerializeToUtf8Bytes(new { frameId = id, frame.SessionId,
                    surfaceId = frame.SessionId + ":canvas", width = snapshot.Width, height = snapshot.Height,
                    stride = checked(snapshot.Width * 4), format = "BGRA8", byteLength = checked(snapshot.BgraPixels.Length * 4) },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                byte[] size = new byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(size, (uint)header.Length);
                await _pipe.WriteAsync(size, _closed.Token);
                await _pipe.WriteAsync(header, _closed.Token);
                // Копия принадлежит отправляемому кадру; Dispatcher и VM не ждут pipe.
                byte[] pixels = MemoryMarshal.AsBytes(snapshot.BgraPixels.AsSpan()).ToArray();
                await _pipe.WriteAsync(pixels, _closed.Token);
                await _pipe.FlushAsync(_closed.Token);
                await ack.WaitAsync(_closed.Token);
            }
        }
        catch (OperationCanceledException) when (_closed.IsCancellationRequested) { }
        catch (Exception e) when (!_closed.IsCancellationRequested) { _failed(e); }
    }

    public void Dispose()
    {
        _closed.Cancel(); _pending.Writer.TryComplete(); _pipe.Dispose();
    }
    private sealed record Frame(string SessionId, ScreenFramebufferSnapshot Snapshot);
}
