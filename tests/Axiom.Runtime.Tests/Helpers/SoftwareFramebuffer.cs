namespace Axiom.Runtime.Tests.Helpers;

public static class SoftwareFramebuffer
{
    public static byte[] ToBmp(IReadOnlyDictionary<(long X, long Y), int> pixels, int width, int height)
    {
        var rowBytes = (width * 3 + 3) & ~3;
        var pixelDataSize = rowBytes * height;
        var fileSize = 54 + pixelDataSize;

        using var stream = new MemoryStream(fileSize);
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0);
        writer.Write(0);

        var padding = rowBytes - width * 3;
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var color = pixels.TryGetValue((x, y), out var c) ? c : 0;
                var (r, g, b) = MapPaletteIndexToRgb(color);
                writer.Write((byte)b);
                writer.Write((byte)g);
                writer.Write((byte)r);
            }

            for (var p = 0; p < padding; p++)
            {
                writer.Write((byte)0);
            }
        }

        return stream.ToArray();
    }

    public static (int MismatchCount, long FirstDiffX, long FirstDiffY) CompareBmp(byte[] actual, byte[] expected)
    {
        if (actual.Length != expected.Length)
        {
            return (1, -1, -1);
        }

        var width = BitConverter.ToInt32(actual, 18);
        var height = BitConverter.ToInt32(actual, 22);
        var rowBytes = (width * 3 + 3) & ~3;
        var offset = 54;
        var mismatches = 0;
        var firstX = -1L;
        var firstY = -1L;

        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var i = offset + (height - 1 - y) * rowBytes + x * 3;
                if (actual[i] != expected[i] || actual[i + 1] != expected[i + 1] || actual[i + 2] != expected[i + 2])
                {
                    if (mismatches == 0)
                    {
                        firstX = x;
                        firstY = y;
                    }

                    mismatches++;
                }
            }
        }

        return (mismatches, firstX, firstY);
    }

    public static (int MismatchCount, long FirstDiffX, long FirstDiffY) ComparePixels(
        IReadOnlyDictionary<(long X, long Y), int> actual,
        IReadOnlyDictionary<(long X, long Y), int> expected,
        int width, int height, int colorTolerance = 0)
    {
        var mismatches = 0;
        var firstX = -1L;
        var firstY = -1L;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var actualColor = actual.TryGetValue((x, y), out var ac) ? ac : 0;
                var expectedColor = expected.TryGetValue((x, y), out var ec) ? ec : 0;

                if (colorTolerance == 0)
                {
                    if (actualColor != expectedColor)
                    {
                        if (mismatches == 0)
                        {
                            firstX = x;
                            firstY = y;
                        }

                        mismatches++;
                    }
                }
                else
                {
                    var (ar, ag, ab) = MapPaletteIndexToRgb(actualColor);
                    var (er, eg, eb) = MapPaletteIndexToRgb(expectedColor);
                    var dr = Math.Abs(ar - er);
                    var dg = Math.Abs(ag - eg);
                    var db = Math.Abs(ab - eb);

                    if (dr > colorTolerance || dg > colorTolerance || db > colorTolerance)
                    {
                        if (mismatches == 0)
                        {
                            firstX = x;
                            firstY = y;
                        }

                        mismatches++;
                    }
                }
            }
        }

        return (mismatches, firstX, firstY);
    }

    internal static (int Red, int Green, int Blue) MapPaletteIndexToRgb(int colorIndex)
    {
        var normalized = Math.Abs(colorIndex) % 256;
        return normalized switch
        {
            0 => (0, 0, 0),
            1 => (0, 0, 170),
            2 => (0, 170, 0),
            3 => (0, 170, 170),
            4 => (170, 0, 0),
            5 => (170, 0, 170),
            6 => (170, 85, 0),
            7 => (170, 170, 170),
            8 => (85, 85, 85),
            9 => (85, 85, 255),
            10 => (85, 255, 85),
            11 => (85, 255, 255),
            12 => (255, 85, 85),
            13 => (255, 85, 255),
            14 => (255, 255, 85),
            15 => (255, 255, 255),
            >= 16 and <= 231 => MapRgbCubeIndexToRgb(normalized),
            _ => MapGrayIndexToRgb(normalized)
        };
    }

    private static (int Red, int Green, int Blue) MapRgbCubeIndexToRgb(int colorIndex)
    {
        var cubeIndex = colorIndex - 16;
        var red = cubeIndex / 36;
        var green = cubeIndex / 6 % 6;
        var blue = cubeIndex % 6;
        return (MapRgbCubeComponent(red), MapRgbCubeComponent(green), MapRgbCubeComponent(blue));
    }

    private static (int Red, int Green, int Blue) MapGrayIndexToRgb(int colorIndex)
    {
        var component = 8 + ((colorIndex - 232) * 10);
        return (component, component, component);
    }

    private static int MapRgbCubeComponent(int component) => component == 0 ? 0 : 55 + (component * 40);
}
