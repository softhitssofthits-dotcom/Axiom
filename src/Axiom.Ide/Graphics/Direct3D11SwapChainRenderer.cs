using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;

namespace Axiom.Ide.Graphics;

internal sealed class Direct3D11SwapChainRenderer : IDisposable
{
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
        _swapChain.Present(1, PresentFlags.None);
    }

    public void PresentFrame(ScreenFramebufferSnapshot frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Width <= 0 || frame.Height <= 0 || frame.BgraPixels.Length != frame.Width * frame.Height)
        {
            throw new ArgumentException("Framebuffer dimensions do not match the pixel buffer.", nameof(frame));
        }

        if (frame.Width != _width || frame.Height != _height)
        {
            Resize(frame.Width, frame.Height);
        }

        if (_backBuffer is null)
        {
            return;
        }

        _context.UpdateSubresource(
            frame.BgraPixels.AsSpan(),
            _backBuffer,
            0,
            (uint)(frame.Width * sizeof(uint)),
            0,
            null);
        _swapChain.Present(1, PresentFlags.None);
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
