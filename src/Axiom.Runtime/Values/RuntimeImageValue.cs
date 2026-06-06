namespace Axiom.Runtime;

public sealed class RuntimeImageValue
{
    public RuntimeImageValue(int width, int height, IReadOnlyList<uint> bgraPixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Image width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Image height must be positive.");
        }

        ArgumentNullException.ThrowIfNull(bgraPixels);
        if (bgraPixels.Count != width * height)
        {
            throw new ArgumentException("Image pixel count must match width * height.", nameof(bgraPixels));
        }

        Width = width;
        Height = height;
        BgraPixels = bgraPixels is uint[] array ? array : bgraPixels.ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public uint[] BgraPixels { get; }
}
