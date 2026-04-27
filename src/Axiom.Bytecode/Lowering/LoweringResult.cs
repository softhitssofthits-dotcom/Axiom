namespace Axiom.Bytecode;

public sealed class LoweringResult
{
    public LoweringResult(LoweredProgram? program, IReadOnlyList<BytecodeDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Program = program;
        Diagnostics = diagnostics;
    }

    public LoweredProgram? Program { get; }

    public IReadOnlyList<BytecodeDiagnostic> Diagnostics { get; }

    public bool Succeeded => Program is not null && Diagnostics.Count == 0;
}
