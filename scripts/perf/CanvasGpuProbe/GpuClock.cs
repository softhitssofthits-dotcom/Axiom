using System.Reflection;
using System.Diagnostics;
using Axiom.Ide.Graphics;
using Vortice.Direct3D11;

internal sealed class GpuClock : IDisposable
{
    private readonly ID3D11DeviceContext _context;
    private readonly ID3D11Query _start, _end, _disjoint;
    public GpuClock(CanvasGpuSurface surface)
    {
        var device = Field<ID3D11Device>(surface, "_device");
        _context = Field<ID3D11DeviceContext>(surface, "_context");
        _start = device.CreateQuery(new QueryDescription(QueryType.Timestamp));
        _end = device.CreateQuery(new QueryDescription(QueryType.Timestamp));
        _disjoint = device.CreateQuery(new QueryDescription(QueryType.TimestampDisjoint));
    }
    public void Begin() { _context.Begin(_disjoint); _context.End(_start); }
    public void End() { _context.End(_end); _context.End(_disjoint); }
    public double Read()
    {
        var watch = Stopwatch.StartNew();
        while (!_context.GetData(_disjoint, out QueryDataTimestampDisjoint data))
        {
            if (watch.Elapsed.TotalSeconds > 5) throw new TimeoutException();
            Thread.Sleep(1);
        }
        QueryDataTimestampDisjoint timing;
        while (!_context.GetData(_disjoint, out timing) || timing.Frequency == 0) Poll();
        ulong start, end;
        while (!_context.GetData(_start, out start) || start == 0) Poll();
        while (!_context.GetData(_end, out end) || end < start) Poll();
        return timing.Disjoint ? double.NaN : (end - start) * 1000d / timing.Frequency;
        void Poll()
        {
            if (watch.Elapsed.TotalSeconds > 5) throw new TimeoutException("Не получены корректные GPU-метки времени.");
            Thread.Sleep(1);
        }
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    public void Dispose() { _start.Dispose(); _end.Dispose(); _disjoint.Dispose(); }
}
