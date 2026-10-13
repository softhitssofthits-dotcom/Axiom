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
    private ID3D11Texture2D? _frameTexture;
    private ID3D11ShaderResourceView? _frameView;
    private ID3D11VertexShader? _vertexShader;
    private ID3D11PixelShader? _pixelShader;
    private ID3D11SamplerState? _linearSampler;
    private ID3D11Buffer? _frameParameters;
    private uint[]? _uploadedPixels;
    private int _width;
    private int _height;
    private bool _disposed;

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
        try
        {
            _vertexShader = _device.CreateVertexShader(ScreenFrameShaders.Vertex.Span);
            _pixelShader = _device.CreatePixelShader(ScreenFrameShaders.Pixel.Span);
            _linearSampler = _device.CreateSamplerState(new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MaxLOD = float.MaxValue
            });
            _frameParameters = _device.CreateBuffer(32, BindFlags.ConstantBuffer);
        }
        catch
        {
            Dispose();
            throw;
        }
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
        DrawFrame(frame);
        _swapChain.Present(PresentSyncInterval, PresentFlags.None);
    }

    internal void DrawFrame(ScreenFramebufferSnapshot frame)
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

        EnsureFrameTexture(frame);
        // Снимки опубликованного кадра неизменны. Перерисовка окна и смена
        // фильтра не требуют повторной загрузки того же логического буфера.
        if (!ReferenceEquals(_uploadedPixels, frame.BgraPixels))
        {
            _context.UpdateSubresource(frame.BgraPixels.AsSpan(), _frameTexture!, 0,
                (uint)(frame.Width * sizeof(uint)), 0, null);
            _uploadedPixels = frame.BgraPixels;
        }

        var scale = Math.Min((double)_width / frame.Width, (double)_height / frame.Height);
        var scaledWidth = Math.Max(1, (int)Math.Floor(frame.Width * scale));
        var scaledHeight = Math.Max(1, (int)Math.Floor(frame.Height * scale));
        var offsetX = (_width - scaledWidth) / 2;
        var offsetY = (_height - scaledHeight) / 2;
        Span<uint> parameters = stackalloc uint[8]
        {
            (uint)frame.Width, (uint)frame.Height, (uint)scaledWidth, (uint)scaledHeight,
            (uint)offsetX, (uint)offsetY, frame.Smooth ? 1u : 0u, 0u
        };
        _context.UpdateSubresource(parameters, _frameParameters!);
        _context.ClearRenderTargetView(_renderTargetView!, new Color4(0, 0, 0, 1));
        _context.OMSetRenderTargets(_renderTargetView!);
        _context.RSSetViewport(new Viewport(offsetX, offsetY, scaledWidth, scaledHeight));
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.VSSetShader(_vertexShader);
        _context.PSSetShader(_pixelShader);
        _context.PSSetConstantBuffer(0, _frameParameters);
        _context.PSSetShaderResource(0, _frameView!);
        _context.PSSetSampler(0, _linearSampler);
        _context.Draw(3, 0);
        _context.PSSetShaderResource(0, null!);
    }

    private void EnsureFrameTexture(ScreenFramebufferSnapshot frame)
    {
        if (_frameTexture is not null && _frameTexture.Description.Width == frame.Width &&
            _frameTexture.Description.Height == frame.Height)
            return;

        _frameView?.Dispose();
        _frameTexture?.Dispose();
        _uploadedPixels = null;
        _frameTexture = _device.CreateTexture2D(new Texture2DDescription
        {
            Width = (uint)frame.Width,
            Height = (uint)frame.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.B8G8R8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource
        });
        _frameView = _device.CreateShaderResourceView(_frameTexture);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try { _context.ClearState(); } catch { /* COM cleanup best-effort */ }
        try { _frameView?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _frameTexture?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _frameParameters?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _linearSampler?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _pixelShader?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _vertexShader?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _renderTargetView?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _backBuffer?.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _swapChain.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _context.Dispose(); } catch { /* COM cleanup best-effort */ }
        try { _device.Dispose(); } catch { /* COM cleanup best-effort */ }
    }

    private void CreateRenderTargetView()
    {
        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = _device.CreateRenderTargetView(_backBuffer);
        _context.OMSetRenderTargets(_renderTargetView);
    }
}
