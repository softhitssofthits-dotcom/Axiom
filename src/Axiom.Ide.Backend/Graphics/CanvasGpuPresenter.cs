using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Axiom.Ide.Graphics;

internal sealed class CanvasGpuPresenter : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGISwapChain _swapChain = null!;
    private readonly ID3D11ShaderResourceView _frameView = null!;
    private readonly ID3D11VertexShader _vertex = null!;
    private readonly ID3D11PixelShader _pixel = null!;
    private readonly ID3D11Buffer _parameters = null!;
    private ID3D11Texture2D? _backBuffer;
    private ID3D11RenderTargetView? _target;
    private readonly int _sourceWidth, _sourceHeight;
    private int _width, _height;

    public CanvasGpuPresenter(ID3D11Device device, ID3D11DeviceContext context, ID3D11Texture2D frame,
        IntPtr hwnd, int width, int height)
    {
        _device = device; _context = context;
        _sourceWidth = (int)frame.Description.Width; _sourceHeight = (int)frame.Description.Height;
        _width = width; _height = height;
        using var dxgiDevice = device.QueryInterface<IDXGIDevice>();
        using var adapter = dxgiDevice.GetAdapter();
        using var factory = adapter.GetParent<IDXGIFactory>();
        try
        {
            _swapChain = factory.CreateSwapChain(device, new SwapChainDescription
            {
                BufferCount = 2, BufferDescription = new ModeDescription((uint)width, (uint)height, Format.B8G8R8A8_UNorm),
                BufferUsage = Usage.RenderTargetOutput, OutputWindow = hwnd,
                SampleDescription = new SampleDescription(1, 0), Windowed = true, SwapEffect = SwapEffect.Discard
            });
            _frameView = device.CreateShaderResourceView(frame);
            _vertex = device.CreateVertexShader(CanvasGpuShaders.Vertex.Span);
            _pixel = device.CreatePixelShader(CanvasGpuShaders.Pixel.Span);
            _parameters = device.CreateBuffer(32, BindFlags.ConstantBuffer);
            CreateTarget();
        }
        catch { Dispose(); throw; }
    }

    private void CreateTarget()
    {
        _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _target = _device.CreateRenderTargetView(_backBuffer);
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0 || (width == _width && height == _height)) return;
        _context.OMSetRenderTargets([], null);
        _target?.Dispose(); _target = null; _backBuffer?.Dispose(); _backBuffer = null;
        _swapChain.ResizeBuffers(2, (uint)width, (uint)height, Format.B8G8R8A8_UNorm, SwapChainFlags.None).CheckError();
        _width = width; _height = height; CreateTarget();
    }

    public void Render(bool smooth)
    {
        DrawFrame(smooth);
        _swapChain.Present(1, PresentFlags.None).CheckError();
    }

    internal void DrawFrame(bool smooth)
    {
        var scale = Math.Min((double)_width / _sourceWidth, (double)_height / _sourceHeight);
        var width = Math.Max(1, (int)Math.Floor(_sourceWidth * scale));
        var height = Math.Max(1, (int)Math.Floor(_sourceHeight * scale));
        var x = (_width - width) / 2; var y = (_height - height) / 2;
        Span<uint> p = stackalloc uint[8] { (uint)_sourceWidth, (uint)_sourceHeight, (uint)width, (uint)height,
            (uint)x, (uint)y, smooth ? 1u : 0u, 0u };
        _context.UpdateSubresource(p, _parameters);
        _context.ClearRenderTargetView(_target!, new Color4(0, 0, 0, 1));
        _context.OMSetRenderTargets(_target!);
        _context.RSSetViewport(new Viewport(x, y, width, height));
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.VSSetShader(_vertex); _context.PSSetShader(_pixel);
        _context.PSSetConstantBuffer(0, _parameters); _context.PSSetShaderResource(0, _frameView);
        _context.Draw(3, 0); _context.PSSetShaderResource(0, null!);
    }

    public void Dispose()
    {
        try { _context.OMSetRenderTargets([], null); } catch { }
        _target?.Dispose(); _backBuffer?.Dispose(); _parameters?.Dispose();
        _pixel?.Dispose(); _vertex?.Dispose(); _frameView?.Dispose(); _swapChain?.Dispose();
    }
}
