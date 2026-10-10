using Axiom.Core;

namespace Axiom.Execution;

public sealed class CompilationResult
{
    public CompilationResult(
        CompilationStatus status,
        IReadOnlyList<Diagnostic> diagnostics,
        CompiledProgram? compiledProgram,
        IReadOnlyList<Diagnostic>? backendDiagnostics = null,
        bool backendSucceeded = false,
        bool bytecodeAvailable = false,
        TimeSpan frontendPreparationDuration = default,
        TimeSpan loweringDuration = default,
        TimeSpan bytecodeEmissionDuration = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Status = status;
        Diagnostics = diagnostics;
        CompiledProgram = compiledProgram;
        BackendDiagnostics = backendDiagnostics ?? [];
        BackendSucceeded = backendSucceeded;
        BytecodeAvailable = bytecodeAvailable;
        FrontendPreparationDuration = frontendPreparationDuration;
        LoweringDuration = loweringDuration;
        BytecodeEmissionDuration = bytecodeEmissionDuration;
    }

    public CompilationStatus Status { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public CompiledProgram? CompiledProgram { get; }

    public bool FrontendSucceeded => Status is CompilationStatus.Success or CompilationStatus.BackendFailure;

    public IReadOnlyList<Diagnostic> BackendDiagnostics { get; }

    public bool BackendSucceeded { get; }

    public bool BytecodeAvailable { get; }

    public TimeSpan FrontendPreparationDuration { get; }

    public TimeSpan LoweringDuration { get; }

    public TimeSpan BytecodeEmissionDuration { get; }

    public bool Succeeded => Status == CompilationStatus.Success && BackendSucceeded && BytecodeAvailable && CompiledProgram?.BytecodeModule is not null;
}
