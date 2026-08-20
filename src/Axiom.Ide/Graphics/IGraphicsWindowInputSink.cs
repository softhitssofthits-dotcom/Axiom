namespace Axiom.Ide.Graphics;

/// <summary>
/// Интерфейс ввода для NativeGraphicsScreenWindow.
/// Позволяет окну SCREEN отправлять события ввода без привязки к конкретной реализации.
/// </summary>
public interface IGraphicsWindowInputSink
{
    bool IsWaitingForInput { get; }
    void EnqueueKey(string keyText);
    void SetKeyDown(string keyText);
    void SetKeyUp(string keyText);
    void SubmitInput();
    void BackspaceInputCharacter();
    void AppendInputCharacter(char character);
    void EnqueueMouseButtonPress(int buttonNumber);
    void SetMouseButtonDown(int buttonNumber);
    void EnqueueMouseButtonRelease(int buttonNumber);
    void SetMouseButtonUp(int buttonNumber);
    void EnqueueMouseMove();
    void EnqueueMouseWheel(int delta);
    void SetMousePosition(long x, long y);
}
