namespace Axiom.Bytecode;

public sealed class BytecodeValidationResult
{
    public BytecodeValidationResult(IReadOnlyList<BytecodeDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<BytecodeDiagnostic> Diagnostics { get; }

    public bool Succeeded => Diagnostics.Count == 0;
}
