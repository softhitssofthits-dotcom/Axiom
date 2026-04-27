using Axiom.Core;

namespace Axiom.Semantics;

public sealed class BoundRoutine : BoundNode
{
    public BoundRoutine(TextLocation location, FunctionSymbol symbol, IReadOnlyList<BoundStatement> statements)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(statements);

        Symbol = symbol;
        Statements = statements;
    }

    public override BoundNodeKind Kind => BoundNodeKind.Routine;

    public FunctionSymbol Symbol { get; }

    public IReadOnlyList<BoundStatement> Statements { get; }
}

public sealed class BoundProgram : BoundNode
{
    public BoundProgram(TextLocation location, IReadOnlyList<BoundStatement> globalStatements, IReadOnlyList<BoundRoutine> routines)
        : base(location)
    {
        ArgumentNullException.ThrowIfNull(globalStatements);
        ArgumentNullException.ThrowIfNull(routines);

        GlobalStatements = globalStatements;
        Routines = routines;
    }

    public override BoundNodeKind Kind => BoundNodeKind.Program;

    public IReadOnlyList<BoundStatement> GlobalStatements { get; }

    public IReadOnlyList<BoundRoutine> Routines { get; }
}
