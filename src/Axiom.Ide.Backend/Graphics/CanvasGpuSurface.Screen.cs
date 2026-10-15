using Axiom.Runtime;
using Vortice.Direct3D11;

namespace Axiom.Ide.Graphics;

internal readonly record struct ScreenGpuTextCell(int X, int Y, char Character, int Foreground, int? Background);

internal sealed partial class CanvasGpuSurface
{
    private sealed class ScreenPage(ID3D11Texture2D texture, ID3D11UnorderedAccessView target)
    {
        public ID3D11Texture2D Texture { get; } = texture;
        public ID3D11UnorderedAccessView Target { get; } = target;
        public uint[] Palette { get; } = Enumerable.Range(0, 256).Select(ScreenFramebuffer.MapColorToBgra).ToArray();
    }

    private ScreenPage?[]? _pages;
    private int _activePage, _visualPage;
    private ID3D11UnorderedAccessView? _publishedTarget;
    private ID3D11Texture2D? _screenTextTexture;
    private ID3D11UnorderedAccessView? _screenTextTarget;
    private ID3D11ShaderResourceView? _screenTextView;
    private ScreenGpuTextCell[] _screenTextCells = [];
    private uint[]? _screenTextPalette;
    public int ScreenTextCompositionCount { get; private set; }
    public int AllocatedScreenPages => _pages?.Count(page => page is not null) ?? 0;

    public void EnableScreenPages()
    {
        if (_pages is not null) return;
        _pages = new ScreenPage?[4];
        _pages[0] = new ScreenPage(_working, _target);
        _publishedTarget = _device.CreateUnorderedAccessView(_published);
        FillIndexed(0, 0, Width - 1, Height - 1, 0);
        PublishScreen([]);
    }

    private ScreenPage GetScreenPage(int page)
    {
        if (_pages is null || page is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(page));
        if (_pages[page] is { } existing) return existing;
        var texture = _device.CreateTexture2D(_working.Description);
        ID3D11UnorderedAccessView? target = null;
        try
        {
            target = _device.CreateUnorderedAccessView(texture);
            var created = new ScreenPage(texture, target);
            var working = _working; var previousTarget = _target;
            try
            {
                _working = texture; _target = target;
                Fill(0, 0, Width - 1, Height - 1, 0, 0);
            }
            finally { _working = working; _target = previousTarget; }
            return _pages[page] = created;
        }
        catch { target?.Dispose(); texture.Dispose(); throw; }
    }

    public void SelectScreenPages(int active, int visual)
    {
        var page = GetScreenPage(active);
        GetScreenPage(visual);
        _activePage = active; _visualPage = visual;
        _working = page.Texture; _target = page.Target;
    }

    private int ScreenRgb(int index) => (int)(GetScreenPage(_activePage).Palette[ScreenFramebuffer.NormalizeColorIndex(index)] & 0xffffff);

    public void FillIndexed(long left, long top, long right, long bottom, int index) =>
        Fill(left, top, right, bottom, ScreenRgb(index), (uint)ScreenFramebuffer.NormalizeColorIndex(index));

    public void DrawIndexedLine(long x1, long y1, long x2, long y2, int index, long left, long top, long right, long bottom) =>
        DrawLine(x1, y1, x2, y2, ScreenRgb(index), left, top, right, bottom, (uint)ScreenFramebuffer.NormalizeColorIndex(index));

    public void DrawIndexedCircle(long x, long y, long radius, int index, long left, long top, long right, long bottom) =>
        DrawCircle(x, y, radius, ScreenRgb(index), false, left, top, right, bottom, (uint)ScreenFramebuffer.NormalizeColorIndex(index));

    public void SetScreenPalette(int index, int color)
    {
        GetScreenPage(_activePage).Palette[ScreenFramebuffer.NormalizeColorIndex(index)] = ScreenFramebuffer.MapColorToBgra(color);
        RemapScreenPalette();
    }

    public void ResetScreenPalette()
    {
        var palette = GetScreenPage(_activePage).Palette;
        for (var i = 0; i < palette.Length; i++) palette[i] = ScreenFramebuffer.MapColorToBgra(i);
        RemapScreenPalette();
    }

    public void RemapScreenPalette()
    {
        using var palette = new ImageTexture(_device, _context, new RuntimeImageValue(256, 1, GetScreenPage(_activePage).Palette));
        var p = ShapeParameters(0, 0, 0, Width - 1, Height - 1, 0);
        p[12] = 8;
        Dispatch(p, palette.View);
    }

    public void CopyScreenPage(int source, int destination)
    {
        var from = GetScreenPage(source); var to = GetScreenPage(destination);
        if (source == destination) return;
        _context.CopyResource(to.Texture, from.Texture);
        Array.Copy(from.Palette, to.Palette, 256);
    }

    public void PublishScreen(IReadOnlyList<ScreenGpuTextCell> text)
    {
        var page = GetScreenPage(_visualPage);
        _context.CopyResource(_published, page.Texture);
        if (text.Count == 0) return;
        ComposeScreenText(text, page.Palette);
        var working = _working; var target = _target;
        try
        {
            _working = _published; _target = _publishedTarget!;
            var p = ShapeParameters(0, 0, 0, Width - 1, Height - 1, 0);
            p[12] = 11;
            Dispatch(p, null, overlay: _screenTextView);
        }
        finally { _working = working; _target = target; }
    }

    private void ComposeScreenText(IReadOnlyList<ScreenGpuTextCell> text, uint[] palette)
    {
        if (_screenTextPalette is not null && palette.AsSpan().SequenceEqual(_screenTextPalette) && text.SequenceEqual(_screenTextCells)) return;
        if (_screenTextTexture is null)
        {
            _screenTextTexture = _device.CreateTexture2D(_working.Description);
            _screenTextTarget = _device.CreateUnorderedAccessView(_screenTextTexture);
            _screenTextView = _device.CreateShaderResourceView(_screenTextTexture);
        }
        var working = _working; var target = _target;
        try
        {
            _working = _screenTextTexture; _target = _screenTextTarget!;
            var clear = ShapeParameters(0, 0, 0, Width - 1, Height - 1, 0);
            clear[12] = 10; Dispatch(clear, null);
            foreach (var cell in text)
            {
                if (cell.Background is { } background)
                    Fill(cell.X, cell.Y, cell.X + 7, cell.Y + 11,
                        (int)(palette[ScreenFramebuffer.NormalizeColorIndex(background)] & 0xffffff));
                if (!_glyphs.TryGetValue(cell.Character, out var glyph))
                {
                    var pixels = new uint[8 * 12];
                    ScreenTextRasterizer.DrawGlyph(pixels, 8, 12, 0, 0, cell.Character, 0xffffffff, 0, 0, 7, 11, null);
                    _glyphs[cell.Character] = glyph = new RuntimeImageValue(8, 12, pixels);
                }
                DrawImageRegion(glyph, 0, 0, 8, 12, cell.X, cell.Y, 8, 12, 0, 0, Width - 1, Height - 1,
                    (int)(palette[ScreenFramebuffer.NormalizeColorIndex(cell.Foreground)] & 0xffffff));
            }
            _screenTextCells = text.ToArray(); _screenTextPalette = palette.ToArray();
            ScreenTextCompositionCount++;
        }
        finally { _working = working; _target = target; }
    }

    public void DrawIndexedPoints(uint[] points, int color)
    {
        if (points.Length == 0) return;
        var width = Math.Min(1024, points.Length); var height = (points.Length + width - 1) / width;
        var packed = new uint[width * height]; points.CopyTo(packed, 0);
        using var texture = new ImageTexture(_device, _context, new RuntimeImageValue(width, height, packed));
        var p = new uint[16]; p[2] = (uint)width; p[3] = (uint)height; p[4] = (uint)points.Length;
        p[10] = ScreenFramebuffer.MapRgbToBgra(ScreenRgb(color));
        p[11] = (uint)ScreenFramebuffer.NormalizeColorIndex(color); p[12] = 9;
        Dispatch(p, texture.View);
    }

    private void DisposeScreenPages()
    {
        _publishedTarget?.Dispose();
        _screenTextView?.Dispose(); _screenTextTarget?.Dispose(); _screenTextTexture?.Dispose();
        if (_pages is null) return;
        foreach (var page in _pages)
            if (page is not null && !ReferenceEquals(page.Texture, _working))
            { page.Target.Dispose(); page.Texture.Dispose(); }
        _pages = null;
    }
}
