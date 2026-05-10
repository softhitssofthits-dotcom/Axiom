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
