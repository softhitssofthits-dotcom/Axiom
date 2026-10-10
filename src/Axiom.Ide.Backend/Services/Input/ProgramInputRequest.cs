namespace Axiom.Ide.Services.Input;

public sealed class ProgramInputRequest
{
    public ProgramInputRequest(int requestNumber, string? promptText, int valueCount)
    {
        if (requestNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestNumber));
        }

        if (valueCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valueCount));
        }

        RequestNumber = requestNumber;
        PromptText = promptText;
        ValueCount = valueCount;
    }

    public int RequestNumber { get; }

    public string? PromptText { get; }

    public int ValueCount { get; }
}
