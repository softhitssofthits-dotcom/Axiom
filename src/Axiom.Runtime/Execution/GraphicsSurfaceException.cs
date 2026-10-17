namespace Axiom.Runtime;

/// <summary>Ожидаемый отказ создания графического устройства или поверхности.</summary>
public sealed class GraphicsSurfaceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
