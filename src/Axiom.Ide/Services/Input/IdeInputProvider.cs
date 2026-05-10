using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

internal sealed class IdeInputProvider : IInputProvider, IKeyboardInputProvider, IMouseInputProvider
{
    private readonly IProgramInputPromptService _promptService;
    private int _requestCount;

    public IdeInputProvider(IProgramInputPromptService promptService)
    {
        ArgumentNullException.ThrowIfNull(promptService);
        _promptService = promptService;
    }

    public string ReadInput(InputRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        _requestCount++;
        return _promptService.RequestInput(new ProgramInputRequest(_requestCount, request.PromptText, request.ValueCount));
    }

    public string ReadKey() =>
        _promptService is IKeyboardInputProvider keyboardInputProvider
            ? keyboardInputProvider.ReadKey()
            : string.Empty;

    public bool IsKeyDown(string keyText) =>
        _promptService is IKeyboardInputProvider keyboardInputProvider &&
        keyboardInputProvider.IsKeyDown(keyText);

    public long GetMouseX() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseX()
            : -1;

    public long GetMouseY() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.GetMouseY()
            : -1;

    public bool IsMouseButtonDown(int buttonNumber) =>
        _promptService is IMouseInputProvider mouseInputProvider &&
        mouseInputProvider.IsMouseButtonDown(buttonNumber);

    public int ReadMouseButtonPress() =>
        _promptService is IMouseInputProvider mouseInputProvider
            ? mouseInputProvider.ReadMouseButtonPress()
            : 0;
}
