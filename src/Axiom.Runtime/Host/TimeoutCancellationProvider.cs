using System.Diagnostics;

namespace Axiom.Runtime;

public sealed class TimeoutCancellationProvider : IRuntimeCancellationProvider, IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly TimeSpan _timeout;
    private volatile bool _timedOut;

    public TimeoutCancellationProvider(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
        }

        _timeout = timeout;
        _stopwatch = Stopwatch.StartNew();
    }

    public bool IsCancellationRequested
    {
        get
        {
            if (_timedOut)
            {
                return true;
            }

            if (_stopwatch.Elapsed >= _timeout)
            {
                _timedOut = true;
                return true;
            }

            return false;
        }
    }

    public bool TimedOut => _timedOut;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Dispose()
    {
        _stopwatch.Stop();
    }
}
