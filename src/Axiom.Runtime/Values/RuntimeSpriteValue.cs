namespace Axiom.Runtime;

public sealed class RuntimeSpriteValue
{
    private readonly int[] _frameLefts;
    private readonly int[] _frameTops;

    public RuntimeSpriteValue(RuntimeImageValue image, int frameWidth, int frameHeight)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (frameWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameWidth), frameWidth, "Sprite frame width must be positive.");
        }

        if (frameHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frameHeight), frameHeight, "Sprite frame height must be positive.");
        }

        Image = image;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        Columns = image.Width / frameWidth;
        Rows = image.Height / frameHeight;
        FrameCount = Columns * Rows;
        _frameLefts = new int[FrameCount];
        _frameTops = new int[FrameCount];

        for (var frame = 0; frame < FrameCount; frame++)
        {
            _frameLefts[frame] = (frame % Columns) * FrameWidth;
            _frameTops[frame] = (frame / Columns) * FrameHeight;
        }
    }

    public RuntimeImageValue Image { get; }

    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public int Columns { get; }

    public int Rows { get; }

    public int FrameCount { get; }

    public bool HasFrames => FrameCount > 0;

    public int GetFrameLeft(int frame) => _frameLefts[frame];

    public int GetFrameTop(int frame) => _frameTops[frame];
}
