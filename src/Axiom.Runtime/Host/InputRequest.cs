namespace Axiom.Runtime;

public sealed class InputRequest
{
    public InputRequest(string? promptText, int valueCount)
    {
        PromptText = promptText;
        ValueCount = valueCount < 1 ? 1 : valueCount;
    }

    public string? PromptText { get; }

    public int ValueCount { get; }
}
