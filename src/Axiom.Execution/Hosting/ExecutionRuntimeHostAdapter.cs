using Axiom.Runtime;

namespace Axiom.Execution;

internal sealed class ExecutionRuntimeHostAdapter : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IOutputWriter _outputWriter;
    private readonly IRuntimeClock _runtimeClock;
    private readonly IRuntimeRandom _runtimeRandom;

    public ExecutionRuntimeHostAdapter(IInputProvider inputProvider, IOutputWriter outputWriter, IRuntimeClock runtimeClock, IRuntimeRandom runtimeRandom)
    {
        ArgumentNullException.ThrowIfNull(inputProvider);
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(runtimeClock);
        ArgumentNullException.ThrowIfNull(runtimeRandom);

        _inputProvider = inputProvider;
        _outputWriter = outputWriter;
        _runtimeClock = runtimeClock;
        _runtimeRandom = runtimeRandom;
    }

    public IReadOnlyList<string> Output => _output;

    public int OutputCheckpoint => _output.Count;

    public string ReadInput(InputRequest request) => _inputProvider.ReadInput(request);

    public void WriteOutput(string text)
    {
        _output.Add(text);
        _outputWriter.WriteOutput(text);
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > _output.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Output checkpoint is outside the current output range.");
        }

        if (checkpoint == _output.Count)
        {
            return;
        }

        _output.RemoveRange(checkpoint, _output.Count - checkpoint);

        if (_outputWriter is IReversibleOutputWriter reversibleOutputWriter)
        {
            reversibleOutputWriter.RestoreOutputCheckpoint(checkpoint);
        }
    }

    public DateTimeOffset GetLocalNow() => _runtimeClock.GetLocalNow();

    public TimeZoneInfo GetLocalTimeZone() => _runtimeClock.GetLocalTimeZone();

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone) => _runtimeClock.TryResolveTimeZone(id, out timeZone);

    public double NextRandomDouble() => _runtimeRandom.NextRandomDouble();
}
