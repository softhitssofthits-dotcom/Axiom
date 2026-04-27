using Axiom.Core;

namespace Axiom.Bytecode;

public sealed class BytecodeDiagnostic
{
    public BytecodeDiagnostic(string code, string message, TextLocation location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        Location = location;
    }

    public string Code { get; }

    public string Message { get; }

    public TextLocation Location { get; }
}
