namespace Axiom.Runtime;

public enum MouseInputEventKind
{
    None,
    Press,
    Release,
    Move,
    Wheel
}

public readonly record struct MouseInputEvent(MouseInputEventKind Kind, int ButtonNumber);

public interface IMouseInputProvider
{
    long GetMouseX();

    long GetMouseY();

    // Атомарный снимок позиции: X и Y должны относиться к одному моменту.
    void GetMousePosition(out long x, out long y)
    {
        x = GetMouseX();
        y = GetMouseY();
    }

    // Поколение очистки позволяет VM сбросить собственный буфер отложенных
    // событий, который не принадлежит снимку ввода.
    long GetMouseClearGeneration() => 0;

    bool IsMouseButtonDown(int buttonNumber);

    int ReadMouseButtonPress();

    MouseInputEvent ReadMouseEvent()
    {
        var buttonNumber = ReadMouseButtonPress();
        return buttonNumber == 0
            ? default
            : new MouseInputEvent(MouseInputEventKind.Press, buttonNumber);
    }
}
