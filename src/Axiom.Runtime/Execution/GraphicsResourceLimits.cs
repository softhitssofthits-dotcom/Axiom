namespace Axiom.Runtime;

/// <summary>Общие пределы изображений и поверхностей до выделения памяти или обращения к DirectX.</summary>
public static class GraphicsResourceLimits
{
    public const int MaximumDimension = 16384;
    public const int MaximumPixels = 16 * 1024 * 1024;
    public const int MaximumTextRows = 16384;
    public const int MaximumTextColumns = 16384;
    public const long MaximumResourceFileBytes = 64 * 1024 * 1024;

    public static bool IsSupported(long width, long height) =>
        width is > 0 and <= MaximumDimension && height is > 0 and <= MaximumDimension &&
        width * height <= MaximumPixels;

    public static int Validate(int width, int height)
    {
        if (!IsSupported(width, height))
            throw new ArgumentOutOfRangeException(nameof(width),
                $"Graphics dimensions must be 1..{MaximumDimension}; area must not exceed {MaximumPixels} pixels.");
        return checked(width * height);
    }
}
