using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

public interface IProgramConsoleService : IProgramInputPromptService, IKeyboardInputProvider, IMouseInputProvider, IReversibleOutputWriter, IGraphicsHost
{
    void BeginSession();

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

    void ClearMouseState();
}
