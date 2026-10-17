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
    void SetMousePosition(long x, long y);

    // Атомарная публикация: позиция, состояние кнопки и событие относятся
    // к одному моменту и не могут быть прочитаны обработчиком порознь.
    void PublishMouseMove(long x, long y);
    void PublishMouseButtonPress(int buttonNumber, long x, long y);
    void PublishMouseButtonRelease(int buttonNumber, long x, long y);
    void PublishMouseWheel(int delta, long x, long y);
    void PublishMouseButtonReleaseKeepPosition(int buttonNumber);
    void ClearMouseState();
}
