using System.Runtime.InteropServices;
using Axiom.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Axiom.Ide.Graphics;

// Все методы вызываются владельцем одного графического контекста.
// Рабочая и опубликованная поверхности никогда не разделяют CPU-буфер кадра.
internal sealed class CanvasGpuSurface : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly ID3D11Texture2D _working = null!;
    private readonly ID3D11Texture2D _published = null!;
    private readonly ID3D11UnorderedAccessView _target = null!;
    private readonly ID3D11ComputeShader _compute = null!;
    private readonly ID3D11Buffer _parameters = null!;
    private readonly Dictionary<RuntimeImageValue, ImageTexture> _images = new(ReferenceEqualityComparer.Instance);
    private const long ImageCacheBudget = 64L * 1024 * 1024;
    private long _imageBytes;
    private long _imageAccess;
    private bool _disposed;
    private CanvasGpuPresenter? _presenter;

    public int Width { get; }
    public int Height { get; }
    public string DriverName { get; }
    public int UploadedImageCount { get; private set; }
    public int CachedImageCount => _images.Count;
    public long ReadbackBytes { get; private set; }
    private readonly Dictionary<char, RuntimeImageValue> _glyphs = new();

    public CanvasGpuSurface(int width, int height, bool forceSoftware = false)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
            throw new ArgumentOutOfRangeException(nameof(width), "Размер поверхности DirectX должен быть от 1 до 16384 пикселей.");
        Width = width;
        Height = height;
        (_device, _context, DriverName) = CanvasDeviceFactory.Select(forceSoftware);
        // Второй канал сохраняет индекс PSET; 0xffffffff обозначает RGB-пиксель.
        var description = new Texture2DDescription
        {
            Width = (uint)width, Height = (uint)height, MipLevels = 1, ArraySize = 1,
            Format = Format.R32G32_UInt, SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default, BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource
        };
        try
        {
            _working = _device.CreateTexture2D(description);
            _published = _device.CreateTexture2D(description);
            _target = _device.CreateUnorderedAccessView(_working);
            _compute = _device.CreateComputeShader(CanvasGpuShaders.Compute.Span);
            _parameters = _device.CreateBuffer(64, BindFlags.ConstantBuffer);
            Clear(0); Publish();
        }
        catch { Dispose(); throw; }
    }

    public void Clear(int color) => Fill(0, 0, Width - 1L, Height - 1L, color);

    public void DrawLine(long x1, long y1, long x2, long y2, int color,
        long clipLeft, long clipTop, long clipRight, long clipBottom, uint paletteIndex = uint.MaxValue)
    {
        // Большие координаты обрабатываются отдельно без переполнения 32-битного шейдера.
        if (Math.Abs((double)x1) > 32768 || Math.Abs((double)y1) > 32768 ||
            Math.Abs((double)x2) > 32768 || Math.Abs((double)y2) > 32768)
        {
            DrawLongLine(x1, y1, x2, y2, color, clipLeft, clipTop, clipRight, clipBottom, paletteIndex);
            return;
        }
        var parameters = ShapeParameters(color, clipLeft, clipTop, clipRight, clipBottom, paletteIndex);
        parameters[4] = unchecked((uint)(int)x1); parameters[5] = unchecked((uint)(int)y1);
        parameters[6] = unchecked((uint)(int)x2); parameters[7] = unchecked((uint)(int)y2);
        parameters[12] = 3;
        Dispatch(parameters, null, singleGroup: true);
    }

    private void DrawLongLine(long x1, long y1, long x2, long y2, int color,
        long left, long top, long right, long bottom, uint paletteIndex)
    {
        Int128 dx = Int128.Abs((Int128)x2 - x1), dy = Int128.Abs((Int128)y2 - y1);
        var sx = x1 < x2 ? 1 : -1; var sy = y1 < y2 ? 1 : -1;
        // Геометрия ограничена видимой осью; полный внеэкранный отрезок не обходится.
        if (dx >= dy)
        {
            for (var x = Math.Max(0, left); x <= Math.Min(Width - 1L, right); x++)
            {
                var k = ((Int128)x - x1) * sx;
                if (k < 0 || k > dx) continue;
                var y = (Int128)y1 + sy * (dx == 0 ? 0 : (k * dy + dx / 2) / dx);
                if (y >= top && y <= bottom) Fill(x, (long)y, x, (long)y, color, paletteIndex);
            }
        }
        else
        {
            for (var y = Math.Max(0, top); y <= Math.Min(Height - 1L, bottom); y++)
            {
                var k = ((Int128)y - y1) * sy;
                if (k < 0 || k > dy) continue;
                var x = (Int128)x1 + sx * ((k * dx + dy / 2) / dy);
                if (x >= left && x <= right) Fill((long)x, y, (long)x, y, color, paletteIndex);
            }
        }
    }

    public void DrawCircle(long x, long y, long radius, int color, bool filled,
        long left, long top, long right, long bottom)
    {
        if (radius < 0) return;
        if (radius <= 1024 && Math.Abs((double)x) <= 32768 && Math.Abs((double)y) <= 32768)
        {
            if (radius == 0) { if (x >= left && x <= right && y >= top && y <= bottom) Fill(x, y, x, y, color); return; }
            var parameters = ShapeParameters(color, Math.Max(left, x - radius), Math.Max(top, y - radius),
                Math.Min(right, x + radius), Math.Min(bottom, y + radius), uint.MaxValue);
            parameters[4] = unchecked((uint)(int)x); parameters[5] = unchecked((uint)(int)y);
            parameters[6] = (uint)radius; parameters[12] = filled ? 2u : 4u;
            Dispatch(parameters, null, singleGroup: !filled);
            return;
        }
        if (!filled)
        {
            GraphicsEllipseRasterizer.Rasterize(x, y, radius, radius, true, 0, Math.Tau, Width, Height,
                (px, py) => { if (px >= left && px <= right && py >= top && py <= bottom) Fill(px, py, px, py, color); });
            return;
        }
        for (var row = Math.Max(0, top); row <= Math.Min(Height - 1L, bottom); row++)
        {
            var distance = (double)row - y;
            if (Math.Abs(distance) > radius) continue;
            var span = Math.Floor(Math.Sqrt(Math.Max(0, (double)radius * radius - distance * distance)));
            var start = Math.Max(left, Math.Max(0, (double)x - span));
            var end = Math.Min(right, Math.Min(Width - 1L, (double)x + span));
            if (start <= end) Fill((long)start, row, (long)end, row, color);
        }
    }

    public void DrawText(string text, long x, long y, int color, long left, long top, long right, long bottom)
    {
        var cursorX = x; var cursorY = y;
        foreach (var ch in text)
        {
            if (ch == '\r') continue;
            if (ch == '\n') { cursorX = x; cursorY += ScreenTextRasterizer.CellHeight; continue; }
            if (!_glyphs.TryGetValue(ch, out var glyph))
            {
                var pixels = new uint[ScreenTextRasterizer.CellWidth * ScreenTextRasterizer.CellHeight];
                ScreenTextRasterizer.DrawGlyph(pixels, ScreenTextRasterizer.CellWidth, ScreenTextRasterizer.CellHeight,
                    0, 0, ch, 0xffffffff, 0, 0, ScreenTextRasterizer.CellWidth - 1, ScreenTextRasterizer.CellHeight - 1, null);
                glyph = new RuntimeImageValue(ScreenTextRasterizer.CellWidth, ScreenTextRasterizer.CellHeight, pixels);
                _glyphs.Add(ch, glyph);
            }
            DrawImageRegion(glyph, 0, 0, glyph.Width, glyph.Height, cursorX, cursorY,
                glyph.Width, glyph.Height, left, top, right, bottom, color);
            cursorX += ScreenTextRasterizer.CellWidth;
        }
        // Прежняя реализация переводит всю обрезанную область текста в RGB,
        // включая пробелы и промежутки между строками; индекс POINT тоже сохраняет этот договор.
        var parameters = ShapeParameters(color, Math.Max(left, x), Math.Max(top, y),
            Math.Min(right, x + CanvasTextMetrics.MeasureWidth(text) - 1),
            Math.Min(bottom, y + CanvasTextMetrics.MeasureHeight(text) - 1), uint.MaxValue);
        parameters[12] = 7;
        Dispatch(parameters, null);
    }

    private uint[] ShapeParameters(int color, long left, long top, long right, long bottom, uint index)
    {
        left = Math.Max(0, left); top = Math.Max(0, top);
        right = Math.Min(Width - 1L, right); bottom = Math.Min(Height - 1L, bottom);
        var p = new uint[16];
        p[0] = (uint)left; p[1] = (uint)top;
        p[2] = (uint)Math.Max(0, right - left + 1); p[3] = (uint)Math.Max(0, bottom - top + 1);
        p[10] = ScreenFramebuffer.MapRgbToBgra(color); p[11] = index;
        return p;
    }

    public void Fill(long left, long top, long right, long bottom, int color, uint paletteIndex = uint.MaxValue)
    {
        left = Math.Max(0, left); top = Math.Max(0, top);
        right = Math.Min(Width - 1L, right); bottom = Math.Min(Height - 1L, bottom);
        if (left > right || top > bottom) return;
        Span<uint> parameters = stackalloc uint[16];
        parameters.Clear();
        parameters[0] = (uint)left; parameters[1] = (uint)top;
        parameters[2] = (uint)(right - left + 1); parameters[3] = (uint)(bottom - top + 1);
        parameters[10] = ScreenFramebuffer.MapRgbToBgra(color); parameters[11] = paletteIndex;
        Dispatch(parameters, null);
    }

    public void DrawImageRegion(RuntimeImageValue image, int sourceX, int sourceY, int sourceWidth, int sourceHeight,
        long left, long top, int width, int height, long clipLeft, long clipTop, long clipRight, long clipBottom, int? tint = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (sourceWidth <= 0 || sourceHeight <= 0 || sourceX < 0 || sourceY < 0 ||
            (long)sourceX + sourceWidth > image.Width || (long)sourceY + sourceHeight > image.Height)
            throw new ArgumentOutOfRangeException(nameof(sourceWidth));
        var x = Math.Max(Math.Max(0, left), clipLeft);
        var y = Math.Max(Math.Max(0, top), clipTop);
        var right = Math.Min(Math.Min(Width - 1L, SaturatingAdd(left, width - 1L)), clipRight);
        var bottom = Math.Min(Math.Min(Height - 1L, SaturatingAdd(top, height - 1L)), clipBottom);
        if (x > right || y > bottom) return;
        var temporary = false;
        if (!_images.TryGetValue(image, out var texture))
        {
            var bytes = (long)image.Width * image.Height * sizeof(uint);
            temporary = bytes > ImageCacheBudget;
            while (!temporary && _images.Count != 0 && (_imageBytes + bytes > ImageCacheBudget || _images.Count >= 256))
            {
                var oldest = _images.MinBy(pair => pair.Value.LastUsed);
                _imageBytes -= oldest.Value.Bytes;
                oldest.Value.Dispose(); _images.Remove(oldest.Key);
            }
            texture = new ImageTexture(_device, _context, image);
            UploadedImageCount++;
            if (!temporary) { _images.Add(image, texture); _imageBytes += bytes; }
        }
        texture.LastUsed = ++_imageAccess;
        Span<uint> parameters = stackalloc uint[16];
        parameters.Clear();
        parameters[0] = (uint)x; parameters[1] = (uint)y;
        parameters[2] = (uint)(right - x + 1); parameters[3] = (uint)(bottom - y + 1);
        parameters[4] = unchecked((uint)(int)left); parameters[5] = unchecked((uint)(int)top);
        parameters[6] = (uint)sourceX; parameters[7] = (uint)sourceY;
        parameters[8] = (uint)(((long)sourceWidth << 16) / width);
        parameters[9] = (uint)(((long)sourceHeight << 16) / height);
        parameters[10] = tint.HasValue ? ScreenFramebuffer.MapRgbToBgra(tint.Value) : 0;
        parameters[12] = tint.HasValue ? 5u : 1u;
        try { Dispatch(parameters, texture.View); }
        finally { if (temporary) texture.Dispose(); }
    }

    private void Dispatch(ReadOnlySpan<uint> parameters, ID3D11ShaderResourceView? image, bool singleGroup = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (parameters[2] == 0 || parameters[3] == 0) return;
        _context.UpdateSubresource(parameters, _parameters);
        _context.CSSetShader(_compute);
        _context.CSSetConstantBuffer(0, _parameters);
        _context.CSSetUnorderedAccessView(0, _target);
        _context.CSSetShaderResource(0, image!);
        _context.Dispatch(singleGroup ? 1u : (parameters[2] + 7) / 8, singleGroup ? 1u : (parameters[3] + 7) / 8, 1);
        _context.CSSetUnorderedAccessView(0, null!);
        _context.CSSetShaderResource(0, null!);
    }

    public void Publish() => _context.CopyResource(_published, _working);

    public void AttachWindow(IntPtr hwnd, int width, int height)
    {
        _presenter?.Dispose();
        _presenter = null;
        _presenter = new CanvasGpuPresenter(_device, _context, _published, hwnd, width, height);
    }
    public void DetachWindow() { _presenter?.Dispose(); _presenter = null; }
    public void Render(bool smooth) => _presenter?.Render(smooth);
    internal void DrawDisplayedFrame(bool smooth) => _presenter!.DrawFrame(smooth);
    public void Resize(int width, int height, bool smooth) { _presenter?.Resize(width, height); Render(smooth); }

    public int GetPixel(long x, long y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return 0;
        var description = _working.Description;
        description.Width = 1; description.Height = 1;
        description.Usage = ResourceUsage.Staging; description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        using var staging = _device.CreateTexture2D(description);
        _context.CopySubresourceRegion(staging, 0, 0, 0, 0, _working, 0,
            new Vortice.Mathematics.Box((int)x, (int)y, 0, (int)x + 1, (int)y + 1, 1));
        ReadbackBytes += 8;
        var mapped = _context.Map(staging, 0, MapMode.Read);
        try
        {
            var index = unchecked((uint)Marshal.ReadInt32(mapped.DataPointer, 4));
            return index == uint.MaxValue
                ? ScreenFramebuffer.FindNearestDefaultPaletteIndex(unchecked((uint)Marshal.ReadInt32(mapped.DataPointer)))
                : (int)index;
        }
        finally { _context.Unmap(staging, 0); }
    }

    public uint[] ReadWorkingRegion(long x, long y, int width, int height)
    {
        var result = new uint[checked(width * height * 2)];
        var left = Math.Max(0, x); var top = Math.Max(0, y);
        var right = Math.Min(Width, SaturatingAdd(x, width)); var bottom = Math.Min(Height, SaturatingAdd(y, height));
        if (left >= right || top >= bottom) return result;
        var description = _working.Description;
        description.Width = (uint)(right - left); description.Height = (uint)(bottom - top);
        description.Usage = ResourceUsage.Staging; description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        using var staging = _device.CreateTexture2D(description);
        _context.CopySubresourceRegion(staging, 0, 0, 0, 0, _working, 0,
            new Vortice.Mathematics.Box((int)left, (int)top, 0, (int)right, (int)bottom, 1));
        ReadbackBytes += (long)description.Width * description.Height * 8;
        var mapped = _context.Map(staging, 0, MapMode.Read);
        try
        {
            var row = new int[(int)description.Width * 2];
            for (var py = top; py < bottom; py++)
            {
                Marshal.Copy(IntPtr.Add(mapped.DataPointer, (int)(py - top) * (int)mapped.RowPitch), row, 0, row.Length);
                Buffer.BlockCopy(row, 0, result, checked((int)(((py - y) * width + left - x) * 8)), row.Length * sizeof(uint));
            }
            return result;
        }
        finally { _context.Unmap(staging, 0); }
    }

    public int[] ReadWorkingIndices(long x, long y, int width, int height) => ConvertIndices(ReadWorkingRegion(x, y, width, height));

    public static int[] ConvertIndices(uint[] pairs)
    {
        var result = new int[pairs.Length / 2];
        var colors = new Dictionary<uint, int>();
        for (var i = 0; i < result.Length; i++)
        {
            var index = pairs[i * 2 + 1];
            if (index != uint.MaxValue) { result[i] = (int)index; continue; }
            var color = pairs[i * 2];
            if (!colors.TryGetValue(color, out var nearest))
            {
                nearest = ScreenFramebuffer.FindNearestDefaultPaletteIndex(color);
                colors[color] = nearest;
            }
            result[i] = nearest;
        }
        return result;
    }

    public void UploadWorkingRegion(int x, int y, int width, int height, uint[] pairs) =>
        _context.UpdateSubresource(pairs.AsSpan(), _working, 0, (uint)(width * 8), 0,
            new Vortice.Mathematics.Box(x, y, 0, x + width, y + height, 1));

    // Только явный запрос снимка использует передачу GPU → CPU.
    public uint[] ReadPublished()
    {
        var description = _published.Description;
        description.Usage = ResourceUsage.Staging;
        description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        using var staging = _device.CreateTexture2D(description);
        _context.CopyResource(staging, _published);
        ReadbackBytes += (long)Width * Height * 8;
        var mapped = _context.Map(staging, 0, MapMode.Read);
        try
        {
            var pixels = new uint[checked(Width * Height)];
            var row = new int[Width * 2];
            for (var y = 0; y < Height; y++)
            {
                Marshal.Copy(IntPtr.Add(mapped.DataPointer, y * (int)mapped.RowPitch), row, 0, row.Length);
                for (var x = 0; x < Width; x++) pixels[y * Width + x] = unchecked((uint)row[x * 2]);
            }
            return pixels;
        }
        finally { _context.Unmap(staging, 0); }
    }

    private static long SaturatingAdd(long value, long amount) => value > long.MaxValue - amount ? long.MaxValue : value + amount;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _presenter?.Dispose();
        try { _context.ClearState(); } catch { }
        foreach (var image in _images.Values) image.Dispose();
        _images.Clear();
        _parameters?.Dispose(); _compute?.Dispose(); _target?.Dispose();
        _published?.Dispose(); _working?.Dispose(); _context.Dispose(); _device.Dispose();
    }

    private sealed class ImageTexture : IDisposable
    {
        private readonly ID3D11Texture2D _texture;
        public ID3D11ShaderResourceView View { get; }
        public long Bytes { get; }
        public long LastUsed { get; set; }
        public ImageTexture(ID3D11Device device, ID3D11DeviceContext context, RuntimeImageValue image)
        {
            Bytes = (long)image.Width * image.Height * sizeof(uint);
            _texture = device.CreateTexture2D(new Texture2DDescription
            {
                Width = (uint)image.Width, Height = (uint)image.Height, MipLevels = 1, ArraySize = 1,
                Format = Format.R32_UInt, SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default, BindFlags = BindFlags.ShaderResource
            });
            try
            {
                context.UpdateSubresource(image.BgraPixels.AsSpan(), _texture, 0, (uint)(image.Width * sizeof(uint)), 0, null);
                View = device.CreateShaderResourceView(_texture);
            }
            catch { _texture.Dispose(); throw; }
        }
        public void Dispose() { View.Dispose(); _texture.Dispose(); }
    }
}
