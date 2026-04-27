namespace Axiom.Bytecode;

public sealed class BytecodeCompilerResult
{
    public BytecodeCompilerResult(BytecodeModule? module, IReadOnlyList<BytecodeDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Module = module;
        Diagnostics = diagnostics;
    }

    public BytecodeModule? Module { get; }

    public IReadOnlyList<BytecodeDiagnostic> Diagnostics { get; }

    public bool Succeeded => Module is not null && Diagnostics.Count == 0;
}
