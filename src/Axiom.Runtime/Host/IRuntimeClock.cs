namespace Axiom.Runtime;

public interface IRuntimeClock
{
    DateTimeOffset GetLocalNow();

    TimeZoneInfo GetLocalTimeZone();

    bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone);
}
