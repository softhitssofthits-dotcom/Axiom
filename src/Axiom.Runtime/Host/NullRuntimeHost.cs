namespace Axiom.Runtime;

public sealed class NullRuntimeHost : IRuntimeHost, IReversibleOutputWriter
{
    public static NullRuntimeHost Instance { get; } = new();

    private NullRuntimeHost()
    {
    }

    public string ReadInput(InputRequest request) => string.Empty;

    public int OutputCheckpoint => 0;

    public void WriteOutput(string text)
    {
    }

    public void RestoreOutputCheckpoint(int checkpoint)
    {
        if (checkpoint != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(checkpoint), checkpoint, "Null output checkpoint must be 0.");
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
