namespace Axiom.Ide.Graphics;

public sealed class ScreenFramebuffer
{
    private readonly int[] _colorIndices;
    private readonly uint[] _palette;

    public ScreenFramebuffer(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Framebuffer width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Framebuffer height must be positive.");
        }

        Width = width;
        Height = height;
        _colorIndices = new int[width * height];
        _palette = CreateDefaultPalette();
    }

    public int Width { get; }

    public int Height { get; }

    public void Clear()
    {
        Array.Clear(_colorIndices);
    }

    public void ResetPalette()
    {
        var defaultPalette = CreateDefaultPalette();
        Array.Copy(defaultPalette, _palette, _palette.Length);
    }

    public void SetPaletteColor(int index, int color)
    {
        _palette[NormalizeColorIndex(index)] = MapColorToBgra(color);
    }

    public void CopyFrom(ScreenFramebuffer source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Width != Width || source.Height != Height)
        {
            throw new InvalidOperationException("Framebuffer sizes must match.");
        }

        Array.Copy(source._colorIndices, _colorIndices, _colorIndices.Length);
        Array.Copy(source._palette, _palette, _palette.Length);
    }

    public void SetPixel(long x, long y, int color)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        _colorIndices[GetOffset((int)x, (int)y)] = NormalizeColorIndex(color);
    }

    public int GetPixel(long x, long y)
    {
        if (!IsInside(x, y))
        {
            return 0;
        }

        return _colorIndices[GetOffset((int)x, (int)y)];
    }

    public void FillRectangle(long left, long top, long right, long bottom, int color)
    {
        var clippedLeft = Math.Max(0, left);
        var clippedTop = Math.Max(0, top);
        var clippedRight = Math.Min(Width - 1L, right);
        var clippedBottom = Math.Min(Height - 1L, bottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        var colorIndex = NormalizeColorIndex(color);
        for (var y = (int)clippedTop; y <= clippedBottom; y++)
        {
            var rowOffset = y * Width;
            for (var x = (int)clippedLeft; x <= clippedRight; x++)
            {
                _colorIndices[rowOffset + x] = colorIndex;
            }
        }
    }

    public ScreenFramebufferSnapshot CreateSnapshot()
    {
        var pixels = new uint[_colorIndices.Length];
        for (var index = 0; index < _colorIndices.Length; index++)
        {
            pixels[index] = MapPaletteColor(_colorIndices[index]);
        }

        return new ScreenFramebufferSnapshot(Width, Height, pixels);
    }

    public ScreenFramebufferSnapshot CreateSnapshot(
        string graphicsText,
        int textForeground,
        int? textBackground,
        string? inputText = null)
    {
        var snapshot = CreateSnapshot();
        var foreground = MapPaletteColor(textForeground);
        var background = textBackground.HasValue ? MapPaletteColor(textBackground.Value) : (uint?)null;

        ScreenTextRasterizer.DrawText(
            snapshot.BgraPixels,
            snapshot.Width,
            snapshot.Height,
            0,
            0,
            graphicsText,
            foreground,
            background);

        if (!string.IsNullOrEmpty(inputText))
        {
            var inputY = Math.Max(0, snapshot.Height - ScreenTextRasterizer.CellHeight);
            ScreenTextRasterizer.DrawText(
                snapshot.BgraPixels,
                snapshot.Width,
                snapshot.Height,
                0,
                inputY,
                inputText,
                foreground,
                MapPaletteColor(0));
        }

        return snapshot;
    }

    public ScreenFramebufferSnapshot CreateSnapshot(
        ScreenTextBuffer graphicsText,
        int inputForeground,
        int? inputBackground,
        string? inputText = null,
        int inputRow = 1,
        int inputColumn = 1,
        int firstVisibleRow = 1,
        int? maxVisibleRows = null,
        int? maxVisibleColumns = null)
    {
        ArgumentNullException.ThrowIfNull(graphicsText);

        var snapshot = CreateSnapshot();

        firstVisibleRow = Math.Max(1, firstVisibleRow);
        var visibleRowCount = maxVisibleRows ?? int.MaxValue;
        var visibleColumnCount = maxVisibleColumns ?? int.MaxValue;
        var lastVisibleRow = visibleRowCount <= 0
            ? firstVisibleRow - 1
            : firstVisibleRow + visibleRowCount - 1;

        for (var row = firstVisibleRow - 1; row < graphicsText.Lines.Count && row <= lastVisibleRow - 1; row++)
        {
            var line = graphicsText.Lines[row];
            var visibleRow = row - (firstVisibleRow - 1);
            var columnLimit = Math.Min(line.Count, visibleColumnCount);
            for (var column = 0; column < columnLimit; column++)
            {
                var cell = line[column];
                ScreenTextRasterizer.DrawGlyph(
                    snapshot.BgraPixels,
                    snapshot.Width,
                    snapshot.Height,
                    column * ScreenTextRasterizer.CellWidth,
                    visibleRow * ScreenTextRasterizer.CellHeight,
                    cell.Character,
                    MapPaletteColor(cell.Foreground),
                    cell.Background.HasValue ? MapPaletteColor(cell.Background.Value) : null);
            }
        }

        if (!string.IsNullOrEmpty(inputText))
        {
            var visibleInputRow = inputRow - firstVisibleRow + 1;
            if (visibleInputRow >= 1 && visibleInputRow <= visibleRowCount)
            {
                var inputX = Math.Max(0, inputColumn - 1) * ScreenTextRasterizer.CellWidth;
                var inputY = Math.Max(0, visibleInputRow - 1) * ScreenTextRasterizer.CellHeight;
                ScreenTextRasterizer.DrawText(
                    snapshot.BgraPixels,
                    snapshot.Width,
                    snapshot.Height,
                    inputX,
                    inputY,
                    inputText,
                    MapPaletteColor(inputForeground),
                    inputBackground.HasValue ? MapPaletteColor(inputBackground.Value) : null);
            }
        }

        return snapshot;
    }

    private bool IsInside(long x, long y) =>
        x >= 0 &&
        y >= 0 &&
        x < Width &&
        y < Height;

    private int GetOffset(int x, int y) => y * Width + x;

    public static int NormalizeColorIndex(int color) => (int)(Math.Abs((long)color) % 256);

    private uint MapPaletteColor(int color) => _palette[NormalizeColorIndex(color)];

    private static uint[] CreateDefaultPalette()
    {
        var palette = new uint[256];
        for (var index = 0; index < palette.Length; index++)
        {
            palette[index] = MapColorToBgra(index);
        }

        return palette;
    }

    public static uint MapColorToBgra(int color) => NormalizeColorIndex(color) switch
    {
        0 => 0xFF000000,
        1 => 0xFF0000AA,
        2 => 0xFF00AA00,
        3 => 0xFF00AAAA,
        4 => 0xFFAA0000,
        5 => 0xFFAA00AA,
        6 => 0xFFAA5500,
        7 => 0xFFAAAAAA,
        8 => 0xFF555555,
        9 => 0xFF5555FF,
        10 => 0xFF55FF55,
        11 => 0xFF55FFFF,
        12 => 0xFFFF5555,
        13 => 0xFFFF55FF,
        14 => 0xFFFFFF55,
        15 => 0xFFFFFFFF,
        var colorIndex and >= 16 and <= 231 => MapRgbCubeColorToBgra(colorIndex),
        var colorIndex => MapGrayColorToBgra(colorIndex)
    };

    private static uint MapRgbCubeColorToBgra(int colorIndex)
    {
        var cubeIndex = colorIndex - 16;
        var red = cubeIndex / 36;
        var green = cubeIndex / 6 % 6;
        var blue = cubeIndex % 6;

        return ToBgra(GetRgbCubeComponent(red), GetRgbCubeComponent(green), GetRgbCubeComponent(blue));
    }

    private static uint MapGrayColorToBgra(int colorIndex)
    {
        var component = 8 + ((colorIndex - 232) * 10);
        return ToBgra(component, component, component);
    }

    private static int GetRgbCubeComponent(int component) =>
        component == 0 ? 0 : 55 + (component * 40);

    private static uint ToBgra(int red, int green, int blue) =>
        0xFF000000u | ((uint)red << 16) | ((uint)green << 8) | (uint)blue;
}

public sealed record ScreenFramebufferSnapshot(int Width, int Height, uint[] BgraPixels);
