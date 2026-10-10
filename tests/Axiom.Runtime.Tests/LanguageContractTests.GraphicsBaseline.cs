using Axiom.Runtime.Tests.Helpers;

namespace Axiom.Runtime.Tests;

public sealed partial class LanguageContractTests
{
    private (Dictionary<(long X, long Y), int> Pixels, RuntimeResult Result) ExecuteGraphicsProgram(string source)
    {
        var (_, host, result) = ExecuteProgram(source);
        return (GetPixels(host), result);
    }

    private static Dictionary<(long X, long Y), int> GetPixels(TestRuntimeHost host)
    {
        var pixels = new Dictionary<(long X, long Y), int>();
        for (var y = 0L; y < host.ScreenHeight; y++)
        {
            for (var x = 0L; x < host.ScreenWidth; x++)
            {
                var color = host.GetAbsolutePixel(x, y);
                if (color != 0)
                {
                    pixels[(x, y)] = color;
                }
            }
        }

        return pixels;
    }

    private static Dictionary<(long X, long Y), int> GetCanvasPixels(TestRuntimeHost host, int width, int height)
    {
        var pixels = new Dictionary<(long X, long Y), int>();
        for (var y = 0L; y < height; y++)
        {
            for (var x = 0L; x < width; x++)
            {
                var color = host.GetAbsolutePixel(x, y);
                if (color != 0)
                {
                    pixels[(x, y)] = color;
                }
            }
        }

        return pixels;
    }

    private static Dictionary<(long X, long Y), int> GetAllCanvasPixels(TestRuntimeHost host, int width, int height)
    {
        var pixels = new Dictionary<(long X, long Y), int>();
        for (var y = 0L; y < height; y++)
        {
            for (var x = 0L; x < width; x++)
            {
                pixels[(x, y)] = host.GetAbsolutePixel(x, y);
            }
        }

        return pixels;
    }

    [Fact]
    public void Baseline_Screen_Line_DrawsHorizontalLine()
    {
        const string source = """
SCREEN 1
LINE (10, 5)-(20, 5), 12
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        for (var x = 10; x <= 20; x++)
        {
            Assert.True(pixels.ContainsKey((x, 5)), $"Expected pixel at ({x}, 5)");
            Assert.Equal(12, pixels[(x, 5)]);
        }

        Assert.Equal(11, pixels.Count);
    }

    [Fact]
    public void Baseline_Screen_Line_DrawsDiagonalLine()
    {
        const string source = """
SCREEN 1
LINE (0, 0)-(10, 10), 15
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        Assert.True(pixels.ContainsKey((0, 0)), "Expected pixel at (0, 0)");
        Assert.True(pixels.ContainsKey((10, 10)), "Expected pixel at (10, 10)");
        Assert.True(pixels.ContainsKey((5, 5)), "Expected pixel at (5, 5)");
        Assert.True(pixels.Count >= 11, $"Expected at least 11 pixels for diagonal, got {pixels.Count}");
        Assert.All(pixels.Values, c => Assert.Equal(15, c));
    }

    [Fact]
    public void Baseline_Screen_Rect_FillsRectangle()
    {
        const string source = """
SCREEN 1
LINE (10, 10)-(20, 15), 9, BF
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        for (var y = 10; y <= 15; y++)
        {
            for (var x = 10; x <= 20; x++)
            {
                Assert.True(pixels.ContainsKey((x, y)), $"Expected pixel at ({x}, {y})");
                Assert.Equal(9, pixels[(x, y)]);
            }
        }

        Assert.Equal(11 * 6, pixels.Count);
    }

    [Fact]
    public void Baseline_Screen_Rect_DrawsBox()
    {
        const string source = """
SCREEN 1
LINE (10, 10)-(20, 15), 14, B
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        for (var x = 10; x <= 20; x++)
        {
            Assert.True(pixels.ContainsKey((x, 10)), $"Expected top edge pixel at ({x}, 10)");
            Assert.True(pixels.ContainsKey((x, 15)), $"Expected bottom edge pixel at ({x}, 15)");
            Assert.Equal(14, pixels[(x, 10)]);
            Assert.Equal(14, pixels[(x, 15)]);
        }

        for (var y = 11; y <= 14; y++)
        {
            Assert.True(pixels.ContainsKey((10, y)), $"Expected left edge pixel at (10, {y})");
            Assert.True(pixels.ContainsKey((20, y)), $"Expected right edge pixel at (20, {y})");
            Assert.Equal(14, pixels[(10, y)]);
            Assert.Equal(14, pixels[(20, y)]);
        }

        var interiorPixelCount = 0;
        for (var y = 11; y <= 14; y++)
        {
            for (var x = 11; x <= 19; x++)
            {
                if (pixels.ContainsKey((x, y)))
                {
                    interiorPixelCount++;
                }
            }
        }

        Assert.Equal(0, interiorPixelCount);
    }

    [Fact]
    public void Baseline_Screen_Circle_DrawsEllipse()
    {
        const string source = """
SCREEN 1
CIRCLE (50, 50), 10, 11
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        Assert.True(pixels.ContainsKey((50, 40)), "Expected pixel at top of circle (50, 40)");
        Assert.True(pixels.ContainsKey((50, 60)), "Expected pixel at bottom of circle (50, 60)");
        Assert.True(pixels.ContainsKey((40, 50)), "Expected pixel at left of circle (40, 50)");
        Assert.True(pixels.ContainsKey((60, 50)), "Expected pixel at right of circle (60, 50)");
        Assert.True(pixels.Count >= 30, $"Expected at least 30 pixels for circle outline, got {pixels.Count}");
        Assert.All(pixels.Values, c => Assert.Equal(11, c));
    }

    [Fact]
    public void Baseline_Screen_Pset_SetsPixels()
    {
        const string source = """
SCREEN 1
PSET (0, 0), 1
PSET (100, 50), 2
PSET (319, 199), 3
PSET (160, 100), 15
""";

        var (pixels, result) = ExecuteGraphicsProgram(source);

        Assert.True(result.Succeeded);
        Assert.Equal(4, pixels.Count);
        Assert.Equal(1, pixels[(0, 0)]);
        Assert.Equal(2, pixels[(100, 50)]);
        Assert.Equal(3, pixels[(319, 199)]);
        Assert.Equal(15, pixels[(160, 100)]);
    }

    [Fact]
    public void Baseline_Canvas_ClearFrame_FillsAllPixels()
    {
        const string source = """
CANVAS 8, 6
CLS RGB(255, 0, 0)
""";

        var (_, host, result) = ExecuteProgram(source);

        Assert.True(result.Succeeded);
        var pixels = GetAllCanvasPixels(host, 8, 6);
        Assert.Equal(48, pixels.Count);
        var sampleColor = pixels[(0, 0)];
        Assert.NotEqual(0, sampleColor);
        var (r, g, b) = SoftwareFramebuffer.MapPaletteIndexToRgb(sampleColor);
        Assert.Equal(255, r);
        Assert.Equal(0, g);
        Assert.Equal(0, b);
        Assert.All(pixels.Values, c => Assert.Equal(sampleColor, c));
    }

    [Fact]
    public void Baseline_Canvas_FillRect_DrawsFilledRectangle()
    {
        const string source = """
CANVAS 32, 24
CLS RGB(0, 0, 0)
FILLRECT 4, 4, 8, 6, RGB(0, 255, 0)
""";

        var (_, host, result) = ExecuteProgram(source);

        Assert.True(result.Succeeded);
        var pixels = GetAllCanvasPixels(host, 32, 24);
        var greenIndex = pixels[(4, 4)];
        Assert.NotEqual(0, greenIndex);

        for (var y = 4; y <= 9; y++)
        {
            for (var x = 4; x <= 11; x++)
            {
                Assert.Equal(greenIndex, pixels[(x, y)]);
            }
        }

        Assert.Equal(0, pixels[(3, 4)]);
        Assert.Equal(0, pixels[(12, 4)]);
        Assert.Equal(0, pixels[(4, 3)]);
        Assert.Equal(0, pixels[(4, 10)]);
    }

    [Fact]
    public void Baseline_Viewport_ClipsDrawing()
    {
        const string source = """
CANVAS 32, 24
CLS RGB(0, 0, 0)
VIEWPORT (8, 8)-(16, 16)
FILLRECT 0, 0, 100, 100, RGB(255, 255, 255)
""";

        var (_, host, result) = ExecuteProgram(source);

        Assert.True(result.Succeeded);
        var pixels = GetAllCanvasPixels(host, 32, 24);
        var whiteIndex = pixels[(8, 8)];
        Assert.NotEqual(0, whiteIndex);

        for (var y = 8; y <= 15; y++)
        {
            for (var x = 8; x <= 15; x++)
            {
                Assert.True(pixels.ContainsKey((x, y)), $"Expected pixel at ({x}, {y})");
                Assert.Equal(whiteIndex, pixels[(x, y)]);
            }
        }

        Assert.Equal(0, pixels[(7, 8)]);
        Assert.Equal(0, pixels[(16, 8)]);
        Assert.Equal(0, pixels[(8, 7)]);
        Assert.Equal(0, pixels[(8, 16)]);
        Assert.Equal(0, pixels[(0, 0)]);
        Assert.Equal(0, pixels[(31, 23)]);
    }
}
