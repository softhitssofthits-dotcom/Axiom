using System.Diagnostics;

namespace Axiom.Runtime;

public interface IRuntimeClock
{
    DateTimeOffset GetLocalNow();

    TimeZoneInfo GetLocalTimeZone();

    bool TryResolveTimeZone(string id, out TimeZoneInfo timeZone);

    /// <summary>
    /// Монотонный счётчик секунд. В отличие от календарных часов он не прыгает
    /// при переводе системного времени и не начинается заново в полночь, поэтому
    /// годится для измерения прошедшего времени. Точка отсчёта произвольна:
    /// значение имеет только разность двух показаний. Тестовые часы переопределяют
    /// этот метод, чтобы измерение было воспроизводимым.
    /// </summary>
    double GetMonotonicSeconds() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
}
