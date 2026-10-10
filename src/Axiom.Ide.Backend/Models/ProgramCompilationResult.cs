namespace Axiom.Ide.Models;

public sealed class ProgramCompilationResult
{
    public ProgramCompilationResult(
        ProgramCompilationStatus status,
        IReadOnlyList<ProgramDiagnosticItem> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Status = status;
        Diagnostics = diagnostics;
    }

    public ProgramCompilationStatus Status { get; }

    public IReadOnlyList<ProgramDiagnosticItem> Diagnostics { get; }

    public bool Succeeded => Status == ProgramCompilationStatus.Success;
}
