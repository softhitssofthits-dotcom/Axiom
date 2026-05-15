namespace Axiom.Runtime;

public static class GraphicsCircleValidation
{
    public static bool HasValidArguments(double radius, double? startAngle, double? endAngle, double aspect)
    {
        return double.IsFinite(radius)
            && radius >= 0d
            && double.IsFinite(aspect)
            && aspect > 0d
            && (!startAngle.HasValue || double.IsFinite(startAngle.Value))
            && (!endAngle.HasValue || double.IsFinite(endAngle.Value));
    }
}
