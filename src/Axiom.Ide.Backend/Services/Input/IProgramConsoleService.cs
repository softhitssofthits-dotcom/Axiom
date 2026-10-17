using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public interface IProgramConsoleService : IProgramInputPromptService, IKeyboardInputProvider, IMouseInputProvider, IReversibleOutputWriter, IGraphicsHost
{
    string? ResourceBaseDirectory { get; set; }

    void BeginSession();

    void EndSession();

    void Clear();

    void ClearOutput();

    void EnqueueKey(string keyText);

    void SetKeyDown(string keyText);

    void SetKeyUp(string keyText);

    void ClearKeyState();

    void SetMousePosition(long x, long y);

    void SetMouseButtonDown(int buttonNumber);

    void SetMouseButtonUp(int buttonNumber);

    void EnqueueMouseButtonPress(int buttonNumber);

    void EnqueueMouseButtonRelease(int buttonNumber);

    void EnqueueMouseMove();

    void EnqueueMouseWheel(int delta);

    void PublishMouseMove(long x, long y);

    void PublishMouseButtonPress(int buttonNumber, long x, long y);

    void PublishMouseButtonRelease(int buttonNumber, long x, long y);

    void PublishMouseWheel(int delta, long x, long y);

    void PublishMouseButtonReleaseKeepPosition(int buttonNumber);

    void ClearMouseState();
}
