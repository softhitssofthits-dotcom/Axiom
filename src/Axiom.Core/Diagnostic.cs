namespace Axiom.Core;

public sealed record Diagnostic
{
    public Diagnostic(string code, string message, TextLocation location)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Diagnostic code must be provided.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Diagnostic message must be provided.", nameof(message));
        }

        Code = code;
        Message = message;
        Location = location;
    }

    public string Code { get; }

    public string Message { get; }

    public TextLocation Location { get; }

    public override string ToString() => $"{Code}: {Message} ({Location})";
}
