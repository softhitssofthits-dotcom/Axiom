using System.Reflection;
using System.Runtime.InteropServices;
using Axiom.Ide.Graphics;
using Vortice.Direct3D11;

namespace Axiom.Ide.Tests;

internal sealed class CanvasRendererTestSurface : IDisposable
{
    private readonly IntPtr _hwnd;
    public CanvasGpuSurface Surface { get; }
    public CanvasRendererTestSurface(int sourceWidth, int sourceHeight, int width, int height)
    {
        _hwnd = CreateWindowEx(0, "STATIC", "Axiom CANVAS GPU verification", 0,
            0, 0, width, height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try { Surface = new CanvasGpuSurface(sourceWidth, sourceHeight); Surface.AttachWindow(_hwnd, width, height); }
        catch { DestroyWindow(_hwnd); throw; }
    }
    public uint[] ReadDisplayed()
    {
        var presenter = Field<object>(Surface, "_presenter");
        var device = Field<ID3D11Device>(presenter, "_device");
        var context = Field<ID3D11DeviceContext>(presenter, "_context");
        var backBuffer = Field<ID3D11Texture2D>(presenter, "_backBuffer");
        var description = backBuffer.Description;
        description.Usage = ResourceUsage.Staging; description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read; description.MiscFlags = ResourceOptionFlags.None;
        using var staging = device.CreateTexture2D(description);
        context.CopyResource(staging, backBuffer);
        var mapped = context.Map(staging, 0, MapMode.Read);
        try
        {
            var width = (int)description.Width;
            var result = new uint[width * (int)description.Height]; var row = new int[width];
            for (var y = 0; y < description.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch), row, 0, width);
                Buffer.BlockCopy(row, 0, result, y * width * 4, width * 4);
            }
            return result;
        }
        finally { context.Unmap(staging, 0); }
    }
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    public void Dispose() { Surface.Dispose(); DestroyWindow(_hwnd); }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string name, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(IntPtr hwnd);
}
