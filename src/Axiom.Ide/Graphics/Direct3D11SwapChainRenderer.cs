using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;

namespace Axiom.Ide.Graphics;

internal sealed class Direct3D11SwapChainRenderer : IDisposable
{
    private const int PresentSyncInterval = 1;

    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGISwapChain _swapChain;
    private ID3D11Texture2D? _backBuffer;
    private ID3D11RenderTargetView? _renderTargetView;
    private int _width;
    private int _height;

    public Direct3D11SwapChainRenderer(IntPtr hwnd, int width, int height)
    {
        if (hwnd == IntPtr.Zero)
        {
            throw new ArgumentException("HWND must not be zero.", nameof(hwnd));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Swap chain size must be positive.");
        }

        _width = width;
        _height = height;

        var featureLevels = new[]
        {
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0
        };

        var swapChainDescription = new SwapChainDescription
        {
            BufferCount = 2,
            BufferDescription = new ModeDescription((uint)width, (uint)height, Format.B8G8R8A8_UNorm),
            BufferUsage = Usage.RenderTargetOutput,
            OutputWindow = hwnd,
            SampleDescription = new SampleDescription(1, 0),
            Windowed = true,
            SwapEffect = SwapEffect.Discard
        };

        IDXGISwapChain? swapChain;
        ID3D11Device? device;
        ID3D11DeviceContext? context;

        D3D11CreateDeviceAndSwapChain(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            featureLevels,
            swapChainDescription,
            out swapChain,
            out device,
            out _,
            out context).CheckError();

        _swapChain = swapChain ?? throw new InvalidOperationException("D3D11 swap chain was not created.");
        _device = device ?? throw new InvalidOperationException("D3D11 device was not created.");
        _context = context ?? throw new InvalidOperationException("D3D11 device context was not created.");
        CreateRenderTargetView();
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (width == _width && height == _height))
        {
            return;
        }

        _width = width;
        _height = height;
        _context.OMSetRenderTargets([], null);
        _renderTargetView?.Dispose();
        _renderTargetView = null;
        _backBuffer?.Dispose();
        _backBuffer = null;
        _swapChain.ResizeBuffers(2, (uint)width, (uint)height, Format.B8G8R8A8_UNorm, SwapChainFlags.None).CheckError();
        CreateRenderTargetView();
    }

    public void ClearAndPresent(Color4 color)
    {
        if (_renderTargetView is null)
        {
            return;
        }

        _context.ClearRenderTargetView(_renderTargetView, color);
        _swapChain.Present(PresentSyncInterval, PresentFlags.None);
    }

    public void PresentFrame(ScreenFramebufferSnapshot frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Width <= 0 || frame.Height <= 0 || frame.BgraPixels.Length != frame.Width * frame.Height)
        {
            throw new ArgumentException("Framebuffer dimensions do not match the pixel buffer.", nameof(frame));
        }

        if (_backBuffer is null || _width <= 0 || _height <= 0)
        {
            return;
        }

        var pixels = frame.Width == _width && frame.Height == _height
            ? frame.BgraPixels
            : ScaleFramePixels(frame, _width, _height);

        _context.UpdateSubresource(
            pixels.AsSpan(),
            _backBuffer,
            0,
            (uint)(_width * sizeof(uint)),
            0,
            null);
        _swapChain.Present(PresentSyncInterval, PresentFlags.None);
    }

    internal static uint[] ScaleFramePixels(ScreenFramebufferSnapshot frame, int targetWidth, int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (targetWidth <= 0 || targetHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetWidth), "Target dimensions must be positive.");
        }

        if (frame.Width == targetWidth && frame.Height == targetHeight)
        {
            return frame.BgraPixels;
        }

        var scale = Math.Min((double)targetWidth / frame.Width, (double)targetHeight / frame.Height);
        var scaledWidth = Math.Max(1, (int)Math.Floor(frame.Width * scale));
        var scaledHeight = Math.Max(1, (int)Math.Floor(frame.Height * scale));
        var offsetX = (targetWidth - scaledWidth) / 2;
        var offsetY = (targetHeight - scaledHeight) / 2;
        var scaled = new uint[targetWidth * targetHeight];
        Array.Fill(scaled, 0xFF000000u);

        for (var y = 0; y < scaledHeight; y++)
        {
            var sourceY = y * frame.Height / scaledHeight;
            var sourceRow = sourceY * frame.Width;
            var targetRow = (offsetY + y) * targetWidth;
            for (var x = 0; x < scaledWidth; x++)
            {
                var sourceX = x * frame.Width / scaledWidth;
                scaled[targetRow + offsetX + x] = frame.BgraPixels[sourceRow + sourceX];
            }
        }

        return scaled;
    }

    public void Dispose()
    {
        _renderTargetView?.Dispose();
        _backBuffer?.Dispose();
        _swapChain.Dispose();
        _context.Dispose();
        _device.Dispose();
    }

    private void CreateRenderTargetView()
    {
        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = _device.CreateRenderTargetView(_backBuffer);
        _context.OMSetRenderTargets(_renderTargetView);
    }
}
