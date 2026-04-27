using Axiom.Runtime;

namespace Axiom.Execution;

internal sealed class ExecutionRuntimeHostAdapter : IRuntimeHost, IReversibleOutputWriter
{
    private readonly List<string> _output = [];
    private readonly IInputProvider _inputProvider;
    private readonly IOutputWriter _outputWriter;

    public ExecutionRuntimeHostAdapter(IInputProvider inputProvider, IOutputWriter outputWriter)
    {
        ArgumentNullException.ThrowIfNull(inputProvider);
        ArgumentNullException.ThrowIfNull(outputWriter);

        _inputProvider = inputProvider;
        _outputWriter = outputWriter;
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

    public DateTimeOffset GetLocalNow() => DateTimeOffset.Now;

    public TimeZoneInfo GetLocalTimeZone() => TimeZoneInfo.Local;

    public bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone)
    {
        if (string.Equals(id, "LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Local;
            return true;
        }

        if (string.Equals(id, "UTC", StringComparison.OrdinalIgnoreCase))
        {
            timeZone = TimeZoneInfo.Utc;
            return true;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
        }
        catch (InvalidTimeZoneException)
        {
        }

        timeZone = TimeZoneInfo.Utc;
        return false;
    }
}
