using Axiom.Bytecode;
using Axiom.Semantics;
using Axiom.Syntax;

namespace Axiom.Execution;

public sealed class CompiledProgram
{
    public CompiledProgram(
        string sourceText,
        ProgramSyntax syntax,
        BoundProgram boundProgram,
        LoweredProgram? loweredProgram = null,
        BytecodeModule? bytecodeModule = null)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentNullException.ThrowIfNull(boundProgram);

        SourceText = sourceText;
        Syntax = syntax;
        BoundProgram = boundProgram;
        LoweredProgram = loweredProgram;
        BytecodeModule = bytecodeModule;
    }

    public string SourceText { get; }

    public ProgramSyntax Syntax { get; }

    public BoundProgram BoundProgram { get; }

    public LoweredProgram? LoweredProgram { get; }

    public BytecodeModule? BytecodeModule { get; }
}
