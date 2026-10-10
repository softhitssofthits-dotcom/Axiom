namespace Axiom.Semantics;

/// <summary>Состояние поверхности до первого оператора компилируемой программы.</summary>
public enum InitialGraphicsContext
{
    None,
    Unknown,
    TextScreen,
    GraphicsScreen,
    Canvas
}
