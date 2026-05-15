namespace Axiom.Runtime;

public static class GraphicsCircleValidation
{
    public const double MaxAspect = 1_000_000d;

    public static bool HasValidArguments(double radius, double? startAngle, double? endAngle, double aspect)
    {
        return double.IsFinite(radius)
            && radius >= 0d
            && double.IsFinite(aspect)
            && aspect > 0d
            && aspect <= MaxAspect
            && (!startAngle.HasValue || double.IsFinite(startAngle.Value))
            && (!endAngle.HasValue || double.IsFinite(endAngle.Value));
    }
}
