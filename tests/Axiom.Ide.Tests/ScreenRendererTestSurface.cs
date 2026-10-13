using System.Reflection;
using System.Runtime.InteropServices;
using Axiom.Ide.Graphics;
using Vortice.Direct3D11;

namespace Axiom.Ide.Tests;

// Считывает фактический результат графического процессора, а не копию алгоритма масштабирования.
internal sealed class ScreenRendererTestSurface : IDisposable
{
    private readonly IntPtr _hwnd;
    public Direct3D11SwapChainRenderer Renderer { get; }

    public ScreenRendererTestSurface(int width, int height)
    {
        _hwnd = CreateWindowEx(0, "STATIC", "Axiom GPU verification", 0,
            0, 0, width, height, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        try { Renderer = new Direct3D11SwapChainRenderer(_hwnd, width, height); }
        catch { DestroyWindow(_hwnd); throw; }
    }

    public uint[] ReadPixels()
    {
        var device = GetField<ID3D11Device>("_device");
        var context = GetField<ID3D11DeviceContext>("_context");
        var backBuffer = GetField<ID3D11Texture2D>("_backBuffer");
        var description = backBuffer.Description;
        description.Usage = ResourceUsage.Staging;
        description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        description.MiscFlags = ResourceOptionFlags.None;
        using var staging = device.CreateTexture2D(description);
        context.CopyResource(staging, backBuffer);
        var mapped = context.Map(staging, 0, MapMode.Read);
        try
        {
            var width = (int)description.Width;
            var pixels = new uint[width * (int)description.Height];
            var row = new int[width];
            for (var y = 0; y < description.Height; y++)
            {
                Marshal.Copy(IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch), row, 0, width);
                Buffer.BlockCopy(row, 0, pixels, y * width * sizeof(uint), width * sizeof(uint));
            }
            return pixels;
        }
        finally { context.Unmap(staging, 0); }
    }

    private T GetField<T>(string name) where T : class =>
        (T)typeof(Direct3D11SwapChainRenderer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Renderer)!;

    public void Dispose()
    {
        Renderer.Dispose();
        DestroyWindow(_hwnd);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string name, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);
}
