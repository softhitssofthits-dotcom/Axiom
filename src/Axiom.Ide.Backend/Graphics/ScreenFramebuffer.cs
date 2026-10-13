using Axiom.Runtime;

namespace Axiom.Ide.Graphics;

public sealed class ScreenFramebuffer
{
    private readonly int[] _colorIndices;
    private readonly uint[] _palette;
    private readonly uint[] _trueColorPixels;
    private readonly bool[] _hasTrueColor;
    private readonly Dictionary<ScaleMapKey, int[]> _scaleMapCache = [];

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
        _trueColorPixels = new uint[width * height];
        _hasTrueColor = new bool[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public void Clear()
    {
        Array.Clear(_colorIndices);
        Array.Clear(_trueColorPixels);
        Array.Clear(_hasTrueColor);
    }

    public void ClearTrueColor(int color)
    {
        var pixel = MapRgbToBgra(color);
        Array.Clear(_colorIndices);
        Array.Fill(_trueColorPixels, pixel);
        Array.Fill(_hasTrueColor, true);
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
        Array.Copy(source._trueColorPixels, _trueColorPixels, _trueColorPixels.Length);
        Array.Copy(source._hasTrueColor, _hasTrueColor, _hasTrueColor.Length);
    }

    public void SetPixel(long x, long y, int color)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        var offset = GetOffset((int)x, (int)y);
        _colorIndices[offset] = NormalizeColorIndex(color);
        _trueColorPixels[offset] = 0;
        _hasTrueColor[offset] = false;
    }

    public void SetTrueColorPixel(long x, long y, int color)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        var offset = GetOffset((int)x, (int)y);
        _trueColorPixels[offset] = MapRgbToBgra(color);
        _hasTrueColor[offset] = true;
    }

    public int GetPixel(long x, long y)
    {
        if (!IsInside(x, y))
        {
            return 0;
        }

        var offset = GetOffset((int)x, (int)y);
        return _hasTrueColor[offset]
            ? FindNearestDefaultPaletteIndex(_trueColorPixels[offset])
            : _colorIndices[offset];
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
                var offset = rowOffset + x;
                _colorIndices[offset] = colorIndex;
                _trueColorPixels[offset] = 0;
                _hasTrueColor[offset] = false;
            }
        }
    }

    public void FillTrueColorRectangle(long left, long top, long right, long bottom, int color)
    {
        var clippedLeft = Math.Max(0, left);
        var clippedTop = Math.Max(0, top);
        var clippedRight = Math.Min(Width - 1L, right);
        var clippedBottom = Math.Min(Height - 1L, bottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        var pixel = MapRgbToBgra(color);
        for (var y = (int)clippedTop; y <= clippedBottom; y++)
        {
            var rowOffset = y * Width;
            for (var x = (int)clippedLeft; x <= clippedRight; x++)
            {
                var offset = rowOffset + x;
                _trueColorPixels[offset] = pixel;
                _hasTrueColor[offset] = true;
            }
        }
    }

    public void DrawTrueColorRectangle(long left, long top, long right, long bottom, int color)
    {
        FillTrueColorRectangle(left, top, right, top, color);
        FillTrueColorRectangle(left, bottom, right, bottom, color);
        FillTrueColorRectangle(left, top, left, bottom, color);
        FillTrueColorRectangle(right, top, right, bottom, color);
    }

    public void DrawTrueColorText(long x, long y, string text, int color, long clipLeft, long clipTop, long clipRight, long clipBottom)
    {
        ArgumentNullException.ThrowIfNull(text);

        var startX = checked((int)x);
        var startY = checked((int)y);
        var width = CanvasTextMetrics.MeasureWidth(text);
        var height = CanvasTextMetrics.MeasureHeight(text);
        if (width == 0)
        {
            return;
        }

        var clippedLeft = (int)Math.Max(Math.Max(0, startX), clipLeft);
        var clippedTop = (int)Math.Max(Math.Max(0, startY), clipTop);
        var clippedRight = (int)Math.Min(Math.Min(Width - 1, startX + width - 1), clipRight);
        var clippedBottom = (int)Math.Min(Math.Min(Height - 1, startY + height - 1), clipBottom);
        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        ScreenTextRasterizer.DrawText(
            _trueColorPixels,
            Width,
            Height,
            startX,
            startY,
            text,
            MapRgbToBgra(color),
            clipLeft,
            clipTop,
            clipRight,
            clipBottom);

        for (var row = clippedTop; row <= clippedBottom; row++)
        {
            var rowOffset = row * Width;
            for (var column = clippedLeft; column <= clippedRight; column++)
            {
                _hasTrueColor[rowOffset + column] = true;
            }
        }
    }

    public void DrawImage(
        RuntimeImageValue image,
        long left,
        long top,
        int width,
        int height,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image target size must be positive.");
        }

        var clippedLeft = Math.Max(Math.Max(0, left), clipLeft);
        var clippedTop = Math.Max(Math.Max(0, top), clipTop);
        var clippedRight = Math.Min(Math.Min(Width - 1L, SaturatingAdd(left, width - 1L)), clipRight);
        var clippedBottom = Math.Min(Math.Min(Height - 1L, SaturatingAdd(top, height - 1L)), clipBottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        DrawTrueColorRegion(
            image.BgraPixels,
            image.Width,
            0,
            0,
            image.Width,
            image.Height,
            left,
            top,
            width,
            height,
            clippedLeft,
            clippedTop,
            clippedRight,
            clippedBottom);
    }

    public void DrawSprite(
        RuntimeSpriteValue sprite,
        int frame,
        long left,
        long top,
        int width,
        int height,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom)
    {
        ArgumentNullException.ThrowIfNull(sprite);
        if (frame < 0 || frame >= sprite.FrameCount)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), frame, "Sprite frame index must reference an existing frame.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Sprite target size must be positive.");
        }

        var clippedLeft = Math.Max(Math.Max(0, left), clipLeft);
        var clippedTop = Math.Max(Math.Max(0, top), clipTop);
        var clippedRight = Math.Min(Math.Min(Width - 1L, SaturatingAdd(left, width - 1L)), clipRight);
        var clippedBottom = Math.Min(Math.Min(Height - 1L, SaturatingAdd(top, height - 1L)), clipBottom);

        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        var sourceImage = sprite.Image;
        DrawTrueColorRegion(
            sourceImage.BgraPixels,
            sourceImage.Width,
            sprite.GetFrameLeft(frame),
            sprite.GetFrameTop(frame),
            sprite.FrameWidth,
            sprite.FrameHeight,
            left,
            top,
            width,
            height,
            clippedLeft,
            clippedTop,
            clippedRight,
            clippedBottom);
    }

    public void DrawImageRegion(
        RuntimeImageValue image,
        int sourceX,
        int sourceY,
        int sourceWidth,
        int sourceHeight,
        long left,
        long top,
        int width,
        int height,
        long clipLeft,
        long clipTop,
        long clipRight,
        long clipBottom)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidth), sourceWidth, "Image source size must be positive.");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image target size must be positive.");
        }

        var clippedLeft = Math.Max(Math.Max(0, left), clipLeft);
        var clippedTop = Math.Max(Math.Max(0, top), clipTop);
        var clippedRight = Math.Min(Math.Min(Width - 1L, SaturatingAdd(left, width - 1L)), clipRight);
        var clippedBottom = Math.Min(Math.Min(Height - 1L, SaturatingAdd(top, height - 1L)), clipBottom);
        if (clippedLeft > clippedRight || clippedTop > clippedBottom)
        {
            return;
        }

        DrawTrueColorRegion(
            image.BgraPixels,
            image.Width,
            sourceX,
            sourceY,
            sourceWidth,
            sourceHeight,
            left,
            top,
            width,
            height,
            clippedLeft,
            clippedTop,
            clippedRight,
            clippedBottom);
    }

    private void DrawTrueColorRegion(
        uint[] pixels,
        int sourceImageWidth,
        int sourceLeft,
        int sourceTop,
        int sourceWidth,
        int sourceHeight,
        long destinationLeft,
        long destinationTop,
        int destinationWidth,
        int destinationHeight,
        long clippedLeft,
        long clippedTop,
        long clippedRight,
        long clippedBottom)
    {
        if (destinationWidth == sourceWidth && destinationHeight == sourceHeight)
        {
            var sourceRowStart = sourceTop + checked((int)(clippedTop - destinationTop));
            var sourceColumnStart = sourceLeft + checked((int)(clippedLeft - destinationLeft));
            for (var destinationY = (int)clippedTop; destinationY <= clippedBottom; destinationY++)
            {
                var sourceOffset = sourceRowStart * sourceImageWidth;
                var destinationRowOffset = destinationY * Width;
                var sourceColumn = sourceColumnStart;

                for (var destinationX = (int)clippedLeft; destinationX <= clippedRight; destinationX++, sourceColumn++)
                {
                    WriteTrueColorPixel(pixels[sourceOffset + sourceColumn], destinationRowOffset + destinationX);
                }

                sourceRowStart++;
            }

            return;
        }

        var rowMap = GetScaleMap(sourceHeight, destinationHeight);
        var columnMap = GetScaleMap(sourceWidth, destinationWidth);
        var rowStart = checked((int)(clippedTop - destinationTop));
        var rowEnd = checked((int)(clippedBottom - destinationTop));
        var columnStart = checked((int)(clippedLeft - destinationLeft));
        var columnEnd = checked((int)(clippedRight - destinationLeft));

        for (var targetRow = rowStart; targetRow <= rowEnd; targetRow++)
        {
            var destinationY = checked((int)(destinationTop + targetRow));
            var sourceOffset = (sourceTop + rowMap[targetRow]) * sourceImageWidth;
            var destinationRowOffset = destinationY * Width;

            for (var targetColumn = columnStart; targetColumn <= columnEnd; targetColumn++)
            {
                var destinationX = checked((int)(destinationLeft + targetColumn));
                var sourceColumn = sourceLeft + columnMap[targetColumn];
                WriteTrueColorPixel(pixels[sourceOffset + sourceColumn], destinationRowOffset + destinationX);
            }
        }
    }

    private void WriteTrueColorPixel(uint sourcePixel, int destinationOffset)
    {
        var alpha = (int)(sourcePixel >> 24);
        if (alpha == 0)
        {
            return;
        }

        var destinationPixel = GetVisiblePixel(destinationOffset);
        _trueColorPixels[destinationOffset] = alpha == 255
            ? ForceOpaque(sourcePixel)
            : BlendOverOpaque(sourcePixel, destinationPixel, alpha);
        _hasTrueColor[destinationOffset] = true;
    }

    private int[] GetScaleMap(int sourceLength, int targetLength)
    {
        var key = new ScaleMapKey(sourceLength, targetLength);
        if (_scaleMapCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var map = new int[targetLength];
        const int fractionBits = 16;
        var sourceFixed = 0L;
        var step = ((long)sourceLength << fractionBits) / targetLength;
        for (var index = 0; index < targetLength; index++)
        {
            map[index] = (int)(sourceFixed >> fractionBits);
            sourceFixed += step;
        }

        _scaleMapCache[key] = map;
        return map;
    }

    public ScreenFramebufferSnapshot CreateSnapshot()
    {
        var pixels = new uint[_colorIndices.Length];
        for (var index = 0; index < _colorIndices.Length; index++)
        {
            pixels[index] = GetVisiblePixel(index);
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
            0,
            0,
            snapshot.Width - 1L,
            snapshot.Height - 1L,
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
                0,
                0,
                snapshot.Width - 1L,
                snapshot.Height - 1L,
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
                    0,
                    0,
                    snapshot.Width - 1L,
                    snapshot.Height - 1L,
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
                    0,
                    0,
                    snapshot.Width - 1L,
                    snapshot.Height - 1L,
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

    private uint GetVisiblePixel(int offset) => _hasTrueColor[offset]
        ? _trueColorPixels[offset]
        : MapPaletteColor(_colorIndices[offset]);

    public static int NormalizeColorIndex(int color) => (int)(Math.Abs((long)color) % 256);

    public static uint MapRgbToBgra(int color) =>
        ToBgra((color >> 16) & 0xFF, (color >> 8) & 0xFF, color & 0xFF);

    private uint MapPaletteColor(int color) => _palette[NormalizeColorIndex(color)];

    private static uint ForceOpaque(uint pixel) => 0xFF000000u | (pixel & 0x00FFFFFFu);

    private static uint BlendOverOpaque(uint source, uint destination, int alpha)
    {
        var inverseAlpha = 255 - alpha;
        var sourceRed = (int)((source >> 16) & 0xFF);
        var sourceGreen = (int)((source >> 8) & 0xFF);
        var sourceBlue = (int)(source & 0xFF);
        var destinationRed = (int)((destination >> 16) & 0xFF);
        var destinationGreen = (int)((destination >> 8) & 0xFF);
        var destinationBlue = (int)(destination & 0xFF);

        var red = ((sourceRed * alpha) + (destinationRed * inverseAlpha) + 127) / 255;
        var green = ((sourceGreen * alpha) + (destinationGreen * inverseAlpha) + 127) / 255;
        var blue = ((sourceBlue * alpha) + (destinationBlue * inverseAlpha) + 127) / 255;
        return ToBgra(red, green, blue);
    }

    internal static int FindNearestDefaultPaletteIndex(uint pixel)
    {
        var red = (int)((pixel >> 16) & 0xFF);
        var green = (int)((pixel >> 8) & 0xFF);
        var blue = (int)(pixel & 0xFF);
        var bestIndex = 0;
        var bestDistance = int.MaxValue;

        for (var index = 0; index < 256; index++)
        {
            var palettePixel = MapColorToBgra(index);
            var paletteRed = (int)((palettePixel >> 16) & 0xFF);
            var paletteGreen = (int)((palettePixel >> 8) & 0xFF);
            var paletteBlue = (int)(palettePixel & 0xFF);
            var distance = SquaredDistance(red, paletteRed) + SquaredDistance(green, paletteGreen) + SquaredDistance(blue, paletteBlue);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestIndex = index;
            if (distance == 0)
            {
                break;
            }
        }

        return bestIndex;
    }

    private static int SquaredDistance(int left, int right)
    {
        var delta = left - right;
        return delta * delta;
    }

    private static long SaturatingAdd(long left, long right)
    {
        try
        {
            return checked(left + right);
        }
        catch (OverflowException)
        {
            return right >= 0 ? long.MaxValue : long.MinValue;
        }
    }

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

    private readonly record struct ScaleMapKey(int SourceLength, int TargetLength);
}

/// <summary>Опубликованный снимок владеет своим массивом пикселей; после публикации массив не изменяется.</summary>
public sealed record ScreenFramebufferSnapshot(int Width, int Height, uint[] BgraPixels, bool Smooth = false);
