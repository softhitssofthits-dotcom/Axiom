using Axiom.Runtime;

namespace Axiom.Ide.Services.Input;

internal sealed class IdeInputProvider : IInputProvider
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
}
