namespace Axiom.Core;

public enum DiagnosticSeverity
{
    Error = 0,
    Warning = 1,
}

public sealed record Diagnostic
{
    public Diagnostic(string code, string message, TextLocation location, DiagnosticSeverity severity = DiagnosticSeverity.Error)
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
        Severity = severity;
    }

    public string Code { get; }

    public string Message { get; }

    public TextLocation Location { get; }

    public DiagnosticSeverity Severity { get; }

    public override string ToString() => $"{Code}: {Message} ({Location})";
}
